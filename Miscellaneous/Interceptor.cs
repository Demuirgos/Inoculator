using System.Reflection;

public abstract class InterceptorAttribute : Attribute {
    public InterceptorAttribute() { }
    public abstract void OnEntry(Method method);
    public abstract void OnException(Method method);
    public abstract void OnSuccess(Method method);
    public abstract void OnExit(Method method);

}