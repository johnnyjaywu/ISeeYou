using System;

namespace ContentContent
{
    [Serializable]
    public struct LibraryEntry<T>
    {
        public string id;
        public T value;
    }
}