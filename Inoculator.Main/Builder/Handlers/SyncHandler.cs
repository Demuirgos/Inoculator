namespace Inoculator.Builder;

using System.Extensions;
using System.Text;
using Inoculator.Core;
using static Inoculator.Builder.HandlerTools;

public static class SyncRewriter {
    public static Result<(ClassDecl.Class, MethodDecl.Method[]), Exception> Rewrite(MethodData metadata, string[] attributeName)
    {
        var newMethod = Handle(metadata, metadata.ClassReference.Id, attributeName);
        switch (newMethod)
        {
            case Error<MethodDecl.Method, Exception> e_method:
                return Error<(ClassDecl.Class, MethodDecl.Method[]), Exception>.From(new Exception($"failed to parse new method\n{e_method.Message}"));
        }

        var n_method = newMethod as Success<MethodDecl.Method, Exception>;

        var renamedMethod = Reader.Parse<MethodDecl.Method>(metadata.Code.ToString().Replace(metadata.Name, metadata.MangledName));
        return renamedMethod switch
        {
            Error<MethodDecl.Method, Exception> e_method
                => Error<(ClassDecl.Class, MethodDecl.Method[]), Exception>.From(new Exception($"failed to parse modified old method\n{e_method.Message}")),
            Success<MethodDecl.Method, Exception> o_method
                => Success<(ClassDecl.Class, MethodDecl.Method[]), Exception>.From((null, new[] { o_method.Value, n_method.Value }))
        };
    }
    private static Result<MethodDecl.Method, Exception> Handle(MethodData metadata, IdentifierDecl.Identifier container, string[] AttributeClass)
    {
        int labelIdx = 0;
        StringBuilder builder = new();
        Dictionary<string, string> jumptable = new();

        builder.AppendLine($".method {metadata.Code.Header} {{");
        foreach (var member in metadata.Code.Body.Items.Values)
        {
            if (member is MethodDecl.LabelItem or MethodDecl.InstructionItem or MethodDecl.LocalsItem or MethodDecl.MaxStackItem) continue;
            builder.AppendLine(member.ToString());
        }
        builder.AppendLine($".maxstack {(metadata.Code.Header.Parameters.Parameters.Values.Length > 0 ? 8 : 2)}");

        builder.AppendLine($$$"""
        .locals init (
            {{{String.Join("\n", AttributeClass.Select((attrClassName, i) => $"class {attrClassName} {GenerateInterceptorName(attrClassName)},"))}}}
            class [Inoculator.Injector]Inoculator.Builder.MethodData metadata,
            {{{(
                metadata.Signature.Output.IsVoid
                    ? String.Empty
                    : $@" {metadata.Signature.Output.Code} result,"
            )}}}
            class [System.Runtime]System.Exception e
        )
        """);

        builder.Append($$$"""
        {{{AttributeClass.Select(
                (attrClassName, i) => $@"
                {GetNextLabel(ref labelIdx)}: newobj instance void {attrClassName}::.ctor()
                {GetNextLabel(ref labelIdx)}: stloc.s {i}"
            ).Aggregate((a, b) => $"{a}\n{b}")}}}
        {{{GetNextLabel(ref labelIdx)}}}: ldstr "{{{new string(metadata.Code.ToString().ToCharArray().Select(c => c != '\n' ? c : ' ').ToArray())}}}"
        {{{GetNextLabel(ref labelIdx)}}}: newobj instance void [Inoculator.Injector]Inoculator.Builder.MethodData::.ctor(string)
        {{{GetNextLabel(ref labelIdx)}}}: stloc.s metadata

        {{{GetNextLabel(ref labelIdx)}}}: ldloc.s metadata
        {{{GetNextLabel(ref labelIdx)}}}: ldc.i4.{{{metadata.Code.Header.Parameters.Parameters.Values.Length}}}
        {{{GetNextLabel(ref labelIdx)}}}: newarr [System.Runtime]System.Object
        
        {{{ExtractArguments(metadata.Code.Header.Parameters, ref labelIdx, 0)}}}
        {{{GetNextLabel(ref labelIdx)}}}: callvirt instance void [Inoculator.Injector]Inoculator.Builder.MethodData::set_Parameters(object[])

        {{{CallMethodOnInterceptors(metadata.ClassReference.Id.ToString(), AttributeClass, "OnEntry", true, ref labelIdx)}}}
        .try
        {
            .try
            {
                {{{(metadata.IsStatic ? String.Empty : $@"{GetNextLabel(ref labelIdx)}: ldarg.0")}}}
                {{{LoadArguments(metadata.Code.Header.Parameters, ref labelIdx, metadata.IsStatic ? 0 : 1)}}}
                {{{GetNextLabel(ref labelIdx)}}}: call {{{metadata.MkMethodReference(true)}}}
                {{{(
                    metadata.Signature.Output.IsVoid
                        ? String.Empty
                        : $@"{GetNextLabel(ref labelIdx)}: stloc.s result"
                )}}}
                {{{GetNextLabel(ref labelIdx)}}}: ldloc.s metadata
                {{{(
                    metadata.Signature.Output.IsVoid
                        ? $@"{GetNextLabel(ref labelIdx)}: ldnull"
                        : $@"{GetNextLabel(ref labelIdx)}: ldloc.s result
                            {(  metadata.Signature.Output.IsReferenceType ? String.Empty
                                : $@"{GetNextLabel(ref labelIdx)}: box {metadata.Signature.Output.ToProperName}"
                            )}"
                )}}}
                {{{GetNextLabel(ref labelIdx)}}}: callvirt instance void [Inoculator.Injector]Inoculator.Builder.MethodData::set_ReturnValue(object)
                {{{UpdateRefArguments(metadata.Code.Header.Parameters, ref labelIdx, metadata.IsStatic ? 0 : 1)}}}
                {{{CallMethodOnInterceptors(metadata.ClassReference.Id.ToString(), AttributeClass, "OnSuccess", true, ref labelIdx)}}}
                {{{(
                    metadata.Signature.Output.IsVoid ? String.Empty
                    : $@"{GetNextLabel(ref labelIdx)}: ldloc.s result"
                )}}}
                {{{GetNextLabel(ref labelIdx)}}}: leave.s ***END***
            } 
            catch [System.Runtime]System.Exception
            {
                {{{GetNextLabel(ref labelIdx)}}}: stloc.s e
                {{{GetNextLabel(ref labelIdx)}}}: ldloc.s metadata
                {{{GetNextLabel(ref labelIdx)}}}: ldloc.s e
                {{{GetNextLabel(ref labelIdx)}}}: callvirt instance void [Inoculator.Injector]Inoculator.Builder.MethodData::set_Exception(class [System.Runtime]System.Exception)
                {{{CallMethodOnInterceptors(metadata.ClassReference.Id.ToString(), AttributeClass, "OnException", true, ref labelIdx)}}}
                {{{GetNextLabel(ref labelIdx)}}}: ldloc.s e
                {{{GetNextLabel(ref labelIdx)}}}: throw
            } 
        } 
        finally
        {
            {{{CallMethodOnInterceptors(metadata.ClassReference.Id.ToString(), AttributeClass, "OnExit", true, ref labelIdx)}}}
            {{{GetNextLabel(ref labelIdx)}}}: endfinally
        } 

        {{{GetNextLabel(ref labelIdx, jumptable, "END")}}}: nop
        {{{(
            metadata.Signature.Output.IsVoid 
                ? String.Empty 
                : $@"{GetNextLabel(ref labelIdx)}: ldloc.s result"
        )}}}
        {{{GetNextLabel(ref labelIdx)}}}: ret
        }
        """);

        foreach(var (label, idx) in jumptable)
        {
            builder.Replace($"***{label}***", idx.ToString());
        }
        var result = builder.ToString();

        return Reader.Parse<MethodDecl.Method>(result);
    }
}