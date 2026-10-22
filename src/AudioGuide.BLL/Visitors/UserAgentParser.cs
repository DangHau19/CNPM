namespace AudioGuide.BLL.Visitors;

public record UserAgentInfo(string DeviceType, string Browser, string Os);

/// <summary>Phân tích User-Agent đơn giản (đủ để thống kê loại thiết bị / trình duyệt / hệ điều hành).</summary>
public static class UserAgentParser
{
    public static UserAgentInfo Parse(string? ua)
    {
        if (string.IsNullOrWhiteSpace(ua)) return new UserAgentInfo("desktop", "Unknown", "Unknown");

        bool Has(string s) => ua.Contains(s, StringComparison.OrdinalIgnoreCase);

        string device;
        if (Has("bot") || Has("crawl") || Has("spider")) device = "bot";
        else if (Has("iPad") || Has("Tablet") || (Has("Android") && !Has("Mobile"))) device = "tablet";
        else if (Has("Mobi") || Has("iPhone") || Has("Android")) device = "mobile";
        else device = "desktop";

        string os;
        if (Has("Windows")) os = "Windows";
        else if (Has("Android")) os = "Android";
        else if (Has("iPhone") || Has("iPad") || Has("iPod")) os = "iOS";
        else if (Has("Mac OS X") || Has("Macintosh")) os = "macOS";
        else if (Has("CrOS")) os = "ChromeOS";
        else if (Has("Linux")) os = "Linux";
        else os = "Other";

        // Thứ tự quan trọng: Edge/Opera/Samsung/Chrome đều chứa chuỗi "Chrome" hoặc "Safari".
        string browser;
        if (Has("Edg/") || Has("EdgA/") || Has("EdgiOS/")) browser = "Edge";
        else if (Has("OPR/") || Has("Opera")) browser = "Opera";
        else if (Has("SamsungBrowser")) browser = "Samsung Internet";
        else if (Has("Firefox/") || Has("FxiOS/")) browser = "Firefox";
        else if (Has("CriOS/") || Has("Chrome/")) browser = "Chrome";
        else if (Has("Safari/")) browser = "Safari";
        else browser = "Other";

        return new UserAgentInfo(device, browser, os);
    }
}
