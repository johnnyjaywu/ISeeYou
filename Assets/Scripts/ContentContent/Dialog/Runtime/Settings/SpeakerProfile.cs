using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ContentContent.Dialog
{
    [Serializable]
    public class SpeakerProfile
    {
        public string speakerID;

        [Tooltip("If left empty, will use speakerID as the name")]
        public string speakerName;

        public Sprite defaultPortrait;

        [SerializeField] private List<LibraryEntry<Sprite>> portraitVariants;

        public Sprite GetPortrait(string portraitID)
        {
            if (string.IsNullOrEmpty(portraitID) || portraitVariants == null) return defaultPortrait;

            foreach (LibraryEntry<Sprite> variant in portraitVariants.Where(entry => entry.id == portraitID))
                return variant.value;

            return defaultPortrait;
        }
    }
}