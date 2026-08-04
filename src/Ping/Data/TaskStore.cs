using Ping.Models;

namespace Ping.Data;

/// <summary>
/// All task persistence. Timestamps are stored as local ISO-8601 ("o" round-trip on DateTime.Kind.Local
/// is not needed for display, so we keep it simple with invariant-culture "yyyy-MM-dd HH:mm:ss").
/// </summary>
public class TaskStore
{
    private const string Fmt = "yyyy-MM-dd HH:mm:ss";

    public PingTask Add(string text, DateTime scheduledAt)
    {
        var task = new PingTask { Text = text.Trim(), ScheduledAt = scheduledAt, CreatedAt = DateTime.Now };
        using var db = Database.Open();
        using var cmd = db.CreateCommand();
        cmd.CommandText = """
            INSERT INTO tasks (id, text, scheduled_at, created_at, printed_at, cancelled, source)
            VALUES ($id, $text, $scheduled, $created, NULL, 0, 'desktop')
            """;
        cmd.Parameters.AddWithValue("$id", task.Id);
        cmd.Parameters.AddWithValue("$text", task.Text);
        cmd.Parameters.AddWithValue("$scheduled", task.ScheduledAt.ToString(Fmt));
        cmd.Parameters.AddWithValue("$created", task.CreatedAt.ToString(Fmt));
        cmd.ExecuteNonQuery();
        return task;
    }

    /// <summary>Pending tasks whose scheduled time has arrived (any day).</summary>
    public List<PingTask> GetDue(DateTime now)
    {
        return Query("""
            SELECT * FROM tasks
            WHERE cancelled = 0 AND printed_at IS NULL AND scheduled_at <= $now
            ORDER BY scheduled_at
            """, c => c.Parameters.AddWithValue("$now", now.ToString(Fmt)));
    }

    /// <summary>Next pending tasks, soonest first. Used by the Upcoming view.</summary>
    public List<PingTask> GetUpcoming(int count)
    {
        return Query("""
            SELECT * FROM tasks
            WHERE cancelled = 0 AND printed_at IS NULL
            ORDER BY scheduled_at
            LIMIT $count
            """, c => c.Parameters.AddWithValue("$count", count));
    }

    /// <summary>Pending tasks still to print today (scheduled today, not yet printed).</summary>
    public List<PingTask> GetRemainingToday()
    {
        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);
        return Query("""
            SELECT * FROM tasks
            WHERE cancelled = 0 AND printed_at IS NULL
              AND scheduled_at >= $start AND scheduled_at < $end
            ORDER BY scheduled_at
            """,
            c =>
            {
                c.Parameters.AddWithValue("$start", today.ToString(Fmt));
                c.Parameters.AddWithValue("$end", tomorrow.ToString(Fmt));
            });
    }

    public void MarkPrinted(string taskId)
    {
        using var db = Database.Open();
        using var cmd = db.CreateCommand();
        cmd.CommandText = "UPDATE tasks SET printed_at = $now WHERE id = $id";
        cmd.Parameters.AddWithValue("$now", DateTime.Now.ToString(Fmt));
        cmd.Parameters.AddWithValue("$id", taskId);
        cmd.ExecuteNonQuery();
    }

    /// <summary>Quietly retire tasks from previous days that were never printed.</summary>
    public int ExpireBeforeToday()
    {
        using var db = Database.Open();
        using var cmd = db.CreateCommand();
        cmd.CommandText = """
            UPDATE tasks SET cancelled = 1
            WHERE cancelled = 0 AND printed_at IS NULL AND scheduled_at < $today
            """;
        cmd.Parameters.AddWithValue("$today", DateTime.Today.ToString(Fmt));
        return cmd.ExecuteNonQuery();
    }

    public void LogPrint(string? taskId, string kind, string result, string? detail = null)
    {
        using var db = Database.Open();
        using var cmd = db.CreateCommand();
        cmd.CommandText = """
            INSERT INTO print_log (id, task_id, kind, printed_at, result, detail)
            VALUES ($id, $taskId, $kind, $now, $result, $detail)
            """;
        cmd.Parameters.AddWithValue("$id", Guid.NewGuid().ToString("N"));
        cmd.Parameters.AddWithValue("$taskId", (object?)taskId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$kind", kind);
        cmd.Parameters.AddWithValue("$now", DateTime.Now.ToString(Fmt));
        cmd.Parameters.AddWithValue("$result", result);
        cmd.Parameters.AddWithValue("$detail", (object?)detail ?? DBNull.Value);
        cmd.ExecuteNonQuery();
    }

    private static List<PingTask> Query(string sql, Action<Microsoft.Data.Sqlite.SqliteCommand> bind)
    {
        using var db = Database.Open();
        using var cmd = db.CreateCommand();
        cmd.CommandText = sql;
        bind(cmd);
        var list = new List<PingTask>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new PingTask
            {
                Id = reader.GetString(0),
                Text = reader.GetString(1),
                ScheduledAt = DateTime.ParseExact(reader.GetString(2), Fmt, null),
                CreatedAt = DateTime.ParseExact(reader.GetString(3), Fmt, null),
                PrintedAt = reader.IsDBNull(4) ? null : DateTime.ParseExact(reader.GetString(4), Fmt, null),
                Cancelled = reader.GetInt32(5) == 1,
                Source = reader.GetString(6)
            });
        }
        return list;
    }
}
