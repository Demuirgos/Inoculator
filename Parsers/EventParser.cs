
using System;
using System.Collections.Generic;
using System.Linq;
using System.Extensions;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.ComponentModel;

namespace Inoculator.Core;

public class EventParser {
    public static void ParseEvent(ref int index, ref string[] code, out Event eventProp)
    {
        eventProp = new Event();

        void ParseModifiers(ref int index, ref string[] code, ref Event eventProp)
        {
            List<String> modifiers = new List<String>();
            while(code[index + 2] != "{") 
            {
                modifiers.Add(code[index++]);
            }
            eventProp.Modifiers = modifiers.ToArray();
            eventProp.Type = code[index++];
            eventProp.Name = code[index++];
            index++;
        }

        void ParseAdder(ref int index, ref string[] code, ref Event eventProp)
        {
            if(code[index] != ".addon") {
                return;
            }

            if(code[index + 1] == "instance") {
                eventProp.Modifiers = new string[] { "instance" };
                index++;
            }
            index+=2;
            eventProp.Adder = code[index].Substring(0, code[index].IndexOf("("));
            index+=2;
        }

        void ParseRemover(ref int index, ref string[] code, ref Event eventProp)
        {
            if(code[index] != ".removeon") {
                return;
            }

            if(code[index + 1] == "instance") {
                eventProp.Modifiers = new string[] { "instance" };
                index++;
            }
            index+=2;
            eventProp.Remover = code[index].Substring(0, code[index].IndexOf("("));
            index+=3;
        }

        void ParseAttributes (ref int index, ref string[] code, ref Event eventProp)
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
            eventProp.Attributes = attributes.ToArray();
        }

        if(code[index++] == ".event") {
            ParseModifiers(ref index, ref code, ref eventProp);
            ParseAttributes(ref index, ref code, ref eventProp);
            ParseAdder(ref index, ref code, ref eventProp);
            ParseRemover(ref index, ref code, ref eventProp);
        } else eventProp = null;

    }
    public static Result<Event, Exception> Parse(ref int i, string[] tokens)
    {
        ParseEvent(ref i, ref tokens, out Event eventProp);
        if(eventProp == null) {
            return Error<Event, Exception>.From(new Exception("Failed to parse event"));
        }
        return Success<Event, Exception>.From(eventProp);
    }
}