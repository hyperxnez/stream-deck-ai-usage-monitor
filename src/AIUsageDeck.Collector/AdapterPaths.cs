namespace AIUsageDeck.Collector;
public sealed record AdapterPaths(string Home,string ClaudeDirectory,string CodexDirectory,string DataDirectory)
{
    public static AdapterPaths Default(){var home=Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);return new(home,Environment.GetEnvironmentVariable("CLAUDE_CONFIG_DIR")??Path.Combine(home,".claude"),Environment.GetEnvironmentVariable("CODEX_HOME")??Path.Combine(home,".codex"),Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"AIUsageDeck"));}
    public string ClaudeMetadata=>File.Exists(Path.Combine(ClaudeDirectory,".claude.json"))?Path.Combine(ClaudeDirectory,".claude.json"):Path.Combine(Home,".claude.json");
}
public static class IdentityTools
{
    public static string Hash(string value)=>Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(value)));
    public static string Executable(string name)
    {
        var home=Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);var local=Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);var app=Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var candidates=new List<string>();
        if(name=="agy")candidates.Add(Path.Combine(local,"agy","bin","agy.exe"));
        if(name=="codex")candidates.Add(Path.Combine(app,"npm","node_modules","@openai","codex","node_modules","@openai","codex-win32-x64","vendor","x86_64-pc-windows-msvc","bin","codex.exe"));
        candidates.Add(Path.Combine(home,".local","bin",name+".exe"));
        candidates.AddRange((Environment.GetEnvironmentVariable("PATH")??"").Split(Path.PathSeparator).Select(p=>Path.Combine(p,name+".exe")));
        return candidates.FirstOrDefault(File.Exists)??throw new AIUsageDeck.Core.UsageFailure("cli_not_found");
    }
}
