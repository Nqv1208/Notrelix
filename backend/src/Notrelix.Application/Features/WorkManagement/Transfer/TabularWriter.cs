using System.Globalization;
using System.IO.Compression;
using System.Security;
using System.Text;

namespace Notrelix.Application.Features.WorkManagement.Transfer;

/// <summary>
/// Writes an export as CSV (UTF-8 with a BOM, so Excel reads Vietnamese) or XLSX (inline strings, typed numbers and
/// dates). Text a person typed is never written as something a spreadsheet would run: a text cell that starts with
/// <c>=</c>, <c>+</c>, <c>-</c>, <c>@</c>, tab or CR gets a leading apostrophe. Typed numbers, dates and booleans are
/// not text and stay as they are.
/// </summary>
public static class TabularWriter
{
    public const string Csv = "csv";
    public const string Xlsx = "xlsx";

    public static string Guard(string text) =>
        text.Length > 0 && text[0] is '=' or '+' or '-' or '@' or '\t' or '\r' ? "'" + text : text;

    public static void Write(string format, IReadOnlyList<string> headers, IEnumerable<object?[]> rows, Stream output)
    {
        if (format == Xlsx) WriteXlsx(headers, rows, output);
        else WriteCsv(headers, rows, output);
    }

    private static void WriteCsv(IReadOnlyList<string> headers, IEnumerable<object?[]> rows, Stream output)
    {
        using var writer = new StreamWriter(output, new UTF8Encoding(true), 81920, leaveOpen: true) { NewLine = "\r\n" };
        writer.WriteLine(string.Join(',', headers.Select(h => CsvText(Guard(h)))));
        foreach (var row in rows)
            writer.WriteLine(string.Join(',', row.Select(CsvCell)));
    }

    private static string CsvCell(object? value) => value switch
    {
        null => "",
        string s => CsvText(Guard(s)),
        _ => CsvText(Format(value)),
    };

    private static string CsvText(string s) =>
        s.IndexOfAny([',', '"', '\r', '\n', ';']) >= 0 ? "\"" + s.Replace("\"", "\"\"") + "\"" : s;

    private static string Format(object value) => value switch
    {
        bool b => b ? "true" : "false",
        DateTimeOffset d => d.TimeOfDay == TimeSpan.Zero ? d.UtcDateTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : d.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture),
        decimal m => m.ToString(CultureInfo.InvariantCulture),
        IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString() ?? "",
    };

    private static void WriteXlsx(IReadOnlyList<string> headers, IEnumerable<object?[]> rows, Stream output)
    {
        using var zip = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true);
        Add(zip, "[Content_Types].xml",
            "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"><Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/><Default Extension=\"xml\" ContentType=\"application/xml\"/><Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/><Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/><Override PartName=\"/xl/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml\"/></Types>");
        Add(zip, "_rels/.rels",
            "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/></Relationships>");
        Add(zip, "xl/workbook.xml",
            "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheets><sheet name=\"Items\" sheetId=\"1\" r:id=\"rId1\"/></sheets></workbook>");
        Add(zip, "xl/_rels/workbook.xml.rels",
            "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/><Relationship Id=\"rId2\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles\" Target=\"styles.xml\"/></Relationships>");
        Add(zip, "xl/styles.xml",
            "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><styleSheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><numFmts count=\"1\"><numFmt numFmtId=\"164\" formatCode=\"yyyy\\-mm\\-dd\"/></numFmts><fonts count=\"2\"><font><sz val=\"11\"/><name val=\"Calibri\"/></font><font><b/><sz val=\"11\"/><name val=\"Calibri\"/></font></fonts><fills count=\"2\"><fill><patternFill patternType=\"none\"/></fill><fill><patternFill patternType=\"gray125\"/></fill></fills><borders count=\"1\"><border><left/><right/><top/><bottom/><diagonal/></border></borders><cellStyleXfs count=\"1\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/></cellStyleXfs><cellXfs count=\"3\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\"/><xf numFmtId=\"164\" fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\" applyNumberFormat=\"1\"/><xf numFmtId=\"0\" fontId=\"1\" fillId=\"0\" borderId=\"0\" xfId=\"0\" applyFont=\"1\"/></cellXfs></styleSheet>");

        var entry = zip.CreateEntry("xl/worksheets/sheet1.xml", CompressionLevel.Fastest);
        using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false));
        writer.Write("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetData>");
        WriteRow(writer, 1, headers.Select(h => (object?)h), header: true);
        var n = 1;
        foreach (var row in rows) WriteRow(writer, ++n, row, header: false);
        writer.Write("</sheetData></worksheet>");
    }

    private static void WriteRow(StreamWriter w, int number, IEnumerable<object?> cells, bool header)
    {
        w.Write($"<row r=\"{number}\">");
        var col = 0;
        foreach (var cell in cells)
        {
            var r = ColumnName(col++) + number.ToString(CultureInfo.InvariantCulture);
            switch (cell)
            {
                case null: break;
                case string s:
                    w.Write($"<c r=\"{r}\" t=\"inlineStr\"{(header ? " s=\"2\"" : "")}><is><t xml:space=\"preserve\">{Escape(Guard(s))}</t></is></c>");
                    break;
                case bool b: w.Write($"<c r=\"{r}\" t=\"b\"><v>{(b ? 1 : 0)}</v></c>"); break;
                case DateTimeOffset d:
                    w.Write($"<c r=\"{r}\" s=\"1\"><v>{d.UtcDateTime.ToOADate().ToString("R", CultureInfo.InvariantCulture)}</v></c>");
                    break;
                default: w.Write($"<c r=\"{r}\"><v>{Format(cell)}</v></c>"); break;
            }
        }

        w.Write("</row>");
    }

    private static string ColumnName(int index)
    {
        var s = "";
        for (var n = index + 1; n > 0; n = (n - 1) / 26) s = (char)('A' + (n - 1) % 26) + s;
        return s;
    }

    private static string Escape(string s)
    {
        var sb = new StringBuilder(SecurityElement.Escape(s) ?? "");
        // XML 1.0 cannot carry most control characters; drop them rather than write a broken file.
        for (var i = sb.Length - 1; i >= 0; i--) if (sb[i] < ' ' && sb[i] is not ('\t' or '\n' or '\r')) sb.Remove(i, 1);
        return sb.ToString();
    }

    private static void Add(ZipArchive zip, string name, string xml)
    {
        using var s = zip.CreateEntry(name, CompressionLevel.Fastest).Open();
        s.Write(new UTF8Encoding(false).GetBytes(xml));
    }
}
