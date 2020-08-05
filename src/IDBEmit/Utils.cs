using System;

namespace IDBEmit
{
    internal static class Utils
    {
        /// <summary>
        /// Returns class name without namespace
        /// </summary>
        /// <param name="name">class name</param>
        internal static string NormalizedName(string name)
        {
            if (!name.Contains("."))
            {
                return name;
            }
            else
            {
                return name.Substring(name.LastIndexOf(".")+1);
            }
        }
    }
}