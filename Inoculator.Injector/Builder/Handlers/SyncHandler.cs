namespace Inoculator.Builder;

using System.Extensions;
using System.Text;
using Inoculator.Core;
using static Inoculator.Builder.HandlerTools;

public static class SyncRewriter {
    public static Result<(ClassDecl.Class[], MethodDecl.Method[]), Exception> Rewrite(ClassDecl.Class _, MethodData metadata, InterceptorData[] modifiers, IEnumerable<string> path)
    {
        var newMethod = Handle(metadata.ClassReference, metadata, modifiers, path);
        switch (newMethod)
        {
            case Error<MethodDecl.Method, Exception> e_method:
                return Error<(ClassDecl.Class[], MethodDecl.Method[]), Exception>.From(new Exception($"failed to parse new method\n{e_method.Message}"));
        }

        var n_method = newMethod as Success<MethodDecl.Method, Exception>;

        var renamedMethod = Reader.Parse<MethodDecl.Method>(metadata.Code.ToString()
            .Replace($"::{metadata.Name(false)}", $"::{metadata.MangledName(false)}")
            .Replace($" {metadata.Name(false)}", $" {metadata.MangledName(false)}")
        );
        return renamedMethod switch
        {
            Error<MethodDecl.Method, Exception> e_method
                => Error<(ClassDecl.Class[], MethodDecl.Method[]), Exception>.From(new Exception($"failed to parse modified old method\n{e_method.Message}")),
            Success<MethodDecl.Method, Exception> o_method
                => Success<(ClassDecl.Class[], MethodDecl.Method[]), Exception>.From((null, new[] { o_method.Value, n_method.Value }))
        };
    }
    private static Result<MethodDecl.Method, Exception> Handle(ClassDecl.Prefix classRef, MethodData metadata, InterceptorData[] modifierClasses, IEnumerable<string> path)
    {
        int labelIdx = 0;
        bool isToBeRewritten = modifierClasses.Any(m => m.IsRewriter);

        var pathList = path?.ToList(); 
        bool isContainedInStruct = classRef.Extends.Type.ToString() == "[System.Runtime] System.ValueType";
        var functionFullPathBuilder = new StringBuilder()
            .Append(isContainedInStruct ? " valuetype " : " class ")
            .Append($"{String.Join("/", path)}");
        if (classRef.TypeParameters?.Parameters.Values.Length > 0)
        {
            functionFullPathBuilder.Append("<")
                .Append(String.Join(", ", classRef.TypeParameters.Parameters.Values.Select(p => $"!{p}")))
                .Append(">");
        }
        var functionFullPath = functionFullPathBuilder.ToString();

        StringBuilder builder = new();
        Dictionary<string, string> jumptable = new();
        int argumentsCount = metadata.IsStatic 
            ? metadata.Code.Header.Parameters.Parameters.Values.Length 
            : metadata.Code.Header.Parameters.Parameters.Values.Length + 1;

            
        builder.AppendLine($".method {metadata.Code.Header} {{");
        foreach (var member in metadata.Code.Body.Items.Values)
        {
            if (member is MethodDecl.LabelItem or MethodDecl.InstructionItem or MethodDecl.LocalsItem or MethodDecl.MaxStackItem) continue;
            builder.AppendLine(member.ToString());
        }
        builder.AppendLine($".maxstack {(metadata.Code.Header.Parameters.Parameters.Values.Length > 0 ? 8 : 2)}");
        builder.AppendLine($$$"""
        .locals init (
            {{{String.Join("\n", modifierClasses?.Select((attrClassName, i) => $"class {attrClassName.ClassName} {GenerateInterceptorName(attrClassName.ClassName)},"))}}}
            class [Inoculator.Interceptors]Inoculator.Builder.MethodData metadata,
            {{{(
                metadata.Signature.Output.IsVoid
                    ? String.Empty
                    : $@" {metadata.Signature.Output.Code} result,"
            )}}}
            class [System.Runtime]System.Exception e
        )
        """);

        builder.Append($$$"""
        {{{String.Join("\n", 
            modifierClasses?.Select(
                (attrClassName, i) => $@"
                {GetNextLabel(ref labelIdx)}: newobj instance void class {attrClassName.ClassName}::.ctor()
                {GetNextLabel(ref labelIdx)}: stloc.s {i}"
        ))}}}
        {{{GetNextLabel(ref labelIdx)}}}: ldstr "{{{new string(metadata.Code.ToString().ToCharArray().Select(c => c != '\n' ? c : ' ').ToArray())}}}"
        {{{GetNextLabel(ref labelIdx)}}}: ldstr "{{{new string(metadata.ClassReference.ToString().ToCharArray().Select(c => c != '\n' ? c : ' ').ToArray())}}}"
        {{{GetNextLabel(ref labelIdx)}}}: ldstr "{{{String.Join("/", path)}}}"
        {{{GetNextLabel(ref labelIdx)}}}: newobj instance void [Inoculator.Interceptors]Inoculator.Builder.MethodData::.ctor(string, string,string)
        {{{GetNextLabel(ref labelIdx)}}}: stloc.s metadata

        
        {{{GetNextLabel(ref labelIdx)}}}: ldloc.s metadata
        {{{GetNextLabel(ref labelIdx)}}}: ldc.i4.{{{argumentsCount}}}
        {{{GetNextLabel(ref labelIdx)}}}: newarr [Inoculator.Interceptors]Inoculator.Builder.ParameterData
        
        {{{ExtractArguments(metadata, ref labelIdx, metadata.IsStatic)}}}
        {{{GetNextLabel(ref labelIdx)}}}: callvirt instance void [Inoculator.Interceptors]Inoculator.Builder.MethodData::set_Parameters(class [Inoculator.Interceptors]Inoculator.Builder.ParameterData[])

        {{{CallMethodOnInterceptors(metadata.ClassReference.Id.ToString(), modifierClasses, "OnEntry", true, ref labelIdx, false)}}}
        .try
        {
            .try
            {
                {{{InvokeFunction(functionFullPath, metadata, ref labelIdx, modifierClasses, isToBeRewritten)}}}
                {{{CallMethodOnInterceptors(metadata.ClassReference.Id.ToString(), modifierClasses, "OnSuccess", true, ref labelIdx)}}}
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
                {{{GetNextLabel(ref labelIdx)}}}: callvirt instance void [Inoculator.Interceptors]Inoculator.Builder.MethodData::set_Exception(class [System.Runtime]System.Exception)
                {{{CallMethodOnInterceptors(metadata.ClassReference.Id.ToString(), modifierClasses, "OnException", true, ref labelIdx)}}}
                {{{GetNextLabel(ref labelIdx)}}}: ldloc.s e
                {{{GetNextLabel(ref labelIdx)}}}: throw
            } 
        } 
        finally
        {
            {{{CallMethodOnInterceptors(metadata.ClassReference.Id.ToString(), modifierClasses, "OnExit", true, ref labelIdx)}}}
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

    private static string InvokeFunction(string functionPath, MethodData metadata, ref int labelIdx, InterceptorData[] modifierClass, bool rewrite) {
        if(!rewrite) {
            return $$$"""
                {{{LoadArguments(metadata.Code.Header.Parameters, ref labelIdx, metadata.IsStatic)}}}
                {{{GetNextLabel(ref labelIdx)}}}: call {{{metadata.MkMethodReference(true, functionPath)}}}
                {{{(
                    metadata.Signature.Output.IsVoid
                        ? String.Empty
                        : $@"{GetNextLabel(ref labelIdx)}: stloc.s result"
                )}}}
                {{{GetNextLabel(ref labelIdx)}}}: ldloc.s metadata
                {{{ExtractReturnValue(metadata.Signature.Output, ref labelIdx)}}}
                {{{UpdateRefArguments(metadata.Code.Header.Parameters, metadata.IsStatic, ref labelIdx)}}}
            """;
        } else {
            var rewriterClass = modifierClass.FirstOrDefault(c => c.IsRewriter);
            string callCode = $"callvirt instance class [Inoculator.Interceptors]Inoculator.Builder.MethodData class {rewriterClass.ClassName}::OnCall(class [Inoculator.Interceptors]Inoculator.Builder.MethodData)";
            return $$$"""
                {{{GetNextLabel(ref labelIdx)}}}: ldloc.s {{{GenerateInterceptorName(rewriterClass.ClassName)}}}
                {{{GetNextLabel(ref labelIdx)}}}: ldloc.s metadata
                {{{GetNextLabel(ref labelIdx)}}}: {{{callCode}}}
                {{{GetNextLabel(ref labelIdx)}}}: stloc.s metadata
                {{{GetNextLabel(ref labelIdx)}}}: ldloc.s metadata
                {{{(
                    metadata.Signature.Output.IsVoid
                        ? String.Empty
                        : $@"
                            {GetNextLabel(ref labelIdx)}: callvirt instance class [Inoculator.Interceptors]Inoculator.Builder.ParameterData [Inoculator.Interceptors]Inoculator.Builder.MethodData::get_ReturnValue()
                            {GetNextLabel(ref labelIdx)}: callvirt instance object [Inoculator.Interceptors]Inoculator.Builder.ParameterData::get_Value()
                            {GetNextLabel(ref labelIdx)}: {(metadata.Signature.Output.IsReferenceType ? $"castclass {metadata.Signature.Output.Name}" : $"unbox.any {metadata.Signature.Output.ToProperName}")}
                        "
                )}}}
                {{{GetNextLabel(ref labelIdx)}}}: stloc.s result
                {{{ReflectRefArguments(metadata.Code.Header.Parameters, metadata.IsStatic, ref labelIdx)}}}
            """;
        }
    }
}