namespace JsonModelBinder
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using Interfaces;
    using static PatchDocumentCache;

    public class PatchPrimitive<T> : IPatchPrimitive<T>
    {
        #region Private Static Properties

        private static readonly Task EmptyTask = Task.FromResult(0);

        #endregion

        #region Private Properties

        private bool? _canCreate;
        private bool? _canPatch;

        #endregion

        #region Public Properties

        public IEnumerable<Error> Errors { get; internal set; }

        public bool Found { get; internal set; }
        
        public bool HasValue => Value != null;

        public bool IgnoreApply { get; set; }

        public PatchKinds Kind => PatchKinds.Primitive;

        public string Name { get; internal set; } = string.Empty;

        public string JsonName { get; internal set; } = string.Empty;

        public string Path { get; internal set; } = string.Empty;

        public string JsonPath { get; internal set; } = string.Empty;
        
        object IPatchPrimitive.Value => Value;
        
        public T Value { get; internal set; }

        #endregion

        #region Constructors

        internal PatchPrimitive()
        {
        }

        #endregion

        #region Public Methods

        public Task Apply(object model)
        {
            var modelType = model.GetType();
            var assemblyQualifiedName = modelType.AssemblyQualifiedName;
            var propertyInfo = PropertyInfos[assemblyQualifiedName, Name]
                ?? modelType.GetTypeInfo().GetProperty(Name);
            
            propertyInfo.SetValue(model, Value);

            return EmptyTask;
        }

        public bool CanCreate() => _canCreate ?? (_canCreate = !HasErrors(ErrorKinds.ApplyToCreate)).Value;
        public bool CanPatch()  => _canPatch  ?? (_canPatch  = !HasErrors(ErrorKinds.ApplyToUpdate)).Value;

        public bool HasErrors() => Errors.Any();
        public bool HasErrors(ErrorKinds errorKind) => Errors.Any(x => (x.ErrorKind & errorKind) == errorKind);

        #endregion

        #region Operators

        public static explicit operator T(PatchPrimitive<T> patchPrimitive)
        {
            return patchPrimitive.Value;
        }

        #endregion
    }
}