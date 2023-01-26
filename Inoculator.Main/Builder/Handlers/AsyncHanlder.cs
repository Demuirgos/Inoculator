using System.Extensions;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
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
        if(classRef.Header.Extends.Type.ToString() == "[System.Runtime] System.ValueType") {
            Console.WriteLine(classRef.Header);
            //return Success<(ClassDecl.Class, MethodDecl.Method[]), Exception>.From((classRef, new[] { metadata.Code }));
            return HandleRelease(classRef, metadata, attributeNames, path);
        }
        return HandleDebug(classRef, metadata, attributeNames, path);
    }

    private static Result<(ClassDecl.Class, MethodDecl.Method[]), Exception> HandleRelease(ClassDecl.Class classRef, MethodData metadata, string[] attributeNames, IEnumerable<string> path)
    {
        int labelIdx = 0;
        var stateMachineFullName = $"valuetype {String.Join("/",  path)}/{classRef.Header.Id}";
        Dictionary<string, string> jumptable = new();
        
        bool HasField(string fieldName) => classRef.Members.Members.Values.Any(x => x is ClassDecl.FieldDefinition field && field.Value.Id.ToString() == fieldName);
        
        var typeContainer = metadata.Code.Header.Type.Components.Types.Values.First() as TypeDecl.CustomTypeReference;
        var itemType = new TypeData(typeContainer.Reference.GenericTypes?.Types.Values.FirstOrDefault()?.ToString() ?? "void");
        
        ClassDecl.MethodDefinition[] HandleMoveNext(ClassDecl.MethodDefinition methodDef) {
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

            
            builder.AppendLine($$$"""
                .maxstack 8
                .locals init (class [System.Runtime]System.Exception e)
            """);            

            builder.Append($$$"""
                {{{GetNextLabel(ref labelIdx)}}}: ldarg.0
                {{{GetNextLabel(ref labelIdx)}}}: ldfld int32 {{{stateMachineFullName}}}::'<>1__state'
                {{{GetNextLabel(ref labelIdx)}}}: brfalse.s ***JUMPDEST1***


                {{{attributeNames.Select(
                    (attrClassName, i) => $@"
                    {GetNextLabel(ref labelIdx)}: ldarg.0
                    {GetNextLabel(ref labelIdx)}: ldfld class {attrClassName} {stateMachineFullName}::'<inoculated>__Interceptor{i}'
                    {GetNextLabel(ref labelIdx)}: ldarg.0
                    {GetNextLabel(ref labelIdx)}: ldfld class [Inoculator.Injector]Inoculator.Builder.MethodData {stateMachineFullName}::'<inoculated>__Metadata'
                    {GetNextLabel(ref labelIdx)}: callvirt instance void {attrClassName}::OnEntry(class [Inoculator.Injector]Inoculator.Builder.MethodData)"
                ).Aggregate((a, b) => $"{a}\n{b}")}}}

                {{{GetNextLabel(ref labelIdx, jumptable, "JUMPDEST1")}}}: nop

                {{{GetNextLabel(ref labelIdx)}}}: ldarg.0
                {{{GetNextLabel(ref labelIdx)}}}: call instance void {{{stateMachineFullName}}}::MoveNext__inoculated()

                {{{GetNextLabel(ref labelIdx)}}}: ldarg.0
                {{{GetNextLabel(ref labelIdx)}}}: ldfld int32 {{{stateMachineFullName}}}::'<>1__state'
                {{{GetNextLabel(ref labelIdx)}}}: ldc.i4.s -2
                {{{GetNextLabel(ref labelIdx)}}}: bne.un.s ***JUMPDEST1.5***

                {{{GetNextLabel(ref labelIdx)}}}: ldarg.0
                {{{GetNextLabel(ref labelIdx)}}}: ldfld class [Inoculator.Injector]Inoculator.Builder.MethodData {{{stateMachineFullName}}}::'<inoculated>__Metadata'
                        
                {{{(
                    itemType.IsVoid 
                    ? $@"
                        {GetNextLabel(ref labelIdx)}: ldnull
                        {GetNextLabel(ref labelIdx)}: callvirt instance void [Inoculator.Injector]Inoculator.Builder.MethodData::set_ReturnValue(object)"
                    : $@"
                        {GetNextLabel(ref labelIdx)}: ldarg.0
                        {GetNextLabel(ref labelIdx)}: ldflda valuetype [System.Runtime]System.Runtime.CompilerServices.AsyncTaskMethodBuilder{itemType.ToGenericArity1} {stateMachineFullName}::'<>t__builder'
                        {GetNextLabel(ref labelIdx)}: call instance class [System.Runtime]System.Threading.Tasks.Task`1<!0> valuetype [System.Runtime]System.Runtime.CompilerServices.AsyncTaskMethodBuilder{itemType.ToGenericArity1}::get_Task()
                        {GetNextLabel(ref labelIdx)}: callvirt instance !0 class [System.Runtime]System.Threading.Tasks.Task{itemType.ToGenericArity1}::get_Result()
                        {(
                            itemType.IsReferenceType ? string.Empty
                            : $"{GetNextLabel(ref labelIdx)}: box {itemType.Name}"
                        )}
                        {GetNextLabel(ref labelIdx)}: callvirt instance void [Inoculator.Injector]Inoculator.Builder.MethodData::set_ReturnValue(object)"
                )}}}

                {{{GetNextLabel(ref labelIdx)}}}: ldarg.0
                {{{GetNextLabel(ref labelIdx)}}}: ldfld class [Inoculator.Injector]Inoculator.Builder.MethodData {{{stateMachineFullName}}}::'<inoculated>__Metadata'
                {{{GetNextLabel(ref labelIdx)}}}: ldarg.0
                {{{GetNextLabel(ref labelIdx)}}}: ldflda valuetype [System.Runtime]System.Runtime.CompilerServices.AsyncTaskMethodBuilder{{{itemType.ToGenericArity1}}} {{{stateMachineFullName}}}::'<>t__builder'
                {{{GetNextLabel(ref labelIdx)}}}: call instance class [System.Runtime]System.Threading.Tasks.Task{{{(String.IsNullOrEmpty(itemType.ToGenericArity1) ? string.Empty : "`1<!0> valuetype")}}} [System.Runtime]System.Runtime.CompilerServices.AsyncTaskMethodBuilder{{{itemType.ToGenericArity1}}}::get_Task()
                {{{GetNextLabel(ref labelIdx)}}}: call instance class [System.Runtime]System.AggregateException [System.Runtime]System.Threading.Tasks.Task::get_Exception()
                {{{GetNextLabel(ref labelIdx)}}}: dup
                {{{GetNextLabel(ref labelIdx)}}}: stloc.0
                {{{GetNextLabel(ref labelIdx)}}}: callvirt instance void [Inoculator.Injector]Inoculator.Builder.MethodData::set_Exception(class [System.Runtime]System.Exception)

                {{{GetNextLabel(ref labelIdx, jumptable, "JUMPDEST1.5")}}}: ldloc.0
                {{{GetNextLabel(ref labelIdx)}}}: brfalse.s ***SUCCESS***

                {{{attributeNames.Select(
                    (attrClassName, i) => $@"
                    {GetNextLabel(ref labelIdx)}: ldarg.0
                    {GetNextLabel(ref labelIdx)}: ldfld class {attrClassName} {stateMachineFullName}::'<inoculated>__Interceptor{i}'
                    {GetNextLabel(ref labelIdx)}: ldarg.0
                    {GetNextLabel(ref labelIdx)}: ldfld class [Inoculator.Injector]Inoculator.Builder.MethodData {stateMachineFullName}::'<inoculated>__Metadata'
                    {GetNextLabel(ref labelIdx)}: callvirt instance void {attrClassName}::OnException(class [Inoculator.Injector]Inoculator.Builder.MethodData)"
                ).Aggregate((a, b) => $"{a}\n{b}")}}}
                {{{GetNextLabel(ref labelIdx)}}}: br.s ***JUMPDEST2***

                {{{GetNextLabel(ref labelIdx, jumptable, "SUCCESS")}}}: nop
                {{{attributeNames.Select(
                    (attrClassName, i) => $@"
                    {GetNextLabel(ref labelIdx)}: ldarg.0
                    {GetNextLabel(ref labelIdx)}: ldfld class {attrClassName} {stateMachineFullName}::'<inoculated>__Interceptor{i}'
                    {GetNextLabel(ref labelIdx)}: ldarg.0
                    {GetNextLabel(ref labelIdx)}: ldfld class [Inoculator.Injector]Inoculator.Builder.MethodData {stateMachineFullName}::'<inoculated>__Metadata'
                    {GetNextLabel(ref labelIdx)}: callvirt instance void {attrClassName}::OnExit(class [Inoculator.Injector]Inoculator.Builder.MethodData)"
                ).Aggregate((a, b) => $"{a}\n{b}")}}}

                {{{GetNextLabel(ref labelIdx, jumptable, "JUMPDEST2")}}}: nop
                {{{GetNextLabel(ref labelIdx)}}}: ret
            }}
            """);

            foreach(var (label, idx) in jumptable)
            {
                builder.Replace($"***{label}***", idx.ToString());
            }
            var newFunction = new ClassDecl.MethodDefinition(Parse<MethodDecl.Method>(builder.ToString()));
            
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
        }

        classRef = classRef with {
            Members = classRef.Members with {
                Members = new ARRAY<ClassDecl.Member>(
                    classRef.Members.Members.Values
                        .SelectMany(member => member switch {
                            ClassDecl.MethodDefinition method => HandleMoveNext(method),
                            _ => new [] { member }
                        }).Union(
                        attributeNames
                            .Select((attr, i) => $".field public class {attr} '<inoculated>__Interceptor{i}'")
                            .Append($".field public class [Inoculator.Injector]Inoculator.Builder.MethodData '<inoculated>__Metadata'")
                            .Select(Parse<ClassDecl.Member>)
                        ).ToArray()
                ) {
                    Options = new ARRAY<ClassDecl.Member>.ArrayOptions() {
                        Delimiters = ('\0', '\n', '\0')
                    }
                }
            }
        };

        labelIdx = 0;
        bool isStatic = metadata.MethodCall is MethodData.CallType.Static;

        string newBody = $$$"""
            .maxstack 8
            .locals init ({{{stateMachineFullName}}} V_0)
            {{{GetNextLabel(ref labelIdx)}}}:  ldloca.s   V_0
            {{{GetNextLabel(ref labelIdx)}}}:  call       valuetype [System.Runtime]System.Runtime.CompilerServices.AsyncTaskMethodBuilder{{{(String.IsNullOrEmpty(itemType.ToGenericArity1) ? string.Empty : "`1<!0> valuetype")}}} [System.Runtime]System.Runtime.CompilerServices.AsyncTaskMethodBuilder{{{itemType.ToGenericArity1}}}::Create()
            {{{GetNextLabel(ref labelIdx)}}}:  stfld      valuetype [System.Runtime]System.Runtime.CompilerServices.AsyncTaskMethodBuilder{{{itemType.ToGenericArity1}}} {{{stateMachineFullName}}}::'<>t__builder'
            {{{GetNextLabel(ref labelIdx)}}}:  ldloca.s   V_0
            {{{GetNextLabel(ref labelIdx)}}}:  ldc.i4.m1
            {{{GetNextLabel(ref labelIdx)}}}:  stfld      int32 {{{stateMachineFullName}}}::'<>1__state'
            
            {{{(
                isStatic || !HasField("'<>4__this'") ? String.Empty : $@"
                    {GetNextLabel(ref labelIdx)}:  ldloca.s V_0
                    {GetNextLabel(ref labelIdx)}:  ldarg.0
                    {GetNextLabel(ref labelIdx)}:  stfld class {String.Join("/", path)} {stateMachineFullName}::'<>4__this'
                "
            )}}}

            {{{(
                String.Join("\n", metadata.Code.Header.Parameters.Parameters.Values.Select(
                    (param, i) => !HasField(param.AsDefaultParameter()?.Id.ToString()) ? String.Empty : $@"
                        {GetNextLabel(ref labelIdx)}: ldloca.s V_0
                        {GetNextLabel(ref labelIdx)}: ldarg.s {param.AsDefaultParameter()?.Id}
                        {GetNextLabel(ref labelIdx)}: stfld {param.AsDefaultParameter().TypeDeclaration} {stateMachineFullName}::{param.AsDefaultParameter()?.Id}
                    "))
            )}}}
            
            {{{GetNextLabel(ref labelIdx)}}}: ldloca.s V_0
            {{{GetNextLabel(ref labelIdx)}}}: ldstr "{{{new string(metadata.Code.ToString().ToCharArray().Select(c => c != '\n' ? c : ' ').ToArray())}}}"
            {{{GetNextLabel(ref labelIdx)}}}: newobj instance void [Inoculator.Injector]Inoculator.Builder.MethodData::.ctor(string)

            {{{GetNextLabel(ref labelIdx)}}}: dup
            {{{GetNextLabel(ref labelIdx)}}}: ldc.i4.s {{{metadata.Code.Header.Parameters.Parameters.Values.Length}}}
            {{{GetNextLabel(ref labelIdx)}}}: newarr [System.Runtime]System.Object
            {{{ExtractArguments(metadata.Code.Header.Parameters, ref labelIdx, 0)}}}

            {{{GetNextLabel(ref labelIdx)}}}: callvirt instance void [Inoculator.Injector]Inoculator.Builder.MethodData::set_Parameters(object[])
            {{{GetNextLabel(ref labelIdx)}}}: stfld class [Inoculator.Injector]Inoculator.Builder.MethodData {{{stateMachineFullName}}}::'<inoculated>__Metadata'
            {{{
                attributeNames.Select(
                    (attrClassName, i) => $@"
                        {GetNextLabel(ref labelIdx)}: ldloca.s V_0
                        {GetNextLabel(ref labelIdx)}: newobj instance void {attrClassName}::.ctor()
                        {GetNextLabel(ref labelIdx)}: stfld class {attrClassName} {stateMachineFullName}::'<inoculated>__Interceptor{i}'
            ").Aggregate((a, b) => $"{a}\n{b}")}}}

            {{{GetNextLabel(ref labelIdx)}}}:  ldloca.s   V_0
            {{{GetNextLabel(ref labelIdx)}}}:  ldflda     valuetype [System.Runtime]System.Runtime.CompilerServices.AsyncTaskMethodBuilder{{{itemType.ToGenericArity1}}} {{{stateMachineFullName}}}::'<>t__builder'
            {{{GetNextLabel(ref labelIdx)}}}:  ldloca.s   V_0
            {{{GetNextLabel(ref labelIdx)}}}:  call       instance void {{{(itemType.ToGenericArity1 == string.Empty ? string.Empty : "valuetype")}}} [System.Runtime]System.Runtime.CompilerServices.AsyncTaskMethodBuilder{{{itemType.ToGenericArity1}}}::Start<{{{stateMachineFullName}}}>(!!0&)
            {{{GetNextLabel(ref labelIdx)}}}:  ldloca.s   V_0
            {{{GetNextLabel(ref labelIdx)}}}:  ldflda     valuetype [System.Runtime]System.Runtime.CompilerServices.AsyncTaskMethodBuilder{{{itemType.ToGenericArity1}}} {{{stateMachineFullName}}}::'<>t__builder'
            {{{GetNextLabel(ref labelIdx)}}}:  call       instance class [System.Runtime]System.Threading.Tasks.Task{{{(String.IsNullOrEmpty(itemType.ToGenericArity1) ? string.Empty : "`1<!0> valuetype")}}} [System.Runtime]System.Runtime.CompilerServices.AsyncTaskMethodBuilder{{{itemType.ToGenericArity1}}}::get_Task()
            {{{GetNextLabel(ref labelIdx)}}}:  ret
            """;
        _ = TryParse<MethodDecl.Member.Collection>(newBody, out MethodDecl.Member.Collection body, out string err);

        metadata.Code = metadata.Code with {
            Body = body
        };

        return Success<(ClassDecl.Class, MethodDecl.Method[]), Exception>.From((classRef, new[] { metadata.Code }));
    }

    private static Result<(ClassDecl.Class, MethodDecl.Method[]), Exception> HandleDebug(ClassDecl.Class classRef, MethodData metadata, string[] attributeNames, IEnumerable<string> path)
    {
        int labelIdx = 0;
        var stateMachineFullName = $"class {String.Join("/",  path)}/{classRef.Header.Id}";
        Dictionary<string, string> jumptable = new();
        
        bool HasField(string fieldName) => classRef.Members.Members.Values.Any(x => x is ClassDecl.FieldDefinition field && field.Value.Id.ToString() == fieldName);
        bool isInReleaseMode = classRef.Header.Extends.Type.ToString() == "[System.Runtime] System.ValueType";
        
        var typeContainer = metadata.Code.Header.Type.Components.Types.Values.First() as TypeDecl.CustomTypeReference;
        var itemType = new TypeData(typeContainer.Reference.GenericTypes?.Types.Values.FirstOrDefault()?.ToString() ?? "void");
        
        ClassDecl.MethodDefinition[] HandleMoveNext(ClassDecl.MethodDefinition methodDef) {
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

            
            builder.AppendLine($$$"""
                .maxstack 8
                .locals init (class [System.Runtime]System.Exception e)
            """);            

            builder.Append($$$"""
                {{{GetNextLabel(ref labelIdx)}}}: ldarg.0
                {{{GetNextLabel(ref labelIdx)}}}: ldfld int32 {{{stateMachineFullName}}}::'<>1__state'
                {{{GetNextLabel(ref labelIdx)}}}: brfalse.s ***JUMPDEST1***


                {{{attributeNames.Select(
                    (attrClassName, i) => $@"
                    {GetNextLabel(ref labelIdx)}: ldarg.0
                    {GetNextLabel(ref labelIdx)}: ldfld class {attrClassName} {stateMachineFullName}::'<inoculated>__Interceptor{i}'
                    {GetNextLabel(ref labelIdx)}: ldarg.0
                    {GetNextLabel(ref labelIdx)}: ldfld class [Inoculator.Injector]Inoculator.Builder.MethodData {stateMachineFullName}::'<inoculated>__Metadata'
                    {GetNextLabel(ref labelIdx)}: callvirt instance void {attrClassName}::OnEntry(class [Inoculator.Injector]Inoculator.Builder.MethodData)"
                ).Aggregate((a, b) => $"{a}\n{b}")}}}

                {{{GetNextLabel(ref labelIdx, jumptable, "JUMPDEST1")}}}: nop

                {{{GetNextLabel(ref labelIdx)}}}: ldarg.0
                {{{GetNextLabel(ref labelIdx)}}}: call instance void {{{stateMachineFullName}}}::MoveNext__inoculated()

                {{{GetNextLabel(ref labelIdx)}}}: ldarg.0
                {{{GetNextLabel(ref labelIdx)}}}: ldfld int32 {{{stateMachineFullName}}}::'<>1__state'
                {{{GetNextLabel(ref labelIdx)}}}: ldc.i4.s -2
                {{{GetNextLabel(ref labelIdx)}}}: bne.un.s ***JUMPDEST1.5***

                {{{GetNextLabel(ref labelIdx)}}}: ldarg.0
                {{{GetNextLabel(ref labelIdx)}}}: ldfld class [Inoculator.Injector]Inoculator.Builder.MethodData {{{stateMachineFullName}}}::'<inoculated>__Metadata'
                        
                {{{(
                    itemType.IsVoid 
                    ? $@"
                        {GetNextLabel(ref labelIdx)}: ldnull
                        {GetNextLabel(ref labelIdx)}: callvirt instance void [Inoculator.Injector]Inoculator.Builder.MethodData::set_ReturnValue(object)"
                    : $@"
                        {GetNextLabel(ref labelIdx)}: ldarg.0
                        {GetNextLabel(ref labelIdx)}: ldflda valuetype [System.Runtime]System.Runtime.CompilerServices.AsyncTaskMethodBuilder{itemType.ToGenericArity1} {stateMachineFullName}::'<>t__builder'
                        {GetNextLabel(ref labelIdx)}: call instance class [System.Runtime]System.Threading.Tasks.Task`1<!0> valuetype [System.Runtime]System.Runtime.CompilerServices.AsyncTaskMethodBuilder{itemType.ToGenericArity1}::get_Task()
                        {GetNextLabel(ref labelIdx)}: callvirt instance !0 class [System.Runtime]System.Threading.Tasks.Task{itemType.ToGenericArity1}::get_Result()
                        {(
                            itemType.IsReferenceType ? string.Empty
                            : $"{GetNextLabel(ref labelIdx)}: box {itemType.Name}"
                        )}
                        {GetNextLabel(ref labelIdx)}: callvirt instance void [Inoculator.Injector]Inoculator.Builder.MethodData::set_ReturnValue(object)"
                )}}}

                {{{GetNextLabel(ref labelIdx)}}}: ldarg.0
                {{{GetNextLabel(ref labelIdx)}}}: ldfld class [Inoculator.Injector]Inoculator.Builder.MethodData {{{stateMachineFullName}}}::'<inoculated>__Metadata'
                {{{GetNextLabel(ref labelIdx)}}}: ldarg.0
                {{{GetNextLabel(ref labelIdx)}}}: ldflda valuetype [System.Runtime]System.Runtime.CompilerServices.AsyncTaskMethodBuilder{{{itemType.ToGenericArity1}}} {{{stateMachineFullName}}}::'<>t__builder'
                {{{GetNextLabel(ref labelIdx)}}}: call instance class [System.Runtime]System.Threading.Tasks.Task{{{(String.IsNullOrEmpty(itemType.ToGenericArity1) ? string.Empty : "`1<!0> valuetype")}}} [System.Runtime]System.Runtime.CompilerServices.AsyncTaskMethodBuilder{{{itemType.ToGenericArity1}}}::get_Task()
                {{{GetNextLabel(ref labelIdx)}}}: call instance class [System.Runtime]System.AggregateException [System.Runtime]System.Threading.Tasks.Task::get_Exception()
                {{{GetNextLabel(ref labelIdx)}}}: dup
                {{{GetNextLabel(ref labelIdx)}}}: stloc.0
                {{{GetNextLabel(ref labelIdx)}}}: callvirt instance void [Inoculator.Injector]Inoculator.Builder.MethodData::set_Exception(class [System.Runtime]System.Exception)

                {{{GetNextLabel(ref labelIdx, jumptable, "JUMPDEST1.5")}}}: ldloc.0
                {{{GetNextLabel(ref labelIdx)}}}: brfalse.s ***SUCCESS***

                {{{attributeNames.Select(
                    (attrClassName, i) => $@"
                    {GetNextLabel(ref labelIdx)}: ldarg.0
                    {GetNextLabel(ref labelIdx)}: ldfld class {attrClassName} {stateMachineFullName}::'<inoculated>__Interceptor{i}'
                    {GetNextLabel(ref labelIdx)}: ldarg.0
                    {GetNextLabel(ref labelIdx)}: ldfld class [Inoculator.Injector]Inoculator.Builder.MethodData {stateMachineFullName}::'<inoculated>__Metadata'
                    {GetNextLabel(ref labelIdx)}: callvirt instance void {attrClassName}::OnException(class [Inoculator.Injector]Inoculator.Builder.MethodData)"
                ).Aggregate((a, b) => $"{a}\n{b}")}}}
                {{{GetNextLabel(ref labelIdx)}}}: br.s ***JUMPDEST2***

                {{{GetNextLabel(ref labelIdx, jumptable, "SUCCESS")}}}: nop
                {{{attributeNames.Select(
                    (attrClassName, i) => $@"
                    {GetNextLabel(ref labelIdx)}: ldarg.0
                    {GetNextLabel(ref labelIdx)}: ldfld class {attrClassName} {stateMachineFullName}::'<inoculated>__Interceptor{i}'
                    {GetNextLabel(ref labelIdx)}: ldarg.0
                    {GetNextLabel(ref labelIdx)}: ldfld class [Inoculator.Injector]Inoculator.Builder.MethodData {stateMachineFullName}::'<inoculated>__Metadata'
                    {GetNextLabel(ref labelIdx)}: callvirt instance void {attrClassName}::OnExit(class [Inoculator.Injector]Inoculator.Builder.MethodData)"
                ).Aggregate((a, b) => $"{a}\n{b}")}}}

                {{{GetNextLabel(ref labelIdx, jumptable, "JUMPDEST2")}}}: nop
                {{{GetNextLabel(ref labelIdx)}}}: ret
            }}
            """);

            foreach(var (label, idx) in jumptable)
            {
                builder.Replace($"***{label}***", idx.ToString());
            }
            var newFunction = new ClassDecl.MethodDefinition(Parse<MethodDecl.Method>(builder.ToString()));
            
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
        }

        classRef = classRef with {
            Members = classRef.Members with {
                Members = new ARRAY<ClassDecl.Member>(
                    classRef.Members.Members.Values
                        .SelectMany(member => member switch {
                            ClassDecl.MethodDefinition method => HandleMoveNext(method),
                            _ => new [] { member }
                        }).Union(
                        attributeNames
                            .Select((attr, i) => $".field public class {attr} '<inoculated>__Interceptor{i}'")
                            .Append($".field public class [Inoculator.Injector]Inoculator.Builder.MethodData '<inoculated>__Metadata'")
                            .Select(Parse<ClassDecl.Member>)
                        ).ToArray()
                ) {
                    Options = new ARRAY<ClassDecl.Member>.ArrayOptions() {
                        Delimiters = ('\0', '\n', '\0')
                    }
                }
            }
        };

        labelIdx = 0;
        bool isStatic = metadata.MethodCall is MethodData.CallType.Static;

        string newBody = $$$"""
            .maxstack 8
            .locals init ({{{stateMachineFullName}}} V_0)
            {{{GetNextLabel(ref labelIdx)}}}:  newobj     instance void {{{stateMachineFullName}}}::.ctor()
            {{{GetNextLabel(ref labelIdx)}}}:  stloc.0
            {{{GetNextLabel(ref labelIdx)}}}:  ldloc.0
            {{{GetNextLabel(ref labelIdx)}}}:  dup
            {{{GetNextLabel(ref labelIdx)}}}:  call       valuetype [System.Runtime]System.Runtime.CompilerServices.AsyncTaskMethodBuilder{{{(String.IsNullOrEmpty(itemType.ToGenericArity1) ? string.Empty : "`1<!0> valuetype")}}} [System.Runtime]System.Runtime.CompilerServices.AsyncTaskMethodBuilder{{{itemType.ToGenericArity1}}}::Create()
            {{{GetNextLabel(ref labelIdx)}}}:  stfld      valuetype [System.Runtime]System.Runtime.CompilerServices.AsyncTaskMethodBuilder{{{itemType.ToGenericArity1}}} {{{stateMachineFullName}}}::'<>t__builder'
            {{{GetNextLabel(ref labelIdx)}}}:  ldc.i4.m1
            {{{GetNextLabel(ref labelIdx)}}}:  stfld      int32 {{{stateMachineFullName}}}::'<>1__state'
            
            {{{(
                isStatic || !HasField("'<>4__this'") ? String.Empty : $@"
                    {GetNextLabel(ref labelIdx)}:  ldloc.s 0
                    {GetNextLabel(ref labelIdx)}:  ldarg.0
                    {GetNextLabel(ref labelIdx)}:  stfld class {String.Join("/", path)} {stateMachineFullName}::'<>4__this'
                "
            )}}}

            {{{(
                String.Join("\n", metadata.Code.Header.Parameters.Parameters.Values.Select(
                    (param, i) => !HasField(param.AsDefaultParameter()?.Id.ToString()) ? String.Empty : $@"
                        {GetNextLabel(ref labelIdx)}: ldloc.s 0
                        {GetNextLabel(ref labelIdx)}: ldarg.s {param.AsDefaultParameter()?.Id}
                        {GetNextLabel(ref labelIdx)}: stfld {param.AsDefaultParameter().TypeDeclaration} {stateMachineFullName}::{param.AsDefaultParameter()?.Id}
                    "))
            )}}}
            
            {{{GetNextLabel(ref labelIdx)}}}: ldloc.0
            {{{GetNextLabel(ref labelIdx)}}}: ldstr "{{{new string(metadata.Code.ToString().ToCharArray().Select(c => c != '\n' ? c : ' ').ToArray())}}}"
            {{{GetNextLabel(ref labelIdx)}}}: newobj instance void [Inoculator.Injector]Inoculator.Builder.MethodData::.ctor(string)

            {{{GetNextLabel(ref labelIdx)}}}: dup
            {{{GetNextLabel(ref labelIdx)}}}: ldc.i4.s {{{metadata.Code.Header.Parameters.Parameters.Values.Length}}}
            {{{GetNextLabel(ref labelIdx)}}}: newarr [System.Runtime]System.Object
            {{{ExtractArguments(metadata.Code.Header.Parameters, ref labelIdx, 0)}}}

            {{{GetNextLabel(ref labelIdx)}}}: callvirt instance void [Inoculator.Injector]Inoculator.Builder.MethodData::set_Parameters(object[])
            {{{GetNextLabel(ref labelIdx)}}}: stfld class [Inoculator.Injector]Inoculator.Builder.MethodData {{{stateMachineFullName}}}::'<inoculated>__Metadata'
            {{{
                attributeNames.Select(
                    (attrClassName, i) => $@"
                        {GetNextLabel(ref labelIdx)}: ldloc.0
                        {GetNextLabel(ref labelIdx)}: newobj instance void {attrClassName}::.ctor()
                        {GetNextLabel(ref labelIdx)}: stfld class {attrClassName} {stateMachineFullName}::'<inoculated>__Interceptor{i}'
            ").Aggregate((a, b) => $"{a}\n{b}")}}}

            {{{GetNextLabel(ref labelIdx)}}}:  ldloc.0
            {{{GetNextLabel(ref labelIdx)}}}:  ldflda     valuetype [System.Runtime]System.Runtime.CompilerServices.AsyncTaskMethodBuilder{{{itemType.ToGenericArity1}}} {{{stateMachineFullName}}}::'<>t__builder'
            {{{GetNextLabel(ref labelIdx)}}}:  ldloca.s   V_0
            {{{GetNextLabel(ref labelIdx)}}}:  call       instance void {{{(itemType.ToGenericArity1 == string.Empty ? string.Empty : "valuetype")}}} [System.Runtime]System.Runtime.CompilerServices.AsyncTaskMethodBuilder{{{itemType.ToGenericArity1}}}::Start<{{{stateMachineFullName}}}>(!!0&)
            {{{GetNextLabel(ref labelIdx)}}}:  ldloc.0
            {{{GetNextLabel(ref labelIdx)}}}:  ldflda     valuetype [System.Runtime]System.Runtime.CompilerServices.AsyncTaskMethodBuilder{{{itemType.ToGenericArity1}}} {{{stateMachineFullName}}}::'<>t__builder'
            {{{GetNextLabel(ref labelIdx)}}}:  call       instance class [System.Runtime]System.Threading.Tasks.Task{{{(String.IsNullOrEmpty(itemType.ToGenericArity1) ? string.Empty : "`1<!0> valuetype")}}} [System.Runtime]System.Runtime.CompilerServices.AsyncTaskMethodBuilder{{{itemType.ToGenericArity1}}}::get_Task()
            {{{GetNextLabel(ref labelIdx)}}}:  ret
            """;
        _ = TryParse<MethodDecl.Member.Collection>(newBody, out MethodDecl.Member.Collection body, out string err);

        metadata.Code = metadata.Code with {
            Body = body
        };

        return Success<(ClassDecl.Class, MethodDecl.Method[]), Exception>.From((classRef, new[] { metadata.Code }));
    }
}