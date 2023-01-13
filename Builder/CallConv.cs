using System.Reflection;

public class CallConv {
    public string Name { get; set; }
    public string Indicator { get; set; }
    public string Type { get; set; }
    public Attribute[] Attributes { get; set; }
    public object? Value { get; set; }
    public string Code  { get; set; }
}