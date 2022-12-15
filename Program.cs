// See https://aka.ms/new-console-template for more information
using System.Extensions;
using Inoculator.Core;

string fieldIl = @"
.class private auto ansi beforefieldinit testClass
    extends [System.Runtime]System.Object
{
    .custom instance void System.Runtime.CompilerServices.NullableContextAttribute::.ctor(uint8) = (
        01 00 01 00 00
    )
    .custom instance void System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8) = (
        01 00 00 00 00
    )

    .class nested private auto ansi beforefieldinit TestChild
        extends [System.Runtime]System.Object
    {
        .custom instance void System.Runtime.CompilerServices.NullableContextAttribute::.ctor(uint8) = (
            01 00 00 00 00
        )
        // Methods
        .method public hidebysig specialname rtspecialname 
            instance void .ctor () cil managed 
        {

            .maxstack 8

            IL_0000: ldarg.0
            IL_0001: call instance void [System.Runtime]System.Object::.ctor()
            IL_0006: nop
            IL_0007: ret
        } 

    } 

    .class nested private auto ansi sealed Test
        extends [System.Runtime]System.Enum
    {
        .custom instance void System.Runtime.CompilerServices.NullableContextAttribute::.ctor(uint8) = (
            01 00 00 00 00
        )

        .field public specialname rtspecialname int32 value__
        .field public static literal valuetype testClass/Test test1 = int32(0)
        .field public static literal valuetype testClass/Test test2 = int32(1)

    } 

    .field private string '<Name>k__BackingField'
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
        01 00 00 00
    )
    .custom instance void [System.Runtime]System.Diagnostics.DebuggerBrowsableAttribute::.ctor(valuetype [System.Runtime]System.Diagnostics.DebuggerBrowsableState) = (
        01 00 00 00 00 00 00 00
    )

    .method public hidebysig specialname 
        instance string get_Name () cil managed 
    {
        .custom instance void [System.Runtime]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
            01 00 00 00
        )

        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: ldfld string testClass::'<Name>k__BackingField'
        IL_0006: ret
    }

    .method public hidebysig specialname 
        instance void set_Name (
            string 'value'
        ) cil managed 
    {
        .custom instance void [System.Runtime]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
            01 00 00 00
        )

        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: ldarg.1
        IL_0002: stfld string testClass::'<Name>k__BackingField'
        IL_0007: ret
    } 

    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {

        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [System.Runtime]System.Object::.ctor()
        IL_0006: nop
        IL_0007: ret
    } 

    .property instance string Name()
    {
        .get instance string testClass::get_Name()
        .set instance void testClass::set_Name(string)
    }

} 
";

var tokens = fieldIl.Split(new char[] { ' ', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
int i = 0;
Result<Class, Exception> result = ClassParser.Parse(ref i, tokens);

if(result is Success<Class, Exception> success) {
    Console.WriteLine(success.Value);
} else {
    Console.WriteLine(result);
}