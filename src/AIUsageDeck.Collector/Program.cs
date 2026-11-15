using System.Text.Json;
using AIUsageDeck.Core;
using AIUsageDeck.Collector;

var paths=AdapterPaths.Default();SecureFiles.EnsureDirectory(paths.DataDirectory);
using var http=new HttpClient(new SocketsHttpHandler{AllowAutoRedirect=false,UseCookies=false}){Timeout=TimeSpan.FromSeconds(25)};
var engine=new CollectorEngine([new ClaudeAdapter(paths,http),new CodexAdapter(paths),new AntigravityAdapter(paths)]);
using var lifetime=new CancellationTokenSource();Console.CancelKeyPress+=(_,e)=>{e.Cancel=true;lifetime.Cancel();};
if(args.Contains("--once")){
    await engine.TickAsync(lifetime.Token);Console.WriteLine(JsonSerializer.Serialize(engine.GetSnapshot(),JsonConfig.Options));return 0;
}
using var singleton=SingleInstance.TryAcquire(Path.Combine(paths.DataDirectory,"collector.lock"));
if(singleton is null)return 0;
try{
    var server=new PipeServer(engine);var serve=server.RunAsync(lifetime.Token);
    var lastSave=DateTimeOffset.MinValue;
    while(!lifetime.IsCancellationRequested){
        engine.PollInterval=TimeSpan.FromSeconds(server.Subscribers>0?120:300);
        await engine.TickAsync(lifetime.Token);
        if(DateTimeOffset.UtcNow-lastSave>TimeSpan.FromSeconds(10)){await SecureFiles.SaveAsync(paths.DataDirectory,engine.GetSnapshot(),lifetime.Token);lastSave=DateTimeOffset.UtcNow;}
        if(serve.IsFaulted)await serve;
        await Task.Delay(TimeSpan.FromSeconds(3),lifetime.Token);
    }
}catch(OperationCanceledException)when(lifetime.IsCancellationRequested){ }
catch(Exception){Console.Error.WriteLine("collector_failed");return 1;}
return 0;
