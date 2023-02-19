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

public static class EnumRewriter {
    public static Result<(ClassDecl.Class[], MethodDecl.Method[]), Exception> Rewrite(ClassDecl.Class classRef, MethodData metadata, InterceptorData[] interceptors, IEnumerable<string> path)
    {
        bool isContainedInStruct = classRef.Header.Extends.Type.ToString() == "[System.Runtime] System.ValueType";
        var typeContainer = metadata.Code.Header.Type.Components.Types.Values.First() as TypeDecl.CustomTypeReference;
        var itemType = new TypeData(typeContainer.Reference.GenericTypes?.Types.Values.FirstOrDefault()?.ToString() ?? "object");
            
        var oldClassMangledName= $"'<>__{Math.Abs(classRef.Header.Id.GetHashCode())}_old'";
        var oldClassRef = Parse<Class>(classRef.ToString().Replace(classRef.Header.Id.ToString(), oldClassMangledName));
        var oldMethodInstance = Parse<Method>(metadata.Code.ToString()
            .Replace(classRef.Header.Id.ToString(), oldClassMangledName)
            .Replace(metadata.Code.Header.Name.ToString(), metadata.MangledName(false))
        );
        var MoveNextHandler = GetMoveNextHandler(metadata, itemType, classRef, path, isContainedInStruct, interceptors);
        var newClassRef = InjectInoculationFields(classRef, MoveNextHandler, interceptors);
        metadata = RewriteInceptionPoint(classRef, metadata, interceptors, path, isContainedInStruct);

        return Success<(ClassDecl.Class[], MethodDecl.Method[]), Exception>.From((new Class[] { oldClassRef, newClassRef }, new Method[] { oldMethodInstance, metadata.Code }));
    }

    private static MethodData RewriteInceptionPoint(Class classRef, MethodData metadata, InterceptorData[] interceptorsClasses, IEnumerable<string> path, bool isReleaseMode)
    {
        int labelIdx = 0;
        bool isToBeRewritten = interceptorsClasses.Any(i => i.IsRewriter);
        var stateMachineFullName = StringifyPath(metadata, classRef.Header, path, isReleaseMode, 2);
        var inoculationSightName = StringifyPath(metadata, classRef.Header, path, isReleaseMode, 0);

        int argumentsCount = metadata.IsStatic 
            ? metadata.Code.Header.Parameters.Parameters.Values.Length 
            : metadata.Code.Header.Parameters.Parameters.Values.Length + 1;

        metadata.Code = metadata.Code with
        {
            Body = metadata.Code.Body with
            {
                Items = new ARRAY<MethodDecl.Member>(
                            metadata.Code.Body.Items.Values
                                .SelectMany(item =>
                                {
                                    if (item is MethodDecl.LabelItem label)
                                    {
                                        return new[] { 
                                            label with {
                                                Value = new CodeLabel(new SimpleName(GetNextLabel(ref labelIdx)))
                                            }
                                        };
                                    } else if(item is MethodDecl.MaxStackItem stack) {
                                        return new[] {
                                            stack with {
                                                Value = new INT(16, 32, false)
                                            }
                                        };
                                    }
                                    else if (item is MethodDecl.InstructionItem instruction)
                                    {
                                        if (instruction.Value.Opcode == "ret")
                                        {
                                            var injectionCode = $$$"""
                                        dup
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
                                        {{{String.Join("\n", 
                                            interceptorsClasses.Select(
                                                    (attrClassName, i) => $"""
                                            dup
                                            {GetAttributeInstance(metadata, inoculationSightName, attrClassName)}
                                            stfld class {attrClassName.ClassName} {stateMachineFullName}::{GenerateInterceptorName(attrClassName.ClassName)}
                                            """
                                        ))}}}
                                        ret
                                        """;
                                            var res = Parse<MethodDecl.Member.Collection>(injectionCode);
                                            return res.Items.Values;
                                        }
                                    }
                                    return new[] { item };
                                }).ToArray()
                        )
                {
                    Options = new ARRAY<MethodDecl.Member>.ArrayOptions()
                    {
                        Delimiters = ('\0', '\n', '\0')
                    }
                }
            }
        };
        return metadata;
    }

    private static ClassDecl.Class InjectInoculationFields(Class classRef, Func<ClassDecl.MethodDefinition, ClassDecl.MethodDefinition[]> MoveNextHandler, InterceptorData[] interceptorsClasses)
    {
        bool isToBeRewritten = interceptorsClasses.Any(i => i.IsRewriter);

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

    private static Func<ClassDecl.MethodDefinition, ClassDecl.MethodDefinition[]> GetMoveNextHandler(MethodData metadata, TypeData returnType, Class classRef, IEnumerable<string> path, bool isContainedInStruct, InterceptorData[] modifierClasses) {
        return (MethodDefinition methodDef) => {
            bool isToBeRewritten = modifierClasses.Any(i => i.IsRewriter);
            int labelIdx = 0;
            Dictionary<string, string> jumptable = new();

            var stateMachineFullName = StringifyPath(metadata, classRef.Header, path, isContainedInStruct, 1);

            if(methodDef.Value.Header.Name.ToString() != "MoveNext") return new [] { methodDef };
            var method = methodDef.Value;
            StringBuilder builder = new();
            builder.AppendLine($".method {method.Header} {{");
            foreach (var member in method.Body.Items.Values)
            {
                if  (member is MethodDecl.LabelItem 
                            or MethodDecl.InstructionItem 
                            or MethodDecl.LocalsItem 
                            or MethodDecl.MaxStackItem
                            or MethodDecl.ExceptionHandlingItem
                            or MethodDecl.ScopeBlock
                    ) continue;
                builder.AppendLine(member.ToString());
            }

            builder.AppendLine($".maxstack 8");

            builder.AppendLine($".locals init (bool, class [System.Runtime]System.Exception e)");            

            builder.Append($$$"""
                ldarg.0
                ldfld int32 {{{stateMachineFullName}}}::'<>1__state'
                brtrue.s ***JUMPDEST1***


                {{{CallMethodOnInterceptors(stateMachineFullName, modifierClasses, "OnEntry", false, ref labelIdx)}}}
                {{{GetNextLabel(ref labelIdx, jumptable, "JUMPDEST1")}}}: nop
                .try {
                    .try {
                        {{{CallMethodOnInterceptors(stateMachineFullName, modifierClasses, "OnBegin", false, ref labelIdx)}}}
                        {{{InvokeFunction(stateMachineFullName, returnType, ref labelIdx, modifierClasses, jumptable)}}}
                        {{{CallMethodOnInterceptors(stateMachineFullName, modifierClasses, "OnSuccess", false, ref labelIdx)}}}
                        leave.s ***END***
                    } catch [System.Runtime]System.Exception
                    {
                        {{{CallMethodOnInterceptors(stateMachineFullName, modifierClasses, "OnEnd", false, ref labelIdx)}}}
                        stloc.s e
                        ldarg.0
                        ldfld class [Inoculator.Interceptors]Inoculator.Builder.MethodData {{{stateMachineFullName}}}::'<inoculated>__Metadata'
                        ldloc.s e
                        callvirt instance void [Inoculator.Interceptors]Inoculator.Builder.MethodData::set_Exception(class [System.Runtime]System.Exception)
                        {{{CallMethodOnInterceptors(stateMachineFullName, modifierClasses, "OnException", false, ref labelIdx)}}}
                        ldloc.s e
                        throw
                    } 
                } finally
                {
                    ldarg.0
                    ldfld int32 {{{stateMachineFullName}}}::'<>1__state'
                    ldc.i4.m1
                    bne.un.s ***JUMPDEST2***

                    {{{CallMethodOnInterceptors(stateMachineFullName, modifierClasses, "OnExit", false, ref labelIdx)}}}
                    {{{GetNextLabel(ref labelIdx, jumptable, "JUMPDEST2")}}}: endfinally
                }
                {{{GetNextLabel(ref labelIdx, jumptable, "END")}}}: nop
                ldloc.0
                ret
            }}
            """);

            foreach(var (label, idx) in jumptable)
            {
                builder.Replace($"***{label}***", idx.ToString());
            }
            
            var newFunction = new ClassDecl.MethodDefinition(
                Dove.Core.Parser.Parse<MethodDecl.Method>(builder.ToString())
            );
            
            var oldFunction = methodDef with {
                Value = methodDef.Value with {
                    Header = methodDef.Value.Header with {
                        Name = Parse<MethodName>("MoveNext__inoculated")
                    },
                    Body = methodDef.Value.Body with {
                        Items = new ARRAY<MethodDecl.Member>(
                            methodDef.Value.Body
                                .Items.Values.Where(member => member is not MethodDecl.OverrideMethodItem)
                                .Select(member => {
                                    var instructionLine = member.ToString().Replace("::" + metadata.Name(false), "::" + metadata.MangledName(false));
                                    return Parse<MethodDecl.Member>(instructionLine);
                                })
                                .ToArray()
                        ) {
                            Options = new ARRAY<MethodDecl.Member>.ArrayOptions() {
                                Delimiters = ('\0', '\n', '\0')
                            }
                        }
                    }
                }
            };


            return new [] { newFunction, oldFunction };
        };
    }

    private static string InvokeFunction(string stateMachineFullName, TypeData returnType, ref int labelIdx, InterceptorData[] modifierClasses, Dictionary<string, string> jumptable) {
        static string ToGenericArity1(TypeData type) => type.IsVoid ? String.Empty : type.IsGeneric ? $"`1<!{type.PureName}>" : $"`1<{type.Name}>";
        string? rewriterClass = modifierClasses?.FirstOrDefault(m => m.IsRewriter)?.ClassName;
        var rewrite = rewriterClass != null;

        if(!rewrite) {
            return $$$"""
                ldarg.0
                call instance bool {{{stateMachineFullName}}}::MoveNext__inoculated()
                stloc.0
                {{{CallMethodOnInterceptors(stateMachineFullName, modifierClasses, "OnEnd", false, ref labelIdx)}}}
                ldarg.0
                ldfld class [Inoculator.Interceptors]Inoculator.Builder.MethodData {{{stateMachineFullName}}}::'<inoculated>__Metadata'
                
                ldarg.0
                ldfld {{{(returnType.IsGeneric ? $"!{returnType.PureName}" : returnType.Name)}}} {{{stateMachineFullName}}}::'<>2__current'
                {{{(
                    returnType.IsReferenceType ? String.Empty
                    : $@"box {(returnType.IsGeneric ? $"!{returnType.PureName}" : returnType.Name)}"
                )}}}
                dup
                callvirt instance class [System.Runtime]System.Type [System.Runtime]System.Object::GetType()
                ldnull
                newobj instance void class [Inoculator.Interceptors]Inoculator.Builder.ParameterData::.ctor(object,class [System.Runtime]System.Type,string)
                callvirt instance void [Inoculator.Interceptors]Inoculator.Builder.MethodData::set_ReturnValue(class [Inoculator.Interceptors]Inoculator.Builder.ParameterData)
            """;
        } else {
            string callCode = $"callvirt instance class [Inoculator.Interceptors]Inoculator.Builder.MethodData class {rewriterClass}::OnCall(class [Inoculator.Interceptors]Inoculator.Builder.MethodData)";
            return $$$"""

                ldarg.0
                dup
                ldfld class {{{rewriterClass}}} {{{stateMachineFullName}}}::{{{GenerateInterceptorName(rewriterClass)}}}
                ldarg.0
                ldfld class [Inoculator.Interceptors]Inoculator.Builder.MethodData {{{stateMachineFullName}}}::'<inoculated>__Metadata'
                {{{callCode}}}
                stfld class [Inoculator.Interceptors]Inoculator.Builder.MethodData {{{stateMachineFullName}}}::'<inoculated>__Metadata'
                {{{CallMethodOnInterceptors(stateMachineFullName, modifierClasses, "OnEnd", false, ref labelIdx)}}}
                
                ldarg.0
                dup
                ldfld class [Inoculator.Interceptors]Inoculator.Builder.MethodData {{{stateMachineFullName}}}::'<inoculated>__Metadata'
                callvirt instance class [Inoculator.Interceptors]Inoculator.Builder.ParameterData [Inoculator.Interceptors]Inoculator.Builder.MethodData::get_ReturnValue()
                callvirt instance object [Inoculator.Interceptors]Inoculator.Builder.ParameterData::get_Value()
                {{{(returnType.IsReferenceType ? $"castclass {returnType.Name}" : $"unbox.any {returnType.ToProperName}")}}}
                stfld {{{(returnType.IsGeneric ? $"!{returnType.PureName}" : returnType.Name)}}} {{{stateMachineFullName}}}::'<>2__current'
                
                
                ldarg.0
                ldfld class [Inoculator.Interceptors]Inoculator.Builder.MethodData {{{stateMachineFullName}}}::'<inoculated>__Metadata'
                callvirt instance bool [Inoculator.Interceptors]Inoculator.Builder.MethodData::get_Stop()
                brtrue.s ***SKIP***

                ldarg.0
                ldc.i4.1
                stfld int32 {{{stateMachineFullName}}}::'<>1__state'
                ldc.i4.1
                stloc.0
                br.s ***NEXT***
                {{{GetNextLabel(ref labelIdx, jumptable, "SKIP")}}}: nop
                
                ldarg.0
                ldc.i4.m1
                stfld int32 {{{stateMachineFullName}}}::'<>1__state'
                ldc.i4.0
                stloc.0
                {{{GetNextLabel(ref labelIdx, jumptable, "NEXT")}}}: nop
            """;
        }
    }
}