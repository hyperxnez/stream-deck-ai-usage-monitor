using AIUsageDeck.Core;
using System.Text.Json;

var tests = new List<(string Name, Action Run)>();
ParserTests.Add(tests);
EngineTests.Add(tests);
IpcTests.Add(tests);
AdapterTests.Add(tests);
var failures = 0;
foreach (var (name, run) in tests)
{
    try { run(); Console.WriteLine($"PASS {name}"); }
    catch (Exception e) { failures++; Console.WriteLine($"FAIL {name}: {e.GetType().Name}: {e.Message}"); }
}
Console.WriteLine($"{tests.Count - failures}/{tests.Count} passed");
return failures == 0 ? 0 : 1;

internal static class Check
{
    public static void Equal<T>(T expected, T actual) { if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new Exception($"expected {expected}, actual {actual}"); }
    public static void True(bool value) { if (!value) throw new Exception("expected true"); }
    public static JsonElement Json(string value) => JsonDocument.Parse(value).RootElement.Clone();
}
