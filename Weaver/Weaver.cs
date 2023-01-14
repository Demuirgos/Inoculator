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
                case MethodDecl.Method method:
                    return HandleMethod(method, null);
                case ClassDecl.Class type:
                    return HandleClass(type);
                default:
                    return new[] { declaration };
            }
        }

        ClassDecl.Class[] HandleClass(ClassDecl.Class @class) {
            ClassDecl.Member[] HandleMember(ClassDecl.Member member){
                switch(member) {
                    case ClassDecl.MethodDefinition method:
                        return HandleMethod(method.Value, @class.Header.Id).Select(x => new ClassDecl.MethodDefinition(x)).ToArray();
                    case ClassDecl.NestedClass type:
                        return HandleClass(type.Value).Select(x => new ClassDecl.NestedClass(x)).ToArray();
                    default:
                        return new[] { member };
                }
            } 

            var newMembers = @class.Members.Members.Values
                .SelectMany(x => HandleMember(x))
                .ToArray();

            return new[] { @class with { Members = @class.Members with { Members = new ARRAY<ClassDecl.Member>(newMembers) {
                Delimiters = ('\0', '\n', '\0')
            } } } };
        }

        MethodDecl.Method[] HandleMethod(MethodDecl.Method method, IdentifierDecl.Identifier parent) {
            var metadata = new Metadata(method) {
                ClassName = parent
            };
            if(!metadata.Code.IsConstructor && Searcher.IsMarked(metadata.Code, targetAttributes)) {
                var result = metadata.ReplaceNameWith($"{metadata.Name}__Inoculated");
                if(result is Success<MethodDecl.Method[], Exception> success) {
                    return success.Value;
                }
            }
            return new[] { method };
        }


        RootDecl.Declaration[] topLevel = 
            assembly.Declarations.Values
            .SelectMany(x => HandleDeclaration(x))
            .ToArray();

        return new Success<IL_Unit, Exception>(assembly with { Declarations = new ARRAY<Declaration>(topLevel) {
            Delimiters = ('\0', '\n', '\0')
        } });
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