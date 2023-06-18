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

public class IsFlakyTestAttribute<TMarker> : Attribute, IInterceptor, IRewriter
{
    private bool IsFlaky;
    private RetryAttribute<TMarker> retryEngine; 

    private int numberOfFails = 0;
    public IsFlakyTestAttribute(bool isFlaky, int numOfReruns) => (IsFlaky, retryEngine) = (isFlaky, new RetryAttribute<TMarker>(numOfReruns));  

    public MethodData OnCall(MethodData method)
    {
        if(IsFlaky) {
            return retryEngine.OnCall(method);;
        } else {
            return method;
        }
        throw new NotImplementedException();
    }

    public void OnEntry(MethodData method)
    {
        Console.WriteLine($"Starting method {method.Name(false)}");
        if(IsFlaky) {
            Console.WriteLine($"Method {method.Name(false)} is flaky");
        }
    }

    public void OnException(MethodData method)
    {
        numberOfFails++;
    }

    public void OnExit(MethodData method)
    {
    }

    public void OnSuccess(MethodData method)
    {
        Console.WriteLine($"Method {method.Name(false)} passed {retryEngine.Reruns - numberOfFails}/{retryEngine.Reruns} times");
    }
}

public class RetryAttribute<TMarker> : RewriterAttribute
{
    public RetryAttribute(int retries) => Retries = retries;
    Engine<TMarker> engine = new();
    public int Retries;
    public int Reruns;
    public override MethodData OnCall(MethodData method)
    {
        bool functionIsDone = false;
        while(!functionIsDone && Retries >= 0) {
            try {
                method = engine.Invoke(method);
                functionIsDone = method.Stop;
                break;
            } catch {
                Reruns++;
                Console.WriteLine($"Retrying {method.Name(false)}, {Retries} retries left");
                if(--Retries == 0) throw; 
                else {
                    engine.Restart();
                }
            }
        }
        return method;
    }
}

public class ElapsedTimeAttribute : InterceptorAttribute
{
    private Stopwatch watch = new();
    public override void OnBegin(MethodData method)
    {
        watch.Start();
    }

    public override void OnEnd(MethodData method)
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