using System;

namespace IDBEmit
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ClientStorageAttribute: Attribute
    {
        public string StorageName {get;set;}

        public ClientStorageAttribute()
        {}
        public ClientStorageAttribute(string name)
        {
            StorageName = name;
        }
    }
}
