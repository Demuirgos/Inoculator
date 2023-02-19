using Inoculator.Attributes;
using Inoculator.Builder;
using System.Diagnostics;

public class LogEntrencyAttribute : InterceptorAttribute
{
    public override void OnEntry(MethodData method)
    {
        Console.WriteLine($"Before: {method}");
    }

    public override void OnBegin(MethodData method)
    {
        Console.WriteLine($"Begin: {method}");
    }

    public override void OnEnd(MethodData method)
    {
        Console.WriteLine($"End: {method}");
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

public class CallCountAttribute : InterceptorAttribute
{
    public static Dictionary<string, int> CallCounter = new Dictionary<string, int>();
    public override void OnEntry(MethodData method)
    {
        if(!CallCounter.ContainsKey(method.MethodName))
            CallCounter.Add(method.MethodName, 0);
        CallCounter[method.MethodName]++;
    }
}

public class RetryAttribute<TMarker> : RewriterAttribute
{
    public RetryAttribute(int retries) => Retries = retries;
    Engine<TMarker> engine = new();
    int Retries;
    public override MethodData OnCall(MethodData method)
    {
        bool functionIsDone = false;
        while(!functionIsDone && Retries > 0) {
            try {
                method = engine.Invoke(method);
                functionIsDone = method.Stop;
                break;
            } catch (Exception e) {
                Console.WriteLine($"Exception: {e.Message}");
                Retries--;
                if(Retries == 0) throw;
                Console.WriteLine($"Retrying {method.MethodName} {Retries} times left");
                engine.Restart();
            }
        }
        return method;
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
        Console.WriteLine($"Method {method.Name(false)} took {watch.ElapsedMilliseconds}ms (interceptor)");
    }
}


public class DurationLoggerAttribute<TAssemblyMarker> : InterpreterAttribute
{
    private Stopwatch watch = new();
    private Engine<TAssemblyMarker> engine = new();
    public override MethodData OnCall(MethodData method)
    {
        return engine.Invoke(method);
    }
    public override void OnEntry(MethodData method)
    {
        watch.Start();
    }

    public override void OnExit(MethodData method)
    {
        watch.Stop();
        Console.WriteLine($"Method {method.Name(false)} took {watch.ElapsedMilliseconds}ms (interceptor)");
    }
}