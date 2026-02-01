using System;
using UnityEngine;

namespace ISeeYou
{
    public class WordsValidator : MonoBehaviour
    {
        [SerializeField] private DropZone dropZone;

        private void Awake()
        {
            dropZone.OnZoneEnter += OnZoneEnter;
            dropZone.OnZoneExit += OnZoneExit;
        }


        private void OnZoneEnter(Draggable obj)
        {
            Words words = obj.GetComponent<Words>();
            if (words == null) return;
        }

        private void OnZoneExit(Draggable obj)
        {
            Words words = obj.GetComponent<Words>();
            if (words == null) return;
        }
    }
}