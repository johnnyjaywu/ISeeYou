using System;
using UnityEngine;

namespace ContentContent
{
    [AttributeUsage(AttributeTargets.Field)]
    public class SerializeReferenceDropdownAttribute : PropertyAttribute
    {
    }
}