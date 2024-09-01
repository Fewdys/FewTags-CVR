using UnityEngine;
using TMPro;
using System.Collections;

namespace FewTags.Utils
{
    public class RainbowTextAnimator : MonoBehaviour
    {
        private TextMeshProUGUI textMeshPro;
        private float duration;
        private float elapsedTime;
        private bool isAnimating;

        public void Initialize(TextMeshProUGUI tmp, float animDuration)
        {
            textMeshPro = tmp;
            duration = animDuration;
            elapsedTime = 0f;
            isAnimating = true;

            // Start the animation coroutine
            StartCoroutine(AnimateRainbow());
        }

        private IEnumerator AnimateRainbow()
        {
            while (isAnimating)
            {
                elapsedTime += Time.deltaTime;
                float t = Mathf.PingPong(elapsedTime / duration, 1);
                Color32[] colors = GetRainbowColors();
                int colorCount = colors.Length;

                // Create the new color gradient for each character
                TMP_TextInfo textInfo = textMeshPro.textInfo;
                int characterCount = textInfo.characterCount;

                // If there are characters, assign them a rainbow color
                if (characterCount > 0)
                {
                    for (int i = 0; i < characterCount; i++)
                    {
                        if (textInfo.characterInfo[i].isVisible)
                        {
                            int colorIndex = (int)((i + t * characterCount) % colorCount);
                            textMeshPro.textInfo.characterInfo[i].color = colors[colorIndex];
                        }
                    }

                    // Update the mesh to apply colors
                    textMeshPro.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
                }

                yield return null;
            }
        }

        private Color32[] GetRainbowColors()
        {
            return new Color32[]
            {
            new Color32(255, 0, 0, 255),    // Red
            new Color32(255, 127, 0, 255),  // Orange
            new Color32(255, 255, 0, 255),  // Yellow
            new Color32(0, 255, 0, 255),    // Green
            new Color32(0, 0, 255, 255),    // Blue
            new Color32(75, 0, 130, 255),   // Indigo
            new Color32(148, 0, 211, 255)   // Violet
            };
        }
    }

}
