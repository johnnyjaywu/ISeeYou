using UnityEngine;
using UnityEngine.UI;

namespace ISeeYou.VFX
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Image))]
    [AddComponentMenu("I See You/Effects/UI Masker")]
    public class UiMasker : MonoBehaviour
    {
        private static readonly int s_min = Shader.PropertyToID("_Min");
        private static readonly int s_max = Shader.PropertyToID("_Max");
        
        private RectTransform m_rectTransform;
        private Image m_image;
        private Material m_material;
        
        private void Start()
        {
            m_image = GetComponent<Image>();
            m_rectTransform = m_image.rectTransform;
            m_material = Instantiate(m_image.material);
            m_image.material = m_material;
            
            Refresh();
        }
        
        private void LateUpdate()
        {
            Refresh();
        }

        private void Refresh()
        {
            m_material.SetVector(s_min, m_rectTransform.anchorMin);
            m_material.SetVector(s_max, m_rectTransform.anchorMax);
        }
    }
}
