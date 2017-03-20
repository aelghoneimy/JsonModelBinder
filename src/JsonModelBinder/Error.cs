namespace JsonModelBinder
{
    using System;

    public class Error
    {
        public ErrorKinds ErrorKind { get; internal set; } = ErrorKinds.ApplyToAll;
        public Type ErrorType { get; internal set; } = typeof(Exception);
        public string JsonName { get; internal set; } = string.Empty;
        public string JsonPath { get; internal set; } = string.Empty;
        public string Message { get; internal set; } = string.Empty;
        public string Name { get; internal set; } = string.Empty;
        public string Path { get; internal set; } = string.Empty;
    }
}