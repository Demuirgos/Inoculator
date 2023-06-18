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
using LocalDecl;
using MethodDecl;
using static Dove.Core.Parser;
using static Inoculator.Builder.HandlerTools; 
namespace Inoculator.Builder;

public static class AsyncRewriter {
    public static Result<(ClassDecl.Class[], MethodDecl.Method[]), Exception> Rewrite(ClassDecl.Class classRef, MethodData metadata, InterceptorData[] interceptors, IEnumerable<string> path)
    {
        bool isReleaseMode = classRef.Header.Extends.Type.ToString() == "[System.Runtime] System.ValueType";
        var typeContainer = metadata.Code.Header.Type.Components.Types.Values.First() as TypeDecl.CustomTypeReference;
        var oldClassMangledName= $"'<>__{Math.Abs(classRef.Header.Id.GetHashCode())}_old'";
        
        var oldClassRef = ReplaceSymbols(
            classRef, new string[] { "::", " " }, metadata.Name(false), metadata.MangledName(false),
            sourceCode => sourceCode.Replace(classRef.Header.Id.ToString(), oldClassMangledName)
        ) as Success<ClassDecl.Class, Exception>;

        
        var oldMethodInstance = ReplaceSymbols(
            metadata.Code, new string[] { "::", " " }, metadata.Code.Header.Name.ToString(), metadata.MangledName(false),
            sourceCode => sourceCode.Replace(classRef.Header.Id.ToString(), oldClassMangledName)
        ) as Success<MethodDecl.Method, Exception>;
        
        var MoveNextHandler = GetNextMethodHandler(metadata, typeContainer, classRef, path, isReleaseMode, interceptors);
        var newClassRef = InjectInoculationFields(classRef, interceptors, MoveNextHandler);
        metadata = RewriteInceptionPoint(classRef, metadata, interceptors, path, isReleaseMode);

        return Success<(ClassDecl.Class[], MethodDecl.Method[]), Exception>.From((new Class[] { oldClassRef.Value, newClassRef }, new Method[] { oldMethodInstance.Value, metadata.Code }));
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
        var localsDecls = metadata.Code.Body.Items.Values.OfType<MethodDecl.LocalsItem>().FirstOrDefault()?.Signatures?.Values?.Values;
        var LocalsClause = $$$"""
            .locals init (
                {{{(
                    localsDecls is null ? string.Empty :
                    String.Join("\n", localsDecls.Select(
                        Local => $@"{Local.Type} {Local.Id},"
                    ))
                )}}}
                class [System.Runtime]System.Reflection.MethodInfo methodInfo
            )
        """;

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
            {{{ExtractArguments(metadata, metadata.IsStatic)}}}

            callvirt instance void [Inoculator.Interceptors]Inoculator.Builder.MethodData::set_Parameters(class [Inoculator.Interceptors]Inoculator.Builder.ParameterData[])
            stfld class [Inoculator.Interceptors]Inoculator.Builder.MethodData {{{stateMachineFullName}}}::'<inoculated>__Metadata'
            {{{
                GetReflectiveMethodInstance(metadata, inoculationSightName)
            }}}
            {{{String.Join("\n",
                interceptorsClasses.Select(
                    (attrClassName, i) => $@"
                        {loadLocalStateMachine}
                        {GetAttributeInstance(attrClassName)}
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
                    .Select(opcodeArgPair => $"{opcodeArgPair}")
                    .Aggregate((a, b) => $"{a}\n{b}")
            }}}
        """;

        if(!TryParse<MethodDecl.Member.Collection>(newBody, out MethodDecl.Member.Collection body, out string err2)) {
            File.WriteAllText("injectionCode.log", newBody);
            throw new Exception(err2);  
        }

        metadata.Code = metadata.Code with
        {
            Body = body
        };
        return metadata;
    }

    private static ClassDecl.Class InjectInoculationFields(Class classRef, InterceptorData[] interceptorsClasses, Func<MethodDefinition, MethodDefinition[]> MoveNextHandler)
    {
        bool isToBeRewritten = interceptorsClasses.Any(interceptor => interceptor.IsRewriter);

        List<string> members = new();
        if(interceptorsClasses.Length > 0 ) {
            members.AddRange(interceptorsClasses.Select((attr, i) => $".field public class {attr.ClassName} {GenerateInterceptorName(attr.ClassName)}"));
        }
        members.Add($".field public class [Inoculator.Interceptors]Inoculator.Builder.MethodData '<inoculated>__Metadata'");

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
                                    members.Select(Parse<ClassDecl.Member>)
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

    private static Func<ClassDecl.MethodDefinition, ClassDecl.MethodDefinition[]> GetNextMethodHandler(MethodData metadata, TypeDecl.CustomTypeReference typeContainer, Class classRef, IEnumerable<string> path, bool isReleaseMode, InterceptorData[] modifierClasses)
    {
        int labelIdx = 0;
        Dictionary<string, string> jumptable = new();
        bool isToBeRewritten = modifierClasses.Any(m => m.IsRewriter);

        var stateMachineFullName = StringifyPath(metadata, classRef.Header, path, isReleaseMode, 1);

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

            static string ToGenericArity1(TypeData type) => type.IsVoid ? String.Empty : type.IsGeneric ? $"`1<!{type.PureName}>" : $"`1<{type.Name}>";
            var returnType = new TypeData(typeContainer.Reference.GenericTypes?.Types.Values.FirstOrDefault()?.ToString() ?? "void");
            string TaskVariantType = typeContainer.Reference.Name.ToString().StartsWith("System.Threading.Tasks.ValueTask") ? "ValueTask" : "Task";

            string builderClassName = typeContainer.Reference.Name.ToString().StartsWith("System.Threading.Tasks.ValueTask") 
                ? "AsyncValueTaskMethodBuilder"
                : "AsyncTaskMethodBuilder"; 
                
            builder.AppendLine($$$"""
                .maxstack 8
                .locals init (class [System.Runtime]System.Exception e, valuetype [System.Runtime]System.Threading.Tasks.ValueTask{{{ToGenericArity1(returnType)}}} ltask, class [System.Runtime]System.Threading.Tasks.Task{{{ToGenericArity1(returnType)}}} ttask)
            """);

            builder.Append($$$"""
                ldarg.0
                ldfld int32 {{{stateMachineFullName}}}::'<>1__state'
                ldc.i4.m1
                bne.un.s ***JUMPDEST1***


                {{{CallMethodOnInterceptors(stateMachineFullName, modifierClasses, "OnEntry", false, ref labelIdx)}}}

                {{{GetNextLabel(ref labelIdx, jumptable, "JUMPDEST1")}}}: nop

                {{{CallMethodOnInterceptors(stateMachineFullName, modifierClasses, "OnBegin", false, ref labelIdx)}}}

                {{{InvokeFunction(stateMachineFullName, typeContainer, ref labelIdx, modifierClasses, jumptable)}}}

                {{{GetNextLabel(ref labelIdx, jumptable, "SUCCESS")}}}: nop
                {{{CallMethodOnInterceptors(stateMachineFullName, modifierClasses, "OnSuccess", false, ref labelIdx)}}}
                br.s ***EXIT***

                {{{GetNextLabel(ref labelIdx, jumptable, "FAILURE")}}}: nop
                {{{CallMethodOnInterceptors(stateMachineFullName, modifierClasses, "OnException", false, ref labelIdx)}}}
                br.s ***EXIT***

                {{{GetNextLabel(ref labelIdx, jumptable, "EXIT")}}}: ldarg.0
                ldfld int32 {{{stateMachineFullName}}}::'<>1__state'
                ldc.i4.s -2
                bne.un.s ***JUMPDEST2***
                {{{CallMethodOnInterceptors(stateMachineFullName, modifierClasses, "OnExit", false, ref labelIdx)}}}
                {{{GetNextLabel(ref labelIdx, jumptable, "JUMPDEST2")}}}: nop
                ret
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
                                .Select(member => {
                                    var instructionLine = member.ToString().Replace("::" + metadata.Name(false), "::" + metadata.MangledName(false));
                                    return Parse<MethodDecl.Member>(instructionLine);
                                })
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

    private static string InvokeFunction(string stateMachineFullName, TypeDecl.CustomTypeReference typeContainer, ref int labelIdx, InterceptorData[] modifierClasses, Dictionary<string, string> jumptable) {
        static string ToGenericArity1(TypeData type) => type.IsVoid ? String.Empty : type.IsGeneric ? $"`1<!{type.PureName}>" : $"`1<{type.Name}>";
        var returnType = new TypeData(typeContainer.Reference.GenericTypes?.Types.Values.FirstOrDefault()?.ToString() ?? "void");
        
        string rewriterClass = modifierClasses.FirstOrDefault(m => m.IsRewriter)?.ClassName;
        bool rewrite = rewriterClass != null;

        string TaskVariantType(bool includeClassIndication, bool isGeneric) => typeContainer.Reference.Name.ToString().StartsWith("System.Threading.Tasks.ValueTask") 
            ? $"{(includeClassIndication ? "valuetype" : string.Empty)} [System.Runtime] System.Threading.Tasks.ValueTask{(isGeneric ? "`1<!0>" : string.Empty)}"
            : $"{(includeClassIndication ? "class" : string.Empty)} [System.Runtime] System.Threading.Tasks.Task{(isGeneric ? "`1<!0>" : string.Empty)}";
        
        string builderClassName = typeContainer.Reference.Name.ToString().StartsWith("System.Threading.Tasks.ValueTask") 
            ? $"valuetype [System.Runtime]System.Runtime.CompilerServices.AsyncValueTaskMethodBuilder{ToGenericArity1(returnType)}"
            : $"valuetype [System.Runtime]System.Runtime.CompilerServices.AsyncTaskMethodBuilder{ToGenericArity1(returnType)}"; 

        if(!rewrite) {
            return $$$"""
                ldarg.0
                call instance void {{{stateMachineFullName}}}::MoveNext__inoculated()
                {{{CallMethodOnInterceptors(stateMachineFullName, modifierClasses, "OnEntry", false, ref labelIdx)}}}

                ldarg.0
                ldflda {{{builderClassName}}} {{{stateMachineFullName}}}::'<>t__builder'
                call instance {{{TaskVariantType(true, !returnType.IsVoid)}}} {{{builderClassName}}}::get_Task()
                {{{(
                    TaskVariantType(true, false).StartsWith("class") 
                    ? $@" 
                        stloc.s ttask
                    "
                    : $@"
                        stloc.s ltask
                        ldloca.s ltask
                        call instance class [System.Runtime]System.Threading.Tasks.Task{(returnType.IsVoid ? string.Empty : "`1<!0>")} valuetype [System.Runtime]System.Threading.Tasks.ValueTask{ToGenericArity1(returnType)}::AsTask()
                        stloc.s ttask
                    "
                )}}}
                ldloc.s ttask
                call instance class [System.Runtime]System.AggregateException [System.Runtime]System.Threading.Tasks.Task::get_Exception()
                stloc.s e

                ldarg.0
                ldfld class [Inoculator.Interceptors]Inoculator.Builder.MethodData {{{stateMachineFullName}}}::'<inoculated>__Metadata'
                ldloc.s e
                callvirt instance void [Inoculator.Interceptors]Inoculator.Builder.MethodData::set_Exception(class [System.Runtime]System.Exception)

                ldloc.s e
                brtrue.s ***FAILURE***

                ldarg.0
                ldfld class [Inoculator.Interceptors]Inoculator.Builder.MethodData {{{stateMachineFullName}}}::'<inoculated>__Metadata'
                {{{(
                    returnType.IsVoid
                    ? $@"
                        ldnull
                        callvirt instance void [Inoculator.Interceptors]Inoculator.Builder.MethodData::set_ReturnValue(class [Inoculator.Interceptors]Inoculator.Builder.ParameterData)"
                    : $@"
                        ldloc.s ttask
                        callvirt instance !0 class [System.Runtime]System.Threading.Tasks.Task{ToGenericArity1(returnType)}::get_Result()
                        {(
                            returnType.IsReferenceType ? string.Empty
                            : $"box {(returnType.IsGeneric ? $"!{returnType.PureName}" : returnType.Name)}"
                        )}
                        dup
                        callvirt instance class [System.Runtime]System.Type [System.Runtime]System.Object::GetType()
                        ldnull
                        newobj instance void class [Inoculator.Interceptors]Inoculator.Builder.ParameterData::.ctor(object,class [System.Runtime]System.Type,string)
                        callvirt instance void [Inoculator.Interceptors]Inoculator.Builder.MethodData::set_ReturnValue(class [Inoculator.Interceptors]Inoculator.Builder.ParameterData)"
                )}}}
            """;
        } else {
            string callCode = $"callvirt instance class [Inoculator.Interceptors]Inoculator.Builder.MethodData class {rewriterClass}::OnCall(class [Inoculator.Interceptors]Inoculator.Builder.MethodData)";
            return $$$"""
                .try 
                {
                    ldarg.0
                    dup
                    ldfld class {{{rewriterClass}}} {{{stateMachineFullName}}}::{{{GenerateInterceptorName(rewriterClass)}}}
                    ldarg.0
                    ldfld class [Inoculator.Interceptors]Inoculator.Builder.MethodData {{{stateMachineFullName}}}::'<inoculated>__Metadata'
                    {{{callCode}}}
                    stfld class [Inoculator.Interceptors]Inoculator.Builder.MethodData {{{stateMachineFullName}}}::'<inoculated>__Metadata'
                    {{{CallMethodOnInterceptors(stateMachineFullName, modifierClasses, "OnEntry", false, ref labelIdx)}}}

                    ldarg.0
                    ldfld class [Inoculator.Interceptors]Inoculator.Builder.MethodData {{{stateMachineFullName}}}::'<inoculated>__Metadata'
                    callvirt instance class [System.Runtime]System.Exception [Inoculator.Interceptors]Inoculator.Builder.MethodData::get_Exception()
                    dup
                    stloc.0
                    brtrue.s ***INNER_FAILURE***

                    {{{(
                        returnType.IsVoid
                            ? String.Empty
                            : $@"
                                ldarg.0
                                ldflda {builderClassName} {stateMachineFullName}::'<>t__builder'
                                ldarg.0
                                ldfld class [Inoculator.Interceptors]Inoculator.Builder.MethodData {stateMachineFullName}::'<inoculated>__Metadata'
                                callvirt instance class [Inoculator.Interceptors]Inoculator.Builder.ParameterData [Inoculator.Interceptors]Inoculator.Builder.MethodData::get_ReturnValue()
                                callvirt instance object [Inoculator.Interceptors]Inoculator.Builder.ParameterData::get_Value()
                                {(returnType.IsReferenceType ? $"castclass {returnType.Name}" : $"unbox.any {returnType.ToProperName}")}
                            "
                    )}}}
                    call instance void {{{builderClassName}}}::SetResult(!0)
                    {{{GetNextLabel(ref labelIdx, jumptable, "INNER_FAILURE")}}}: nop
                    leave.s ***OUTSIDE***
                } 
                catch [System.Runtime]System.Exception 
                {
                    stloc.0

                    ldarg.0
                    ldfld class [Inoculator.Interceptors]Inoculator.Builder.MethodData {{{stateMachineFullName}}}::'<inoculated>__Metadata'
                    ldloc.0
                    callvirt instance void [Inoculator.Interceptors]Inoculator.Builder.MethodData::set_Exception(class [System.Runtime]System.Exception)
                    
                    ldarg.0
                    ldflda {{{builderClassName}}} {{{stateMachineFullName}}}::'<>t__builder'
                    ldloc.0
                    call instance void {{{builderClassName}}}::SetException(class [System.Runtime]System.Exception)
                    leave.s ***OUTSIDE***
                }
                {{{GetNextLabel(ref labelIdx, jumptable, "OUTSIDE")}}}: ldarg.0
                ldc.i4.s -2
                stfld int32 {{{stateMachineFullName}}}::'<>1__state'

                ldarg.0
                ldfld class [Inoculator.Interceptors]Inoculator.Builder.MethodData {{{stateMachineFullName}}}::'<inoculated>__Metadata'
                callvirt instance class [System.Runtime]System.Exception [Inoculator.Interceptors]Inoculator.Builder.MethodData::get_Exception()
                dup
                stloc.0
                brtrue.s ***FAILURE***
            """;
        }
    }
}