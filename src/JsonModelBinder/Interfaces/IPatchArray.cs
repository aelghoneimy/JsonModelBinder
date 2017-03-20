namespace JsonModelBinder.Interfaces
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public interface IPatchArray : IPatchBase, IApplicableNew, IEnumerable<IPatchArrayDocument>
    {
        IPatchArrayDocument this[string key] { get; }

        int Count { get; }
        IEnumerable<string> Keys { get; }
        IEnumerable<IPatchArrayDocument> Values { get; }
        
        bool Contains(string key);
        bool Contains(IPatchArrayDocument item);
    }

    public interface IPatchArray<T> : IPatchArray
    {
        new IPatchArrayDocument<T> this[string key] { get; }

        new IEnumerable<IPatchArrayDocument<T>> Values { get; }
        
        Task Apply(IEnumerable<T> model);
        Task<IEnumerable<T>> ApplyNew(IEnumerable<T> model);
        bool Contains(IPatchArrayDocument<T> item);
        new IEnumerator<IPatchArrayDocument<T>> GetEnumerator();
    }
}