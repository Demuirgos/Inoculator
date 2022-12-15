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
        01 00 07 01 00 00 00 00
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

.class private auto ansi beforefieldinit testsspace.testClass
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
        .field public static literal valuetype testsspace.testClass/Test test1 = int32(0)
        .field public static literal valuetype testsspace.testClass/Test test2 = int32(1)

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
        IL_0001: ldfld string testsspace.testClass::'<Name>k__BackingField'
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
        IL_0002: stfld string testsspace.testClass::'<Name>k__BackingField'
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

    .event [System.Runtime]System.Action tests
    {
        .addon instance void testsspace.testClass::add_tests(class [System.Runtime]System.Action)
        .removeon instance void testsspace.testClass::remove_tests(class [System.Runtime]System.Action)
    }

    .property instance string Name()
    {
        .get instance string testsspace.testClass::get_Name()
        .set instance void testsspace.testClass::set_Name(string)
    }
}

.class private auto ansi sealed beforefieldinit Microsoft.CodeAnalysis.EmbeddedAttribute
    extends [System.Runtime]System.Attribute
{
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
        01 00 00 00
    )
    .custom instance void Microsoft.CodeAnalysis.EmbeddedAttribute::.ctor() = (
        01 00 00 00
    )
    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [System.Runtime]System.Attribute::.ctor()
        IL_0006: nop
        IL_0007: ret
    }

}

.class private auto ansi sealed beforefieldinit System.Runtime.CompilerServices.NullableAttribute
    extends [System.Runtime]System.Attribute
{
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
        01 00 00 00
    )
    .custom instance void Microsoft.CodeAnalysis.EmbeddedAttribute::.ctor() = (
        01 00 00 00
    )
    .custom instance void [System.Runtime]System.AttributeUsageAttribute::.ctor(valuetype [System.Runtime]System.AttributeTargets) = (
        01 00 84 6b 00 00 02 00 54 02 0d 41 6c 6c 6f 77
        4d 75 6c 74 69 70 6c 65 00 54 02 09 49 6e 68 65
        72 69 74 65 64 00
    )
    .field public initonly uint8[] NullableFlags

    .method public hidebysig specialname rtspecialname 
        instance void .ctor (
            uint8 ''
        ) cil managed 
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [System.Runtime]System.Attribute::.ctor()
        IL_0006: nop
        IL_0007: ldarg.0
        IL_0008: ldc.i4.1
        IL_0009: newarr [System.Runtime]System.Byte
        IL_000e: dup
        IL_000f: ldc.i4.0
        IL_0010: ldarg.1
        IL_0011: stelem.i1
        IL_0012: stfld uint8[] System.Runtime.CompilerServices.NullableAttribute::NullableFlags
        IL_0017: ret
    }

    .method public hidebysig specialname rtspecialname 
        instance void .ctor (
            uint8[] ''
        ) cil managed 
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [System.Runtime]System.Attribute::.ctor()
        IL_0006: nop
        IL_0007: ldarg.0
        IL_0008: ldarg.1
        IL_0009: stfld uint8[] System.Runtime.CompilerServices.NullableAttribute::NullableFlags
        IL_000e: ret
    }

}

.class private auto ansi sealed beforefieldinit System.Runtime.CompilerServices.NullableContextAttribute
    extends [System.Runtime]System.Attribute
{
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
        01 00 00 00
    )
    .custom instance void Microsoft.CodeAnalysis.EmbeddedAttribute::.ctor() = (
        01 00 00 00
    )
    .custom instance void [System.Runtime]System.AttributeUsageAttribute::.ctor(valuetype [System.Runtime]System.AttributeTargets) = (
        01 00 4c 14 00 00 02 00 54 02 0d 41 6c 6c 6f 77
        4d 75 6c 74 69 70 6c 65 00 54 02 09 49 6e 68 65
        72 69 74 65 64 00
    )
    .field public initonly uint8 Flag

    .method public hidebysig specialname rtspecialname 
        instance void .ctor (
            uint8 ''
        ) cil managed 
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [System.Runtime]System.Attribute::.ctor()
        IL_0006: nop
        IL_0007: ldarg.0
        IL_0008: ldarg.1
        IL_0009: stfld uint8 System.Runtime.CompilerServices.NullableContextAttribute::Flag
        IL_000e: ret
    } 

}

.class private auto ansi sealed beforefieldinit System.Runtime.CompilerServices.RefSafetyRulesAttribute
    extends [System.Runtime]System.Attribute
{
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
        01 00 00 00
    )
    .custom instance void Microsoft.CodeAnalysis.EmbeddedAttribute::.ctor() = (
        01 00 00 00
    )
    .custom instance void [System.Runtime]System.AttributeUsageAttribute::.ctor(valuetype [System.Runtime]System.AttributeTargets) = (
        01 00 02 00 00 00 02 00 54 02 0d 41 6c 6c 6f 77
        4d 75 6c 74 69 70 6c 65 00 54 02 09 49 6e 68 65
        72 69 74 65 64 00
    )
    .field public initonly int32 Version

    .method public hidebysig specialname rtspecialname 
        instance void .ctor (
            int32 ''
        ) cil managed 
    {
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [System.Runtime]System.Attribute::.ctor()
        IL_0006: nop
        IL_0007: ldarg.0
        IL_0008: ldarg.1
        IL_0009: stfld int32 System.Runtime.CompilerServices.RefSafetyRulesAttribute::Version
        IL_000e: ret
    }

}