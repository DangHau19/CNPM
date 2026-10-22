namespace AudioGuide.BLL.Visitors;

/// <summary>
/// Chọn ngôn ngữ từ header Accept-Language (hoặc navigator.languages) của thiết bị.
/// Ví dụ "vi-VN,vi;q=0.9,en-US;q=0.8" → "vi"; "zh-TW" → "zh"; ngôn ngữ chưa hỗ trợ → fallback.
/// </summary>
public static class LanguageNegotiator
{
    public static string Pick(string? acceptLanguage, IReadOnlyCollection<string> activeCodes, string fallback)
    {
        var active = new HashSet<string>(activeCodes.Select(c => c.ToLowerInvariant()));
        if (!string.IsNullOrWhiteSpace(acceptLanguage))
        {
            var candidates = acceptLanguage.Split(',')
                .Select(Parse)
                .Where(x => x.Tag.Length > 0 && x.Q > 0)
                .OrderByDescending(x => x.Q);   // OrderBy của LINQ ổn định: cùng q giữ nguyên thứ tự xuất hiện

            foreach (var c in candidates)
            {
                if (active.Contains(c.Tag)) return c.Tag;                       // khớp đầy đủ (vd: pt-br nếu có)
                var baseCode = c.Tag.Split('-')[0];
                if (active.Contains(baseCode)) return baseCode;                 // khớp mã gốc (vi-VN → vi)
            }
        }
        if (active.Contains(fallback.ToLowerInvariant())) return fallback.ToLowerInvariant();
        return activeCodes.FirstOrDefault()?.ToLowerInvariant() ?? fallback.ToLowerInvariant();
    }

    private static (string Tag, double Q) Parse(string part)
    {
        var pieces = part.Trim().Split(';');
        var tag = pieces[0].Trim().ToLowerInvariant();
        var q = 1.0;
        for (var i = 1; i < pieces.Length; i++)
        {
            var kv = pieces[i].Trim();
            if (kv.StartsWith("q=", StringComparison.OrdinalIgnoreCase) &&
                double.TryParse(kv.AsSpan(2), System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out var parsed))
                q = parsed;
        }
        return (tag == "*" ? "" : tag, q);
    }
}
