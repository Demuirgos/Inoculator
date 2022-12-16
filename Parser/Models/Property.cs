using System;
using System.Linq;

namespace Inoculator.Parser.Models;
public class Property : Printable<Property> {
    public Attribute[] Attributes { get; set; } 
    public String[] Modifiers { get; set; } 
    public String Name { get; set; }
    public String Type { get; set; }
    public String Getter { get; set; }
    public String Setter { get; set; }
    public bool? IsStatic => !Modifiers?.Contains("instance");
    public string Code { get; set; }
}