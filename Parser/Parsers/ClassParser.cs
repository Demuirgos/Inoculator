
using System.Collections.Generic;
using System.Linq;
using Inoculator.Parser.Models;
using Attribute = Inoculator.Parser.Models.Attribute;

namespace Inoculator.Parser.Core;

public class ClassParser {
    public static void ParseClass(ref int index, ref string[] code, out Class type)
    {
        type = new Class();

        void ParseModifiers(ref int index, ref string[] code, ref Class type)
        {
            index++;
            List<string> modifiers = new List<string>();
            while(code[index + 1] is not "extends" and not "implements" and not "{") {
                modifiers.Add(code[index]);
                index += 1;
            }
            type.Name = code[index++];
            if(code[index] == "{") {
                index++;
                return;
            }

            type.Modifiers = modifiers.ToArray();
            index++;
            type.BaseClass = code[index++];
            if(code[index] == "implements") {
                index += 1;
                List<string> interfaces = new List<string>();
                while(code[index] != "{") {
                    if(code[index] == ".custom") {
                        index += 1; int count = 0;
                        while(code[index] != ")" && count != 2) {
                            index += 1;
                            if(code[index] == ")") {
                                count += 1;
                            }
                        }
                        index += 1;
                    }
                    interfaces.Add(code[index++].Replace(",", string.Empty));
                }
                type.Interfaces = interfaces.ToArray();
            }
            index += 1;
        }

        void ParseClasses (ref int index, ref string[] code, ref Class type)
        {
            List<Class> typedefs = new List<Class>();
            while(code[index] == ".class") {
                var attributeResult = ClassParser.Parse(ref index, code);
                switch(attributeResult) {
                    case Success<Class, System.Exception> success:
                        typedefs.Add(success.Value);
                        break;
                    default : break;
                }
            }
            type.TypeDefs = typedefs.ToArray();
        }

        void ParseAttributes (ref int index, ref string[] code, ref Class type)
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
            type.Attributes = attributes.ToArray();
        }

        void ParseFields (ref int index, ref string[] code, ref Class type)
        {
            List<Field> fields = new List<Field>();
            while(code[index] == ".field") {
                var attributeResult = FieldParser.Parse(ref index, code);
                switch(attributeResult) {
                    case Success<Field, System.Exception> success:
                        fields.Add(success.Value);
                        break;
                    default : break;
                }
            }
            type.Fields = fields.ToArray();
        }

        void ParseMethods (ref int index, ref string[] code, ref Class type)
        {
            List<Method> methods = new List<Method>();
            while(code[index] == ".method") {
                var attributeResult = MethodParser.Parse(ref index, code);
                switch(attributeResult) {
                    case Success<Method, System.Exception> success:
                        methods.Add(success.Value);
                        break;
                    default : break;
                }
            }
            type.Methods = methods.ToArray();
        }

        void ParseProperties (ref int index, ref string[] code, ref Class type)
        {
            List<Property> properties = new List<Property>();
            while(code[index] == ".property") {
                var attributeResult = PropertyParser.Parse(ref index, code);
                switch(attributeResult) {
                    case Success<Property, System.Exception> success:
                        properties.Add(success.Value);
                        break;
                    default : break;
                }
            }
            type.Properties = properties.ToArray();
        }

        void ParseEvents (ref int index, ref string[] code, ref Class type)
        {
            List<Event> events = new List<Event>();
            while(code[index] == ".event") {
                var attributeResult = EventParser.Parse(ref index, code);
                switch(attributeResult) {
                    case Success<Event, System.Exception> success:
                        events.Add(success.Value);
                        break;
                    default : break;
                }
            }
            type.Events = events.ToArray();
        }

        if(code[index] == ".class") {
            ParseModifiers(ref index, ref code, ref type);
            ParseAttributes(ref index, ref code, ref type);
            ParseClasses(ref index, ref code, ref type);
            ParseFields(ref index, ref code, ref type);
            ParseMethods(ref index, ref code, ref type);
            ParseEvents(ref index, ref code, ref type);
            ParseProperties(ref index, ref code, ref type);
            index++;
        }

    }
    public static Result<Class, System.Exception> Parse(ref int i, string[] tokens)
    {
        var start = i;
        ParseClass(ref i, ref tokens, out Class type);
        if(type is null) {
            return Error<Class, System.Exception>.From(new System.Exception("Failed to parse class"));
        }
        type.Code = tokens[start..i].Join(" ");
        return Success<Class, System.Exception>.From(type);
    }
}
