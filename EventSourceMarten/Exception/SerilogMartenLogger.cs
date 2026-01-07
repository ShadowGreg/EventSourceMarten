using Marten;
using Marten.Services;
using Npgsql;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Serilog;
using SerilogLogger = Serilog.ILogger;

public sealed class SerilogMartenLogger : IMartenLogger
{
    private readonly SerilogLogger _log;

    public SerilogMartenLogger(SerilogLogger log)
    {
        _log = log.ForContext<SerilogMartenLogger>();
    }

    public IMartenSessionLogger StartSession(IQuerySession session)
        => new SessionLogger(_log);

    public void SchemaChange(string sql)
        => _log.Information("Marten schema change (DDL):{NewLine}{Sql}", Environment.NewLine, PrettySql(sql));

    private sealed class SessionLogger : IMartenSessionLogger
    {
        private readonly SerilogLogger _log;
        private readonly Stopwatch _sw = new();

        public SessionLogger(SerilogLogger log)
        {
            _log = log.ForContext("Component", "Marten");
        }

        public void RecordSavedChanges(IDocumentSession session, IChangeSet commit)
        {
            _log.Information(
                "Marten SaveChanges: inserts={Inserted}, updates={Updated}, deletes={Deleted}",
                commit.Inserted.Count(), commit.Updated.Count(), commit.Deleted.Count()
            );
        }

        public void OnBeforeExecute(NpgsqlCommand command)
        {
            _sw.Restart();

            _log
                .ForContext("IsSql", true)
                .ForContext("Db", "postgres")
                .Debug("SQL start:{NewLine}{Sql}", Environment.NewLine, PrettySql(command.CommandText));
        }

        public void OnBeforeExecute(NpgsqlBatch batch)
        {
            _sw.Restart();

            _log
                .ForContext("IsSql", true)
                .ForContext("Db", "postgres")
                .Debug("SQL batch start: {Count} commands", batch.BatchCommands.Count);
        }

        public void LogSuccess(NpgsqlCommand command)
        {
            _sw.Stop();

            _log
                .ForContext("IsSql", true)
                .ForContext("Db", "postgres")
                .ForContext("ElapsedMs", _sw.ElapsedMilliseconds)
                .Debug("SQL ok ({ElapsedMs} ms):{NewLine}{Sql}",
                    _sw.ElapsedMilliseconds,
                    Environment.NewLine,
                    PrettySql(command.CommandText));
        }

        public void LogSuccess(NpgsqlBatch batch)
        {
            _sw.Stop();

            _log
                .ForContext("IsSql", true)
                .ForContext("Db", "postgres")
                .ForContext("ElapsedMs", _sw.ElapsedMilliseconds)
                .Debug("SQL batch ok ({ElapsedMs} ms): {Count} commands",
                    _sw.ElapsedMilliseconds,
                    batch.BatchCommands.Count);
        }

        public void LogFailure(NpgsqlCommand command, Exception ex)
        {
            _sw.Stop();

            _log
                .ForContext("IsSql", true)
                .ForContext("Db", "postgres")
                .ForContext("ElapsedMs", _sw.ElapsedMilliseconds)
                .Error(ex, "SQL fail ({ElapsedMs} ms):{NewLine}{Sql}",
                    _sw.ElapsedMilliseconds,
                    Environment.NewLine,
                    PrettySql(command.CommandText));
        }

        public void LogFailure(NpgsqlBatch batch, Exception ex)
        {
            _sw.Stop();

            _log
                .ForContext("IsSql", true)
                .ForContext("Db", "postgres")
                .ForContext("ElapsedMs", _sw.ElapsedMilliseconds)
                .Error(ex, "SQL batch fail ({ElapsedMs} ms): {Count} commands",
                    _sw.ElapsedMilliseconds,
                    batch.BatchCommands.Count);
        }

        public void LogFailure(Exception ex, string message)
        {
            _log.Error(ex, "Marten failure: {Message}", message);
        }

        public static string PrettySql(string sql)
        {
            if (string.IsNullOrWhiteSpace(sql)) return sql;

            // 1) Если это строка с буквальными "\n" (как на твоём скрине), превращаем в реальные переносы
            sql = sql.Replace("\\r\\n", "\n").Replace("\\n", "\n").Replace("\\t", "\t");

            // 2) Если всё равно одна строка — делаем базовый перенос по ключевым словам
            if (!sql.Contains('\n'))
            {
                sql = Regex.Replace(sql, @"\s+(FROM)\s+", "\nFROM ", RegexOptions.IgnoreCase);
                sql = Regex.Replace(sql, @"\s+(WHERE)\s+", "\nWHERE ", RegexOptions.IgnoreCase);
                sql = Regex.Replace(sql, @"\s+(INNER\s+JOIN)\s+", "\nINNER JOIN ", RegexOptions.IgnoreCase);
                sql = Regex.Replace(sql, @"\s+(LEFT\s+JOIN)\s+", "\nLEFT JOIN ", RegexOptions.IgnoreCase);
                sql = Regex.Replace(sql, @"\s+(RIGHT\s+JOIN)\s+", "\nRIGHT JOIN ", RegexOptions.IgnoreCase);
                sql = Regex.Replace(sql, @"\s+(FULL\s+JOIN)\s+", "\nFULL JOIN ", RegexOptions.IgnoreCase);
                sql = Regex.Replace(sql, @"\s+(GROUP\s+BY)\s+", "\nGROUP BY ", RegexOptions.IgnoreCase);
                sql = Regex.Replace(sql, @"\s+(ORDER\s+BY)\s+", "\nORDER BY ", RegexOptions.IgnoreCase);
                sql = Regex.Replace(sql, @"\s+(HAVING)\s+", "\nHAVING ", RegexOptions.IgnoreCase);
                sql = Regex.Replace(sql, @"\s+(LIMIT)\s+", "\nLIMIT ", RegexOptions.IgnoreCase);
                sql = Regex.Replace(sql, @"\s+(OFFSET)\s+", "\nOFFSET ", RegexOptions.IgnoreCase);
            }

            return sql.Trim();
        }
    }

    private static string PrettySql(string sql)
        => SessionLogger.PrettySql(sql);
}
