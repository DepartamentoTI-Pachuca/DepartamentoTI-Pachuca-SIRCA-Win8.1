using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using BCrypt.Net;
using BC = BCrypt.Net.BCrypt;

public class UserRepository
{
    private const string DbPath = "users.db";

    public UserRepository()
    {
        using var conn = new SqliteConnection($"Data Source={DbPath}");
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS users (
                id       INTEGER PRIMARY KEY AUTOINCREMENT,
                username TEXT    NOT NULL UNIQUE,
                hash     TEXT    NOT NULL,
                role     TEXT    NOT NULL DEFAULT 'user',
                created  TEXT    NOT NULL
            )";
        cmd.ExecuteNonQuery();
    }

    public bool Register(string username, string password, string role = "user")
    {
        using var conn = new SqliteConnection($"Data Source={DbPath}");
        conn.Open();
        var hash = BCrypt.Net.BCrypt.HashPassword(password);
        try
        {
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO users (username, hash, role, created)
                VALUES ($u, $h, $r, $d)";
            cmd.Parameters.AddWithValue("$u", username);
            cmd.Parameters.AddWithValue("$h", hash);
            cmd.Parameters.AddWithValue("$r", role);
            cmd.Parameters.AddWithValue("$d", DateTime.UtcNow.ToString("o"));
            cmd.ExecuteNonQuery();
            return true;
        }
        catch (SqliteException) { return false; } // username duplicado
    }

    public bool Login(string username, string password)
    {
        using var conn = new SqliteConnection($"Data Source={DbPath}");
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT hash FROM users WHERE username = $u";
        cmd.Parameters.AddWithValue("$u", username);
        var hash = cmd.ExecuteScalar() as string;
        if (hash == null) return false;
        return BCrypt.Net.BCrypt.Verify(password, hash);
    }

    public void DeleteUser(string username)
    {
        using var conn = new SqliteConnection($"Data Source={DbPath}");
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM users WHERE username = $u";
        cmd.Parameters.AddWithValue("$u", username);
        cmd.ExecuteNonQuery();
    }
}