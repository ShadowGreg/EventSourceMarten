using System.Text.RegularExpressions;
using Serilog.Events;
using Serilog.Formatting;

namespace EventSourceMarten.Exception;
public class AnsiSqlFormatter: ITextFormatter
{
    private const string Reset = "\u001b[0m";
    private const string Cyan = "\u001b[36m";
    private const string Yellow = "\u001b[33m";
    private const string Gray = "\u001b[90m";
    private const string Bold = "\u001b[1m";

    private static readonly Regex Keywords = new(
        @"\b(SELECT|FROM|WHERE|AND|OR|INNER|LEFT|RIGHT|FULL|JOIN|ON|GROUP BY|ORDER BY|HAVING|LIMIT|OFFSET|INSERT|INTO|VALUES|UPDATE|SET|DELETE|RETURNING)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public void Format(LogEvent logEvent, TextWriter output) {
        // Берём отформатированное сообщение (у нас там уже NewLine + Sql)
        var msg = logEvent.RenderMessage();

        // Подсветим SQL-ключевые слова
        msg = Keywords.Replace(msg, m => $"{Bold}{Cyan}{m.Value.ToUpperInvariant()}{Reset}");

        // Немного отделим SQL блок визуально
        var ts = logEvent.Timestamp.ToString("HH:mm:ss");
        var lvl = logEvent.Level.ToString().ToUpperInvariant().PadRight(3);

        output.WriteLine($"{Gray}[{ts} {lvl}]{Reset} {Yellow}Marten SQL{Reset}");
        output.WriteLine(msg);
        output.WriteLine(); // пустая строка после запроса
    }
}