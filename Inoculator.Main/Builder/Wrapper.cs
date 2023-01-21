using System.Extensions;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using IdentifierDecl;
using Inoculator.Core;
using LabelDecl;
using MethodDecl;
using static Dove.Core.Parser;
namespace Inoculator.Builder;

public static class Wrapper {
    static string GetNextLabel(ref int labelIdx, Dictionary<string, string> marks = null, string mark = null) {
        string label = $"IL_{labelIdx++:X4}";
        if (marks is not null && mark is not null) marks.Add(mark, label);
        return label;
    }
    public static Result<(ClassDecl.Class, MethodDecl.Method[]), Exception> ReplaceNameWith(Metadata metadata, String name, string[] attributeName, ClassDecl.Class classRef = null, IEnumerable<string> path = null) {
        switch (metadata.MethodBehaviour)
        {
            case Metadata.MethodType.Sync:
                return RunNMMethodManipulation(metadata, name, attributeName);
            case Metadata.MethodType.Iter:
                return RunSMMethodManipulation(classRef, metadata, name, attributeName, path);
        }
        return Success<(ClassDecl.Class, MethodDecl.Method[]), Exception>.From((classRef, new[] { metadata.Code }));
    }

    private static Result<(ClassDecl.Class, MethodDecl.Method[]), Exception> RunSMMethodManipulation(ClassDecl.Class classRef, Metadata metadata, string name, string[] attributeNames, IEnumerable<string> path)
    {
        int labelIdx = 0;
        var stateMachineFullName = $"{String.Join("/",  path)}/{classRef.Header.Id}";
        Console.WriteLine(stateMachineFullName);
        Dictionary<string, string> marks = new();
        ClassDecl.MethodDefinition[] HandleMoveNext(ClassDecl.MethodDefinition methodDef) {
            if(methodDef.Value.Header.Name.ToString() != "MoveNext") return new [] { methodDef };
            _ = !ReturnTypeOf(metadata.Code.Header, out var type);
            bool isPrimitive = _primitives.Contains(type);
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
                .maxstack 4
                .locals init ( bool)
            """);            

            builder.Append($$$"""
                {{{GetNextLabel(ref labelIdx)}}}: ldarg.0
                {{{GetNextLabel(ref labelIdx)}}}: ldfld int32 {{{stateMachineFullName}}}::'<>1__state'
                {{{GetNextLabel(ref labelIdx)}}}: brtrue.s ***JUMPDEST1***


                {{{attributeNames.Select(
                    (attrClassName, i) => $@"
                    {GetNextLabel(ref labelIdx)}: ldarg.0
                    {GetNextLabel(ref labelIdx)}: ldfld class [Inoculator.Injector]Inoculator.Builder.Metadata {stateMachineFullName}::'<inoculated>__Metadata'
                    {GetNextLabel(ref labelIdx)}: ldarg.0
                    {GetNextLabel(ref labelIdx)}: ldfld class {attrClassName} {stateMachineFullName}::'<inoculated>__Interceptor{i}'
                    {GetNextLabel(ref labelIdx)}: callvirt instance void {attrClassName}::OnEntry(class [Inoculator.Injector]Inoculator.Builder.Metadata)"
                ).Aggregate((a, b) => $"{a}\n{b}")}}}

                {{{GetNextLabel(ref labelIdx, marks, "JUMPDEST1")}}}: nop
                .try {
                    .try {
                        {{{GetNextLabel(ref labelIdx)}}}: ldarg.0
                        {{{GetNextLabel(ref labelIdx)}}}: call instance bool {{{stateMachineFullName}}}::MoveNext__inoculated()
                        {{{GetNextLabel(ref labelIdx)}}}: stloc.0
                        {{{GetNextLabel(ref labelIdx)}}}: ldarg.0
                        {{{GetNextLabel(ref labelIdx)}}}: ldfld {{{type}}} {{{stateMachineFullName}}}::'<>2__current'
                        {{{GetNextLabel(ref labelIdx)}}}: ldarg.0
                        {{{GetNextLabel(ref labelIdx)}}}: ldfld class [Inoculator.Injector]Inoculator.Builder.Metadata {{{stateMachineFullName}}}::'<inoculated>__Metadata'
                        {{{(
                            isPrimitive
                                    ? $@"{GetNextLabel(ref labelIdx)}: box {ToProperNamedType(type)}"
                                    : String.Empty
                        )}}}
                        {{{GetNextLabel(ref labelIdx)}}}: callvirt instance void [Inoculator.Injector]Inoculator.Builder.Metadata::set_ReturnValue(object)
                        {{{attributeNames.Select(
                            (attrClassName, i) => $@"
                            {GetNextLabel(ref labelIdx)}: ldarg.0
                            {GetNextLabel(ref labelIdx)}: ldfld class {attrClassName} {stateMachineFullName}::'<inoculated>__Interceptor{i}'
                            {GetNextLabel(ref labelIdx)}: callvirt instance void {attrClassName}::OnSuccess(class [Inoculator.Injector]Inoculator.Builder.Metadata)"
                        ).Aggregate((a, b) => $"{a}\n{b}")}}}
                        {{{GetNextLabel(ref labelIdx)}}}: leave.s ***END***
                    } catch [System.Runtime]System.Exception
                    {
                        {{{GetNextLabel(ref labelIdx)}}}: dup
                        {{{GetNextLabel(ref labelIdx)}}}: ldarg.0
                        {{{GetNextLabel(ref labelIdx)}}}: ldfld class [Inoculator.Injector]Inoculator.Builder.Metadata {{{stateMachineFullName}}}::'<inoculated>__Metadata'
                        {{{GetNextLabel(ref labelIdx)}}}: callvirt instance void [Inoculator.Injector]Inoculator.Builder.Metadata::set_Exception(class [System.Runtime]System.Exception)
                        {{{attributeNames.Select(
                                (attrClassName, i) => $@"
                                {GetNextLabel(ref labelIdx)}: ldarg.0
                                {GetNextLabel(ref labelIdx)}: ldfld class [Inoculator.Injector]Inoculator.Builder.Metadata {stateMachineFullName}::'<inoculated>__Metadata'
                                {GetNextLabel(ref labelIdx)}: ldarg.0
                                {GetNextLabel(ref labelIdx)}: ldfld class {attrClassName} {stateMachineFullName}::'<inoculated>__Interceptor{i}'
                                {GetNextLabel(ref labelIdx)}: callvirt instance void {attrClassName}::OnException(class [Inoculator.Injector]Inoculator.Builder.Metadata)"
                            ).Aggregate((a, b) => $"{a}\n{b}")}}}
                        {{{GetNextLabel(ref labelIdx)}}}: throw
                    } 
                } finally
                {
                    {{{GetNextLabel(ref labelIdx)}}}: ldarg.0
                    {{{GetNextLabel(ref labelIdx)}}}: ldfld int32 {{{stateMachineFullName}}}::'<>1__state'
                    {{{GetNextLabel(ref labelIdx)}}}: ldc.i4.m1
                    {{{GetNextLabel(ref labelIdx)}}}: bne.un.s ***JUMPDEST2***

                    {{{attributeNames.Select(
                        (attrClassName, i) => $@"
                        {GetNextLabel(ref labelIdx)}: ldarg.0
                        {GetNextLabel(ref labelIdx)}: ldfld class [Inoculator.Injector]Inoculator.Builder.Metadata {stateMachineFullName}::'<inoculated>__Metadata'
                        {GetNextLabel(ref labelIdx)}: ldarg.0
                        {GetNextLabel(ref labelIdx)}: ldfld class {attrClassName} {stateMachineFullName}::'<inoculated>__Interceptor{i}'
                        {GetNextLabel(ref labelIdx)}: callvirt instance void {attrClassName}::OnExit(class [Inoculator.Injector]Inoculator.Builder.Metadata)"
                    ).Aggregate((a, b) => $"{a}\n{b}")}}}
                    {{{GetNextLabel(ref labelIdx, marks, "JUMPDEST2")}}}: endfinally
                }
                {{{GetNextLabel(ref labelIdx, marks, "END")}}}: nop
                {{{GetNextLabel(ref labelIdx)}}}: ldloc.0
                {{{GetNextLabel(ref labelIdx)}}}: ret
            }}
            """);

            foreach(var (label, idx) in marks)
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
                            .Append($".field public class [Inoculator.Injector]Inoculator.Builder.Metadata '<inoculated>__Metadata'")
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
        bool isStatic = metadata.MethodCall is Metadata.CallType.Static;
        metadata.Code = metadata.Code with {
            Body = metadata.Code.Body with {
                Items = new ARRAY<MethodDecl.Member>(
                    metadata.Code.Body.Items.Values
                        .SelectMany(item => {
                            //Where(x => x is not MethodDecl.LabelItem or MethodDecl.InstructionItem)
                            if(item is MethodDecl.LabelItem label) {
                                return new[] { label with {
                                        Value = new CodeLabel(new SimpleName(GetNextLabel(ref labelIdx)))
                                    }
                                };
                            } else if(item is MethodDecl.InstructionItem instruction) {
                                if(instruction.Value.Opcode == "ret") {
                                    var newcode = $$$"""
                                        {{{GetNextLabel(ref labelIdx)}}}: dup
                                        {{{GetNextLabel(ref labelIdx)}}}: ldstr "{{{new string(metadata.Code.ToString().ToCharArray().Select(c => c != '\n' ? c : ' ').ToArray())}}}"
                                        {{{GetNextLabel(ref labelIdx)}}}: newobj instance void [Inoculator.Injector]Inoculator.Builder.Metadata::.ctor(string)
                                        {{{ExtractArguments(metadata.Code.Header.Parameters, ref labelIdx, isStatic ? 0 : 1)}}}
                                        {{{GetNextLabel(ref labelIdx)}}}: callvirt instance void [Inoculator.Injector]Inoculator.Builder.Metadata::set_Parameters(object[])
                                        {{{GetNextLabel(ref labelIdx)}}}: stfld class [Inoculator.Injector]Inoculator.Builder.Metadata {{{stateMachineFullName}}}::'<inoculated>__Metadata'
                                        {{{attributeNames.Select(
                                            (attrClassName, i) => $"""
                                        {GetNextLabel(ref labelIdx)}: dup
                                        {GetNextLabel(ref labelIdx)}: newobj instance void {attrClassName}::.ctor()
                                        {GetNextLabel(ref labelIdx)}: stfld class {attrClassName} {stateMachineFullName}::'<inoculated>__Interceptor{i}'
                                        """).Aggregate((a, b) => $"{a}\n{b}")}}}
                                        {{{GetNextLabel(ref labelIdx)}}}: ret
                                        """;
                                    _ = TryParse<MethodDecl.Member.Collection>(newcode, out MethodDecl.Member.Collection res, out string err);
                                    return res.Items.Values;
                                } 
                            }
                            return new[] { item };
                        }).ToArray()
                ) {
                    Options = new ARRAY<MethodDecl.Member>.ArrayOptions() {
                        Delimiters = ('\0', '\n', '\0')
                    }
                }
            }
        };

        return Success<(ClassDecl.Class, MethodDecl.Method[]), Exception>.From((classRef, new[] { metadata.Code }));
    }

    private static Result<(ClassDecl.Class, MethodDecl.Method[]), Exception> RunNMMethodManipulation(Metadata metadata, string name, string[] attributeName)
    {
        var newMethod = Handle(metadata, metadata.ClassName, attributeName);
        switch (newMethod)
        {
            case Error<MethodDecl.Method, Exception> e_method:
                return Error<(ClassDecl.Class, MethodDecl.Method[]), Exception>.From(new Exception($"failed to parse new method\n{e_method.Message}"));
        }

        var n_method = newMethod as Success<MethodDecl.Method, Exception>;

        var renamedMethod = Reader.Parse<MethodDecl.Method>(metadata.Code.ToString().Replace(metadata.Name, name));
        return renamedMethod switch
        {
            Error<MethodDecl.Method, Exception> e_method
                => Error<(ClassDecl.Class, MethodDecl.Method[]), Exception>.From(new Exception($"failed to parse modified old method\n{e_method.Message}")),
            Success<MethodDecl.Method, Exception> o_method
                => Success<(ClassDecl.Class, MethodDecl.Method[]), Exception>.From((null, new[] { o_method.Value, n_method.Value }))
        };
    }

    public static Result<MethodDecl.Method, Exception> Handle(Metadata metadata, Identifier container, string[] AttributeClass)
    {
        int labelIdx = 0;
        StringBuilder builder = new();
        bool isVoidCall = !ReturnTypeOf(metadata.Code.Header, out var type);
        bool isPrimitive = _primitives.Contains(type);
        bool isStatic = metadata.MethodCall is Metadata.CallType.Static;
        bool hasArgs = metadata.Code.Header.Parameters.Parameters.Values.Length > 0;
        (int metadataOffset, int? resultOffset, int? returnOffset, int exceptionOffset) = (AttributeClass.Length, isVoidCall ? null : (int?)AttributeClass.Length + 1, isVoidCall ? null : (int?)AttributeClass.Length + 2, AttributeClass.Length + (isVoidCall ? 1 : 3));
        builder.AppendLine($".method {metadata.Code.Header} {{");
        foreach (var member in metadata.Code.Body.Items.Values)
        {
            if (member is MethodDecl.LabelItem or MethodDecl.InstructionItem or MethodDecl.LocalsItem) continue;
            builder.AppendLine(member.ToString());
        }
        builder.AppendLine($".maxstack {(hasArgs ? 8 : 2)}");
        builder.AppendLine($$$"""
        .locals init (
            {{{String.Join("\n", AttributeClass.Select((attrClassName, i) => $"class {attrClassName} interceptor{i},"))}}}
            class [Inoculator.Injector]Inoculator.Builder.Metadata metadata,
            {{{(
                isVoidCall
                    ? String.Empty
                    : $@" {type} result,
                          {type},"
            )}}}
            class [System.Runtime]System.Exception e
        )
        """);

        builder.Append($$$"""
        {{{AttributeClass.Select(
                (attrClassName, i) => $@"
                {GetNextLabel(ref labelIdx)}: newobj instance void {attrClassName}::.ctor()
                {GetNextLabel(ref labelIdx)}: stloc.s {i}"
            ).Aggregate((a, b) => $"{a}\n{b}")}}}
        {{{GetNextLabel(ref labelIdx)}}}: ldstr "{{{new string(metadata.Code.ToString().ToCharArray().Select(c => c != '\n' ? c : ' ').ToArray())}}}"
        {{{GetNextLabel(ref labelIdx)}}}: newobj instance void [Inoculator.Injector]Inoculator.Builder.Metadata::.ctor(string)
        {{{GetNextLabel(ref labelIdx)}}}: stloc.s {{{metadataOffset}}}

        {{{GetNextLabel(ref labelIdx)}}}: ldloc.s {{{metadataOffset}}}
        {{{GetNextLabel(ref labelIdx)}}}: ldc.i4.{{{metadata.Code.Header.Parameters.Parameters.Values.Length}}}
        {{{GetNextLabel(ref labelIdx)}}}: newarr [System.Runtime]System.Object
        
        {{{ExtractArguments(metadata.Code.Header.Parameters, ref labelIdx, isStatic ? 0 : 1)}}}
        {{{GetNextLabel(ref labelIdx)}}}: callvirt instance void [Inoculator.Injector]Inoculator.Builder.Metadata::set_Parameters(object[])

        {{{AttributeClass.Select(
                (attrClassName, i) => $@"
                {GetNextLabel(ref labelIdx)}: ldloc.s {i}
                {GetNextLabel(ref labelIdx)}: ldloc.s {AttributeClass.Length}
                {GetNextLabel(ref labelIdx)}: callvirt instance void {attrClassName}::OnEntry(class [Inoculator.Injector]Inoculator.Builder.Metadata)"
            ).Aggregate((a, b) => $"{a}\n{b}")}}}
        .try
        {
            .try
            {
                {{{(isStatic ? String.Empty : $@"{GetNextLabel(ref labelIdx)}: ldarg.0")}}}
                {{{LoadArguments(metadata.Code.Header.Parameters, ref labelIdx, isStatic ? 0 : 1)}}}
                {{{GetNextLabel(ref labelIdx)}}}: call {{{MkMethodReference(metadata.Code.Header, container)}}}
                {{{(
                    isVoidCall
                        ? String.Empty
                        : $@"{GetNextLabel(ref labelIdx)}: stloc.s {resultOffset}"
                )}}}
                {{{GetNextLabel(ref labelIdx)}}}: ldloc.s {{{metadataOffset}}}
                {{{(
                    isVoidCall
                        ? $@"{GetNextLabel(ref labelIdx)}: ldnull"
                        : $@"{GetNextLabel(ref labelIdx)}: ldloc.s {resultOffset}
                            {(isPrimitive
                                    ? $@"{GetNextLabel(ref labelIdx)}: box {ToProperNamedType(type)}"
                                    : String.Empty
                            )}"
                )}}}
                {{{GetNextLabel(ref labelIdx)}}}: callvirt instance void [Inoculator.Injector]Inoculator.Builder.Metadata::set_ReturnValue(object)
                {{{AttributeClass.Select(
                        (attrClassName, i) => $@"
                        {GetNextLabel(ref labelIdx)}: ldloc.s {i}
                        {GetNextLabel(ref labelIdx)}: ldloc.s {AttributeClass.Length}
                        {GetNextLabel(ref labelIdx)}: callvirt instance void {attrClassName}::OnSuccess(class [Inoculator.Injector]Inoculator.Builder.Metadata)"
                    ).Aggregate((a, b) => $"{a}\n{b}")}}}
                {{{(
                    isVoidCall
                        ? String.Empty
                        : $@"{GetNextLabel(ref labelIdx)}: ldloc.s {resultOffset}
                             {GetNextLabel(ref labelIdx)}: stloc.s {returnOffset}"
                )}}}
                {{{GetNextLabel(ref labelIdx)}}}: leave.s ***END***
            } 
            catch [System.Runtime]System.Exception
            {
                {{{GetNextLabel(ref labelIdx)}}}: stloc.s {{{exceptionOffset}}}
                {{{GetNextLabel(ref labelIdx)}}}: ldloc.s {{{metadataOffset}}}
                {{{GetNextLabel(ref labelIdx)}}}: ldloc.s {{{exceptionOffset}}}
                {{{GetNextLabel(ref labelIdx)}}}: callvirt instance void [Inoculator.Injector]Inoculator.Builder.Metadata::set_Exception(class [System.Runtime]System.Exception)
                {{{AttributeClass.Select(
                        (attrClassName, i) => $@"
                        {GetNextLabel(ref labelIdx)}: ldloc.s {i}
                        {GetNextLabel(ref labelIdx)}: ldloc.s {AttributeClass.Length}
                        {GetNextLabel(ref labelIdx)}: callvirt instance void {attrClassName}::OnException(class [Inoculator.Injector]Inoculator.Builder.Metadata)"
                    ).Aggregate((a, b) => $"{a}\n{b}")}}}
                {{{GetNextLabel(ref labelIdx)}}}: ldloc.s {{{exceptionOffset}}}
                {{{GetNextLabel(ref labelIdx)}}}: throw
            } 
        } 
        finally
        {
            {{{AttributeClass.Select(
                    (attrClassName, i) => $@"
                    {GetNextLabel(ref labelIdx)}: ldloc.s {i}
                    {GetNextLabel(ref labelIdx)}: ldloc.s {AttributeClass.Length}
                    {GetNextLabel(ref labelIdx)}: callvirt instance void {attrClassName}::OnExit(class [Inoculator.Injector]Inoculator.Builder.Metadata)"
                ).Aggregate((a, b) => $"{a}\n{b}")}}}
            {{{GetNextLabel(ref labelIdx)}}}: endfinally
        } 

        {{{(isVoidCall ? String.Empty : $@"{GetNextLabel(ref labelIdx)}: ldloc.s {returnOffset}")}}}
        {{{GetNextLabel(ref labelIdx)}}}: ret

        """);

        string endLabel = $"IL_{labelIdx - (isVoidCall ? 1 : 2):X4}";
        builder.Replace("***END***", endLabel);
        builder.AppendLine("}");
        var result = builder.ToString();
        return Reader.Parse<MethodDecl.Method>(result);
    }

    private static string InvokeFunctionOnTypes(string[] types, string functionSpec, int labelIdx) {
        string InvokeFunctionOnType(string type, string functionSpec) => $"{GetNextLabel(ref labelIdx)}: callvirt instance void {type}::{functionSpec}";
        return types.Select(type => InvokeFunctionOnType(type, functionSpec)).Aggregate((a, b) => $"{a}\n{b}");
    }

    private static bool ReturnTypeOf(MethodDecl.Prefix header, out string type) {
        var typeComp = header.Type.Components.Types.Values.First().AsTypePrefix();
        type = typeComp?.ToString() switch {
            "void" => null,
            _ => header.Type.ToString(),
        };
        return type != null;
    }

    private static string MkMethodReference(MethodDecl.Prefix Name, Identifier container) {
        // int32 Test::method_old(int32, object, uint8, class [System.Runtime]System.Collections.Generic.IEnumerable`1<string>, valuetype testE, string)
        var builder = new StringBuilder();
        builder.Append(Name.Type.ToString());
        if(container is not null) {
            builder.Append(" ");
            builder.Append(container.ToString());
            builder.Append("::");
        }
        builder.Append($"{Name.Name}__Inoculated");
        builder.Append("(");
        builder.Append(string.Join(", ", Name.Parameters.Parameters.Values.Select(x => x.ToString())));
        builder.Append(")");
        return builder.ToString();
    }

    public static string ExtractArguments(ParameterDecl.Parameter.Collection parameter, ref int labelIdx, int startingIdx) {
        StringBuilder builder = new StringBuilder();
        foreach(ParameterDecl.DefaultParameter param in parameter.Parameters.Values.OfType<ParameterDecl.DefaultParameter>()){
            builder.AppendLine(ExtractArgument(param, ref labelIdx, startingIdx++));
        }
        return builder.ToString();
    }

    public static string LoadArguments(ParameterDecl.Parameter.Collection parameter, ref int labelIdx, int startingIdx) {
        StringBuilder builder = new StringBuilder();
        foreach(ParameterDecl.DefaultParameter param in parameter.Parameters.Values.OfType<ParameterDecl.DefaultParameter>()){
            builder.AppendLine(LoadArgument(param, ref labelIdx, startingIdx++));
        }
        return builder.ToString();
    }

    static String[] _primitives = new String[] { "bool", "char", "float32", "float64", "int8", "int16", "int32", "int64", "uint8", "uint16", "uint32", "native" };
    public static string ExtractArgument(ParameterDecl.Parameter parameter, ref int labelIdx, int paramIdx = 0) {
        StringBuilder builder = new StringBuilder();
        if(parameter is not ParameterDecl.DefaultParameter param) {
            throw new Exception("Unknown parameter type");
        }
        var typeComp = param.TypeDeclaration.Components.Types.Values.First().AsTypePrefix()?.AsTypePrimitive();
        var ilcode = typeComp is null ? string.Empty  : $$$"""
            {{{GetNextLabel(ref labelIdx)}}}: dup
            {{{GetNextLabel(ref labelIdx)}}}: ldc.i4.s {{{paramIdx}}}
            {{{LoadArgument(param, ref labelIdx, paramIdx)}}}
            {{{( !_primitives.Contains(typeComp.TypeName) ? String.Empty :
                    $"{GetNextLabel(ref labelIdx)}: box {ToProperNamedType(typeComp.TypeName)}"
            )}}}
            {{{GetNextLabel(ref labelIdx)}}}: stelem.ref
            """;

        builder.Append(ilcode);
        return builder.ToString();
    }

    public static string LoadArgument(ParameterDecl.Parameter parameter, ref int labelIdx, int paramIdx = 0) {
        String[] _primitives = new String[] { "bool", "char", "float32", "float64", "int8", "int16", "int32", "int64", "uint8", "uint16", "uint32", "unsigned int8", "unsigned int16", "unsigned int32" , "native" };
        StringBuilder builder = new StringBuilder();
        if(parameter is not ParameterDecl.DefaultParameter param) {
            throw new Exception("Unknown parameter type");
        }
        var typeComp = param.TypeDeclaration?.ToString();
        var ilcode = typeComp is null ? String.Empty : $"{GetNextLabel(ref labelIdx)}: ldarg.s {param.Id}";

        builder.Append(ilcode);
        return builder.ToString();
    }

    private static string ToProperNamedType(string type)
    {
        string ret = type;

        if (!type.Contains("[System.Runtime]"))
        {
            ret = "[System.Runtime]System.";
            switch (type.ToLower())
            {
                case "int32":
                    ret += "Int32";
                    break;
                case "int16":
                    ret += "Int16";
                    break;
                case "int64":
                    ret += "Int64";
                    break;
                case "uint32":
                    ret += "UInt32";
                    break;
                case "uint16":
                    ret += "UInt16";
                    break;
                case "uint64":
                    ret += "UInt64";
                    break;
                case "long":
                    ret += "Int64";
                    break;
                case "ulong":
                    ret += "UInt64";
                    break;
                case "short":
                    ret += "Int16";
                    break;
                case "ushort":
                    ret += "UInt16";
                    break;
                case "decimal":
                    ret += "Decimal";
                    break;
                case "string":
                    ret += "Object";
                    break;
                case "bool":
                    ret += "Boolean";
                    break;
                case "float64":
                    ret += "Double";
                    break;
                case "double":
                    ret += "Double";
                    break;
                case "float32":
                    ret += "Single";
                    break;
                case "object":
                    ret += "Object";
                    break;
                case "byte":
                    ret += "Byte";
                    break;
                case "sbyte":
                    ret += "SByte";
                    break;
                case "char":
                    ret += "Char";
                    break;
                default:
                    if (type.StartsWith("valuetype "))
                        ret = ret.Replace("valuetype ", "");
                    else
                        ret = type;
                    break;
            }
        }

        return ret;
    }
}