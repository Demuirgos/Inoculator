using System.Extensions;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using IdentifierDecl;
using Inoculator.Core;
using MethodDecl;
using RootDecl;

namespace Inoculator.Builder;

public static class Searcher {
    public static List<Metadata> SearchForMethods(Declaration.Collection ilfile, Predicate<Metadata> predicate = null)
    {
        static IEnumerable<Metadata> SearchForMethods(ClassDecl.Class type)
        {
            var nestedTypes = type.Members.Members.Values.OfType<ClassDecl.NestedClass>();
            var result = nestedTypes.SelectMany(c => SearchForMethods(c.Value)).ToList();

            var methods = type.Members.Members.Values.OfType<ClassDecl.MethodDefinition>();
            var metadata = methods.Select(x => new Metadata(x.Value)
            {
                ClassName = type.Header.Id
            });
            result.AddRange(metadata);
            return result;
        }

        var toplevel = ilfile.Declarations.Values.OfType<ClassDecl.Class>().SelectMany(SearchForMethods);
        return toplevel.Where(x => predicate?.Invoke(x) ?? true).ToList();
    }

    public static bool IsMarked(MethodDecl.Method method, List<IdentifierDecl.Identifier> targets) {
        var attrs = method.Body.Items
            .Values.OfType<MethodDecl.CustomAttributeItem>()
            .Select(attr => attr.Value.AttributeCtor.Spec.ToString().Replace("\0", ""));
        var interceptors = targets.Select(x => x.ToString().Replace("\0", ""));
        return attrs.Any(attr => interceptors.Any(id => attr.Contains(id)));
    }

    public static List<IdentifierDecl.Identifier> SearchForInterceptors(Declaration.Collection ilfile)
    {
        var toplevel = ilfile
            .Declarations.Values
            .OfType<ClassDecl.Class>()
            .Where(type => {
                var inheritedType = new String(type.Header.Extends?.Type.ToString().Where(c => !Char.IsWhiteSpace(c) && c != '\0').ToArray());
                return inheritedType == "[Inoculator.Injector]Inoculator.Attributes.InterceptorAttribute";
            });
        return toplevel.Select(x => x.Header.Id).ToList();
    }
}