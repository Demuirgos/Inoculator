using Inoculator.Builder;
namespace Inoculator.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class InterceptorAttribute : System.Attribute
{
    public InterceptorAttribute() { }
    public virtual void OnEntry(Metadata method) {}
    public virtual void OnException(Metadata method) {}
    public virtual void OnSuccess(Metadata method) {}
    public virtual void OnExit(Metadata method) {}

}