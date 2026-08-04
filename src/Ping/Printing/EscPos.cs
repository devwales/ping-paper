using System.Text;

namespace Ping.Printing;

/// <summary>
/// Minimal ESC/POS command builder, tuned for generic 58mm / 80mm thermal printers
/// such as the CISSIYOG / POSSAF E. Standard ESC/POS only - no vendor-specific codes.
/// </summary>
public class EscPos
{
    private readonly List<byte> _buf = new();

    // Character columns per paper width (Font A, 12x24).
    public static int ColumnsFor(int paperWidthMm) => paperWidthMm >= 80 ? 48 : 32;

    public EscPos Init() { _buf.AddRange(new byte[] { 0x1B, 0x40 }); return this; }               // ESC @
    public EscPos AlignLeft() { _buf.AddRange(new byte[] { 0x1B, 0x61, 0x00 }); return this; }    // ESC a 0
    public EscPos AlignCenter() { _buf.AddRange(new byte[] { 0x1B, 0x61, 0x01 }); return this; }  // ESC a 1
    public EscPos AlignRight() { _buf.AddRange(new byte[] { 0x1B, 0x61, 0x02 }); return this; }   // ESC a 2
    public EscPos Bold(bool on) { _buf.AddRange(new byte[] { 0x1B, 0x45, (byte)(on ? 1 : 0) }); return this; } // ESC E n
    public EscPos FontA() { _buf.AddRange(new byte[] { 0x1B, 0x4D, 0x00 }); return this; }        // ESC M 0

    /// <summary>GS ! n - character size. 0 = normal, 1 = double. </summary>
    public EscPos Size(int widthMul, int heightMul)
    {
        var n = (byte)(((widthMul & 0x07) << 4) | (heightMul & 0x07));
        _buf.AddRange(new byte[] { 0x1D, 0x21, n });
        return this;
    }

    public EscPos NewLine() { _buf.Add(0x0A); return this; }

    public EscPos Feed(int lines) { _buf.AddRange(new byte[] { 0x1B, 0x64, (byte)Math.Min(lines, 255) }); return this; } // ESC d n

    /// <summary>Feed a little, then cut. GS V 66 n is supported by virtually all cutters.</summary>
    public EscPos Cut() { _buf.AddRange(new byte[] { 0x1D, 0x56, 0x42, 0x03 }); return this; }    // GS V 66 3

    public EscPos Text(string text)
    {
        _buf.AddRange(Encoding.ASCII.GetBytes(Sanitize(text)));
        return this;
    }

    public EscPos TextLine(string text)
    {
        Text(text);
        return NewLine();
    }

    public EscPos Divider(int columns)
    {
        return TextLine(new string('-', columns));
    }

    /// <summary>Word-wrap to a column count, printing each line.</summary>
    public EscPos Wrapped(string text, int columns)
    {
        foreach (var line in Wrap(text, columns))
            TextLine(line);
        return this;
    }

    public static List<string> Wrap(string text, int columns)
    {
        var lines = new List<string>();
        foreach (var raw in text.Split('\n'))
        {
            var remaining = raw.Trim();
            if (remaining.Length == 0) { lines.Add(""); continue; }
            while (remaining.Length > columns)
            {
                var cut = remaining.LastIndexOf(' ', columns);
                if (cut <= 0) cut = columns;
                lines.Add(remaining[..cut].TrimEnd());
                remaining = remaining[cut..].TrimStart();
            }
            if (remaining.Length > 0) lines.Add(remaining);
        }
        return lines;
    }

    public byte[] Build() => _buf.ToArray();

    /// <summary>
    /// Thermal printers speak ASCII code pages. Map common unicode punctuation to
    /// ASCII and drop anything else so receipts never print garbage glyphs.
    /// </summary>
    public static string Sanitize(string text)
    {
        var sb = new StringBuilder(text.Length);
        foreach (var c in text.Normalize(NormalizationForm.FormD))
        {
            switch (c)
            {
                case '\u2018' or '\u2019' or '\u02BC': sb.Append('\''); break;
                case '\u201C' or '\u201D': sb.Append('"'); break;
                case '\u2013' or '\u2014': sb.Append('-'); break;
                case '\u2026': sb.Append("..."); break;
                case '\u2022': sb.Append('*'); break;
                case '\u2192': sb.Append("->"); break;
                case >= (char)32 and < (char)127: sb.Append(c); break;
                default:
                    // Strip diacritics that survived normalization; drop the rest.
                    if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) !=
                        System.Globalization.UnicodeCategory.NonSpacingMark && c < 256 && c >= 32)
                        sb.Append('?');
                    break;
            }
        }
        return sb.ToString();
    }
}
