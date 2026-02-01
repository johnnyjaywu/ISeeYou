using System.Collections.Generic;
using ContentContent;
using UnityEngine;

namespace ISeeYou
{
    public class SpeakerManager : SingletonBehaviour<SpeakerManager>
    {
        private readonly List<Speaker> speakers = new();

        public static void Register(Speaker speaker)
        {
            if (Instance.speakers.Contains(speaker)) return;
            Instance.speakers.Add(speaker);
        }

        public static Speaker GetSpeaker(string id)
        {
            return Instance.speakers.Find(s => s.ID == id);
        }
    }
}