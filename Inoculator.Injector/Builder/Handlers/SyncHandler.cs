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
        var functionFullPath = StringifyPath(metadata, classRef, path, isContainedInStruct, 0);

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
                {GetAttributeInstance(metadata, functionFullPath, attrClassName)}
                stloc.s {i}"
        ))}}}
        ldstr "{{{GetCleanedString(metadata.Code.ToString())}}}"
        ldstr "{{{GetCleanedString(metadata.ClassReference.ToString())}}}"
        ldstr "{{{String.Join("/", path)}}}"
        newobj instance void [Inoculator.Interceptors]Inoculator.Builder.MethodData::.ctor(string, string,string)
        stloc.s metadata

        
        ldloc.s metadata
        ldc.i4.{{{argumentsCount}}}
        newarr [Inoculator.Interceptors]Inoculator.Builder.ParameterData
        
        {{{ExtractArguments(metadata, metadata.IsStatic)}}}
        callvirt instance void [Inoculator.Interceptors]Inoculator.Builder.MethodData::set_Parameters(class [Inoculator.Interceptors]Inoculator.Builder.ParameterData[])

        {{{CallMethodOnInterceptors(metadata.ClassReference.Id.ToString(), modifierClasses, "OnEntry", true, ref labelIdx, false)}}}
        .try
        {
            .try
            {
                {{{CallMethodOnInterceptors(metadata.ClassReference.Id.ToString(), modifierClasses, "OnBegin", true, ref labelIdx, false)}}}
                {{{InvokeFunction(functionFullPath, metadata, ref labelIdx, modifierClasses, isToBeRewritten)}}}
                {{{CallMethodOnInterceptors(metadata.ClassReference.Id.ToString(), modifierClasses, "OnSuccess", true, ref labelIdx)}}}
                {{{(
                    metadata.Signature.Output.IsVoid ? String.Empty
                    : $@"ldloc.s result"
                )}}}
                leave.s ***END***
            } 
            catch [System.Runtime]System.Exception
            {
                {{{CallMethodOnInterceptors(metadata.ClassReference.Id.ToString(), modifierClasses, "OnEnd", true, ref labelIdx, false)}}}
                stloc.s e
                ldloc.s metadata
                ldloc.s e
                callvirt instance void [Inoculator.Interceptors]Inoculator.Builder.MethodData::set_Exception(class [System.Runtime]System.Exception)
                {{{CallMethodOnInterceptors(metadata.ClassReference.Id.ToString(), modifierClasses, "OnException", true, ref labelIdx)}}}
                ldloc.s e
                throw
            } 
        } 
        finally
        {
            {{{CallMethodOnInterceptors(metadata.ClassReference.Id.ToString(), modifierClasses, "OnExit", true, ref labelIdx)}}}
            endfinally
        } 

        {{{GetNextLabel(ref labelIdx, jumptable, "END")}}}: nop
        {{{(
            metadata.Signature.Output.IsVoid 
                ? String.Empty 
                : $@"ldloc.s result"
        )}}}
        ret
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
                {{{LoadArguments(metadata.Code.Header.Parameters, metadata.IsStatic)}}}
                call {{{metadata.MkMethodReference(true, functionPath)}}}
                {{{CallMethodOnInterceptors(metadata.ClassReference.Id.ToString(), modifierClass, "OnEnd", true, ref labelIdx, false)}}}
                {{{(
                    metadata.Signature.Output.IsVoid
                        ? String.Empty
                        : $@"stloc.s result"
                )}}}
                ldloc.s metadata
                {{{ExtractReturnValue(metadata.Signature.Output, ref labelIdx)}}}
                {{{UpdateRefArguments(metadata.Code.Header.Parameters, metadata.IsStatic)}}}
            """;
        } else {
            var rewriterClass = modifierClass.FirstOrDefault(c => c.IsRewriter);
            string callCode = $"callvirt instance class [Inoculator.Interceptors]Inoculator.Builder.MethodData class {rewriterClass.ClassName}::OnCall(class [Inoculator.Interceptors]Inoculator.Builder.MethodData)";
            return $$$"""
                ldloc.s {{{GenerateInterceptorName(rewriterClass.ClassName)}}}
                ldloc.s metadata
                {{{callCode}}}
                stloc.s metadata
                {{{CallMethodOnInterceptors(metadata.ClassReference.Id.ToString(), modifierClass, "OnEnd", true, ref labelIdx, false)}}}
                ldloc.s metadata
                {{{(
                    metadata.Signature.Output.IsVoid
                        ? String.Empty
                        : $@"
                            callvirt instance class [Inoculator.Interceptors]Inoculator.Builder.ParameterData [Inoculator.Interceptors]Inoculator.Builder.MethodData::get_ReturnValue()
                            callvirt instance object [Inoculator.Interceptors]Inoculator.Builder.ParameterData::get_Value()
                            {(metadata.Signature.Output.IsReferenceType ? $"castclass {metadata.Signature.Output.Name}" : $"unbox.any {metadata.Signature.Output.ToProperName}")}
                        "
                )}}}
                stloc.s result
                {{{ReflectRefArguments(metadata.Code.Header.Parameters, metadata.IsStatic)}}}
            """;
        }
    }
}