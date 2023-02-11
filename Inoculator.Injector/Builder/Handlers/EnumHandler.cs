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
    public static Result<(ClassDecl.Class[], MethodDecl.Method[]), Exception> Rewrite(ClassDecl.Class classRef, MethodData metadata, string[] interceptors, IEnumerable<string> path)
    {
        bool isContainedInStruct = classRef.Header.Extends.Type.ToString() == "[System.Runtime] System.ValueType";
        var typeContainer = metadata.Code.Header.Type.Components.Types.Values.First() as TypeDecl.CustomTypeReference;
        var itemType = new TypeData(typeContainer.Reference.GenericTypes?.Types.Values.FirstOrDefault()?.ToString() ?? "object");
            
        var oldClassMangledName= $"'<>__{Math.Abs(classRef.Header.Id.GetHashCode())}_old'";
        var oldClassRef = Parse<Class>(classRef.ToString().Replace(classRef.Header.Id.ToString(), oldClassMangledName));
        var oldMethodInstance = Parse<Method>(metadata.Code.ToString()
            .Replace(classRef.Header.Id.ToString(), oldClassMangledName)
            .Replace(metadata.Code.Header.Name.ToString(), $"'<>__{metadata.Name(false)}_old'")
        );
        var MoveNextHandler = GetMoveNextHandler(itemType, classRef, path, isContainedInStruct, interceptors);
        var newClassRef = InjectInoculationFields(classRef, MoveNextHandler, interceptors);
        metadata = RewriteInceptionPoint(classRef, metadata, interceptors, path, isContainedInStruct);

        return Success<(ClassDecl.Class[], MethodDecl.Method[]), Exception>.From((new Class[] { oldClassRef, newClassRef }, new Method[] { oldMethodInstance, metadata.Code }));
    }

    private static MethodData RewriteInceptionPoint(Class classRef, MethodData metadata, string[] interceptorsClasses, IEnumerable<string> path, bool isReleaseMode)
    {
        int labelIdx = 0;

        var stateMachineFullNameBuilder = new StringBuilder()
            .Append(isReleaseMode ? " valuetype " : " class ")
            .Append($"{String.Join("/", path)}")
            .Append($"/{classRef.Header.Id}");
        if (classRef.Header.TypeParameters?.Parameters.Values.Length > 0)
        {
            var classTypeParametersCount = classRef.Header.TypeParameters.Parameters.Values.Length;
            var functionTypeParametersCount = metadata.Code.Header.TypeParameters?.Parameters.Values.Length ?? 0;
            var classTPs = classRef.Header.TypeParameters.Parameters.Values.Take(classTypeParametersCount - functionTypeParametersCount).Select(p => $"!{p}");
            var methodTPs = classRef.Header.TypeParameters.Parameters.Values.TakeLast(functionTypeParametersCount).Select(p => $"!!{p}");
            stateMachineFullNameBuilder.Append("<")
                .Append( String.Join(",", classTPs.Union(methodTPs)))
                .Append(">");
        }
        var stateMachineFullName = stateMachineFullNameBuilder.ToString();
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
                                        {{{GetNextLabel(ref labelIdx)}}}: dup
                                        {{{GetNextLabel(ref labelIdx)}}}: ldstr "{{{metadata.Code.ToString().Replace("\n", " ")}}}"
                                        {{{GetNextLabel(ref labelIdx)}}}: ldstr "{{{metadata.ClassReference.ToString().Replace("\n", " ")}}}"
                                        {{{GetNextLabel(ref labelIdx)}}}: ldstr "{{{String.Join("/", path)}}}"
                                        {{{GetNextLabel(ref labelIdx)}}}: newobj instance void [Inoculator.Interceptors]Inoculator.Builder.MethodData::.ctor(string, string, string)

                                        {{{GetNextLabel(ref labelIdx)}}}: dup
                                        {{{GetNextLabel(ref labelIdx)}}}: ldc.i4.s {{{argumentsCount}}}
                                        {{{GetNextLabel(ref labelIdx)}}}: newarr [Inoculator.Interceptors]Inoculator.Builder.ParameterData
                                        {{{ExtractArguments(metadata, ref labelIdx, metadata.IsStatic)}}}

                                        {{{GetNextLabel(ref labelIdx)}}}: callvirt instance void [Inoculator.Interceptors]Inoculator.Builder.MethodData::set_Parameters(class [Inoculator.Interceptors]Inoculator.Builder.ParameterData[])
                                        {{{GetNextLabel(ref labelIdx)}}}: stfld class [Inoculator.Interceptors]Inoculator.Builder.MethodData {{{stateMachineFullName}}}::'<inoculated>__Metadata'
                                        {{{String.Join("\n", 
                                            interceptorsClasses.Select(
                                                    (attrClassName, i) => $"""
                                            {GetNextLabel(ref labelIdx)}: dup
                                            {GetNextLabel(ref labelIdx)}: newobj instance void class {attrClassName}::.ctor()
                                            {GetNextLabel(ref labelIdx)}: stfld class {attrClassName} {stateMachineFullName}::{GenerateInterceptorName(attrClassName)}
                                            """
                                        ))}}}
                                        {{{GetNextLabel(ref labelIdx)}}}: ret
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

    private static ClassDecl.Class InjectInoculationFields(Class classRef, Func<ClassDecl.MethodDefinition, ClassDecl.MethodDefinition[]> MoveNextHandler, string[] interceptorsClasses)
    {
        List<string> members = new();
        if(interceptorsClasses.Length > 0 ) {
            members.AddRange(interceptorsClasses.Select((attr, i) => $".field public class {attr} {GenerateInterceptorName(attr)}"));
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

    private static Func<ClassDecl.MethodDefinition, ClassDecl.MethodDefinition[]> GetMoveNextHandler(TypeData returnType, Class classRef, IEnumerable<string> path, bool isContainedInStruct, string[] interceptorsClasses) {
        return (MethodDefinition methodDef) => {
            int labelIdx = 0;
            Dictionary<string, string> jumptable = new();

            var stateMachineFullNameBuilder = new StringBuilder()
                .Append(isContainedInStruct ? " valuetype " : " class ")
                .Append($"{String.Join("/", path)}")
                .Append($"/{classRef.Header.Id}");
            if (classRef.Header.TypeParameters?.Parameters.Values.Length > 0)
            {
                stateMachineFullNameBuilder.Append("<")
                    .Append(String.Join(", ", classRef.Header.TypeParameters.Parameters.Values.Select(p => $"!{p}")))
                    .Append(">");
            }
            var stateMachineFullName = stateMachineFullNameBuilder.ToString();

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
                {{{GetNextLabel(ref labelIdx)}}}: ldarg.0
                {{{GetNextLabel(ref labelIdx)}}}: ldfld int32 {{{stateMachineFullName}}}::'<>1__state'
                {{{GetNextLabel(ref labelIdx)}}}: brtrue.s ***JUMPDEST1***


                {{{String.Join("\n", 
                    interceptorsClasses?.Select(
                        (attrClassName, i) => $@"
                        {GetNextLabel(ref labelIdx)}: ldarg.0
                        {GetNextLabel(ref labelIdx)}: ldfld class {attrClassName} {stateMachineFullName}::{GenerateInterceptorName(attrClassName)}
                        {GetNextLabel(ref labelIdx)}: ldarg.0
                        {GetNextLabel(ref labelIdx)}: ldfld class [Inoculator.Interceptors]Inoculator.Builder.MethodData {stateMachineFullName}::'<inoculated>__Metadata'
                        {GetNextLabel(ref labelIdx)}: callvirt instance void class {attrClassName}::OnEntry(class [Inoculator.Interceptors]Inoculator.Builder.MethodData)"
                ))}}}

                {{{GetNextLabel(ref labelIdx, jumptable, "JUMPDEST1")}}}: nop
                .try {
                    .try {
                        {{{InvokeFunction(stateMachineFullName, returnType, ref labelIdx)}}}
                        {{{String.Join("\n",
                            interceptorsClasses?.Select(
                                (attrClassName, i) => $@"
                                {GetNextLabel(ref labelIdx)}: ldarg.0
                                {GetNextLabel(ref labelIdx)}: ldfld class {attrClassName} {stateMachineFullName}::{GenerateInterceptorName(attrClassName)}
                                {GetNextLabel(ref labelIdx)}: ldarg.0
                                {GetNextLabel(ref labelIdx)}: ldfld class [Inoculator.Interceptors]Inoculator.Builder.MethodData {stateMachineFullName}::'<inoculated>__Metadata'
                                {GetNextLabel(ref labelIdx)}: callvirt instance void class {attrClassName}::OnSuccess(class [Inoculator.Interceptors]Inoculator.Builder.MethodData)"
                        ))}}}
                        {{{GetNextLabel(ref labelIdx)}}}: leave.s ***END***
                    } catch [System.Runtime]System.Exception
                    {
                        {{{GetNextLabel(ref labelIdx)}}}: stloc.s e
                        {{{GetNextLabel(ref labelIdx)}}}: ldarg.0
                        {{{GetNextLabel(ref labelIdx)}}}: ldfld class [Inoculator.Interceptors]Inoculator.Builder.MethodData {{{stateMachineFullName}}}::'<inoculated>__Metadata'
                        {{{GetNextLabel(ref labelIdx)}}}: ldloc.s e
                        {{{GetNextLabel(ref labelIdx)}}}: callvirt instance void [Inoculator.Interceptors]Inoculator.Builder.MethodData::set_Exception(class [System.Runtime]System.Exception)
                        {{{String.Join("\n",
                            interceptorsClasses?.Select(
                                (attrClassName, i) => $@"
                                {GetNextLabel(ref labelIdx)}: ldarg.0
                                {GetNextLabel(ref labelIdx)}: ldfld class {attrClassName} {stateMachineFullName}::{GenerateInterceptorName(attrClassName)}
                                {GetNextLabel(ref labelIdx)}: ldarg.0
                                {GetNextLabel(ref labelIdx)}: ldfld class [Inoculator.Interceptors]Inoculator.Builder.MethodData {stateMachineFullName}::'<inoculated>__Metadata'
                                {GetNextLabel(ref labelIdx)}: callvirt instance void class {attrClassName}::OnException(class [Inoculator.Interceptors]Inoculator.Builder.MethodData)"
                        ))}}}
                        {{{GetNextLabel(ref labelIdx)}}}: ldloc.s e
                        {{{GetNextLabel(ref labelIdx)}}}: throw
                    } 
                } finally
                {
                    {{{GetNextLabel(ref labelIdx)}}}: ldarg.0
                    {{{GetNextLabel(ref labelIdx)}}}: ldfld int32 {{{stateMachineFullName}}}::'<>1__state'
                    {{{GetNextLabel(ref labelIdx)}}}: ldc.i4.m1
                    {{{GetNextLabel(ref labelIdx)}}}: bne.un.s ***JUMPDEST2***

                    {{{String.Join("\n",
                        interceptorsClasses?.Select(
                            (attrClassName, i) => $@"
                            {GetNextLabel(ref labelIdx)}: ldarg.0
                            {GetNextLabel(ref labelIdx)}: ldfld class {attrClassName} {stateMachineFullName}::{GenerateInterceptorName(attrClassName)}
                            {GetNextLabel(ref labelIdx)}: ldarg.0
                            {GetNextLabel(ref labelIdx)}: ldfld class [Inoculator.Interceptors]Inoculator.Builder.MethodData {stateMachineFullName}::'<inoculated>__Metadata'
                            {GetNextLabel(ref labelIdx)}: callvirt instance void class {attrClassName}::OnExit(class [Inoculator.Interceptors]Inoculator.Builder.MethodData)"
                    ))}}}
                    {{{GetNextLabel(ref labelIdx, jumptable, "JUMPDEST2")}}}: endfinally
                }
                {{{GetNextLabel(ref labelIdx, jumptable, "END")}}}: nop
                {{{GetNextLabel(ref labelIdx)}}}: ldloc.0
                {{{GetNextLabel(ref labelIdx)}}}: ret
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

    private static string InvokeFunction(string stateMachineFullName, TypeData returnType, ref int labelIdx) {
        static string ToGenericArity1(TypeData type) => type.IsVoid ? String.Empty : type.IsGeneric ? $"`1<!{type.PureName}>" : $"`1<{type.Name}>";
        return $$$"""
            {{{GetNextLabel(ref labelIdx)}}}: ldarg.0
            {{{GetNextLabel(ref labelIdx)}}}: call instance bool {{{stateMachineFullName}}}::MoveNext__inoculated()
            {{{GetNextLabel(ref labelIdx)}}}: stloc.0
            {{{GetNextLabel(ref labelIdx)}}}: ldarg.0
            {{{GetNextLabel(ref labelIdx)}}}: ldfld class [Inoculator.Interceptors]Inoculator.Builder.MethodData {{{stateMachineFullName}}}::'<inoculated>__Metadata'
            
            {{{GetNextLabel(ref labelIdx)}}}: ldstr "{{{returnType.Name}}}"
            {{{GetNextLabel(ref labelIdx)}}}: ldarg.0
            {{{GetNextLabel(ref labelIdx)}}}: ldfld {{{(returnType.IsGeneric ? $"!{returnType.PureName}" : returnType.Name)}}} {{{stateMachineFullName}}}::'<>2__current'
            {{{(
                returnType.IsReferenceType ? String.Empty
                : $@"{GetNextLabel(ref labelIdx)}: box {(returnType.IsGeneric ? $"!{returnType.PureName}" : returnType.Name)}"
            )}}}
            {{{GetNextLabel(ref labelIdx)}}}: dup
            {{{GetNextLabel(ref labelIdx)}}}: callvirt instance class [System.Runtime]System.Type [System.Runtime]System.Object::GetType()
            {{{GetNextLabel(ref labelIdx)}}}: ldnull
            {{{GetNextLabel(ref labelIdx)}}}: newobj instance void class [Inoculator.Interceptors]Inoculator.Builder.ParameterData::.ctor(string,object,class [System.Runtime]System.Type,string)
            {{{GetNextLabel(ref labelIdx)}}}: callvirt instance void [Inoculator.Interceptors]Inoculator.Builder.MethodData::set_ReturnValue(class [Inoculator.Interceptors]Inoculator.Builder.ParameterData)
        """;
    }
}