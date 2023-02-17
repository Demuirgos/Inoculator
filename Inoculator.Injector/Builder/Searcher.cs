using System.Extensions;
using System.Reflection;
using System.Runtime.Loader;
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

    public static bool IsMarked(MethodDecl.Method method, List<InterceptorData> modifiers, out InterceptorData[] foundFlags) {
        var attrs = method.Body.Items
            .Values.OfType<MethodDecl.CustomAttributeItem>()
            .Select(attr => attr.Value.AttributeCtor.Spec.ToString().Trim());
        foundFlags = attrs.Select(attr => (attr, modifiers.Where(flag => attr.Contains(flag.ClassName)).FirstOrDefault()))
            .Where(pair => pair.Item2 != null)
            .Select(pair => {
                pair.Item2.ClassName = pair.Item1.StartsWith("class") ? pair.Item1.Substring(6) : pair.Item1;
                return pair.Item2;
            })
            .ToArray();

        if(foundFlags.Count(interceptor => interceptor.IsRewriter) > 1) {
            throw new Exception("Multiple rewriters found on method " + method.Header.Name.ToString());
        }
        return foundFlags.Count(interceptor => interceptor.IsInterceptor) > 0 || foundFlags.Count(interceptor => interceptor.IsRewriter) == 1;
    }

    public static List<InterceptorData> SearchForModifiers(string currentPath)
    {
        var ctx = new AssemblyLoadContext("Inoculator.Temporary.Rewriters", true);
        try {
            return Directory.GetFiles(Directory.GetCurrentDirectory(), "*.dll").Where(path => !path.EndsWith(currentPath)).SelectMany(path => {
                Assembly? assembly = Assembly.LoadFrom(path);
                var types = assembly.GetTypes();
                var interceptors = types.Where(x => (x.IsAssignableTo(typeof(IRewriter)) || x.IsAssignableTo(typeof(IInterceptor))) && x.IsSubclassOf(typeof(System.Attribute)));
                return interceptors.Select(typedata => new { 
                    Name = $"[{Path.GetFileNameWithoutExtension(path)}] {typedata.FullName}",
                    Type = typedata
                }).Select(result => new InterceptorData {
                    ClassName = result.Name,
                    IsInterceptor = result.Type.IsAssignableTo(typeof(IInterceptor)),
                    IsRewriter = result.Type.IsAssignableTo(typeof(IRewriter))
                }).ToList();
            }).ToList();
        } finally {
            ctx.Unload();
        }
    }
}