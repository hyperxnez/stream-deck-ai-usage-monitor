using AIUsageDeck.Core;

internal static class ParserTests
{
    public static void Add(List<(string, Action)> tests)
    {
        tests.Add(("Claude returns remaining rather than used percentage", () => {
            var q = QuotaParser.ParseClaude(Check.Json("""{"five_hour":{"utilization":16,"resets_at":"2026-11-15T09:29:59+00:00"},"seven_day":{"utilization":48}}"""));
            Check.Equal(84d, q.Single(x => x.Window == "fiveHour").RemainingPercent);
            Check.Equal(52d, q.Single(x => x.Window == "sevenDay").RemainingPercent);
            Check.Equal(9, q[0].ResetAt!.Value.Hour);
        }));
        tests.Add(("Claude missing five-hour data never borrows weekly", () => {
            var q = QuotaParser.ParseClaude(Check.Json("""{"five_hour":null,"seven_day":{"utilization":20}}"""));
            Check.Equal(1, q.Count); Check.Equal("sevenDay", q[0].Window);
        }));
        tests.Add(("Invalid utilization does not become zero remaining", () => {
            var q = QuotaParser.ParseClaude(Check.Json("""{"five_hour":{"utilization":"16"},"seven_day":{"utilization":120},"seven_day_opus":{"utilization":false}}"""));
            Check.True(q.All(x => x.RemainingPercent is null));
        }));
        tests.Add(("Codex uses actual window durations and all buckets", () => {
            var q = QuotaParser.ParseCodex(Check.Json("""{"rateLimitsByLimitId":{"codex":{"primary":{"usedPercent":57,"windowDurationMins":300,"resetsAt":1794735082},"secondary":{"usedPercent":15,"windowDurationMins":10080}},"other":{"primary":{"usedPercent":10,"windowDurationMins":15}}}}"""));
            Check.Equal(3, q.Count);Check.Equal(43d,q.Single(x=>x.GroupId=="codex"&&x.Window=="fiveHour").RemainingPercent);
            Check.Equal(85d,q.Single(x=>x.Window=="sevenDay").RemainingPercent);
            Check.Equal("15m",q.Single(x=>x.GroupId=="other").Window);
        }));
        tests.Add(("Codex legacy response remains supported", () => {
            var q=QuotaParser.ParseCodex(Check.Json("""{"rateLimits":{"limitId":"codex","primary":{"usedPercent":0,"windowDurationMins":300}}}"""));
            Check.Equal(100d,q.Single().RemainingPercent);
        }));
        tests.Add(("Antigravity keeps independent groups and converts fractions", () => {
            var q=QuotaParser.ParseAntigravity(Check.Json("""{"status":"SUCCESS","command":{"name":"usage","data":{"groups":[{"name":"Gemini Models","buckets":[{"id":"gemini-weekly","window":"weekly","remaining_fraction":0.25},{"id":"gemini-5h","window":"5h","remaining_fraction":1}]},{"name":"Claude and GPT models","buckets":[{"id":"3p-weekly","window":"weekly","remaining_fraction":0.8}]}]}}}"""));
            Check.Equal(3,q.Count);Check.Equal(2,q.Select(x=>x.GroupId).Distinct().Count());
            Check.Equal(25d,q.Single(x=>x.GroupName=="Gemini Models"&&x.Window=="sevenDay").RemainingPercent);
            Check.Equal(100d,q.Single(x=>x.Window=="fiveHour").RemainingPercent);
        }));
        tests.Add(("Antigravity unknown fraction stays unknown", () => {
            var q=QuotaParser.ParseAntigravity(Check.Json("""{"status":"SUCCESS","command":{"name":"usage","data":{"groups":[{"name":"G","buckets":[{"window":"5h","remaining_fraction":-1}]}]}}}"""));
            Check.Equal<double?>(null,q.Single().RemainingPercent);
        }));
        tests.Add(("Failed Antigravity command is rejected", () => {
            var threw=false;try{QuotaParser.ParseAntigravity(Check.Json("""{"status":"ERROR","command":{"name":"usage"}}"""));}catch(System.IO.InvalidDataException){threw=true;}Check.True(threw);
        }));
    }
}
