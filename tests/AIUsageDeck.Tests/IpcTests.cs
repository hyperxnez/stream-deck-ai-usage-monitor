using System.Text;
using AIUsageDeck.Collector;
internal static class IpcTests
{
    public static void Add(List<(string,Action)> tests)
    {
        tests.Add(("Collector singleton lock excludes duplicates and releases across async threads",()=>{
            var file=Path.Combine(Path.GetTempPath(),"ai-usage-lock-"+Guid.NewGuid().ToString("N"));
            try{var first=SingleInstance.TryAcquire(file);Check.True(first!=null);Check.Equal<FileStream?>(null,SingleInstance.TryAcquire(file));Task.Run(()=>first!.Dispose()).GetAwaiter().GetResult();using var next=SingleInstance.TryAcquire(file);Check.True(next!=null);}finally{File.Delete(file);}
        }));
        tests.Add(("IPC rejects unknown methods rather than executing commands",()=>{
            var rejected=false;try{PipeProtocol.Parse("""{"version":1,"id":"1","method":"exec","provider":"claude"}""");}catch(InvalidDataException){rejected=true;}Check.True(rejected);
        }));
        tests.Add(("IPC rejects unsupported protocol versions",()=>{
            var rejected=false;try{PipeProtocol.Parse("""{"version":2,"method":"get"}""");}catch(InvalidDataException){rejected=true;}Check.True(rejected);
        }));
        tests.Add(("IPC accepts bounded valid refresh requests",()=>{
            var request=PipeProtocol.Parse("""{"version":1,"id":"1","method":"refresh","provider":"claude"}""");Check.Equal("claude",request.Provider);
        }));
        tests.Add(("IPC oversized line is rejected before allocation grows",()=>{
            using var reader=new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(new string('x',65537)+"\n")));
            var rejected=false;try{PipeProtocol.ReadLineAsync(reader,default).GetAwaiter().GetResult();}catch(InvalidDataException){rejected=true;}Check.True(rejected);
        }));
        tests.Add(("IPC EOF with partial JSON is rejected",()=>{
            using var reader=new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes("{\"version\":1")));
            var rejected=false;try{PipeProtocol.ReadLineAsync(reader,default).GetAwaiter().GetResult();}catch(InvalidDataException){rejected=true;}Check.True(rejected);
        }));
        tests.Add(("Bounded process runner captures read-only child output",()=>{
            var output=ProcessRunner.RunAsync(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System),"whoami.exe"),[],Path.GetTempPath(),TimeSpan.FromSeconds(5),default).GetAwaiter().GetResult();Check.True(output.Trim().Length>0);
        }));
    }
}
