using System.Net;
using AIUsageDeck.Collector;
using AIUsageDeck.Core;
internal static class AdapterTests
{
    public static void Add(List<(string,Action)> tests)
    {
        tests.Add(("Claude rejects a token belonging to a different account",()=>WithFixture(async(paths)=>{
            using var http=new HttpClient(new FixtureHandler(false,HttpStatusCode.OK));var adapter=new ClaudeAdapter(paths,http);
            var code="";try{await adapter.FetchAsync(default);}catch(UsageFailure e){code=e.Code;}Check.Equal("identity_mismatch",code);
        })));
        tests.Add(("Claude usage fetch returns quota only for verified identity",()=>WithFixture(async(paths)=>{
            using var http=new HttpClient(new FixtureHandler(true,HttpStatusCode.OK));var adapter=new ClaudeAdapter(paths,http);var q=await adapter.FetchAsync(default);
            Check.Equal(84d,q.Buckets.Single().RemainingPercent);Check.Equal(await adapter.ReadIdentityAsync(default),q.Identity);
        })));
        tests.Add(("Claude 401 requests login without clearing credential files",()=>WithFixture(async(paths)=>{
            using var http=new HttpClient(new FixtureHandler(true,HttpStatusCode.Unauthorized));var adapter=new ClaudeAdapter(paths,http);var state="";
            try{await adapter.FetchAsync(default);}catch(UsageFailure e){state=e.State;}Check.Equal("needsLogin",state);Check.True(File.ReadAllText(Path.Combine(paths.ClaudeDirectory,".credentials.json")).Contains("fixture-token"));
        })));
        tests.Add(("Claude 429 preserves the server retry delay",()=>WithFixture(async(paths)=>{
            using var http=new HttpClient(new FixtureHandler(true,HttpStatusCode.TooManyRequests));var adapter=new ClaudeAdapter(paths,http);TimeSpan? delay=null;
            try{await adapter.FetchAsync(default);}catch(UsageFailure e){delay=e.RetryAfter;}Check.Equal(TimeSpan.FromSeconds(600),delay);
        })));
    }
    private static void WithFixture(Func<AdapterPaths,Task> run)
    {
        var root=Path.Combine(Path.GetTempPath(),"aiusagedeck-test-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(Path.Combine(root,".claude"));
        try{
            File.WriteAllText(Path.Combine(root,".claude",".credentials.json"),"""{"claudeAiOauth":{"accessToken":"fixture-token","expiresAt":9999999999999}}""");
            File.WriteAllText(Path.Combine(root,".claude.json"),"""{"oauthAccount":{"accountUuid":"account-A","organizationUuid":"org-A"}}""");
            run(new(root,Path.Combine(root,".claude"),Path.Combine(root,".codex"),Path.Combine(root,"data"))).GetAwaiter().GetResult();
        }finally{Directory.Delete(root,true);}
    }
    private sealed class FixtureHandler(bool matching,HttpStatusCode status):HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken ct)
        {
            Check.Equal("api.anthropic.com",request.RequestUri!.Host);Check.Equal("Bearer",request.Headers.Authorization?.Scheme);Check.Equal("fixture-token",request.Headers.Authorization?.Parameter);
            var json=request.RequestUri.AbsolutePath.EndsWith("profile")?"{\"account\":{\"uuid\":\""+(matching?"account-A":"account-B")+"\"},\"organization\":{\"uuid\":\"org-A\"}}":"""{"five_hour":{"utilization":16}}""";
            var r=new HttpResponseMessage(status){Content=new StringContent(json)};if(status==HttpStatusCode.TooManyRequests)r.Headers.RetryAfter=new(TimeSpan.FromSeconds(600));return Task.FromResult(r);
        }
    }
}
