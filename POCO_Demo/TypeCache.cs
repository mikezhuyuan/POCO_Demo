using System;

namespace POCO_Demo
{
    /// <summary>
    /// A very fast type cache. http://xheo.com/blog/easy-fast-thread-safe-dictionary-with-a-type-key
    /// </summary>    
    public static class TypeCache<T>
    {
        public static readonly Type Type = typeof(T);

        //public static readonly PropertyInfo[] Properties = typeof( T ).GetProperties();
    }
}
