using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;

namespace ISeeYou
{
    public class FadeInOut : MonoBehaviour
    {
        public UIAnimator animator;
        public float holdDuration = 3f;
        public UnityEvent onFinish;

        [Button]
        public void Play()
        {
            Debug.Log("Playing FadeInOut");
            gameObject.SetActive(true);
            animator.FadeIn().Delay(holdDuration).FadeOut().OnFinish(() =>
            {
                onFinish?.Invoke();
                gameObject.SetActive(false);
            }).Play();
        }
    }
}