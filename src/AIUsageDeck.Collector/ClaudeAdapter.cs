using AIUsageDeck.Core;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
namespace AIUsageDeck.Collector;
public sealed class ClaudeAdapter:IUsageAdapter
{
    private readonly AdapterPaths paths;private readonly HttpClient client;
    public ClaudeAdapter(AdapterPaths paths,HttpClient client){this.paths=paths;this.client=client;}
    public string Name=>"claude";
    private async Task<(string Token,string Account,string Organization,string Identity)> ReadCredentialsAsync(CancellationToken ct)
    {
        try{
            var credentials=JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(paths.ClaudeDirectory,".credentials.json"),ct)).RootElement.Field("claudeAiOauth");
            var metadata=JsonDocument.Parse(await File.ReadAllTextAsync(paths.ClaudeMetadata,ct)).RootElement.Field("oauthAccount");
            var token=credentials.Field("accessToken").Text();var account=metadata.Field("accountUuid").Text();var org=metadata.Field("organizationUuid").Text();
            if(string.IsNullOrEmpty(token)||string.IsNullOrEmpty(account)||string.IsNullOrEmpty(org))throw new UsageFailure("login_required","needsLogin");
            if(credentials.Field("expiresAt").TryNumber(out var expiry)&&expiry<DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())throw new UsageFailure("login_expired","needsLogin");
            return(token,account,org,IdentityTools.Hash(account+"|"+org+"|"+token));
        }catch(Exception e)when(e is IOException or JsonException){throw new UsageFailure("credentials_unavailable","needsLogin");}
    }
    public async Task<string> ReadIdentityAsync(CancellationToken ct)=>(await ReadCredentialsAsync(ct)).Identity;
    public async Task<ProviderRead> FetchAsync(CancellationToken ct)
    {
        var credential=await ReadCredentialsAsync(ct);
        var profile=await GetAsync("profile",credential.Token,ct);
        if(profile.Field("account").Field("uuid").Text()!=credential.Account||profile.Field("organization").Field("uuid").Text()!=credential.Organization)
            throw new UsageFailure("identity_mismatch","accountChanged");
        var usage=await GetAsync("usage",credential.Token,ct);
        return new(credential.Identity,QuotaParser.ParseClaude(usage),"Claude OAuth usage (read-only)");
    }
    private async Task<JsonElement> GetAsync(string route,string token,CancellationToken ct)
    {
        using var request=new HttpRequestMessage(HttpMethod.Get,"https://api.anthropic.com/api/oauth/"+route);
        request.Headers.Authorization=new AuthenticationHeaderValue("Bearer",token);
        request.Headers.Add("anthropic-beta","oauth-2025-04-20");request.Headers.UserAgent.ParseAdd("claude-cli/2.1.296 (external, cli)");
        using var response=await client.SendAsync(request,HttpCompletionOption.ResponseHeadersRead,ct);
        if(response.StatusCode==HttpStatusCode.TooManyRequests){var retry=response.Headers.RetryAfter;var wait=retry?.Delta??(retry?.Date-DateTimeOffset.UtcNow);throw new UsageFailure("http_429","rateLimited",wait??TimeSpan.FromMinutes(5));}
        if(response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)throw new UsageFailure("login_required","needsLogin");
        if(!response.IsSuccessStatusCode)throw new UsageFailure("http_error");
        using var stream=await response.Content.ReadAsStreamAsync(ct);using var reader=new StreamReader(stream);
        var json=await ProcessRunner.ReadLimitedAsync(reader,1048576,ct);return JsonDocument.Parse(json).RootElement.Clone();
    }
}
