using System.Text;

namespace Inoculator.Builder;
public static class HandlerTools {
    public static string GetNextLabel(ref int labelIdx, Dictionary<string, string> marks = null, string mark = null) {
        string label = $"IL_{labelIdx++:X4}";
        if (marks is not null && mark is not null) marks.Add(mark, label);
        return label;
    }

    public static string ExtractArguments(ParameterDecl.Parameter.Collection parameter, ref int labelIdx, int startingIdx) {
        StringBuilder builder = new StringBuilder();
        foreach(ParameterDecl.DefaultParameter param in parameter.Parameters.Values.OfType<ParameterDecl.DefaultParameter>()){
            builder.AppendLine(ExtractArgument(param, ref labelIdx, startingIdx++));
        }
        return builder.ToString();
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
                 {GetNextLabel(ref labelIdx)}: ldfld class [Inoculator.Injector]Inoculator.Builder.MethodData {classContainer}::'<inoculated>__Metadata'";
        
        string callCode = $"{GetNextLabel(ref labelIdx)}: callvirt instance void {interceptorClass}::{methodName}(class [Inoculator.Injector]Inoculator.Builder.MethodData)";
        return $"{piping}\n{callCode}";
    }

    public static string LoadArguments(ParameterDecl.Parameter.Collection parameter, ref int labelIdx, int startingIdx) {
        StringBuilder builder = new StringBuilder();
        foreach(ParameterDecl.DefaultParameter param in parameter.Parameters.Values.OfType<ParameterDecl.DefaultParameter>()){
            builder.AppendLine(LoadArgument(param, ref labelIdx, startingIdx++));
        }
        return builder.ToString();
    }

    
    
    
    public static string ExtractArgument(ParameterDecl.Parameter parameter, ref int labelIdx, int paramIdx = 0) {
        StringBuilder builder = new StringBuilder();
        if(parameter is not ParameterDecl.DefaultParameter param) {
            throw new Exception("Unknown parameter type");
        }
        
        var typeData = new TypeData(parameter);
        Console.WriteLine(typeData);
        var ilcode = typeData.IsVoid ? string.Empty  : $$$"""
            {{{GetNextLabel(ref labelIdx)}}}: dup
            {{{GetNextLabel(ref labelIdx)}}}: ldc.i4.s {{{paramIdx}}}
            {{{LoadArgument(param, ref labelIdx, paramIdx)}}}
            {{{(!typeData.IsByRef ? string.Empty
                    : $"{GetNextLabel(ref labelIdx)}: {GetCILIndirectLoadOpcode(typeData)}" 
            )}}}
            {{{( typeData.IsReferenceType ? String.Empty 
                    : $"{GetNextLabel(ref labelIdx)}: box {typeData.ToProperName}"
            )}}}
            {{{GetNextLabel(ref labelIdx)}}}: stelem.ref
            """;

        builder.Append(ilcode);
        return builder.ToString();
    }

    public static string LoadArgument(ParameterDecl.Parameter parameter, ref int labelIdx, int paramIdx = 0) {
        StringBuilder builder = new StringBuilder();
        if(parameter is not ParameterDecl.DefaultParameter param) {
            throw new Exception("Unknown parameter type");
        }
        var typeComp = param.TypeDeclaration?.ToString();
        var ilcode = typeComp is null ? String.Empty : $"{GetNextLabel(ref labelIdx)}: ldarg.s {param.Id}";
        builder.Append(ilcode);
        return builder.ToString();
    }

    public static string GetCILIndirectLoadOpcode(TypeData type) {
        return type.PureName switch {
            "int" => "ldind.i",
            "int8" => "ldind.i1",
            "int16" => "ldind.i2",
            "int32" => "ldind.i4",
            "int64" => "ldind.i8",
            "uint8" or "unsigned int8"  => "ldind.u1",
            "int16" or "unsigned int16" => "ldind.u2",
            "int32" or "unsigned int32" => "ldind.u4",
            "int64" or "unsigned int64" => "ldind.u8",
            "float32" => "ldind.r4",
            "float64" => "ldind.r8",
            _ when type.PureName.StartsWith("valuetype") => "ldobj",
            _ => "ldind.ref"
        };
    }
}