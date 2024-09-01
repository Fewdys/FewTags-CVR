using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace FewTags.Utils
{
    public enum AnimationType
    {
        None,
        Rainbow,
        SmoothRainbow,
        LetterByLetter,
        Bounce, // New animation type
    }

    public class AnimationManager : MonoBehaviour
    {
        public TextMeshProUGUI textMeshPro;
        public List<AnimationType> animationTypes = new List<AnimationType>();
        public float animationSpeed = 1f; // Adjust speed as needed
        public float rain = 2.5f;
        private string originalText;

        private Coroutine colorCoroutine;
        private Coroutine contentCoroutine;

        // Define rainbow colors
        public Color32[] rainbowColors = new Color32[]
        {
            new Color32(255, 0, 0, 255), // Red
            new Color32(255, 127, 0, 255), // Orange
            new Color32(255, 255, 0, 255), // Yellow
            new Color32(0, 255, 0, 255), // Green
            new Color32(0, 0, 255, 255), // Blue
            new Color32(75, 0, 130, 255), // Indigo
            new Color32(148, 0, 211, 255) // Violet
        };

        public void Start()
        {
            if (textMeshPro == null)
            {
                Debug.LogError("TextMeshProUGUI component is not assigned.");
                return;
            }

            originalText = textMeshPro.text;
            StartAnimation();
        }

        public void StartAnimation()
        {
            bool hasColorAnimation = animationTypes.Contains(AnimationType.Rainbow) || animationTypes.Contains(AnimationType.SmoothRainbow) || animationTypes.Contains(AnimationType.Bounce);

            bool hasContentAnimation = animationTypes.Contains(AnimationType.LetterByLetter);

            if (hasColorAnimation)
            {
                if (colorCoroutine != null)
                {
                    StopCoroutine(colorCoroutine);
                }

                if (animationTypes.Contains(AnimationType.Bounce))
                {
                    colorCoroutine = StartCoroutine(BounceAnimation());
                }
                else if (animationTypes.Contains(AnimationType.Rainbow))
                {
                    colorCoroutine = StartCoroutine(RainbowAnimation());
                }
                else if (animationTypes.Contains(AnimationType.SmoothRainbow))
                {
                    colorCoroutine = StartCoroutine(SmoothRainbowAnimation());
                }
            }

            if (hasContentAnimation)
            {
                if (contentCoroutine != null)
                {
                    StopCoroutine(contentCoroutine);
                }

                contentCoroutine = StartCoroutine(LetterByLetterAnimation());
            }
            else
            {
                textMeshPro.text = originalText;
            }
        }

        private IEnumerator RainbowAnimation()
        {
            float hueShiftSpeed = rain;
            int colorCount = rainbowColors.Length;
            float time = 0;
            StringBuilder sb = new StringBuilder();

            while (true)
            {
                time += Time.deltaTime * hueShiftSpeed;
                time %= 1.0f;

                sb.Clear();

                for (int i = 0; i < originalText.Length; i++)
                {
                    int colorIndex = (i + Mathf.FloorToInt(time * colorCount)) % colorCount;
                    Color32 color = rainbowColors[colorIndex];
                    sb.Append($"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{originalText[i]}</color>");
                }

                textMeshPro.text = sb.ToString();
                yield return null;
            }
        }

        private IEnumerator SmoothRainbowAnimation()
        {
            float t = 0;
            float hueShiftSpeed = animationSpeed / 8f;
            StringBuilder sb = new StringBuilder();

            while (true)
            {
                t += Time.deltaTime * hueShiftSpeed;
                if (t > 1) t -= 1;

                sb.Clear();

                for (int i = 0; i < originalText.Length; i++)
                {
                    float hue = (t + i * 1.0f / originalText.Length) % 1.0f;
                    Color color = Color.HSVToRGB(hue, 1.0f, 1.0f);
                    sb.Append($"<color=#{ColorUtility.ToHtmlStringRGBA(color)}>{originalText[i]}</color>");
                }

                textMeshPro.text = sb.ToString();
                yield return null;
            }
        }

        private IEnumerator LetterByLetterAnimation()
        {
            float delay = 0.225f;
            StringBuilder sb = new StringBuilder();

            while (true)
            {
                for (int i = originalText.Length; i > 0; i--)
                {
                    sb.Clear();
                    sb.Append(originalText.Substring(0, i));
                    textMeshPro.text = sb.ToString();
                    yield return new WaitForSeconds(delay);
                }

                for (int i = 0; i <= originalText.Length; i++)
                {
                    sb.Clear();
                    sb.Append(originalText.Substring(0, i));
                    textMeshPro.text = sb.ToString();
                    yield return new WaitForSeconds(delay);
                }
            }
        }

        private IEnumerator BounceAnimation()
        {
            float bounceSpeed = animationSpeed;
            float delay = 0.007f;
            int length = originalText.Length;
            string redColorHex = ColorUtility.ToHtmlStringRGB(Color.red);
            StringBuilder sb = new StringBuilder();

            while (true)
            {
                for (int i = 0; i < length; i++)
                {
                    sb.Clear();
                    for (int j = 0; j < length; j++)
                    {
                        sb.Append(j == i ? $"<color=#{redColorHex}>{originalText[j]}</color>" : originalText[j]);
                    }
                    textMeshPro.text = sb.ToString();
                    yield return new WaitForSeconds(delay);
                }

                for (int i = length - 1; i >= 0; i--)
                {
                    sb.Clear();
                    for (int j = 0; j < length; j++)
                    {
                        sb.Append(j == i ? $"<color=#{redColorHex}>{originalText[j]}</color>" : originalText[j]);
                    }
                    textMeshPro.text = sb.ToString();
                    yield return new WaitForSeconds(delay);
                }
            }
        }
    }
}
