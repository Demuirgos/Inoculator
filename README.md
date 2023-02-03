# Innoculator
An IL code Injector using Ilasm and Ildasm
# Plan 
    * Needs Excessive Testing
    * Automatically Add MSbuild PostBuild event handler
# Usage
* Reference Inoculator.Injecter
* Add to Msbuild :
   ```
   <Target Name="InjectionStep" BeforeTargets="AfterBuild">
       <Exec Command="$(MSbuildProjectDirectory)\$(BaseOutputPath)$(Configuration)\$(TargetFramework)\Inoculator.Injector.exe   $(MSbuildProjectDirectory)\$(BaseOutputPath)$(Configuration)\$(TargetFramework)\$(AssemblyName).dll" />
   </Target>
  ```
* Inherit InterceptorAttribute and override Function lifecycle nodes :  
```csharp
public class ElapsedTimeAttribute : InterceptorAttribute
{
    private Stopwatch watch = new();
    public override void OnEntry(MethodData method)
        => watch.Start();

    public override void OnExit(MethodData method)
        => Console.WriteLine($"Method {method.Name(false)} took {watch.ElapsedMilliseconds}ms");
}
```
* Flag function to be injected with code : 
```csharp
async static Task Main(string[] args) {
            // Gen region
    _ = SumIntervalIsEven(7, 23, out _);
    foreach(var kvp in CallCountAttribute.CallCounter) {
        Console.WriteLine($"{kvp.Key}: called {kvp.Value}: times");
    }
}


[ElapsedTime, LogEntrency, CallCount]
public static bool SumIntervalIsEven(int start, int end, out int r) {
    int result = 0;
    for (int j = start; j < end; j++) {
        result += j;
    }
    r = result;
    return r % 2 == 0;
}
```
* Output :
```json
Before: {
  "MethodName": "SumIntervalIsEven",
  "TypeSignature": "(int32, int32, int32 & -> bool)",
  "TypeParameters": [],
  "Parameters": [
    7,
    23,
    0
  ]
}

Success: True

Method SumIntervalIsEven took 93ms

After: {
  "MethodName": "SumIntervalIsEven",
  "TypeSignature": "(int32, int32, int32 & -> bool)",
  "TypeParameters": [],
  "Parameters": [
    7,
    23,
    232
  ],
  "ReturnValue": true
}

SumIntervalIsEven: called 1: times
```
