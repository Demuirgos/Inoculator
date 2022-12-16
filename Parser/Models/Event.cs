using System;
using System.Linq;

namespace Inoculator.Parser.Models;
public class Event : Printable<Event> {
    public Attribute[] Attributes { get; set; } 
    public String[] Modifiers { get; set; } 
    public String Name { get; set; }
    public String Type { get; set; }
    public String Adder { get; set; }
    public String Remover { get; set; }
    public bool? IsStatic => !Modifiers?.Contains("instance");
    public string Code { get; set; }
}