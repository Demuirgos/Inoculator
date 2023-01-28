# Innoculator
An IL code Injector using Ilasm and Ildasm (WIP)
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
public class LogEntrencyAttribute : InterceptorAttribute
{
    public override void OnEntry(MethodData method)
        => Console.WriteLine($"Before: {method}");

    public override void OnExit(MethodData method)
        => Console.WriteLine($"After: {method}");
}
```
* Flag function to be injected with code : 
```csharp
async static Task Main(string[] args) {
    TestS(out int start, 10);
}

[ElapsedTime, LogEntrency]
public static int TestS(out int k, int m) {
    int i = 0;
    k = 23;
    for (int j = 0; j < k; j++) {
        i += m + j;
    }
    return i;
}
```
* Output :
```json
Before: {
  "EmbededResource": {
    "IsRunning": true,
    "Elapsed": "00:00:00.0695625",
    "ElapsedMilliseconds": 71,
    "ElapsedTicks": 713658
  },
  "Name": "TestS",
  "MethodBehaviour": 1,
  "MethodCall": 0,
  "IsStatic": true,
  "TypeSignature": "(int32 &, int32 -> int32)",
  "TypeParameters": [],
  "Parameters": [
    0,
    10
  ],
  "MangledName": "'<>__TestS__Inoculated'"
}

Method TestS took 84ms

After: {
  "EmbededResource": {
    "IsRunning": false,
    "Elapsed": "00:00:00.0845090",
    "ElapsedMilliseconds": 84,
    "ElapsedTicks": 845090
  },
  "Name": "TestS",
  "MethodBehaviour": 1,
  "MethodCall": 0,
  "IsStatic": true,
  "TypeSignature": "(int32 &, int32 -> int32)",
  "TypeParameters": [],
  "Parameters": [
    23,
    10
  ],
  "ReturnValue": 483,
  "MangledName": "'<>__TestS__Inoculated'"
}
```
