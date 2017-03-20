namespace JsonModelBinder.Attributes
{
    using System;

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class IgnorePatchAttribute : Attribute
    {
    }
}