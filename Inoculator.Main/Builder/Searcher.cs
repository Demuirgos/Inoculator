using System.Extensions;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using IdentifierDecl;
using Inoculator.Attributes;
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

    public static bool IsMarked(MethodDecl.Method method, List<string> interceptors, out string[] foundAttributes) {
        var attrs = method.Body.Items
            .Values.OfType<MethodDecl.CustomAttributeItem>()
            .Select(attr => attr.Value.AttributeCtor.Spec.ToString());
        foundAttributes = attrs.Where(attr => interceptors.Any(id => attr.Contains(id))).ToArray();

        return foundAttributes.Length > 0;
    }
    public static List<string> SearchForInterceptors(string currentPath)
    {
        var dlls = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.dll").Where(path => !path.EndsWith(currentPath));
        return dlls.SelectMany(dll => {
            var assembly = Assembly.LoadFrom(dll);
            var types = assembly.GetTypes();
            var interceptors = types.Where(x => x.IsSubclassOf(typeof(InterceptorAttribute)));
            return interceptors.Select(x => $"[{Path.GetFileNameWithoutExtension(dll)}] {x.FullName}"); // hack : space between file ref and name 
        }).ToList();
    }
}