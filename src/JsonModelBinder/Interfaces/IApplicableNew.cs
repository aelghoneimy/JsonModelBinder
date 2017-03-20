namespace JsonModelBinder.Interfaces
{
    using System.Threading.Tasks;

    public interface IApplicableNew
    {
        Task<object> ApplyNew(object model);
    }
}