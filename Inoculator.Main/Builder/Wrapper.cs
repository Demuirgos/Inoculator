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
    public static Result<MethodDecl.Method, Exception> Handle(this MethodDecl.Method method, Identifier container, string[] AttributeClass) {
        int labelIdx = 0;
        StringBuilder builder = new();
        builder.AppendLine($".method {method.Header} {{");
        foreach (var member in method.Body.Items.Values) {
            if(member is MethodDecl.LabelItem or MethodDecl.InstructionItem or MethodDecl.LocalsItem) continue;
            builder.AppendLine(member.ToString());
        }
        bool isVoidCall = !ReturnTypeOf(method.Header, out var type);
        bool isPrimitive = _primitives.Contains(type);
        bool isStatic = method.Header.Convention is null || method.Header.MethodAttributes.Attributes.Values.Any(a => a is AttributeDecl.MethodSimpleAttribute { Name: "static" });

        builder.AppendLine($$$"""
        .locals init (
            {{{
                String.Join("\n", AttributeClass.Select((attrClassName, i) => $"class {attrClassName} interceptor{i},"))
            }}}
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
        {{{
            AttributeClass.Select(
                (attrClassName, i) => $@"
                {getNextLabel(ref labelIdx)}: newobj instance void {attrClassName}::.ctor()
                {getNextLabel(ref labelIdx)}: stloc.{i}"
            ).Aggregate((a, b) => $"{a}\n{b}")
        }}}
        {{{getNextLabel(ref labelIdx)}}}: ldstr "{{{new string(method.ToString().ToCharArray().Where(c => !Char.IsWhiteSpace(c)).ToArray())}}}"
        {{{getNextLabel(ref labelIdx)}}}: newobj instance void [Inoculator.Injector]Inoculator.Builder.Metadata::.ctor(string)
        {{{getNextLabel(ref labelIdx)}}}: stloc.{{{AttributeClass.Length}}}

        {{{getNextLabel(ref labelIdx)}}}: ldloc.{{{AttributeClass.Length}}}
        {{{getNextLabel(ref labelIdx)}}}: ldc.i4.{{{method.Header.Parameters.Parameters.Values.Length}}}
        {{{getNextLabel(ref labelIdx)}}}: newarr [System.Runtime]System.Object
        
        {{{ExtractArguments(method.Header.Parameters, ref labelIdx)}}}
        {{{getNextLabel(ref labelIdx)}}}: callvirt instance void [Inoculator.Injector]Inoculator.Builder.Metadata::set_Parameters(object[])

        {{{
            AttributeClass.Select(
                (attrClassName, i) => $@"
                {getNextLabel(ref labelIdx)}: ldloc.{i}
                {getNextLabel(ref labelIdx)}: ldloc.{AttributeClass.Length}
                {getNextLabel(ref labelIdx)}: callvirt instance void {attrClassName}::OnEntry(class [Inoculator.Injector]Inoculator.Builder.Metadata)"
            ).Aggregate((a, b) => $"{a}\n{b}")
        }}}
        .try
        {
            .try
            {
                {{{(isStatic ? String.Empty : $@"{getNextLabel(ref labelIdx)}: ldarg.0")}}}
                {{{LoadArguments(method.Header.Parameters, ref labelIdx)}}}
                {{{getNextLabel(ref labelIdx)}}}: call {{{MkMethodReference(method.Header, container)}}}
                {{{(
                    isVoidCall 
                        ? String.Empty 
                        : $@"{getNextLabel(ref labelIdx)}: stloc.2"
                )}}}
                {{{getNextLabel(ref labelIdx)}}}: ldloc.1
                {{{(
                    isVoidCall 
                        ? $@"{getNextLabel(ref labelIdx)}: ldnull"
                        : $@"{getNextLabel(ref labelIdx)}: ldloc.2
                            {(  isPrimitive 
                                    ? $@"{getNextLabel(ref labelIdx)}: box {ToProperNamedType(type)}"
                                    : String.Empty
                            )}"
                )}}}
                {{{getNextLabel(ref labelIdx)}}}: callvirt instance void [Inoculator.Injector]Inoculator.Builder.Metadata::set_ReturnValue(object)
                {{{
                    AttributeClass.Select(
                        (attrClassName, i) => $@"
                        {getNextLabel(ref labelIdx)}: ldloc.{i}
                        {getNextLabel(ref labelIdx)}: ldloc.{AttributeClass.Length}
                        {getNextLabel(ref labelIdx)}: callvirt instance void {attrClassName}::OnSuccess(class [Inoculator.Injector]Inoculator.Builder.Metadata)"
                    ).Aggregate((a, b) => $"{a}\n{b}")
                }}}
                {{{(
                    isVoidCall 
                        ? String.Empty 
                        : $@"{getNextLabel(ref labelIdx)}: ldloc.2
                             {getNextLabel(ref labelIdx)}: stloc.3"
                )}}}
                {{{getNextLabel(ref labelIdx)}}}: leave.s ***END***
            } 
            catch [System.Runtime]System.Exception
            {
                {{{getNextLabel(ref labelIdx)}}}: stloc.s 4
                {{{getNextLabel(ref labelIdx)}}}: ldloc.1
                {{{getNextLabel(ref labelIdx)}}}: ldloc.s 4
                {{{getNextLabel(ref labelIdx)}}}: callvirt instance void [Inoculator.Injector]Inoculator.Builder.Metadata::set_Exception(class [System.Runtime]System.Exception)
                {{{
                    AttributeClass.Select(
                        (attrClassName, i) => $@"
                        {getNextLabel(ref labelIdx)}: ldloc.{i}
                        {getNextLabel(ref labelIdx)}: ldloc.{AttributeClass.Length}
                        {getNextLabel(ref labelIdx)}: callvirt instance void {attrClassName}::OnException(class [Inoculator.Injector]Inoculator.Builder.Metadata)"
                    ).Aggregate((a, b) => $"{a}\n{b}")
                }}}
                {{{getNextLabel(ref labelIdx)}}}: ldloc.s 4
                {{{getNextLabel(ref labelIdx)}}}: throw
            } 
        } 
        finally
        {
            {{{
                AttributeClass.Select(
                    (attrClassName, i) => $@"
                    {getNextLabel(ref labelIdx)}: ldloc.{i}
                    {getNextLabel(ref labelIdx)}: ldloc.{AttributeClass.Length}
                    {getNextLabel(ref labelIdx)}: callvirt instance void {attrClassName}::OnExit(class [Inoculator.Injector]Inoculator.Builder.Metadata)"
                ).Aggregate((a, b) => $"{a}\n{b}")
            }}}
            {{{getNextLabel(ref labelIdx)}}}: endfinally
        } 

        {{{( isVoidCall ? String.Empty : $@"{getNextLabel(ref labelIdx)}: ldloc.{AttributeClass.Length+2}" )}}}
        {{{getNextLabel(ref labelIdx)}}}: ret

        """);

        string endLabel = $"IL_{labelIdx - (isVoidCall ? 1 : 2):X4}";
        builder.Replace("***END***", endLabel);
        builder.AppendLine("}");
        var result = builder.ToString();
        return Reader.Parse<MethodDecl.Method>(result);
    }

    private static string InvokeFunctionOnTypes(string[] types, string functionSpec, int labelIdx) {
        string InvokeFunctionOnType(string type, string functionSpec) => $"{getNextLabel(ref labelIdx)}: callvirt instance void {type}::{functionSpec}";
        return types.Select(type => InvokeFunctionOnType(type, functionSpec)).Aggregate((a, b) => $"{a}\n{b}");
    }

    private static bool ReturnTypeOf(MethodDecl.Prefix header, out string type) {
        var typeComp = header.Type.Components.Types.Values.First().AsTypePrefix()?.AsTypePrimitive();
        type = typeComp.TypeName switch {
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

    public static string ExtractArguments(ParameterDecl.Parameter.Collection parameter, ref int labelIdx) {
        StringBuilder builder = new StringBuilder();
        int paramIdx = 0;
        foreach(ParameterDecl.DefaultParameter param in parameter.Parameters.Values.OfType<ParameterDecl.DefaultParameter>()){
            builder.AppendLine(ExtractArgument(param, ref labelIdx, paramIdx));
        }
        return builder.ToString();
    }

    public static string LoadArguments(ParameterDecl.Parameter.Collection parameter, ref int labelIdx) {
        StringBuilder builder = new StringBuilder();
        int paramIdx = 0;
        foreach(ParameterDecl.DefaultParameter param in parameter.Parameters.Values.OfType<ParameterDecl.DefaultParameter>()){
            builder.AppendLine(LoadArgument(param, ref labelIdx, paramIdx++));
        }
        return builder.ToString();
    }

    static String[] _primitives = new String[] { "bool", "char", "float32", "float64", "int8", "int16", "int32", "int64", "uint8", "uint16", "uint32", "native" };
    public static string ExtractArgument(ParameterDecl.Parameter parameter, ref int labelIdx, int paramIdx = 0) {
        StringBuilder builder = new StringBuilder();
        if(parameter is not ParameterDecl.DefaultParameter param) {
            throw new Exception("Unknown parameter type");
        }
        var typeComp = param.TypeDeclaration.Components.Types.Values.First().AsTypePrefix()?.AsTypePrimitive();
        var ilcode = typeComp is null ? string.Empty  : typeComp.TypeName switch {
                _ when _primitives.Contains(typeComp.TypeName) => $$$"""
                    {{{getNextLabel(ref labelIdx)}}}: dup
                    {{{getNextLabel(ref labelIdx)}}}: ldc.i4.{{{paramIdx}}}
                    {{{getNextLabel(ref labelIdx)}}}: ldarg.{{{paramIdx + 1}}}
                    {{{getNextLabel(ref labelIdx)}}}: box {{{ToProperNamedType(typeComp.TypeName)}}}
                    {{{getNextLabel(ref labelIdx)}}}: stelem.ref
                    """,
                _ => $$$"""
                    {{{getNextLabel(ref labelIdx)}}}: dup
                    {{{getNextLabel(ref labelIdx)}}}: ldc.i4.{{{paramIdx}}}
                    {{{LoadArgument(param, ref labelIdx, paramIdx + 1)}}}
                    {{{getNextLabel(ref labelIdx)}}}: stelem.ref
                    """
            };

        builder.Append(ilcode);
        return builder.ToString();
    }

    public static string LoadArgument(ParameterDecl.Parameter parameter, ref int labelIdx, int paramIdx = 0) {
        String[] _primitives = new String[] { "bool", "char", "float32", "float64", "int8", "int16", "int32", "int64", "uint8", "uint16", "uint32", "unsigned int8", "unsigned int16", "unsigned int32" , "native" };
        StringBuilder builder = new StringBuilder();
        if(parameter is not ParameterDecl.DefaultParameter param) {
            throw new Exception("Unknown parameter type");
        }
        var typeComp = param.TypeDeclaration?.ToString();
        var ilcode = typeComp is null ? String.Empty : typeComp switch {
            "object" => $$$"""
                {{{getNextLabel(ref labelIdx)}}}: ldarg.{{{paramIdx + 1}}}
                """,
            _ when _primitives.Contains(typeComp) => $$$"""
                {{{getNextLabel(ref labelIdx)}}}: ldarg.{{{paramIdx + 1}}}
                """,
            _ => $$$"""
                {{{getNextLabel(ref labelIdx)}}}: ldarg.s {{{param.Id}}}
                """
        };

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