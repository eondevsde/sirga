#:package Microsoft.Extensions.Identity.Core@10.0.7
#:package Microsoft.Extensions.Identity.Stores@10.0.7
#:package Microsoft.Data.SqlClient@6.0.1

using System;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;

var connString = "Server=localhost;Database=SIRGA_DB;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true";
var newPassword = args.Length > 0 ? args[0] : "12345";
var emailFilter = args.Length > 1 ? args[1] : null;

using var conn = new SqlConnection(connString);
conn.Open();
Console.WriteLine($"Conectado a {conn.Database} - Password: '{newPassword}'");
Console.WriteLine();

var hasher = new PasswordHasher<IdentityUser>();

// 1) Asegurar roles
EnsureRole(conn, "Admin");
EnsureRole(conn, "Auditor");

// 2) Reset de passwords de usuarios existentes (todos o filtrado por email)
var sql = "SELECT Id, UserName, Email FROM AspNetUsers";
if (emailFilter != null)
    sql += " WHERE Email = @email";

using (var cmd = new SqlCommand(sql, conn))
{
    if (emailFilter != null)
        cmd.Parameters.AddWithValue("@email", emailFilter);

    using var reader = cmd.ExecuteReader();
    var users = new List<(string Id, string UserName, string Email)>();
    while (reader.Read())
        users.Add((reader.GetString(0), reader.GetString(1), reader.GetString(2)));
    reader.Close();

    foreach (var u in users)
        ResetPassword(conn, hasher, u.Id, u.UserName, u.Email, newPassword);
}

// 3) Crear Auditor si no existe
EnsureUser(conn, hasher, "auditor@sirga.com", newPassword, "Auditor");

Console.WriteLine();
Console.WriteLine("=== RESUMEN ===");
Console.WriteLine($"Password de todos los usuarios: {newPassword}");
Console.WriteLine("admin@sirga.com   -> Admin (ya existia, password reseteada)");
Console.WriteLine("auditor@sirga.com -> Auditor (creado)");
Console.WriteLine();

static void EnsureRole(SqlConnection conn, string roleName)
{
    var normalized = roleName.ToUpperInvariant();
    using var cmd = new SqlCommand(
        "SELECT COUNT(*) FROM AspNetRoles WHERE NormalizedName = @n", conn);
    cmd.Parameters.AddWithValue("@n", normalized);
    var count = (int)cmd.ExecuteScalar()!;
    if (count == 0)
    {
        var id = Guid.NewGuid().ToString();
        using var ins = new SqlCommand(
            "INSERT INTO AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp) VALUES (@id, @name, @n, @c)", conn);
        ins.Parameters.AddWithValue("@id", id);
        ins.Parameters.AddWithValue("@name", roleName);
        ins.Parameters.AddWithValue("@n", normalized);
        ins.Parameters.AddWithValue("@c", Guid.NewGuid().ToString());
        ins.ExecuteNonQuery();
        Console.WriteLine($"Rol creado: {roleName}");
    }
    else
    {
        Console.WriteLine($"Rol existente: {roleName}");
    }
}

static void ResetPassword(SqlConnection conn, PasswordHasher<IdentityUser> hasher,
    string id, string userName, string email, string password)
{
    var user = new IdentityUser { Id = id, UserName = userName };
    var hash = hasher.HashPassword(user, password);

    using var cmd = new SqlCommand(
        "UPDATE AspNetUsers SET PasswordHash = @h, SecurityStamp = @s, ConcurrencyStamp = @c WHERE Id = @id", conn);
    cmd.Parameters.AddWithValue("@h", hash);
    cmd.Parameters.AddWithValue("@s", Guid.NewGuid().ToString());
    cmd.Parameters.AddWithValue("@c", Guid.NewGuid().ToString());
    cmd.Parameters.AddWithValue("@id", id);
    cmd.ExecuteNonQuery();

    var ok = hasher.VerifyHashedPassword(user, hash, password);
    Console.WriteLine($"{(ok == PasswordVerificationResult.Success ? "Reset OK " : "FAIL    ")}: {email}");
}

static void EnsureUser(SqlConnection conn, PasswordHasher<IdentityUser> hasher,
    string email, string password, string role)
{
    using var check = new SqlCommand("SELECT COUNT(*) FROM AspNetUsers WHERE Email = @e", conn);
    check.Parameters.AddWithValue("@e", email);
    if ((int)check.ExecuteScalar()! > 0)
    {
        Console.WriteLine($"Usuario ya existe (no se crea): {email}");
        return;
    }

    var id = Guid.NewGuid().ToString();
    var user = new IdentityUser { Id = id, UserName = email, Email = email };
    var hash = hasher.HashPassword(user, password);

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
    roleCmd.Parameters.AddWithValue("@n", role.ToUpperInvariant());
    var roleId = (string)roleCmd.ExecuteScalar()!;

    using var ur = new SqlCommand(
        "INSERT INTO AspNetUserRoles (UserId, RoleId) VALUES (@uid, @rid)", conn);
    ur.Parameters.AddWithValue("@uid", id);
    ur.Parameters.AddWithValue("@rid", roleId);
    ur.ExecuteNonQuery();

    var ok = hasher.VerifyHashedPassword(user, hash, password);
    Console.WriteLine($"{(ok == PasswordVerificationResult.Success ? "Creado OK" : "Creado  ")}: {email} -> {password} (rol {role})");
}