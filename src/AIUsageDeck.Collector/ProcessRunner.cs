using System.Diagnostics;
using System.Text;
using AIUsageDeck.Core;
namespace AIUsageDeck.Collector;

public static class ProcessRunner
{
    public static async Task<string> RunAsync(string executable,IReadOnlyList<string> arguments,string workingDirectory,TimeSpan timeout,CancellationToken ct)
    {
        var info=new ProcessStartInfo(executable){WorkingDirectory=workingDirectory,UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true,RedirectStandardInput=true};
        foreach(var arg in arguments)info.ArgumentList.Add(arg);
        info.Environment["AGY_CLI_DISABLE_AUTO_UPDATE"]="true";info.Environment["CI"]="true";
        using var process=new Process{StartInfo=info};
        using var limit=CancellationTokenSource.CreateLinkedTokenSource(ct);limit.CancelAfter(timeout);
        try{
            process.Start();process.StandardInput.Close();
            var stdout=ReadLimitedAsync(process.StandardOutput,1048576,limit.Token);
            var stderr=ReadLimitedAsync(process.StandardError,65536,limit.Token);
            // A failed output reader must cancel the whole child, not leave it blocked on a full pipe.
            _=stdout.ContinueWith(_=>limit.Cancel(),CancellationToken.None,TaskContinuationOptions.OnlyOnFaulted,TaskScheduler.Default);
            _=stderr.ContinueWith(_=>limit.Cancel(),CancellationToken.None,TaskContinuationOptions.OnlyOnFaulted,TaskScheduler.Default);
            await process.WaitForExitAsync(limit.Token);
            var output=await stdout;await stderr;
            if(process.ExitCode!=0)throw new UsageFailure("cli_failed");return output;
        }catch(OperationCanceledException)when(!ct.IsCancellationRequested){throw new UsageFailure("cli_timeout");}
        finally{try{if(process.Id>0&&!process.HasExited){process.Kill(true);await process.WaitForExitAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(5));}}catch(Exception){}}
    }
    public static async Task<string> ReadLimitedAsync(StreamReader reader,int maximum,CancellationToken ct)
    {
        var text=new StringBuilder();var chars=new char[4096];int count;
        while((count=await reader.ReadAsync(chars.AsMemory(),ct))>0){if(text.Length+count>maximum)throw new InvalidDataException("output_too_large");text.Append(chars,0,count);}
        return text.ToString();
    }
}
