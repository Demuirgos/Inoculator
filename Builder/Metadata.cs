using System.Extensions;
using System.Text.Json;
using System.Text.Json.Serialization;
using Inoculator.Core;

namespace Inoculator.Builder;

public class Metadata : Printable<Metadata> {
    public Object EmbededResource { get; set; }
    public Metadata(MethodDecl.Method source) => Code = source;
    public Metadata(string sourceCode) => Code = Reader.Parse<MethodDecl.Method>(sourceCode) switch {
        Success<MethodDecl.Method, Exception> success => success.Value,
        Error<MethodDecl.Method, Exception> failure => throw failure.Message,
    };
    public IdentifierDecl.Identifier ClassName {get; set;}
    public String Name => Code.Header.Name.ToString();
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

    public Result<MethodDecl.Method[], Exception> ReplaceNameWith(String name) {
        var newMethod = Wrapper.Handle(Code, ClassName);
        if(newMethod is not Success<MethodDecl.Method, Exception> n_method) 
            return Error<MethodDecl.Method[], Exception>.From(new Exception("failed to wrap nethod"));
        var odlMethod = Reader.Parse<MethodDecl.Method>(Code.ToString().Replace(Name, name));
        if(odlMethod is not Success<MethodDecl.Method, Exception> o_method) 
            return Error<MethodDecl.Method[], Exception>.From(new Exception("failed to parse modifed old method"));

        return Success<MethodDecl.Method[], Exception>.From(new[] { o_method.Value, n_method.Value });
    }
}