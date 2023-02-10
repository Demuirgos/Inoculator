using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Inoculator.Builder;
public class ExceptionData : Printable<ExceptionData> {
    public record Datum(String Message, string StackTrace, String Source);
    public ExceptionData(Exception e) {
        switch(e) {
            case AggregateException agg : 
                InnerData = new Datum(agg?.Message, agg?.StackTrace, agg?.Source);
                ChildData = agg.InnerExceptions.Select(i_e => new ExceptionData(i_e)).ToArray();
                break;
            default : 
                InnerData = new Datum(e?.Message, e?.StackTrace, e?.Source);
                ChildData = e?.InnerException is not null 
                                ? new  ExceptionData[] {new ExceptionData(e?.InnerException)}
                                : null ;
                break;
        }
    } 
    public Datum InnerData {get; set;}
    public ExceptionData[] ChildData {get; set;}
}
public class TypeData : Printable<TypeData> {
    public enum TypeBehaviour {
        ValueType, ReferenceType
    }

    public enum TypeDegree {
        Zero, One
    }

    public enum TypeNature {
        Value, Pointer
    }

    public enum TypeKind {
        Class, Struct, Interface, Enum, Delegate
    }

    public enum TypeValue {
        Typed, Void, VarArg
    }


    [JsonIgnore]
    public TypeDecl.Type Code { get; set; }

    public TypeData(TypeDecl.Type source) => Code = source;

    public TypeData(string sourceCode) => Code = Dove.Core.Parser.Parse<TypeDecl.Type>(sourceCode);

    public TypeData(ParameterDecl.Parameter param) {
        Code = param switch {
            ParameterDecl.DefaultParameter p => p.TypeDeclaration,
            ParameterDecl.VarargParameter p => throw new Exception("vararg parameters are not supported"),
        };
    }

    public String FilteredName(bool removeRefIndicators, bool removeGenIndicators) {
        var fullName = Code.ToString().Trim();
        if(removeGenIndicators) {
            fullName = fullName.Replace("!", string.Empty);
        }
        if(removeRefIndicators) {
            fullName = fullName.Replace("&", string.Empty);
        }
        return fullName.Trim();
    }
    public String Name => FilteredName(false, false);
    
    [JsonIgnore]
    public String PureName => FilteredName(true, true);
    [JsonIgnore]
    public TypeBehaviour Behaviour => IsValueType ? TypeBehaviour.ValueType : TypeBehaviour.ReferenceType;
    public bool IsReferenceType => Behaviour is TypeBehaviour.ReferenceType;
    [JsonIgnore]
    public TypeNature Nature => Code.Components.Types.Values.Any(comp => comp is TypeDecl.ReferenceSuffix) ? TypeNature.Pointer : TypeNature.Value;
    public bool IsByRef => Nature is TypeNature.Pointer;
    [JsonIgnore]
    public TypeDegree GenericOrder => Code.Components.Types.Values.Any(comp => comp is TypeDecl.GenericTypeParameter) ? TypeDegree.One : TypeDegree.Zero;
    public bool IsGeneric => GenericOrder is TypeDegree.One;
    [JsonIgnore]
    public string ToProperName => ToProperNamedType(PureName);
    [JsonIgnore]
    public TypeValue ValueKind => Name == "void" ? TypeValue.Void : TypeValue.Typed;
    [JsonIgnore]
    public bool IsVoid => ValueKind is TypeValue.Void;
    [JsonIgnore]
    private bool IsValueType {
        get {
            String[] _primitives = new String[] { "bool", "char", "float32", "float64", "int8", "int16", "int32", "int64", "uint8", "uint16", "uint32", "native" };
            return _primitives.Contains(PureName) || PureName.StartsWith("valuetype") || IsGeneric;
        }
    }    

    public static string ToProperNamedType(string type)
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
                    else if (type.StartsWith("class "))
                        ret = ret.Replace("class ", "");
                    else
                        ret = type;
                    break;
            }
        }

        return ret;
    }
}

public class ParameterData : Printable<ParameterData> {
    public ParameterData(string typesrc, Object Value, string name = null) {
        Name = name is null ? String.Empty : name;
        Type = new TypeData(typesrc);
        this.Value = Value;
    }

    public String Name {get; init; }
    public TypeData Type { get; init; }
    public Object Value { get; set; }
}

public class MethodData : Printable<MethodData> {
    public enum MethodType {
        Async, Sync, Iter
    }

    public enum CallType {
        Static, Instance
    }

    public Object EmbededResource { get; set; }
    public MethodData(MethodDecl.Method source) => Code = source;

    public MethodData(string sourceCode, string classRefHeader, string path) {
        ClassReference = Dove.Core.Parser.Parse<ClassDecl.Prefix>(classRefHeader);    
        Code = Dove.Core.Parser.Parse<MethodDecl.Method>(sourceCode);
        ReferencePath = path[(1+path.IndexOf(" "))..].Replace("/", "+");
    }

    
    public string ReferencePath {get; init;}
    public string MethodName => Code.Header.Name.ToString();
    public String Name(bool isFull) => $"{Code.Header.Name}{(isFull ? $"<Code.Header.TypeParameters.ToString().Trim()>" : string.Empty)}";
    public string MangledName(bool isFull) => $"'<{TypeSignature.Replace(" ", String.Empty)}>__{Name(false)}__Inoculated'{(isFull ? $"<Code.Header.TypeParameters.ToString().Trim()>" : string.Empty)}";
    

    public string TypeSignature => $"({string.Join(", ", Signature.Input.Select(item => item.Name.ToString().Replace(" ", String.Empty)))}) -> {Signature.Output.Code}";
    public string[] TypeParameters => Code.Header?.TypeParameters?
        .Parameters.Values
        .Select(x => x.Id.ToString())
        .ToArray() ?? new string[0];

    public ParameterData?[] Parameters { get; set; }
    public ExceptionData ExceptionRef => Exception is null ? null : new ExceptionData(Exception);
    public ParameterData? ReturnValue { get; set; }

    [JsonIgnore]
    public ClassDecl.Prefix ClassReference {get; init;}
    [JsonIgnore]
    public (TypeData[] Input, TypeData Output)  Signature 
        => (Code.Header.Parameters.Parameters.Values.Length > 0 
                ? Code.Header.Parameters.Parameters.Values.Select(x => new TypeData(x)).ToArray()
                : new[] { new TypeData("void") },
            new TypeData(Code.Header.Type));

    [JsonIgnore]
    public MethodType MethodBehaviour =>
        Code.Body.Items.Values.
            OfType<MethodDecl.CustomAttributeItem>()
            .Any(a => a.Value.AttributeCtor.Spec.ToString() == "[System.Runtime] System.Runtime.CompilerServices.AsyncStateMachineAttribute")
            ? MethodType.Async : 
                Code.Body.Items.Values
                    .OfType<MethodDecl.CustomAttributeItem>()
                    .Any(a => a.Value.AttributeCtor.Spec.ToString() == "[System.Runtime] System.Runtime.CompilerServices.IteratorStateMachineAttribute")
                ? MethodType.Iter  : MethodType.Sync;
    [JsonIgnore]
    public CallType MethodCall => Code.Header.Convention is null || Code.Header.MethodAttributes.Attributes.Values.Any(a => a is AttributeDecl.MethodSimpleAttribute { Name: "static" }) ? CallType.Static : CallType.Instance;
    [JsonIgnore]
    public bool IsStatic => MethodCall is CallType.Static;
    [JsonIgnore]
    public Exception Exception { get; set; }
    [JsonIgnore]
    public MethodDecl.Method Code { get; set; }
    [JsonIgnore]
    public MethodInfo ReflectionInfo {
        get {
            switch(MethodBehaviour) {
                case MethodType.Sync:
                    var assembly = Assembly.GetCallingAssembly();
                    var type = assembly.GetType(ReferencePath);
                    var functionName = $"{MangledName(false).ToString()[1..^1]}";
                    if(TypeParameters.Length > 0)
                        functionName += $"`{TypeParameters.Length}";
                    var methodinfo = type.GetMethod(functionName); 
                    return methodinfo;
                default:
                    throw new Exception("Invalid method call type");
            }
        }
    }
    [JsonIgnore]
    public ClassDecl.Class Generated { get; set; }
    
    public string MkMethodReference(bool isInoculated, string? path = null) {
        var builder = new StringBuilder();
        if(MethodCall is MethodData.CallType.Instance)
            builder.Append("instance");
        builder.Append(Signature.Output.Code);
        if(ClassReference is not null) {
            builder.Append(" ");
            builder.Append(path ?? ClassReference.Id.ToString());
            builder.Append("::");
        }
        builder.Append(isInoculated ? MangledName(false) : Name(false));
        if(TypeParameters.Length > 0) {
            builder.Append("<");
            builder.Append(string.Join(", ", Code.Header.TypeParameters.Parameters.Values.Select(param => $"!!{param.Id}")));
            builder.Append(">");
        }
        builder.Append("(");
        builder.Append(string.Join(", ", Code.Header.Parameters.Parameters.Values.Select(x => x.ToString())));
        builder.Append(")");
        return builder.ToString();
    }

}