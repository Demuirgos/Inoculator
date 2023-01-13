using System.Text.Json;

public class Attribute : Printable<Attribute> {
    public string[] Target { get; set; }
    public String[] Modifiers { get; set; } 
    public String Name { get; set; }
    public String Constructor { get; set; }
    public string[] Bytes { get; set; }
    public string Code { get; set; }
}