using System;
using System.Extensions;
using System.IO;
using System.Text;
using Inoculator.Core;
using Inoculator.Builder;

Reader
    .Parse<RootDecl.Declaration.Collection>(File.ReadAllText("./Test.il", Encoding.ASCII))
    .Bind(assembly => {
        List<Metadata> SearchForMethods(ClassDecl.Class type)
        {
            var nestedTypes = type.Members.Members.Values.OfType<ClassDecl.NestedClass>();
            var result = nestedTypes.SelectMany(c => SearchForMethods(c.Value)).ToList();

            var methods = type.Members.Members.Values.OfType<ClassDecl.MethodDefinition>();
            var metadata = methods.Select(x => new Metadata(x.Value) {
                ClassName = type.Header.Id
            });
            result.AddRange(metadata);
            return result;
        }
        foreach (var declaration in assembly.Declarations.Values)
        {
            if(declaration is not ClassDecl.Class type) continue;
            foreach (var metadata in SearchForMethods(type))
            {
                if(metadata.Code.IsConstructor || !metadata.IsMarked) continue;
                switch(metadata.ReplaceNameWith($"{metadata.Name}__Inoculated"))
                {
                    case Success<MethodDecl.Method[], Exception> success:
                        var (oldMethod, newMethod) = (success.Value[0], success.Value[1]);
                        File.AppendAllLines("./TestRes.il", new[] {
                            oldMethod.ToString().Replace("\0", ""),
                            newMethod.ToString().Replace("\0", "")
                        });
                        break;
                    case Error<MethodDecl.Method[], Exception> error:
                        Console.WriteLine(error.Message);
                        break;
                }
            }
        }
        return Success<int, Exception>.From(0);
    });
