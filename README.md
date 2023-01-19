# Innoculator
An IL code Injector using Ilasm and Ildasm (WIP)
# Limitations
    * (that I know of): Only Works on Synchronous functions
# Plan 
    * make it Work on Asynchronous functions
# Usage
* Inherit InterceptorAttribute and override Function lifecycle nodes :  
```csharp
public class ElapsedTimeAttribute : InterceptorAttribute
{
    public override void OnEntry(Metadata method)
    {
        method.EmbededResource = Stopwatch.StartNew();        
    }

    public override void OnExit(Metadata method)
    {
        var sw = (Stopwatch)method.EmbededResource;
        sw.Stop();
        Console.WriteLine($"Method {method.Name} took {sw.ElapsedMilliseconds}ms");
    }
}
```
* Flag function to be injected with code : 
```csharp
static void Main(string[] args) {
    Test();
}

[ElapsedTime, LogEntrency]
public static void Test() {
    int i = 0;
    for (int j = 0; j < 100; j++) {
        i++;
    }
    Console.WriteLine(i);
}
```
* Output :
```
Started Method Test                                                                                                                                           
100                                                                                                                                                           
Method Testtook 4ms
Finished Method Test
```
