namespace JsonModelBinder
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using Newtonsoft.Json;
    using Converters;
    using Interfaces;

    [JsonConverter(typeof(PatchDocumentJsonConverter))]
    public class PatchDocument<T> : IPatchDocument<T>
    {
        #region Protected Properties

        protected bool? _CanCreate;
        protected bool? _CanPatch;
        protected List<Error> ErrorsCache;
        protected IEnumerable<Error> OwnErrors = new List<Error>();

        #endregion

        #region Public Properties

        public IPatchBase this[string key] => Values.FirstOrDefault(x => x.Name == key);

        public int Count => Values.Count();

        public virtual IEnumerable<Error> Errors
        {
            get
            {
                if (ErrorsCache == null)
                {
                    var errors = new List<Error>();

                    errors.AddRange(OwnErrors);
                    errors.AddRange(Values.SelectMany(x => x.Errors));
                    
                    ErrorsCache = errors;
                }

                return ErrorsCache;
            }

            internal set { OwnErrors = value; }
        }

        public bool Found => true;

        public bool IgnoreApply { get; set; }

        public virtual PatchKinds Kind => PatchKinds.Document;
        
        public bool HasValue => Values.Any();

        public IEnumerable<string> Keys => Values.Select(x => x.Name).ToList();

        public string Name { get; internal set; } = string.Empty;

        public string JsonName { get; internal set; } = string.Empty;

        public string Path { get; internal set; } = string.Empty;

        public string JsonPath { get; internal set; } = string.Empty;

        public IEnumerable<IPatchBase> Values { get; internal set; } = new List<IPatchBase>(0);

        #endregion

        #region Constructors

        internal PatchDocument()
        {
        }

        #endregion

        #region Private Methods
        
        private async Task _Apply(T model, bool isNew = false)
        {
            var hasErrors = !(isNew ? CanCreate() : CanPatch());

            if (hasErrors)
            {
                throw new Exception("Apply not possible when document has errors.");
            }

            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            var modelType = typeof(T).GetTypeInfo();

            foreach (var property in Values)
            {
                if (property.IgnoreApply) continue;

                if (property.Kind == PatchKinds.Primitive)
                {
                    await property.Apply(model);
                }
                else
                {
                    var propertyInfo = modelType.GetProperty(property.Name);
                    var propertyValue = propertyInfo.GetValue(model) ?? Activator.CreateInstance(propertyInfo.PropertyType);

                    await (isNew ? ((IApplicableNew)property).ApplyNew(propertyValue) : property.Apply(propertyValue));
                }
            }
        }
        
        #endregion

        #region Public Methods

        Task IPatchBase.Apply(object model)
        {
            if (!(model is T))
            {
                throw new ArgumentException($"{nameof(model)} must be of type {nameof(T)}");
            }

            return Apply((T)model);
        }

        public virtual async Task Apply(T model)
        {
            await _Apply(model);
        }

        async Task<object> IApplicableNew.ApplyNew(object model)
        {
            if (!(model is T))
            {
                throw new ArgumentException($"{nameof(model)} must be of type {nameof(T)}");
            }

            await ApplyNew((T)model);

            return model;
        }

        public virtual async Task<T> ApplyNew(T model)
        {
            if (model == null) { model = Activator.CreateInstance<T>(); }

            await _Apply(model, true);

            return model;
        }

        public bool CanCreate() => _CanCreate ?? (_CanCreate = !HasErrors(ErrorKinds.ApplyToCreate)).Value;
        public bool CanPatch()  => _CanPatch  ?? (_CanPatch  = !HasErrors(ErrorKinds.ApplyToUpdate)).Value;

        public bool Contains(string key) => Values.Any(x => x.Name == key);
        public bool Contains(IPatchBase item) => Contains(item.Name);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public IEnumerator<IPatchBase> GetEnumerator() => Values.GetEnumerator();

        public virtual bool HasErrors() => Errors.Any();
        public virtual bool HasErrors(ErrorKinds errorKind) => Errors.Any(x => (x.ErrorKind & errorKind) == errorKind);

        #endregion
    }
}