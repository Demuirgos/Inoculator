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
        => Console.WriteLine($"Method {method.Name(false)} took {watch.Elapsed}");
}
public class RetryAttribute<TMarker> : RewriterAttribute
{
    public RetryAttribute(int retries) => Retries = retries;
    Engine<TMarker> engine = new();
    int Retries;
    public override MethodData OnCall(MethodData method)
    {
        bool functionIsDone = false;
        while(!method.Stop && Retries >= 0) {
            try {
                method = engine.Invoke(method);
                break;
            } catch {
                if(--Retries == 0) throw; 
                else {
                    Console.WriteLine($"Retrying {method.Name(false)} : {Retries} left")
                    engine.Restart();
                }
            }
        }
        return method;
    }
}
```
* Flag function to be injected with code : 
```csharp
async static Task Main(string[] args)  {
    _ = SumIntervalIsEven(7, 23, out _);
    GuessNumberSeven(0, 10);
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

[RetryAttribute<typeof(Main)>(5)]
public static void GuessNumberSeven(int start, int end) {
    var random = new Random();
    if(random.Next(start, end) != 7) {
        throw new Exception("Wrong guess");
    }
    Console.WriteLine("Congrats Done");
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

Retrying GuessNumberSeven : 4 left
Retrying GuessNumberSeven : 3 left
Retrying GuessNumberSeven : 2 left
Congrats Done
```
