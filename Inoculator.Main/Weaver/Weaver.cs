using System;
using System.Collections.Generic;
using System.Linq;
using System.Extensions;
using System.Diagnostics;
using IL_Unit = RootDecl.Declaration.Collection;
using Inoculator.Builder;
using RootDecl;

namespace Inoculator.Core;

public class Weaver {
    public static Result<string, Exception> Assemble(string code, string path)
        => Writer.Create<Writer>(path) switch {
            Success<Writer, Exception> writer 
                => writer.Value.Run(),
            Error<Writer, Exception> error 
                => Error<string, Exception>.From(error.Message)
        }; 

    public static Result<IL_Unit, Exception> Modify(IL_Unit assembly) {
        var targetAttributes = Searcher.SearchForInterceptors(assembly);
        Declaration[] HandleDeclaration(Declaration declaration) {
            switch(declaration) {
                case ClassDecl.Class type:
                    return HandleClass(type, new List<string>());
                default:
                    return new[] { declaration };
            }
        }

        ClassDecl.Class[] HandleClass(ClassDecl.Class @class, List<string> parentNamespaces = null) {
            // TODO: Handle nested classes name collisions
            List<string> flaggedNestedClasses = new();
            parentNamespaces.Add(@class.Header.Id.ToString());
            ClassDecl.Member[] HandleMember(ClassDecl.Member member){
                switch(member) {
                    case ClassDecl.MethodDefinition method:
                        var result = HandleMethod(method.Value, @class, parentNamespaces);
                        var CastedResult = result.Item2.Select(x => new ClassDecl.MethodDefinition(x) as ClassDecl.Member);
                        if(result.Item1 is not null) {
                            flaggedNestedClasses.Add(result.Item1.Header.Id.ToString());
                            CastedResult = CastedResult.Append(new ClassDecl.NestedClass(result.Item1));
                        }
                        return CastedResult.ToArray();
                    case ClassDecl.NestedClass type:
                        return HandleClass(type.Value, parentNamespaces).Select(x => new ClassDecl.NestedClass(x)).ToArray();
                    default:
                        return new[] { member };
                }
            } 

            var classMethodSegregation = @class.Members.Members.Values
                .GroupBy(x => x is ClassDecl.NestedClass);
            
            var methodMembers = classMethodSegregation
                .Where(x => !x.Key)
                .SelectMany(x => x.SelectMany(y => HandleMember(y)));

            foreach (var flagged in flaggedNestedClasses)
            {
                Console.WriteLine("flagged : " + flagged);
            }

            var nestedClasses = classMethodSegregation
                .Where(x => x.Key)
                .SelectMany(members =>  
                    members.Cast<ClassDecl.NestedClass>()
                        .Where(c => !flaggedNestedClasses.Contains(c.Value.Header.Id.ToString()))
                        .SelectMany(y => HandleMember(y))
                );
            var newMembers = methodMembers.Cast<ClassDecl.Member>().Union(nestedClasses).ToArray();

            return new[] { @class with { Members = @class.Members with { Members = new ARRAY<ClassDecl.Member>(newMembers) {
                Options = new ARRAY<ClassDecl.Member>.ArrayOptions() {
                    Delimiters = ('\0', '\n', '\0')
                }
            }}}};
        }

        (ClassDecl.Class, MethodDecl.Method[]) HandleMethod(MethodDecl.Method method, ClassDecl.Class parent, IEnumerable<string> path) {
            var metadata = new MethodData(method) {
                ClassName = parent.Header.Id
            };

            if(!metadata.Code.IsConstructor && Searcher.IsMarked(metadata.Code, targetAttributes, out string[] marks)) {
                if(metadata.MethodBehaviour is MethodData.MethodType.Sync) {
                    var result = Wrapper.ReplaceNameWith(metadata, marks);
                    if(result is Success<(ClassDecl.Class, MethodDecl.Method[]), Exception> success) {
                        return success.Value;
                    } else if(result is Error<(ClassDecl.Class, MethodDecl.Method[]), Exception> failure) {
                        throw failure.Message;
                    }
                } else {
                    var generatedStateMachineClass = parent.Members.Members.Values
                        .OfType<ClassDecl.NestedClass>()
                        .Select(x => x.Value)
                        .Where(x => x.Header.Id.ToString().StartsWith($"'<{metadata.Name}>"))
                        .FirstOrDefault();
                    var result = Wrapper.ReplaceNameWith(metadata, marks, generatedStateMachineClass, path);
                    if(result is Success<(ClassDecl.Class, MethodDecl.Method[]), Exception> success) {
                        return success.Value;
                    } else if(result is Error<(ClassDecl.Class, MethodDecl.Method[]), Exception> failure) {
                        throw failure.Message;
                    }
                }
            }
            return (null, new[] { method });
        }


        RootDecl.Declaration[] topLevel = 
            assembly.Declarations.Values
            .SelectMany(x => HandleDeclaration(x))
            .ToArray();

        return new Success<IL_Unit, Exception>(assembly with { Declarations = new ARRAY<Declaration>(topLevel) {
            Options = new ARRAY<Declaration>.ArrayOptions() {
                Delimiters = ('\0', '\n', '\0')
            }
        }});
    }

    public static Result<IL_Unit, Exception> Disassemble(string path)
        => Reader.Create<Reader>(path) switch {
            Success<Reader, Exception> reader =>
                reader.Value.Run() switch {
                    Success<IL_Unit, Exception> holder
                        => Modify(holder.Value),
                    Error<IL_Unit, Exception> error 
                        => Error<IL_Unit, Exception>.From(error.Message),
                    _ => throw new Exception("Unreachable code")
                },
            Error<Reader, Exception> error => Error<IL_Unit, Exception>.From(error.Message)
        };
}