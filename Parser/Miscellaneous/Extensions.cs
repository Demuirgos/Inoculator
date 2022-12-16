public static class TypeExtensions {
    public static string Join(this string[] types, string separator) {
        return string.Join(separator, types);
    }
}
