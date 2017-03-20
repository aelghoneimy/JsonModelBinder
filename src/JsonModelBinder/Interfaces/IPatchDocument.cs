namespace JsonModelBinder.Interfaces
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public interface IPatchDocument : IPatchBase, IApplicableNew, IEnumerable<IPatchBase>
    {
        IPatchBase this[string key] { get; }

        int Count { get; }
        IEnumerable<string> Keys { get; }
        IEnumerable<IPatchBase> Values { get; }
        
        bool Contains(string key);
        bool Contains(IPatchBase item);
    }

    public interface IPatchDocument<T> : IPatchDocument
    {
        Task Apply(T model);
        Task<T> ApplyNew(T model);
    }
}