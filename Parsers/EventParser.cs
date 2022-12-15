
using System;
using System.Collections.Generic;
using System.Linq;
using System.Extensions;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.ComponentModel;

namespace Inoculator.Core;

public class EventParser {
    public static void ParseEvent(ref int index, ref string[] code, out Event eventProp)
    {
        eventProp = new Event();

        void ParseModifiers(ref int index, ref string[] code, ref Event eventProp)
        {
            List<String> modifiers = new List<String>();
            while(code[index + 2] != "{") 
            {
                modifiers.Add(code[index++]);
            }
            eventProp.Modifiers = modifiers.ToArray();
            eventProp.Type = code[index++];
            eventProp.Name = code[index++];
            index++;
        }

        void ParseAdder(ref int index, ref string[] code, ref Event eventProp)
        {
            if(code[index] != ".addon") {
                return;
            }
            index+=2;
            eventProp.Adder = code[index].Substring(0, code[index].IndexOf("("));
            index+=2;
        }

        void ParseRemover(ref int index, ref string[] code, ref Event eventProp)
        {
            if(code[index] != ".removeon") {
                return;
            }
            index+=2;
            eventProp.Remover = code[index].Substring(0, code[index].IndexOf("("));
            index+=2;
        }

        void ParseAttributes (ref int index, ref string[] code, ref Event eventProp)
        {
            List<String> attributes = new List<String>();
            while(code[index] == ".custom") {
                var attributeResult = AttributeParser.Parse(ref index, code);
                switch(attributeResult) {
                    case Success<String, Exception> success:
                        attributes.Add(success.Value);
                        break;
                    default : break;
                }
            }
            eventProp.Attributes = attributes.ToArray();
        }

        if(code[index++] == ".event") {
            ParseModifiers(ref index, ref code, ref eventProp);
            ParseAttributes(ref index, ref code, ref eventProp);
            ParseAdder(ref index, ref code, ref eventProp);
            ParseRemover(ref index, ref code, ref eventProp);
        } else eventProp = null;

    }
    public static Result<Event, Exception> Parse(ref int i, string[] tokens)
    {
        ParseEvent(ref i, ref tokens, out Event eventProp);
        if(eventProp == null) {
            return Error<Event, Exception>.From(new Exception("Failed to parse event"));
        }
        return Success<Event, Exception>.From(eventProp);
    }
}

/*

.class public auto ansi beforefieldinit test.TestChildIs
    extends test.TestBase
    implements .custom instance void System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8) = (
        01 00 00 00 00
    )
    [System.Runtime]System.IDisposable,
               .custom instance void System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8) = (
        01 00 00 00 00
    )
    test.ITest
{
    .custom instance void System.Runtime.CompilerServices.NullableContextAttribute::.ctor(uint8) = (
        01 00 02 00 00
    )
    .custom instance void System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8) = (
        01 00 00 00 00
    )
    // Fields
    .field private string '<Name>k__BackingField'
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
        01 00 00 00
    )
    .custom instance void [System.Runtime]System.Diagnostics.DebuggerBrowsableAttribute::.ctor(valuetype [System.Runtime]System.Diagnostics.DebuggerBrowsableState) = (
        01 00 00 00 00 00 00 00
    )

    // Methods
    .method public final hidebysig newslot virtual 
        instance void Dispose () cil managed 
    {
        // Method begins at RVA 0x20b4
        // Code size 2 (0x2)
        .maxstack 8

        IL_0000: nop
        IL_0001: ret
    } // end of method TestChildIs::Dispose

    .method public hidebysig specialname 
        instance string get_Name () cil managed 
    {
        .custom instance void [System.Runtime]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
            01 00 00 00
        )
        // Method begins at RVA 0x20b7
        // Code size 7 (0x7)
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: ldfld string test.TestChildIs::'<Name>k__BackingField'
        IL_0006: ret
    } // end of method TestChildIs::get_Name

    .method public hidebysig specialname 
        instance void set_Name (
            string 'value'
        ) cil managed 
    {
        .custom instance void [System.Runtime]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
            01 00 00 00
        )
        // Method begins at RVA 0x20bf
        // Code size 8 (0x8)
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: ldarg.1
        IL_0002: stfld string test.TestChildIs::'<Name>k__BackingField'
        IL_0007: ret
    } // end of method TestChildIs::set_Name

    .method public hidebysig 
        instance object Function (
            object test,
            object test2
        ) cil managed 
    {
        .custom instance void System.Runtime.CompilerServices.NullableContextAttribute::.ctor(uint8) = (
            01 00 01 00 00
        )
        // Method begins at RVA 0x20c8
        // Code size 7 (0x7)
        .maxstack 1
        .locals init (
            [0] object
        )

        IL_0000: nop
        IL_0001: ldarg.1
        IL_0002: stloc.0
        IL_0003: br.s IL_0005

        IL_0005: ldloc.0
        IL_0006: ret
    } // end of method TestChildIs::Function

    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        // Method begins at RVA 0x20ab
        // Code size 8 (0x8)
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void test.TestBase::.ctor()
        IL_0006: nop
        IL_0007: ret
    } // end of method TestChildIs::.ctor

    // Properties
    .eventProp instance string Name()
    {
        .get instance string test.TestChildIs::get_Name()
        .set instance void test.TestChildIs::set_Name(string)
    }

} // end of class test.TestChildIs

*/

/*
using System;
using System.Reflection;
class Name {
[Interceptor]
int testOverwritten() {
    int j = 0;
    Point X = new Point{
        x = 23
        };
    Console.WriteLine("Hello World");
    return j; 
}

[Interceptor]
static void statictestOverwritten2() {
    Console.WriteLine("Hello World");
}

[Interceptor]
private Object testOverwritten3() {
    Console.WriteLine("Hello World");
    return new object(); 
}


int test() {
    var InterceptorAttribute = new InterceptorAttribute();
    var method = new MethodMetadata{
        MethodName = nameof(testOverwritten)
        };
    InterceptorAttribute.OnEntry(method);
    try {
        method.ReturnValue = testOverwritten();
        InterceptorAttribute.OnSuccess(method);
        return (int)method.ReturnValue;
    } catch (Exception e) {
        method.Exception = e;
        InterceptorAttribute.OnException(method);
    } finally {
        InterceptorAttribute.OnExit(method);
    }
    return default;
}        


public class MethodMetadata {
    public String MethodName { get; set; }
    public String[] Attributes { get; set; } 
    public object[] Parameters { get; set; }
    public object ReturnValue { get; set; }
    public Exception? Exception { get; set; }
}

public class InterceptorAttribute : Attribute {
    public InterceptorAttribute() { }
    public void OnEntry(MethodMetadata method) {}
    public void OnException(MethodMetadata method) {}
    public void OnSuccess(MethodMetadata method) {}
    public void OnExit(MethodMetadata method) {}

}
    
public struct Point {
    public int x;
}
}
*/