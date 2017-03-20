namespace JsonModelBinder
{
    using System;
    using System.Collections;
    using System.ComponentModel.DataAnnotations;
    using System.Linq;
    using System.Reflection;
    using Attributes;

    internal static class Extensions
    {
        private static readonly object ValidationObject = new object();

        public static string CheckValidationAttribute(this ValidationAttribute attribute, string displayName, object value)
        {
            var validationContext = new ValidationContext(ValidationObject)
            {
                MemberName = displayName
            };

            return attribute.GetValidationResult(value, validationContext)?.ErrorMessage;
        }

        public static bool IsBindable(BindAttribute attribute, string name)
        {
            return (attribute == null) || attribute.Include.Contains(name);
        }

        public static object CreateGenericInstance(this Type genericType, Type genericArgument, params object[] args)
        {
            var concreteGenericType = genericType.MakeGenericType(genericArgument);

            return Activator.CreateInstance(concreteGenericType, args);
        }

        /// <summary>
        /// Creates a new instance out of a generic type.
        /// </summary>
        /// <typeparam name="TReturn">Specify the return type of this method.</typeparam>
        /// <param name="genericType">The type which the new instance will be created from.</param>
        /// <param name="genericArgument">The generic argument which will be associated with the new instance.</param>
        /// <param name="args">Parameters that need to be passed to the constructor of the calling type.</param>
        /// <returns>New instance of <see cref="TReturn"/>.</returns>
        public static TReturn CreateGenericInstance<TReturn>(this Type genericType, Type genericArgument, params object[] args)
        {
            var concreteGenericType = genericType.MakeGenericType(genericArgument);

            return (TReturn)Activator.CreateInstance(concreteGenericType, args);
        }

        /// <summary>
        /// Creates a new instance out of a generic type.
        /// </summary>
        /// <typeparam name="TReturn">Specify the return type of this method.</typeparam>
        /// <param name="genericType">The type which the new instance will be created from.</param>
        /// <param name="genericArgument">The generic argument which will be associated with the new instance.</param>
        /// <param name="nonPublic">Specify whether to look for non public constructors or not.</param>
        /// <returns>New instance of <see cref="TReturn"/>.</returns>
        public static TReturn CreateGenericInstance<TReturn>(this Type genericType, Type genericArgument, bool nonPublic)
        {
            var concreteGenericType = genericType.MakeGenericType(genericArgument);

            return (TReturn)Activator.CreateInstance(concreteGenericType, nonPublic);
        }

        public static Type GetPropertyGenericArgument(this Type type, string propertyName)
        {
            var propertyType = type.GetTypeInfo().GetProperty(propertyName)?.PropertyType;

            return propertyType?.GetGenericArgument();
        }

        public static T GetAttribute<T>(this Type type) where T : Attribute
        {
            return type.GetTypeInfo().GetCustomAttribute<T>();
        }

        public static Type GetGenericArgument(this Type type)
        {
            return type.IsArray
                ? type.GetElementType()
                : type.GenericTypeArguments.FirstOrDefault();
        }

        public static bool IsPrimitiveWise(this Type type)
        {
            var typeInfo = type.GetTypeInfo();

            return typeInfo.IsPrimitive
                   || typeInfo.IsEnum
                   || type == typeof(string)
                   || type == typeof(DateTime);
        }

        public static bool IsIEnumerable(this Type type)
        {
            return type.GetTypeInfo().ImplementedInterfaces.Any(x => x == typeof(IEnumerable));
        }
    }
}