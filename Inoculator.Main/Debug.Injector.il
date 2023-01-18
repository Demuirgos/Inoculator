.method public hidebysig static 
	void Test () cil managed  {
.custom instance void LogEntrencyAttribute::.ctor ()=(01000000)
.maxstack 2
.locals init (
    class InterceptorAttribute interceptor,
    class Metadata metadata,
    
    class [System.Runtime]System.Exception e
)
IL_0000: newobj instance void LogEntrencyAttribute::.ctor()
IL_0001: stloc.0
IL_0002: ldstr ".methodpublichidebysigstaticvoidTest()cilmanaged{.custominstancevoidLogEntrencyAttribute::.ctor()=(01000000).maxstack2.localsinit(int32V_0,int32V_1,boolV_2)IL_0000:nopIL_0001:ldc.i4.0IL_0002:stloc.0IL_0003:ldc.i4.0IL_0004:stloc.1IL_0005:br.sIL_0011IL_0007:nopIL_0008:ldloc.0IL_0009:ldc.i4.1IL_000a:addIL_000b:stloc.0IL_000c:nopIL_000d:ldloc.1IL_000e:ldc.i4.1IL_000f:addIL_0010:stloc.1IL_0011:ldloc.1IL_0012:ldc.i4.s100IL_0014:cltIL_0016:stloc.2IL_0017:ldloc.2IL_0018:brtrue.sIL_0007IL_001a:ldloc.0IL_001b:callvoid[System.Console]System.Console::WriteLine(int32)IL_0020:nopIL_0021:ret}"
IL_0003: newobj instance void [Inoculator.Injector]Inoculator.Builder.Metadata::.ctor(string)
IL_0004: stloc.1

IL_0005: ldloc.1
IL_0006: ldc.i4.0
IL_0007: newarr [System.Runtime]System.Object


IL_0008: callvirt instance void [Inoculator.Injector]Inoculator.Builder.Metadata::set_Parameters(object[])

IL_0009: ldloc.0
IL_000A: ldloc.1
IL_000B: callvirt instance void LogEntrencyAttribute::OnEntry(class Metadata)
.try
{
    .try
    {
        
        
        IL_000C: call void Program.Program::Test__Inoculated()
        
        IL_000D: ldloc.1
        IL_000E: ldnull
        IL_000F: callvirt instance void [Inoculator.Injector]Inoculator.Builder.Metadata::set_ReturnValue(object)
        IL_0010: ldloc.0
        IL_0011: ldloc.1
        IL_0012: callvirt instance void LogEntrencyAttribute::OnSuccess(class Metadata)
        
        IL_0013: leave.s IL_0021
    } 
    catch [System.Runtime]System.Exception
    {
        IL_0014: stloc.s 4
        IL_0015: ldloc.1
        IL_0016: ldloc.s 4
        IL_0017: callvirt instance void [Inoculator.Injector]Inoculator.Builder.Metadata::set_Exception(class [System.Runtime]System.Exception)
        IL_0018: ldloc.0
        IL_0019: ldloc.1
        IL_001A: callvirt instance void LogEntrencyAttribute::OnException(class Metadata)
        IL_001B: ldloc.s 4
        IL_001C: throw
    } 
} 
finally
{
    IL_001D: ldloc.0
    IL_001E: ldloc.1
    IL_001F: callvirt instance void LogEntrencyAttribute::OnExit(class Metadata)
    IL_0020: endfinally
} 


IL_0021: ret
}
