using Inoculator.Attributes;
using Inoculator.Builder;
using System.Diagnostics;

public class LogEntrencyAttribute : InterceptorAttribute
{
    public override void OnEntry(MethodData method)
    {
        Console.WriteLine($"Before: {method}");
    }
    public override void OnException(MethodData method)
    {
        Console.WriteLine($"Failed: {method.Exception.Message}");
    }
    public override void OnSuccess(MethodData method)
    {
        Console.WriteLine($"Success: {method.ReturnValue}");
    }
    public override void OnExit(MethodData method)
    {
        Console.WriteLine($"After: {method}");
    }
}

public class ElapsedTimeAttribute : InterceptorAttribute
{
    private Stopwatch watch = new();
    public override void OnEntry(MethodData method)
    {
        watch.Start();
    }

    public override void OnExit(MethodData method)
    {
        watch.Stop();
        Console.WriteLine($"Method {method.Name(false)} took {watch.ElapsedMilliseconds}ms");
    }
}