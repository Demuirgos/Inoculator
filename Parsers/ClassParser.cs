
using System;
using System.Collections.Generic;
using System.Linq;
using System.Extensions;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.ComponentModel;

namespace Inoculator.Core;

public class ClassParser {
    public static void ParseClass(ref int index, ref string[] code, out Class type)
    {
        type = new Class();

        void ParseModifiers(ref int index, ref string[] code, ref Class type)
        {
            index++;
            List<String> modifiers = new List<String>();
            while(code[index + 1] is not "extends") {
                modifiers.Add(code[index]);
                index += 1;
            }
            type.Modifiers = modifiers.ToArray();
            type.Name = code[index++];
            index++;
            type.BaseClass = code[index++];
            if(code[index] == "implements") {
                index += 1;
                List<String> interfaces = new List<String>();
                while(code[index] != "{") {
                    if(code[index] == ".custom") {
                        index += 1; int count = 0;
                        while(code[index] != ")" && count != 2) {
                            index += 1;
                            if(code[index] == ")") {
                                count += 1;
                            }
                        }
                        index += 1;
                    }
                    interfaces.Add(code[index++].Replace(",", String.Empty));
                }
                type.Interfaces = interfaces.ToArray();
            }
            index += 1;
        }

        void ParseClasses (ref int index, ref string[] code, ref Class type)
        {
            List<Class> typedefs = new List<Class>();
            while(code[index] == ".class") {
                var attributeResult = ClassParser.Parse(ref index, code);
                switch(attributeResult) {
                    case Success<Class, Exception> success:
                        typedefs.Add(success.Value);
                        break;
                    default : break;
                }
            }
            type.TypeDefs = typedefs.ToArray();
        }

        void ParseAttributes (ref int index, ref string[] code, ref Class type)
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
            type.Attributes = attributes.ToArray();
        }

        void ParseFields (ref int index, ref string[] code, ref Class type)
        {
            List<Field> fields = new List<Field>();
            while(code[index] == ".field") {
                var attributeResult = FieldParser.Parse(ref index, code);
                switch(attributeResult) {
                    case Success<Field, Exception> success:
                        fields.Add(success.Value);
                        break;
                    default : break;
                }
            }
            type.Fields = fields.ToArray();
        }

        void ParseMethods (ref int index, ref string[] code, ref Class type)
        {
            List<Method> methods = new List<Method>();
            while(code[index] == ".method") {
                var attributeResult = MethodParser.Parse(ref index, code);
                switch(attributeResult) {
                    case Success<Method, Exception> success:
                        methods.Add(success.Value);
                        break;
                    default : break;
                }
            }
            type.Methods = methods.ToArray();
        }

        void ParseProperties (ref int index, ref string[] code, ref Class type)
        {
            List<Property> properties = new List<Property>();
            while(code[index] == ".property") {
                var attributeResult = PropertyParser.Parse(ref index, code);
                switch(attributeResult) {
                    case Success<Property, Exception> success:
                        properties.Add(success.Value);
                        break;
                    default : break;
                }
            }
            type.Properties = properties.ToArray();
        }

        void ParseEvents (ref int index, ref string[] code, ref Class type)
        {
            List<Event> events = new List<Event>();
            while(code[index] == ".event") {
                var attributeResult = EventParser.Parse(ref index, code);
                switch(attributeResult) {
                    case Success<Event, Exception> success:
                        events.Add(success.Value);
                        break;
                    default : break;
                }
            }
            type.Events = events.ToArray();
        }

        if(code[index] == ".class") {
            ParseModifiers(ref index, ref code, ref type);
            ParseAttributes(ref index, ref code, ref type);
            ParseClasses(ref index, ref code, ref type);
            ParseFields(ref index, ref code, ref type);
            ParseMethods(ref index, ref code, ref type);
            ParseProperties(ref index, ref code, ref type);
            ParseEvents(ref index, ref code, ref type);
            index++;
        }

    }
    public static Result<Class, Exception> Parse(ref int i, string[] tokens)
    {
        ParseClass(ref i, ref tokens, out Class type);
        return Success<Class, Exception>.From(type);
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
    .property instance string Name()
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