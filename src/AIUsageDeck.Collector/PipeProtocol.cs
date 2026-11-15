using System.Text;
using System.Text.Json;
namespace AIUsageDeck.Collector;
public sealed record PipeRequest(int Version,string? Id,string Method,string? Provider);
public static class PipeProtocol
{
    public const int MaxLine=65536;
    public static async Task<string?> ReadLineAsync(StreamReader reader,CancellationToken ct)
    {
        var text=new StringBuilder();var one=new char[1];
        while(await reader.ReadAsync(one.AsMemory(),ct)>0){
            if(one[0]=='\n')return text.ToString().TrimEnd('\r');
            if(text.Length>=MaxLine)throw new InvalidDataException("message_too_large");text.Append(one[0]);
        }
        if(text.Length>0)throw new InvalidDataException("partial_message");return null;
    }
    public static PipeRequest Parse(string line)
    {
        if(line.Length>MaxLine)throw new InvalidDataException("message_too_large");
        PipeRequest? request;
        try{request=JsonSerializer.Deserialize<PipeRequest>(line,JsonConfig.Options);}catch(JsonException){throw new InvalidDataException("invalid_json");}
        if(request is null||request.Version!=1||request.Method is not ("get" or "refresh" or "subscribe")||request.Id?.Length>100)throw new InvalidDataException("invalid_request");
        if(request.Method=="refresh"&&request.Provider is not ("claude" or "codex" or "antigravity"))throw new InvalidDataException("invalid_provider");
        return request;
    }
}
public static class JsonConfig{public static readonly JsonSerializerOptions Options=new(JsonSerializerDefaults.Web){WriteIndented=false,MaxDepth=32};}
