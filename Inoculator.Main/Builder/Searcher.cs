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
    public static List<MethodData> SearchForMethods(Declaration.Collection ilfile, Predicate<MethodData> predicate = null)
    {
        static IEnumerable<MethodData> SearchForMethods(ClassDecl.Class type)
        {
            var nestedTypes = type.Members.Members.Values.OfType<ClassDecl.NestedClass>();
            var result = nestedTypes.SelectMany(c => SearchForMethods(c.Value)).ToList();

            var methods = type.Members.Members.Values.OfType<ClassDecl.MethodDefinition>();
            var metadata = methods.Select(x => new MethodData(x.Value)
            {
                ClassReference = type.Header
            });
            result.AddRange(metadata);
            return result;
        }

        var toplevel = ilfile.Declarations.Values.OfType<ClassDecl.Class>().SelectMany(SearchForMethods);
        return toplevel.Where(x => predicate?.Invoke(x) ?? true).ToList();
    }

    public static bool IsMarked(MethodDecl.Method method, List<IdentifierDecl.Identifier> targets, out string[] foundAttributes) {
        var attrs = method.Body.Items
            .Values.OfType<MethodDecl.CustomAttributeItem>()
            .Select(attr => attr.Value.AttributeCtor.Spec.ToString());
        var interceptors = targets.Select(x => x.ToString());
        // TODO : Handle multiple interceptors
        foundAttributes = attrs.Where(attr => interceptors.Any(id => attr.Contains(id))).ToArray();

        return foundAttributes.Length > 0;
    }

    public static List<IdentifierDecl.Identifier> SearchForInterceptors(Declaration.Collection ilfile)
    {
        var toplevel = ilfile
            .Declarations.Values
            .OfType<ClassDecl.Class>()
            .Where(type => {
                var inheritedType = new String(type.Header.Extends?.Type.ToString().Where(c => !Char.IsWhiteSpace(c)).ToArray());
                return inheritedType == "[Inoculator.Injector]Inoculator.Attributes.InterceptorAttribute";
            });
        return toplevel.Select(x => x.Header.Id).ToList();
    }
}