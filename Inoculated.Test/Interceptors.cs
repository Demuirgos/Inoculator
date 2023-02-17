using Inoculator.Attributes;
using Inoculator.Builder;
using System.Collections;
using System.Diagnostics;
using System.Reflection;


public class DateAttribute : InterceptorAttribute
{
    private Stopwatch watch = new();
    public override void OnEntry(MethodData method)
    {
        Console.WriteLine($"started : {DateTime.UtcNow}");
    }

    public override void OnExit(MethodData method)
    {
        Console.WriteLine($"ended : {DateTime.UtcNow}");
    }
}

public class DateAndTimeAttribute : DateAttribute
{
    private Stopwatch watch = new Stopwatch();
    public override void OnEntry(MethodData method)
    {
        base.OnEntry(method);
        watch.Start();
    }

    public override void OnExit(MethodData method)
    {
        watch.Stop();
        base.OnExit(method);
        Console.WriteLine($"Method {method.MethodName} took {watch.ElapsedMilliseconds}ms");
    }
}

public class WireAttribute : RewriterAttribute
{
    public WireAttribute(object returnValue) => ReturnValue = returnValue;
    object? ReturnValue;
    public override MethodData OnCall(MethodData method)
    {
        method.ReturnValue = new ParameterData(
            type : method.Signature.Output.TypeInstance,
            value : ReturnValue
        );
        return method;
    }
}

public class UpdateStaticClassAttribute<T> : InterceptorAttribute
{
    private FieldInfo targetField;
    public UpdateStaticClassAttribute() {
        var assembly = typeof(T).Assembly;
        // get static field of type Name U
        var type = assembly.GetType(typeof(T).FullName);
        targetField = type.GetField("CallCount");
    }
    public static Dictionary<string, int> CallCounter = new Dictionary<string, int>();
    public override void OnEntry(MethodData method)
    {
        targetField.SetValue(null, 23);
    }
}

public class DurationAttribute : RewriterAttribute
{
    private Stopwatch watch = new();
    Engine<Program.Entry> engine = new();
    public override MethodData OnCall(MethodData method)
    {
        watch.Start();
        engine.Invoke(method);
        watch.Stop();
        Console.WriteLine($"Method {method.MethodName} took {watch.ElapsedMilliseconds}ms (rewriter)");
        return method;
    }
}


public class MemoizeAttribute : RewriterAttribute
{
    private static Dictionary<int, ParameterData> cache = new Dictionary<int, ParameterData>();
    public int StringifyAndHash(object[] parameters) {
        var result = "";
        foreach(var parameter in parameters) {
            result += parameter.ToString();
        }
        return result.GetHashCode();
    }
    Engine<Program.Entry> engine = new();
    public override MethodData OnCall(MethodData method)
    {
        var argumentsHash = StringifyAndHash(method.Parameters);
        if(method.MethodBehaviour is not MethodData.MethodType.Iter && cache.ContainsKey(argumentsHash)) {
            method.ReturnValue = cache[argumentsHash];
        } else {
            method = engine.Invoke(method);
        }
        cache.TryAdd(argumentsHash, method.ReturnValue);
        Console.WriteLine(method);
        return method;
    }
}