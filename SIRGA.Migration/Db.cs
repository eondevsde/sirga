using System.Data;
using System.Text;
using Microsoft.Data.SqlClient;

namespace SIRGA.Migration;

public static class Db
{
    // Las cadenas se pueden cambiar por línea de comandos (--origen / --destino), así el mismo ETL
    // sirve para la copia de producción, para la demo y para producción sin recompilar.
    public static string LegacyCs { get; set; } =
        "Server=localhost;Database=Sindicato;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true";

    public static string TargetCs { get; set; } =
        "Server=localhost;Database=SIRGA_DB;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true";

    /// <summary>Si es true, no escribe nada: sólo cuenta lo que haría (para revisar antes de aplicar).</summary>
    public static bool Simulacro { get; set; }

    /// <summary>Arma una cadena para una base local con autenticación de Windows.</summary>
    public static string Local(string baseDatos) =>
        $"Server=localhost;Database={baseDatos};Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true";

    public static SqlConnection Open(string cs)
    {
        var c = new SqlConnection(cs);
        c.Open();
        return c;
    }

    /// <summary>Normaliza un CUIT a 11 dígitos (completa con ceros a la izquierda).</summary>
    public static string PadCuit(string? v)
    {
        if (string.IsNullOrWhiteSpace(v)) return "";
        var d = new string(v.Trim().Where(char.IsDigit).ToArray());
        if (d.Length == 0) return "";
        return d.Length >= 11 ? d : d.PadLeft(11, '0');
    }

    public static decimal Dec(object? v) => v == DBNull.Value || v == null ? 0m : Convert.ToDecimal(v);
    public static int Int(object? v) => v == DBNull.Value || v == null ? 0 : Convert.ToInt32(v);
    public static string Str(object? v) => v == DBNull.Value || v == null ? "" : Convert.ToString(v)!.Trim();
    public static DateTime? Dt(object? v) => v == DBNull.Value || v == null ? null : Convert.ToDateTime(v);
    public static bool Bit(object? v) => v != DBNull.Value && v != null && Convert.ToInt32(v) != 0;

    public static void Run(string cs, string sql, int timeoutSeconds = 300)
    {
        using var c = Open(cs);
        using var cmd = new SqlCommand(sql, c) { CommandTimeout = timeoutSeconds };
        cmd.ExecuteNonQuery();
    }

    public static int ScalarInt(string cs, string sql)
    {
        using var c = Open(cs);
        using var cmd = new SqlCommand(sql, c);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    public static void BulkInsert(SqlConnection target, string table, DataTable dt)
    {
        using var bc = new SqlBulkCopy(target);
        bc.DestinationTableName = table;
        bc.BulkCopyTimeout = 0;
        foreach (DataColumn col in dt.Columns) bc.ColumnMappings.Add(col.ColumnName, col.ColumnName);
        bc.WriteToServer(dt);
    }
}

public static class Log
{
    private static readonly object _lock = new();
    private static readonly string _path = Path.Combine(AppContext.BaseDirectory, "migracion.log");

    public static void Write(string msg)
    {
        lock (_lock)
        {
            var line = $"[{DateTime.Now:HH:mm:ss}] {msg}";
            Console.WriteLine(line);
            File.AppendAllText(_path, line + Environment.NewLine);
        }
    }

    public static void Section(string msg)
    {
        lock (_lock)
        {
            var line = Environment.NewLine + "=== " + msg + " ===";
            Console.WriteLine(line);
            File.AppendAllText(_path, line + Environment.NewLine);
        }
    }
}