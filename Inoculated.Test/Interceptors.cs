using Inoculator.Attributes;
using Inoculator.Builder;
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


public class InvokeReflectiveAttribute : InterceptorAttribute
{
    public override void OnEntry(MethodData method)
    {
        var methodInfo = method.ReflectionInfo;
        Console.WriteLine("ReflectionInfo: {methodInfo?.Name}");
    }
}