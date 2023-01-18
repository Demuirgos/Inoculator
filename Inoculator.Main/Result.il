.assembly extern System.Runtime  
{
.publickeytoken = ((B0 3F 5F 7F 11 D5 0A 3A))
.ver 7:0:0:0
}
.assembly extern Inoculator.Injector  
{
.ver 1:0:0:0
}
.assembly extern System.Console  
{
.publickeytoken = ((B0 3F 5F 7F 11 D5 0A 3A))
.ver 7:0:0:0
}
.assembly Inoculated.Test {
.custom instance void [System.Runtime] System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor ( int32 )=(0100080000000000)
.custom instance void [System.Runtime] System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::.ctor ()=(01000100540216577261704E6F6E457863657074696F6E5468726F777301)
.custom instance void [System.Runtime] System.Runtime.Versioning.TargetFrameworkAttribute::.ctor ( string )=(0100182E4E4554436F72654170702C56657273696F6E3D76372E300100540E144672616D65776F726B446973706C61794E616D65082E4E455420372E30)
.custom instance void [System.Runtime] System.Reflection.AssemblyCompanyAttribute::.ctor ( string )=(01000F496E6F63756C617465642E546573740000)
.custom instance void [System.Runtime] System.Reflection.AssemblyConfigurationAttribute::.ctor ( string )=(01000544656275670000)
.custom instance void [System.Runtime] System.Reflection.AssemblyFileVersionAttribute::.ctor ( string )=(010007312E302E302E300000)
.custom instance void [System.Runtime] System.Reflection.AssemblyInformationalVersionAttribute::.ctor ( string )=(010005312E302E300000)
.custom instance void [System.Runtime] System.Reflection.AssemblyProductAttribute::.ctor ( string )=(01000F496E6F63756C617465642E546573740000)
.custom instance void [System.Runtime] System.Reflection.AssemblyTitleAttribute::.ctor ( string )=(01000F496E6F63756C617465642E546573740000)
.hash algorithm 0x8004
.ver 1:0:0:0
}
.module  Inoculated.Test.dll
.custom instance void System.Runtime.CompilerServices.RefSafetyRulesAttribute::.ctor ( int32 )=(01000B0000000000)
.imagebase 0x400000
.file alignment 0x200
.stackreserve 0x100000
.subsystem 0x03
.corflags 0x01
.class private auto ansi sealed beforefieldinit Microsoft.CodeAnalysis.EmbeddedAttribute
	extends [System.Runtime] System.Attribute 
{
.custom instance void [System.Runtime] System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor ()=(01000000)
.custom instance void Microsoft.CodeAnalysis.EmbeddedAttribute::.ctor ()=(01000000)
.method public hidebysig specialname rtspecialname instance 
	void .ctor () cil managed  
{
.maxstack 8
IL_0000:
ldarg.0 
IL_0001:
call instance void [System.Runtime] System.Attribute::.ctor ()
IL_0006:
nop 
IL_0007:
ret 
}
}
.class private auto ansi sealed beforefieldinit System.Runtime.CompilerServices.NullableAttribute
	extends [System.Runtime] System.Attribute 
{
.custom instance void [System.Runtime] System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor ()=(01000000)
.custom instance void Microsoft.CodeAnalysis.EmbeddedAttribute::.ctor ()=(01000000)
.custom instance void [System.Runtime] System.AttributeUsageAttribute::.ctor ( valuetype [System.Runtime] System.AttributeTargets )=(0100846B0000020054020D416C6C6F774D756C7469706C6500540209496E6865726974656400)
.field public initonly unsigned int8 [] NullableFlags 
.method public hidebysig specialname rtspecialname instance 
	void .ctor (unsigned int8 A_1) cil managed  
{
.maxstack 8
IL_0000:
ldarg.0 
IL_0001:
call instance void [System.Runtime] System.Attribute::.ctor ()
IL_0006:
nop 
IL_0007:
ldarg.0 
IL_0008:
ldc.i4.1 
IL_0009:
newarr [System.Runtime] System.Byte
IL_000e:
dup 
IL_000f:
ldc.i4.0 
IL_0010:
ldarg.1 
IL_0011:
stelem.i1 
IL_0012:
stfld unsigned int8 [] System.Runtime.CompilerServices.NullableAttribute::NullableFlags
IL_0017:
ret 
}
.method public hidebysig specialname rtspecialname instance 
	void .ctor (unsigned int8 [] A_1) cil managed  
{
.maxstack 8
IL_0000:
ldarg.0 
IL_0001:
call instance void [System.Runtime] System.Attribute::.ctor ()
IL_0006:
nop 
IL_0007:
ldarg.0 
IL_0008:
ldarg.1 
IL_0009:
stfld unsigned int8 [] System.Runtime.CompilerServices.NullableAttribute::NullableFlags
IL_000e:
ret 
}
}
.class private auto ansi sealed beforefieldinit System.Runtime.CompilerServices.NullableContextAttribute
	extends [System.Runtime] System.Attribute 
{
.custom instance void [System.Runtime] System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor ()=(01000000)
.custom instance void Microsoft.CodeAnalysis.EmbeddedAttribute::.ctor ()=(01000000)
.custom instance void [System.Runtime] System.AttributeUsageAttribute::.ctor ( valuetype [System.Runtime] System.AttributeTargets )=(01004C140000020054020D416C6C6F774D756C7469706C6500540209496E6865726974656400)
.field public initonly unsigned int8 Flag 
.method public hidebysig specialname rtspecialname instance 
	void .ctor (unsigned int8 A_1) cil managed  
{
.maxstack 8
IL_0000:
ldarg.0 
IL_0001:
call instance void [System.Runtime] System.Attribute::.ctor ()
IL_0006:
nop 
IL_0007:
ldarg.0 
IL_0008:
ldarg.1 
IL_0009:
stfld unsigned int8 System.Runtime.CompilerServices.NullableContextAttribute::Flag
IL_000e:
ret 
}
}
.class private auto ansi sealed beforefieldinit System.Runtime.CompilerServices.RefSafetyRulesAttribute
	extends [System.Runtime] System.Attribute 
{
.custom instance void [System.Runtime] System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor ()=(01000000)
.custom instance void Microsoft.CodeAnalysis.EmbeddedAttribute::.ctor ()=(01000000)
.custom instance void [System.Runtime] System.AttributeUsageAttribute::.ctor ( valuetype [System.Runtime] System.AttributeTargets )=(010002000000020054020D416C6C6F774D756C7469706C6500540209496E6865726974656400)
.field public initonly int32 Version 
.method public hidebysig specialname rtspecialname instance 
	void .ctor (int32 A_1) cil managed  
{
.maxstack 8
IL_0000:
ldarg.0 
IL_0001:
call instance void [System.Runtime] System.Attribute::.ctor ()
IL_0006:
nop 
IL_0007:
ldarg.0 
IL_0008:
ldarg.1 
IL_0009:
stfld int32 System.Runtime.CompilerServices.RefSafetyRulesAttribute::Version
IL_000e:
ret 
}
}
.class public auto ansi beforefieldinit LogEntrencyAttribute
	extends [Inoculator.Injector] Inoculator.Attributes.InterceptorAttribute 
{
.custom instance void System.Runtime.CompilerServices.NullableContextAttribute::.ctor ( unsigned int8 )=(0100010000)
.custom instance void System.Runtime.CompilerServices.NullableAttribute::.ctor ( unsigned int8 )=(0100000000)
.method public hidebysig virtual instance 
	void OnEntry (class [Inoculator.Injector] Inoculator.Builder.Metadata 'method') cil managed  
{
.maxstack 8
IL_0000:
nop 
IL_0001:
ldstr "Started Method "
IL_0006:
ldarg.1 
IL_0007:
callvirt instance string [Inoculator.Injector] Inoculator.Builder.Metadata::get_Name ()
IL_000c:
call string [System.Runtime] System.String::Concat ( string , string )
IL_0011:
call void [System.Console] System.Console::WriteLine ( string )
IL_0016:
nop 
IL_0017:
ret 
}
.method public hidebysig virtual instance 
	void OnExit (class [Inoculator.Injector] Inoculator.Builder.Metadata 'method') cil managed  
{
.maxstack 8
IL_0000:
nop 
IL_0001:
ldstr "Finished Method "
IL_0006:
ldarg.1 
IL_0007:
callvirt instance string [Inoculator.Injector] Inoculator.Builder.Metadata::get_Name ()
IL_000c:
call string [System.Runtime] System.String::Concat ( string , string )
IL_0011:
call void [System.Console] System.Console::WriteLine ( string )
IL_0016:
nop 
IL_0017:
ret 
}
.method public hidebysig specialname rtspecialname instance 
	void .ctor () cil managed  
{
.maxstack 8
IL_0000:
ldarg.0 
IL_0001:
call instance void [Inoculator.Injector] Inoculator.Attributes.InterceptorAttribute::.ctor ()
IL_0006:
nop 
IL_0007:
ret 
}
}
.class private auto ansi beforefieldinit Program.Program
	extends [System.Runtime] System.Object 
{
.method private hidebysig static 
	void Main (string [] args) cil managed  
{
.entrypoint
.custom instance void System.Runtime.CompilerServices.NullableContextAttribute::.ctor ( unsigned int8 )=(0100010000)
.maxstack 8
IL_0000:
nop 
IL_0001:
ldstr "Hello World!"
IL_0006:
call void [System.Console] System.Console::WriteLine ( string )
IL_000b:
nop 
IL_000c:
ret 
}
.method public hidebysig static 
	void Test__Inoculated () cil managed  
{
.custom instance void LogEntrencyAttribute::.ctor ()=(01000000)
.maxstack 2
.locals init (
int32 V_0,
int32 V_1,
bool V_2
)
IL_0000:
nop 
IL_0001:
ldc.i4.0 
IL_0002:
stloc.0 
IL_0003:
ldc.i4.0 
IL_0004:
stloc.1 
IL_0005:
br.s IL_0011
IL_0007:
nop 
IL_0008:
ldloc.0 
IL_0009:
ldc.i4.1 
IL_000a:
add 
IL_000b:
stloc.0 
IL_000c:
nop 
IL_000d:
ldloc.1 
IL_000e:
ldc.i4.1 
IL_000f:
add 
IL_0010:
stloc.1 
IL_0011:
ldloc.1 
IL_0012:
ldc.i4.s 100
IL_0014:
clt 
IL_0016:
stloc.2 
IL_0017:
ldloc.2 
IL_0018:
brtrue.s IL_0007
IL_001a:
ldloc.0 
IL_001b:
call void [System.Console] System.Console::WriteLine ( int32 )
IL_0020:
nop 
IL_0021:
ret 
}
.method public hidebysig static 
	void Test () cil managed  
{
.custom instance void LogEntrencyAttribute::.ctor ()=(01000000)
.maxstack 2
.locals init (
class InterceptorAttribute interceptor,
class Metadata metadata,
class [System.Runtime] System.Exception e
)
IL_0000:
newobj instance void LogEntrencyAttribute::.ctor ()
IL_0001:
stloc.0 
IL_0002:
ldstr ".methodpublichidebysigstaticvoidTest()cilmanaged{.custominstancevoidLogEntrencyAttribute::.ctor()=(01000000).maxstack2.localsinit(int32V_0,int32V_1,boolV_2)IL_0000:nopIL_0001:ldc.i4.0IL_0002:stloc.0IL_0003:ldc.i4.0IL_0004:stloc.1IL_0005:br.sIL_0011IL_0007:nopIL_0008:ldloc.0IL_0009:ldc.i4.1IL_000a:addIL_000b:stloc.0IL_000c:nopIL_000d:ldloc.1IL_000e:ldc.i4.1IL_000f:addIL_0010:stloc.1IL_0011:ldloc.1IL_0012:ldc.i4.s100IL_0014:cltIL_0016:stloc.2IL_0017:ldloc.2IL_0018:brtrue.sIL_0007IL_001a:ldloc.0IL_001b:callvoid[System.Console]System.Console::WriteLine(int32)IL_0020:nopIL_0021:ret}"
IL_0003:
newobj instance void [Inoculator.Injector] Inoculator.Builder.Metadata::.ctor ( string )
IL_0004:
stloc.1 
IL_0005:
ldloc.1 
IL_0006:
ldc.i4.0 
IL_0007:
newarr [System.Runtime] System.Object
IL_0008:
callvirt instance void [Inoculator.Injector] Inoculator.Builder.Metadata::set_Parameters ( object [] )
IL_0009:
ldloc.0 
IL_000A:
ldloc.1 
IL_000B:
callvirt instance void LogEntrencyAttribute::OnEntry ( class Metadata )
.try {
.try {
IL_000C:
call void Program.Program::Test__Inoculated ()
IL_000D:
ldloc.1 
IL_000E:
ldnull 
IL_000F:
callvirt instance void [Inoculator.Injector] Inoculator.Builder.Metadata::set_ReturnValue ( object )
IL_0010:
ldloc.0 
IL_0011:
ldloc.1 
IL_0012:
callvirt instance void LogEntrencyAttribute::OnSuccess ( class Metadata )
IL_0013:
leave.s IL_0021
} catch [System.Runtime] System.Exception {
IL_0014:
stloc.s 4
IL_0015:
ldloc.1 
IL_0016:
ldloc.s 4
IL_0017:
callvirt instance void [Inoculator.Injector] Inoculator.Builder.Metadata::set_Exception ( class [System.Runtime] System.Exception )
IL_0018:
ldloc.0 
IL_0019:
ldloc.1 
IL_001A:
callvirt instance void LogEntrencyAttribute::OnException ( class Metadata )
IL_001B:
ldloc.s 4
IL_001C:
throw 
}
} finally {
IL_001D:
ldloc.0 
IL_001E:
ldloc.1 
IL_001F:
callvirt instance void LogEntrencyAttribute::OnExit ( class Metadata )
IL_0020:
endfinally 
}
IL_0021:
ret 
}
.method public hidebysig specialname rtspecialname instance 
	void .ctor () cil managed  
{
.maxstack 8
IL_0000:
ldarg.0 
IL_0001:
call instance void [System.Runtime] System.Object::.ctor ()
IL_0006:
nop 
IL_0007:
ret 
}
}