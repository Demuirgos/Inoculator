using System;
using System.Extensions;
using System.IO;
using System.Text;
using Inoculator.Core;
using Inoculator.Builder;

var result = Reader
    .Parse<RootDecl.Declaration.Collection>(File.ReadAllText("./TestI.il", Encoding.ASCII))
    .Bind(assembly => {
        var result = Weaver.Modify(assembly);
        if(result is Success<RootDecl.Declaration.Collection, Exception> success) {
            File.WriteAllTextAsync("./TestR.il", success.Value.ToString().Replace("\0", String.Empty), Encoding.ASCII);
        }
        return result;
    });

Console.WriteLine(result is Success<RootDecl.Declaration.Collection, Exception> ? "Success" : "Failure");