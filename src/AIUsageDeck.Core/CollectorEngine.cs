namespace AIUsageDeck.Core;

public interface IUsageAdapter
{
    string Name { get; }
    Task<string> ReadIdentityAsync(CancellationToken cancellationToken);
    Task<ProviderRead> FetchAsync(CancellationToken cancellationToken);
}
public sealed class UsageFailure(string code, string state = "unavailable", TimeSpan? retryAfter = null) : Exception(code)
{
    public string Code { get; } = code;
    public string State { get; } = state;
    public TimeSpan? RetryAfter { get; } = retryAfter;
}
public sealed class CollectorEngine
{
    private sealed class Slot(IUsageAdapter adapter)
    {
        public readonly IUsageAdapter Adapter=adapter;
        public string? Identity;public bool Busy;public int Failures;
        public DateTimeOffset NextAt=DateTimeOffset.MinValue,LastStart=DateTimeOffset.MinValue;
        public ProviderSnapshot Snapshot=new(adapter.Name,"unavailable",null,[],null,null,"not_checked",null,null);
    }
    private readonly object sync=new();
    private readonly Dictionary<string,Slot> slots;
    private readonly Func<DateTimeOffset> clock;
    public TimeSpan PollInterval {get;set;}=TimeSpan.FromSeconds(120);
    public CollectorEngine(IEnumerable<IUsageAdapter> adapters, Func<DateTimeOffset>? clock = null)
    { slots=adapters.ToDictionary(a=>a.Name,a=>new Slot(a));this.clock=clock??(()=>DateTimeOffset.UtcNow); }
    public UsageSnapshot GetSnapshot()
    {
        lock(sync){var now=clock();return new(1,now,slots.Values.Select(s=>{
            var q=s.Snapshot;
            if(q.State=="fresh"){
                if(q.ObservedAt is {} observed && now-observed>PollInterval*2) q=q with {State="stale"};
                else if(q.Buckets.Any(b=>b.ResetAt is {} reset&&reset<=now))q=q with {State="resetPending"};
            }return q;
        }).ToArray());}
    }
    public async Task RefreshAsync(string provider, bool force = false, CancellationToken cancellationToken = default)
    {
        if(!slots.TryGetValue(provider,out var slot))return;
        var now=clock();
        lock(sync){
            if(slot.Busy||slot.Snapshot.RetryAt>now||now-slot.LastStart<TimeSpan.FromSeconds(15)||(!force&&slot.NextAt>now))return;
            slot.Busy=true;slot.LastStart=now;
            slot.Snapshot=slot.Snapshot with {LastAttemptAt=now};
        }
        try{
            var identity=await slot.Adapter.ReadIdentityAsync(cancellationToken);
            lock(sync)UpdateIdentity(slot,identity);
            var read=await slot.Adapter.FetchAsync(cancellationToken);
            var after=await slot.Adapter.ReadIdentityAsync(cancellationToken);
            lock(sync){
                if(after!=identity||read.Identity!=identity||slot.Identity!=identity){UpdateIdentity(slot,after);slot.Snapshot=slot.Snapshot with {State="accountChanged",Buckets=[],ObservedAt=null,ErrorCode="identity_changed"};slot.NextAt=clock().AddSeconds(15);return;}
                var observed=clock();slot.Failures=0;
                slot.Snapshot=new(provider,"fresh",provider=="antigravity"?"CLI":"Account "+identity[..Math.Min(6,identity.Length)],read.Buckets,observed,now,null,null,read.Source);
                slot.NextAt=observed+PollInterval;
                var upcoming=read.Buckets.Where(b=>b.ResetAt>observed).Select(b=>b.ResetAt!.Value.AddSeconds(3)).Order().FirstOrDefault();
                if(upcoming!=default&&upcoming<slot.NextAt)slot.NextAt=upcoming;
            }
        }catch(OperationCanceledException)when(cancellationToken.IsCancellationRequested){ }
        catch(Exception error){
            lock(sync){
                var failure=error as UsageFailure;slot.Failures++;
                var delay=failure?.RetryAfter??TimeSpan.FromSeconds(Math.Min(1800,120*Math.Pow(2,Math.Min(slot.Failures-1,4)))+Random.Shared.Next(0,15));
                if(delay<TimeSpan.FromSeconds(15))delay=TimeSpan.FromSeconds(15);
                var state=failure?.State??"unavailable";
                var clear=state is "accountChanged" or "needsLogin";
                slot.Snapshot=slot.Snapshot with {State=state=="unavailable"&&slot.Snapshot.ObservedAt!=null?"stale":state,ErrorCode=failure?.Code??"source_error",RetryAt=clock()+delay,
                    Buckets=clear?[]:slot.Snapshot.Buckets,ObservedAt=clear?null:slot.Snapshot.ObservedAt};
                slot.NextAt=clock()+delay;
            }
        }finally{lock(sync)slot.Busy=false;}
    }
    public async Task TickAsync(CancellationToken cancellationToken = default)
    {
        await Task.WhenAll(slots.Values.Select(async slot=>{
            try{
                var id=await slot.Adapter.ReadIdentityAsync(cancellationToken);
                lock(sync)UpdateIdentity(slot,id);
            }catch(Exception)when(!cancellationToken.IsCancellationRequested){
                lock(sync){slot.Snapshot=slot.Snapshot with {State="needsLogin",Buckets=[],ObservedAt=null,ErrorCode="identity_unavailable"};}
            }
            await RefreshAsync(slot.Adapter.Name,false,cancellationToken);
        }));
    }
    private void UpdateIdentity(Slot slot,string identity)
    {
        if(slot.Identity==identity)return;
        if(slot.Identity!=null){slot.Snapshot=slot.Snapshot with {State="accountChanged",AccountLabel=null,Buckets=[],ObservedAt=null,RetryAt=null,ErrorCode="identity_changed"};slot.NextAt=DateTimeOffset.MinValue;slot.Failures=0;}
        slot.Identity=identity;
    }
}
