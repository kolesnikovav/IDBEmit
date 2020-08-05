using System;

namespace IDBEmit
{
    [AttributeUsage(AttributeTargets.Property)]
    /// <summary>
    /// Mark this entity field as index on object store IndexedDB
    /// </summary>
    /// <param name="Unique">make this field unique at client</param>
    public class ClientIndexAttribute : Attribute
    {
        /// <summary>
        /// make this field unique at client
        /// </summary>
        public bool Unique { get; set; }
        /// <summary>
        /// Default constructor for make non unique index at client
        /// </summary>
        public ClientIndexAttribute()
        { }
        /// <summary>
        /// constructor for make non unique or unique index at client
        /// </summary>
        /// <param name="unique">make this field unique at client</param>
        public ClientIndexAttribute(bool unique)
        {
            Unique = unique;
        }
    }
}
