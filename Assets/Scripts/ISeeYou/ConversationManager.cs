using System.Collections.Generic;
using ContentContent; 
using NaughtyAttributes;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem; // Required for InputActionAsset
using UnityEngine.UI;

namespace ISeeYou
{
    public class ConversationManager : MonoBehaviour
    {
        [Header("Game Data")]
        [Expandable, Required]
        [SerializeField] private ConversationData conversation;

        [Header("Scene References")]
        [SerializeField] private Words wordsPrefab;
        [SerializeField] private RectTransform spawnContainer; 
        [SerializeField] private DropZone speechBubble;
        [SerializeField] private ConversationTransitionController transitions;
        [Tooltip("The Text component that displays the final Truth string.")]
        [SerializeField] private TextMeshProUGUI truthText;
        
        [Header("Input Settings")]
        [Tooltip("The Input Actions asset used by the EventSystem.")]
        [SerializeField] private InputActionAsset inputAsset;
        [Tooltip("The name of the Action Map that drives UI interactions (default is usually 'UI').")]
        [SerializeField] private string uiActionMapName = "UI";

        [Header("Spawning Settings")]
        [SerializeField] private float wallPadding = 20f;

        private StateMachine stateMachine;
        private InputActionMap uiMap;
        private CanvasGroup speechBubbleCanvasGroup;

        // Public Accessors
        public StateMachine StateMachine => stateMachine; 
        public List<Words> WordsList { get; private set; } = new();
        public ConversationData CurrentData => conversation;
        public ConversationTransitionController Transitions => transitions;
        public DropZone SpeechBubble => speechBubble;
        public CanvasGroup BubbleCanvasGroup => speechBubbleCanvasGroup;
        public TextMeshProUGUI TruthText => truthText;
        
        private void Awake()
        {
            stateMachine = new StateMachine();

            // Cache the UI Map
            if (inputAsset != null)
            {
                uiMap = inputAsset.FindActionMap(uiActionMapName);
                if (uiMap == null)
                {
                    Debug.LogWarning($"[ConversationManager] Could not find Action Map named '{uiActionMapName}' in the Input Asset.");
                }
            }
            
            if (speechBubble != null)
                speechBubbleCanvasGroup = speechBubble.GetComponent<CanvasGroup>();
        }

        private void Start()
        {
            if (conversation == null || speechBubble == null)
            {
                Debug.LogError("[ConversationManager] Missing ConversationData or DropZone assignment!");
                return;
            }

            var filteringState = new FilteringState(this);
            stateMachine.ChangeState(filteringState);
        }

        private void Update()
        {
            stateMachine.Update();
        }

        /// <summary>
        /// Global switch for Player Input.
        /// Enables/Disables the entire UI Action Map, effectively pausing all Drag/Click/Hover events.
        /// </summary>
        public void SetInputActive(bool active)
        {
            if (uiMap == null) return;

            if (active)
            {
                if (!uiMap.enabled) uiMap.Enable();
            }
            else
            {
                if (uiMap.enabled) uiMap.Disable();
            }
        }

        public void SpawnNoisePhase()
        {
            ClearWordsList();
            var noiseWords = conversation.GetNoiseWords();
            if (noiseWords == null) return;

            foreach (var data in noiseWords)
            {
                SpawnWord(data); 
            }
        }

        public Words SpawnWord(WordsData wordsData, Transform targetParent = null)
        {
            Transform parent = targetParent != null ? targetParent : spawnContainer;
            var words = Instantiate(wordsPrefab, parent);
            words.Initialize(wordsData);
            WordsList.Add(words);

            if (parent == spawnContainer)
            {
                RectTransform wordRect = words.GetComponent<RectTransform>();
                LayoutRebuilder.ForceRebuildLayoutImmediate(wordRect);

                Rect containerRect = spawnContainer.rect;
                float halfWidth = wordRect.rect.width / 2f;
                float halfHeight = wordRect.rect.height / 2f;

                float minX = containerRect.xMin + halfWidth + wallPadding;
                float maxX = containerRect.xMax - halfWidth - wallPadding;
                float minY = containerRect.yMin + halfHeight + wallPadding;
                float maxY = containerRect.yMax - halfHeight - wallPadding;

                if (minX > maxX) { minX = 0; maxX = 0; }
                if (minY > maxY) { minY = 0; maxY = 0; }

                wordRect.anchoredPosition = new Vector2(
                    Random.Range(minX, maxX), 
                    Random.Range(minY, maxY)
                );
                wordRect.localPosition = new Vector3(wordRect.localPosition.x, wordRect.localPosition.y, 0);
            }
            else
            {
                words.transform.localScale = Vector3.one;
                words.transform.localPosition = Vector3.zero;
            }

            return words;
        }

        private void ClearWordsList()
        {
            foreach (Words word in WordsList)
            {
                if (word != null) Destroy(word.gameObject);
            }
            WordsList.Clear();
        }
    }
}