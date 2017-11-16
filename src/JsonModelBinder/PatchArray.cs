namespace JsonModelBinder
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Threading.Tasks;
    using Newtonsoft.Json;
    using Converters;
    using Interfaces;
    using static PatchDocumentCache;

    [JsonConverter(typeof(PatchArrayJsonConverter))]
    public class PatchArray<T> : IPatchArray<T>
    {
        #region Private Properties

        private bool? _canCreate;
        private bool? _canPatch;
        private List<Error> _errorsCache;
        private IEnumerable<Error> _ownErrors = new List<Error>();

        #endregion

        #region Public Properties

        IPatchArrayDocument IPatchArray.this[string key] => this[key];

        public IPatchArrayDocument<T> this[string key] => Values.FirstOrDefault(x => x.Name == key);

        public int Count => Values.Count();

        public IEnumerable<Error> Errors
        {
            get
            {
                if (_errorsCache == null)
                {
                    var errors = new List<Error>(_ownErrors);

                    foreach (var value in Values)
                    {
                        foreach (var valueError in value.Errors)
                        {
                            var error = (value.PatchType == null || value.PatchType == PatchTypes.Add) && valueError.Name != "_patchType"
                                ? new Error
                                {
                                    ErrorType = valueError.ErrorType,
                                    JsonName = valueError.JsonName,
                                    JsonPath = valueError.JsonPath,
                                    Message = valueError.Message,
                                    Name = valueError.Name,
                                    Path = valueError.Path
                                }
                                : valueError;

                            errors.Add(error);
                        }
                    }

                    _errorsCache = errors;
                }

                return _errorsCache;
            }

            internal set { _ownErrors = value; }
        }

        public bool Found { get; internal set; }
        
        public bool HasValue => Values.Any();

        public bool IgnoreApply { get; set; }

        public IEnumerable<string> Keys => Values.Select(x => x.Name).ToList();

        public PatchKinds Kind => PatchKinds.Array;

        public string Name { get; internal set; } = string.Empty;

        public string JsonName { get; internal set; } = string.Empty;

        public string Path { get; internal set; } = string.Empty;

        public string JsonPath { get; internal set; } = string.Empty;

        IEnumerable<IPatchArrayDocument> IPatchArray.Values => Values;

        public IEnumerable<IPatchArrayDocument<T>> Values { get; internal set; } = new List<IPatchArrayDocument<T>>(0);

        #endregion

        #region Constructors

        internal PatchArray()
        {
        }

        #endregion

        #region Private Static Methods

        public static Expression GenerateFilterExpression(Type tSource, string propertyName, object value)
        {
            var typeQualifiedName = tSource.AssemblyQualifiedName;

            var parameter = ParameterExpressions[typeQualifiedName]
                ?? (ParameterExpressions[typeQualifiedName] = Expression.Parameter(tSource, "x"));

            var property = PropertyExpressions[typeQualifiedName, propertyName]
                ?? (PropertyExpressions[typeQualifiedName, propertyName] = Expression.Property(parameter, propertyName));

            //var propertyType = ((PropertyInfo)property.Member).PropertyType;
            Expression constant = Expression.Constant(value);
            constant = Expression.Convert(constant, typeof(object));
            //if (((ConstantExpression)constant).Type != propertyType)
            //{ constant = Expression.Convert(constant, propertyType); }

            return Expression.Call(property, ObjectEquals, constant);
        }

        private static TSource FirstOrDefault<TSource>(IEnumerable<TSource> source, IPatchArrayDocument patchPatchDocument)
        {
            var type = typeof(TSource);
            var typeQualifiedName = type.AssemblyQualifiedName;

            foreach (var key in patchPatchDocument.PatchKeys)
            {
                var propertyInfo = PropertyInfos[typeQualifiedName, key.Name]
                    ?? (PropertyInfos[typeQualifiedName, key.Name] = type.GetTypeInfo().GetProperty(key.Name));

                source = source.Where(x => propertyInfo.GetValue(x).Equals(key.Value));
            }

            return source.FirstOrDefault();
        }

        private static IEnumerable<TSource> LoadIEnumerableItems<TSource>(IEnumerable<TSource> source, IEnumerable<IPatchArrayDocument> patchDocuments)
        {
            var result = new List<TSource>();

            foreach (var patchDocument in patchDocuments)
            {
                var item = FirstOrDefault(source, patchDocument);

                if (item == null)
                {
                    throw new Exception($"Could find {nameof(TSource)} with Id(s) " +
                                        string.Join(", ", patchDocument.PatchKeys.Select(x => x.Value.ToString())));
                }

                result.Add(item);
            }

            return result;
        }

        private static async Task<IEnumerable<TSource>> LoadIQuerableItems<TSource>(IQueryable<TSource> source, IEnumerable<IPatchArrayDocument> patchDocuments)
        {
            Expression orPredicate = null;

            foreach (var patchDocument in patchDocuments)
            {
                Expression andPredicate = null;

                foreach (var patchValue in patchDocument.PatchKeys)
                {
                    var temp = GenerateFilterExpression(typeof(TSource), patchValue.Name, patchValue.Value);

                    andPredicate = andPredicate != null ? Expression.AndAlso(andPredicate, temp) : temp;
                }

                orPredicate = orPredicate != null ? Expression.OrElse(orPredicate, andPredicate) : andPredicate;
            }

            var parameter = ParameterExpressions[typeof(TSource).AssemblyQualifiedName]
                ?? (ParameterExpressions[typeof(TSource).AssemblyQualifiedName] = Expression.Parameter(typeof(TSource), "x"));

            var predicate = Expression.Lambda<Func<TSource, bool>>(orPredicate, parameter);

            var result = source.Where(predicate);
            var toListAsync = result.GetType().GetTypeInfo().GetMethod("ToListAsync");

            return toListAsync != null ? await (Task<IEnumerable<TSource>>)toListAsync.Invoke(result, null) : result.ToList();
        }

        #endregion

        #region Public Methods

        Task IPatchBase.Apply(object model)
        {
            if (!(model is IEnumerable<T>))
            {
                throw new ArgumentException($"{nameof(model)} must be of type {nameof(T)}");
            }

            return Apply((IEnumerable<T>)model);
        }

        public async Task Apply(IEnumerable<T> model)
        {
            if (!CanPatch())
            {
                throw new Exception("Apply not possible when document has errors.");
            }

            var arrayTypeInfo = model.GetType().GetTypeInfo();
            var modelType = typeof(T);

            if (!Values.Any())
            {
                if (Found)
                {
                    MethodInfo clearMethod;

                    if ((clearMethod = arrayTypeInfo.GetMethod(nameof(IList.Clear))) == null)
                    {
                        throw new NotSupportedException("Clear list is not supported.");
                    }

                    clearMethod.Invoke(model, null);
                }

                return;
            }

            //dynamic modelAsDynamic = model;
            MethodInfo addMethod = null;
            MethodInfo removeMethod = null;

            if (Values.Any(x => x.PatchType == PatchTypes.Add) &&
                (addMethod = arrayTypeInfo.GetMethod(nameof(IList.Add))) == null)
            {
                throw new NotSupportedException("Add to the list is not supported.");
            }

            if (Values.Any(x => x.PatchType == PatchTypes.Remove) &&
                (removeMethod = arrayTypeInfo.GetMethod(nameof(IList.Remove))) == null)
            {
                throw new NotSupportedException("Removing from the list is not supported.");
            }

            var documentsToPatch = Values
                .Where(x => x.PatchType == PatchTypes.Update || x.PatchType == PatchTypes.Remove)
                .ToList();

            var itemsToPatch = documentsToPatch.Any()
                ? (model is IQueryable
                    ? await LoadIQuerableItems((IQueryable<T>)model, documentsToPatch)
                    : LoadIEnumerableItems(model, documentsToPatch))
                : null;

            foreach (var patchDocument in Values)
            {
                if (patchDocument.PatchType == PatchTypes.Add)
                {
                    var newListItem = Activator.CreateInstance(modelType);

                    await patchDocument.Apply(newListItem);

                    // ReSharper disable once PossibleNullReferenceException
                    addMethod.Invoke(model, new[] { newListItem });
                }
                else
                {
                    var item = FirstOrDefault(itemsToPatch, patchDocument);

                    if (patchDocument.PatchType == PatchTypes.Update)
                    {
                        await patchDocument.Apply(item);
                    }
                    else if (patchDocument.PatchType == PatchTypes.Remove)
                    {
                        // ReSharper disable once PossibleNullReferenceException
                        removeMethod.Invoke(model, new object[] { item });
                    }
                }
            }
        }

        async Task<object> IApplicableNew.ApplyNew(object model)
        {
            return await ApplyNew((IEnumerable<T>)model);
        }

        public async Task<IEnumerable<T>> ApplyNew(IEnumerable<T> model)
        {
            if (!CanCreate())
            {
                throw new Exception("Apply not possible when document has errors.");
            }

            if (model == null) { model = new List<T>(); }


            var addMethod = model.GetType().GetMethod(nameof(IList.Add));

            if (addMethod == null)
            {
                throw new NotSupportedException("Add to the list is not supported.");
            }

            var modelType = typeof(T);

            foreach (var document in Values)
            {
                var newListItem = Activator.CreateInstance(modelType);

                await document.ApplyNew(newListItem);

                addMethod.Invoke(model, new[] { newListItem });
            }

            return model;
        }

        public bool CanCreate() => _canCreate ?? (_canCreate = !HasErrors(ErrorKinds.ApplyToCreate)).Value;
        public bool CanPatch()
        {
            return _canPatch
                ?? (_canPatch = _ownErrors.All(x => (x.ErrorKind & ErrorKinds.ApplyToUpdate) != ErrorKinds.ApplyToUpdate)
                    && this.All(x => x.PatchType == null || x.PatchType == PatchTypes.Add
                                        ? x.CanCreate()
                                        : x.PatchType == PatchTypes.Remove || x.CanPatch())).Value;
            
        }

        public bool Contains(string key) => Values.Any(x => x.Name == key);
        bool IPatchArray.Contains(IPatchArrayDocument item) => Contains(item.Name);
        public bool Contains(IPatchArrayDocument<T> item) => Contains(item.Name);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        IEnumerator<IPatchArrayDocument> IEnumerable<IPatchArrayDocument>.GetEnumerator() => GetEnumerator();
        public IEnumerator<IPatchArrayDocument<T>> GetEnumerator() => Values.GetEnumerator();

        public bool HasErrors() => Errors.Any();
        public bool HasErrors(ErrorKinds errorKind) => Errors.Any(x => (x.ErrorKind & errorKind) == errorKind);

        #endregion
    }
}