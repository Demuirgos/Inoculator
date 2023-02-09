using System.Text;

namespace Inoculator.Builder;
public static class HandlerTools {
    public static string GetNextLabel(ref int labelIdx, Dictionary<string, string> marks = null, string mark = null) {
        string label = $"IL_{labelIdx++:X4}";
        if (marks is not null && mark is not null) marks.Add(mark, label);
        return label;
    }

    public static string CallMethodOnInterceptors(string classContainer, string[] interceptorsClass, string methodName, bool isSyncMode, ref int labelIdx) 
    {
        StringBuilder builder = new StringBuilder();
        foreach (var interceptorClass in interceptorsClass)
        {
            builder.Append(CallMethodOnInterceptor(classContainer, interceptorClass, methodName, isSyncMode, ref labelIdx));
        }
        return builder.ToString();
    }

    public static string GenerateInterceptorName(string className) => $"'<inoculated>interceptor__{Math.Abs(className.GetHashCode()) % 1000:X3}'"; 

    public static string CallMethodOnInterceptor(string classContainer, string interceptorClass, string methodName, bool isSyncMode, ref int labelIdx) {
        string piping = isSyncMode 
            ? $@"{GetNextLabel(ref labelIdx)}: ldloc.s {GenerateInterceptorName(interceptorClass)}
                 {GetNextLabel(ref labelIdx)}: ldloc.s metadata"
            : $@"{GetNextLabel(ref labelIdx)}: ldarg.0
                 {GetNextLabel(ref labelIdx)}: ldfld class {interceptorClass} {classContainer}::{GenerateInterceptorName(interceptorClass)}
                 {GetNextLabel(ref labelIdx)}: ldarg.0
                 {GetNextLabel(ref labelIdx)}: ldfld class [Inoculator.Interceptors]Inoculator.Builder.MethodData {classContainer}::'<inoculated>__Metadata'";
        
        string callCode = $"{GetNextLabel(ref labelIdx)}: callvirt instance void class {interceptorClass}::{methodName}(class [Inoculator.Interceptors]Inoculator.Builder.MethodData)";
        return $"{piping}\n{callCode}";
    }

    public static string LoadArguments(ParameterDecl.Parameter.Collection parameter, ref int labelIdx, bool isStatic, bool includeLabels = true) {
        StringBuilder builder = new StringBuilder();
        if(!isStatic) {
            builder.AppendLine(LoadThisArgument(ref labelIdx, includeLabels));
        }

        foreach(ParameterDecl.DefaultParameter param in parameter.Parameters.Values.OfType<ParameterDecl.DefaultParameter>()){
            builder.AppendLine(LoadArgument(param, ref labelIdx, includeLabels));
        }
        return builder.ToString();
    }

    public static string ExtractArguments(MethodData methodAst, ref int labelIdx, bool isStatic, bool includeLabels = true) {
        StringBuilder builder = new StringBuilder();
        int startingIdx = isStatic ? 0 : 1;

        if(!isStatic) {
            builder.AppendLine(ExtractThisArgument(methodAst.ClassReference, ref labelIdx, includeLabels));
        }
        foreach(ParameterDecl.DefaultParameter param in methodAst.Code.Header.Parameters.Parameters.Values.OfType<ParameterDecl.DefaultParameter>()){
            builder.AppendLine(ExtractArgument(param, ref labelIdx, startingIdx++, includeLabels));
        }
        return builder.ToString();
    }

    public static string UpdateRefArguments(ParameterDecl.Parameter.Collection parameter, bool isStatic, ref int labelIdx) {
        StringBuilder builder = new StringBuilder();
        int startingIdx = isStatic ? 0 : 1;

        builder.AppendLine($"");
        foreach(ParameterDecl.DefaultParameter param in parameter.Parameters.Values.OfType<ParameterDecl.DefaultParameter>()){
            builder.AppendLine(UpdateRefArgument(param, ref labelIdx, startingIdx++));
        }
        return builder.ToString();
    }

    public static string ExtractThisArgument(ClassDecl.Prefix ClassHeader, ref int labelIdx, bool includeLabels = true) {
        StringBuilder builder = new StringBuilder();
        var IsValueType = ClassHeader.Implements is not null && ClassHeader.Implements.Types.ToString(string.Empty).Contains("System.ValueType");
        builder.Append($$$"""
            {{{(includeLabels ? $"{GetNextLabel(ref labelIdx)}:" : string.Empty)}}} dup
            {{{(includeLabels ? $"{GetNextLabel(ref labelIdx)}:" : string.Empty)}}} ldc.i4.s 0
            {{{(includeLabels ? $"{GetNextLabel(ref labelIdx)}:" : string.Empty)}}} ldarg.0
        """);
        if(IsValueType) {
            builder.Append($$$"""
            {{{(includeLabels ? $"{GetNextLabel(ref labelIdx)}:" : string.Empty)}}} ldobj {ClassHeader.Name}
            {{{(includeLabels ? $"{GetNextLabel(ref labelIdx)}:" : string.Empty)}}} box {ClassHeader.Name}
        """);
        }   
        builder.Append($$$"""
            {{{(includeLabels ? $"{GetNextLabel(ref labelIdx)}:" : string.Empty)}}} stelem.ref
        """);

        return builder.ToString();

    }
    public static string ExtractArgument(ParameterDecl.Parameter parameter, ref int labelIdx, int paramIdx = 0, bool includeLabels = true) {
        StringBuilder builder = new StringBuilder();
        if(parameter is not ParameterDecl.DefaultParameter param) {
            throw new Exception("Unknown parameter type");
        }
        
        var typeData = new TypeData(parameter);
        var ilcode = typeData.IsVoid ? string.Empty  : $$$"""
            {{{(includeLabels ? $"{GetNextLabel(ref labelIdx)}:" : string.Empty)}}} dup
            {{{(includeLabels ? $"{GetNextLabel(ref labelIdx)}:" : string.Empty)}}} ldc.i4.s {{{paramIdx}}}
            {{{LoadArgument(param, ref labelIdx, includeLabels)}}}
            {{{(!typeData.IsByRef ? string.Empty
                    : $"{(includeLabels ? $"{GetNextLabel(ref labelIdx)}:" : string.Empty)} {GetCILIndirectLoadOpcode(typeData).load}" 
            )}}}
            {{{( typeData.IsReferenceType ? String.Empty 
                    : $"{(includeLabels ? $"{GetNextLabel(ref labelIdx)}:" : string.Empty)} box {(typeData.IsGeneric ? typeData.FilteredName(true, false) : typeData.ToProperName)}"
            )}}}
            {{{(includeLabels ? $"{GetNextLabel(ref labelIdx)}:" : string.Empty)}}} stelem.ref
            """;

        builder.Append(ilcode);
        return builder.ToString();
    }

    public static string UpdateRefArgument(ParameterDecl.Parameter parameter, ref int labelIdx, int paramIdx = 0) {
        StringBuilder builder = new StringBuilder();
        if(parameter is not ParameterDecl.DefaultParameter param) {
            throw new Exception("Unknown parameter type");
        }
        
        var typeData = new TypeData(parameter);

        if(!typeData.IsByRef) {
            return string.Empty;
        }

        var ilcode = typeData.IsVoid ? string.Empty  : $$$"""
            {{{GetNextLabel(ref labelIdx)}}}: ldloc.s metadata
            {{{GetNextLabel(ref labelIdx)}}}: callvirt instance object[] [Inoculator.Interceptors]Inoculator.Builder.MethodData::get_Parameters()
            {{{GetNextLabel(ref labelIdx)}}}: ldc.i4.s {{{paramIdx}}}
            {{{LoadArgument(param, ref labelIdx)}}}
            {{{(!typeData.IsByRef ? string.Empty
                    : $"{GetNextLabel(ref labelIdx)}: {GetCILIndirectLoadOpcode(typeData).load}" 
            )}}}
            {{{( typeData.IsReferenceType ? String.Empty 
                    : $"{GetNextLabel(ref labelIdx)}: box {(typeData.IsGeneric ? typeData.FilteredName(true, false) : typeData.ToProperName)}"
            )}}}
            {{{GetNextLabel(ref labelIdx)}}}: stelem.ref
            """;

        builder.Append(ilcode);
        return builder.ToString();
    }

    public static bool HasField(ClassDecl.Class classRef, string fieldName) => classRef.Members.Members.Values.Any(x => x is ClassDecl.FieldDefinition field && field.Value.Id.ToString() == fieldName);

    public static string LoadThisArgument(ref int labelIdx, bool includeLabels = true) {
        StringBuilder builder = new StringBuilder();
        var ilcode = $"{(includeLabels ? $"{GetNextLabel(ref labelIdx)}:" : string.Empty)} ldarg.0";
        builder.Append(ilcode);
        return builder.ToString();
    }
    public static string LoadArgument(ParameterDecl.Parameter parameter, ref int labelIdx, bool includeLabels = true) {
        StringBuilder builder = new StringBuilder();
        if(parameter is not ParameterDecl.DefaultParameter param) {
            throw new Exception("Unknown parameter type");
        }
        var typeComp = param.TypeDeclaration?.ToString();
        var ilcode = typeComp is null ? String.Empty : $"{(includeLabels ? $"{GetNextLabel(ref labelIdx)}:" : string.Empty)} ldarg.s {param.Id}";
        builder.Append(ilcode);
        return builder.ToString();
    }

    public static (string load, string set) GetCILIndirectLoadOpcode(TypeData type) {
        return type.PureName switch {
            "int"   => ("ldind.i", "stind.i"),
            "int8"  => ("ldind.i1", "stind.i1"),
            "int16" => ("ldind.i2", "stind.i2"),
            "int32" => ("ldind.i4", "stind.i4"),
            "int64" => ("ldind.i8", "stind.i8"),
            "uint8" or "unsigned int8"  => ("ldind.u1", "stind.u1"),
            "int16" or "unsigned int16" => ("ldind.u2", "stind.u2"),
            "int32" or "unsigned int32" => ("ldind.u4", "stind.u4"),
            "int64" or "unsigned int64" => ("ldind.u8", "stind.u8"),
            "float32" => ("ldind.r4", "stind.r4"),
            "float64" => ("ldind.r8", "stind.r8"),
            _ when type.PureName.StartsWith("valuetype") => ("ldobj", "stobj"),
            _ => ("ldind.ref", "stind.ref")
        };
    }
}