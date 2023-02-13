using System.Collections;
using System.Reflection;
using Inoculator.Builder;
namespace Inoculator.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public abstract class RewriterAttribute : System.Attribute
{
    public abstract MethodData OnCall(MethodData method);
}

[AttributeUsage(AttributeTargets.Method)]
public class InterceptorAttribute : System.Attribute
{
    public virtual void OnEntry(MethodData method) {}
    public virtual void OnException(MethodData method) {}
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
            var result = (Task)method.Invoke(instance, parameters);
            result.Wait();
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

