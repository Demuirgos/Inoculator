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
    public class Config {
        public bool IsReleaseMode;
        public InterceptorData[] Interceptors;
        public string[] Path;  
        public MethodData Metadata;
        public ClassDecl.Class ClassRef;
        public TypeDecl.CustomTypeReference typeContainer => Metadata.Code.Header.Type.Components.Types.Values.First() as TypeDecl.CustomTypeReference;

    }
    public static Result<(ClassDecl.Class[], MethodDecl.Method[]), Exception> Rewrite(ClassDecl.Class classRef, MethodData metadata, InterceptorData[] interceptors, IEnumerable<string> path)
    {
        Config? config = new Config {
            IsReleaseMode = classRef.Header.Extends.Type.ToString() == "[System.Runtime] System.ValueType",
            Interceptors = interceptors,
            Path = path?.ToArray(),
            Metadata = metadata,
            ClassRef = classRef
        };

        var typeContainer = metadata.Code.Header.Type.Components.Types.Values.First() as TypeDecl.CustomTypeReference;
        var oldClassMangledName= $"'<>__{Math.Abs(classRef.Header.Id.GetHashCode())}_old'";
        File.WriteAllText("AsyncEnum.test", classRef.ToString()
            .Replace(classRef.Header.Id.ToString(), oldClassMangledName)
            .Replace(metadata.Code.Header.Name.ToString(), metadata.MangledName(false)));
        var oldClassRef = Parse<Class>(classRef.ToString()
            .Replace(classRef.Header.Id.ToString(), oldClassMangledName)
            .Replace(metadata.Code.Header.Name.ToString(), metadata.MangledName(false))
        );

        var oldMethodInstance = Parse<Method>(metadata.Code.ToString()
            .Replace(classRef.Header.Id.ToString(), oldClassMangledName)
            .Replace(metadata.Code.Header.Name.ToString(), metadata.MangledName(false))
        );

        var newClassRef = InjectFieldsInClass(classRef, config);
        var newMethodInstance = InjectInitialization(classRef, config);

        return Success<(ClassDecl.Class[], MethodDecl.Method[]), Exception>.From((new Class[] { oldClassRef, newClassRef }, new Method[] { oldMethodInstance, newMethodInstance }));
    }

    private static Method InjectInitialization(Class classRef, Config config)
    {
        int labelIdx = 0;
        bool isToBeRewritten = config.Interceptors.Any(i => i.IsRewriter);

        bool isStatic = config.Metadata.MethodCall is MethodData.CallType.Static;
        int argumentsCount = config.Metadata.IsStatic 
            ? config.Metadata.Code.Header.Parameters.Parameters.Values.Length 
            : config.Metadata.Code.Header.Parameters.Parameters.Values.Length + 1;

        var stateMachineFullName = StringifyPath(config.Metadata, classRef.Header, config.Path, false, 2);
        var inoculationSightName = StringifyPath(config.Metadata, classRef.Header, config.Path, false, 0);


        string CustomAttributes = String.Join("\n", config.Metadata.Code.Body.Items.Values.OfType<MethodDecl.CustomAttributeItem>()
            .Select(i => i.ToString()));


        string stackClause = ".maxstack 16";
        var LocalsClause = $@".locals init (
            {stateMachineFullName} V_0
            {
                config.Metadata.Code.Body.Items.Values.OfType<MethodDecl.LocalsItem>().FirstOrDefault()?.Signatures.Values.Values.Select(
                    i => $",{i.Type} {i.Id}"
                ).Aggregate((a, b) => $"{a}{b}")
            }
        )";

        var oldInstructions = config.Metadata.Code.Body.Items.Values.OfType<MethodDecl.InstructionItem>();
        var newInstructions = new List<MethodDecl.InstructionItem>();
        newInstructions.AddRange(oldInstructions.Take(2));


        string injectionCode = $$$"""
            stloc.s    V_0
            ldloc.s    V_0
            ldstr "{{{GetCleanedString(config.Metadata.Code.ToString())}}}"
            ldstr "{{{GetCleanedString(config.Metadata.ClassReference.ToString())}}}"
            ldstr "{{{String.Join("/", config.Path)}}}"
            newobj instance void [Inoculator.Interceptors]Inoculator.Builder.MethodData::.ctor(string, string, string)
            dup
            ldc.i4.s {{{argumentsCount}}}
            newarr [Inoculator.Interceptors]Inoculator.Builder.ParameterData
            {{{ExtractArguments(config.Metadata, config.Metadata.IsStatic)}}}

            callvirt instance void [Inoculator.Interceptors]Inoculator.Builder.MethodData::set_Parameters(class [Inoculator.Interceptors]Inoculator.Builder.ParameterData[])
            stfld class [Inoculator.Interceptors]Inoculator.Builder.MethodData {{{stateMachineFullName}}}::'<inoculated>__Metadata'
            {{{String.Join("\n",
                config.Interceptors.Select(
                    (attrClassName, i) => $@"
                        ldloc.s    V_0
                        {GetAttributeInstance(config.Metadata, inoculationSightName, attrClassName)}
                        stfld class {attrClassName.ClassName} {stateMachineFullName}::{GenerateInterceptorName(attrClassName.ClassName)}"
            ))}}}
            ldloc.s    V_0
            """;
        
        var injectionCodeCast = Parse<InstructionDecl.Instruction.Block>(injectionCode);
        newInstructions.AddRange(injectionCodeCast.Opcodes.Values.Select(opcode => new InstructionItem(opcode)));
        newInstructions.AddRange(oldInstructions.Skip(2));

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
        if(TryParse<MethodDecl.Member.Collection>(newBody, out MethodDecl.Member.Collection body, out string error)) {
            File.WriteAllText("AsyncEnum.error", newBody);
        }

        config.Metadata.Code = config.Metadata.Code with
        {
            Body = body
        };

        File.WriteAllText("AsyncEnum.test2", config.Metadata.Code.ToString());

        return config.Metadata.Code;
    }

    private static ClassDecl.Class InjectFieldsInClass(Class classRef, Config config)
    {
        bool isToBeRewritten = config.Interceptors.Any(interceptor => interceptor.IsRewriter);

        List<string> members = new();
        if(config.Interceptors.Length > 0 ) {
            members.AddRange(config.Interceptors.Select((attr, i) => $".field public class {attr.ClassName} {GenerateInterceptorName(attr.ClassName)}"));
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
                                    ClassDecl.MethodDefinition method => MoveNextHandler(method, config),
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

    private static MethodDefinition[] MoveNextHandler(MethodDefinition method, Config config) {
        if(method.Value.Header.Name.Name.EndsWith("MoveNextAsync'")) 
            return UpdateMoveNextAsync(method, config);
        if(method.Value.Header.Name.Name == "MoveNext") 
            return UpdateMoveNext(method, config);
        return new [] { method };
    }

    private static MethodDefinition[] UpdateMoveNextAsync(MethodDefinition methodDef, Config config)
    {
        // OnEntry // OnExit
        
        int labelIdx = 0;
        Dictionary<string, string> jumptable = new();
        bool isToBeRewritten = config.Interceptors.Any(m => m.IsRewriter);

        var stateMachineFullName = StringifyPath(config.Metadata, config.ClassRef.Header, config.Path, false, 1);

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

        
        builder.AppendLine($".maxstack 8");

        builder.AppendLine($@"
                .locals init (bool, class [System.Runtime]System.Exception e, valuetype [System.Runtime]System.Threading.Tasks.ValueTask`1<bool> vtask, class [System.Runtime]System.Threading.Tasks.Task`1<bool> ttask)"
            );
        // builder.Append($$$"""
        //     {{{CallMethodOnInterceptors(stateMachineFullName, config.Interceptors, "OnEntry", false, ref labelIdx)}}}
        //     {{{InvokeStepMove(config, stateMachineFullName, config.typeContainer, ref labelIdx, jumptable)}}}

        //     {{{GetNextLabel(ref labelIdx, jumptable, "SUCCESS")}}}: nop
        //     {{{CallMethodOnInterceptors(stateMachineFullName, config.Interceptors, "OnSuccess", false, ref labelIdx)}}}
        //     br.s ***EXIT***

        //     {{{GetNextLabel(ref labelIdx, jumptable, "FAILURE")}}}: nop
        //     {{{CallMethodOnInterceptors(stateMachineFullName, config.Interceptors, "OnException", false, ref labelIdx)}}}
        //     br.s ***EXIT***

        //     {{{GetNextLabel(ref labelIdx, jumptable, "EXIT")}}}: nop
        //     {{{CallMethodOnInterceptors(stateMachineFullName, config.Interceptors, "OnExit", false, ref labelIdx)}}}
        //     ret
        // }}
        // """);

        builder.Append($$$"""
        
            ldarg.0
            ldfld int32 {{{stateMachineFullName}}}::'<>1__state'
            ldc.i4.m1
            bne.un.s ***DEJA-START***

            {{{CallMethodOnInterceptors(stateMachineFullName, config.Interceptors, "OnEntry", false, ref labelIdx)}}}

            {{{InvokeStepMove(config, stateMachineFullName, config.typeContainer, ref labelIdx, jumptable)}}}

            {{{CallMethodOnInterceptors(stateMachineFullName, config.Interceptors, "OnExit", false, ref labelIdx)}}}
                
            {{{GetNextLabel(ref labelIdx, jumptable, "NOT-DONE")}}}: nop    
            newobj instance void valuetype [System.Runtime]System.Threading.Tasks.ValueTask`1<bool>::.ctor(!0)
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
                    Name = Parse<MethodName>("MoveNextAsync__inoculated")
                },

                Body = methodDef.Value.Body with
                {
                    Items = new ARRAY<MethodDecl.Member>(
                        methodDef.Value.Body
                            .Items.Values.Where(member => member is not MethodDecl.OverrideMethodItem)
                            .Select(member => {
                                var instructionLine = member.ToString().Replace("::" + config.Metadata.Name(false), "::" + config.Metadata.MangledName(false));
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

    private static MethodDefinition[] UpdateMoveNext(MethodDefinition methodDef, Config config)
    {
        // OnBegin
        // OnEnd
        // OnSuccess
        // OnException

        int labelIdx = 0;
        Dictionary<string, string> jumptable = new();
        bool isToBeRewritten = config.Interceptors.Any(m => m.IsRewriter);

        var stateMachineFullName = StringifyPath(config.Metadata, config.ClassRef.Header, config.Path, false, 1);

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

        
        builder.AppendLine($".maxstack 8");

        builder.AppendLine($".locals init (bool, class [System.Runtime]System.Exception e)");          
        builder.Append($$$"""
            {{{CallMethodOnInterceptors(stateMachineFullName, config.Interceptors, "OnBegin", false, ref labelIdx)}}}
            {{{InvokeFunction(config, stateMachineFullName, config.typeContainer, ref labelIdx, config.Interceptors.FirstOrDefault(m => m.IsRewriter)?.ClassName, isToBeRewritten, jumptable)}}}

            {{{GetNextLabel(ref labelIdx, jumptable, "SUCCESS")}}}: nop            
            {{{CallMethodOnInterceptors(stateMachineFullName, config.Interceptors, "OnSuccess", false, ref labelIdx)}}}
            br.s ***EXIT***

            {{{GetNextLabel(ref labelIdx, jumptable, "FAILURE")}}}: nop
            {{{CallMethodOnInterceptors(stateMachineFullName, config.Interceptors, "OnException", false, ref labelIdx)}}}
            br.s ***EXIT***

            {{{GetNextLabel(ref labelIdx, jumptable, "EXIT")}}}: nop
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
                                var instructionLine = member.ToString().Replace("::" + config.Metadata.Name(false), "::" + config.Metadata.MangledName(false));
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

    private static string InvokeStepMove(Config config, string stateMachineFullName, TypeDecl.CustomTypeReference typeContainer, ref int labelIdx, Dictionary<string, string> jumptable) {
        static string ToGenericArity1(TypeData type) => type.IsVoid ? String.Empty : type.IsGeneric ? $"`1<!{type.PureName}>" : $"`1<{type.Name}>";
        var returnType = new TypeData(typeContainer.Reference.GenericTypes?.Types.Values.FirstOrDefault()?.ToString() ?? "void");
        string TaskFieldType = $"valuetype [System.Runtime]System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore`1<bool>";

        return $$$"""
            {{{GetNextLabel(ref labelIdx, jumptable, "DEJA-START")}}}: nop
            ldarg.0
            call instance valuetype [System.Runtime]System.Threading.Tasks.ValueTask`1<bool> {{{stateMachineFullName}}}::MoveNextAsync__inoculated()
            stloc.s vtask

            ldloca.s vtask
            call instance class [System.Runtime]System.Threading.Tasks.Task`1<!0> valuetype [System.Runtime]System.Threading.Tasks.ValueTask`1<bool>::AsTask()
            stloc.s ttask

            ldloc.s ttask
            callvirt instance !0 class [System.Runtime]System.Threading.Tasks.Task`1<bool>::get_Result()
            dup
            brtrue.s ***NOT-DONE***
        """;
    }
    private static string InvokeFunction(Config config, string stateMachineFullName, TypeDecl.CustomTypeReference typeContainer, ref int labelIdx, string? rewriterClass, bool rewrite, Dictionary<string, string> jumptable) {
        static string ToGenericArity1(TypeData type) => type.IsVoid ? String.Empty : type.IsGeneric ? $"`1<!{type.PureName}>" : $"`1<{type.Name}>";
        var returnType = new TypeData(typeContainer.Reference.GenericTypes?.Types.Values.FirstOrDefault()?.ToString() ?? "void");
        string TaskFieldType = $"valuetype [System.Runtime]System.Threading.Tasks.Sources.ManualResetValueTaskSourceCore`1<bool>";

        if(!rewrite) {
            return $$$"""
                ldarg.0
                call instance void {{{stateMachineFullName}}}::MoveNext__inoculated()
                {{{CallMethodOnInterceptors(stateMachineFullName, config.Interceptors, "OnEnd", false, ref labelIdx)}}}

                ldarg.0
                ldflda {{{TaskFieldType}}} {{{stateMachineFullName}}}::'<>v__promiseOfValueOrEnd'
                dup
                call instance int16 {{{TaskFieldType}}}::get_Version()
                call instance valuetype [System.Runtime]System.Threading.Tasks.Sources.ValueTaskSourceStatus {{{TaskFieldType}}}::GetStatus(int16)
                
                ldc.i4.2
                bne.un.s ***NOTFAULTED***

                {{{GetNextLabel(ref labelIdx, jumptable, "FAULTED")}}}: nop
                .try 
                {
                    ldarg.0
                    ldflda {{{TaskFieldType}}} {{{stateMachineFullName}}}::'<>v__promiseOfValueOrEnd'
                    dup
                    call instance int16 {{{TaskFieldType}}}::get_Version()
                    call instance !0 {{{TaskFieldType}}}::GetResult(int16)
                    pop

                    leave.s ***OUTSIDE***
                } catch [mscorlib]System.Exception
                {
                    stloc.s e
                    ldarg.0
                    ldfld class [Inoculator.Interceptors]Inoculator.Builder.MethodData {{{stateMachineFullName}}}::'<inoculated>__Metadata'
                    ldloc.s e
                    callvirt instance void [Inoculator.Interceptors]Inoculator.Builder.MethodData::set_Exception(class [System.Runtime]System.Exception)

                    leave.s ***OUTSIDE***
                }
                {{{GetNextLabel(ref labelIdx, jumptable, "OUTSIDE")}}}: br.s ***FAILURE***

                {{{GetNextLabel(ref labelIdx, jumptable, "NOTFAULTED")}}}: nop
                ldarg.0
                ldflda {{{TaskFieldType}}} {{{stateMachineFullName}}}::'<>v__promiseOfValueOrEnd'
                dup
                call instance int16 {{{TaskFieldType}}}::get_Version()
                call instance valuetype [System.Runtime]System.Threading.Tasks.Sources.ValueTaskSourceStatus {{{TaskFieldType}}}::GetStatus(int16)
                ldc.i4.1
                bne.un.s ***UNKNOWN***

                {{{GetNextLabel(ref labelIdx, jumptable, "COMPLETED")}}}: nop
                ldarg.0
                ldfld class [Inoculator.Interceptors]Inoculator.Builder.MethodData {{{stateMachineFullName}}}::'<inoculated>__Metadata'
                ldarg.0
                ldfld {{{(returnType.IsGeneric ? $"!{returnType.PureName}" : returnType.Name)}}} {{{stateMachineFullName}}}::'<>2__current'
                {{{(
                    returnType.IsReferenceType ? string.Empty
                    : $"box {(returnType.IsGeneric ? $"!{returnType.PureName}" : returnType.Name)}"
                )}}}
                dup
                callvirt instance class [System.Runtime]System.Type [System.Runtime]System.Object::GetType()
                ldstr "current"
                newobj instance void class [Inoculator.Interceptors]Inoculator.Builder.ParameterData::.ctor(object,class [System.Runtime]System.Type,string)
                callvirt instance void [Inoculator.Interceptors]Inoculator.Builder.MethodData::set_ReturnValue(class [Inoculator.Interceptors]Inoculator.Builder.ParameterData)
                br.s ***SUCCESS***
        
                {{{GetNextLabel(ref labelIdx, jumptable, "UNKNOWN")}}}: nop
                br.s ***EXIT***
                
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
                    {{{CallMethodOnInterceptors(stateMachineFullName, config.Interceptors, "OnEnd", false, ref labelIdx)}}}

                    ldarg.0
                    ldfld class [Inoculator.Interceptors]Inoculator.Builder.MethodData {{{stateMachineFullName}}}::'<inoculated>__Metadata'
                    callvirt instance class [System.Runtime]System.Exception [Inoculator.Interceptors]Inoculator.Builder.MethodData::get_Exception()
                    dup
                    stloc.0
                    brtrue.s ***INNER_FAILURE***

                    ldarg.0
                    dup
                    ldfld class [Inoculator.Interceptors]Inoculator.Builder.MethodData {{{stateMachineFullName}}}::'<inoculated>__Metadata'
                    callvirt instance class [Inoculator.Interceptors]Inoculator.Builder.ParameterData [Inoculator.Interceptors]Inoculator.Builder.MethodData::get_ReturnValue()
                    callvirt instance object [Inoculator.Interceptors]Inoculator.Builder.ParameterData::get_Value()
                    {{{(returnType.IsReferenceType ? $"castclass {returnType.Name}" : $"unbox.any {returnType.ToProperName}")}}}
                    stfld {{{(returnType.IsGeneric ? $"!{returnType.PureName}" : returnType.Name)}}} {{{stateMachineFullName}}}::'<>2__current'

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
                    ldflda {{{TaskFieldType}}} {{{stateMachineFullName}}}::'<>v__promiseOfValueOrEnd'
                    ldloc.0
                    call instance void {{{TaskFieldType}}}::SetException(class [System.Runtime]System.Exception)
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
                br.s ***SUCCESS***
            """;
        }
    }
}