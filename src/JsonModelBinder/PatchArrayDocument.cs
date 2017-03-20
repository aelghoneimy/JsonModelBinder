namespace JsonModelBinder
{
    using System.Collections.Generic;
    using Interfaces;

    public class PatchArrayDocument<T> : PatchDocument<T>, IPatchArrayDocument<T>
    {
        #region Constrants
        
        internal const string PatchTypeKey = "_patchType";

        #endregion

        #region Public Properties

        public override PatchKinds Kind => PatchKinds.ArrayDocument;
        
        public IEnumerable<IPatchPrimitive> PatchKeys { get; internal set; }

        public PatchTypes? PatchType { get; internal set; }

        #endregion

        #region Constructors

        internal PatchArrayDocument()
        {
        }

        #endregion    
    }
}