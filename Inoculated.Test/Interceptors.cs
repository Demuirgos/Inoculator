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
    public MethodData Invoke(MethodData method) {
        (object Instance, object[] Parameters, object ReturnValue) = (null, null, null);
        if(method.IsStatic) {
            Parameters = method.Parameters.Select(p => p.Value).ToArray();
        } else {
            Instance = method.Parameters[0].Value;
            Parameters = method.Parameters.Skip(1).Select(p => p.Value).ToArray();
        }
        var methodInfo = method.ReflectionInfo;


        var result = methodInfo.Invoke(Instance, Parameters);
        ReturnValue = result;

        
        method.ReturnValue = new ParameterData(
            typesrc  : method.Signature.Output.Name.ToString(),
            Value : ReturnValue
        );
        return method;
    }

    public MethodData InvokeAsync(MethodData method) {
        (object Instance, object[] Parameters, object ReturnValue) = (null, null, null);
        if(method.IsStatic) {
            Parameters = method.Parameters.Select(p => p.Value).ToArray();
        } else {
            Instance = method.Parameters[0].Value;
            Parameters = method.Parameters.Skip(1).Select(p => p.Value).ToArray();
        }
        var methodInfo = method.ReflectionInfo;


        var result = (Task)methodInfo.Invoke(Instance, Parameters);
        result.Wait();
        var resultProperty = result.GetType().GetProperty("Result");
        var resultValue = resultProperty.GetValue(result);
        ReturnValue = resultValue;
        
        method.ReturnValue = new ParameterData(
            typesrc  : method.Signature.Output.Name.ToString(),
            Value : resultValue
        );
        return method;
    }

    public MethodData InvokeEnum(MethodData method) {
        Console.WriteLine("here");   
        (object Instance, object[] Parameters, object ReturnValue) = (null, null, null);
        if(method.IsStatic) {
            Parameters = method.Parameters.Select(p => p.Value).ToArray();
        } else {
            Instance = method.Parameters[0].Value;
            Parameters = method.Parameters.Skip(1).Select(p => p.Value).ToArray();
        }
        var methodInfo = method.ReflectionInfo;


        var result = (IEnumerable)methodInfo.Invoke(Instance, Parameters);

        method.ReturnValue = new ParameterData(
            typesrc  : method.Signature.Output.Name.ToString(),
            Value : result
        );
        return method;
    }
    public override MethodData OnCall(MethodData method)
    {
        var argumentsHash = StringifyAndHash(method.Parameters);
        if(cache.ContainsKey(argumentsHash)) {
            method.ReturnValue = cache[argumentsHash];
        } else {
            switch(method.MethodBehaviour) {
                case MethodData.MethodType.Async:
                    method = InvokeAsync(method);
                    break;
                case MethodData.MethodType.Sync:
                    method = Invoke(method);
                    break;
                case MethodData.MethodType.Iter:
                    method = InvokeEnum(method);
                    break;
            }
        }
        cache.Add(argumentsHash, method.ReturnValue);
        Console.WriteLine(method);
        return method;
    }
}