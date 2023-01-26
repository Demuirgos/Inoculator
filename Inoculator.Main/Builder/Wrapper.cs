using System.Extensions;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using IdentifierDecl;
using Inoculator.Core;
using LabelDecl;
using MethodDecl;
using static Dove.Core.Parser;
using static Inoculator.Builder.HandlerTools; 
namespace Inoculator.Builder;

public static class Wrapper {
    public static Result<(ClassDecl.Class, MethodDecl.Method[]), Exception> ReplaceNameWith(MethodData metadata, string[] attributeNames, ClassDecl.Class classRef = null, IEnumerable<string> path = null) {
        switch (metadata.MethodBehaviour)
        {
            case MethodData.MethodType.Sync:
                return SyncRewriter.Rewrite(metadata, attributeNames);
            case MethodData.MethodType.Iter:
                return EnumRewriter.Rewrite(classRef, metadata, attributeNames, path);
            case MethodData.MethodType.Async:
                return AsyncRewriter.Rewrite(classRef, metadata, attributeNames, path);
        }
        return Success<(ClassDecl.Class, MethodDecl.Method[]), Exception>.From((classRef, new[] { metadata.Code }));
    }
}