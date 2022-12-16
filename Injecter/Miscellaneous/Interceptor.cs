using Inoculator.Parser.Models;
public class InterceptorAttribute : System.Attribute
{
    public InterceptorAttribute() { }
    public virtual void OnEntry(Method method) {}
    public virtual void OnException(Method method) {}
    public virtual void OnSuccess(Method method) {}
    public virtual void OnExit(Method method) {}

}