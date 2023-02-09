using Inoculator.Builder;
namespace Inoculator.Attributes;

public interface IEntryInterceptor
{
    void OnEntry(MethodData method);
}

public interface IExceptionInterceptor
{
    void OnException(MethodData method);
}

public interface ICallInterceptor
{
    void OnCall(MethodData method);
}

public interface ISuccessInterceptor
{
    void OnSuccess(MethodData method);
}

public interface IExitInterceptor
{
    void OnExit(MethodData method);
}

public interface IInterceptor : IEntryInterceptor, IExceptionInterceptor, ISuccessInterceptor, IExitInterceptor, ICallInterceptor {}

[AttributeUsage(AttributeTargets.Method)]
public class InterceptorAttribute : System.Attribute, IInterceptor
{
    public bool OverwriteCall;
    public InterceptorAttribute() {}
    public virtual void OnEntry(MethodData method) {}
    public virtual void OnException(MethodData method) {}
    public virtual void OnCall(MethodData method) {}
    public virtual void OnSuccess(MethodData method) {}
    public virtual void OnExit(MethodData method) {}
}

