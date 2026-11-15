using System.Text.Json;
namespace AIUsageDeck.Core;

public static class QuotaParser
{
    public static IReadOnlyList<QuotaBucket> ParseClaude(JsonElement root)
    {
        var result = new List<QuotaBucket>();
        Add("five_hour", "default", "All models", "fiveHour");
        Add(root.Field("seven_day").ValueKind == JsonValueKind.Object ? "seven_day" : "seven_day_oauth_apps", "default", "All models", "sevenDay");
        if (root.ValueKind == JsonValueKind.Object)
            foreach (var p in root.EnumerateObject())
                if (p.Name.StartsWith("seven_day_") && p.Name is not ("seven_day_oauth_apps" or "seven_day_breakdown") && p.Value.Field("utilization").ValueKind != JsonValueKind.Undefined)
                    Add(p.Name, p.Name, p.Name[10..].Replace('_', ' '), "sevenDay");
        return result;
        void Add(string key, string group, string name, string window)
        {
            var item = root.Field(key);
            if (item.ValueKind != JsonValueKind.Object) return;
            result.Add(new(group, name, window, Percent(item.Field("utilization"), true), Date(item.Field("resets_at"))));
        }
    }
    public static IReadOnlyList<QuotaBucket> ParseCodex(JsonElement root)
    {
        var result = new List<QuotaBucket>();
        var multi = root.Field("rateLimitsByLimitId");
        if (multi.ValueKind == JsonValueKind.Object && multi.EnumerateObject().Any())
            foreach (var p in multi.EnumerateObject()) Add(p.Value, p.Name);
        else Add(root.Field("rateLimits"), "codex");
        return result;
        void Add(JsonElement group, string id)
        {
            foreach (var key in new[] { "primary", "secondary" })
            {
                var item = group.Field(key);
                if (item.ValueKind != JsonValueKind.Object) continue;
                var mins = item.Field("windowDurationMins");
                var window = mins.TryNumber(out var duration) ? duration switch { 300 => "fiveHour", 10080 => "sevenDay", _ => $"{duration}m" } : "unknown";
                result.Add(new(id, group.Field("limitName").Text() ?? id, window, Percent(item.Field("usedPercent"), true), Date(item.Field("resetsAt"))));
            }
        }
    }
    public static IReadOnlyList<QuotaBucket> ParseAntigravity(JsonElement root)
    {
        if (root.Field("status").Text() != "SUCCESS" || root.Field("command").Field("name").Text() is not ("usage" or "quota"))
            throw new InvalidDataException("invalid_usage_report");
        var result = new List<QuotaBucket>();
        var groups = root.Field("command").Field("data").Field("groups");
        if (groups.ValueKind != JsonValueKind.Array) throw new InvalidDataException("missing_quota_groups");
        foreach (var group in groups.EnumerateArray())
        {
            var name = group.Field("name").Text() ?? "Unknown group";
            var id = group.Field("id").Text() ?? name.ToLowerInvariant().Replace(' ', '-');
            var buckets = group.Field("buckets");
            if (buckets.ValueKind != JsonValueKind.Array) continue;
            foreach (var item in buckets.EnumerateArray())
            {
                var rawWindow = item.Field("window").Text() ?? "unknown";
                var window = rawWindow switch { "5h" => "fiveHour", "weekly" => "sevenDay", _ => rawWindow };
                double? fraction = item.Field("remaining_fraction").TryNumber(out var n) && n is >= 0 and <= 1 ? n * 100 : null;
                result.Add(new(id, name, window, fraction, Date(item.Field("reset_time"))));
            }
        }
        return result;
    }
    private static double? Percent(JsonElement value, bool used) => value.TryNumber(out var n) && n is >= 0 and <= 100 ? used ? 100 - n : n : null;
    private static DateTimeOffset? Date(JsonElement value)
    {
        if (value.TryNumber(out var unix) && unix >= 0 && unix < 253402300800) return DateTimeOffset.FromUnixTimeSeconds((long)unix);
        return value.ValueKind == JsonValueKind.String && DateTimeOffset.TryParse(value.GetString(), System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AssumeUniversal, out var date) ? date : null;
    }
}

public static class JsonFields
{
    public static JsonElement Field(this JsonElement value, string key) => value.ValueKind == JsonValueKind.Object && value.TryGetProperty(key, out var field) ? field : default;
    public static string? Text(this JsonElement value) => value.ValueKind == JsonValueKind.String ? value.GetString() : null;
    public static bool TryNumber(this JsonElement value, out double number) { number = 0; return value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out number) && double.IsFinite(number); }
}
