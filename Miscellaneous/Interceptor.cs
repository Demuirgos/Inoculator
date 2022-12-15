using System.Reflection;

public abstract class InterceptorAttribute : Attribute {
    public InterceptorAttribute() { }
    public abstract void OnEntry(Method method);
    public abstract void OnException(Method method);
    public abstract void OnSuccess(Method method);
    public abstract void OnExit(Method method);

}

/*
[Interceptor]
int test() {
    Console.WriteLine("Hello World");
    return 0; 
}

void test() {
    var method = new MethodMetadata();
    InterceptorAttribute.OnEntry(method);
    try {
        Console.WriteLine("Hello World");
        method.ReturnValue = 0;
        InterceptorAttribute.OnSuccess(method);
        return method.ReturnValue;
    } catch (Exception e) {
        method.Exception = e;
        InterceptorAttribute.OnException(method);
    } finally {
        InterceptorAttribute.OnExit();
    }
}

*/