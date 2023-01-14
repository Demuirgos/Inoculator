using System.Extensions;
using System.Text.Json;
using System.Text.Json.Serialization;
using Inoculator.Core;

namespace Inoculator.Builder;

public class Metadata : Printable<Metadata> {
    public Metadata(MethodDecl.Method source) => Code = source;
    public Metadata(string sourceCode) => Code = Reader.Parse<MethodDecl.Method>(sourceCode) switch {
        Success<MethodDecl.Method, Exception> success => success.Value,
        Error<MethodDecl.Method, Exception> failure => throw failure.Message,
    };
    public String Name => Code.Header.Name.ToString();
    public bool IsMarked => Code.Body
        .Items.Values
        .OfType<MethodDecl.CustomAttributeItem>()
        .Any(
            attr => attr.Value.AttributeCtor.Spec.ToString().Contains("InterceptorAttribute")
        );
    [JsonIgnore]
    public (String[] Input, String Output)  Signature 
        => (
            Code.Header.Parameters.Parameters.Values.Select(
                x => x switch {
                    ParameterDecl.DefaultParameter p => p.TypeDeclaration.ToString(),
                    ParameterDecl.VarargParameter p => "...",
                }).ToArray(),
            Code.Header.Type.ToString()
        );

    public string TypeSignature => $"({string.Join(", ", Signature.Input)} -> {Signature.Output})";
    public string[] TypeParameters => Code.Header?.TypeParameters?
        .Parameters.Values
        .Select(x => x.Id.ToString())
        .ToArray() ?? new string[0];
    public Object?[] Parameters { get; set; }
    public Exception Exception{ get; set; }
    public Object? ReturnValue { get; set; }
    [JsonIgnore]
    public MethodDecl.Method Code { get; set; }

    public MethodDecl.Method ReplaceNameWith(String name) {
        var code = Code.ToString().Replace(Name, name).Replace("\0", "");
        return Reader.Parse<MethodDecl.Method>(code) switch {
            Success<MethodDecl.Method, Exception> success => success.Value,
            Error<MethodDecl.Method, Exception> failure => throw failure.Message,
        };
    }
}