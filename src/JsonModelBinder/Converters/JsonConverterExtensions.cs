namespace JsonModelBinder.Converters
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Linq;
    using System.Reflection;
    using Attributes;
    using Interfaces;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Serialization;
    using Properties;
    using static Extensions;
    using static PatchDocumentCache;

    internal static class JsonConverterExtensions
    {
        #region Static Properties

        internal static MethodInfo SelectStrategyMethodInfo =
            ((Func<JsonReader, string, string, PropertyInfo, IPatchBase>)SelectStrategy<object>).GetMethodInfo()
                .GetGenericMethodDefinition();

        internal static MethodInfo ReadArrayMethodInfo =
            ((Func<JsonReader, string, string, string, IEnumerable<ValidationAttribute>, IPatchArray>)ReadArray<object>)
                .GetMethodInfo().GetGenericMethodDefinition();

        internal static MethodInfo ReadPrimitiveArrayMethodInfo =
            ((Func<JsonReader, string, string, string, IEnumerable<ValidationAttribute>, bool, IPatchPrimitive>)
                    ReadPrimitiveArray<object>)
                .GetMethodInfo().GetGenericMethodDefinition();

        internal static MethodInfo ReadPatchDocumentMethodInfo =
            ((Func<JsonReader, string, string, string, IEnumerable<ValidationAttribute>, PatchDocument<object>>)
                    ReadPatchDocument<object>)
                .GetMethodInfo().GetGenericMethodDefinition();

        #endregion

        /// <summary>
        /// Determines which strategy should handle the extraction of the token value.
        /// </summary>
        /// <typeparam name="T">Actual value type found on the CLR type.</typeparam>
        /// <param name="reader">The reader should point to the first toke of the value.</param>
        /// <param name="path"></param>
        /// <param name="propertyInfo"><see cref="PropertyInfo"/> of the CLR type.</param>
        /// <param name="name"></param>
        /// <returns></returns>
        internal static IPatchBase SelectStrategy<T>(JsonReader reader, string name, string path,
            PropertyInfo propertyInfo)
        {
            var type = typeof(T);
            var validationAttributes = propertyInfo.GetCustomAttributes<ValidationAttribute>();
            var displayName = propertyInfo.GetCustomAttribute<DisplayAttribute>()?.GetName() ?? name;

            IPatchBase result;

            if (type.IsPrimitiveWise())
            {
                result = ReadValue<T>(reader, name, path, displayName, validationAttributes);
            }
            else if (type.IsIEnumerable())
            {
                var genericArgument = type.GetGenericArgument();

                if (genericArgument.IsPrimitiveWise())
                {
                    result = (IPatchPrimitive)ReadPrimitiveArrayMethodInfo.MakeGenericMethod(genericArgument)
                        .Invoke(null, new object[] { reader, name, path, displayName, validationAttributes, type.IsArray });
                }
                else
                {
                    result = (IPatchArray)ReadArrayMethodInfo.MakeGenericMethod(genericArgument)
                        .Invoke(null, new object[] { reader, name, path, displayName, validationAttributes });
                }
            }
            else
            {
                result = reader != null
                    ? ReadPatchDocument<T>(reader, name, path, displayName, validationAttributes)
                    : (IPatchBase)ReadValue<T>(null, name, path, displayName, validationAttributes);
            }

            return result;
        }

        internal static PatchDocument<T> ReadPatchDocument<T>(JsonReader reader, string name, string path, string displayName,
            IEnumerable<ValidationAttribute> validationAttributes)
        {
            var jsonPath = reader.Path;
            var jsonName = reader.TokenType == JsonToken.PropertyName
                ? (string)reader.Value
                : !string.IsNullOrWhiteSpace(jsonPath) ? jsonPath.Split('.').Last() : jsonPath;

            var errors = new List<Error>();

            if (reader.TokenType != JsonToken.StartObject) reader.Read();

            var values = reader.TokenType == JsonToken.StartObject
                ? ReadPatchDocumentProperties<T>(reader, path)
                : null;

            if (values != null)
            {
                errors.AddRange(validationAttributes.Validate(name, path, jsonName, jsonPath, displayName, values, ErrorKinds.ApplyToAll));
            }
            else
            {
                values = new List<IPatchBase>();

                errors.Add(new Error
                {
                    ErrorType = typeof(InvalidCastException),
                    JsonName = jsonName,
                    JsonPath = jsonPath,
                    Message = Resources.InvalidCast,
                    Name = name,
                    Path = path
                });
            }

            return new PatchDocument<T>
            {
                Errors = errors,
                JsonName = jsonName,
                JsonPath = jsonPath,
                Name = name,
                Path = path,
                Values = values
            };
        }

        internal static IPatchArrayDocument<T> ReadArrayDocument<T>(JsonReader reader, string name, string path,
            BindAttribute bindAttribute = null)
        {
            var jsonPath = reader.Path;
            var jsonName = !string.IsNullOrWhiteSpace(jsonPath) ? jsonPath.Split('.').Last() : jsonPath;

            var errors = new List<Error>();
            var patchKeys = new List<IPatchPrimitive>();
            var patchTypeFound = false;
            PatchTypes? patchType = null;
            const string patchTypeKey = PatchArrayDocument<object>.PatchTypeKey;

            var values = reader.TokenType == JsonToken.StartObject
                ? ReadPatchDocumentProperties<T>(reader, path, bindAttribute,
                    patchKeyReader =>
                    {
                        try
                        {
                            patchTypeFound = true;

                            var patchKeyAsInt = patchKeyReader.ReadAsInt32();

                            patchType = Enum.IsDefined(typeof(PatchTypes), patchKeyAsInt)
                                ? (PatchTypes?)patchKeyAsInt
                                : null;

                            if (patchType == null)
                            {
                                errors.Add(new Error
                                {
                                    ErrorKind = ErrorKinds.ApplyToUpdate,
                                    ErrorType = typeof(InvalidCastException),
                                    JsonName = patchTypeKey,
                                    JsonPath = $"{jsonPath}.{patchTypeKey}",
                                    Message = Resources.InvalidCast,
                                    Name = patchTypeKey,
                                    Path = $"{path}.{patchTypeKey}"
                                });
                            }
                        }
                        catch (Exception)
                        {
                            reader.Skip();

                            errors.Add(new Error
                            {
                                ErrorKind = ErrorKinds.ApplyToUpdate,
                                ErrorType = typeof(InvalidCastException),
                                JsonName = patchTypeKey,
                                JsonPath = $"{jsonPath}.{patchTypeKey}",
                                Message = Resources.InvalidCast,
                                Name = patchTypeKey,
                                Path = $"{path}.{patchTypeKey}"
                            });
                        }
                    },
                    x =>
                    {
                        if (patchType != null && patchType != PatchTypes.Add && !x.Found)
                        {
                            errors.Add(new Error
                            {
                                ErrorKind = ErrorKinds.ApplyToUpdate,
                                ErrorType = typeof(RequiredAttribute),
                                JsonName = x.JsonName,
                                JsonPath = x.JsonPath,
                                Message = string.Format(Resources.KeyNotFound, x.Name),
                                Name = x.Name,
                                Path = x.Path
                            });
                        }

                        patchKeys.Add(x);
                    })
                : null;

            if (values != null)
            {
                //ToDo: Check for validations
                //errors.AddRange(Validate(name, jsonPath, validationAttributes, values, ErrorKinds.ApplyToAll));

                if (!patchTypeFound)
                {
                    errors.Add(new Error
                    {
                        ErrorKind = ErrorKinds.ApplyToUpdate,
                        ErrorType = typeof(RequiredAttribute),
                        JsonName = patchTypeKey,
                        JsonPath = $"{jsonPath}.{patchTypeKey}",
                        Message = Resources.PatchTypeNotFound,
                        Name = patchTypeKey,
                        Path = $"{path}.{patchTypeKey}"
                    });
                }
            }
            else
            {
                values = new List<IPatchBase>();

                errors.Add(new Error
                {
                    ErrorKind = ErrorKinds.ApplyToUpdate,
                    ErrorType = typeof(InvalidCastException),
                    JsonName = jsonName,
                    JsonPath = jsonPath,
                    Message = Resources.InvalidCast,
                    Name = name,
                    Path = path
                });
            }

            return new PatchArrayDocument<T>
            {
                Errors = errors,
                JsonName = jsonName,
                JsonPath = jsonPath,
                Name = name,
                PatchKeys = patchKeys,
                PatchType = patchType,
                Path = path,
                Values = values
            };
        }

        internal static List<IPatchBase> ReadPatchDocumentProperties<T>(JsonReader reader, string path,
            BindAttribute bindAttribute = null, Action<JsonReader> patchTypeAction = null,
            Action<IPatchPrimitive> keyAction = null)
        {
            if (reader == null) return null;

            var modelTypeAssemblyName = typeof(T).AssemblyQualifiedName;
            bool cacheProperties;
            const string patchTypeKey = PatchArrayDocument<object>.PatchTypeKey;

            // ReSharper disable once AssignmentInConditionalExpression
            var modelProperties = (cacheProperties = PropertyInfos.All(x => x.Key.Key1 != modelTypeAssemblyName))
                ? typeof(T).GetTypeInfo().GetProperties()
                : PropertyInfos.Where(x => x.Key.Key1 == modelTypeAssemblyName).Select(x => x.Value).ToArray();

            var values = new List<IPatchBase>();
            var startPath = reader.Path;
            bindAttribute = bindAttribute ?? typeof(T).GetAttribute<BindAttribute>();
            path = string.IsNullOrWhiteSpace(path) ? string.Empty : path + ".";

            var keys = new List<IPatchPrimitive>();

            while (reader.Read())
            {
                if (reader.Path == startPath) break;

                if (reader.TokenType != JsonToken.PropertyName)
                {
                    while (reader.Read())
                    {
                        if (reader.Path == startPath) return null;
                    }
                }

                var readerValueAsName = (string)reader.Value;

                if (readerValueAsName == patchTypeKey)
                {
                    if (patchTypeAction != null)
                    {
                        patchTypeAction(reader);
                    }
                    else
                    {
                        reader.Skip();
                    }

                    continue;
                }
                
                var propertyInfo = modelProperties.FirstOrDefault(x =>
                {
                    var jsonPropertyNameAttribute = x.GetCustomAttribute<JsonPropertyAttribute>();

                    if (jsonPropertyNameAttribute != null)
                    {
                        return string.Equals(jsonPropertyNameAttribute.PropertyName, readerValueAsName);
                    }

                    return readerValueAsName.Equals(new CamelCaseNamingStrategy().GetPropertyName(x.Name, false))
                           || readerValueAsName.Equals(x.Name)
                           || readerValueAsName.Equals(new SnakeCaseNamingStrategy().GetPropertyName(x.Name, false));
                });

                // If property not found, we should report an error with it.
                if (propertyInfo == null)
                {
                    values.Add(new PatchPrimitive<object>
                    {
                        Errors = new List<Error>
                        {
                            new Error
                            {
                                ErrorType = typeof(InvalidCastException),
                                JsonName = readerValueAsName,
                                JsonPath = reader.Path,
                                Message = string.Format(Resources.UnknownProperty, readerValueAsName),
                                Name = readerValueAsName,
                                Path = reader.Path
                            }
                        },
                        Found = true,
                        JsonName = readerValueAsName,
                        JsonPath = reader.Path,
                        Name = readerValueAsName,
                        Path = reader.Path,
                        Value = null
                    });

                    // Since this is an unknown property, we should not parse it.
                    reader.Skip();

                    continue;
                }

                var propertyName = propertyInfo.Name;
                var isKey = propertyInfo.GetCustomAttribute<KeyAttribute>() != null;
                var isBindable = IsBindable(bindAttribute, propertyName);

                if (!isBindable && (!isKey || keyAction == null))
                {
                    reader.Skip();
                    continue;
                }

                var value = (IPatchBase)SelectStrategyMethodInfo.MakeGenericMethod(propertyInfo.PropertyType)
                    .Invoke(null, new object[] { reader, propertyName, path + propertyName, propertyInfo });

                value.IgnoreApply = propertyInfo.GetCustomAttribute<IgnorePatchAttribute>() != null;

                if (isKey && keyAction != null) keys.Add((IPatchPrimitive)value);

                if (isBindable) values.Add(value);
            }

            foreach (var propertyInfo in modelProperties)
            {
                if (cacheProperties) PropertyInfos.Add(modelTypeAssemblyName, propertyInfo.Name, propertyInfo);

                if (values.Any(x => x.Name == propertyInfo.Name)) continue;

                var propertyName = propertyInfo.Name;
                var isKey = propertyInfo.GetCustomAttribute<KeyAttribute>() != null;
                var isBindable = IsBindable(bindAttribute, propertyName);

                if (!isBindable && (!isKey || keyAction == null)) continue;

                var value = (IPatchBase)SelectStrategyMethodInfo.MakeGenericMethod(propertyInfo.PropertyType)
                    .Invoke(null, new object[] { null, propertyName, path + propertyName, propertyInfo });

                value.IgnoreApply = propertyInfo.GetCustomAttribute<IgnorePatchAttribute>() != null;

                if (isKey && keyAction != null && keys.All(x => x.Name != value.Name)) keys.Add((IPatchPrimitive)value);

                if (isBindable) values.Add(value);
            }

            if (keys.Any())
            {
                foreach (var key in keys)
                {
                    // ReSharper disable once PossibleNullReferenceException
                    keyAction.Invoke(key);
                }
            }

            return values;
        }

        internal static IPatchArray ReadArray<T>(JsonReader reader, string name, string path, string displayName,
            IEnumerable<ValidationAttribute> validationAttributes)
        {
            var jsonPath = path;
            var jsonName = name;

            var errors = new List<Error>();
            var values = new List<IPatchArrayDocument<T>>();
            var found = reader != null;

            if (reader?.TokenType != JsonToken.StartArray) reader?.Read();

            if (found && reader.TokenType == JsonToken.StartArray)
            {
                var bindAttribute = typeof(T).GetAttribute<BindAttribute>();

                jsonPath = reader.Path;
                jsonName = reader.TokenType == JsonToken.PropertyName
                ? (string)reader.Value
                : !string.IsNullOrWhiteSpace(jsonPath) ? jsonPath.Split('.').Last() : jsonPath;

                var index = 0;
                while (reader.Read())
                {
                    if (reader.Path == jsonPath) break;

                    var arrayDocumentName = $"{name}[{index}]";

                    if (reader.TokenType == JsonToken.StartObject)
                    {
                        values.Add(ReadArrayDocument<T>(reader, arrayDocumentName,
                            reader.Path, bindAttribute));
                    }
                    else
                    {
                        errors.Add(new Error
                        {
                            ErrorType = typeof(InvalidCastException),
                            JsonName = $"{jsonName}[{index}]",
                            JsonPath = reader.Path,
                            Message = Resources.InvalidCast,
                            Name = arrayDocumentName,
                            Path = $"{jsonPath}[{index}]"
                        });

                        reader.Skip();
                    }

                    index++;
                }
            }

            if (!errors.Any()) errors = validationAttributes.Validate(name, path, jsonName, jsonPath, displayName, values, ErrorKinds.ApplyToAll);

            return new PatchArray<T>
            {
                Errors = errors,
                Found = found,
                JsonName = jsonName,
                JsonPath = jsonPath,
                Name = name,
                Path = path,
                Values = values
            };
        }

        internal static IPatchPrimitive ReadPrimitiveArray<T>(JsonReader reader, string name, string path, string displayName,
            IEnumerable<ValidationAttribute> validationAttributes,
            bool toArray)
        {
            var jsonPath = path;
            var jsonName = name;

            var errors = new List<Error>();
            IList value = new List<T>();

            if (reader != null)
            {
                try
                {
                    jsonPath = reader.Path;
                    jsonName = !string.IsNullOrWhiteSpace(jsonPath) ? jsonPath.Split('.').Last() : jsonPath;

                    if (toArray && typeof(T) == typeof(byte))
                    {
                        value = reader.ReadAsBytes();
                    }
                    else
                    {
                        reader.Read();

                        while (reader.Read())
                        {
                            if (reader.Path == path) break;

                            value.Add(typeof(T) != reader.Value.GetType()
                                ? Convert.ChangeType(reader.Value, typeof(T))
                                : reader.Value);
                        }

                        if (toArray)
                        {
                            value = ((List<T>)value).ToArray();
                        }
                    }
                }
                catch (Exception)
                {
                    errors.Add(new Error
                    {
                        ErrorType = typeof(InvalidCastException),
                        JsonName = jsonName,
                        JsonPath = jsonPath,
                        Message = Resources.InvalidCast,
                        Name = name,
                        Path = path
                    });
                    reader.Skip();
                }
            }
            else if(toArray)
            {
                value = new T[0];
            }

            if (!errors.Any()) errors = validationAttributes.Validate(name, path, jsonName, jsonPath, displayName, value, ErrorKinds.ApplyToAll);

            return new PatchPrimitive<IEnumerable<T>>
            {
                Errors = errors,
                Found = true,
                JsonName = jsonName,
                JsonPath = jsonPath,
                Name = name,
                Path = path,
                Value = (IEnumerable<T>)value
            };
        }

        internal static IPatchPrimitive ReadValue<T>(JsonReader reader, string name, string path, string displayName,
            IEnumerable<ValidationAttribute> validationAttributes)
        {
            var jsonPath = path;
            var jsonName = name;

            var value = default(T);
            var errors = new List<Error>();
            object boxedValue = null;
            var found = reader != null;

            if (found)
            {
                try
                {
                    jsonPath = reader.Path;
                    jsonName = reader.TokenType == JsonToken.PropertyName
                    ? (string)reader.Value
                    : jsonPath;

                    reader.Read();

                    if (!IsPropertyValue(reader)) throw new InvalidCastException();

                    var type = typeof(T);
                    boxedValue = reader.Value;

                    if (type.IsEnum)
                    {
                        value = (T)Enum.Parse(type, reader.Value?.ToString());
                    }
                    else if ((reader.ValueType == typeof(long) && type != typeof(long))
                        || (reader.ValueType == typeof(double) && type != typeof(double)))
                    {
                        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                        {
                            type = Nullable.GetUnderlyingType(type);
                        }

                        value = (T)Convert.ChangeType(reader.Value, type);
                    }
                    else
                    {
                        value = (T)reader.Value;
                    }
                }
                catch
                {
                    reader.Skip();

                    errors.Add(new Error
                    {
                        ErrorType = typeof(InvalidCastException),
                        JsonName = jsonName,
                        JsonPath = jsonPath,
                        Message = Resources.InvalidCast,
                        Name = name,
                        Path = path
                    });
                }
            }

            if (!errors.Any())
            {
                errors.AddRange(validationAttributes.Validate(name, path, jsonName, jsonPath, displayName,
                    found ? boxedValue : null,
                    found ? ErrorKinds.ApplyToAll : ErrorKinds.ApplyToCreate));
            }

            return new PatchPrimitive<T>
            {
                Errors = errors,
                Found = found,
                JsonName = jsonName,
                JsonPath = jsonPath,
                Name = name,
                Path = path,
                Value = value
            };
        }

        internal static List<Error> Validate(this IEnumerable<ValidationAttribute> validationAttributes, string name, string path,
            string jsonName, string jsonPath, string displayName, object value, ErrorKinds errorKind)
        {
            var errors = new List<Error>();

            foreach (var attribute in validationAttributes)
            {
                string errorMessage;

                if ((errorMessage = attribute.CheckValidationAttribute(displayName, value)) != null)
                {
                    errors.Add(new Error
                    {
                        ErrorKind = errorKind,
                        ErrorType = attribute.GetType(),
                        JsonName = jsonName,
                        JsonPath = jsonPath,
                        Message = errorMessage,
                        Name = name,
                        Path = path
                    });
                }
            }

            return errors;
        }
        
        internal static bool IsPropertyValue(JsonReader reader)
        {
            var isValue = false;

            // ReSharper disable once SwitchStatementMissingSomeCases
            switch (reader.TokenType)
            {
                case JsonToken.Raw:
                case JsonToken.Integer:
                case JsonToken.Float:
                case JsonToken.String:
                case JsonToken.Boolean:
                case JsonToken.Date:
                case JsonToken.Bytes:
                case JsonToken.Null:
                    isValue = true;
                    break;
            }

            return isValue;
        }
    }
}