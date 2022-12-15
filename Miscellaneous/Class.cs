using System.Reflection;

public class Class {
    public string Name { get; set; }
    public Method[] Methods { get; set; }
    public string[] Attributes { get; set; }
    public string[] Modifiers { get; set; }
    public string[] Body { get; set; }
}