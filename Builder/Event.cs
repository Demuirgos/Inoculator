using System.Text.Json;

public class Event : Printable<Event> {
    public Attribute[] Attributes { get; set; } 
    public String[] Modifiers { get; set; } 
    public String Name { get; set; }
    public String Arguments { get; set; }
    public String Type { get; set; }
    public String Add { get; set; }
    public String Remove { get; set; }
    public String Fire { get; set; }
    public String Other { get; set; }
    public bool? IsStatic => !Modifiers?.Contains("instance");
    public string Code { get; set; }
}