# Innoculator
An IL code Injector using Ilasm and Ildasm (WIP)

# Strategy (Normal) : 
  ```csharp
class parentClass {
    [InterceptorAttr] 
    private (Output_T|void) FunctionName(Input_T1 input, Input_T2 input, ...) {
        // body of function
        return output?;
    } 
}

// becomes
class parentClass {
    private (Output_T|void) FunctionName_Old(Input_T1 input, Input_T2 input, ...) {
        // body of function
        return output?;
    }
    
    public (Output_T|void) FunctionName(Input_T1 input, Input_T2 input) {
        var interceptor = new InterceptorAttribute();
        var metadata = new Metadata(Code);
        metadata.Parameters = new object[] { input, input, .. };
        interceptor.OnEntry(metadata);
        try {
            (Output_T result =)? FunctionName_Old(input, input, ...);
            metadata.ReturnValue = result;
            interceptor.OnSuccess(metadata);
	    return result?;
        } catch (Exception e) {
            metadata.Exception = e;
            interceptor.OnException(metadata);
        } finally {
            interceptor.OnExit(metadata);
        }
    }
}
``` 

# Strategy (StateMachine) : 
  ```csharp
class parentClass {
    [InterceptorAttr] IStateMachine<Output_T> FunctionName(Input_T1 input, Input_T2 input, ...) {
        // body of function
        return output;
    } 
}

// becomes
class parentClass {
    class parentClass {
    	private class '<FunctionName>_IStateMachine' {
	    // original fields
	    public Interceptor Interceptor_0;
	    public Metadata Metadata;
	    // rest of properties and methods and fields ...
	    private (void|bool) MoveNext_Old(Input_T1 input, Input_T2 input, ...) {
		// body of function
		return output?;
	    }
	    
	    private (void|bool) MoveNext(Input_T1 input, Input_T2 input, ...) {
		if(state == 0) {
		    Interceptor_0.OnEntry(Metadata);
		}
		r = MoveNext_Old();
		if(this.builder.Task.Result is not null) {
		    metadata.Output = this.builder.Task.Result;
		    Interceptor_0.OnSuccess(Metadata);
		} else {
		    metadata.Exception = this.builder.Task.Exception;
		    Interceptor_0.OnException(Metadata);
		}
		if(state == -2) {
		    Interceptor_0.OnExit(Metadata);
		}
	    }
	}
	
	[InterceptorAttr] IStateMachine<Output_T> FunctionName(Input_T1 input, Input_T2 input, ...) {
	    // body of function setup
	    resultStateMachine.Metadata = new Metadata();
            resultStateMachine.Metadata.Parameters = new object[] { input, input, .. };
            resultStateMachine.Metadata.Interceptor_0 = new Interceptor();
	    // body of function hooks
	    return output;
	} 
    }
}
``` 
