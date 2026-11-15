using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using AIUsageDeck.Core;
namespace AIUsageDeck.Collector;
public sealed class PipeServer(CollectorEngine engine)
{
    public const string Name="AIUsageDeck.v1";
    private int subscribers;
    public int Subscribers=>Volatile.Read(ref subscribers);
    public async Task RunAsync(CancellationToken ct)
    {
        var clients=new List<Task>();
        while(!ct.IsCancellationRequested){
            var pipe=new NamedPipeServerStream(Name,PipeDirection.InOut,8,PipeTransmissionMode.Byte,PipeOptions.Asynchronous|PipeOptions.CurrentUserOnly,4096,4096);
            try{await pipe.WaitForConnectionAsync(ct);}catch{pipe.Dispose();throw;}
            clients.RemoveAll(t=>t.IsCompleted);clients.Add(HandleAsync(pipe,ct));
        }
        await Task.WhenAll(clients);
    }
    private async Task HandleAsync(NamedPipeServerStream pipe,CancellationToken ct)
    {
        using(pipe)using(var reader=new StreamReader(pipe,new UTF8Encoding(false),false,4096,true))using(var writer=new StreamWriter(pipe,new UTF8Encoding(false),4096,true){AutoFlush=true}){
            var subscribed=false;
            try{
                using var first=CancellationTokenSource.CreateLinkedTokenSource(ct);first.CancelAfter(TimeSpan.FromSeconds(10));
                var line=await PipeProtocol.ReadLineAsync(reader,first.Token);if(line is null)return;var request=PipeProtocol.Parse(line);
                if(request.Method=="refresh")await engine.RefreshAsync(request.Provider!,true,ct);
                if(request.Method=="subscribe"){Interlocked.Increment(ref subscribers);subscribed=true;}
                do{
                    var reply=JsonSerializer.Serialize(new{version=1,id=request.Id,snapshot=engine.GetSnapshot()},JsonConfig.Options);
                    if(reply.Length>PipeProtocol.MaxLine)throw new InvalidDataException("snapshot_too_large");
                    await writer.WriteLineAsync(reply.AsMemory(),ct);if(!subscribed)break;await Task.Delay(TimeSpan.FromSeconds(2),ct);
                }while(!ct.IsCancellationRequested&&pipe.IsConnected);
            }catch(Exception e)when(e is IOException or OperationCanceledException or InvalidDataException or JsonException){ }
            finally{if(subscribed)Interlocked.Decrement(ref subscribers);}
        }
    }
}
