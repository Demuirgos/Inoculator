// See https://aka.ms/new-console-template for more information
using System.Extensions;
using Inoculator.Core;

string methodIl = @"
.method private hidebysig 
        instance int32 testOverwritten () cil managed 
    {
        .custom instance void Name/InterceptorAttribute::.ctor() = (
            01 00 00 00
        )
        .maxstack 3
        .locals init (
            [0] valuetype Name/Point X,
            [1] uint32 j,
            [2] object,
            [3] class Name/Test
        )

        IL_0000: ldc.i4.0
        IL_0001: ldloca.s 0
        IL_0003: initobj Name/Point
        IL_0009: ldloca.s 0
        IL_000b: ldc.i4.s 23
        IL_000d: stfld int32 Name/Point::x
        IL_0012: ldstr ""Hello World""
        IL_0017: call void [System.Console]System.Console::WriteLine(string)
        IL_001c: ret
    } 
";

Result<Method, Exception> result = MethodParser.Parse(methodIl);

if(result is Success<Method, Exception> success) {
    Console.WriteLine(success.Value);
} else {
    Console.WriteLine(result);
}
