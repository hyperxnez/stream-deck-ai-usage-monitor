using System.Diagnostics;
using System.Text.Json;
using AIUsageDeck.Core;
namespace AIUsageDeck.Collector;
public sealed class CodexAdapter(AdapterPaths paths):IUsageAdapter
{
    public string Name=>"codex";
    public async Task<string> ReadIdentityAsync(CancellationToken ct)
    {
        try{
            var auth=JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(paths.CodexDirectory,"auth.json"),ct)).RootElement;
            var tokens=auth.Field("tokens");var id=tokens.Field("account_id").Text();var token=tokens.Field("access_token").Text();
            if(string.IsNullOrEmpty(id)||string.IsNullOrEmpty(token))throw new UsageFailure("chatgpt_login_required","needsLogin");
            return IdentityTools.Hash(id+"|"+token);
        }catch(Exception e)when(e is IOException or JsonException){throw new UsageFailure("credentials_unavailable","needsLogin");}
    }
    public async Task<ProviderRead> FetchAsync(CancellationToken ct)
    {
        var identity=await ReadIdentityAsync(ct);
        var info=new ProcessStartInfo(IdentityTools.Executable("codex")){UseShellExecute=false,CreateNoWindow=true,RedirectStandardInput=true,RedirectStandardOutput=true,RedirectStandardError=true,WorkingDirectory=paths.DataDirectory};
        foreach(var arg in new[]{"-c","mcp_servers={}","app-server","--stdio"})info.ArgumentList.Add(arg);
        info.Environment["CODEX_HOME"]=paths.CodexDirectory;
        using var process=new Process{StartInfo=info};using var deadline=CancellationTokenSource.CreateLinkedTokenSource(ct);deadline.CancelAfter(TimeSpan.FromSeconds(30));
        try{
            process.Start();var stderr=ProcessRunner.ReadLimitedAsync(process.StandardError,65536,deadline.Token);
            _=stderr.ContinueWith(_=>deadline.Cancel(),CancellationToken.None,TaskContinuationOptions.OnlyOnFaulted,TaskScheduler.Default);
            await Rpc(1,"initialize",new{clientInfo=new{name="ai_usage_deck",version="0.1.0"},capabilities=new{}});
            await process.StandardInput.WriteLineAsync("{\"method\":\"initialized\",\"params\":{}}");
            var account=await Rpc(2,"account/read",new{refreshToken=false});
            if(account.Field("account").Field("type").Text()!="chatgpt")throw new UsageFailure("chatgpt_login_required","needsLogin");
            var limits=await Rpc(3,"account/rateLimits/read",new{});
            return new(identity,QuotaParser.ParseCodex(limits),"Codex app-server account/rateLimits/read");
            async Task<JsonElement> Rpc(int id,string method,object parameters){
                await process.StandardInput.WriteLineAsync(JsonSerializer.Serialize(new{id,method,@params=parameters},JsonConfig.Options));
                for(var messages=0;messages<100;messages++){
                    var line=await PipeProtocol.ReadLineAsync(process.StandardOutput,deadline.Token);if(line is null)throw new UsageFailure("codex_closed");
                    var message=JsonDocument.Parse(line).RootElement;
                    if(message.Field("id").TryNumber(out var number)&&number==id){if(message.Field("error").ValueKind!=JsonValueKind.Undefined)throw new UsageFailure("codex_rpc_error");return message.Field("result").Clone();}
                }throw new UsageFailure("codex_message_limit");
            }
        }catch(OperationCanceledException)when(!ct.IsCancellationRequested){throw new UsageFailure("codex_timeout");}
        finally{deadline.Cancel();try{if(process.Id>0&&!process.HasExited){process.Kill(true);await process.WaitForExitAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(5));}}catch(Exception){}}
    }
}
