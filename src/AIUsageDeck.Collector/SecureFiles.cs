using System.Security.AccessControl;
using System.Security.Principal;
using System.Text.Json;
using AIUsageDeck.Core;
namespace AIUsageDeck.Collector;
public static class SecureFiles
{
    public static void EnsureDirectory(string path)
    {
        var dir=Directory.CreateDirectory(path);var security=new DirectorySecurity();security.SetAccessRuleProtection(true,false);
        var user=WindowsIdentity.GetCurrent().User!;security.AddAccessRule(new FileSystemAccessRule(user,FileSystemRights.FullControl,InheritanceFlags.ContainerInherit|InheritanceFlags.ObjectInherit,PropagationFlags.None,AccessControlType.Allow));
        dir.SetAccessControl(security);
    }
    public static async Task SaveAsync(string directory,UsageSnapshot snapshot,CancellationToken ct)
    {
        var file=Path.Combine(directory,"snapshot.json");var temp=file+".tmp";
        await File.WriteAllTextAsync(temp,JsonSerializer.Serialize(snapshot,JsonConfig.Options),ct);File.Move(temp,file,true);
    }
}
