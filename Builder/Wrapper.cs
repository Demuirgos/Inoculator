using System.Extensions;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Inoculator.Core;
using MethodDecl;

namespace Inoculator.Builder;

public static class Wrap {
    static string getNextLabel(ref int labelIdx) => $"IL_{labelIdx++:X4}";
    public static MethodDecl.Method Handle(this MethodDecl.Method method) {
        int labelIdx = 0;
        StringBuilder builder = new();
        builder.AppendLine(".method");
        builder.AppendLine(method.Header.ToString());
        builder.AppendLine("{");
        foreach (var member in method.Body.Items.Values) {
            if(member is MethodDecl.LabelItem or MethodDecl.InstructionItem) continue;
            builder.AppendLine(member.ToString());
        }
        builder.Append($$$"""
        {{{getNextLabel(ref labelIdx)}}}: newobj instance void InterceptorAttribute::.ctor()
        {{{getNextLabel(ref labelIdx)}}}: stloc.0
        {{{getNextLabel(ref labelIdx)}}}: ldstr "{{{method.ToString().Trim(' ', '\0', '\n', '\r', '\t')}}}"
        {{{getNextLabel(ref labelIdx)}}}: newobj instance void Metadata::.ctor(string)
        {{{getNextLabel(ref labelIdx)}}}: stloc.1

        {{{getNextLabel(ref labelIdx)}}}: ldloc.1
        {{{getNextLabel(ref labelIdx)}}}: ldc.i4.{{{method.Header.Parameters.Parameters.Values.Length}}}
        {{{getNextLabel(ref labelIdx)}}}: newarr [System.Runtime]System.Object
        
        {{{ExtractArguments(method.Header.Parameters, ref labelIdx)}}}
        {{{getNextLabel(ref labelIdx)}}}: callvirt instance void Metadata::set_Parameters(object[])

        {{{getNextLabel(ref labelIdx)}}}: ldloc.0
        {{{getNextLabel(ref labelIdx)}}}: ldloc.1
        {{{getNextLabel(ref labelIdx)}}}: callvirt instance void InterceptorAttribute::OnEntry(class Metadata)
        .try
        {
            .try
            {
                {{{LoadArguments(method.Header.Parameters, ref labelIdx)}}}
                {{{getNextLabel(ref labelIdx)}}}: call {{{method.Header}}}
                {{{getNextLabel(ref labelIdx)}}}: stloc.2
                {{{getNextLabel(ref labelIdx)}}}: ldloc.1
                {{{getNextLabel(ref labelIdx)}}}: ldloc.2
                {{{getNextLabel(ref labelIdx)}}}: box [System.Runtime]System.Int32
                {{{getNextLabel(ref labelIdx)}}}: callvirt instance void Metadata::set_ReturnValue(object)
                {{{getNextLabel(ref labelIdx)}}}: ldloc.0
                {{{getNextLabel(ref labelIdx)}}}: ldloc.1
                {{{getNextLabel(ref labelIdx)}}}: callvirt instance void InterceptorAttribute::OnSuccess(class Metadata)
                {{{getNextLabel(ref labelIdx)}}}: ldloc.2
                {{{getNextLabel(ref labelIdx)}}}: stloc.3
                {{{getNextLabel(ref labelIdx)}}}: leave.s IL_007e
            } 
            catch [System.Runtime]System.Exception
            {
                {{{getNextLabel(ref labelIdx)}}}: stloc.s 4
                {{{getNextLabel(ref labelIdx)}}}: ldloc.1
                {{{getNextLabel(ref labelIdx)}}}: ldloc.s 4
                {{{getNextLabel(ref labelIdx)}}}: callvirt instance void Metadata::set_Exception(class [System.Runtime]System.Exception)
                {{{getNextLabel(ref labelIdx)}}}: ldloc.0
                {{{getNextLabel(ref labelIdx)}}}: ldloc.1
                {{{getNextLabel(ref labelIdx)}}}: callvirt instance void InterceptorAttribute::OnException(class Metadata)
                {{{getNextLabel(ref labelIdx)}}}: ldloc.s 4
                {{{getNextLabel(ref labelIdx)}}}: throw
            } 
        } 
        finally
        {
            {{{getNextLabel(ref labelIdx)}}}: ldloc.0
            {{{getNextLabel(ref labelIdx)}}}: ldloc.1
            {{{getNextLabel(ref labelIdx)}}}: callvirt instance void InterceptorAttribute::OnExit(class Metadata)
            {{{getNextLabel(ref labelIdx)}}}: endfinally
        } 

        {{{getNextLabel(ref labelIdx)}}}: ldloc.3
        {{{getNextLabel(ref labelIdx)}}}: ret

        """);
        builder.AppendLine("}");
        var result = builder.ToString().Replace("\0", "");
        Console.WriteLine(result);
        return Reader.Parse<MethodDecl.Method>(result) switch {
            Success<MethodDecl.Method, Exception> success => success.Value,
            Error<MethodDecl.Method, Exception> failure => throw new Exception("Wrap failed"),
        };
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
            builder.AppendLine(LoadArgument(param, ref labelIdx, paramIdx));
        }
        return builder.ToString();
    }

    public static string ExtractArgument(ParameterDecl.Parameter parameter, ref int labelIdx, int paramIdx = 0) {
        String[] _primitives = new String[] { "bool", "char", "float32", "float64", "int8", "int16", "int32", "int64", "uint8", "uint16", "uint32", "native" };
        StringBuilder builder = new StringBuilder();
        if(parameter is not ParameterDecl.DefaultParameter param) {
            throw new Exception("Unknown parameter type");
        }
        var typeComp = param.TypeDeclaration.Value.Types.Values.OfType<TypeDecl.TypePrimitive>().FirstOrDefault();
        Console.WriteLine(parameter);
        var ilcode = typeComp.TypeName switch {
            _ when _primitives.Contains(typeComp.TypeName) => $$$"""
                {{{getNextLabel(ref labelIdx)}}}: dup
                {{{getNextLabel(ref labelIdx)}}}: ldc.i4.{{{paramIdx}}}
                {{{getNextLabel(ref labelIdx)}}}: ldarg.{{{paramIdx + 1}}}
                {{{getNextLabel(ref labelIdx)}}}: box {{{typeComp.TypeName}}}
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
        String[] _primitives = new String[] { "bool", "char", "float32", "float64", "int8", "int16", "int32", "int64", "uint8", "uint16", "uint32", "native" };
        StringBuilder builder = new StringBuilder();
        if(parameter is not ParameterDecl.DefaultParameter param) {
            throw new Exception("Unknown parameter type");
        }
        var typeComp = param.TypeDeclaration.Value.Types.Values.OfType<TypeDecl.TypePrimitive>().FirstOrDefault();
        var ilcode = typeComp.TypeName switch {
            "object" => $$$"""
                {{{getNextLabel(ref labelIdx)}}}: ldarg.{{{paramIdx + 1}}}
                """,
            _ when _primitives.Contains(typeComp.TypeName) => $$$"""
                {{{getNextLabel(ref labelIdx)}}}: ldarg.{{{paramIdx + 1}}}
                """,
            _ => $$$"""
                {{{getNextLabel(ref labelIdx)}}}: ldarg.s {{{param.Id}}}
                """
        };

        builder.Append(ilcode);
        return builder.ToString();
    }
}