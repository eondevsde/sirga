#:package Microsoft.Extensions.Identity.Core@10.0.7
#:package Microsoft.Extensions.Identity.Stores@10.0.7
#:package Microsoft.Data.SqlClient@6.0.1
#:package System.Text.Encoding.CodePages@10.0.0

// ==========================================================================================
// IMPORTAR ESTUDIOS CONTABLES desde el listado de empresas del sindicato.
//
// El archivo de origen es el "Usuario estudio contables.csv": una fila por EMPRESA, con los datos
// del estudio que la maneja. De ahí se sacan:
//   - Los estudios (nombre, email, teléfonos, domicilio, localidad, persona de referencia).
//   - El usuario de acceso de cada estudio (el email) con rol EstudioContable.
//   - La contraseña de TODOS los usuarios de estudio, que se fija en "12345".
//   - Los estudios que NO están en el archivo se marcan como bloqueados (no pueden ingresar).
//
// USO
//   dotnet run tools/importar-estudios.cs -- "<ruta csv>" "<base>" [--aplicar] [--bloquear]
//
//   Sin --aplicar: corre TODO dentro de una transacción y la REVIERTE (simulacro real, se ve el
//   informe con los mismos números que aplicaría).
//   Con --aplicar: confirma los cambios.
//   --empresas: además actualiza qué estudio maneja cada empresa y reconstruye su historial.
//   --bloquear: además marca bloqueados los estudios que no figuran en el archivo (por defecto NO,
//               para poder revisar primero el listado).
// ==========================================================================================

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;

var rutaCsv = args.Length > 0 ? args[0] : @"C:\Users\drop1\Downloads\Usuario estudio contables.csv";
var baseDatos = args.Length > 1 ? args[1] : "SIRGA_DB_DEMO";
var aplicar = args.Contains("--aplicar");
var bloquear = args.Contains("--bloquear");
var empresas = args.Contains("--empresas");
const string ClaveNueva = "12345";

if (!File.Exists(rutaCsv)) { Console.WriteLine($"ERROR: no existe el archivo {rutaCsv}"); return 2; }

var connString = $"Server=localhost;Database={baseDatos};Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true";

// ---------------------------------------------------------------- lectura del archivo
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
var texto = File.ReadAllText(rutaCsv, Encoding.GetEncoding(1252));
var lineas = texto.Split('\n').Select(l => l.TrimEnd('\r')).Where(l => l.Trim() != "").ToList();
if (lineas.Count > 0 && lineas[0].StartsWith("sep=", StringComparison.OrdinalIgnoreCase)) lineas.RemoveAt(0);
lineas.RemoveAt(0); // encabezado

var filas = new List<FilaCsv>();
foreach (var l in lineas)
{
    var c = l.Split(';');
    if (c.Length < 11) continue;
    var cuil = c[0].Trim();
    if (cuil.Length == 0 || !cuil.All(char.IsDigit)) continue;
    filas.Add(new FilaCsv
    {
        Cuit = cuil,
        Empresa = c[1].Trim(),
        Activo = c[2].Trim(),
        Fecha = ParsearFecha(c[3].Trim()),
        Responsable = c[4].Trim(),
        Email = c[5].Trim().ToLowerInvariant(),
        Telefono = c[6].Trim(),
        Estudio = c[7].Trim(),
        EstudioTelefono = c[8].Trim(),
        EstudioDomicilio = c[9].Trim(),
        EstudioLocalidad = c[10].Trim(),
    });
}
Console.WriteLine($"Archivo: {rutaCsv}");
Console.WriteLine($"  filas válidas: {filas.Count}");

// ---------------------------------------------------------------- agrupación por estudio
var estudios = filas
    .Where(f => f.Estudio.Length > 0 && !EsCuit(f.Estudio))
    .GroupBy(f => Normalizar(f.Estudio))
    .Select(g => new EstudioCsv
    {
        Clave = g.Key,
        Nombre = g.Select(f => f.Estudio).First(),
        // El email de acceso: el que más se repite en las filas del estudio (si hay empate, el primero).
        Email = g.Where(f => f.Email.Length > 0).GroupBy(f => f.Email)
                 .OrderByDescending(e => e.Count()).Select(e => e.Key).DefaultIfEmpty("").First(),
        EmailsDistintos = g.Select(f => f.Email).Where(e => e.Length > 0).Distinct().Count(),
        Responsable = g.Select(f => f.Responsable).FirstOrDefault(r => r.Length > 0) ?? "",
        Telefono = g.Select(f => f.Telefono).FirstOrDefault(t => t.Length > 0) ?? "",
        EstudioTelefono = g.Select(f => f.EstudioTelefono).FirstOrDefault(t => t.Length > 0) ?? "",
        Domicilio = g.Select(f => f.EstudioDomicilio).FirstOrDefault(t => t.Length > 0) ?? "",
        Localidad = g.Select(f => f.EstudioLocalidad).FirstOrDefault(t => t.Length > 0) ?? "",
    })
    .ToList();

Console.WriteLine($"  estudios distintos: {estudios.Count}");
Console.WriteLine($"  de ellos con email: {estudios.Count(e => e.Email.Length > 0)}");
Console.WriteLine($"  con más de un email en el archivo: {estudios.Count(e => e.EmailsDistintos > 1)}");

// ---------------------------------------------------------------- base
using var conn = new SqlConnection(connString);
conn.Open();
Console.WriteLine($"Base: {conn.Database}");
Console.WriteLine();

using var tx = conn.BeginTransaction();
var hasher = new PasswordHasher<IdentityUser>();

var existentes = CargarEstudios(conn, tx);
var ciudades = CargarCiudades(conn, tx);
var rolId = AsegurarRol(conn, tx, "EstudioContable");

var clavesCsv = new HashSet<string>(estudios.Select(e => e.Clave));
var crear = new List<EstudioCsv>();
var actualizar = new List<(EstudioCsv Csv, EstudioDb Db)>();
var sinEmail = new List<EstudioCsv>();
var ciudadesSinMatch = new SortedSet<string>();

foreach (var e in estudios)
{
    var db = existentes.FirstOrDefault(x => Normalizar(x.Nombre) == e.Clave);
    if (e.Email.Length == 0) { sinEmail.Add(e); continue; }
    if (db is null) crear.Add(e);
    else actualizar.Add((e, db));

    if (e.Localidad.Length > 0 && BuscarCiudad(ciudades, e.Localidad) is null)
        ciudadesSinMatch.Add(e.Localidad);
}

var aBloquear = existentes.Where(d => !clavesCsv.Contains(Normalizar(d.Nombre))).ToList();

// ---------------------------------------------------------------- informe previo
Console.WriteLine("=== LO QUE VA A HACER ===");
Console.WriteLine($"Estudios a CREAR          : {crear.Count}");
Console.WriteLine($"Estudios a ACTUALIZAR     : {actualizar.Count}");
Console.WriteLine($"  de esos, cambia el login: {actualizar.Count(a => !string.Equals(a.Db.Responsable, a.Csv.Email, StringComparison.OrdinalIgnoreCase))}");
Console.WriteLine($"Sin email en el archivo   : {sinEmail.Count}   (no se puede crear su usuario)");
Console.WriteLine($"Estudios en la base que NO están en el archivo: {aBloquear.Count}");
Console.WriteLine($"  (se marcan bloqueados solo si se pasa --bloquear: {(bloquear ? "SÍ" : "NO")})");
Console.WriteLine($"Localidades sin ciudad    : {ciudadesSinMatch.Count}");
foreach (var l in ciudadesSinMatch.Take(8)) Console.WriteLine($"    - {l}");
Console.WriteLine();

// ---------------------------------------------------------------- aplicación
var usuariosCreados = 0;
var usuariosActualizados = 0;
var estudiosCreados = 0;
var estudiosActualizados = 0;
var bloqueados = 0;
var errores = new List<string>();

foreach (var e in crear)
{
    var ciudadId = BuscarCiudad(ciudades, e.Localidad);
    try
    {
        var id = InsertarEstudio(conn, tx, e, ciudadId);
        estudiosCreados++;
        AsegurarUsuario(conn, tx, hasher, e.Email, rolId, ref usuariosCreados, ref usuariosActualizados);
    }
    catch (Exception ex) { errores.Add($"crear {e.Nombre}: {ex.Message}"); }
}

foreach (var (csv, db) in actualizar)
{
    var ciudadId = BuscarCiudad(ciudades, csv.Localidad);
    try
    {
        ActualizarEstudio(conn, tx, db.Id, csv, ciudadId);
        estudiosActualizados++;
        AsegurarUsuario(conn, tx, hasher, csv.Email, rolId, ref usuariosCreados, ref usuariosActualizados);
    }
    catch (Exception ex) { errores.Add($"actualizar {csv.Nombre}: {ex.Message}"); }
}

if (bloquear)
{
    foreach (var d in aBloquear)
    {
        using var cmd = new SqlCommand("UPDATE AccountingStudios SET IsBlocked = 1 WHERE Id = @id", conn, tx);
        cmd.Parameters.AddWithValue("@id", d.Id);
        cmd.ExecuteNonQuery();
        bloqueados++;
    }
}

// ---------------------------------------------------------------- fase 2: empresas y traspasos
var empresasActualizadas = 0;
var empresasReasignadas = 0;
var empresasSinMatch = 0;
var historialInsertado = 0;
var empresasConTraspasos = 0;

if (empresas)
{
    const string Marca = "Importado del listado de empresas del sindicato";

    // Mapa: nombre normalizado del estudio -> Id (ya creados/actualizados arriba)
    var mapa = new Dictionary<string, int>(StringComparer.Ordinal);
    using (var cmd = new SqlCommand("SELECT Id, Name FROM AccountingStudios", conn, tx))
    using (var r = cmd.ExecuteReader())
        while (r.Read()) mapa[Normalizar(r.GetString(1))] = r.GetInt32(0);

    // Por CUIT: la secuencia de estudios con su fecha (la más reciente es la actual)
    foreach (var grupo in filas.Where(f => f.Estudio.Length > 0 && !EsCuit(f.Estudio) && f.Fecha is not null)
                               .GroupBy(f => f.Cuit))
    {
        var pasos = grupo
            .GroupBy(f => Normalizar(f.Estudio))
            .Select(s => (Clave: s.Key, Fecha: s.Max(x => x.Fecha!.Value)))
            .Where(s => mapa.ContainsKey(s.Clave))
            .OrderBy(s => s.Fecha)
            .ToList();
        if (pasos.Count == 0) continue;

        var empresaId = BuscarEmpresa(conn, tx, grupo.Key);
        if (empresaId is null) { empresasSinMatch++; continue; }

        var actual = mapa[pasos[^1].Clave];
        var anterior = StudioActual(conn, tx, empresaId.Value);

        using (var upd = new SqlCommand("UPDATE Companies SET AccountingStudioId = @e WHERE Id = @id", conn, tx))
        {
            upd.Parameters.AddWithValue("@e", actual);
            upd.Parameters.AddWithValue("@id", empresaId.Value);
            upd.ExecuteNonQuery();
        }
        empresasActualizadas++;
        if (anterior != actual) empresasReasignadas++;
        if (pasos.Count > 1) empresasConTraspasos++;

        // Historial: se rehace el tramo de este importador (idempotente por la marca).
        // El índice único es FILTRADO ([To] IS NULL): sólo puede haber UNA fila abierta por empresa.
        using (var del = new SqlCommand("DELETE FROM CompanyStudioHistories WHERE CompanyId = @id AND Reason = @m", conn, tx))
        {
            del.Parameters.AddWithValue("@id", empresaId.Value);
            del.Parameters.AddWithValue("@m", Marca);
            del.ExecuteNonQuery();
        }

        var ultimaFecha = pasos[^1].Fecha;
        var abierta = FilaAbierta(conn, tx, empresaId.Value);
        var insertarUltimo = true;

        if (abierta is { } a)
        {
            if (a.Estudio == actual)
            {
                // Ya hay una fila abierta para el estudio actual: se corrige la fecha y no se duplica.
                using var upd = new SqlCommand("UPDATE CompanyStudioHistories SET [From] = @f WHERE Id = @id", conn, tx);
                upd.Parameters.AddWithValue("@f", ultimaFecha);
                upd.Parameters.AddWithValue("@id", a.Id);
                upd.ExecuteNonQuery();
                insertarUltimo = false;
            }
            else
            {
                // Se cierra la anterior justo cuando empieza la nueva.
                using var upd = new SqlCommand("UPDATE CompanyStudioHistories SET [To] = @f WHERE Id = @id", conn, tx);
                upd.Parameters.AddWithValue("@f", ultimaFecha);
                upd.Parameters.AddWithValue("@id", a.Id);
                upd.ExecuteNonQuery();
            }
        }

        for (var i = 0; i < pasos.Count; i++)
        {
            if (i == pasos.Count - 1 && !insertarUltimo) break;
            using var ins = new SqlCommand(@"
                INSERT INTO CompanyStudioHistories (CompanyId, AccountingStudioId, [From], [To], AuthorizedByUsername, Reason)
                VALUES (@id, @estudio, @desde, @hasta, @quien, @m)", conn, tx);
            ins.Parameters.AddWithValue("@id", empresaId.Value);
            ins.Parameters.AddWithValue("@estudio", mapa[pasos[i].Clave]);
            ins.Parameters.AddWithValue("@desde", pasos[i].Fecha);
            ins.Parameters.AddWithValue("@hasta", (object?) (i + 1 < pasos.Count ? pasos[i + 1].Fecha : null) ?? DBNull.Value);
            ins.Parameters.AddWithValue("@quien", "IMPORTACION");
            ins.Parameters.AddWithValue("@m", Marca);
            ins.ExecuteNonQuery();
            historialInsertado++;
        }
    }

    Console.WriteLine();
    Console.WriteLine("=== EMPRESAS ===");
    Console.WriteLine($"Empresas actualizadas   : {empresasActualizadas}");
    Console.WriteLine($"  cambiaron de estudio  : {empresasReasignadas}");
    Console.WriteLine($"  con traspasos         : {empresasConTraspasos}");
    Console.WriteLine($"CUIT del archivo que no están en SIRGA: {empresasSinMatch}");
    Console.WriteLine($"Renglones de historial  : {historialInsertado}");
}
// ---------------------------------------------------------------- cierre
if (aplicar)
{
    tx.Commit();
    Console.WriteLine("=== APLICADO (commit) ===");
}
else
{
    tx.Rollback();
    Console.WriteLine("=== SIMULACRO: se revirtió todo (nada cambió) ===");
}

Console.WriteLine($"Estudios creados      : {estudiosCreados}");
Console.WriteLine($"Estudios actualizados : {estudiosActualizados}");
Console.WriteLine($"Usuarios creados      : {usuariosCreados}");
Console.WriteLine($"Usuarios con clave '{ClaveNueva}': {usuariosActualizados}");
Console.WriteLine($"Estudios bloqueados   : {bloqueados}");
Console.WriteLine($"Errores               : {errores.Count}");
foreach (var e in errores.Take(10)) Console.WriteLine($"  ERROR {e}");
if (!aplicar) Console.WriteLine("\nPara aplicarlo de verdad, agregar --aplicar");
return errores.Count == 0 ? 0 : 1;

// ===================================================================================== helpers

static DateTime? ParsearFecha(string s) =>
    DateTime.TryParseExact(s, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var f) ? f : null;

static bool EsCuit(string s) => s.Length == 11 && s.All(char.IsDigit);
static string LimpiarLocalidad(string localidad) => System.Text.RegularExpressions.Regex.Replace(localidad, @"\s*\(\s*\d*\s*\)\s*$", "").Trim();

static string Normalizar(string s)
{
    if (string.IsNullOrWhiteSpace(s)) return "";
    var d = s.Normalize(NormalizationForm.FormD);
    var sb = new StringBuilder();
    foreach (var ch in d)
        if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark) sb.Append(ch);
    var limpio = new string(sb.ToString().ToUpperInvariant().Where(ch => char.IsLetterOrDigit(ch)).ToArray());
    return limpio;
}

static int? BuscarCiudad(List<Ciudad> ciudades, string localidad)
{
    if (string.IsNullOrWhiteSpace(localidad)) return null;
    // En el archivo la localidad viene con el código postal: "Alta Gracia (5186)".
    var limpia = LimpiarLocalidad(localidad);
    var norm = Normalizar(limpia);

    // Alias para los nombres que el archivo y el catálogo escriben distinto.
    var alias = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["CIUDADAUTONOMADEBUENOSAIRES"] = "CAPITALFEDERAL",
        ["CAPITALFEDERAL"] = "CAPITALFEDERAL",
        ["SANFERNANDODELVALLEDECATAMARCA"] = "SANFDODELVALLEDECATAMARCA",
    };
    if (alias.TryGetValue(norm, out var destino)) norm = destino;

    var exacta = ciudades.FirstOrDefault(c => Normalizar(c.Nombre) == norm);
    if (exacta is not null) return exacta.Id;

    // Segundo intento por prefijo: hay nombres del catálogo con la Ñ mal codificada
    // (por ejemplo "PRESIDENCIA ROQUE SAENZ PEÃƒÆ’Ã¢â‚¬ËœA"), así que comparo el arranque del nombre.
    if (norm.Length >= 12)
    {
        var prefijo = norm[..12];
        var porPrefijo = ciudades.FirstOrDefault(c => Normalizar(c.Nombre).StartsWith(prefijo, StringComparison.Ordinal));
        if (porPrefijo is not null) return porPrefijo.Id;
    }
    return null;
}

static List<EstudioDb> CargarEstudios(SqlConnection conn, SqlTransaction tx)
{
    var lista = new List<EstudioDb>();
    using var cmd = new SqlCommand(
        "SELECT Id, ISNULL(Name,''), ISNULL(ResponsibleUsername,''), ISNULL(Email,''), IsApproved, IsActive, ISNULL(IsBlocked,0) FROM AccountingStudios", conn, tx);
    using var r = cmd.ExecuteReader();
    while (r.Read())
        lista.Add(new EstudioDb
        {
            Id = r.GetInt32(0),
            Nombre = r.GetString(1),
            Responsable = r.GetString(2),
            Email = r.GetString(3),
            Aprobado = r.GetBoolean(4),
            Activo = r.GetBoolean(5),
            Bloqueado = r.GetBoolean(6),
        });
    return lista;
}

static List<Ciudad> CargarCiudades(SqlConnection conn, SqlTransaction tx)
{
    var lista = new List<Ciudad>();
    using var cmd = new SqlCommand("SELECT Id, ISNULL(Name,'') FROM Cities", conn, tx);
    using var r = cmd.ExecuteReader();
    while (r.Read()) lista.Add(new Ciudad { Id = r.GetInt32(0), Nombre = r.GetString(1) });
    return lista;
}

static string AsegurarRol(SqlConnection conn, SqlTransaction tx, string rol)
{
    var norm = rol.ToUpperInvariant();
    using (var sel = new SqlCommand("SELECT Id FROM AspNetRoles WHERE NormalizedName = @n", conn, tx))
    {
        sel.Parameters.AddWithValue("@n", norm);
        if (sel.ExecuteScalar() is string existente) return existente;
    }
    var id = Guid.NewGuid().ToString();
    using var ins = new SqlCommand(
        "INSERT INTO AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp) VALUES (@id, @name, @n, @c)", conn, tx);
    ins.Parameters.AddWithValue("@id", id);
    ins.Parameters.AddWithValue("@name", rol);
    ins.Parameters.AddWithValue("@n", norm);
    ins.Parameters.AddWithValue("@c", Guid.NewGuid().ToString());
    ins.ExecuteNonQuery();
    return id;
}

static int InsertarEstudio(SqlConnection conn, SqlTransaction tx, EstudioCsv e, int? ciudadId)
{
    using var cmd = new SqlCommand(@"
        INSERT INTO AccountingStudios
            (Name, ResponsibleUsername, Email, ContactPhone, ContactCellPhone, ReferencePerson, ReferencePhone,
             Address, CityId, IsApproved, IsActive, IsBlocked, CreatedAt, ApprovedAt)
        VALUES
            (@nombre, @usuario, @email, @tel, @tel, @ref, @telref, @dom, @ciudad, 1, 1, 0, GETDATE(), GETDATE());
        SELECT CAST(SCOPE_IDENTITY() AS int);", conn, tx);
    cmd.Parameters.AddWithValue("@nombre", Recortar(e.Nombre, 200));
    cmd.Parameters.AddWithValue("@usuario", Recortar(e.Email, 100));
    cmd.Parameters.AddWithValue("@email", Recortar(e.Email, 200));
    cmd.Parameters.AddWithValue("@tel", (object?)Recortar(e.EstudioTelefono.Length > 0 ? e.EstudioTelefono : e.Telefono, 50) ?? DBNull.Value);
    cmd.Parameters.AddWithValue("@ref", (object?)Recortar(e.Responsable, 100) ?? DBNull.Value);
    cmd.Parameters.AddWithValue("@telref", (object?)Recortar(e.Telefono, 50) ?? DBNull.Value);
    cmd.Parameters.AddWithValue("@dom", (object?)Recortar(e.Domicilio, 250) ?? DBNull.Value);
    cmd.Parameters.AddWithValue("@ciudad", (object?)ciudadId ?? DBNull.Value);
    return (int)cmd.ExecuteScalar()!;
}

static void ActualizarEstudio(SqlConnection conn, SqlTransaction tx, int id, EstudioCsv e, int? ciudadId)
{
    using var cmd = new SqlCommand(@"
        UPDATE AccountingStudios SET
            ResponsibleUsername = @usuario,
            Email               = @email,
            ContactPhone        = COALESCE(@tel, ContactPhone),
            ContactCellPhone    = COALESCE(@tel, ContactCellPhone),
            ReferencePerson     = COALESCE(@ref, ReferencePerson),
            ReferencePhone      = COALESCE(@telref, ReferencePhone),
            Address             = COALESCE(@dom, Address),
            CityId              = COALESCE(@ciudad, CityId),
            IsApproved          = 1,
            IsActive            = 1
        WHERE Id = @id", conn, tx);
    cmd.Parameters.AddWithValue("@id", id);
    cmd.Parameters.AddWithValue("@usuario", Recortar(e.Email, 100));
    cmd.Parameters.AddWithValue("@email", Recortar(e.Email, 200));
    cmd.Parameters.AddWithValue("@tel", (object?)Recortar(e.EstudioTelefono.Length > 0 ? e.EstudioTelefono : e.Telefono, 50) ?? DBNull.Value);
    cmd.Parameters.AddWithValue("@ref", (object?)Recortar(e.Responsable, 100) ?? DBNull.Value);
    cmd.Parameters.AddWithValue("@telref", (object?)Recortar(e.Telefono, 50) ?? DBNull.Value);
    cmd.Parameters.AddWithValue("@dom", (object?)Recortar(e.Domicilio, 250) ?? DBNull.Value);
    cmd.Parameters.AddWithValue("@ciudad", (object?)ciudadId ?? DBNull.Value);
    cmd.ExecuteNonQuery();
}

/// <summary>Crea el usuario si no existe y, en todos los casos, deja la contraseña en 12345.</summary>
static void AsegurarUsuario(SqlConnection conn, SqlTransaction tx, PasswordHasher<IdentityUser> hasher,
                            string email, string rolId, ref int creados, ref int actualizados)
{
    if (string.IsNullOrWhiteSpace(email)) return;

    string? id;
    using (var sel = new SqlCommand(
        "SELECT TOP 1 Id FROM AspNetUsers WHERE Email = @e OR UserName = @e ORDER BY Id", conn, tx))
    {
        sel.Parameters.AddWithValue("@e", email);
        id = sel.ExecuteScalar() as string;
    }

    var usuario = new IdentityUser { UserName = email, Email = email };
    var hash = hasher.HashPassword(usuario, ClaveNueva);

    if (id is null)
    {
        id = Guid.NewGuid().ToString();
        using var ins = new SqlCommand(@"
            INSERT INTO AspNetUsers (Id, UserName, NormalizedUserName, Email, NormalizedEmail,
                EmailConfirmed, PasswordHash, SecurityStamp, ConcurrencyStamp,
                PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnabled, AccessFailedCount)
            VALUES (@id, @u, @nu, @e, @ne, 1, @h, @s, @c, 0, 0, 1, 0)", conn, tx);
        ins.Parameters.AddWithValue("@id", id);
        ins.Parameters.AddWithValue("@u", email);
        ins.Parameters.AddWithValue("@nu", email.ToUpperInvariant());
        ins.Parameters.AddWithValue("@e", email);
        ins.Parameters.AddWithValue("@ne", email.ToUpperInvariant());
        ins.Parameters.AddWithValue("@h", hash);
        ins.Parameters.AddWithValue("@s", Guid.NewGuid().ToString());
        ins.Parameters.AddWithValue("@c", Guid.NewGuid().ToString());
        ins.ExecuteNonQuery();
        creados++;
    }
    else
    {
        // Clave 12345 + sello nuevo (invalida las sesiones abiertas).
        using var upd = new SqlCommand(
            "UPDATE AspNetUsers SET PasswordHash = @h, SecurityStamp = @s, EmailConfirmed = 1, LockoutEnd = NULL WHERE Id = @id", conn, tx);
        upd.Parameters.AddWithValue("@h", hash);
        upd.Parameters.AddWithValue("@s", Guid.NewGuid().ToString());
        upd.Parameters.AddWithValue("@id", id);
        upd.ExecuteNonQuery();
        actualizados++;
    }

    using var rol = new SqlCommand(
        "IF NOT EXISTS (SELECT 1 FROM AspNetUserRoles WHERE UserId = @u AND RoleId = @r) INSERT INTO AspNetUserRoles (UserId, RoleId) VALUES (@u, @r)", conn, tx);
    rol.Parameters.AddWithValue("@u", id);
    rol.Parameters.AddWithValue("@r", rolId);
    rol.ExecuteNonQuery();
}

static int? BuscarEmpresa(SqlConnection conn, SqlTransaction tx, string cuit)
{
    using var cmd = new SqlCommand(
        "SELECT TOP 1 Id FROM Companies WHERE NormalizedCuit = @c OR REPLACE(REPLACE(Cuit,'-',''),' ','') = @c ORDER BY Id", conn, tx);
    cmd.Parameters.AddWithValue("@c", cuit);
    var valor = cmd.ExecuteScalar();
    return valor is null || valor == DBNull.Value ? null : Convert.ToInt32(valor);
}

static (int Id, int Estudio)? FilaAbierta(SqlConnection conn, SqlTransaction tx, int empresaId)
{
    using var cmd = new SqlCommand(
        "SELECT TOP 1 Id, AccountingStudioId FROM CompanyStudioHistories WHERE CompanyId = @id AND [To] IS NULL", conn, tx);
    cmd.Parameters.AddWithValue("@id", empresaId);
    using var r = cmd.ExecuteReader();
    return r.Read() ? (r.GetInt32(0), r.GetInt32(1)) : null;
}

static int? StudioActual(SqlConnection conn, SqlTransaction tx, int empresaId)
{
    using var cmd = new SqlCommand("SELECT AccountingStudioId FROM Companies WHERE Id = @id", conn, tx);
    cmd.Parameters.AddWithValue("@id", empresaId);
    var valor = cmd.ExecuteScalar();
    return valor is null || valor == DBNull.Value ? null : Convert.ToInt32(valor);
}

static string? Recortar(string? valor, int largo)
{
    if (string.IsNullOrWhiteSpace(valor)) return null;
    var v = valor.Trim();
    return v.Length <= largo ? v : v[..largo];
}

// ===================================================================================== tipos

public class FilaCsv
{
    public string Cuit = "", Empresa = "", Activo = "", Responsable = "", Email = "";
    public string Telefono = "", Estudio = "", EstudioTelefono = "", EstudioDomicilio = "", EstudioLocalidad = "";
    public DateTime? Fecha;
}

public class EstudioCsv
{
    public string Clave = "", Nombre = "", Email = "", Responsable = "", Telefono = "";
    public string EstudioTelefono = "", Domicilio = "", Localidad = "";
    public int EmailsDistintos;
}

public class EstudioDb
{
    public int Id;
    public string Nombre = "", Responsable = "", Email = "";
    public bool Aprobado, Activo, Bloqueado;
}

public class Ciudad
{
    public int Id;
    public string Nombre = "";
}
