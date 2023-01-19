using System.Diagnostics;
using Inoculator.Attributes;
using Inoculator.Builder;

public class LogEntrencyAttribute : InterceptorAttribute
{
    public override void OnEntry(Metadata method)
    {
        Console.WriteLine($"Started Method {method.Name}");
    }

    public override void OnExit(Metadata method)
    {
        Console.WriteLine($"Finished Method {method.Name} with {method.ReturnValue}");
    }
}

public class ElapsedTimeAttribute : InterceptorAttribute
{
    public override void OnEntry(Metadata method)
    {
        method.EmbededResource = Stopwatch.StartNew();        
    }

    public override void OnExit(Metadata method)
    {
        var sw = (Stopwatch)method.EmbededResource;
        sw.Stop();
        Console.WriteLine($"Method {method.Name} took {sw.ElapsedMilliseconds}ms");
    }
}