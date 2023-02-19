using System.Collections;
using System.Reflection;
using Inoculator.Builder;
namespace Inoculator.Attributes;
public interface IRewriter
{
    MethodData OnCall(MethodData method);
}

public interface IInterceptor
{
    void OnEntry(MethodData method);
    void OnException(MethodData method);
    void OnSuccess(MethodData method);
    void OnExit(MethodData method);
}

[AttributeUsage(AttributeTargets.Method)]
public abstract class RewriterAttribute : System.Attribute, IRewriter
{
    /// <summary>
    /// Called Inplace in Call Sight
    /// This replaces the method call with the return value of this method.
    /// It gets full metadata of the function before execution
    /// and Sets the metadata of the function after execution
    /// relies on the user to set the return value and exception
    /// </summary>
    public abstract MethodData OnCall(MethodData method);
}


[AttributeUsage(AttributeTargets.Method)]
public class InterceptorAttribute : System.Attribute, IInterceptor
{
    /// <summary>
    /// Called before the method is executed.
    /// This is called before the method is called at the beginning of the method.
    /// It gets full metadata of the function before execution
    /// It is suitable for setting up the environment for the method.
    /// </summary>

    public virtual void OnEntry(MethodData method) {}

    /// <summary>
    /// Called Before method Call.
    /// This is called before the method is called, but exactly before the method is called.
    /// </summary>
    public virtual void OnBegin(MethodData method) {}

    /// <summary>
    /// Called After method Call.
    /// This is called after the method is called, but before the method returns.
    /// It doesn't Update the return value. nor does it update the exception. 
    /// </summary>
    public virtual void OnEnd(MethodData method) {}
    
    /// <summary>
    /// Called when the method throws an exception.
    /// This is called after the method is called when function faults and throws an exception.
    /// It gets the exception
    /// </summary>
    public virtual void OnException(MethodData method) {}

    /// <summary>
    /// Called when the method returns successfully.
    /// This is called after the method is called when function returns successfully.
    /// It gets the return value
    /// </summary>
    public virtual void OnSuccess(MethodData method) {}

    /// <summary>
    /// Called after the method is executed.
    /// This is called after the method is called at the end of the method.
    /// It gets full metadata of the function after execution
    /// </summary>
    public virtual void OnExit(MethodData method) {}
}

public abstract class InterpreterAttribute : System.Attribute, IInterceptor, IRewriter
{
    public virtual void OnEntry(MethodData method) {}
    public virtual void OnException(MethodData method) {}
    public abstract MethodData OnCall(MethodData method);
    public virtual void OnSuccess(MethodData method) {}
    public virtual void OnExit(MethodData method) {}
}

public class Engine<TAssemblyMarker>
{
    public interface HandlerBase {
        public abstract (object, bool) HandleMethodInvocation(MethodInfo method, object instance, object[] parameters);
        public MethodData Invoke(MethodData method) {
            (object Instance, object[] Parameters, object ReturnValue) = (null, null, null);
            if(method.IsStatic) {
                Parameters = method.Parameters.Select(p => p.Value).ToArray();
            } else {
                Instance = method.Parameters[0].Value;
                Parameters = method.Parameters.Skip(1).Select(p => p.Value).ToArray();
            }
            
            (ReturnValue, bool Stop) = HandleMethodInvocation(method.ReflectionInfo<TAssemblyMarker>(), Instance, Parameters);
            
            for (int i = 0; i < Parameters.Length; i++)
            {
                method.Parameters[i + (method.IsStatic ? 0 : 1)].Value = Parameters[i];
            }
            
            method.Stop = Stop;
            method.ReturnValue = new ParameterData(
                value : ReturnValue,
                type : ReturnValue.GetType()
            );
            return method;
        }
    }

    public class SyncHandler : HandlerBase {
        public (object, bool) HandleMethodInvocation(MethodInfo method, object instance, object[] parameters) 
            => (method.Invoke(instance, parameters), true);
    }

    public class AsyncHandler : HandlerBase {
        public (object, bool) HandleMethodInvocation(MethodInfo method, object instance, object[] parameters)  {
            var result = method.Invoke(instance, parameters);
            if(result is Task casted_result) 
                casted_result.Wait(); 
            else if(result is ValueTask casted_result2) 
                casted_result2.AsTask().Wait(); 
            else 
                throw new Exception("Invalid async method");

            var resultProperty = result.GetType().GetProperty("Result");
            return (resultProperty.GetValue(result), true);
        }
    }

    public class EnumHandler : HandlerBase {
        private (IEnumerable Container, IEnumerator Iterator) enumState;
        public (object, bool) HandleMethodInvocation(MethodInfo method, object instance, object[] parameters)  {
            enumState.Container= (IEnumerable)method.Invoke(instance, parameters);
            enumState.Iterator = enumState.Iterator ?? enumState.Container.GetEnumerator();
            bool Stop = !enumState.Iterator.MoveNext();
            var resultValue = enumState.Iterator.Current;
            return (resultValue, Stop);
        }
    }
    HandlerBase engine = null;
    public MethodData Invoke(MethodData method)
    {
        engine = engine ?? method.MethodBehaviour switch {
            MethodData.MethodType.Async => new AsyncHandler(),
            MethodData.MethodType.Sync => new SyncHandler(),
            MethodData.MethodType.Iter => new EnumHandler(),
            _ => throw new Exception("Invalid method behaviour")
        }; 
        return engine.Invoke(method);
    }
}

