
using System.Collections.Generic;
using Inoculator.Parser.Models;
using Attribute = Inoculator.Parser.Models.Attribute;

namespace Inoculator.Parser.Core;

public class AssemblyParser {
    public static void ParseAssembly(ref int index, ref string[] code, out Assembly module)
    {
        module = new Assembly();

        void ParseModifiers(ref int index, ref string[] code, ref Assembly module)
        {
            module.Name =code[++index];
            index+=2;
        }

        void ParseClasses (ref int index, ref string[] code, ref Assembly module)
        {
            List<Class> typedefs = new List<Class>();
            while(code[index] == ".class") {
                
                var attributeResult = ClassParser.Parse(ref index, code);
                switch(attributeResult) {
                    case Success<Class, System.Exception> success:
                        typedefs.Add(success.Value);
                        break;
                    default : 
                        break;
                }
                if(index >= code.Length) {
                    break;
                }
            }
            module.Classes = typedefs.ToArray();
        }

        void IgnoreTokens(ref int index, ref string[] code, ref Assembly _)
        {
            while(code[index] != ".class") {
                index += 1;
            }
            return;
        }

        void ParseAttributes (ref int index, ref string[] code, ref Assembly module)
        {
            List<Attribute> attributes = new List<Attribute>();
            while(code[index] == ".custom") {
                var attributeResult = AttributeParser.Parse(ref index, code);
                switch(attributeResult) {
                    case Success<Attribute, System.Exception> success:
                        attributes.Add(success.Value);
                        break;
                    default : break;
                }
            }
            module.Attributes = attributes.ToArray();
        }

        

        if(code[index] == ".assembly") {
            ParseModifiers(ref index, ref code, ref module);
            ParseAttributes(ref index, ref code, ref module);
            IgnoreTokens(ref index, ref code, ref module);
            ParseClasses(ref index, ref code, ref module);
        }

    }
    public static Result<Assembly, System.Exception> Parse(string code)
    {
        int i = 0;
        var tokens = code.Split(
            new char[] { ' ', '\n', '\r' }, 
            System.StringSplitOptions.RemoveEmptyEntries | System.StringSplitOptions.TrimEntries
        );

        ParseAssembly(ref i, ref tokens, out Assembly module);
        if(module == null) {
            return Error<Assembly, System.Exception>.From(new System.Exception("Failed to parse assembly"));
        }
        module.Code = code;
        return Success<Assembly, System.Exception>.From(module);
    }
}