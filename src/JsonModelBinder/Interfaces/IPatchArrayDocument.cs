namespace JsonModelBinder.Interfaces
{
    using System.Collections.Generic;

    public interface IPatchArrayDocument : IPatchDocument
    {
        IEnumerable<IPatchPrimitive> PatchKeys { get; }

        PatchTypes? PatchType { get; }
    }

    public interface IPatchArrayDocument<T> : IPatchDocument<T>, IPatchArrayDocument
    {
    }
}