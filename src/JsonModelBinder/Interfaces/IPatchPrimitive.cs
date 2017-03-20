namespace JsonModelBinder.Interfaces
{
    public interface IPatchPrimitive : IPatchBase
    {
        object Value { get; }
    }
    
    public interface IPatchPrimitive<out T> : IPatchPrimitive
    {
        new T Value { get; }
    }
}