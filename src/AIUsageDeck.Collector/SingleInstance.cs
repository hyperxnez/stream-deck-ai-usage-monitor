namespace AIUsageDeck.Collector;
public static class SingleInstance
{
    public static FileStream? TryAcquire(string path){try{return new FileStream(path,FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);}catch(IOException){return null;}}
}
