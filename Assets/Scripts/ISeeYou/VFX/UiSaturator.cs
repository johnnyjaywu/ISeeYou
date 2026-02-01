using UnityEngine;
using UnityEngine.UI;

namespace ISeeYou.VFX
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Image))]
    [AddComponentMenu("I See You/Effects/UI Saturator")]
    public class UiSaturator : MonoBehaviour
    {
        private static readonly int s_saturation = Shader.PropertyToID("_Saturation");
        
        private Image m_image;
        private Material m_material;
        private float m_previousSaturation;
        
        [Tooltip("Color saturation.")]
        [SerializeField]
        [Range(0f, 1f)]
        private float m_saturation;

        private void Start()
        {
            m_image = GetComponent<Image>();
            m_material = Instantiate(m_image.material);
            m_image.material = m_material;
            
            m_saturation = m_material.GetFloat(s_saturation);
            m_previousSaturation = m_saturation;
            
            Refresh();
        }
        
        private void LateUpdate()
        {
            if (Mathf.Approximately(m_saturation, m_previousSaturation))
            {
                return;
            }
            
            m_previousSaturation = m_saturation;
            
            Refresh();
        }

        private void Refresh() => m_material.SetFloat(s_saturation, m_saturation);
    }
}