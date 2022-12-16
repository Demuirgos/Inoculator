
using System.Collections.Generic;
using System.Linq;
using Inoculator.Parser.Models;
using Attribute = Inoculator.Parser.Models.Attribute;

namespace Inoculator.Parser.Core;

public class PropertyParser {
    public static void ParseProperty(ref int index, ref string[] code, out Property property)
    {
        property = new Property();

        void ParseModifiers(ref int index, ref string[] code, ref Property property)
        {
            List<string> modifiers = new List<string>();
            while(!code[index + 1].EndsWith("()")) 
            {
                modifiers.Add(code[index++]);
            }
            property.Modifiers = modifiers.ToArray();
            property.Type = code[index++];
            property.Name = code[index++].Replace("()", string.Empty);
        }

        bool IgnoreTokens(ref int index, ref string[] code, ref Property _)
        {
            while(code[index] != "{") {
                index += 1;
            }
            index++;
            return code[index] == "}";
        }

        void ParseGetter(ref int index, ref string[] code, ref Property property)
        {
            if(code[index] != ".get") {
                return;
            }
            while(code[index + 1] != ".set" && code[index + 1] != "}" ) {
                index += 1;
            }
            property.Getter = code[index].Substring(0, code[index].IndexOf("("));
            index++;
        }

        void ParseSetter(ref int index, ref string[] code, ref Property property)
        {
            if(code[index] != ".set") {
                return;
            }
            while(code[index - 1] != "void" ) {
                index += 1;
            }
            property.Setter = code[index].Substring(0, code[index].IndexOf("("));

            while(code[index++] != "}" ){}
        }

        void ParseAttributes (ref int index, ref string[] code, ref Property property)
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
            property.Attributes = attributes.ToArray();
        }

        if(code[index++] == ".property") {
            ParseModifiers(ref index, ref code, ref property);
            IgnoreTokens(ref index, ref code, ref property);
            ParseAttributes(ref index, ref code, ref property);
            ParseGetter(ref index, ref code, ref property);
            ParseSetter(ref index, ref code, ref property);
        } else property = null;

    }
    public static Result<Property, System.Exception> Parse(ref int i, string[] tokens)
    {
        var start = i;
        ParseProperty(ref i, ref tokens, out Property property);
        if(property is null) {
            return Error<Property, System.Exception>.From(new System.Exception());
        }
        property.Code = tokens[start..i].Join(" ");
        return Success<Property, System.Exception>.From(property);
    }
}