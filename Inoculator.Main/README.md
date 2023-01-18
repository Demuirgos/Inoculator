# Innoculator
An IL code Injector using Ilasm and Ildasm (WIP)
# Progress So far : 
  * Target function : 
  ```
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
  ```
  * Result function :
  ```
 .method public hidebysig static 
		int32 method_that_does_stuff (int32 arg1,object arg2,unsigned int8 arg3,class [System.Runtime] System.Collections.Generic.IEnumerable`1<string> arg4,string arg5) cil managed  
	{
		.custom instance void DummyInterceptorAttribute::.ctor (  )=(01000000)
		.maxstack 8
		.locals init (
			[0] class InterceptorAttribute interceptor,
			[1] class Metadata metadata,
			[2] int32 result,
			[3] int32 ,
			[4] class [System.Runtime] System.Exception e
		)

		IL_0000: newobj instance void [Inoculator.Injector] Inoculator.Attributes.InterceptorAttribute::.ctor (  )
		IL_0001: stloc.0 
		IL_0002: ldstr ".methodpublichidebysigstaticint32method_that_does_stuff(int32arg1,objectarg2,unsignedint8arg3,class[System.Runtime]System.Collections.Generic.IEnumerable`1<string>arg4,stringarg5)cilmanaged{.custominstancevoidDummyInterceptorAttribute::.ctor()=(01000000).maxstack8IL_0000:ldc.i4.s69IL_0002:ret}"
		IL_0003: newobj instance void [Inoculator.Injector] Inoculator.Builder.Metadata::.ctor ( string )
		IL_0004: stloc.1 
		IL_0005: ldloc.1 
		IL_0006: ldc.i4.5 
		IL_0007: newarr [System.Runtime] System.Object
		IL_0008: dup 
		IL_0009: ldc.i4.0 
		IL_000A: ldarg.1 
		IL_000B: box int32
		IL_000C: stelem.ref 
		IL_000D: dup 
		IL_000E: ldc.i4.0 
		IL_000F: ldarg.2 
		IL_0010: stelem.ref 
		IL_0011: dup 
		IL_0012: ldc.i4.0 
		IL_0013: ldarg.2 
		IL_0014: stelem.ref 
		IL_0015: dup 
		IL_0016: ldc.i4.0 
		IL_0017: ldarg.s arg5
		IL_0018: stelem.ref 
		IL_0019: callvirt instance void [Inoculator.Injector] Inoculator.Builder.Metadata::set_Parameters ( object [...] )
		IL_001A: ldloc.0 
		IL_001B: ldloc.1 
		IL_001C: callvirt instance void [Inoculator.Injector] Inoculator.Attributes.InterceptorAttribute::OnEntry ( class Metadata )
		.try {
			.try {
				IL_001E: ldarg.1 
				IL_001F: ldarg.2 
				IL_0020: ldarg.3 
				IL_0021: ldarg.s arg4
				IL_0022: ldarg.s arg5
				IL_0023: call int32 Experiment::method_that_does_stuff__Inoculated ( int32 , object , unsigned int8 , class [System.Runtime] System.Collections.Generic.IEnumerable`1<string> , string )
				IL_0024: stloc.2 
				IL_0025: ldloc.1 
				IL_0026: ldloc.2 
                IL_0026: box int32
				IL_0027: callvirt instance void [Inoculator.Injector] Inoculator.Builder.Metadata::set_ReturnValue ( object )
				IL_0028: ldloc.0 
				IL_0029: ldloc.1 
				IL_002A: callvirt instance void [Inoculator.Injector] Inoculator.Attributes.InterceptorAttribute::OnSuccess ( class Metadata )
				IL_002B: ldloc.2 
				IL_002C: stloc.3 
				IL_002D: leave.s IL_003B
			} catch [System.Runtime] System.Exception {
				IL_002E: stloc.s 4
				IL_002F: ldloc.1 
				IL_0030: ldloc.s 4
				IL_0031: callvirt instance void [Inoculator.Injector] Inoculator.Builder.Metadata::set_Exception ( class [System.Runtime] System.Exception )
				IL_0032: ldloc.0 
				IL_0033: ldloc.1 
				IL_0034: callvirt instance void [Inoculator.Injector] Inoculator.Attributes.InterceptorAttribute::OnException ( class Metadata )
				IL_0035: ldloc.s 4
				IL_0036: throw 
			}
		} finally {
			IL_0037: ldloc.0 
			IL_0038: ldloc.1 
			IL_0039: callvirt instance void [Inoculator.Injector] Inoculator.Attributes.InterceptorAttribute::OnExit ( class Metadata )
			IL_003A: endfinally 
		}
		IL_003B: ldloc.3 
		IL_003C: ret 
	}
  ```
# Strategy : 
  ```csharp
class parentClass {
    [InterceptorAttr] Output_T FunctionName(Input_T1 input, Input_T2 input, ...) {
        // body of function
        return output;
    } 
}

// becomes
class parentClass {
    private Output_T FunctionName_Old(Input_T1 input, Input_T2 input, ...) {
        // body of function
        return output;
    }
    
    public Output_T invoke(Input_T1 input, Input_T2 input) {
        var interceptor = new InterceptorAttribute();
        var metadata = new Metadata(Code);
        metadata.Parameters = new object[] { input, input, .. };
        interceptor.OnEntry(metadata);
        try {
            Output_T result = FunctionName_Old(input, input, ...);
            metadata.ReturnValue = result;
            interceptor.OnSuccess(metadata);
        } catch (Exception e) {
            metadata.Exception = e;
            interceptor.OnException(metadata);
        } finally {
            interceptor.OnExit(metadata);
        }
    }
}
``` 
