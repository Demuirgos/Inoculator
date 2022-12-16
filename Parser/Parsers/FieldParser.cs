
using System.Collections.Generic;
using System.Linq;
using Inoculator.Parser.Models;
using Attribute = Inoculator.Parser.Models.Attribute;

namespace Inoculator.Parser.Core;

public class FieldParser {
    public static void ParseField(ref int index, ref string[] code, out Field field)
    {
        field = new Field();

        void ParseModifiers(ref int index, ref string[] code, ref Field field)
        {
            List<string> modifiers = new List<string>();
            bool NextBlock(ref string[] code, ref int index, int offset) 
                => code[index + offset] == ".custom" || code[index + offset] == ".field" 
                || code[index + offset] == ".method" || code[index + offset] == ".property" 
                || code[index + offset] == ".event"  || code[index + offset] == "}";
            
            while(index + 2 < code.Length - 1   && code[index + 2] != "="
                && !NextBlock(ref code, ref index, 2)) 
            {
                modifiers.Add(code[index++]);
            }

            field.Modifiers = modifiers.ToArray();
            field.Type = code[index++];
            field.Name = code[index++];
            if(code[index] == "=") {
                while (!NextBlock(ref code, ref index, 0)) index++;
            }
        }

        void ParseAttributes (ref int index, ref string[] code, ref Field field)
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
            field.Attributes = attributes.ToArray();
        }

        if(code[index++] == ".field") {
            ParseModifiers(ref index, ref code, ref field);
            ParseAttributes(ref index, ref code, ref field);
        } else field = null;

    }
    public static Result<Field, System.Exception> Parse(ref int i, string[] tokens)
    {
        var start = i;
        ParseField(ref i, ref tokens, out Field field);
        if(field is null) {
            return Error<Field, System.Exception>.From(new System.Exception("Failed to parse field"));
        }
        field.Code = tokens[start..i].Join(" ");
        return Success<Field, System.Exception>.From(field);
    }
}