using System.Reflection;

public class Argument {
    public string Name { get; set; }
    public string Type { get; set; }
    public string[] Attributes { get; set; }
    public object? Value { get; set; }
}