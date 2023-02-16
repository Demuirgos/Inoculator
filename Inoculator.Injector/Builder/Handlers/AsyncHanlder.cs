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

public static class AsyncRewriter {
    public static Result<(ClassDecl.Class[], MethodDecl.Method[]), Exception> Rewrite(ClassDecl.Class classRef, MethodData metadata, InterceptorData[] interceptors, IEnumerable<string> path)
    {
        bool isReleaseMode = classRef.Header.Extends.Type.ToString() == "[System.Runtime] System.ValueType";
        var typeContainer = metadata.Code.Header.Type.Components.Types.Values.First() as TypeDecl.CustomTypeReference;
        var itemType = new TypeData(typeContainer.Reference.GenericTypes?.Types.Values.FirstOrDefault()?.ToString() ?? "void");

        var oldClassMangledName= $"'<>__{Math.Abs(classRef.Header.Id.GetHashCode())}_old'";
        var oldClassRef = Parse<Class>(classRef.ToString().Replace(classRef.Header.Id.ToString(), oldClassMangledName));
        var oldMethodInstance = Parse<Method>(metadata.Code.ToString()
            .Replace(classRef.Header.Id.ToString(), oldClassMangledName)
            .Replace(metadata.Code.Header.Name.ToString(), $"'<>__{metadata.Code.Header.Name}_old'")
        );
        var MoveNextHandler = GetNextMethodHandler(metadata, itemType, classRef, path, isReleaseMode, interceptors);
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
        
        string stackClause = ".maxstack 16";
        var LocalsClause = metadata.Code.Body.Items.Values.OfType<MethodDecl.LocalsItem>().FirstOrDefault();

        var oldInstructions = metadata.Code.Body.Items.Values.OfType<MethodDecl.InstructionItem>();
        var newInstructions = new List<MethodDecl.InstructionItem>();
        if(!isReleaseMode) {
            newInstructions.AddRange(oldInstructions.Take(2));
        }


        string injectionCode = $$$"""
            {{{loadLocalStateMachine}}}
            ldstr "{{{metadata.Code.ToString().Replace("\n", " ")}}}"
            ldstr "{{{metadata.ClassReference.ToString().Replace("\n", " ")}}}"
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

    private static Func<ClassDecl.MethodDefinition, ClassDecl.MethodDefinition[]> GetNextMethodHandler(MethodData metadata, TypeData returnType, Class classRef, IEnumerable<string> path, bool isReleaseMode, InterceptorData[] modifierClasses)
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


            builder.AppendLine($$$"""
                .maxstack 8
                .locals init (class [System.Runtime]System.Exception e)
            """);

            builder.Append($$$"""
                {{{GetNextLabel(ref labelIdx)}}}: ldarg.0
                {{{GetNextLabel(ref labelIdx)}}}: ldfld int32 {{{stateMachineFullName}}}::'<>1__state'
                {{{GetNextLabel(ref labelIdx)}}}: ldc.i4.m1
                {{{GetNextLabel(ref labelIdx)}}}: bne.un.s ***JUMPDEST1***


                {{{String.Join("\n",
                    modifierClasses?.Where(m => m.IsInterceptor).Select(
                        (attrClassName, i) => $@"
                        {GetNextLabel(ref labelIdx)}: ldarg.0
                        {GetNextLabel(ref labelIdx)}: ldfld class {attrClassName} {stateMachineFullName}::{GenerateInterceptorName(attrClassName.ClassName)}
                        {GetNextLabel(ref labelIdx)}: ldarg.0
                        {GetNextLabel(ref labelIdx)}: ldfld class [Inoculator.Interceptors]Inoculator.Builder.MethodData {stateMachineFullName}::'<inoculated>__Metadata'
                        {GetNextLabel(ref labelIdx)}: callvirt instance void class {attrClassName}::OnEntry(class [Inoculator.Interceptors]Inoculator.Builder.MethodData)"
                ))}}}

                {{{GetNextLabel(ref labelIdx, jumptable, "JUMPDEST1")}}}: nop

                {{{InvokeFunction(stateMachineFullName, returnType, ref labelIdx, modifierClasses.FirstOrDefault(m => m.IsRewriter)?.ClassName, isToBeRewritten, jumptable)}}}

                {{{GetNextLabel(ref labelIdx, jumptable, "SUCCESS")}}}: nop
                {{{String.Join("\n",
                    modifierClasses?.Where(m => m.IsInterceptor).Select(
                        (attrClassName, i) => $@"
                        {GetNextLabel(ref labelIdx)}: ldarg.0
                        {GetNextLabel(ref labelIdx)}: ldfld class {attrClassName.ClassName} {stateMachineFullName}::{GenerateInterceptorName(attrClassName.ClassName)}
                        {GetNextLabel(ref labelIdx)}: ldarg.0
                        {GetNextLabel(ref labelIdx)}: ldfld class [Inoculator.Interceptors]Inoculator.Builder.MethodData {stateMachineFullName}::'<inoculated>__Metadata'
                        {GetNextLabel(ref labelIdx)}: callvirt instance void class {attrClassName.ClassName}::OnSuccess(class [Inoculator.Interceptors]Inoculator.Builder.MethodData)"
                ))}}}
                {{{GetNextLabel(ref labelIdx)}}}: br.s ***EXIT***

                {{{GetNextLabel(ref labelIdx, jumptable, "FAILURE")}}}: nop
                {{{String.Join("\n",
                    modifierClasses?.Where(m => m.IsInterceptor).Select(
                        (attrClassName, i) => $@"
                        {GetNextLabel(ref labelIdx)}: ldarg.0
                        {GetNextLabel(ref labelIdx)}: ldfld class {attrClassName.ClassName} {stateMachineFullName}::{GenerateInterceptorName(attrClassName.ClassName)}
                        {GetNextLabel(ref labelIdx)}: ldarg.0
                        {GetNextLabel(ref labelIdx)}: ldfld class [Inoculator.Interceptors]Inoculator.Builder.MethodData {stateMachineFullName}::'<inoculated>__Metadata'
                        {GetNextLabel(ref labelIdx)}: callvirt instance void class {attrClassName.ClassName}::OnException(class [Inoculator.Interceptors]Inoculator.Builder.MethodData)"
                ))}}}
                {{{GetNextLabel(ref labelIdx)}}}: br.s ***EXIT***

                {{{GetNextLabel(ref labelIdx, jumptable, "EXIT")}}}: ldarg.0
                {{{GetNextLabel(ref labelIdx)}}}: ldfld int32 {{{stateMachineFullName}}}::'<>1__state'
                {{{GetNextLabel(ref labelIdx)}}}: ldc.i4.s -2
                {{{GetNextLabel(ref labelIdx)}}}: bne.un.s ***JUMPDEST2***
                {{{String.Join("\n",
                    modifierClasses?.Where(m => m.IsInterceptor).Select(
                        (attrClassName, i) => $@"
                        {GetNextLabel(ref labelIdx)}: ldarg.0
                        {GetNextLabel(ref labelIdx)}: ldfld class {attrClassName.ClassName} {stateMachineFullName}::{GenerateInterceptorName(attrClassName.ClassName)}
                        {GetNextLabel(ref labelIdx)}: ldarg.0
                        {GetNextLabel(ref labelIdx)}: ldfld class [Inoculator.Interceptors]Inoculator.Builder.MethodData {stateMachineFullName}::'<inoculated>__Metadata'
                        {GetNextLabel(ref labelIdx)}: callvirt instance void class {attrClassName.ClassName}::OnExit(class [Inoculator.Interceptors]Inoculator.Builder.MethodData)"
                ))}}}

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

    private static string InvokeFunction(string stateMachineFullName, TypeData returnType, ref int labelIdx, string? rewriterClass, bool rewrite, Dictionary<string, string> jumptable) {
        static string ToGenericArity1(TypeData type) => type.IsVoid ? String.Empty : type.IsGeneric ? $"`1<!{type.PureName}>" : $"`1<{type.Name}>";
        
        if(!rewrite) {
            return $$$"""
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
                        {GetNextLabel(ref labelIdx)}: callvirt instance void [Inoculator.Interceptors]Inoculator.Builder.MethodData::set_ReturnValue(class [Inoculator.Interceptors]Inoculator.Builder.ParameterData)"
                    : $@"
                        {GetNextLabel(ref labelIdx)}: ldarg.0
                        {GetNextLabel(ref labelIdx)}: ldflda valuetype [System.Runtime]System.Runtime.CompilerServices.AsyncTaskMethodBuilder{ToGenericArity1(returnType)} {stateMachineFullName}::'<>t__builder'
                        {GetNextLabel(ref labelIdx)}: call instance class [System.Runtime]System.Threading.Tasks.Task`1<!0> valuetype [System.Runtime]System.Runtime.CompilerServices.AsyncTaskMethodBuilder{ToGenericArity1(returnType)}::get_Task()
                        {GetNextLabel(ref labelIdx)}: callvirt instance !0 class [System.Runtime]System.Threading.Tasks.Task{ToGenericArity1(returnType)}::get_Result()
                        {(
                            returnType.IsReferenceType ? string.Empty
                            : $"{GetNextLabel(ref labelIdx)}: box {(returnType.IsGeneric ? $"!{returnType.PureName}" : returnType.Name)}"
                        )}
                        {GetNextLabel(ref labelIdx)}: dup
                        {GetNextLabel(ref labelIdx)}: callvirt instance class [System.Runtime]System.Type [System.Runtime]System.Object::GetType()
                        {GetNextLabel(ref labelIdx)}: ldnull
                        {GetNextLabel(ref labelIdx)}: newobj instance void class [Inoculator.Interceptors]Inoculator.Builder.ParameterData::.ctor(object,class [System.Runtime]System.Type,string)
                        {GetNextLabel(ref labelIdx)}: callvirt instance void [Inoculator.Interceptors]Inoculator.Builder.MethodData::set_ReturnValue(class [Inoculator.Interceptors]Inoculator.Builder.ParameterData)"
                )}}}
            """;
        } else {
            string callCode = $"callvirt instance class [Inoculator.Interceptors]Inoculator.Builder.MethodData class {rewriterClass}::OnCall(class [Inoculator.Interceptors]Inoculator.Builder.MethodData)";
            return $$$"""
                .try 
                {
                    {{{GetNextLabel(ref labelIdx)}}}: ldarg.0
                    {{{GetNextLabel(ref labelIdx)}}}: dup
                    {{{GetNextLabel(ref labelIdx)}}}: ldfld class {{{rewriterClass}}} {{{stateMachineFullName}}}::{{{GenerateInterceptorName(rewriterClass)}}}
                    {{{GetNextLabel(ref labelIdx)}}}: ldarg.0
                    {{{GetNextLabel(ref labelIdx)}}}: ldfld class [Inoculator.Interceptors]Inoculator.Builder.MethodData {{{stateMachineFullName}}}::'<inoculated>__Metadata'
                    {{{GetNextLabel(ref labelIdx)}}}: {{{callCode}}}
                    {{{GetNextLabel(ref labelIdx)}}}: stfld class [Inoculator.Interceptors]Inoculator.Builder.MethodData {{{stateMachineFullName}}}::'<inoculated>__Metadata'

                    {{{GetNextLabel(ref labelIdx)}}}: ldarg.0
                    {{{GetNextLabel(ref labelIdx)}}}: ldfld class [Inoculator.Interceptors]Inoculator.Builder.MethodData {{{stateMachineFullName}}}::'<inoculated>__Metadata'
                    {{{GetNextLabel(ref labelIdx)}}}: callvirt instance class [System.Runtime]System.Exception [Inoculator.Interceptors]Inoculator.Builder.MethodData::get_Exception()
                    {{{GetNextLabel(ref labelIdx)}}}: dup
                    {{{GetNextLabel(ref labelIdx)}}}: stloc.0
                    {{{GetNextLabel(ref labelIdx)}}}: brtrue.s ***INNER_FAILURE***

                    {{{(
                        returnType.IsVoid
                            ? String.Empty
                            : $@"
                                {GetNextLabel(ref labelIdx)}: ldarg.0
                                {GetNextLabel(ref labelIdx)}: ldflda valuetype [System.Runtime]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<{returnType.Name}> {stateMachineFullName}::'<>t__builder'
                                {GetNextLabel(ref labelIdx)}: ldarg.0
                                {GetNextLabel(ref labelIdx)}: ldfld class [Inoculator.Interceptors]Inoculator.Builder.MethodData {stateMachineFullName}::'<inoculated>__Metadata'
                                {GetNextLabel(ref labelIdx)}: callvirt instance class [Inoculator.Interceptors]Inoculator.Builder.ParameterData [Inoculator.Interceptors]Inoculator.Builder.MethodData::get_ReturnValue()
                                {GetNextLabel(ref labelIdx)}: callvirt instance object [Inoculator.Interceptors]Inoculator.Builder.ParameterData::get_Value()
                                {GetNextLabel(ref labelIdx)}: {(returnType.IsReferenceType ? $"castclass {returnType.Name}" : $"unbox.any {returnType.ToProperName}")}
                            "
                    )}}}
                    {{{GetNextLabel(ref labelIdx)}}}: call instance void valuetype [System.Runtime]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<{{{returnType.Name}}}>::SetResult(!0)
                    {{{GetNextLabel(ref labelIdx, jumptable, "INNER_FAILURE")}}}: nop
                    {{{GetNextLabel(ref labelIdx)}}}: leave.s ***OUTSIDE***
                } 
                catch [System.Runtime]System.Exception 
                {
                    {{{GetNextLabel(ref labelIdx)}}}: stloc.0

                    {{{GetNextLabel(ref labelIdx)}}}: ldarg.0
                    {{{GetNextLabel(ref labelIdx)}}}: ldfld class [Inoculator.Interceptors]Inoculator.Builder.MethodData {{{stateMachineFullName}}}::'<inoculated>__Metadata'
                    {{{GetNextLabel(ref labelIdx)}}}: ldloc.0
                    {{{GetNextLabel(ref labelIdx)}}}: callvirt instance void [Inoculator.Interceptors]Inoculator.Builder.MethodData::set_Exception(class [System.Runtime]System.Exception)
                    
                    {{{GetNextLabel(ref labelIdx)}}}: ldarg.0
                    {{{GetNextLabel(ref labelIdx)}}}: ldflda valuetype [System.Runtime]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<{{{returnType.Name}}}> {{{stateMachineFullName}}}::'<>t__builder'
                    {{{GetNextLabel(ref labelIdx)}}}: ldloc.0
                    {{{GetNextLabel(ref labelIdx)}}}: call instance void valuetype [System.Runtime]System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1<{{{returnType.Name}}}>::SetException(class [System.Runtime]System.Exception)
                    {{{GetNextLabel(ref labelIdx)}}}: leave.s ***OUTSIDE***
                }
                {{{GetNextLabel(ref labelIdx, jumptable, "OUTSIDE")}}}: ldarg.0
                {{{GetNextLabel(ref labelIdx)}}}: ldc.i4.s -2
                {{{GetNextLabel(ref labelIdx)}}}: stfld int32 {{{stateMachineFullName}}}::'<>1__state'

                {{{GetNextLabel(ref labelIdx)}}}: ldarg.0
                {{{GetNextLabel(ref labelIdx)}}}: ldfld class [Inoculator.Interceptors]Inoculator.Builder.MethodData {{{stateMachineFullName}}}::'<inoculated>__Metadata'
                {{{GetNextLabel(ref labelIdx)}}}: callvirt instance class [System.Runtime]System.Exception [Inoculator.Interceptors]Inoculator.Builder.MethodData::get_Exception()
                {{{GetNextLabel(ref labelIdx)}}}: dup
                {{{GetNextLabel(ref labelIdx)}}}: stloc.0
                {{{GetNextLabel(ref labelIdx)}}}: brtrue.s ***FAILURE***
            """;
        }
    }
}