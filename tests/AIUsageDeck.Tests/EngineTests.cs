using AIUsageDeck.Core;

internal static class EngineTests
{
    public static void Add(List<(string,Action)> tests)
    {
        tests.Add(("Failed refresh retains original observation time and becomes stale",()=>Run(async()=>{
            var now=DateTimeOffset.Parse("2026-11-15T00:00:00Z");var a=new FakeAdapter();var e=new CollectorEngine([a],()=>now);
            await e.RefreshAsync("claude");var observed=e.GetSnapshot().Providers[0].ObservedAt;
            now=now.AddMinutes(5);a.Error=new UsageFailure("network");await e.RefreshAsync("claude",true);
            var s=e.GetSnapshot().Providers[0];Check.Equal(observed,s.ObservedAt);Check.Equal("stale",s.State);Check.Equal(75d,s.Buckets[0].RemainingPercent);
        })));
        tests.Add(("Account swap discards late response from previous credentials",()=>Run(async()=>{
            var a=new FakeAdapter{Gate=new(TaskCreationOptions.RunContinuationsAsynchronously)};var e=new CollectorEngine([a]);
            var pending=e.RefreshAsync("claude");await a.Started.Task;a.Identity="B";a.Gate.SetResult();await pending;
            var s=e.GetSnapshot().Providers[0];Check.Equal(0,s.Buckets.Count);Check.Equal("accountChanged",s.State);
        })));
        tests.Add(("Invalid identity after swap never retains previous quota",()=>Run(async()=>{
            var now=DateTimeOffset.UtcNow;var a=new FakeAdapter();var e=new CollectorEngine([a],()=>now);await e.RefreshAsync("claude");
            a.Identity="B";a.Error=new UsageFailure("identity_mismatch","accountChanged");now=now.AddMinutes(3);await e.RefreshAsync("claude",true);
            Check.Equal(0,e.GetSnapshot().Providers[0].Buckets.Count);
        })));
        tests.Add(("Six concurrent refresh requests make one provider request",()=>Run(async()=>{
            var a=new FakeAdapter{Gate=new(TaskCreationOptions.RunContinuationsAsynchronously)};var e=new CollectorEngine([a]);
            var all=Enumerable.Range(0,6).Select(_=>e.RefreshAsync("claude",true)).ToArray();await a.Started.Task;a.Gate.SetResult();await Task.WhenAll(all);Check.Equal(1,a.Calls);
        })));
        tests.Add(("Manual refresh cannot bypass Retry-After",()=>Run(async()=>{
            var now=DateTimeOffset.UtcNow;var a=new FakeAdapter{Error=new UsageFailure("http_429","rateLimited",TimeSpan.FromMinutes(10))};var e=new CollectorEngine([a],()=>now);
            await e.RefreshAsync("claude");now=now.AddMinutes(2);await e.RefreshAsync("claude",true);Check.Equal(1,a.Calls);Check.Equal("rateLimited",e.GetSnapshot().Providers[0].State);
            now=now.AddMinutes(9);a.Error=null;await e.RefreshAsync("claude",true);Check.Equal(2,a.Calls);
        })));
        tests.Add(("Past reset marks quota pending without manufacturing a refill",()=>Run(async()=>{
            var now=DateTimeOffset.Parse("2026-11-15T00:00:00Z");var a=new FakeAdapter{ResetAt=now.AddSeconds(20)};var e=new CollectorEngine([a],()=>now);await e.RefreshAsync("claude");
            now=now.AddSeconds(21);var s=e.GetSnapshot().Providers[0];Check.Equal("resetPending",s.State);Check.Equal(75d,s.Buckets[0].RemainingPercent);
        })));
        tests.Add(("Already expired reset response does not cause repeated polling",()=>Run(async()=>{
            var now=DateTimeOffset.UtcNow;var a=new FakeAdapter{ResetAt=now.AddHours(-1)};var e=new CollectorEngine([a],()=>now);await e.RefreshAsync("claude");
            for(var i=0;i<10;i++){now=now.AddSeconds(1);await e.TickAsync();}Check.Equal(1,a.Calls);
        })));
        tests.Add(("Identity poll clears old account before next scheduled fetch",()=>Run(async()=>{
            var now=DateTimeOffset.UtcNow;var a=new FakeAdapter();var e=new CollectorEngine([a],()=>now);await e.RefreshAsync("claude");
            a.Identity="B";a.Error=new UsageFailure("login_required","needsLogin");now=now.AddSeconds(5);await e.TickAsync();Check.Equal(0,e.GetSnapshot().Providers[0].Buckets.Count);
        })));
        tests.Add(("Shrinking poll interval refreshes idle slot instead of staying stale",()=>Run(async()=>{
            var now=DateTimeOffset.Parse("2026-11-15T00:00:00Z");var a=new FakeAdapter();var e=new CollectorEngine([a],()=>now){PollInterval=TimeSpan.FromSeconds(300)};
            await e.TickAsync();Check.Equal(1,a.Calls);
            now=now.AddSeconds(130);e.PollInterval=TimeSpan.FromSeconds(120);await e.TickAsync();Check.Equal(2,a.Calls);
        })));
    }
    private static void Run(Func<Task> f)=>f().GetAwaiter().GetResult();
    private sealed class FakeAdapter:IUsageAdapter
    {
        public string Name=>"claude";public string Identity="A";public int Calls;public Exception? Error;public DateTimeOffset? ResetAt;
        public TaskCompletionSource? Gate;public TaskCompletionSource Started=new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Task<string> ReadIdentityAsync(CancellationToken ct)=>Task.FromResult(Identity);
        public async Task<ProviderRead> FetchAsync(CancellationToken ct){var id=Identity;Interlocked.Increment(ref Calls);Started.TrySetResult();if(Gate!=null)await Gate.Task.WaitAsync(ct);if(Error!=null)throw Error;return new(id,[new("default","All models","fiveHour",75,ResetAt)],"test");}
    }
}
