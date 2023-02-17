using System.Extensions;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ClassDecl;
using ExtraTools;
using IdentifierDecl;
using Inoculator.Core;
using LabelDecl;
using MethodDecl;
using static Dove.Core.Parser;
using static Inoculator.Builder.HandlerTools; 
namespace Inoculator.Builder;

public static class AsyncEnumRewriter {
    public static Result<(ClassDecl.Class[], MethodDecl.Method[]), Exception> Rewrite(ClassDecl.Class classRef, MethodData metadata, InterceptorData[] interceptors, IEnumerable<string> path)
    {
        bool isReleaseMode = classRef.Header.Extends.Type.ToString() == "[System.Runtime] System.ValueType";
        var typeContainer = metadata.Code.Header.Type.Components.Types.Values.First() as TypeDecl.CustomTypeReference;
        var oldClassMangledName= $"'<>__{Math.Abs(classRef.Header.Id.GetHashCode())}_old'";
        var oldClassRef = Parse<Class>(classRef.ToString()
            .Replace(classRef.Header.Id.ToString(), oldClassMangledName)
            .Replace(metadata.Code.Header.Name.ToString(), metadata.MangledName(false))
        );
        var oldMethodInstance = Parse<Method>(metadata.Code.ToString()
            .Replace(classRef.Header.Id.ToString(), oldClassMangledName)
            .Replace(metadata.Code.Header.Name.ToString(), metadata.MangledName(false))
        );
        var MoveNextHandler = GetNextMethodHandler(metadata, typeContainer, classRef, path, isReleaseMode, interceptors);
        var newClassRef = InjectInoculationFields(classRef, interceptors, MoveNextHandler);
        metadata = RewriteInceptionPoint(classRef, metadata, interceptors, path, isReleaseMode);

        return Success<(ClassDecl.Class[], MethodDecl.Method[]), Exception>.From((new Class[] { oldClassRef, newClassRef }, new Method[] { oldMethodInstance, metadata.Code }));
    }

    private static MethodData RewriteInceptionPoint(Class classRef, MethodData metadata, InterceptorData[] interceptorsClasses, IEnumerable<string> path, bool isReleaseMode)
    {
        int labelIdx = 0;
        bool isToBeRewritten = interceptorsClasses.Any(i => i.IsRewriter);

        bool isStatic = metadata.MethodCall is MethodData.CallType.Static;
        int argumentsCount = metadata.IsStatic 
            ? metadata.Code.Header.Parameters.Parameters.Values.Length 
            : metadata.Code.Header.Parameters.Parameters.Values.Length + 1;

        var stateMachineFullName = StringifyPath(metadata, classRef.Header, path, isReleaseMode, 2);
        var inoculationSightName = StringifyPath(metadata, classRef.Header, path, isReleaseMode, 0);


        string loadLocalStateMachine = isReleaseMode ? "ldloca.s    V_0" : "ldloc.s    V_0";

        bool isWithinAStruct = metadata.ClassReference.Extends.Type.ToString() == "[System.Runtime] System.ValueType";
        
        string CustomAttributes = String.Join("\n", metadata.Code.Body.Items.Values.OfType<MethodDecl.CustomAttributeItem>()
            .Select(i => i.ToString()));


        string stackClause = ".maxstack 16";
        var LocalsClause = metadata.Code.Body.Items.Values.OfType<MethodDecl.LocalsItem>().FirstOrDefault();

        var oldInstructions = metadata.Code.Body.Items.Values.OfType<MethodDecl.InstructionItem>();
        var newInstructions = new List<MethodDecl.InstructionItem>();
        if(!isReleaseMode) {
            newInstructions.AddRange(oldInstructions.Take(2));
        }


        string injectionCode = $$$"""
            {{{loadLocalStateMachine}}}
            ldstr "{{{GetCleanedString(metadata.Code.ToString())}}}"
            ldstr "{{{GetCleanedString(metadata.ClassReference.ToString())}}}"
            ldstr "{{{String.Join("/", path)}}}"
            newobj instance void [Inoculator.Interceptors]Inoculator.Builder.MethodData::.ctor(string, string, string)
            dup
            ldc.i4.s {{{argumentsCount}}}
            newarr [Inoculator.Interceptors]Inoculator.Builder.ParameterData
            {{{ExtractArguments(metadata, ref labelIdx, metadata.IsStatic, false)}}}

            callvirt instance void [Inoculator.Interceptors]Inoculator.Builder.MethodData::set_Parameters(class [Inoculator.Interceptors]Inoculator.Builder.ParameterData[])
            stfld class [Inoculator.Interceptors]Inoculator.Builder.MethodData {{{stateMachineFullName}}}::'<inoculated>__Metadata'
            {{{String.Join("\n",
                interceptorsClasses.Select(
                    (attrClassName, i) => $@"
                        {loadLocalStateMachine}
                        {GetAttributeInstance(metadata, inoculationSightName, attrClassName, ref labelIdx, false)}
                        stfld class {attrClassName.ClassName} {stateMachineFullName}::{GenerateInterceptorName(attrClassName.ClassName)}"
            ))}}}
            """;
        
        var injectionCodeCast = Parse<InstructionDecl.Instruction.Block>(injectionCode);
        newInstructions.AddRange(injectionCodeCast.Opcodes.Values.Select(opcode => new InstructionItem(opcode)));
        newInstructions.AddRange(oldInstructions.Skip(isReleaseMode ? 0 : 2));

        string newBody = $$$"""
            {{{CustomAttributes}}}
            {{{stackClause}}}
            {{{LocalsClause}}}
            {{{
                newInstructions
                    .Select(opcodeArgPair => $"{GetNextLabel(ref labelIdx)}: {opcodeArgPair}")
                    .Aggregate((a, b) => $"{a}\n{b}")
            }}}
        """;
        _ = TryParse<MethodDecl.Member.Collection>(newBody, out MethodDecl.Member.Collection body, out string err2);

        metadata.Code = metadata.Code with
        {
            Body = body
        };
        return metadata;
    }

    private static ClassDecl.Class InjectInoculationFields(Class classRef, InterceptorData[] interceptorsClasses, Func<MethodDefinition, MethodDefinition[]> MoveNextHandler)
    {
        throw new NotImplementedException();
    }

    private static Func<ClassDecl.MethodDefinition, ClassDecl.MethodDefinition[]> GetNextMethodHandler(MethodData metadata, TypeDecl.CustomTypeReference typeContainer, Class classRef, IEnumerable<string> path, bool isReleaseMode, InterceptorData[] modifierClasses)
    {
        throw new NotImplementedException();
    }

    private static string InvokeFunction(string stateMachineFullName, TypeDecl.CustomTypeReference typeContainer, ref int labelIdx, string? rewriterClass, bool rewrite, Dictionary<string, string> jumptable) {
        throw new NotImplementedException();
    }
}