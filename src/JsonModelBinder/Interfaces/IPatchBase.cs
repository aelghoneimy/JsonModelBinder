namespace JsonModelBinder.Interfaces
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public interface IPatchBase
    {
        IEnumerable<Error> Errors { get; }

        bool Found { get; }

        bool HasValue { get; }

        bool IgnoreApply { get; set; }

        PatchKinds Kind { get; }

        string Name { get; }

        string JsonName { get; }

        string Path { get; }

        string JsonPath { get; }

        Task Apply(object model);

        bool CanCreate();

        bool CanPatch();

        bool HasErrors();

        bool HasErrors(ErrorKinds errorKind);
    }
}