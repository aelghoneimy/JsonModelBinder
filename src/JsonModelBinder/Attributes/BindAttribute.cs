namespace JsonModelBinder.Attributes
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class BindAttribute : Attribute
    {
        public BindAttribute(params string[] include)
        {
            var items = new List<string>();
            foreach (var item in include)
            {
                items.AddRange(SplitString(item));
            }

            Include = items.ToArray();
        }
        
        public string[] Include { get; }
        
        private static IEnumerable<string> SplitString(string original)
        {
            if (string.IsNullOrEmpty(original))
            {
                return new string[0];
            }

            var split = original.Split(',').Select(piece => piece.Trim()).Where(piece => !string.IsNullOrEmpty(piece));

            return split;
        }
    }
}
