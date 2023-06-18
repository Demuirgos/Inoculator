using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Dove.Core;

namespace Inoculator.Builder;
public static class HandlerTools {
    public static string GetNextLabel(ref int labelIdx, Dictionary<string, string> marks = null, string mark = null) {
        string label = $"IL_{labelIdx++:X4}";
        if (marks is not null && mark is not null) marks.Add(mark, label);
        return label;
    }

    public static string CallMethodOnInterceptors(string classContainer, IEnumerable<InterceptorData> modifierClasses, string methodName, bool isSyncMode, ref int labelIdx, bool inverse = true) 
    {
        var interceptorsClass = modifierClasses.Where(m => m.IsInterceptor);
        if(inverse) interceptorsClass = interceptorsClass.Reverse();
        StringBuilder builder = new StringBuilder();
        foreach (var interceptorClass in interceptorsClass)
        {
            builder.Append(CallMethodOnInterceptor(classContainer, interceptorClass.ClassName, methodName, isSyncMode, ref labelIdx));
        }
        return builder.ToString();
    }

    public static string CallMethodOnInterceptorsSM(string classContainer, InterceptorData[] modifierClasses, string methodName, bool isSyncMode, ref int labelIdx, bool inverse = true) 
    {
        var interceptorsClass = modifierClasses.Where(m => m.IsInterceptor);
        if(inverse) interceptorsClass = interceptorsClass.Reverse();
        StringBuilder builder = new StringBuilder();
        foreach (var interceptorClass in interceptorsClass)
        {
            builder.Append(CallMethodOnInterceptorSM(classContainer, interceptorClass.ClassName, methodName, isSyncMode, ref labelIdx));
        }
        return builder.ToString();
    }

    public static string GenerateInterceptorName(string className) => $"'<inoculated>interceptor__{Math.Abs(className.GetHashCode()) % 1000:X3}'"; 

    public static string CallMethodOnInterceptor(string classContainer, string interceptorClass, string methodName, bool isSyncMode, ref int labelIdx) {
        string piping = isSyncMode 
            ? $@"ldloc.s {GenerateInterceptorName(interceptorClass)}
                 ldloc.s metadata"
            : $@"ldarg.0
                 ldfld class {interceptorClass} {classContainer}::{GenerateInterceptorName(interceptorClass)}
                 ldarg.0
                 ldfld class [Inoculator.Interceptors]Inoculator.Builder.MethodData {classContainer}::'<inoculated>__Metadata'";
        
        string callCode = $"callvirt instance void class {interceptorClass}::{methodName}(class [Inoculator.Interceptors]Inoculator.Builder.MethodData)";
        return $"{piping}\n{callCode}";
    }

     public static string CallMethodOnInterceptorSM(string classContainer, string interceptorClass, string methodName, bool isSyncMode, ref int labelIdx) {
        string piping = isSyncMode 
            ? $@"ldloc.s {GenerateInterceptorName(interceptorClass)}
                 ldloc.s metadata"
            : $@"ldarg.0
                 ldfld class {interceptorClass} {classContainer}::{GenerateInterceptorName(interceptorClass)}
                 ldarg.0
                 ldfld class [Inoculator.Interceptors]Inoculator.Builder.MethodData {classContainer}::'<inoculated>__Metadata'";
        
        string callCode = $"callvirt instance void class {interceptorClass}::{methodName}(class [Inoculator.Interceptors]Inoculator.Builder.MethodData)";
        return $"{piping}\n{callCode}";
    }

    public static string LoadArguments(ParameterDecl.Parameter.Collection parameter, bool isStatic) {
        StringBuilder builder = new StringBuilder();
        if(!isStatic) {
            builder.AppendLine(LoadThisArgument());
        }

        foreach(ParameterDecl.DefaultParameter param in parameter.Parameters.Values.OfType<ParameterDecl.DefaultParameter>()){
            builder.AppendLine(LoadArgument(param));
        }
        return builder.ToString();
    }

    public static string ExtractArguments(MethodData methodAst, bool isStatic) {
        StringBuilder builder = new StringBuilder();
        int startingIdx = isStatic ? 0 : 1;

        if(!isStatic) {
            builder.AppendLine(ExtractThisArgument(methodAst.ClassReference));
        }
        foreach(ParameterDecl.DefaultParameter param in methodAst.Code.Header.Parameters.Parameters.Values.OfType<ParameterDecl.DefaultParameter>()){
            builder.AppendLine(ExtractArgument(param, startingIdx++));
        }
        return builder.ToString();
    }

    public static string UpdateRefArguments(ParameterDecl.Parameter.Collection parameter, bool isStatic) {
        StringBuilder builder = new StringBuilder();
        int startingIdx = isStatic ? 0 : 1;

        builder.AppendLine($"");
        foreach(ParameterDecl.DefaultParameter param in parameter.Parameters.Values.OfType<ParameterDecl.DefaultParameter>()){
            builder.AppendLine(UpdateRefArgument(param, startingIdx++));
        }
        return builder.ToString();
    }

    public static string ReflectRefArguments(ParameterDecl.Parameter.Collection parameter, bool isStatic) {
        StringBuilder builder = new StringBuilder();
        int startingIdx = isStatic ? 0 : 1;

        builder.AppendLine($"");
        foreach(ParameterDecl.DefaultParameter param in parameter.Parameters.Values.OfType<ParameterDecl.DefaultParameter>()){
            builder.AppendLine(ReflectRefArgument(param, startingIdx++));
        }
        return builder.ToString();
    }

    public static string ExtractThisArgument(ClassDecl.Prefix ClassHeader) {
        StringBuilder builder = new StringBuilder();
        var IsValueType = ClassHeader.Implements is not null && ClassHeader.Implements.Types.ToString(string.Empty).Contains("System.ValueType");
        builder.Append($$$"""
            dup
            ldc.i4.s 0
            ldarg.0
        """);
        if(IsValueType) {
            builder.Append($$$"""
            ldobj {ClassHeader.Name}
            box {ClassHeader.Name}
        """);
        }   
        builder.Append($$$"""
            dup
            callvirt instance class [System.Runtime]System.Type [System.Runtime]System.Object::GetType()
            ldstr "<>this"
            newobj instance void [Inoculator.Interceptors]Inoculator.Builder.ParameterData::.ctor(object,class [System.Runtime]System.Type,string)
            stelem.ref
        """);

        return builder.ToString();

    }

    public static string ExtractReturnValue(TypeData Output, ref int labelIdx) {
        StringBuilder builder = new StringBuilder();
        builder.Append($$$"""
            {{{(
                Output.IsVoid
                    ? $@"ldnull"
                    : $@"ldloc.s result
                        {(  Output.IsReferenceType ? String.Empty
                            : $@"box {Output.ToProperName}"
                        )}"
            )}}}
            dup
            callvirt instance class [System.Runtime]System.Type [System.Runtime]System.Object::GetType()
            ldnull
            newobj instance void [Inoculator.Interceptors]Inoculator.Builder.ParameterData::.ctor(object,class [System.Runtime]System.Type,string)
            callvirt instance void [Inoculator.Interceptors]Inoculator.Builder.MethodData::set_ReturnValue(class [Inoculator.Interceptors]Inoculator.Builder.ParameterData)
        """);

        return builder.ToString();

    }

    public static string ExtractArgument(ParameterDecl.Parameter parameter, int paramIdx = 0) {
        StringBuilder builder = new StringBuilder();
        if(parameter is not ParameterDecl.DefaultParameter param) {
            throw new Exception("Unknown parameter type");
        }
        
        var typeData = new TypeData(parameter);
        var ilcode = typeData.IsVoid ? string.Empty  : $$$"""
            dup
            ldc.i4.s {{{paramIdx}}}
            {{{LoadArgument(param)}}}
            {{{(!typeData.IsByRef ? string.Empty
                    : $"{GetCILIndirectLoadOpcode(typeData).load}" 
            )}}}
            {{{( typeData.IsReferenceType ? String.Empty 
                    : $"box {(typeData.IsGeneric ? typeData.FilteredName(true, false) : typeData.ToProperName)}"
            )}}}
            dup
            callvirt instance class [System.Runtime]System.Type [System.Runtime]System.Object::GetType()
            ldstr "{{{parameter.AsDefaultParameter()?.Id}}}"
            newobj instance void [Inoculator.Interceptors]Inoculator.Builder.ParameterData::.ctor(object,class [System.Runtime]System.Type,string)
            stelem.ref
            """;

        builder.Append(ilcode);
        return builder.ToString();
    }

    public static string MkMethodReference(this MethodData @this,bool isInoculated, string? path = null) {
        var builder = new StringBuilder();
        if(@this.MethodCall is MethodData.CallType.Instance)
            builder.Append("instance ");
        builder.Append(@this.Signature.Output.Code);
        if(@this.ClassReference is not null) {
            builder.Append(" ");
            builder.Append(path ?? @this.ClassReference.Id.ToString());
            builder.Append("::");
        }
        builder.Append(isInoculated ? @this.MangledName(false) : @this.Name(false));
        if(@this.TypeParameters?.Length > 0) {
            builder.Append("<");
            builder.Append(string.Join(", ", @this.Code.Header.TypeParameters.Parameters.Values.Select(param => $"!!{param.Id}")));
            builder.Append(">");
        }
        builder.Append("(");
        builder.Append(string.Join(", ", @this.Code.Header.Parameters.Parameters.Values.Select(x => x.ToString())));
        builder.Append(")");
        return builder.ToString();
    }

    public static string UpdateRefArgument(ParameterDecl.Parameter parameter, int paramIdx = 0) {
        StringBuilder builder = new StringBuilder();
        if(parameter is not ParameterDecl.DefaultParameter param) {
            throw new Exception("Unknown parameter type");
        }
        
        var typeData = new TypeData(parameter);

        if(!typeData.IsByRef) {
            return string.Empty;
        }

        var ilcode = typeData.IsVoid ? string.Empty  : $$$"""
            ldloc.s metadata
            callvirt instance class [Inoculator.Interceptors]Inoculator.Builder.ParameterData[] [Inoculator.Interceptors]Inoculator.Builder.MethodData::get_Parameters()
            ldc.i4.s {{{paramIdx}}}
            ldelem.ref
            {{{LoadArgument(param)}}}
            {{{GetCILIndirectLoadOpcode(typeData).load}}}
            {{{( typeData.IsReferenceType ? String.Empty 
                    : $"box {(typeData.IsGeneric ? typeData.FilteredName(true, false) : typeData.ToProperName)}"
            )}}}
            
            callvirt instance void [Inoculator.Interceptors]Inoculator.Builder.ParameterData::set_Value(object)
            """;

        builder.Append(ilcode);
        return builder.ToString();
    }

    public static string ReflectRefArgument(ParameterDecl.Parameter parameter, int paramIdx = 0) {
        StringBuilder builder = new StringBuilder();
        if(parameter is not ParameterDecl.DefaultParameter param) {
            throw new Exception("Unknown parameter type");
        }
        
        var typeData = new TypeData(parameter);

        if(!typeData.IsByRef) {
            return string.Empty;
        }

        var ilcode = typeData.IsVoid ? string.Empty  : $$$"""
            {{{LoadArgument(param)}}}
            ldloc.s metadata
            callvirt instance class [Inoculator.Interceptors]Inoculator.Builder.ParameterData[] [Inoculator.Interceptors]Inoculator.Builder.MethodData::get_Parameters()
            ldc.i4.s {{{paramIdx}}}
            ldelem.ref
            callvirt instance object [Inoculator.Interceptors]Inoculator.Builder.ParameterData::get_Value()
            {{{(typeData.IsReferenceType ? $"castclass {typeData.Name}" : $"unbox.any {typeData.ToProperName}")}}}
            {{{GetCILIndirectLoadOpcode(typeData).set}}}
            """;

        builder.Append(ilcode);
        return builder.ToString();
    }

    public static bool HasField(ClassDecl.Class classRef, string fieldName) => classRef.Members.Members.Values.Any(x => x is ClassDecl.FieldDefinition field && field.Value.Id.ToString() == fieldName);

    public static string LoadThisArgument() {
        StringBuilder builder = new StringBuilder();
        var ilcode = $"ldarg.0";
        builder.Append(ilcode);
        return builder.ToString();
    }
    public static string LoadArgument(ParameterDecl.Parameter parameter) {
        StringBuilder builder = new StringBuilder();
        if(parameter is not ParameterDecl.DefaultParameter param) {
            throw new Exception("Unknown parameter type");
        }
        var typeComp = param.TypeDeclaration?.ToString();
        var ilcode = typeComp is null ? String.Empty : $"ldarg.s {param.Id}";
        builder.Append(ilcode);
        return builder.ToString();
    }

    public static (string load, string set) GetCILIndirectLoadOpcode(TypeData type) {
        return type.PureName switch {
            "int"   => ("ldind.i", "stind.i"),
            "int8"  => ("ldind.i1", "stind.i1"),
            "int16" => ("ldind.i2", "stind.i2"),
            "int32" => ("ldind.i4", "stind.i4"),
            "int64" => ("ldind.i8", "stind.i8"),
            "uint8" or "unsigned int8"  => ("ldind.u1", "stind.u1"),
            "int16" or "unsigned int16" => ("ldind.u2", "stind.u2"),
            "int32" or "unsigned int32" => ("ldind.u4", "stind.u4"),
            "int64" or "unsigned int64" => ("ldind.u8", "stind.u8"),
            "float32" => ("ldind.r4", "stind.r4"),
            "float64" => ("ldind.r8", "stind.r8"),
            _ when type.PureName.StartsWith("valuetype") => ("ldobj", "stobj"),
            _ => ("ldind.ref", "stind.ref")
        };
    }

    public static string StringifyPath(MethodData metadata, ClassDecl.Prefix classRef, IEnumerable<string> path, bool isStruct, int ForStateMachine = 0) {
        var pathList = path?.ToList(); 
        bool isContainedInStruct = classRef.Extends.Type.ToString() == "[System.Runtime] System.ValueType";
        var functionFullPathBuilder = new StringBuilder()
            .Append(isStruct ? "valuetype " : "class ")
            .Append($"{String.Join("/", path)}");

        if(ForStateMachine > 0) {
            functionFullPathBuilder.Append($"/{classRef.Id}");
        }

        if (classRef.TypeParameters?.Parameters.Values.Length > 0)
        {
            if(ForStateMachine > 0) {
                if(ForStateMachine == 1) {
                    functionFullPathBuilder.Append("<")
                        .Append(String.Join(", ", classRef.TypeParameters.Parameters.Values.Select(p => $"!{p}")))
                        .Append(">");
                } else {
                    var classTypeParametersCount = classRef.TypeParameters.Parameters.Values.Length;
                    var functionTypeParametersCount = metadata.Code.Header.TypeParameters?.Parameters.Values.Length ?? 0;
                    var classTPs = classRef.TypeParameters.Parameters.Values.Take(classTypeParametersCount - functionTypeParametersCount).Select(p => $"!{p}");
                    var methodTPs = classRef.TypeParameters.Parameters.Values.TakeLast(functionTypeParametersCount).Select(p => $"!!{p}");
                    functionFullPathBuilder.Append("<")
                        .Append( String.Join(",", classTPs.Union(methodTPs)))
                        .Append(">");
                }
            } else {
                functionFullPathBuilder.Append("<")
                    .Append(String.Join(", ", classRef.TypeParameters.Parameters.Values.Select(p => $"!{p}")))
                    .Append(">");

            }
        }
        var functionFullPath = functionFullPathBuilder.ToString();
        return functionFullPath;
    }

    public static string GetReflectiveMethodInstance(MethodData function, string classPath) {
        return $$$"""
            ldtoken {{{classPath[classPath.IndexOf(' ')..]}}}
            call class [System.Runtime]System.Type [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
            callvirt instance string [System.Runtime]System.Type::get_FullName()
            call class [System.Runtime]System.Type [System.Runtime]System.Type::GetType(string)
            ldstr "{{{function.Name(false)}}}"
            callvirt instance class [System.Runtime]System.Reflection.MethodInfo [System.Runtime]System.Type::GetMethod(string)
            stloc.s methodInfo
        """;
    }

    public static string GetAttributeInstance(InterceptorData attribute) {
        var attributeRef = attribute.ClassName.Contains('<') ? attribute.ClassName[..attribute.ClassName.IndexOf('<')] : attribute.ClassName;
        var hook = $$$"""
            ldtoken {{{attributeRef}}}
            call class [System.Runtime]System.Type [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
            ldloc.s methodInfo
            call !!0 [Inoculator.Injector]Inoculator.Builder.HandlerTools/AttributeResolver::GetAttributeInstance<class {{{attribute.ClassName}}}>(class [System.Runtime]System.Type, class [System.Runtime]System.Reflection.MethodInfo)
        """;
        return hook;
    } 

    public static string GetCleanedString(string str) {
        return str.Replace("\\", "\\\\")
                    .Replace("\"","'")
                    .Replace("\r", "\\r")
                    .Replace("\n", "\\n")
                    .Replace("\t", "\\t");
    } 

    public static System.Extensions.Result<T, Exception> ReplaceSymbols<T>(T source, string[] prefixPatterns, string symbol, string replacement, Func<string, string> Modifier = null) where T : IDeclaration<T> {
        var newSource = source.ToString();
        if(Modifier != null) {
            newSource = Modifier(newSource);
        }
        foreach(var pattern in prefixPatterns) {
            newSource = newSource.Replace($"{pattern}{symbol}", $"{pattern}{replacement}");
        }
        if(Parser.TryParse(newSource, out T result, out string error)) {
            return System.Extensions.Success<T, Exception>.From(result);
        }
        return System.Extensions.Error<T, Exception>.From(new Exception(error));
    }

    public static class AttributeResolver {
        public static T GetAttributeInstance<T>(Type attrType, MethodInfo methodInfo){
            var attrs = methodInfo.CustomAttributes;
            var attr = attrs.Where(attr => attr.AttributeType.GUID == attrType.GUID).FirstOrDefault();
            if(attr == null) {
                return default;
            }
            var instance =  (T)attr?.Constructor.Invoke(attr.ConstructorArguments.Select(arg => arg.Value).ToArray());
            return instance;
        }
    }

}