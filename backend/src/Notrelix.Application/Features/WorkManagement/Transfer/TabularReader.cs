using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace Notrelix.Application.Features.WorkManagement.Transfer;

/// <summary>One data row of an uploaded file; <paramref name="Number"/> is its 1-based place after the header row.</summary>
public sealed record TabularRow(int Number, string?[] Cells);

/// <param name="TotalRows">Data rows in the file; <paramref name="Rows"/> keeps only the first <see cref="TabularReader.MaxRows"/>.</param>
public sealed record TabularData(string Format, IReadOnlyList<string> Headers, IReadOnlyList<TabularRow> Rows, int TotalRows);

/// <summary>A file this import refuses; <see cref="Code"/> is the stable reason the client words.</summary>
public sealed class TabularFileException(string code) : Exception(code)
{
    public string Code { get; } = code;
}

/// <summary>
/// Reads a CSV or XLSX upload on the server. The format comes from the bytes (a zip header is XLSX, otherwise the
/// file must be UTF-8 text), never from the declared type or the name. An XLSX with macros, embedded objects or
/// links to other workbooks is refused; only cell text is read, formulas are never evaluated.
/// </summary>
public static class TabularReader
{
    public const int MaxRows = 5_000;
    public const int MaxColumns = 100;
    public const long MaxFileBytes = 10 * 1024 * 1024;
    private const int MaxCountedRows = 200_000;
    private const long MaxUncompressedBytes = 200L * 1024 * 1024;
    private const int MaxCellLength = 32_767;

    public const string UnsupportedType = "unsupported-type";
    public const string NoRows = "no-rows";
    public const string TooManyColumns = "too-many-columns";
    public const string Unsafe = "unsafe-content";
    public const string Malformed = "malformed";

    public static TabularData Read(byte[] bytes)
    {
        if (bytes.Length == 0) throw new TabularFileException(NoRows);
        var data = IsZip(bytes) ? ReadXlsx(bytes) : ReadCsv(bytes);
        if (data.Headers.Count > MaxColumns) throw new TabularFileException(TooManyColumns);
        if (data.TotalRows == 0) throw new TabularFileException(NoRows);
        return data;
    }

    private static bool IsZip(byte[] b) => b.Length > 4 && b[0] == 0x50 && b[1] == 0x4B && b[2] == 0x03 && b[3] == 0x04;

    // ----- CSV -----
    private static TabularData ReadCsv(byte[] bytes)
    {
        // Text only: a NUL byte or invalid UTF-8 means a binary file (PDF, image, legacy XLS).
        if (Array.IndexOf(bytes, (byte)0) >= 0 || LooksLikeOtherFile(bytes)) throw new TabularFileException(UnsupportedType);
        string text;
        try { text = new UTF8Encoding(false, true).GetString(bytes); }
        catch (DecoderFallbackException) { throw new TabularFileException(UnsupportedType); }
        if (text.Length > 0 && text[0] == '﻿') text = text[1..];

        var delimiter = DetectDelimiter(text);
        var records = new List<string?[]>();
        var total = 0;
        string?[]? header = null;
        foreach (var record in CsvRecords(text, delimiter))
        {
            if (header is null) { header = record; continue; }
            total++;
            if (total > MaxCountedRows) break;
            if (records.Count < MaxRows) records.Add(record);
        }

        if (header is null) throw new TabularFileException(NoRows);
        return Build("csv", header, records.Select((r, i) => new TabularRow(i + 1, r)).ToList(), total);
    }

    private static char DetectDelimiter(string text)
    {
        var line = text.Split('\n', 2)[0];
        var best = new[] { ',', ';', '\t' }.MaxBy(d => line.Count(c => c == d));
        return line.Contains(best) ? best : ',';
    }

    private static IEnumerable<string?[]> CsvRecords(string text, char delimiter)
    {
        var cells = new List<string?>();
        var cell = new StringBuilder();
        var quoted = false;
        var any = false;
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (quoted)
            {
                if (c == '"')
                {
                    if (i + 1 < text.Length && text[i + 1] == '"') { cell.Append('"'); i++; }
                    else quoted = false;
                }
                else cell.Append(c);
                continue;
            }

            if (c == '"' && cell.Length == 0) { quoted = true; any = true; }
            else if (c == delimiter) { cells.Add(Cap(cell)); cell.Clear(); any = true; }
            else if (c is '\r' or '\n')
            {
                if (c == '\r' && i + 1 < text.Length && text[i + 1] == '\n') i++;
                if (any || cell.Length > 0) { cells.Add(Cap(cell)); yield return cells.ToArray(); }
                cells.Clear(); cell.Clear(); any = false;
            }
            else { cell.Append(c); any = true; }
        }

        if (quoted) throw new TabularFileException(Malformed);
        if (any || cell.Length > 0) { cells.Add(Cap(cell)); yield return cells.ToArray(); }
    }

    private static readonly byte[][] OtherSignatures =
    [
        "%PDF-"u8.ToArray(), "MZ"u8.ToArray(), [0x7F, (byte)'E', (byte)'L', (byte)'F'], [0xD0, 0xCF, 0x11, 0xE0], [0x89, (byte)'P', (byte)'N', (byte)'G'],
        "GIF8"u8.ToArray(), [0xFF, 0xD8, 0xFF], "{\\rtf"u8.ToArray(), "<!DOCTYPE html"u8.ToArray(), "<html"u8.ToArray(),
    ];

    /// <summary>Text that is really another kind of file (a PDF with only text in its first bytes, a web page).</summary>
    private static bool LooksLikeOtherFile(byte[] bytes) => OtherSignatures.Any(sig => bytes.AsSpan().StartsWith(sig));

    private static string? Cap(StringBuilder cell)
    {
        if (cell.Length > MaxCellLength) throw new TabularFileException(Malformed);
        return cell.Length == 0 ? null : cell.ToString();
    }

    // ----- XLSX -----
    private static TabularData ReadXlsx(byte[] bytes)
    {
        var budget = new Budget();
        try
        {
            using var zip = new ZipArchive(new MemoryStream(bytes), ZipArchiveMode.Read);
            GuardPackage(zip, budget);
            var sheetPath = FirstSheetPath(zip, budget);
            var shared = ReadSharedStrings(zip, budget);
            var dateStyles = ReadDateStyles(zip, budget);
            return ReadSheet(zip, budget, sheetPath, shared, dateStyles);
        }
        catch (InvalidDataException) { throw new TabularFileException(UnsupportedType); }
        // The runtime cuts a stream off at its declared size, so an entry that inflates past it shows up as XML cut short there.
        catch (XmlException) { throw new TabularFileException(budget.HitDeclared ? Unsafe : Malformed); }
    }

    private static void GuardPackage(ZipArchive zip, Budget budget)
    {
        if (zip.Entries.Count > 1_000 || zip.Entries.Sum(e => e.Length) > MaxUncompressedBytes) throw new TabularFileException(Unsafe);
        if (zip.GetEntry("[Content_Types].xml") is null || zip.GetEntry("xl/workbook.xml") is null)
            throw new TabularFileException(UnsupportedType);

        foreach (var entry in zip.Entries)
        {
            var name = entry.FullName.Replace('\\', '/').ToLowerInvariant();
            if (name.StartsWith("xl/vbaproject") || name.StartsWith("xl/externallinks/") || name.StartsWith("xl/embeddings/")
                || name.StartsWith("xl/activex/") || name.EndsWith("vbadata.xml") || name.Contains("../") || name.StartsWith("xl/macrosheets/"))
                throw new TabularFileException(Unsafe);
            if (name.EndsWith(".rels"))
            {
                using var rels = XmlReader.Create(budget.Open(entry), Secure);
                while (rels.Read())
                {
                    if (rels.NodeType != XmlNodeType.Element || rels.LocalName != "Relationship") continue;
                    var external = string.Equals(rels.GetAttribute("TargetMode"), "External", StringComparison.OrdinalIgnoreCase);
                    var type = rels.GetAttribute("Type") ?? "";
                    // A plain web hyperlink is only text on the page; every other outside reference is refused.
                    if (external && !type.EndsWith("/hyperlink", StringComparison.OrdinalIgnoreCase)) throw new TabularFileException(Unsafe);
                }
            }
        }

        using var types = new StreamReader(budget.Open(zip.GetEntry("[Content_Types].xml")!));
        var typesXml = types.ReadToEnd();
        if (typesXml.Contains("macroEnabled", StringComparison.OrdinalIgnoreCase) || typesXml.Contains("vbaProject", StringComparison.OrdinalIgnoreCase))
            throw new TabularFileException(Unsafe);
    }

    /// <summary>
    /// Counts the bytes an XLSX really inflates to. A zip header can declare small sizes and still inflate far past
    /// them, so every entry is read through <see cref="Open"/>: the total across entries may not pass the cap and one
    /// entry may not pass its own declared length.
    /// </summary>
    private sealed class Budget
    {
        private long used;

        public bool HitDeclared { get; private set; }

        public Stream Open(ZipArchiveEntry entry) => new CountingStream(entry.Open(), entry.Length, n =>
        {
            used += n;
            if (used > MaxUncompressedBytes) throw new TabularFileException(Unsafe);
        }, () => HitDeclared = true);
    }

    private sealed class CountingStream(Stream inner, long declared, Action<int> total, Action reachedDeclared) : Stream
    {
        private long read;

        public override int Read(byte[] buffer, int offset, int count) => Count(inner.Read(buffer, offset, count));
        public override int Read(Span<byte> buffer) => Count(inner.Read(buffer));

        private int Count(int n)
        {
            read += n;
            if (read > declared) throw new TabularFileException(Unsafe);
            if (n > 0 && read == declared) reachedDeclared();
            total(n);
            return n;
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        protected override void Dispose(bool disposing) { if (disposing) inner.Dispose(); base.Dispose(disposing); }
    }

    private static readonly XmlReaderSettings Secure = new() { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersFromEntities = 0 };

    private static string FirstSheetPath(ZipArchive zip, Budget budget)
    {
        string? rid = null;
        using (var wb = XmlReader.Create(budget.Open(zip.GetEntry("xl/workbook.xml")!), Secure))
        {
            while (wb.Read())
                if (wb.NodeType == XmlNodeType.Element && wb.LocalName == "sheet") { rid = wb.GetAttribute("id", "http://schemas.openxmlformats.org/officeDocument/2006/relationships"); break; }
        }

        var relsEntry = zip.GetEntry("xl/_rels/workbook.xml.rels");
        if (rid is null || relsEntry is null) throw new TabularFileException(Malformed);
        using var rels = XmlReader.Create(budget.Open(relsEntry), Secure);
        while (rels.Read())
        {
            if (rels.NodeType == XmlNodeType.Element && rels.LocalName == "Relationship" && rels.GetAttribute("Id") == rid)
            {
                var target = (rels.GetAttribute("Target") ?? "").TrimStart('/');
                return target.StartsWith("xl/") ? target : "xl/" + target;
            }
        }

        throw new TabularFileException(Malformed);
    }

    private static List<string> ReadSharedStrings(ZipArchive zip, Budget budget)
    {
        var list = new List<string>();
        var entry = zip.GetEntry("xl/sharedStrings.xml");
        if (entry is null) return list;
        using var xml = XmlReader.Create(budget.Open(entry), Secure);
        var sb = new StringBuilder();
        var inSi = false;
        while (xml.Read())
        {
            if (xml.NodeType == XmlNodeType.Element && xml.LocalName == "si") { inSi = true; sb.Clear(); }
            else if (xml.NodeType == XmlNodeType.EndElement && xml.LocalName == "si") { inSi = false; list.Add(sb.ToString()); }
            else if (inSi && xml.NodeType == XmlNodeType.Element && xml.LocalName == "t") sb.Append(xml.ReadElementContentAsString());
        }

        return list;
    }

    /// <summary>The cell style indexes whose number format shows a date.</summary>
    private static HashSet<int> ReadDateStyles(ZipArchive zip, Budget budget)
    {
        var result = new HashSet<int>();
        var entry = zip.GetEntry("xl/styles.xml");
        if (entry is null) return result;
        var custom = new Dictionary<int, string>();
        var xfs = new List<int>();
        using var xml = XmlReader.Create(budget.Open(entry), Secure);
        var inCellXfs = false;
        while (xml.Read())
        {
            if (xml.NodeType == XmlNodeType.Element && xml.LocalName == "numFmt")
                custom[int.Parse(xml.GetAttribute("numFmtId") ?? "0", CultureInfo.InvariantCulture)] = xml.GetAttribute("formatCode") ?? "";
            else if (xml.NodeType == XmlNodeType.Element && xml.LocalName == "cellXfs") inCellXfs = true;
            else if (xml.NodeType == XmlNodeType.EndElement && xml.LocalName == "cellXfs") inCellXfs = false;
            else if (inCellXfs && xml.NodeType == XmlNodeType.Element && xml.LocalName == "xf")
                xfs.Add(int.Parse(xml.GetAttribute("numFmtId") ?? "0", CultureInfo.InvariantCulture));
        }

        for (var i = 0; i < xfs.Count; i++)
        {
            var id = xfs[i];
            var date = id is (>= 14 and <= 22) or (>= 45 and <= 47)
                || (custom.TryGetValue(id, out var code) && IsDateFormat(code));
            if (date) result.Add(i);
        }

        return result;
    }

    private static bool IsDateFormat(string code)
    {
        var stripped = System.Text.RegularExpressions.Regex.Replace(code, "\"[^\"]*\"|\\[[^\\]]*\\]|\\\\.", "");
        return stripped.IndexOfAny(['y', 'Y', 'd', 'D', 'h', 'H', 's', 'S']) >= 0 || stripped.Contains('m', StringComparison.OrdinalIgnoreCase) && !stripped.Contains('0');
    }

    private static TabularData ReadSheet(ZipArchive zip, Budget budget, string path, List<string> shared, HashSet<int> dateStyles)
    {
        var entry = zip.GetEntry(path) ?? throw new TabularFileException(Malformed);
        using var xml = XmlReader.Create(budget.Open(entry), Secure);
        var rows = new List<(int Number, string?[] Cells)>();
        string?[]? header = null;
        var total = 0;
        var rowNumber = 0;
        List<string?>? cells = null;
        while (xml.Read())
        {
            if (xml.NodeType == XmlNodeType.Element && xml.LocalName == "row")
            {
                rowNumber = int.TryParse(xml.GetAttribute("r"), out var r) ? r : rowNumber + 1;
                cells = [];
                if (xml.IsEmptyElement) cells = null;
            }
            else if (xml.NodeType == XmlNodeType.Element && xml.LocalName == "c" && cells is not null)
            {
                var col = ColumnIndex(xml.GetAttribute("r"));
                var type = xml.GetAttribute("t");
                var style = int.TryParse(xml.GetAttribute("s"), out var s) ? s : -1;
                var value = ReadCell(xml, type);
                if (col >= MaxColumns + 1) throw new TabularFileException(TooManyColumns);
                while (cells.Count <= col) cells.Add(null);
                cells[col] = Interpret(value, type, style, shared, dateStyles);
            }
            else if (xml.NodeType == XmlNodeType.EndElement && xml.LocalName == "row" && cells is not null)
            {
                var arr = cells.ToArray();
                cells = null;
                if (header is null) { if (arr.Any(c => !string.IsNullOrWhiteSpace(c))) header = arr; continue; }
                if (arr.All(c => string.IsNullOrWhiteSpace(c))) continue; // a blank row is ignored
                total++;
                if (total > MaxCountedRows) break;
                if (rows.Count < MaxRows) rows.Add((total, arr));
            }
        }

        if (header is null) throw new TabularFileException(NoRows);
        return Build("xlsx", header, rows.Select(r => new TabularRow(r.Number, r.Cells)).ToList(), total);
    }

    private static string? ReadCell(XmlReader xml, string? type)
    {
        if (xml.IsEmptyElement) return null;
        var depth = xml.Depth;
        string? v = null;
        var inline = new StringBuilder();
        while (xml.Read() && xml.Depth > depth)
        {
            if (xml.NodeType != XmlNodeType.Element) continue;
            if (xml.LocalName == "v") v = xml.ReadElementContentAsString();
            else if (xml.LocalName == "t" && type == "inlineStr") inline.Append(xml.ReadElementContentAsString());
        }

        return type == "inlineStr" ? inline.ToString() : v;
    }

    private static string? Interpret(string? raw, string? type, int style, List<string> shared, HashSet<int> dateStyles)
    {
        if (raw is null) return null;
        switch (type)
        {
            case "s": return int.TryParse(raw, out var i) && i >= 0 && i < shared.Count ? Cap(shared[i]) : null;
            case "b": return raw == "1" ? "true" : "false";
            case "e": return null;
            case "str": case "inlineStr": return Cap(raw);
        }

        if (dateStyles.Contains(style) && double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var serial) && serial is > 0 and < 2_958_466)
        {
            var date = DateTime.FromOADate(serial);
            return date.TimeOfDay == TimeSpan.Zero ? date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : date.ToString("yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture);
        }

        return Cap(raw);
    }

    private static string? Cap(string value) => value.Length > MaxCellLength ? throw new TabularFileException(Malformed) : value.Length == 0 ? null : value;

    private static int ColumnIndex(string? reference)
    {
        var n = 0;
        foreach (var ch in reference ?? "A")
        {
            if (!char.IsAsciiLetter(ch)) break;
            n = n * 26 + (char.ToUpperInvariant(ch) - 'A' + 1);
        }

        return Math.Max(n - 1, 0);
    }

    private static TabularData Build(string format, string?[] header, IReadOnlyList<TabularRow> rows, int total)
    {
        var width = header.Length;
        var names = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < width; i++)
        {
            var name = (header[i] ?? "").Trim();
            if (name.Length == 0) name = $"#{i + 1}";
            var unique = name;
            for (var n = 2; !seen.Add(unique); n++) unique = $"{name} ({n})";
            names.Add(unique);
        }

        var fixedRows = rows.Select(r => new TabularRow(r.Number, Enumerable.Range(0, width).Select(i => i < r.Cells.Length ? r.Cells[i] : null).ToArray())).ToList();
        return new TabularData(format, names, fixedRows, total);
    }
}
