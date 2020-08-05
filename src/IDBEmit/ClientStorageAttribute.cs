using System;

namespace IDBEmit
{
    [AttributeUsage(AttributeTargets.Class)]
    /// <summary>
    /// Mark this entity type as client stored with IndexedDB
    /// </summary>
    public class ClientStorageAttribute : Attribute
    {
        /// <summary>
        /// The name of IndexedDB object store
        /// </summary>
        public string StorageName { get; set; }
        /// <summary>
        /// Default ctor. The name of object store is the same as class name
        /// </summary>

        public ClientStorageAttribute()
        { }
        /// <summary>
        /// The name of object store can be defined hire
        /// </summary>
        /// <param name="name">Object storage name</param>

        public ClientStorageAttribute(string name)
        {
            StorageName = name;
        }
    }
}
