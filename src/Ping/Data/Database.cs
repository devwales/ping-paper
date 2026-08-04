using Microsoft.Data.Sqlite;

namespace Ping.Data;

public static class Database
{
    private static string _path = "";

    public static void Initialize(string path)
    {
        _path = path;
        using var db = Open();
        using var cmd = db.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS tasks (
                id           TEXT PRIMARY KEY,
                text         TEXT NOT NULL,
                scheduled_at TEXT NOT NULL,
                created_at   TEXT NOT NULL,
                printed_at   TEXT NULL,
                cancelled    INTEGER NOT NULL DEFAULT 0,
                source       TEXT NOT NULL DEFAULT 'desktop'
            );
            CREATE INDEX IF NOT EXISTS idx_tasks_due ON tasks (cancelled, printed_at, scheduled_at);

            CREATE TABLE IF NOT EXISTS settings (
                key   TEXT PRIMARY KEY,
                value TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS print_log (
                id         TEXT PRIMARY KEY,
                task_id    TEXT NULL,
                kind       TEXT NOT NULL,
                printed_at TEXT NOT NULL,
                result     TEXT NOT NULL,
                detail     TEXT NULL
            );
            """;
        cmd.ExecuteNonQuery();
    }

    public static SqliteConnection Open()
    {
        var db = new SqliteConnection($"Data Source={_path}");
        db.Open();
        return db;
    }
}
