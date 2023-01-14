using System;
using System.Extensions;
using System.IO;
using System.Text;
using Inoculator.Core;
using Inoculator.Builder;

var result = Reader
    .Parse<RootDecl.Declaration.Collection>(File.ReadAllText("./Test.il", Encoding.ASCII))
    .Bind(assembly => {
        var targetAttributes = Searcher.SearchForInterceptors(assembly);
        targetAttributes.ForEach(Console.WriteLine);
        foreach (var metadata in Searcher.SearchForMethods(assembly, 
            metadata => !metadata.Code.IsConstructor && Searcher.IsMarked(metadata.Code, targetAttributes)))
        {
            Console.WriteLine(metadata);
            switch(metadata.ReplaceNameWith($"{metadata.Name}__Inoculated"))
            {
                case Success<MethodDecl.Method[], Exception> success:
                    var (oldMethod, newMethod) = (success.Value[0], success.Value[1]);

                    File.WriteAllLines("./TestRes.il", new[] {
                        oldMethod.ToString().Replace("\0", ""),
                        newMethod.ToString().Replace("\0", "")
                    });
                    break;
                case Error<MethodDecl.Method[], Exception> error:
                    Console.WriteLine(error.Message);
                    break;
            }
        }
        return Success<int, Exception>.From(0);
    });

Console.WriteLine(result);