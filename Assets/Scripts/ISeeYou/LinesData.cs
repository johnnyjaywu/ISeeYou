using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ContentContent.Audio;
using NaughtyAttributes;
using Swarmkeeper;
using UnityEngine;

namespace ISeeYou
{
    [System.Serializable]
    public struct Line
    {
        public string text;
        public SoundData voiceLine;
    }
    
    [CreateAssetMenu(fileName = "Lines", menuName = "I See You/Lines Data")]
    public class LinesData : ScriptableObject
    {
        [SerializeField] private List<Line> lines;
        public List<Line> Lines => lines;
    }
}