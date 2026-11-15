using System.Text.Json;
using AIUsageDeck.Core;
namespace AIUsageDeck.Collector;
public sealed class AntigravityAdapter(AdapterPaths paths):IUsageAdapter
{
    public string Name=>"antigravity";
    public Task<string> ReadIdentityAsync(CancellationToken ct)=>Task.FromResult("ambient-cli");
    public async Task<ProviderRead> FetchAsync(CancellationToken ct)
    {
        var probe=Path.Combine(paths.DataDirectory,"probe");SecureFiles.EnsureDirectory(probe);
        var raw=await ProcessRunner.RunAsync(IdentityTools.Executable("agy"),["-p","/usage","--output-format","json","--print-timeout","30s"],probe,TimeSpan.FromSeconds(45),ct);
        var report=JsonDocument.Parse(raw).RootElement;
        if(report.Field("num_turns").TryNumber(out var turns)&&turns!=0||report.Field("usage").Field("total_tokens").TryNumber(out var tokens)&&tokens!=0)
            throw new UsageFailure("unexpected_agent_turn");
        return new("ambient-cli",QuotaParser.ParseAntigravity(report),"agy /usage (0 agent turns)");
    }
}
