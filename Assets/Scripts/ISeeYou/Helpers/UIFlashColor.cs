using UnityEngine;
using UnityEngine.UI;
using PrimeTween;

public class UIFlashColor : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private Graphic targetGraphic;

    [SerializeField] private Color flashColor = Color.white;
    [SerializeField] private float flashDuration = 1f;
    [SerializeField] private Ease easeType = Ease.InSine;
    
    // Updated Default: 2 cycles with Yoyo ensures we go Original -> Flash -> Original
    [SerializeField] private int cycles = 2; 
    [SerializeField] private CycleMode cycleMode = CycleMode.Yoyo;

    private Color originalColor;
    private Tween flashTween;

    private void Awake()
    {
        if (targetGraphic == null)
        {
            targetGraphic = GetComponent<Graphic>();
        }

        if (targetGraphic != null)
        {
            originalColor = targetGraphic.color;
        }
        else
        {
            Debug.LogError("[UIDamageFlasher] No Graphic component found.", this);
        }
    }

    public void Flash()
    {
        if (targetGraphic == null) return;

        if (flashTween.isAlive)
        {
            flashTween.Stop();
        }

        // Logic changed: Instead of snapping to flashColor instantly, 
        // we tween FROM the current color TO the flashColor.
        flashTween = Tween.Color(
            targetGraphic,
            flashColor,
            flashDuration,
            easeType,
            cycles,
            cycleMode
        );
    }

    private void OnDestroy()
    {
        if (flashTween.isAlive)
        {
            flashTween.Stop();
        }
    }
}