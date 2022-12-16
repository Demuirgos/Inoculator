using System;

public record Result<T, V>() ;
public record Error<T, V>(V Message) : Result<T, V> {
    public static Error<T, V> From(V message) => new(message);
}
public record Success<T, V>(T Value) : Result<T, V> {
    public static Success<T, V> From(T Value) => new(Value);
}

public static class ResultExtensions {
    public static Result<U, V> Map<T, V, U>(this Result<T, V> result, Func<T, U> f) {
        return result switch {
            Success<T, V> success => Success<U, V>.From(f(success.Value)),
            Error<T, V> error => Error<U, V>.From(error.Message),
            _ => throw new Exception("Invalid result type")
        };
    }
    
    public static Result<U, V> Bind<T, V, U>(this Result<T, V> result, Func<T, Result<U, V>> f) {
        return result switch {
            Success<T, V> success => f(success.Value),
            Error<T, V> error => Error<U, V>.From(error.Message),
            _ => throw new Exception("Invalid result type")
        };
    }
}
