using UnityEngine;

namespace ContentContent
{
    public class LibraryEntryAttribute : PropertyAttribute
    {
        // valueLabel is optional; if omitted, it defaults to "Value"
        public LibraryEntryAttribute(string idLabel, string valueLabel = "Value")
        {
            IdLabel = idLabel;
            ValueLabel = valueLabel;
        }

        public string IdLabel { get; private set; }
        public string ValueLabel { get; private set; }
    }
}