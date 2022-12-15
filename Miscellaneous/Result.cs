namespace System.Extensions;

public record Result<T, V>();
public record Error<T, V>(V Message) : Result<T, V> {
    public static Error<T, V> From(V message) => new(message);
}
public record Success<T, V>(T Value) : Result<T, V> {
    public static Success<T, V> From(T Value) => new(Value);
}
