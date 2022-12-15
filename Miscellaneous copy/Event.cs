using System.Text.Json;

public class Event : Printable<Event> {
    public String[] Attributes { get; set; } 
    public String[] Modifiers { get; set; } 
    public String Name { get; set; }
    public String Type { get; set; }
    public String Adder { get; set; }
    public String Remover { get; set; }
    public bool? IsStatic => !Modifiers?.Contains("instance");
}