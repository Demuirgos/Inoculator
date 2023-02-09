using System.Extensions;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ClassDecl;
using IdentifierDecl;
using Inoculator.Core;
using LabelDecl;
using MethodDecl;
using static Dove.Core.Parser;
using static Inoculator.Builder.HandlerTools; 
namespace Inoculator.Builder;

public static class AsyncRewriter {
    public static Result<(ClassDecl.Class, MethodDecl.Method[]), Exception> Rewrite(ClassDecl.Class classRef, MethodData metadata, string[] attributeNames, IEnumerable<string> path)
    {
        bool isReleaseMode = classRef.Header.Extends.Type.ToString() == "[System.Runtime] System.ValueType";
        var typeContainer = metadata.Code.Header.Type.Components.Types.Values.First() as TypeDecl.CustomTypeReference;
        var itemType = new TypeData(typeContainer.Reference.GenericTypes?.Types.Values.FirstOrDefault()?.ToString() ?? "void");

        var MoveNextHandler = GetNextMethodHandler(itemType, classRef, path, isReleaseMode, attributeNames);
        classRef = InjectInoculationFields(classRef, attributeNames, MoveNextHandler);
        metadata = RewriteInceptionPoint(classRef, metadata, attributeNames, path, isReleaseMode);

        return Success<(ClassDecl.Class, MethodDecl.Method[]), Exception>.From((classRef, new[] { metadata.Code }));
    }

    private static MethodData RewriteInceptionPoint(Class classRef, MethodData metadata, string[] attributeNames, IEnumerable<string> path, bool isReleaseMode)
    {
        int labelIdx = 0;
        bool isStatic = metadata.MethodCall is MethodData.CallType.Static;
        int argumentsCount = metadata.IsStatic 
            ? metadata.Code.Header.Parameters.Parameters.Values.Length 
            : metadata.Code.Header.Parameters.Parameters.Values.Length + 1;

        var stateMachineFullNameBuilder = new StringBuilder()
            .Append(isReleaseMode ? " valuetype " : " class ")
            .Append($"{String.Join("/", path)}")
            .Append($"/{classRef.Header.Id}");
        if (classRef.Header.TypeParameters?.Parameters.Values.Length > 0)
        {
            var classTypeParametersCount = classRef.Header.TypeParameters.Parameters.Values.Length;
            var functionTypeParametersCount = metadata.Code.Header.TypeParameters.Parameters.Values.Length;
            var classTPs = classRef.Header.TypeParameters.Parameters.Values.Take(classTypeParametersCount - functionTypeParametersCount).Select(p => $"!{p}");
            var methodTPs = classRef.Header.TypeParameters.Parameters.Values.TakeLast(functionTypeParametersCount).Select(p => $"!!{p}");
            stateMachineFullNameBuilder.Append("<")
                .Append( String.Join(",", classTPs.Union(methodTPs)))
                .Append(">");
        }
        var stateMachineFullName = stateMachineFullNameBuilder.ToString();

        string loadLocalStateMachine = isReleaseMode ? "ldloca.s    V_0" : "ldloc.s    V_0";

        bool isWithinAStruct = metadata.ClassReference.Extends.Type.ToString() == "[System.Runtime] System.ValueType";
        
        string stackClause = ".maxstack 8";
        var LocalsClause = metadata.Code.Body.Items.Values.OfType<MethodDecl.LocalsItem>().FirstOrDefault();

        var oldInstructions = metadata.Code.Body.Items.Values.OfType<MethodDecl.InstructionItem>();
        var newInstructions = new List<MethodDecl.InstructionItem>();
        if(!isReleaseMode) {
            newInstructions.AddRange(oldInstructions.Take(2));
        }


        string injectionCode = $$$"""
            {{{loadLocalStateMachine}}}
            ldstr "{{{new string(metadata.Code.ToString().ToCharArray().Select(c => c != '\n' ? c : ' ').ToArray())}}}"
            {{{GetNextLabel(ref labelIdx)}}}: ldstr "{{{new string(metadata.ClassReference.ToString().ToCharArray().Select(c => c != '\n' ? c : ' ').ToArray())}}}"
            {{{GetNextLabel(ref labelIdx)}}}: newobj instance void [Inoculator.Interceptors]Inoculator.Builder.MethodData::.ctor(string, string)


            dup
            ldc.i4.s {{{argumentsCount}}}
            newarr [System.Runtime]System.Object
            {{{ExtractArguments(metadata, ref labelIdx, metadata.IsStatic, false)}}}

            callvirt instance void [Inoculator.Interceptors]Inoculator.Builder.MethodData::set_Parameters(object[])
            stfld class [Inoculator.Interceptors]Inoculator.Builder.MethodData {{{stateMachineFullName}}}::'<inoculated>__Metadata'
            {{{attributeNames.Select(
                    (attrClassName, i) => $@"
                        {loadLocalStateMachine}
                        newobj instance void class {attrClassName}::.ctor()
                        stfld class {attrClassName} {stateMachineFullName}::'<inoculated>__Interceptor{i}'
            ").Aggregate((a, b) => $"{a}\n{b}")}}}
            """;
        
        var injectionCodeCast = Parse<InstructionDecl.Instruction.Block>(injectionCode);
        newInstructions.AddRange(injectionCodeCast.Opcodes.Values.Select(opcode => new InstructionItem(opcode)));
        newInstructions.AddRange(oldInstructions.Skip(isReleaseMode ? 0 : 2));

        string newBody = $$$"""
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

    private static ClassDecl.Class InjectInoculationFields(Class classRef, string[] attributeNames, Func<MethodDefinition, MethodDefinition[]> MoveNextHandler)
    {
        classRef = classRef with
        {
            Members = classRef.Members with
            {
                Members = new ARRAY<ClassDecl.Member>(
                            classRef.Members.Members.Values
                                .SelectMany(member => member switch
                                {
                                    ClassDecl.MethodDefinition method => MoveNextHandler(method),
                                    _ => new[] { member }
                                }).Union(
                                attributeNames
                                    .Select((attr, i) => $".field public class {attr} '<inoculated>__Interceptor{i}'")
                                    .Append($".field public class [Inoculator.Interceptors]Inoculator.Builder.MethodData '<inoculated>__Metadata'")
                                    .Select(Parse<ClassDecl.Member>)
                                ).ToArray()
                        )
                {
                    Options = new ARRAY<ClassDecl.Member>.ArrayOptions()
                    {
                        Delimiters = ('\0', '\n', '\0')
                    }
                }
            }
        };
        return classRef;
    }

    private static Func<ClassDecl.MethodDefinition, ClassDecl.MethodDefinition[]> GetNextMethodHandler(TypeData returnType, Class classRef, IEnumerable<string> path, bool isReleaseMode, string[] attributeNames)
    {
        static string ToGenericArity1(TypeData type) => type.IsVoid ? String.Empty : type.IsGeneric ? $"`1<!{type.PureName}>" : $"`1<{type.Name}>";

        int labelIdx = 0;
        Dictionary<string, string> jumptable = new();

        var stateMachineFullNameBuilder = new StringBuilder()
            .Append(isReleaseMode ? " valuetype " : " class ")
            .Append($"{String.Join("/", path)}")
            .Append($"/{classRef.Header.Id}");
        if (classRef.Header.TypeParameters?.Parameters.Values.Length > 0)
        {
            stateMachineFullNameBuilder.Append("<")
                .Append(String.Join(", ", classRef.Header.TypeParameters.Parameters.Values.Select(p => $"!{p}")))
                .Append(">");
        }
        var stateMachineFullName = stateMachineFullNameBuilder.ToString();

        ClassDecl.MethodDefinition[] HandleMoveNext(ClassDecl.MethodDefinition methodDef)
        {
            if (methodDef.Value.Header.Name.ToString() != "MoveNext") return new[] { methodDef };
            var method = methodDef.Value;
            StringBuilder builder = new();
            builder.AppendLine($".method {method.Header} {{");
            foreach (var member in method.Body.Items.Values)
            {
                if (member is MethodDecl.LabelItem
                            or MethodDecl.InstructionItem
                            or MethodDecl.LocalsItem
                            or MethodDecl.MaxStackItem
                            or MethodDecl.ExceptionHandlingItem
                            or MethodDecl.ScopeBlock
                    ) continue;
                builder.AppendLine(member.ToString());
            }


            builder.AppendLine($$$"""
                .maxstack 8
                .locals init (class [System.Runtime]System.Exception e)
            """);

            builder.Append($$$"""
                {{{GetNextLabel(ref labelIdx)}}}: ldarg.0
                {{{GetNextLabel(ref labelIdx)}}}: ldfld int32 {{{stateMachineFullName}}}::'<>1__state'
                {{{GetNextLabel(ref labelIdx)}}}: ldc.i4.m1
                {{{GetNextLabel(ref labelIdx)}}}: bne.un.s ***JUMPDEST1***


                {{{attributeNames.Select(
                    (attrClassName, i) => $@"
                    {GetNextLabel(ref labelIdx)}: ldarg.0
                    {GetNextLabel(ref labelIdx)}: ldfld class {attrClassName} {stateMachineFullName}::'<inoculated>__Interceptor{i}'
                    {GetNextLabel(ref labelIdx)}: ldarg.0
                    {GetNextLabel(ref labelIdx)}: ldfld class [Inoculator.Interceptors]Inoculator.Builder.MethodData {stateMachineFullName}::'<inoculated>__Metadata'
                    {GetNextLabel(ref labelIdx)}: callvirt instance void class {attrClassName}::OnEntry(class [Inoculator.Interceptors]Inoculator.Builder.MethodData)"
                ).Aggregate((a, b) => $"{a}\n{b}")}}}

                {{{GetNextLabel(ref labelIdx, jumptable, "JUMPDEST1")}}}: nop

                {{{GetNextLabel(ref labelIdx)}}}: ldarg.0
                {{{GetNextLabel(ref labelIdx)}}}: call instance void {{{stateMachineFullName}}}::MoveNext__inoculated()

                {{{GetNextLabel(ref labelIdx)}}}: ldarg.0
                {{{GetNextLabel(ref labelIdx)}}}: ldfld class [Inoculator.Interceptors]Inoculator.Builder.MethodData {{{stateMachineFullName}}}::'<inoculated>__Metadata'
                {{{GetNextLabel(ref labelIdx)}}}: ldarg.0
                {{{GetNextLabel(ref labelIdx)}}}: ldflda valuetype [System.Runtime]System.Runtime.CompilerServices.AsyncTaskMethodBuilder{{{ToGenericArity1(returnType)}}} {{{stateMachineFullName}}}::'<>t__builder'
                {{{GetNextLabel(ref labelIdx)}}}: call instance class [System.Runtime]System.Threading.Tasks.Task{{{(String.IsNullOrEmpty(ToGenericArity1(returnType)) ? string.Empty : "`1<!0> valuetype")}}} [System.Runtime]System.Runtime.CompilerServices.AsyncTaskMethodBuilder{{{ToGenericArity1(returnType)}}}::get_Task()
                {{{GetNextLabel(ref labelIdx)}}}: call instance class [System.Runtime]System.AggregateException [System.Runtime]System.Threading.Tasks.Task::get_Exception()
                {{{GetNextLabel(ref labelIdx)}}}: dup
                {{{GetNextLabel(ref labelIdx)}}}: stloc.0
                {{{GetNextLabel(ref labelIdx)}}}: callvirt instance void [Inoculator.Interceptors]Inoculator.Builder.MethodData::set_Exception(class [System.Runtime]System.Exception)

                {{{GetNextLabel(ref labelIdx)}}}: ldloc.0
                {{{GetNextLabel(ref labelIdx)}}}: brtrue.s ***FAILURE***

                {{{GetNextLabel(ref labelIdx)}}}: ldarg.0
                {{{GetNextLabel(ref labelIdx)}}}: ldfld class [Inoculator.Interceptors]Inoculator.Builder.MethodData {{{stateMachineFullName}}}::'<inoculated>__Metadata'
                {{{(
                    returnType.IsVoid
                    ? $@"
                        {GetNextLabel(ref labelIdx)}: ldnull
                        {GetNextLabel(ref labelIdx)}: callvirt instance void [Inoculator.Interceptors]Inoculator.Builder.MethodData::set_ReturnValue(object)"
                    : $@"
                        {GetNextLabel(ref labelIdx)}: ldarg.0
                        {GetNextLabel(ref labelIdx)}: ldflda valuetype [System.Runtime]System.Runtime.CompilerServices.AsyncTaskMethodBuilder{ToGenericArity1(returnType)} {stateMachineFullName}::'<>t__builder'
                        {GetNextLabel(ref labelIdx)}: call instance class [System.Runtime]System.Threading.Tasks.Task`1<!0> valuetype [System.Runtime]System.Runtime.CompilerServices.AsyncTaskMethodBuilder{ToGenericArity1(returnType)}::get_Task()
                        {GetNextLabel(ref labelIdx)}: callvirt instance !0 class [System.Runtime]System.Threading.Tasks.Task{ToGenericArity1(returnType)}::get_Result()
                        {(
                            returnType.IsReferenceType ? string.Empty
                            : $"{GetNextLabel(ref labelIdx)}: box {(returnType.IsGeneric ? $"!{returnType.PureName}" : returnType.Name)}"
                        )}
                        {GetNextLabel(ref labelIdx)}: callvirt instance void [Inoculator.Interceptors]Inoculator.Builder.MethodData::set_ReturnValue(object)"
                )}}}

                {{{GetNextLabel(ref labelIdx, jumptable, "SUCCESS")}}}: nop
                {{{attributeNames.Select(
                    (attrClassName, i) => $@"
                    {GetNextLabel(ref labelIdx)}: ldarg.0
                    {GetNextLabel(ref labelIdx)}: ldfld class {attrClassName} {stateMachineFullName}::'<inoculated>__Interceptor{i}'
                    {GetNextLabel(ref labelIdx)}: ldarg.0
                    {GetNextLabel(ref labelIdx)}: ldfld class [Inoculator.Interceptors]Inoculator.Builder.MethodData {stateMachineFullName}::'<inoculated>__Metadata'
                    {GetNextLabel(ref labelIdx)}: callvirt instance void class {attrClassName}::OnSuccess(class [Inoculator.Interceptors]Inoculator.Builder.MethodData)"
                ).Aggregate((a, b) => $"{a}\n{b}")}}}
                {{{GetNextLabel(ref labelIdx)}}}: br.s ***EXIT***

                {{{GetNextLabel(ref labelIdx, jumptable, "FAILURE")}}}: nop
                {{{attributeNames.Select(
                    (attrClassName, i) => $@"
                    {GetNextLabel(ref labelIdx)}: ldarg.0
                    {GetNextLabel(ref labelIdx)}: ldfld class {attrClassName} {stateMachineFullName}::'<inoculated>__Interceptor{i}'
                    {GetNextLabel(ref labelIdx)}: ldarg.0
                    {GetNextLabel(ref labelIdx)}: ldfld class [Inoculator.Interceptors]Inoculator.Builder.MethodData {stateMachineFullName}::'<inoculated>__Metadata'
                    {GetNextLabel(ref labelIdx)}: callvirt instance void class {attrClassName}::OnException(class [Inoculator.Interceptors]Inoculator.Builder.MethodData)"
                ).Aggregate((a, b) => $"{a}\n{b}")}}}
                {{{GetNextLabel(ref labelIdx)}}}: br.s ***EXIT***

                {{{GetNextLabel(ref labelIdx, jumptable, "EXIT")}}}: ldarg.0
                {{{GetNextLabel(ref labelIdx)}}}: ldfld int32 {{{stateMachineFullName}}}::'<>1__state'
                {{{GetNextLabel(ref labelIdx)}}}: ldc.i4.s -2
                {{{GetNextLabel(ref labelIdx)}}}: bne.un.s ***JUMPDEST2***
                {{{attributeNames.Select(
                    (attrClassName, i) => $@"
                    {GetNextLabel(ref labelIdx)}: ldarg.0
                    {GetNextLabel(ref labelIdx)}: ldfld class {attrClassName} {stateMachineFullName}::'<inoculated>__Interceptor{i}'
                    {GetNextLabel(ref labelIdx)}: ldarg.0
                    {GetNextLabel(ref labelIdx)}: ldfld class [Inoculator.Interceptors]Inoculator.Builder.MethodData {stateMachineFullName}::'<inoculated>__Metadata'
                    {GetNextLabel(ref labelIdx)}: callvirt instance void class {attrClassName}::OnExit(class [Inoculator.Interceptors]Inoculator.Builder.MethodData)"
                ).Aggregate((a, b) => $"{a}\n{b}")}}}

                {{{GetNextLabel(ref labelIdx, jumptable, "JUMPDEST2")}}}: nop
                {{{GetNextLabel(ref labelIdx)}}}: ret
            }}
            """);

            foreach (var (label, idx) in jumptable)
            {
                builder.Replace($"***{label}***", idx.ToString());
            }
            var newFunction = new ClassDecl.MethodDefinition(Parse<MethodDecl.Method>(builder.ToString()));

            var oldFunction = methodDef with
            {
                Value = methodDef.Value with
                {
                    Header = methodDef.Value.Header with
                    {
                        Name = Parse<MethodName>("MoveNext__inoculated")
                    },
                    Body = methodDef.Value.Body with
                    {
                        Items = new ARRAY<MethodDecl.Member>(
                            methodDef.Value.Body
                                .Items.Values.Where(member => member is not MethodDecl.OverrideMethodItem)
                                .ToArray()
                        )
                        {
                            Options = new ARRAY<MethodDecl.Member>.ArrayOptions()
                            {
                                Delimiters = ('\0', '\n', '\0')
                            }
                        }
                    }
                }
            };


            return new[] { newFunction, oldFunction };
        }

        return HandleMoveNext;
    }
}