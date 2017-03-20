namespace JsonModelBinder.Converters
{
    using System;
    using System.Collections;
    using System.ComponentModel.DataAnnotations;
    using System.Linq;
    using System.Reflection;
    using Interfaces;
    using Newtonsoft.Json;
    using Properties;

    public class PatchArrayJsonConverter : JsonConverter
    {
        #region Private Properties

        private Type _genericType;

        #endregion

        #region Public Methods

        public override bool CanConvert(Type objectType)
        {
            return objectType.GetTypeInfo().GetInterfaces().Contains(typeof(IPatchArray));
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            try
            {
                if (reader.TokenType == JsonToken.Null)
                {
                    throw new InvalidCastException();
                }

                if (_genericType == null)
                {
                    _genericType = objectType.GetTypeInfo().GenericTypeArguments.FirstOrDefault() ?? typeof(object);
                }

                var validationAttributes = _genericType.GetTypeInfo().GetCustomAttributes<ValidationAttribute>();

                return JsonConverterExtensions.ReadArrayMethodInfo
                    .MakeGenericMethod(_genericType)
                    .Invoke(null, new object[] { reader, "", "", "", validationAttributes });
            }
            catch
            {
                var patchArray = typeof(PatchArray<>).CreateGenericInstance<IPatchArray>(_genericType, true);
                ((IList)patchArray.Errors).Add(new Error
                {
                    ErrorType = typeof(JsonReaderException),
                    Message = Resources.JsonReaderException
                });

                return patchArray;
            }
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
