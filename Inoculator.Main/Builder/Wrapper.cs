using System.Extensions;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using IdentifierDecl;
using Inoculator.Core;
using MethodDecl;

namespace Inoculator.Builder;

public static class Wrapper {
    static string getNextLabel(ref int labelIdx) => $"IL_{labelIdx++:X4}";
    public static Result<MethodDecl.Method, Exception> Handle(this Metadata method, Identifier container, string[] AttributeClass)
    {
        int labelIdx = 0;
        StringBuilder builder = new();
        switch (method.MethodBehaviour)
        {
            case Metadata.MethodType.Sync:
                bool isVoidCall = !ReturnTypeOf(method.Code.Header, out var type);
                bool isPrimitive = isValueType(type);
                bool hasArgs = method.Code.Header.Parameters.Parameters.Values.Length > 0;
                labelIdx = HandleSyncMethod(method.Code, container, AttributeClass, labelIdx, builder, isVoidCall, type, isPrimitive, hasArgs, method.MethodCall == Metadata.CallType.Static);
                break;
            default:
                builder.Append(method.Code.ToString());
                break;
        }
        var result = builder.ToString();
        return Reader.Parse<MethodDecl.Method>(result);
    }

    private static int HandleSyncMethod(Method method, Identifier container, string[] AttributeClass, int labelIdx, StringBuilder builder, bool isVoidCall, string type, bool isPrimitive, bool hasArgs, bool isStatic)
    {
        (int metadataOffset, int? resultOffset, int? returnOffset, int exceptionOffset) = (AttributeClass.Length, isVoidCall ? null : (int?)AttributeClass.Length + 1, isVoidCall ? null : (int?)AttributeClass.Length + 2, AttributeClass.Length + (isVoidCall ? 1 : 3));
        builder.AppendLine($".method {method.Header} {{");
        foreach (var member in method.Body.Items.Values)
        {
            if (member is MethodDecl.LabelItem or MethodDecl.InstructionItem or MethodDecl.LocalsItem) continue;
            builder.AppendLine(member.ToString());
        }
        builder.AppendLine($".maxstack {(hasArgs ? 8 : 2)}");
        builder.AppendLine($$$"""
        .locals init (
            {{{String.Join("\n", AttributeClass.Select((attrClassName, i) => $"class {attrClassName} interceptor{i},"))}}}
            class [Inoculator.Injector]Inoculator.Builder.Metadata metadata,
            {{{(
                isVoidCall
                    ? String.Empty
                    : $@" {type} result,
                          {type},"
            )}}}
            class [System.Runtime]System.Exception e
        )
        """);

        builder.Append($$$"""
        {{{AttributeClass.Select(
                (attrClassName, i) => $@"
                {getNextLabel(ref labelIdx)}: newobj instance void {attrClassName}::.ctor()
                {getNextLabel(ref labelIdx)}: stloc.s {i}"
            ).Aggregate((a, b) => $"{a}\n{b}")}}}
        {{{getNextLabel(ref labelIdx)}}}: ldstr "{{{new string(method.ToString().ToCharArray().Select(c => c != '\n' ? c : ' ').ToArray())}}}"
        {{{getNextLabel(ref labelIdx)}}}: newobj instance void [Inoculator.Injector]Inoculator.Builder.Metadata::.ctor(string)
        {{{getNextLabel(ref labelIdx)}}}: stloc.s {{{metadataOffset}}}

        {{{getNextLabel(ref labelIdx)}}}: ldloc.s {{{metadataOffset}}}
        {{{getNextLabel(ref labelIdx)}}}: ldc.i4.{{{method.Header.Parameters.Parameters.Values.Length}}}
        {{{getNextLabel(ref labelIdx)}}}: newarr [System.Runtime]System.Object
        
        {{{ExtractArguments(method.Header.Parameters, ref labelIdx, isStatic ? 0 : 1)}}}
        {{{getNextLabel(ref labelIdx)}}}: callvirt instance void [Inoculator.Injector]Inoculator.Builder.Metadata::set_Parameters(object[])

        {{{AttributeClass.Select(
                (attrClassName, i) => $@"
                {getNextLabel(ref labelIdx)}: ldloc.s {i}
                {getNextLabel(ref labelIdx)}: ldloc.s {AttributeClass.Length}
                {getNextLabel(ref labelIdx)}: callvirt instance void {attrClassName}::OnEntry(class [Inoculator.Injector]Inoculator.Builder.Metadata)"
            ).Aggregate((a, b) => $"{a}\n{b}")}}}
        .try
        {
            .try
            {
                {{{(isStatic ? String.Empty : $@"{getNextLabel(ref labelIdx)}: ldarg.0")}}}
                {{{LoadArguments(method.Header.Parameters, ref labelIdx, isStatic ? 0 : 1)}}}
                {{{getNextLabel(ref labelIdx)}}}: call {{{MkMethodReference(method.Header, container)}}}
                {{{(
                    isVoidCall
                        ? String.Empty
                        : $@"{getNextLabel(ref labelIdx)}: stloc.s {resultOffset}"
                )}}}
                {{{getNextLabel(ref labelIdx)}}}: ldloc.s {{{metadataOffset}}}
                {{{(
                    isVoidCall
                        ? $@"{getNextLabel(ref labelIdx)}: ldnull"
                        : $@"{getNextLabel(ref labelIdx)}: ldloc.s {resultOffset}
                            {(isPrimitive
                                    ? $@"{getNextLabel(ref labelIdx)}: box {ToProperNamedType(type)}"
                                    : String.Empty
                            )}"
                )}}}
                {{{getNextLabel(ref labelIdx)}}}: callvirt instance void [Inoculator.Injector]Inoculator.Builder.Metadata::set_ReturnValue(object)
                {{{AttributeClass.Select(
                        (attrClassName, i) => $@"
                        {getNextLabel(ref labelIdx)}: ldloc.s {i}
                        {getNextLabel(ref labelIdx)}: ldloc.s {AttributeClass.Length}
                        {getNextLabel(ref labelIdx)}: callvirt instance void {attrClassName}::OnSuccess(class [Inoculator.Injector]Inoculator.Builder.Metadata)"
                    ).Aggregate((a, b) => $"{a}\n{b}")}}}
                {{{(
                    isVoidCall
                        ? String.Empty
                        : $@"{getNextLabel(ref labelIdx)}: ldloc.s {resultOffset}
                             {getNextLabel(ref labelIdx)}: stloc.s {returnOffset}"
                )}}}
                {{{getNextLabel(ref labelIdx)}}}: leave.s ***END***
            } 
            catch [System.Runtime]System.Exception
            {
                {{{getNextLabel(ref labelIdx)}}}: stloc.s {{{exceptionOffset}}}
                {{{getNextLabel(ref labelIdx)}}}: ldloc.s {{{metadataOffset}}}
                {{{getNextLabel(ref labelIdx)}}}: ldloc.s {{{exceptionOffset}}}
                {{{getNextLabel(ref labelIdx)}}}: callvirt instance void [Inoculator.Injector]Inoculator.Builder.Metadata::set_Exception(class [System.Runtime]System.Exception)
                {{{AttributeClass.Select(
                        (attrClassName, i) => $@"
                        {getNextLabel(ref labelIdx)}: ldloc.s {i}
                        {getNextLabel(ref labelIdx)}: ldloc.s {AttributeClass.Length}
                        {getNextLabel(ref labelIdx)}: callvirt instance void {attrClassName}::OnException(class [Inoculator.Injector]Inoculator.Builder.Metadata)"
                    ).Aggregate((a, b) => $"{a}\n{b}")}}}
                {{{getNextLabel(ref labelIdx)}}}: ldloc.s {{{exceptionOffset}}}
                {{{getNextLabel(ref labelIdx)}}}: throw
            } 
        } 
        finally
        {
            {{{AttributeClass.Select(
                    (attrClassName, i) => $@"
                    {getNextLabel(ref labelIdx)}: ldloc.s {i}
                    {getNextLabel(ref labelIdx)}: ldloc.s {AttributeClass.Length}
                    {getNextLabel(ref labelIdx)}: callvirt instance void {attrClassName}::OnExit(class [Inoculator.Injector]Inoculator.Builder.Metadata)"
                ).Aggregate((a, b) => $"{a}\n{b}")}}}
            {{{getNextLabel(ref labelIdx)}}}: endfinally
        } 

        {{{(isVoidCall ? String.Empty : $@"{getNextLabel(ref labelIdx)}: ldloc.s {returnOffset}")}}}
        {{{getNextLabel(ref labelIdx)}}}: ret

        """);

        string endLabel = $"IL_{labelIdx - (isVoidCall ? 1 : 2):X4}";
        builder.Replace("***END***", endLabel);
        builder.AppendLine("}");
        return labelIdx;
    }

    private static string InvokeFunctionOnTypes(string[] types, string functionSpec, int labelIdx) {
        string InvokeFunctionOnType(string type, string functionSpec) => $"{getNextLabel(ref labelIdx)}: callvirt instance void {type}::{functionSpec}";
        return types.Select(type => InvokeFunctionOnType(type, functionSpec)).Aggregate((a, b) => $"{a}\n{b}");
    }

    private static bool ReturnTypeOf(MethodDecl.Prefix header, out string type) {
        var typeComp = header.Type.Components.Types.Values.First().AsTypePrefix();
        Console.WriteLine(header);
        type = typeComp?.ToString() switch {
            "void" => null,
            _ => header.Type.ToString(),
        };
        return type != null;
    }

    private static string MkMethodReference(MethodDecl.Prefix Name, Identifier container) {
        // int32 Test::method_old(int32, object, uint8, class [System.Runtime]System.Collections.Generic.IEnumerable`1<string>, valuetype testE, string)
        var builder = new StringBuilder();
        builder.Append(Name.Type.ToString());
        if(container is not null) {
            builder.Append(" ");
            builder.Append(container.ToString());
            builder.Append("::");
        }
        builder.Append($"{Name.Name}__Inoculated");
        builder.Append("(");
        builder.Append(string.Join(", ", Name.Parameters.Parameters.Values.Select(x => x.ToString())));
        builder.Append(")");
        return builder.ToString();
    }

    public static string ExtractArguments(ParameterDecl.Parameter.Collection parameter, ref int labelIdx, int startingIdx) {
        StringBuilder builder = new StringBuilder();
        foreach(ParameterDecl.DefaultParameter param in parameter.Parameters.Values.OfType<ParameterDecl.DefaultParameter>()){
            builder.AppendLine(ExtractArgument(param, ref labelIdx, startingIdx++));
        }
        return builder.ToString();
    }

    public static string LoadArguments(ParameterDecl.Parameter.Collection parameter, ref int labelIdx, int startingIdx) {
        StringBuilder builder = new StringBuilder();
        foreach(ParameterDecl.DefaultParameter param in parameter.Parameters.Values.OfType<ParameterDecl.DefaultParameter>()){
            builder.AppendLine(LoadArgument(param, ref labelIdx, startingIdx++));
        }
        return builder.ToString();
    }

    static bool isValueType (string type) {
        String[] _primitives = new String[] { "bool", "char", "float32", "float64", "int8", "int16", "int32", "int64", "uint8", "uint16", "uint32", "native" };
        return _primitives.Contains(type) || type.StartsWith("valuetype");
    }    
    public static string ExtractArgument(ParameterDecl.Parameter parameter, ref int labelIdx, int paramIdx = 0) {
        StringBuilder builder = new StringBuilder();
        if(parameter is not ParameterDecl.DefaultParameter param) {
            throw new Exception("Unknown parameter type");
        }
        var typeComp = param.TypeDeclaration.Components.Types.Values.First().AsTypePrefix()?.AsTypePrimitive();
        var ilcode = typeComp is null ? string.Empty  : $$$"""
            {{{getNextLabel(ref labelIdx)}}}: dup
            {{{getNextLabel(ref labelIdx)}}}: ldc.i4.s {{{paramIdx}}}
            {{{LoadArgument(param, ref labelIdx, paramIdx)}}}
            {{{( !isValueType(typeComp.TypeName) ? String.Empty :
                    $"{getNextLabel(ref labelIdx)}: box {ToProperNamedType(typeComp.TypeName)}"
            )}}}
            {{{getNextLabel(ref labelIdx)}}}: stelem.ref
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
        var ilcode = typeComp is null ? String.Empty : $"{getNextLabel(ref labelIdx)}: ldarg.s {param.Id}";

        builder.Append(ilcode);
        return builder.ToString();
    }

    private static string ToProperNamedType(string type)
    {
        string ret = type;

        if (!type.Contains("[System.Runtime]"))
        {
            ret = "[System.Runtime]System.";
            switch (type.ToLower())
            {
                case "int32":
                    ret += "Int32";
                    break;
                case "int16":
                    ret += "Int16";
                    break;
                case "int64":
                    ret += "Int64";
                    break;
                case "uint32":
                    ret += "UInt32";
                    break;
                case "uint16":
                    ret += "UInt16";
                    break;
                case "uint64":
                    ret += "UInt64";
                    break;
                case "long":
                    ret += "Int64";
                    break;
                case "ulong":
                    ret += "UInt64";
                    break;
                case "short":
                    ret += "Int16";
                    break;
                case "ushort":
                    ret += "UInt16";
                    break;
                case "decimal":
                    ret += "Decimal";
                    break;
                case "string":
                    ret += "Object";
                    break;
                case "bool":
                    ret += "Boolean";
                    break;
                case "float64":
                    ret += "Double";
                    break;
                case "double":
                    ret += "Double";
                    break;
                case "float32":
                    ret += "Single";
                    break;
                case "object":
                    ret += "Object";
                    break;
                case "byte":
                    ret += "Byte";
                    break;
                case "sbyte":
                    ret += "SByte";
                    break;
                case "char":
                    ret += "Char";
                    break;
                default:
                    if (type.StartsWith("valuetype "))
                        ret = ret.Replace("valuetype ", "");
                    else
                        ret = type;
                    break;
            }
        }

        return ret;
    }
}