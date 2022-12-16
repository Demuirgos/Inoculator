using System;

namespace Inoculator.Parser.Models;
public class Attribute : Printable<Attribute> {
    public String[] Modifiers { get; set; } 
    public String Name { get; set; }
    public String Constructor { get; set; }
    public string[] Bytes { get; set; }
    public string Code { get; set; }
}