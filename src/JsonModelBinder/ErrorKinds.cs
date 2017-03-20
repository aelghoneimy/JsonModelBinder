namespace JsonModelBinder
{
    using System;

    [Flags]
    public enum ErrorKinds
    {
        ApplyToCreate = 0x01,
        ApplyToUpdate = 0x10,
        ApplyToAll = ApplyToCreate | ApplyToUpdate,
    }
}