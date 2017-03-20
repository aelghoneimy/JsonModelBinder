namespace JsonModelBinder
{
    using System.Linq.Expressions;
    using System.Reflection;

    internal class PatchDocumentCache
    {
        #region Public Static Properties

        internal static MethodInfo ObjectEquals = typeof(object).GetTypeInfo().GetMethod(nameof(object.Equals), new[] { typeof(object) });
        internal static readonly Cache<string, ParameterExpression> ParameterExpressions = new Cache<string, ParameterExpression>();
        internal static readonly Cache<string, string, MemberExpression> PropertyExpressions = new Cache<string, string, MemberExpression>();
        internal static readonly Cache<string, string, PropertyInfo> PropertyInfos = new Cache<string, string, PropertyInfo>();

        #endregion
    }
}