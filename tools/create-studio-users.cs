#:package Microsoft.Extensions.Identity.Core@10.0.7
#:package Microsoft.Extensions.Identity.Stores@10.0.7
#:package Microsoft.Data.SqlClient@6.0.1

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;

var connString = "Server=localhost;Database=SIRGA_DB;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true";
var newPassword = args.Length > 0 ? args[0] : "12345";
var csvPath = args.Length > 1 ? args[1] : @"C:\Proyectos\SIRGA\tools\estudios_usuarios.csv";

using var conn = new SqlConnection(connString);
conn.Open();
Console.WriteLine($"Conectado a {conn.Database} - Password: '{newPassword}'");
Console.WriteLine();

var hasher = new PasswordHasher<IdentityUser>();

EnsureRole(conn, "EstudioContable");

// 1) Estudios con email valido
var studios = new List<(string Email, string Name, string Phone)>();
using (var cmd = new SqlCommand(
    "SELECT Email, Name, ContactPhone FROM AccountingStudios WHERE Email IS NOT NULL AND LTRIM(RTRIM(Email)) <> ''", conn))
using (var reader = cmd.ExecuteReader())
{
    while (reader.Read())
        studios.Add((reader.GetString(0).Trim(), reader.IsDBNull(1) ? "" : reader.GetString(1), reader.IsDBNull(2) ? "" : reader.GetString(2)));
}

// 2) Emails ya existentes como usuarios
var existing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
using (var cmd = new SqlCommand("SELECT Email FROM AspNetUsers WHERE Email IS NOT NULL", conn))
using (var reader = cmd.ExecuteReader())
{
    while (reader.Read())
        existing.Add(reader.GetString(0).Trim());
}

var created = 0;
var skippedExisting = 0;
var errors = new List<string>();
var csvLines = new List<string> { "Email;Password;Nombre;Telefono" };

foreach (var (email, name, phone) in studios)
{
    if (existing.Contains(email))
    {
        skippedExisting++;
        csvLines.Add($"{email};{newPassword};{name};{phone}");
        continue;
    }

    var id = Guid.NewGuid().ToString();
    var user = new IdentityUser { Id = id, UserName = email, Email = email };
    var hash = hasher.HashPassword(user, newPassword);

    try
    {
        using var ins = new SqlCommand(@"
            INSERT INTO AspNetUsers (Id, UserName, NormalizedUserName, Email, NormalizedEmail,
                EmailConfirmed, PasswordHash, SecurityStamp, ConcurrencyStamp,
                PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnabled, AccessFailedCount)
            VALUES (@id, @u, @nu, @e, @ne, 1, @h, @s, @c, 0, 0, 1, 0)", conn);
        ins.Parameters.AddWithValue("@id", id);
        ins.Parameters.AddWithValue("@u", email);
        ins.Parameters.AddWithValue("@nu", email.ToUpperInvariant());
        ins.Parameters.AddWithValue("@e", email);
        ins.Parameters.AddWithValue("@ne", email.ToUpperInvariant());
        ins.Parameters.AddWithValue("@h", hash);
        ins.Parameters.AddWithValue("@s", Guid.NewGuid().ToString());
        ins.Parameters.AddWithValue("@c", Guid.NewGuid().ToString());
        ins.ExecuteNonQuery();

        using var roleCmd = new SqlCommand(
            "SELECT Id FROM AspNetRoles WHERE NormalizedName = @n", conn);
        roleCmd.Parameters.AddWithValue("@n", "ESTUDIOCONTABLE");
        var roleId = (string)roleCmd.ExecuteScalar()!;

        using var ur = new SqlCommand(
            "INSERT INTO AspNetUserRoles (UserId, RoleId) VALUES (@uid, @rid)", conn);
        ur.Parameters.AddWithValue("@uid", id);
        ur.Parameters.AddWithValue("@rid", roleId);
        ur.ExecuteNonQuery();

        created++;
        csvLines.Add($"{email};{newPassword};{name};{phone}");
        existing.Add(email);
    }
    catch (Exception ex)
    {
        errors.Add($"{email}: {ex.Message}");
    }
}

var csvContent = string.Join("\r\n", csvLines) + "\r\n";
File.WriteAllText(csvPath, csvContent, new System.Text.UTF8Encoding(true));

Console.WriteLine("=== RESUMEN ===");
Console.WriteLine($"Estudios con email: {studios.Count}");
Console.WriteLine($"Creados          : {created}");
Console.WriteLine($"Ya existentes    : {skippedExisting}");
Console.WriteLine($"Errores          : {errors.Count}");
Console.WriteLine();
Console.WriteLine($"CSV generado: {csvPath}");
foreach (var e in errors)
    Console.WriteLine($"  ERROR {e}");

static void EnsureRole(SqlConnection conn, string roleName)
{
    var normalized = roleName.ToUpperInvariant();
    using var cmd = new SqlCommand(
        "SELECT COUNT(*) FROM AspNetRoles WHERE NormalizedName = @n", conn);
    cmd.Parameters.AddWithValue("@n", normalized);
    var count = (int)cmd.ExecuteScalar()!;
    if (count > 0)
    {
        Console.WriteLine($"Rol existente: {roleName}");
        return;
    }

    using var ins = new SqlCommand(
        "INSERT INTO AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp) VALUES (@id, @name, @n, @c)", conn);
    ins.Parameters.AddWithValue("@id", Guid.NewGuid().ToString());
    ins.Parameters.AddWithValue("@name", roleName);
    ins.Parameters.AddWithValue("@n", normalized);
    ins.Parameters.AddWithValue("@c", Guid.NewGuid().ToString());
    ins.ExecuteNonQuery();
    Console.WriteLine($"Rol creado: {roleName}");
}
