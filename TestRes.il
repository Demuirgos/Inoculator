.method public hidebysig specialname instance 
	void set_Name__Inoculated (string 'value') cil managed  
{
	.custom instance void [System.Runtime] System.Runtime.CompilerServices.InterceptorAttribute::.ctor (  )=(01000000)
	.maxstack 8
	
	IL_0000: ldarg.0 
	IL_0001: ldarg.1 
	IL_0002: stfld string testsspace.testClass::'<Name>k__BackingField'
	IL_0007: ret 
}

.method public hidebysig specialname instance 
	void set_Name (string 'value') cil managed  
{
	.custom instance void [System.Runtime] System.Runtime.CompilerServices.InterceptorAttribute::.ctor (  )=(01000000)
	.maxstack 8
	.locals init (
		[0] class InterceptorAttribute interceptor,
		[1] class Metadata metadata,
		[2] class [System.Runtime] System.Exception e
	)
	
	IL_0000: newobj instance void InterceptorAttribute::.ctor (  )
	IL_0001: stloc.0 
	IL_0002: ldstr ".methodpublichidebysigspecialnameinstancevoidset_Name(string'value')cilmanaged{.custominstancevoid[System.Runtime]System.Runtime.CompilerServices.InterceptorAttribute::.ctor()=(01000000).maxstack8IL_0000:ldarg.0IL_0001:ldarg.1IL_0002:stfldstringtestsspace.testClass::'<Name>k__BackingField'IL_0007:ret}"
	IL_0003: newobj instance void Metadata::.ctor ( string )
	IL_0004: stloc.1 
	IL_0005: ldloc.1 
	IL_0006: ldc.i4.1 
	IL_0007: newarr [System.Runtime] System.Object
	IL_0008: dup 
	IL_0009: ldc.i4.0 
	IL_000A: ldarg.s 'value'
	IL_000B: stelem.ref 
	IL_000C: callvirt instance void Metadata::set_Parameters ( object [...] )
	IL_000D: ldloc.0 
	IL_000E: ldloc.1 
	IL_000F: callvirt instance void InterceptorAttribute::OnEntry ( class Metadata )
	.try {
		.try {
			IL_0010: ldarg.s 'value'
			IL_0011: call void testsspace.testClass::set_Name__Inoculated ( string )
			IL_0012: ldloc.1 
			IL_0013: ldnull 
			IL_0014: callvirt instance void Metadata::set_ReturnValue ( object )
			IL_0015: ldloc.0 
			IL_0016: ldloc.1 
			IL_0017: callvirt instance void InterceptorAttribute::OnSuccess ( class Metadata )
			IL_0018: leave.s IL_0026
		} catch [System.Runtime] System.Exception {
			IL_0019: stloc.s 4
			IL_001A: ldloc.1 
			IL_001B: ldloc.s 4
			IL_001C: callvirt instance void Metadata::set_Exception ( class [System.Runtime] System.Exception )
			IL_001D: ldloc.0 
			IL_001E: ldloc.1 
			IL_001F: callvirt instance void InterceptorAttribute::OnException ( class Metadata )
			IL_0020: ldloc.s 4
			IL_0021: throw 
		}
	} finally {
		IL_0022: ldloc.0 
		IL_0023: ldloc.1 
		IL_0024: callvirt instance void InterceptorAttribute::OnExit ( class Metadata )
		IL_0025: endfinally 
	}
	IL_0026: ret 
}
