using System.Data;
using System.Text;

namespace ADAccessReporter;

public static class CsvExporter
{
    public static void WriteDataTable(DataTable table, string path)
    {
        using var writer = new StreamWriter(path, false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));

        var headers = table.Columns.Cast<DataColumn>().Select(column => Escape(column.ColumnName));
        writer.WriteLine(string.Join(",", headers));

        foreach (DataRow row in table.Rows)
        {
            var fields = table.Columns.Cast<DataColumn>().Select(column => Escape(row[column]));
            writer.WriteLine(string.Join(",", fields));
        }
    }

    private static string Escape(object? value)
    {
        var text = value?.ToString() ?? string.Empty;
        if (text.Contains('"'))
        {
            text = text.Replace("\"", "\"\"");
        }

        if (text.IndexOfAny(new[] { ',', '"', '\r', '\n' }) >= 0)
        {
            text = $"\"{text}\"";
        }

        return text;
    }
}
