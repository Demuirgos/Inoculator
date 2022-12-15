
using System;
using System.Collections.Generic;
using System.Linq;
using System.Extensions;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.ComponentModel;

namespace Inoculator.Core;

public class PropertyParser {
    public static void ParseProperty(ref int index, ref string[] code, out Property property)
    {
        property = new Property();

        void ParseModifiers(ref int index, ref string[] code, ref Property property)
        {
            List<String> modifiers = new List<String>();
            while(!code[index + 1].EndsWith("()")) 
            {
                modifiers.Add(code[index++]);
            }
            property.Modifiers = modifiers.ToArray();
            property.Type = code[index++];
            property.Name = code[index++].Replace("()", String.Empty);
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
            while(code[index + 1] != "}" ) {
                index += 1;
            }
            var token = code[index];
            property.Setter = code[index].Substring(0, code[index].IndexOf("("));
            index++;
        }

        void ParseAttributes (ref int index, ref string[] code, ref Property property)
        {
            List<String> attributes = new List<String>();
            while(code[index] == ".custom") {
                var attributeResult = AttributeParser.Parse(ref index, code);
                switch(attributeResult) {
                    case Success<String, Exception> success:
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
    public static Result<Property, Exception> Parse(ref int i, string[] tokens)
    {
        ParseProperty(ref i, ref tokens, out Property property);
        if(property == null) {
            return Error<Property, Exception>.From(new Exception());
        }
        return Success<Property, Exception>.From(property);
    }
}