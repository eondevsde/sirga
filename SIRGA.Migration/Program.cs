using System.Data;
using System.Text;
using Microsoft.Data.SqlClient;

namespace SIRGA.Migration;

public static class Program
{
    private static int _unionId;
    private static int _ramaId;
    private static decimal _lawPct = 2m;
    private static decimal _memberPct = 2m;

    private static readonly Dictionary<string, int> _provByLetter = new();
    private static readonly Dictionary<string, int> _cityByKey = new();      // (letra|codloc) -> CityId
    private static readonly Dictionary<string, int> _companyByCuit = new();
    private static readonly Dictionary<string, int> _employeeByCuil = new();
    private static readonly Dictionary<string, int> _assignmentByKey = new(); // (cuil|cuit) -> AssignmentId
    private static readonly Dictionary<string, int> _registryByKey = new();   // (cuit|yyyy-MM) -> DeclarationId
    private static readonly Dictionary<string, int> _rectByKey = new();        // (cuit|yyyy-MM) -> rect (última versión)
    private static readonly Dictionary<int, int> _studioById = new();         // legacy EstudioContableId -> StudioId
    private static readonly Dictionary<string, int> _categoryByCode = new();

    public static async Task<int> Main(string[] args)
    {
        var step = args.Length > 0 && !args[0].StartsWith("--") ? args[0].ToLowerInvariant() : "all";

        // Parámetros opcionales: --origen <base o cadena> --destino <base o cadena> --simulacro
        for (var i = 0; i < args.Length; i++)
        {
            var sig = i + 1 < args.Length ? args[i + 1] : null;
            if (args[i] is "--origen" or "--legado" && sig is not null) { Db.LegacyCs = sig.Contains('=') ? sig : Db.Local(sig); i++; }
            else if (args[i] == "--destino" && sig is not null) { Db.TargetCs = sig.Contains('=') ? sig : Db.Local(sig); i++; }
            else if (args[i] == "--simulacro") Db.Simulacro = true;
            else if (args[i] == "--alinear-estados") AlinearEstados();
        }
        Console.OutputEncoding = Encoding.UTF8;
        Log.Section($"SIRGA Migration - step={step}");

        try
        {
            switch (step)
            {
                case "geo": Geo(); break;
                case "union": Union(); break;
                case "interest": Interest(); break;
                case "categories": Categories(); break;
                case "scales": Scales(); break;
                case "companies": Companies(); break;
                case "employees": Employees(); break;
                case "assignments": Assignments(); break;
                case "studios": Studios(); break;
                case "declarations": Declarations(); break;
                case "sync": CargarMapasDesdeDestino(); Declarations(); Details(); RefreshDesdeLegado(); break;
                case "refresh": RefreshDesdeLegado(); break;
                case "align": AlinearEstados(); break;
                case "details": Details(); break;
                case "finalize": FinalizeTotals(); break;
                case "reconcile": Reconcile(); break;
                case "check": CheckConsistencia(); break;
                case "clean": Clean(); break;
                case "all":
                    Clean();
                    Geo(); Union(); Interest(); Categories(); Scales();
                    Companies(); Employees(); Assignments(); Studios();
                    Declarations(); Details(); FinalizeTotals();
                    Reconcile();
                    break;
                default:
                    Console.WriteLine($"Paso desconocido: {step}. Use: geo|union|interest|categories|scales|companies|employees|assignments|studios|declarations|details|reconcile|clean|all");
                    return 2;
            }
            Log.Section("Migración completada OK");
            return 0;
        }
        catch (Exception ex)
        {
            Log.Write("ERROR: " + ex.Message);
            Log.Write(ex.StackTrace ?? "");
            return 1;
        }
    }

    // ---------- LIMPIEZA ----------
    private static void Clean()
    {
        Log.Section("Clean: eliminando datos de prueba del destino");
        var sql = """
            TRUNCATE TABLE DeclarationDetails;
            DELETE FROM PaymentPlanInstallments;
            DELETE FROM PaymentPlans;
            DELETE FROM PaymentTransactions;
            DELETE FROM CompanyBalances;
            DELETE FROM BalanceHistories;
            DELETE FROM Declarations;
            DELETE FROM Assignments;
            DELETE FROM SalaryScales;
            DELETE FROM EmployeeCategories;
            DELETE FROM Companies;
            DELETE FROM Employees;
            // AccountingStudios: NO se borran (upsert por ResponsibleUsername) para no perder
            // estudios registrados desde la app.
            DELETE FROM InterestRates;
            DELETE FROM Unions;
            DELETE FROM Cities;
            DELETE FROM Provinces;
            DELETE FROM AuditLogs;
            DELETE FROM InspectionActPeriods;
            DELETE FROM InspectionActs;
            DELETE FROM InspectionVisits;
            DELETE FROM PrejudicialNotices;
            DELETE FROM NoticeAttachments;
            DELETE FROM UnionSubscriptionPayments;
            DELETE FROM UnionSubscriptions;
            DELETE FROM UnionSubscriptionRequests;
            DELETE FROM AssociationRequests;
            DELETE FROM Inspectors;
            """;
        Db.Run(Db.TargetCs, sql);
        Log.Write("Limpieza OK");
    }

    // ---------- GEOGRAFIA ----------
    private static void Geo()
    {
        Log.Section("Geo: provincias y localidades desde legado");
        using var src = Db.Open(Db.LegacyCs);

        // Provincias
        using (var prov = new SqlCommand("SELECT MAEPROV_CODPRO, MAEPROV_NOMBRE FROM provincias ORDER BY MAEPROV_CODPRO", src))
        using (var r = prov.ExecuteReader())
        {
            while (r.Read())
            {
                var letter = Db.Str(r["MAEPROV_CODPRO"]);
                var name = Db.Str(r["MAEPROV_NOMBRE"]);
                if (name.Length == 0) continue;
                using var conn = Db.Open(Db.TargetCs);
                using var cmd = new SqlCommand("INSERT INTO Provinces (Name) OUTPUT INSERTED.Id VALUES (@name)", conn);
                cmd.Parameters.AddWithValue("@name", name);
                var id = (int)cmd.ExecuteScalar();
                _provByLetter[letter] = id;
            }
        }
        Log.Write($"Provincias: {_provByLetter.Count}");

        // Localidades
        using (var loc = new SqlCommand(
            "SELECT MAELOC_CODPROV, MAELOC_CODLOC, MAELOC_NOMBRE FROM localidad WHERE MAELOC_CODLOC <> 0 AND MAELOC_NOMBRE IS NOT NULL AND LTRIM(RTRIM(MAELOC_NOMBRE)) <> '' ORDER BY MAELOC_CODLOC", src))
        using (var r = loc.ExecuteReader())
        {
            var count = 0;
            while (r.Read())
            {
                var letter = Db.Str(r["MAELOC_CODPROV"]);
                var cod = Convert.ToInt32(r["MAELOC_CODLOC"]);
                var name = Db.Str(r["MAELOC_NOMBRE"]);
                if (!_provByLetter.TryGetValue(letter, out var provId)) continue;
                using var conn = Db.Open(Db.TargetCs);
                using var cmd = new SqlCommand("INSERT INTO Cities (Name, ProvinceId) OUTPUT INSERTED.Id VALUES (@name, @prov)", conn);
                cmd.Parameters.AddWithValue("@name", name);
                cmd.Parameters.AddWithValue("@prov", provId);
                var cityId = (int)cmd.ExecuteScalar();
                _cityByKey[$"{letter}|{cod}"] = cityId;
                count++;
            }
            Log.Write($"Localidades: {count}");
        }
    }

    // ---------- UNION ----------
    private static void Union()
    {
        Log.Section("Union: desde Filial (SEC)");
        using var src = Db.Open(Db.LegacyCs);
        using var cmd = new SqlCommand(
            "SELECT Nombre, Provincia, Localidad, PorcentajeDescuentoLey, PorcentajeDescuentoSocio FROM Filial WHERE Id=1", src);
        using var r = cmd.ExecuteReader();
        if (!r.Read()) throw new Exception("Filial Id=1 no encontrada");
        var nombre = Db.Str(r["Nombre"]);
        var provincia = Db.Str(r["Provincia"]);
        var localidad = Db.Str(r["Localidad"]);
        if (r["PorcentajeDescuentoLey"] != DBNull.Value && Convert.ToDecimal(r["PorcentajeDescuentoLey"]) > 0)
            _lawPct = Convert.ToDecimal(r["PorcentajeDescuentoLey"]);
        if (r["PorcentajeDescuentoSocio"] != DBNull.Value && Convert.ToDecimal(r["PorcentajeDescuentoSocio"]) > 0)
            _memberPct = Convert.ToDecimal(r["PorcentajeDescuentoSocio"]);
        r.Close();

        using var conn = Db.Open(Db.TargetCs);
        using var ins = new SqlCommand("""
            INSERT INTO Unions (Name, JurisdictionProvince, CctCode, IsActive, JurisdictionCity, LawContributionPercentage, UnionMemberPercentage, AdminEmail)
            OUTPUT INSERTED.Id VALUES (@nombre, @prov, '130/75', 1, @localidad, @ley, @socio, 'sindicato@sirga.com')
            """, conn);
        ins.Parameters.AddWithValue("@nombre", nombre);
        ins.Parameters.AddWithValue("@prov", provincia);
        ins.Parameters.AddWithValue("@localidad", localidad);
        ins.Parameters.AddWithValue("@ley", _lawPct);
        ins.Parameters.AddWithValue("@socio", _memberPct);
        _unionId = (int)ins.ExecuteScalar();
        Log.Write($"Union Id={_unionId}: {nombre} (ley {_lawPct}%, socio {_memberPct}%)");

        // Las categorías y las escalas cuelgan de la RAMA (el convenio), no del sindicato. Este
        // importador es de Comercio 130/75, así que resuelve (o crea) esa rama y deja el sindicato
        // vinculado: sin eso, el sindicato queda sin rama y no se puede liquidar.
        using var connRama = Db.Open(Db.TargetCs);
        using (var sel = new SqlCommand("SELECT Id FROM Ramas WHERE Code = '130/75'", connRama))
        {
            if (sel.ExecuteScalar() is int idExistente) _ramaId = idExistente;
        }
        if (_ramaId == 0)
        {
            using var insRama = new SqlCommand(
                "INSERT INTO Ramas (Code, Name, Notes, IsActive) OUTPUT INSERTED.Id VALUES ('130/75', 'Comercio', 'Empleados de Comercio', 1)", connRama);
            _ramaId = (int)insRama.ExecuteScalar();
        }
        using (var upd = new SqlCommand("UPDATE Unions SET RamaId = @rama WHERE Id = @id", connRama))
        {
            upd.Parameters.AddWithValue("@rama", _ramaId);
            upd.Parameters.AddWithValue("@id", _unionId);
            upd.ExecuteNonQuery();
        }
        Log.Write($"Rama Id={_ramaId} (130/75) vinculada al sindicato {_unionId}");
    }

    // ---------- TASA DE INTERES ----------
    private static void Interest()
    {
        Log.Section("InterestRate: 3% mensual default");
        using var conn = Db.Open(Db.TargetCs);
        using var cmd = new SqlCommand(
            "INSERT INTO InterestRates (EffectiveDate, MonthlyRate, Description) VALUES ('1900-01-01', 0.03, 'Tasa mensual por mora (default SIRGA)')", conn);
        cmd.ExecuteNonQuery();
        Log.Write("InterestRate OK");
    }

    // ---------- CATEGORIAS ----------
    private static void Categories()
    {
        Log.Section("EmployeeCategories: desde categorias_empleado (excl. TODAS)");
        using var src = Db.Open(Db.LegacyCs);
        using var cmd = new SqlCommand(
            "SELECT MAECAT_CODCAT, MAECAT_NOMCAT FROM categorias_empleado WHERE MAECAT_CODCAT <> 0 ORDER BY MAECAT_CODCAT", src);
        using var r = cmd.ExecuteReader();
        var count = 0;
        while (r.Read())
        {
            var code = Convert.ToInt32(r["MAECAT_CODCAT"]).ToString();
            var name = Db.Str(r["MAECAT_NOMCAT"]);
            if (name.Length == 0) continue;
            using var conn = Db.Open(Db.TargetCs);
            using var ins = new SqlCommand(
                "INSERT INTO EmployeeCategories (Code, Name, RamaId, IsActive) OUTPUT INSERTED.Id VALUES (@code, @name, @rama, 1)", conn);
            ins.Parameters.AddWithValue("@code", code);
            ins.Parameters.AddWithValue("@name", name);
            ins.Parameters.AddWithValue("@rama", _ramaId);
            _categoryByCode[code] = (int)ins.ExecuteScalar();
            count++;
        }
        Log.Write($"Categorías: {count}");
    }

    // ---------- ESCALAS ----------
    private static void Scales()
    {
        Log.Section("SalaryScales: desde EscalaSalarial");
        using var src = Db.Open(Db.LegacyCs);
        using var cmd = new SqlCommand(
            "SELECT Periodo, CodCategoria, Importe, AcuerdoNR1, AcuerdoNR2, Estado FROM EscalaSalarial ORDER BY Periodo, CodCategoria", src);
        using var r = cmd.ExecuteReader();
        var count = 0;
        while (r.Read())
        {
            var code = Convert.ToInt32(r["CodCategoria"]).ToString();
            if (!_categoryByCode.TryGetValue(code, out var catId)) continue;
            using var conn = Db.Open(Db.TargetCs);
            using var ins = new SqlCommand("""
                INSERT INTO SalaryScales (Period, EmployeeCategoryId, BasicAmount, AntiguedadPercentage, AgreementNR1, AgreementNR2, IsActive)
                VALUES (@period, @cat, @basic, 0, @nr1, @nr2, @active)
                """, conn);
            ins.Parameters.AddWithValue("@period", Convert.ToDateTime(r["Periodo"]));
            ins.Parameters.AddWithValue("@cat", catId);
            ins.Parameters.AddWithValue("@basic", Db.Dec(r["Importe"]));
            ins.Parameters.AddWithValue("@nr1", Db.Dec(r["AcuerdoNR1"]));
            ins.Parameters.AddWithValue("@nr2", Db.Dec(r["AcuerdoNR2"]));
            ins.Parameters.AddWithValue("@active", Convert.ToInt32(r["Estado"]) == 1);
            ins.ExecuteNonQuery();
            count++;
        }
        Log.Write($"Escalas: {count}");
    }

    // ---------- EMPRESAS ----------
    /// <summary>
    /// Carga (sin ruido) los mapas que necesitan los pasos incrementales, LEYENDO EL DESTINO:
    /// empresas por CUIT, empleados por CUIL, asignaciones por cuil|cuit y el sindicato.
    /// Con esto, correr Companies/Employees/Assignments otra vez NO duplica: cada paso saltea
    /// lo que ya existe.
    /// </summary>
    private static void CargarMapasDelDestino()
    {
        using var conn = Db.Open(Db.TargetCs);

        _companyByCuit.Clear();
        using (var cmd = new SqlCommand("SELECT Id, Cuit FROM Companies", conn) { CommandTimeout = 900 })
        using (var r = cmd.ExecuteReader())
            while (r.Read())
            {
                var cuit = Db.PadCuit(Db.Str(r["Cuit"]));
                if (cuit.Length > 0) _companyByCuit[cuit] = r.GetInt32(0);
            }

        _employeeByCuil.Clear();
        using (var cmd = new SqlCommand("SELECT Id, Dni FROM Employees", conn) { CommandTimeout = 900 })
        using (var r = cmd.ExecuteReader())
            while (r.Read())
            {
                var cuil = Db.PadCuit(Db.Str(r["Dni"]));
                if (cuil.Length > 0) _employeeByCuil[cuil] = r.GetInt32(0);
            }

        _assignmentByKey.Clear();
        using (var cmd = new SqlCommand("""
            SELECT a.Id, e.Dni, c.Cuit FROM Assignments a
            JOIN Employees e ON e.Id = a.EmployeeId
            JOIN Companies c ON c.Id = a.CompanyId
            """, conn) { CommandTimeout = 900 })
        using (var r = cmd.ExecuteReader())
            while (r.Read())
            {
                var cuil = Db.PadCuit(Db.Str(r["Dni"]));
                var cuit = Db.PadCuit(Db.Str(r["Cuit"]));
                if (cuil.Length > 0 && cuit.Length > 0) _assignmentByKey[cuil + "|" + cuit] = r.GetInt32(0);
            }

        using (var cmd = new SqlCommand("SELECT TOP 1 Id, LawContributionPercentage, UnionMemberPercentage FROM Unions ORDER BY Id", conn))
        using (var r = cmd.ExecuteReader())
            if (r.Read())
            {
                _unionId = r.GetInt32(0);
                _lawPct = Db.Dec(r["LawContributionPercentage"]);
                _memberPct = Db.Dec(r["UnionMemberPercentage"]);
            }
    }
    private static void Companies()
    {
        Log.Section("Companies: desde fotos_.maeemp");
        using var src = Db.Open(Db.LegacyCs);
        using var cmd = new SqlCommand("""
            SELECT m.MEEMP_CUIT_STR, m.MAEEMP_RAZSOC, m.MAEEMP_CALLE, m.MAEEMP_NRO, m.MAEEMP_CODPROV, m.MAEEMP_CODLOC, m.MAEEMP_TEL, m.MAEEMP_EMAIL,
                   CASE WHEN EXISTS (SELECT 1 FROM fotos_.socemp s WHERE s.SOCEMP_CUITE_STR = m.MEEMP_CUIT_STR AND s.SOCEMP_ESTADO = 1)
                          OR EXISTS (SELECT 1 FROM ddjj d WHERE d.CUIT_STR = m.MEEMP_CUIT_STR AND d.periodo >= '2026-01-01')
                        THEN 1 ELSE 0 END AS activa
            FROM fotos_.maeemp m ORDER BY m.MEEMP_CUIT_STR
            """, src);
        using var r = cmd.ExecuteReader();
        CargarMapasDelDestino();          // empresas ya existentes: no se duplican
        var count = 0;
        var yaEstaban = 0;
        while (r.Read())
        {
            var cuit = Db.PadCuit(Db.Str(r["MEEMP_CUIT_STR"]));
            if (cuit.Length == 0) continue;
            // Ya está en el destino: se saltea (idempotente).
            if (_companyByCuit.ContainsKey(cuit)) { yaEstaban++; continue; }
            if (Db.Simulacro) { count++; continue; }
            var razsoc = Db.Str(r["MAEEMP_RAZSOC"]);
            if (razsoc.Length == 0) razsoc = cuit;
            var calle = Db.Str(r["MAEEMP_CALLE"]);
            var nro = Db.Str(r["MAEEMP_NRO"]);
            var address = (calle + (nro.Length > 0 ? " " + nro : "")).Trim();
            if (address.Length == 0) address = "SIN DIRECCIÓN";
            var cityKey = Db.Str(r["MAEEMP_CODPROV"]) + "|" + Convert.ToInt64(r["MAEEMP_CODLOC"]);
            _cityByKey.TryGetValue(cityKey, out var cityId);
            var isActive = Convert.ToInt32(r["activa"]);
            using var conn = Db.Open(Db.TargetCs);
            using var ins = new SqlCommand("""
                INSERT INTO Companies (BusinessName, Cuit, Address, CityId, Phone, ContactEmail, Status, IsActive)
                OUTPUT INSERTED.Id VALUES (@name, @cuit, @address, @city, @tel, @email, 1, @active)
                """, conn);
            ins.Parameters.AddWithValue("@name", Trunc(razsoc, 200));
            ins.Parameters.AddWithValue("@cuit", cuit);
            ins.Parameters.AddWithValue("@address", Trunc(address, 200));
            ins.Parameters.AddWithValue("@city", cityId == 0 ? DBNull.Value : cityId);
            ins.Parameters.AddWithValue("@tel", NullIfEmpty(Db.Str(r["MAEEMP_TEL"]), 100));
            ins.Parameters.AddWithValue("@email", NullIfEmpty(Db.Str(r["MAEEMP_EMAIL"]), 100));
            ins.Parameters.AddWithValue("@active", isActive);
            _companyByCuit[cuit] = (int)ins.ExecuteScalar();
            count++;
        }
        Log.Write($"Empresas nuevas: {count} | ya estaban: {yaEstaban}");
    }

    // ---------- EMPLEADOS ----------
    private static void Employees()
    {
        Log.Section("Employees: desde fotos_.maesoc");
        using var src = Db.Open(Db.LegacyCs);
        using var cmd = new SqlCommand("""
            SELECT MAESOC_CUIL_STR, APENOM, MAESOC_APELLIDO, MAESOC_NOMBRE, MAESOC_FECHAALTA, MAESOC_FECHADESEMPLEO, MAESOC_ESTADO
            FROM fotos_.maesoc ORDER BY MAESOC_CUIL_STR
            """, src);
        using var r = cmd.ExecuteReader();
        CargarMapasDelDestino();          // empleados ya existentes: no se duplican
        var count = 0;
        var yaEstaban = 0;
        while (r.Read())
        {
            var cuil = Db.PadCuit(Db.Str(r["MAESOC_CUIL_STR"]));
            if (cuil.Length == 0) continue;
            if (_employeeByCuil.ContainsKey(cuil)) { yaEstaban++; continue; }
            if (Db.Simulacro) { count++; continue; }
            var apenom = Db.Str(r["APENOM"]);
            if (apenom.Length == 0)
                apenom = (Db.Str(r["MAESOC_APELLIDO"]) + " " + Db.Str(r["MAESOC_NOMBRE"])).Trim();
            if (apenom.Length == 0) apenom = cuil;
            var alta = Db.Dt(r["MAESOC_FECHAALTA"]) ?? new DateTime(1900, 1, 1);
            if (alta.Year < 1900) alta = new DateTime(1900, 1, 1);
            var baja = Db.Dt(r["MAESOC_FECHADESEMPLEO"]);
            if (baja.HasValue && (baja.Value.Year < 1900 || baja.Value > DateTime.Today)) baja = null;
            var estado = Convert.ToInt32(r["MAESOC_ESTADO"]);
            using var conn = Db.Open(Db.TargetCs);
            using var ins = new SqlCommand("""
                INSERT INTO Employees (Dni, FullName, HireDate, TerminationDate, Status)
                OUTPUT INSERTED.Id VALUES (@dni, @name, @alta, @baja, @status)
                """, conn);
            ins.Parameters.AddWithValue("@dni", cuil);
            ins.Parameters.AddWithValue("@name", Trunc(apenom, 200));
            ins.Parameters.AddWithValue("@alta", alta);
            ins.Parameters.AddWithValue("@baja", baja.HasValue ? baja.Value : DBNull.Value);
            ins.Parameters.AddWithValue("@status", estado == 1 ? 1 : 5);
            _employeeByCuil[cuil] = (int)ins.ExecuteScalar();
            count++;
        }
        Log.Write($"Empleados nuevos: {count} | ya estaban: {yaEstaban}");
    }

    // ---------- ASIGNACIONES ----------
    private static void Assignments()
    {
        Log.Section("Assignments: desde fotos_.socemp");
        using var src = Db.Open(Db.LegacyCs);
        using var cmd = new SqlCommand("""
            SELECT s.SOCEMP_CUIL_STR, s.SOCEMP_CUITE_STR, s.SOCEMP_FECHAING, s.SOCEMP_ESTADO, s.SOCEMP_FECHABAJA,
                   CASE WHEN EXISTS (SELECT 1 FROM ddjj d
                                     WHERE d.CUIL_STR = s.SOCEMP_CUIL_STR
                                       AND d.CUIT_STR = s.SOCEMP_CUITE_STR
                                       AND d.item2 = 1) THEN 1 ELSE 0 END AS vol
            FROM fotos_.socemp s
            """, src) { CommandTimeout = 900 };
        using var r = cmd.ExecuteReader();
        CargarMapasDelDestino();          // asignaciones ya existentes: no se duplican
        var count = 0;
        var yaEstaban = 0;
        while (r.Read())
        {
            var cuil = Db.PadCuit(Db.Str(r["SOCEMP_CUIL_STR"]));
            var cuit = Db.PadCuit(Db.Str(r["SOCEMP_CUITE_STR"]));
            if (cuil.Length == 0 || cuit.Length == 0) continue;
            if (!_employeeByCuil.TryGetValue(cuil, out var empId)) continue;
            if (!_companyByCuit.TryGetValue(cuit, out var compId)) continue;
            var key = cuil + "|" + cuit;
            if (_assignmentByKey.ContainsKey(key)) { yaEstaban++; continue; }
            if (Db.Simulacro) { count++; continue; }
            var fecha = Db.Dt(r["SOCEMP_FECHAING"]) ?? DateTime.Now;
            var isVoluntary = Convert.ToBoolean(r["vol"]) ? 1 : 0;
            var isActive = Convert.ToInt32(r["SOCEMP_ESTADO"]) == 1 ? 1 : 0;
            var baja = Db.Dt(r["SOCEMP_FECHABAJA"]);
            if (baja.HasValue && (baja.Value.Year < 1900 || baja.Value > DateTime.Today)) baja = null;
            using var conn = Db.Open(Db.TargetCs);
            using var ins = new SqlCommand("""
                INSERT INTO Assignments (EmployeeId, CompanyId, UnionId, IsVoluntaryMember, CategoryCode, CreatedAt, IsActive, TerminationDate)
                OUTPUT INSERTED.Id VALUES (@emp, @comp, @union, @vol, NULL, @fecha, @active, @baja)
                """, conn);
            ins.Parameters.AddWithValue("@emp", empId);
            ins.Parameters.AddWithValue("@comp", compId);
            ins.Parameters.AddWithValue("@union", _unionId);
            ins.Parameters.AddWithValue("@vol", isVoluntary);
            ins.Parameters.AddWithValue("@fecha", fecha);
            ins.Parameters.AddWithValue("@active", isActive);
            ins.Parameters.AddWithValue("@baja", baja.HasValue ? baja.Value : DBNull.Value);
            _assignmentByKey[key] = (int)ins.ExecuteScalar();
            count++;
        }
        Log.Write($"Asignaciones nuevas: {count} | ya estaban: {yaEstaban}");
    }

    // ---------- ESTUDIOS ----------
    private static void Studios()
    {
        Log.Section("AccountingStudios: desde EstudiosContables + EstudioContableEmpresa");
        using var src = Db.Open(Db.LegacyCs);
        using (var cmd = new SqlCommand(
            "SELECT Id, RazonSocial, Nombre, Email, Estado, Domicilio, Telefono FROM EstudiosContables ORDER BY Id", src))
        using (var r = cmd.ExecuteReader())
        {
            var count = 0;
            while (r.Read())
            {
                var legacyId = Convert.ToInt32(r["Id"]);
                var name = Db.Str(r["RazonSocial"]);
                if (name.Length <= 1 || name == ".") name = Db.Str(r["Nombre"]);
                if (name.Length == 0) name = "ESTUDIO " + legacyId;
                var email = Db.Str(r["Email"]);
                var estado = Convert.ToInt32(r["Estado"]);
                var username = Trunc(email.Length > 0 ? email : "estudio" + legacyId, 100);
                using var conn = Db.Open(Db.TargetCs);
                using var ins = new SqlCommand("""
                    IF EXISTS (SELECT 1 FROM AccountingStudios WHERE ResponsibleUsername = @user)
                        SELECT Id FROM AccountingStudios WHERE ResponsibleUsername = @user;
                    ELSE
                        INSERT INTO AccountingStudios (Name, ResponsibleUsername, Email, ContactPhone, Address, IsApproved, IsActive, CreatedAt)
                        OUTPUT INSERTED.Id VALUES (@name, @user, @email, @tel, @address, @approved, 1, @now);
                    """, conn);
                ins.Parameters.AddWithValue("@name", Trunc(name, 200));
                ins.Parameters.AddWithValue("@user", username);
                ins.Parameters.AddWithValue("@email", NullIfEmpty(email, 200));
                ins.Parameters.AddWithValue("@tel", NullIfEmpty(Db.Str(r["Telefono"]), 50));
                ins.Parameters.AddWithValue("@address", NullIfEmpty(Db.Str(r["Domicilio"]), 250));
                ins.Parameters.AddWithValue("@approved", estado == 1);
                ins.Parameters.AddWithValue("@now", DateTime.UtcNow);
                _studioById[legacyId] = (int)ins.ExecuteScalar();
                count++;
            }
            Log.Write($"Estudios: {count}");
        }

        // Vincular empresas al estudio
        using (var cmd = new SqlCommand(
            "SELECT EstudioContableId, CUIT FROM EstudioContableEmpresa", src))
        using (var r = cmd.ExecuteReader())
        {
            var linked = 0;
            while (r.Read())
            {
                var studioLegacyId = Convert.ToInt32(r["EstudioContableId"]);
                var cuit = Db.PadCuit(Db.Str(r["CUIT"]));
                if (!_studioById.TryGetValue(studioLegacyId, out var studioId)) continue;
                if (!_companyByCuit.TryGetValue(cuit, out var compId)) continue;
                Db.Run(Db.TargetCs, $"UPDATE Companies SET AccountingStudioId = {studioId} WHERE Id = {compId}");
                linked++;
            }
            Log.Write($"Empresas vinculadas a estudios: {linked}");
        }
    }

    // ---------- DECLARACIONES ----------
    /// <summary>
    /// Carga en memoria los mapas que necesitan las declaraciones, LEYENDO EL DESTINO (no el legado).
    /// Se usa en el paso `sync`: así se pueden traer sólo las DDJJ que faltan sin volver a correr los
    /// pasos que insertan (empresas, empleados, asignaciones), que no son idempotentes.
    /// </summary>
    private static void CargarMapasDesdeDestino()
    {
        Log.Section("Sync: leyendo empresas, asignaciones y sindicato del destino");

        using var conn = Db.Open(Db.TargetCs);

        using (var cmd = new SqlCommand("SELECT Id, Cuit FROM Companies", conn) { CommandTimeout = 900 })
        using (var r = cmd.ExecuteReader())
            while (r.Read())
            {
                var cuit = Db.PadCuit(Db.Str(r["Cuit"]));
                if (cuit.Length > 0) _companyByCuit[cuit] = r.GetInt32(0);
            }
        Log.Write($"  empresas: {_companyByCuit.Count}");

        using (var cmd = new SqlCommand("""
            SELECT a.Id, e.Dni, c.Cuit
            FROM Assignments a
            JOIN Employees e ON e.Id = a.EmployeeId
            JOIN Companies c ON c.Id = a.CompanyId
            """, conn) { CommandTimeout = 900 })
        using (var r = cmd.ExecuteReader())
            while (r.Read())
            {
                var cuil = Db.PadCuit(Db.Str(r["Dni"]));
                var cuit = Db.PadCuit(Db.Str(r["Cuit"]));
                if (cuil.Length > 0 && cuit.Length > 0) _assignmentByKey[cuil + "|" + cuit] = r.GetInt32(0);
            }
        Log.Write($"  asignaciones: {_assignmentByKey.Count}");
        using (var cmd = new SqlCommand("SELECT Id, Dni FROM Employees", conn) { CommandTimeout = 900 })
        using (var r = cmd.ExecuteReader())
            while (r.Read())
            {
                var cuil = Db.PadCuit(Db.Str(r["Dni"]));
                if (cuil.Length > 0) _employeeByCuil[cuil] = r.GetInt32(0);
            }
        Log.Write($"  empleados: {_employeeByCuil.Count}");

        using (var cmd = new SqlCommand("SELECT TOP 1 Id, LawContributionPercentage, UnionMemberPercentage FROM Unions ORDER BY Id", conn))
        using (var r = cmd.ExecuteReader())
            if (r.Read())
            {
                _unionId = r.GetInt32(0);
                _lawPct = Db.Dec(r["LawContributionPercentage"]);
                _memberPct = Db.Dec(r["UnionMemberPercentage"]);
                Log.Write($"  sindicato: {_unionId} (ley {_lawPct}% / socio {_memberPct}%)");
            }
    }
    /// <summary>
    /// Refresca el ESTADO de las declaraciones que ya estaban en el destino, tomándolo del legado:
    ///   - pagos: si el legado tiene fecha de pago y SIRGA no, se marca Pagada con esa fecha.
    ///   - rectificaciones: si el legado tiene una rectificación y SIRGA no la conocía, se marca.
    /// Sin esto, las DDJJ copiadas se quedan con el estado del día que se migraron (una DDJJ pagada
    /// en el sistema viejo seguiría figurando impaga en SIRGA).
    /// </summary>
    private static void RefreshDesdeLegado()
    {
        Log.Section("Refresh: pagos y rectificaciones del legado sobre lo que ya estaba" +
                    (Db.Simulacro ? "  [SIMULACRO: no escribe]" : ""));

        using var target = Db.Open(Db.TargetCs);

        // Copia del legado a una tabla temporal del destino (sirve aunque estén en servidores distintos)
        using (var crear = new SqlCommand("CREATE TABLE #Legado (Cuit varchar(20), Periodo varchar(7), Fpago datetime2, Rect int, Total decimal(18,2))", target))
            crear.ExecuteNonQuery();

        // El rect de los RENGLONES puede no coincidir con el de la cabecera "buena". Se toma el maximo
        // rect que tenga renglones: si la rectificativa no cargó renglones, se usan los de la original.
        var rectRenglones = new Dictionary<string, int>(StringComparer.Ordinal);
        using (var srcR = Db.Open(Db.LegacyCs))
        using (var cmdR = new SqlCommand("""
            SELECT CUIT_STR, CONVERT(varchar(7), periodo, 126) p, MAX(rect) r
            FROM ddjj WHERE periodo >= '1990-01-01' AND periodo <= GETDATE()
            GROUP BY CUIT_STR, CONVERT(varchar(7), periodo, 126)
            """, srcR) { CommandTimeout = 900 })
        using (var rr = cmdR.ExecuteReader())
            while (rr.Read())
            {
                var cuitR = Db.PadCuit(Db.Str(rr["CUIT_STR"]));
                if (cuitR.Length == 0) continue;
                rectRenglones[cuitR + "|" + Db.Str(rr["p"])] = Db.Int(rr["r"]);
            }

        var dt = new DataTable();
        dt.Columns.Add("Cuit", typeof(string));
        dt.Columns.Add("Periodo", typeof(string));
        dt.Columns.Add("Fpago", typeof(DateTime));
        dt.Columns.Add("Rect", typeof(int));
        dt.Columns.Add("Total", typeof(decimal));

        using (var src = Db.Open(Db.LegacyCs))
        using (var cmd = new SqlCommand("""
            SELECT CUIT_STR, CONVERT(varchar(7), periodo, 126) AS p, fpago, rect,
                   ISNULL(titem1, 0) + ISNULL(titem2, 0) + ISNULL(titem3, 0) AS total
            FROM ddjjt WHERE rect < 100
            """, src) { CommandTimeout = 900 })
        using (var r = cmd.ExecuteReader())
            while (r.Read())
            {
                var cuit = Db.PadCuit(Db.Str(r["CUIT_STR"]));
                if (cuit.Length == 0) continue;
                dt.Rows.Add(cuit, Db.Str(r["p"]),
                            r["fpago"] == DBNull.Value ? (object)DBNull.Value : Convert.ToDateTime(r["fpago"]),
                            Db.Int(r["rect"]), Db.Dec(r["total"]));
            }

        Db.BulkInsert(target, "#Legado", dt);
        Log.Write($"  filas del legado para comparar: {dt.Rows.Count}");

        const string join = """
            FROM Declarations d
            JOIN Companies c ON c.Id = d.CompanyId
            JOIN #Legado l ON l.Periodo = d.Period AND l.Cuit = REPLACE(REPLACE(c.Cuit, '-', ''), ' ', '')
            """;

        // 1) Pagos
        var sqlPagos = "UPDATE d SET Status = 5, PaymentDate = l.Fpago, BalanceAmount = 0 " + join +
                       " WHERE d.PaymentDate IS NULL AND l.Fpago IS NOT NULL";
        // 2) Rectificaciones
        var sqlRect = "UPDATE d SET IsRectification = 1, " +
                      "VersionNumber = CASE WHEN l.Rect + 1 > d.VersionNumber THEN l.Rect + 1 ELSE d.VersionNumber END " + join +
                      " WHERE d.IsRectification = 0 AND l.Rect > 0";

        if (Db.Simulacro)
        {
            using (var q1 = new SqlCommand("SELECT COUNT(*) " + join + " WHERE d.PaymentDate IS NULL AND l.Fpago IS NOT NULL", target) { CommandTimeout = 900 })
                Log.Write($"  (simulacro) se marcarían PAGADAS: {q1.ExecuteScalar()}");
            using (var q2 = new SqlCommand("SELECT COUNT(*) " + join + " WHERE d.IsRectification = 0 AND l.Rect > 0", target) { CommandTimeout = 900 })
                Log.Write($"  (simulacro) se marcarían RECTIFICADAS: {q2.ExecuteScalar()}");
        }
        else
        {
            using var p = new SqlCommand(sqlPagos, target) { CommandTimeout = 900 };
            var pagos = p.ExecuteNonQuery();
            using var rc = new SqlCommand(sqlRect, target) { CommandTimeout = 900 };
            var rects = rc.ExecuteNonQuery();
            Log.Write($"  marcadas PAGADAS: {pagos} | marcadas RECTIFICADAS: {rects}");
        }
    }
    private static void Declarations()
    {
        Log.Section("Declarations: desde ddjjt (dedupe por periodo+cuit, futuras excluidas)" +
                    (Db.Simulacro ? "  [SIMULACRO: no escribe]" : ""));

        // Lo que YA existe en el destino. El ETL original insertaba a ciegas: si se volvia a correr,
        // duplicaba todo. Ahora se saltea lo que ya esta y se registra su Id para el paso de detalles.
        var existentes = new Dictionary<string, int>(StringComparer.Ordinal);
        using (var t = Db.Open(Db.TargetCs))
        using (var tc = new SqlCommand("SELECT Id, CompanyId, Period FROM Declarations", t) { CommandTimeout = 900 })
        using (var tr = tc.ExecuteReader())
            while (tr.Read())
                existentes[$"{tr.GetInt32(1)}|{tr.GetString(2)}"] = tr.GetInt32(0);
        Log.Write($"  ya en el destino: {existentes.Count} declaraciones");
        using var src = Db.Open(Db.LegacyCs);
        using var cmd = new SqlCommand("""
            SELECT CUIT_STR, periodo, estado, rect, fpres, fpago, titem1, titem2, titem3
            FROM (
                SELECT CUIT_STR, periodo, estado, rect, fpres, fpago, titem1, titem2, titem3,
                       ROW_NUMBER() OVER (PARTITION BY CONVERT(varchar(7),periodo,120), CUIT_STR
                                          ORDER BY rect DESC, fmod DESC, fpres DESC, Id DESC) rn
                FROM ddjjt WHERE periodo <= GETDATE() AND periodo >= '1990-01-01' AND rect < 100
            ) x WHERE rn = 1
            """, src) { CommandTimeout = 900 };
        using var r = cmd.ExecuteReader();
        var count = 0;
        var yaEstaban = 0;
        while (r.Read())
        {
            var cuit = Db.PadCuit(Db.Str(r["CUIT_STR"]));
            var periodo = Convert.ToDateTime(r["periodo"]);
            if (cuit.Length == 0) continue;
            if (!_companyByCuit.TryGetValue(cuit, out var compId)) continue;
            var period = periodo.ToString("yyyy-MM");
            var key = cuit + "|" + period;
            var estado = Convert.ToInt32(r["estado"]);
            var rect = Convert.ToInt32(r["rect"]);

            // Ya existe en el destino (Migrate() corrido antes): se saltea, pero se registra su Id
            // para que el paso de detalles no la vuelva a procesar.
            if (existentes.TryGetValue(compId + "|" + period, out var existenteId))
            {
                _registryByKey[key] = existenteId;
                _rectByKey[key] = rect;
                yaEstaban++;
                continue;
            }
            var fpres = Db.Dt(r["fpres"]) ?? periodo;
            var fpago = Db.Dt(r["fpago"]);
            var titem1 = Db.Dec(r["titem1"]);
            var titem2 = Db.Dec(r["titem2"]);
            var titem3 = Db.Dec(r["titem3"]);
            var total = Math.Round(titem1 + titem2 + titem3, 2);
            int status = MapStatus(estado, fpago);
            var balance = fpago.HasValue ? 0m : total;

            if (Db.Simulacro)
            {
                _registryByKey[key] = -1;   // -1 = se insertaria: permite contar los detalles en simulacro
                _rectByKey[key] = rect;
                count++;
                continue;
            }

            using var conn = Db.Open(Db.TargetCs);
            using var ins = new SqlCommand("""
                INSERT INTO Declarations (Period, CompanyId, UnionId, Status, TotalAmount, IsRectification,
                                          SubmissionDate, ExternalReference, MercadoPagoPaymentId, PaymentDate,
                                          PresencialBarCode, PresencialReference, BalanceAmount, PreviousAmount,
                                          VersionNumber, IsHistorical)
                OUTPUT INSERTED.Id
                VALUES (@period, @comp, @union, @status, @total, @rect, @fpres, NULL, NULL, @fpago, NULL, NULL, @balance, 0,
                        1, 1)
                """, conn);
            ins.Parameters.AddWithValue("@period", period);
            ins.Parameters.AddWithValue("@comp", compId);
            ins.Parameters.AddWithValue("@union", _unionId);
            ins.Parameters.AddWithValue("@status", status);
            ins.Parameters.AddWithValue("@total", total);
            ins.Parameters.AddWithValue("@rect", rect > 0);
            ins.Parameters.AddWithValue("@fpres", fpres);
            ins.Parameters.AddWithValue("@fpago", fpago.HasValue ? fpago.Value : DBNull.Value);
            ins.Parameters.AddWithValue("@balance", balance);
            _registryByKey[key] = (int)ins.ExecuteScalar();
            _rectByKey[key] = rect;
            count++;
        }
        Log.Write($"Declaraciones nuevas: {count} | ya estaban (no se tocan): {yaEstaban}");
    }

    private static int MapStatus(int estado, DateTime? fpago)
    {
        if (fpago.HasValue) return 5;                       // Paid
        if (estado is 10 or 11 or 20 or 21) return 4;       // Rejected
        return 2;                                            // Submitted
    }

    // ---------- DETALLES ----------
    /// <summary>
    /// Alinea el ESTADO de empleados y asignaciones con el legado: es lo que se usa en la carga inicial,
    /// donde el sistema viejo es la fuente. SIRGA es el dueño del ciclo de vida (altas y bajas) de ahí en
    /// adelante, así que este paso se corre una sola vez, no en cada sincronización.
    ///   - Empleados: MAESOC_ESTADO y MAESOC_FECHADESEMPLEO (la fecha centinela se toma como "sin baja").
    ///   - Asignaciones: SOCEMP_ESTADO y SOCEMP_FECHABAJA.
    /// </summary>
    private static void AlinearEstados()
    {
        Log.Section("Alinear estados de empleados y asignaciones con el legado" +
                    (Db.Simulacro ? "  [SIMULACRO: no escribe]" : ""));

        using var target = Db.Open(Db.TargetCs);

        // ---- empleados ----
        using (var crear = new SqlCommand("CREATE TABLE #Emp (Cuil varchar(20) PRIMARY KEY, Estado int, Baja datetime2)", target))
            crear.ExecuteNonQuery();
        var emp = new DataTable();
        emp.Columns.Add("Cuil", typeof(string));
        emp.Columns.Add("Estado", typeof(int));
        emp.Columns.Add("Baja", typeof(DateTime));
        using (var src = Db.Open(Db.LegacyCs))
        using (var cmd = new SqlCommand("SELECT MAESOC_CUIL_STR, MAESOC_ESTADO, MAESOC_FECHADESEMPLEO FROM fotos_.maesoc", src) { CommandTimeout = 900 })
        using (var r = cmd.ExecuteReader())
            while (r.Read())
            {
                var cuil = Db.PadCuit(Db.Str(r["MAESOC_CUIL_STR"]));
                if (cuil.Length == 0) continue;
                var baja = Db.Dt(r["MAESOC_FECHADESEMPLEO"]);
                if (baja.HasValue && (baja.Value.Year < 1900 || baja.Value > DateTime.Today)) baja = null;
                emp.Rows.Add(cuil, Db.Int(r["MAESOC_ESTADO"]), baja.HasValue ? baja.Value : (object)DBNull.Value);
            }
        Db.BulkInsert(target, "#Emp", emp);

        const string sqlEmp = """
            UPDATE e SET Status = CASE WHEN l.Estado = 1 THEN 1 ELSE 5 END, TerminationDate = l.Baja
            FROM Employees e JOIN #Emp l ON l.Cuil = e.Dni
            WHERE e.Status <> CASE WHEN l.Estado = 1 THEN 1 ELSE 5 END
               OR ISNULL(CONVERT(varchar(19), e.TerminationDate, 120), '') <> ISNULL(CONVERT(varchar(19), l.Baja, 120), '')
            """;

        // ---- asignaciones ----
        using (var crear = new SqlCommand("CREATE TABLE #Asig (Cuil varchar(20), Cuit varchar(20), Estado int, Baja datetime2, PRIMARY KEY (Cuil, Cuit))", target))
            crear.ExecuteNonQuery();
        var asi = new DataTable();
        asi.Columns.Add("Cuil", typeof(string));
        asi.Columns.Add("Cuit", typeof(string));
        asi.Columns.Add("Estado", typeof(int));
        asi.Columns.Add("Baja", typeof(DateTime));
        var vistos = new HashSet<string>(StringComparer.Ordinal);
        using (var src = Db.Open(Db.LegacyCs))
        using (var cmd = new SqlCommand("SELECT SOCEMP_CUIL_STR, SOCEMP_CUITE_STR, SOCEMP_ESTADO, SOCEMP_FECHABAJA FROM fotos_.socemp", src) { CommandTimeout = 900 })
        using (var r = cmd.ExecuteReader())
            while (r.Read())
            {
                var cuil = Db.PadCuit(Db.Str(r["SOCEMP_CUIL_STR"]));
                var cuit = Db.PadCuit(Db.Str(r["SOCEMP_CUITE_STR"]));
                if (cuil.Length == 0 || cuit.Length == 0 || cuit == "99999999999") continue;
                var baja = Db.Dt(r["SOCEMP_FECHABAJA"]);
                if (baja.HasValue && (baja.Value.Year < 1900 || baja.Value > DateTime.Today)) baja = null;
                if (vistos.Add(cuil + "|" + cuit))
                    asi.Rows.Add(cuil, cuit, Db.Int(r["SOCEMP_ESTADO"]), baja.HasValue ? baja.Value : (object)DBNull.Value);
            }
        Db.BulkInsert(target, "#Asig", asi);

        const string sqlAsig = """
            UPDATE a SET IsActive = CASE WHEN l.Estado = 1 THEN 1 ELSE 0 END, TerminationDate = l.Baja
            FROM Assignments a
            JOIN Employees e ON e.Id = a.EmployeeId
            JOIN Companies c ON c.Id = a.CompanyId
            JOIN #Asig l ON l.Cuil = e.Dni AND l.Cuit = REPLACE(REPLACE(c.Cuit,'-',''),' ','')
            WHERE a.IsActive <> CASE WHEN l.Estado = 1 THEN 1 ELSE 0 END
               OR ISNULL(CONVERT(varchar(19), a.TerminationDate, 120), '') <> ISNULL(CONVERT(varchar(19), l.Baja, 120), '')
            """;

        if (Db.Simulacro)
        {
            using (var q1 = new SqlCommand("SELECT COUNT(*) FROM Employees e JOIN #Emp l ON l.Cuil = e.Dni WHERE e.Status <> CASE WHEN l.Estado = 1 THEN 1 ELSE 5 END OR ISNULL(CONVERT(varchar(19), e.TerminationDate, 120), '') <> ISNULL(CONVERT(varchar(19), l.Baja, 120), '')", target))
                Log.Write($"  (simulacro) empleados a alinear: {q1.ExecuteScalar()}");
            using (var q2 = new SqlCommand("SELECT COUNT(*) FROM Assignments a JOIN Employees e ON e.Id = a.EmployeeId JOIN Companies c ON c.Id = a.CompanyId JOIN #Asig l ON l.Cuil = e.Dni AND l.Cuit = REPLACE(REPLACE(c.Cuit,'-',''),' ','') WHERE a.IsActive <> CASE WHEN l.Estado = 1 THEN 1 ELSE 0 END OR ISNULL(CONVERT(varchar(19), a.TerminationDate, 120), '') <> ISNULL(CONVERT(varchar(19), l.Baja, 120), '')", target))
                Log.Write($"  (simulacro) asignaciones a alinear: {q2.ExecuteScalar()}");
        }
        else
        {
            using (var u1 = new SqlCommand(sqlEmp, target) { CommandTimeout = 900 }) Log.Write($"  empleados alineados: {u1.ExecuteNonQuery()}");
            using (var u2 = new SqlCommand(sqlAsig, target) { CommandTimeout = 900 }) Log.Write($"  asignaciones alineadas: {u2.ExecuteNonQuery()}");
        }
    }
    private static void Details()
    {
        Log.Section("DeclarationDetails: desde ddjj (bulk)" + (Db.Simulacro ? "  [SIMULACRO: no escribe]" : ""));

        // 1) Cuántos renglones tiene hoy cada declaración en el destino.
        var conDetalle = new Dictionary<int, int>();
        using (var t = Db.Open(Db.TargetCs))
        using (var tc = new SqlCommand("SELECT RegistryId, COUNT(*) FROM DeclarationDetails GROUP BY RegistryId", t) { CommandTimeout = 900 })
        using (var tr = tc.ExecuteReader())
            while (tr.Read()) conDetalle[tr.GetInt32(0)] = tr.GetInt32(1);
        Log.Write($"  declaraciones con renglones: {conDetalle.Count}");

        // Empleados/empresas faltantes en ddjj pero no en los maestros (en simulacro NO se crea nada).
        if (Db.Simulacro) Log.Write("  (simulacro) no se crean maestros faltantes");
        else EnsureMissingEntities();

        // 2) Del legado: para cada (CUIT, período), el rect con renglones y cuántos son.
        //    El rect de los RENGLONES puede no coincidir con el de la cabecera "buena": si la
        //    rectificativa no cargó renglones, se usan los de la original.
        var rectRenglones = new Dictionary<string, int>(StringComparer.Ordinal);
        var cuentaRenglones = new Dictionary<string, int>(StringComparer.Ordinal);
        using (var srcR = Db.Open(Db.LegacyCs))
        using (var cmdR = new SqlCommand("""
            SELECT CUIT_STR, CONVERT(varchar(7), periodo, 126) p, rect r, COUNT(*) n
            FROM ddjj WHERE periodo >= '1990-01-01' AND periodo <= GETDATE()
            GROUP BY CUIT_STR, CONVERT(varchar(7), periodo, 126), rect
            """, srcR) { CommandTimeout = 900 })
        using (var rr = cmdR.ExecuteReader())
            while (rr.Read())
            {
                var cuitR = Db.PadCuit(Db.Str(rr["CUIT_STR"]));
                if (cuitR.Length == 0) continue;
                var clave = cuitR + "|" + Db.Str(rr["p"]);
                var rectFila = Db.Int(rr["r"]);
                var cuenta = Db.Int(rr["n"]);
                // Se queda con la rectificativa MÁS ALTA que tenga renglones, y SU cantidad.
                if (!rectRenglones.TryGetValue(clave, out var rectActual) || rectFila >= rectActual)
                {
                    rectRenglones[clave] = rectFila;
                    cuentaRenglones[clave] = cuenta;
                }
            }
        Log.Write($"  combinaciones CUIT/período con renglones en el legado: {rectRenglones.Count}");

        // 3) Declaraciones con renglones INCOMPLETOS: las que ya tenían algunos se salteaban enteras,
        //    así que quedaban a medias para siempre. Se reconstruyen (se borran y se vuelven a cargar).
        var porReparar = new HashSet<int>();
        using (var t = Db.Open(Db.TargetCs))
        using (var cmd = new SqlCommand("""
            SELECT d.Id, d.Period, REPLACE(REPLACE(c.Cuit,'-',''),' ','') cuit
            FROM Declarations d
            JOIN Companies c ON c.Id = d.CompanyId
            WHERE EXISTS (SELECT 1 FROM DeclarationDetails dd WHERE dd.RegistryId = d.Id)
            """, t) { CommandTimeout = 900 })
        using (var r = cmd.ExecuteReader())
            while (r.Read())
            {
                var clave = Db.PadCuit(Db.Str(r["cuit"])) + "|" + Db.Str(r["Period"]);
                if (!cuentaRenglones.TryGetValue(clave, out var esperados)) continue;
                var actual = conDetalle.TryGetValue(r.GetInt32(0), out var n) ? n : 0;
                if (actual != esperados) porReparar.Add(r.GetInt32(0));
            }
        Log.Write($"  declaraciones incompletas a reconstruir: {porReparar.Count}");

        if (porReparar.Count > 0 && !Db.Simulacro)
        {
            using var target0 = Db.Open(Db.TargetCs);
            using (var crear = new SqlCommand("CREATE TABLE #reparar (Id int PRIMARY KEY)", target0)) crear.ExecuteNonQuery();
            var tabla = new DataTable();
            tabla.Columns.Add("Id", typeof(int));
            foreach (var id in porReparar) tabla.Rows.Add(id);
            Db.BulkInsert(target0, "#reparar", tabla);
            using (var borrar = new SqlCommand("DELETE dd FROM DeclarationDetails dd JOIN #reparar r ON r.Id = dd.RegistryId", target0) { CommandTimeout = 900 })
            {
                var borrados = borrar.ExecuteNonQuery();
                Log.Write($"  renglones borrados para reconstruir: {borrados}");
            }
            foreach (var id in porReparar) conDetalle.Remove(id);
        }

        // 4) Carga de renglones.
        var dt = new DataTable();
        dt.Columns.Add("RegistryId", typeof(int));
        dt.Columns.Add("AssignmentId", typeof(int));
        dt.Columns.Add("BaseSalary", typeof(decimal));
        dt.Columns.Add("LawContribution", typeof(decimal));
        dt.Columns.Add("MemberContribution", typeof(decimal));
        dt.Columns.Add("TotalWithheld", typeof(decimal));

        long total = 0;
        using var src = Db.Open(Db.LegacyCs);
        using var cmd2 = new SqlCommand("""
            SELECT CUIL_STR, CUIT_STR, periodo, impo, impoaux, item2, rect FROM ddjj WHERE periodo <= GETDATE()
            """, src) { CommandTimeout = 900 };
        using var reader = cmd2.ExecuteReader();
        using var target = Db.Open(Db.TargetCs);
        while (reader.Read())
        {
            var cuil = Db.PadCuit(Db.Str(reader["CUIL_STR"]));
            var cuit = Db.PadCuit(Db.Str(reader["CUIT_STR"]));
            var periodo = Convert.ToDateTime(reader["periodo"]);
            var rect = Convert.ToInt32(reader["rect"]);
            if (cuil.Length == 0 || cuit.Length == 0) continue;
            var regKey = cuit + "|" + periodo.ToString("yyyy-MM");
            if (!_registryByKey.TryGetValue(regKey, out var regId) || regId == 0) continue;
            if (conDetalle.ContainsKey(regId)) continue;
            if (rectRenglones.TryGetValue(regKey, out var rectBueno) && rect != rectBueno) continue;
            var isVoluntary = Convert.ToBoolean(reader["item2"]);
            var assKey = cuil + "|" + cuit;
            if (!_assignmentByKey.TryGetValue(assKey, out var assId))
            {
                if (Db.Simulacro) { total++; continue; }
                if (!TryCreateMissingAssignment(cuil, cuit, periodo, isVoluntary, out assId)) continue;
            }
            var baseSalary = Math.Round(Db.Dec(reader["impo"]), 2);
            var aux = Db.Dec(reader["impoaux"]);
            var baseTotal = Math.Round(baseSalary + Math.Round(aux, 2), 2);
            var law = Math.Round(baseTotal * _lawPct / 100m, 2);
            var member = isVoluntary ? Math.Round(baseTotal * _memberPct / 100m, 2) : 0m;
            dt.Rows.Add(regId, assId, baseSalary, law, member, Math.Round(law + member, 2));
            total++;
            if (total % 50000 == 0)
            {
                if (!Db.Simulacro) Db.BulkInsert(target, "DeclarationDetails", dt);
                Log.Write($"  detalles acumulados: {total}");
                dt.Clear();
            }
        }
        if (dt.Rows.Count > 0 && !Db.Simulacro) Db.BulkInsert(target, "DeclarationDetails", dt);
        Log.Write($"Detalles totales: {total}");
    }

    // Alinea TotalAmount/BalanceAmount de cabeceras con la suma real de sus detalles
    private static void FinalizeTotals()
    {
        Log.Section("Finalize: TotalAmount y BalanceAmount = suma de detalles");
        Db.Run(Db.TargetCs, """
            UPDATE d
            SET d.TotalAmount = ISNULL(x.s, 0),
                d.BalanceAmount = CASE WHEN d.PaymentDate IS NOT NULL THEN 0 ELSE ISNULL(x.s, 0) END
            FROM Declarations d
            CROSS APPLY (SELECT ROUND(SUM(dt.TotalWithheld), 2) s FROM DeclarationDetails dt WHERE dt.RegistryId = d.Id) x
            """, 900);
        Log.Write("Finalize OK");
    }

    private static void EnsureMissingEntities()
    {
        Log.Write("Verificando maestros faltantes para detalles...");
        // Empresas de ddjj no presentes
        using var src = Db.Open(Db.LegacyCs);
        using (var cmd = new SqlCommand(
            "SELECT DISTINCT CUIT_STR FROM ddjj WHERE CUIT_STR NOT IN (SELECT MEEMP_CUIT_STR FROM fotos_.maeemp) UNION SELECT DISTINCT CUIT_STR FROM ddjjt WHERE CUIT_STR NOT IN (SELECT MEEMP_CUIT_STR FROM fotos_.maeemp)", src) { CommandTimeout = 600 })
        using (var r = cmd.ExecuteReader())
        {
            while (r.Read())
            {
                var cuit = Db.PadCuit(Db.Str(r["CUIT_STR"]));
                if (cuit.Length == 0 || _companyByCuit.ContainsKey(cuit)) continue;
                using var conn = Db.Open(Db.TargetCs);
                using var ins = new SqlCommand(
                    "INSERT INTO Companies (BusinessName, Cuit, Address, Status, IsActive) OUTPUT INSERTED.Id VALUES (@name, @cuit, 'SIN DIRECCIÓN', 1, 1)", conn);
                ins.Parameters.AddWithValue("@name", cuit);
                ins.Parameters.AddWithValue("@cuit", cuit);
                _companyByCuit[cuit] = (int)ins.ExecuteScalar();
                Log.Write($"  Empresa faltante creada: {cuit}");
            }
        }
        // Empleados de ddjj no presentes
        using (var cmd = new SqlCommand(
            "SELECT DISTINCT CUIL_STR FROM ddjj WHERE CUIL_STR NOT IN (SELECT MAESOC_CUIL_STR FROM fotos_.maesoc)", src) { CommandTimeout = 600 })
        using (var r = cmd.ExecuteReader())
        {
            while (r.Read())
            {
                var cuil = Db.PadCuit(Db.Str(r["CUIL_STR"]));
                if (cuil.Length == 0 || _employeeByCuil.ContainsKey(cuil)) continue;
                using var conn = Db.Open(Db.TargetCs);
                using var ins = new SqlCommand(
                    "INSERT INTO Employees (Dni, FullName, HireDate, Status) OUTPUT INSERTED.Id VALUES (@dni, @name, '1900-01-01', 5)", conn);
                ins.Parameters.AddWithValue("@dni", cuil);
                ins.Parameters.AddWithValue("@name", "SIN NOMBRE " + cuil);
                _employeeByCuil[cuil] = (int)ins.ExecuteScalar();
                Log.Write($"  Empleado faltante creado: {cuil}");
            }
        }
    }

    private static bool TryCreateMissingAssignment(string cuil, string cuit, DateTime fecha, bool isVoluntary, out int assId)
    {
        assId = 0;
        if (!_employeeByCuil.TryGetValue(cuil, out var empId)) return false;
        if (!_companyByCuit.TryGetValue(cuit, out var compId)) return false;
        var key = cuil + "|" + cuit;
        using var conn = Db.Open(Db.TargetCs);
        using var ins = new SqlCommand("""
            INSERT INTO Assignments (EmployeeId, CompanyId, UnionId, IsVoluntaryMember, CategoryCode, CreatedAt, IsActive, TerminationDate)
            OUTPUT INSERTED.Id VALUES (@emp, @comp, @union, @vol, NULL, @fecha, 1, NULL)
            """, conn);
        ins.Parameters.AddWithValue("@emp", empId);
        ins.Parameters.AddWithValue("@comp", compId);
        ins.Parameters.AddWithValue("@union", _unionId);
        ins.Parameters.AddWithValue("@vol", isVoluntary ? 1 : 0);
        ins.Parameters.AddWithValue("@fecha", fecha);
        assId = (int)ins.ExecuteScalar();
        _assignmentByKey[key] = assId;
        return true;
    }

    // ---------- RECONCILIACION ----------
    private static void Reconcile()
    {
        Log.Section("Reconciliación destino vs legado");
        var q = """
            SELECT
              (SELECT COUNT(*) FROM Provinces) provs,
              (SELECT COUNT(*) FROM Cities) cities,
              (SELECT COUNT(*) FROM Unions) unions,
              (SELECT COUNT(*) FROM EmployeeCategories) cats,
              (SELECT COUNT(*) FROM SalaryScales) scales,
              (SELECT COUNT(*) FROM Companies) companies,
              (SELECT COUNT(*) FROM Employees) employees,
              (SELECT COUNT(*) FROM Assignments) assignments,
              (SELECT COUNT(*) FROM AccountingStudios) studios,
              (SELECT COUNT(*) FROM Declarations) declarations,
              (SELECT COUNT(*) FROM DeclarationDetails) details,
              (SELECT ROUND(SUM(TotalAmount),2) FROM Declarations) decl_amount,
              (SELECT ROUND(SUM(TotalWithheld),2) FROM DeclarationDetails) detail_amount
            """;
        using var conn = Db.Open(Db.TargetCs);
        using var cmd = new SqlCommand(q, conn);
        using var r = cmd.ExecuteReader();
        r.Read();
        Log.Write($"Provincias: {r["provs"]}  Ciudades: {r["cities"]}  Sindicatos: {r["unions"]}");
        Log.Write($"Categorías: {r["cats"]}  Escalas: {r["scales"]}  Empresas: {r["companies"]}");
        Log.Write($"Empleados: {r["employees"]}  Asignaciones: {r["assignments"]}  Estudios: {r["studios"]}");
        Log.Write($"Declaraciones: {r["declarations"]}  Detalles: {r["details"]}");
        Log.Write($"Monto cabeceras: {r["decl_amount"]}  Monto detalles: {r["detail_amount"]}");
    }

    /// <summary>
    /// Verifica que el DESTINO (SIRGA) y el LEGADO (sistema viejo) digan lo mismo en lo que tiene que
    /// coincidir: cabeceras de DDJJ, renglones, empleados y asignaciones. No escribe nada.
    /// Devuelve por pantalla las diferencias con ejemplos, para poder corregirlas con sync.
    /// </summary>
    private static void CheckConsistencia()
    {
        Log.Section("Consistencia destino vs legado (no escribe)");

        // ---------------- cabeceras de DDJJ (última rectificación por período) ----------------
        var legado = new Dictionary<string, (int Rect, decimal Total, bool Pagada)>(StringComparer.Ordinal);
        using (var src = Db.Open(Db.LegacyCs))
        using (var cmd = new SqlCommand("""
            SELECT CUIT_STR, p, rect, fpago, total FROM (
              SELECT CUIT_STR, CONVERT(varchar(7), periodo, 126) p, rect, fpago,
                     ISNULL(titem1,0) + ISNULL(titem2,0) + ISNULL(titem3,0) total,
                     ROW_NUMBER() OVER (PARTITION BY CONVERT(varchar(7), periodo, 126), CUIT_STR
                                        ORDER BY rect DESC, fmod DESC, fpres DESC, Id DESC) rn
              FROM ddjjt WHERE rect < 100 AND periodo >= '1990-01-01' AND periodo <= GETDATE()
            ) x WHERE rn = 1
            """, src) { CommandTimeout = 900 })
        using (var r = cmd.ExecuteReader())
            while (r.Read())
            {
                var cuit = Db.PadCuit(Db.Str(r["CUIT_STR"]));
                if (cuit.Length == 0) continue;
                legado[cuit + "|" + Db.Str(r["p"])] =
                    (Db.Int(r["rect"]), Db.Dec(r["total"]), r["fpago"] != DBNull.Value);
            }
        Log.Write($"  Legado: {legado.Count} cabeceras");

        // Pagos: se mira si CUALQUIER fila del período está pagada (la original puede estar paga y la
        // rectificativa no, o al revés). Comparar sólo la elegida daba 1.694 falsos positivos.
        var pagadasLegado = new HashSet<string>(StringComparer.Ordinal);
        using (var srcP = Db.Open(Db.LegacyCs))
        using (var cmdP = new SqlCommand("""
            SELECT DISTINCT CUIT_STR, CONVERT(varchar(7), periodo, 126) p
            FROM ddjjt WHERE fpago IS NOT NULL AND rect < 100
            """, srcP) { CommandTimeout = 900 })
        using (var rP = cmdP.ExecuteReader())
            while (rP.Read())
            {
                var cuitP = Db.PadCuit(Db.Str(rP["CUIT_STR"]));
                if (cuitP.Length > 0) pagadasLegado.Add(cuitP + "|" + Db.Str(rP["p"]));
            }

        var sirga = new Dictionary<string, (int Id, decimal Total, bool Pagada, bool Rect, int Estado)>(StringComparer.Ordinal);
        using (var t = Db.Open(Db.TargetCs))
        using (var cmd = new SqlCommand("""
            SELECT d.Id, d.Period, REPLACE(REPLACE(c.Cuit,'-',''),' ','') cuit, d.TotalAmount, d.PaymentDate, d.IsRectification, d.Status
            FROM Declarations d JOIN Companies c ON c.Id = d.CompanyId
            """, t) { CommandTimeout = 900 })
        using (var r = cmd.ExecuteReader())
            while (r.Read())
                sirga[Db.Str(r["cuit"]) + "|" + Db.Str(r["Period"])] =
                    (r.GetInt32(0), Db.Dec(r["TotalAmount"]), r["PaymentDate"] != DBNull.Value, Db.Bit(r["IsRectification"]), Db.Int(r["Status"]));
        Log.Write($"  SIRGA : {sirga.Count} cabeceras");

        var faltanEnSirga = legado.Keys.Where(k => !sirga.ContainsKey(k)).ToList();
        var sobranEnSirga = sirga.Keys.Where(k => !legado.ContainsKey(k)).ToList();
        var impagas = legado.Where(kv => pagadasLegado.Contains(kv.Key) && sirga.TryGetValue(kv.Key, out var s) && !s.Pagada).Select(kv => kv.Key).ToList();
        var pagadasDeMas = legado.Where(kv => !pagadasLegado.Contains(kv.Key) && sirga.TryGetValue(kv.Key, out var s) && s.Pagada).Select(kv => kv.Key).ToList();
        var rectMal = legado.Where(kv => kv.Value.Rect > 0 && sirga.TryGetValue(kv.Key, out var s) && !s.Rect).Select(kv => kv.Key).ToList();

        Log.Section("1) CABECERAS DDJJ (ddjjt vs Declarations)");
        Log.Write($"  En el legado y NO en SIRGA : {faltanEnSirga.Count}");
        var borradores = 0;
        var sobrantesReales = new List<string>();
        foreach (var k in sobranEnSirga)
        {
            if (sirga.TryGetValue(k, out var s) && s.Estado == 1) borradores++;   // Status 1 = borrador
            else sobrantesReales.Add(k);
        }
        Log.Write($"  En SIRGA y NO en el legado : {sobrantesReales.Count}  (+ {borradores} borradores de SIRGA [informativo])");
        Log.Write($"  Pagadas en legado / impagas en SIRGA : {impagas.Count}");
        Log.Write($"  Pagadas en SIRGA / impagas en legado : {pagadasDeMas.Count}");
        Log.Write($"  Rectificadas en legado sin marca en SIRGA : {rectMal.Count}");
        foreach (var k in faltanEnSirga.Take(5)) Log.Write($"     falta: {k}");
        foreach (var k in sobranEnSirga.Take(5)) Log.Write($"     sobra: {k}");

        // ---------------- renglones (ddjj vs DeclarationDetails) ----------------
        // Sólo la rectificativa ELEGIDA (la más alta que tenga renglones): comparar contra todas daba
        // falsos positivos, porque la original y la rectificativa no tienen la misma cantidad.
        var renLegado = new Dictionary<string, int>(StringComparer.Ordinal);
        var rectElegido = new Dictionary<string, int>(StringComparer.Ordinal);
        using (var src = Db.Open(Db.LegacyCs))
        using (var cmd = new SqlCommand("""
            SELECT CUIT_STR, CONVERT(varchar(7), periodo, 126) p, rect, COUNT(*) n
            FROM ddjj WHERE periodo >= '1990-01-01' AND periodo <= GETDATE()
            GROUP BY CUIT_STR, CONVERT(varchar(7), periodo, 126), rect
            """, src) { CommandTimeout = 900 })
        using (var r = cmd.ExecuteReader())
            while (r.Read())
            {
                var cuit = Db.PadCuit(Db.Str(r["CUIT_STR"]));
                if (cuit.Length == 0) continue;
                var clave = cuit + "|" + Db.Str(r["p"]);
                var rectFila = Db.Int(r["rect"]);
                if (!rectElegido.TryGetValue(clave, out var actual) || rectFila >= actual)
                {
                    rectElegido[clave] = rectFila;
                    renLegado[clave] = Db.Int(r["n"]);
                }
            }

        // Se compara SIEMPRE la versión más alta de SIRGA contra la rectificativa más alta del legado:
        // una rectificación es una fila más del mismo período, así que sumar todas las versiones
        // daba falsos positivos. Es el mismo criterio con el que el paso de detalles repara.
        var renSirga = new Dictionary<string, int>(StringComparer.Ordinal);
        using (var t = Db.Open(Db.TargetCs))
        using (var cmd = new SqlCommand("""
            SELECT cuit, periodo, n FROM (
              SELECT REPLACE(REPLACE(c.Cuit,'-',''),' ','') cuit, d.Period periodo,
                     (SELECT COUNT(*) FROM DeclarationDetails dd WHERE dd.RegistryId = d.Id) n,
                     ROW_NUMBER() OVER (PARTITION BY REPLACE(REPLACE(c.Cuit,'-',''),' ',''), d.Period
                                        ORDER BY d.VersionNumber DESC) rn
              FROM Declarations d JOIN Companies c ON c.Id = d.CompanyId
            ) x WHERE rn = 1
            """, t) { CommandTimeout = 900 })
        using (var r = cmd.ExecuteReader())
            while (r.Read())
                renSirga[Db.Str(r["cuit"]) + "|" + Db.Str(r["periodo"])] = Db.Int(r["n"]);

        var sinRenglones = renSirga.Count(kv => kv.Value == 0);
        var distinto = renLegado.Where(kv => renSirga.TryGetValue(kv.Key, out var n) && n != kv.Value)
                                .Select(kv => (kv.Key, Legado: kv.Value, Sirga: renSirga[kv.Key])).ToList();

        Log.Section("2) RENGLONES (ddjj vs DeclarationDetails)");
        Log.Write($"  Declaraciones en SIRGA SIN renglones : {sinRenglones}");
        Log.Write($"  Declaraciones con cantidad distinta : {distinto.Count}");
        foreach (var d in distinto.Take(5)) Log.Write($"     {d.Key}: legado {d.Legado} / SIRGA {d.Sirga}");

        // ---------------- empleados (maesoc vs Employees) ----------------
        var socios = new HashSet<string>(StringComparer.Ordinal);
        var estadoSocio = new Dictionary<string, (int Estado, DateTime? Baja)>(StringComparer.Ordinal);
        using (var src = Db.Open(Db.LegacyCs))
        using (var cmd = new SqlCommand("SELECT MAESOC_CUIL_STR, MAESOC_ESTADO, MAESOC_FECHADESEMPLEO FROM fotos_.maesoc", src) { CommandTimeout = 900 })
        using (var r = cmd.ExecuteReader())
            while (r.Read())
            {
                var cuil = Db.PadCuit(Db.Str(r[0]));
                if (cuil.Length > 0)
                {
                    socios.Add(cuil);
                    var baja = Db.Dt(r["MAESOC_FECHADESEMPLEO"]);
                    if (baja.HasValue && (baja.Value.Year < 1900 || baja.Value > DateTime.Today)) baja = null;
                    estadoSocio[cuil] = (Db.Int(r["MAESOC_ESTADO"]), baja);
                }
            }

        var empleados = new HashSet<string>(StringComparer.Ordinal);
        using (var t = Db.Open(Db.TargetCs))
        using (var cmd = new SqlCommand("SELECT Dni FROM Employees", t) { CommandTimeout = 900 })
        using (var r = cmd.ExecuteReader())
            while (r.Read())
            {
                var cuil = Db.PadCuit(Db.Str(r[0]));
                if (cuil.Length > 0) empleados.Add(cuil);
            }

        var sociosSinEmpleado = socios.Where(s => !empleados.Contains(s)).ToList();
        var empleadosSinSocio = empleados.Where(e => !socios.Contains(e)).ToList();

        Log.Section("3) EMPLEADOS (fotos_.maesoc vs Employees)");
        Log.Write($"  Socios en el legado : {socios.Count} | Empleados en SIRGA : {empleados.Count}");
        Log.Write($"  Socios SIN empleado en SIRGA : {sociosSinEmpleado.Count}");
        Log.Write($"  Empleados SIN ficha en el padrón (sólo están en DDJJ): {empleadosSinSocio.Count}  [informativo]");
        foreach (var s in sociosSinEmpleado.Take(5)) Log.Write($"     falta empleado: {s}");

        // ---------------- asignaciones (socemp vs Assignments) ----------------
        var paresLegado = new HashSet<string>(StringComparer.Ordinal);
        var estadoPar = new Dictionary<string, (int Estado, DateTime? Baja)>(StringComparer.Ordinal);
        using (var src = Db.Open(Db.LegacyCs))
        using (var cmd = new SqlCommand("SELECT SOCEMP_CUIL_STR, SOCEMP_CUITE_STR, SOCEMP_ESTADO, SOCEMP_FECHABAJA FROM fotos_.socemp", src) { CommandTimeout = 900 })
        using (var r = cmd.ExecuteReader())
            while (r.Read())
            {
                var cuil = Db.PadCuit(Db.Str(r[0]));
                var cuit = Db.PadCuit(Db.Str(r[1]));
                if (cuil.Length > 0 && cuit.Length > 0 && cuit != "99999999999")
                {
                    var clave = cuil + "|" + cuit;
                    paresLegado.Add(clave);
                    var baja = Db.Dt(r["SOCEMP_FECHABAJA"]);
                    if (baja.HasValue && (baja.Value.Year < 1900 || baja.Value > DateTime.Today)) baja = null;
                    estadoPar[clave] = (Db.Int(r["SOCEMP_ESTADO"]), baja);
                }
            }

        var paresSirga = new HashSet<string>(StringComparer.Ordinal);
        using (var t = Db.Open(Db.TargetCs))
        using (var cmd = new SqlCommand("""
            SELECT e.Dni, c.Cuit FROM Assignments a
            JOIN Employees e ON e.Id = a.EmployeeId
            JOIN Companies c ON c.Id = a.CompanyId
            """, t) { CommandTimeout = 900 })
        using (var r = cmd.ExecuteReader())
            while (r.Read())
            {
                var cuil = Db.PadCuit(Db.Str(r[0]));
                var cuit = Db.PadCuit(Db.Str(r[1]));
                if (cuil.Length > 0 && cuit.Length > 0) paresSirga.Add(cuil + "|" + cuit);
            }

        var paresFaltan = paresLegado.Where(p => !paresSirga.Contains(p)).ToList();
        var paresSobran = paresSirga.Where(p => !paresLegado.Contains(p)).ToList();

        Log.Section("4) ASIGNACIONES (fotos_.socemp vs Assignments)");
        Log.Write($"  Pares en el legado : {paresLegado.Count} | En SIRGA : {paresSirga.Count}");
        Log.Write($"  Pares del legado SIN asignación en SIRGA : {paresFaltan.Count}");
        Log.Write($"  Asignaciones de SIRGA SIN par en el legado : {paresSobran.Count}");
        foreach (var p in paresFaltan.Take(5)) Log.Write($"     falta asignación: {p}");

        // ---------------- estados (altas y bajas) ----------------
        var empEstadoMal = 0;
        using (var t = Db.Open(Db.TargetCs))
        using (var cmd = new SqlCommand("SELECT Dni, Status, TerminationDate FROM Employees", t) { CommandTimeout = 900 })
        using (var r = cmd.ExecuteReader())
            while (r.Read())
            {
                var cuil = Db.PadCuit(Db.Str(r["Dni"]));
                if (!estadoSocio.TryGetValue(cuil, out var leg)) continue;
                var esperado = leg.Estado == 1 ? 1 : 5;
                var bajaSirga = Db.Dt(r["TerminationDate"]);
                if (bajaSirga.HasValue && (bajaSirga.Value.Year < 1900 || bajaSirga.Value > DateTime.Today)) bajaSirga = null;
                if (Db.Int(r["Status"]) != esperado
                    || (bajaSirga?.Date ?? DateTime.MinValue) != (leg.Baja?.Date ?? DateTime.MinValue)) empEstadoMal++;
            }

        var asigEstadoMal = 0;
        using (var t = Db.Open(Db.TargetCs))
        using (var cmd = new SqlCommand("""
            SELECT e.Dni, c.Cuit, a.IsActive, a.TerminationDate FROM Assignments a
            JOIN Employees e ON e.Id = a.EmployeeId
            JOIN Companies c ON c.Id = a.CompanyId
            """, t) { CommandTimeout = 900 })
        using (var r = cmd.ExecuteReader())
            while (r.Read())
            {
                var clave = Db.PadCuit(Db.Str(r["Dni"])) + "|" + Db.PadCuit(Db.Str(r["Cuit"]));
                if (!estadoPar.TryGetValue(clave, out var leg)) continue;
                var esperado = leg.Estado == 1 ? 1 : 0;
                var bajaSirga = Db.Dt(r["TerminationDate"]);
                if (bajaSirga.HasValue && (bajaSirga.Value.Year < 1900 || bajaSirga.Value > DateTime.Today)) bajaSirga = null;
                if ((Db.Bit(r["IsActive"]) ? 1 : 0) != esperado
                    || (bajaSirga?.Date ?? DateTime.MinValue) != (leg.Baja?.Date ?? DateTime.MinValue)) asigEstadoMal++;
            }

        Log.Section("5) ESTADOS (altas y bajas)");
        Log.Write($"  Empleados con estado o fecha de baja distinta : {empEstadoMal}");
        Log.Write($"  Asignaciones con estado o baja distinta       : {asigEstadoMal}");

        Log.Section("RESUMEN");
        var problemas = faltanEnSirga.Count + sobrantesReales.Count + impagas.Count + pagadasDeMas.Count + rectMal.Count
                      + distinto.Count + sociosSinEmpleado.Count + paresFaltan.Count + empEstadoMal + asigEstadoMal;
        var informativos = sinRenglones + borradores + empleadosSinSocio.Count + paresSobran.Count;

        if (problemas == 0)
            Log.Write("  CONSISTENTE: no hay diferencias que requieran accion.");
        else
            Log.Write($"  PROBLEMAS A RESOLVER: {problemas} (ver el detalle arriba)");

        Log.Write($"  Informativo (no requiere accion): {informativos}");
        if (sinRenglones > 0) Log.Write($"     {sinRenglones} declaraciones sin renglones: el legado tampoco los tiene (períodos viejos)");
        if (empleadosSinSocio.Count > 0) Log.Write($"     {empleadosSinSocio.Count} empleados que sólo existen en las DDJJ del legado (sin ficha en el padrón)");
        if (borradores > 0) Log.Write($"     {borradores} borradores de SIRGA que todavía no se presentaron");
        if (paresFaltan.Count > 0) Log.Write($"     {paresFaltan.Count} pares socio-empresa del legado sin asignación (revisar: pueden ser socios sin empresa real)");
        if (paresSobran.Count > 0) Log.Write($"     {paresSobran.Count} asignaciones de SIRGA que no están en el legado");
        if (faltanEnSirga.Count > 0) Log.Write($"     {faltanEnSirga.Count} declaraciones del legado sin empresa en SIRGA");
    }
    // ---------- HELPERS ----------
    private static string Trunc(string s, int max) => s.Length <= max ? s : s[..max];
    private static object NullIfEmpty(string s, int max) => s.Length == 0 ? DBNull.Value : Trunc(s, max);
}
