using System.Diagnostics;
using Inoculator.Attributes;
using Inoculator.Builder;

public class LogEntrencyAttribute : InterceptorAttribute
{
    public override void OnEntry(MethodData method)
    {
        Console.WriteLine($"Started Method {method.Name}");
    }

    public override void OnExit(MethodData method)
    {
        Console.WriteLine($"Finished Method {method.Name} with {method.ReturnValue}");
    }
}

public class ElapsedTimeAttribute : InterceptorAttribute
{
    public override void OnEntry(MethodData method)
    {
        method.EmbededResource = Stopwatch.StartNew();        
    }

    public override void OnExit(MethodData method)
    {
        var sw = (Stopwatch)method.EmbededResource;
        sw.Stop();
        Console.WriteLine($"Method {method.Name} took {sw.ElapsedMilliseconds}ms");
    }
}