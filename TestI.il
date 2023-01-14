.assembly _
{
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = (
        01 00 08 00 00 00 00 00
    )
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::.ctor() = (
        01 00 01 00 54 02 16 57 72 61 70 4e 6f 6e 45 78
        63 65 70 74 69 6f 6e 54 68 72 6f 77 73 01
    )
    .custom instance void [System.Runtime]System.Diagnostics.DebuggableAttribute::.ctor(valuetype [System.Runtime]System.Diagnostics.DebuggableAttribute/DebuggingModes) = (
        01 00 02 00 00 00 00 00
    )
    .permissionset reqmin = (
        2e 01 80 8a 53 79 73 74 65 6d 2e 53 65 63 75 72
        69 74 79 2e 50 65 72 6d 69 73 73 69 6f 6e 73 2e
        53 65 63 75 72 69 74 79 50 65 72 6d 69 73 73 69
        6f 6e 41 74 74 72 69 62 75 74 65 2c 20 53 79 73
        74 65 6d 2e 52 75 6e 74 69 6d 65 2c 20 56 65 72
        73 69 6f 6e 3d 37 2e 30 2e 30 2e 30 2c 20 43 75
        6c 74 75 72 65 3d 6e 65 75 74 72 61 6c 2c 20 50
        75 62 6c 69 63 4b 65 79 54 6f 6b 65 6e 3d 62 30
        33 66 35 66 37 66 31 31 64 35 30 61 33 61 15 01
        54 02 10 53 6b 69 70 56 65 72 69 66 69 63 61 74
        69 6f 6e 01
    )
    .hash algorithm 0x00008004 
    .ver 0:0:0:0
}

.class private auto ansi '<Module>'
{
} 

.class public auto ansi beforefieldinit Experiment
    extends [System.Runtime]System.Object
{
    
    .method public hidebysig static 
        int32 method_that_does_stuff (
            int32 arg1,
            object arg2,
            uint8 arg3,
            class [System.Runtime]System.Collections.Generic.IEnumerable`1<string> arg4,
            string arg5
        ) cil managed 
    {
        .custom instance void DummyInterceptorAttribute::.ctor() = (
            01 00 00 00
        )
        
        .maxstack 8

        IL_0000: ldc.i4.s 69
        IL_0002: ret
    } 

    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [System.Runtime]System.Object::.ctor()
        IL_0006: ret
    } 

} 

.class public auto ansi beforefieldinit DummyInterceptorAttribute
    extends [Inoculator.Injector]Inoculator.Attributes.InterceptorAttribute
{
    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [Inoculator.Injector]Inoculator.Attributes.InterceptorAttribute::.ctor()
        IL_0006: ret
    } 

    .method public hidebysig newslot virtual 
        instance void OnEntry (
            class Metadata 'method'
        ) cil managed 
    {
        .maxstack 8

        IL_0000: ret
    } 

    .method public hidebysig newslot virtual 
        instance void OnException (
            class Metadata 'method'
        ) cil managed 
    {
        .maxstack 8

        IL_0000: ret
    } 

    .method public hidebysig newslot virtual 
        instance void OnSuccess (
            class Metadata 'method'
        ) cil managed 
    {
        .maxstack 8

        IL_0000: ret
    } 

    .method public hidebysig newslot virtual 
        instance void OnExit (
            class Metadata 'method'
        ) cil managed 
    {
        .maxstack 8

        IL_0000: ret
    } 

} 