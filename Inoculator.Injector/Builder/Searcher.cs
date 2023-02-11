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

    public static bool IsMarked(MethodDecl.Method method, List<string> interceptors, List<string> rewriters, out string[] foundInterceptors, out string foundRewriter) {
        var attrs = method.Body.Items
            .Values.OfType<MethodDecl.CustomAttributeItem>()
            .Select(attr => attr.Value.AttributeCtor.Spec.ToString().Trim());
        foundInterceptors = attrs.Where(attr => interceptors.Any(id => attr.Contains(id)))
            .Select(fullname => fullname.StartsWith("class") ? fullname.Substring(6) : fullname)
            .ToArray();
        var foundRewriters = attrs.Where(attr => rewriters.Any(id => attr.Contains(id)))
            .Select(fullname => fullname.StartsWith("class") ? fullname.Substring(6) : fullname);
        foundRewriter = foundRewriters.FirstOrDefault();
        return foundInterceptors.Length > 0 || foundRewriters.Count() == 1;
    }
    public static List<string>  SearchForInterceptors(Declaration.Collection ilfile)
    {
        var toplevel = ilfile
            .Declarations.Values
            .OfType<ClassDecl.Class>()
            .Where(type => {
                var inheritedType = new String(type.Header.Extends?.Type.ToString().Where(c => !Char.IsWhiteSpace(c)).ToArray());
                return inheritedType == "[Inoculator.Injector]Inoculator.Attributes.InterceptorAttribute";
            });
        return toplevel.Select(x => x.Header.Id.ToString()).ToList();
    }
    public static List<string> SearchForInterceptors(string currentPath, Declaration.Collection ilfile)
    {
        IEnumerable<string> handlePath(string path) {
            if(!path.EndsWith(currentPath)) {
                var assembly = Assembly.LoadFrom(path);
                var types = assembly.GetTypes();
                var interceptors = types.Where(x => x.IsSubclassOf(typeof(InterceptorAttribute)));
                return interceptors.Select(x => $"[{Path.GetFileNameWithoutExtension(path)}] {x.FullName}"); // hack : space between file ref and name 
            } else {
                var toplevel = ilfile
                    .Declarations.Values
                    .OfType<ClassDecl.Class>()
                    .Where(type => {
                        var inheritedType = new String(type.Header.Extends?.Type.ToString().Where(c => !Char.IsWhiteSpace(c)).ToArray());
                        return inheritedType == "[Inoculator.Interceptors]Inoculator.Attributes.InterceptorAttribute";
                    });
                return toplevel.Select(x => x.Header.Id.ToString().Trim());
            }
        }

        return Directory.GetFiles(Directory.GetCurrentDirectory(), "*.dll").SelectMany(handlePath).ToList();
    }

    public static List<string> SearchForRewriters(string currentPath, Declaration.Collection ilfile)
    {
        IEnumerable<string> handlePath(string path) {
            if(!path.EndsWith(currentPath)) {
                var assembly = Assembly.LoadFrom(path);
                var types = assembly.GetTypes();
                var interceptors = types.Where(x => x.IsSubclassOf(typeof(RewriterAttribute)));
                return interceptors.Select(x => $"[{Path.GetFileNameWithoutExtension(path)}] {x.FullName}"); // hack : space between file ref and name 
            } else {
                var toplevel = ilfile
                    .Declarations.Values
                    .OfType<ClassDecl.Class>()
                    .Where(type => {
                        var inheritedType = new String(type.Header.Extends?.Type.ToString().Where(c => !Char.IsWhiteSpace(c)).ToArray());
                        return inheritedType == "[Inoculator.Interceptors]Inoculator.Attributes.RewriterAttribute";
                    });
                return toplevel.Select(x => x.Header.Id.ToString().Trim());
            }
        }

        return Directory.GetFiles(Directory.GetCurrentDirectory(), "*.dll").SelectMany(handlePath).ToList();
    }
}