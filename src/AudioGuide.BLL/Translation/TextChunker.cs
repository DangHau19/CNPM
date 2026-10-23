using System.Text;
using System.Text.RegularExpressions;

namespace AudioGuide.BLL.Translation;

/// <summary>Cắt văn bản dài thành các đoạn ngắn (theo số BYTE UTF-8) ở ranh giới câu, vì dịch vụ dịch miễn phí giới hạn độ dài mỗi yêu cầu.</summary>
public static class TextChunker
{
    private static readonly Regex SentenceEnd = new(@"(?<=[.!?。！？])\s+", RegexOptions.Compiled);

    public static List<string> Split(string text, int maxBytes)
    {
        var chunks = new List<string>();
        var current = new StringBuilder();

        void Flush()
        {
            if (current.Length == 0) return;
            chunks.Add(current.ToString());
            current.Clear();
        }

        foreach (var sentence in SentenceEnd.Split((text ?? "").Trim()))
        {
            var s = sentence.Trim();
            if (s.Length == 0) continue;
            foreach (var piece in HardSplit(s, maxBytes))
            {
                if (current.Length > 0 && Bytes(current.ToString()) + 1 + Bytes(piece) > maxBytes) Flush();
                if (current.Length > 0) current.Append(' ');
                current.Append(piece);
            }
        }
        Flush();
        return chunks;
    }

    /// <summary>Câu dài hơn giới hạn: cắt theo từ; từ dài hơn giới hạn: cắt theo ký tự.</summary>
    private static List<string> HardSplit(string s, int maxBytes)
    {
        var pieces = new List<string>();
        if (Bytes(s) <= maxBytes) { pieces.Add(s); return pieces; }

        var current = new StringBuilder();
        void Flush()
        {
            if (current.Length == 0) return;
            pieces.Add(current.ToString());
            current.Clear();
        }

        foreach (var word in s.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            var w = word;
            while (Bytes(w) > maxBytes)
            {
                Flush();
                var n = 1;
                while (n < w.Length && Bytes(w[..(n + 1)]) <= maxBytes) n++;
                pieces.Add(w[..n]);
                w = w[n..];
            }
            if (current.Length > 0 && Bytes(current.ToString()) + 1 + Bytes(w) > maxBytes) Flush();
            if (current.Length > 0) current.Append(' ');
            current.Append(w);
        }
        Flush();
        return pieces;
    }

    private static int Bytes(string s) => Encoding.UTF8.GetByteCount(s);
}
