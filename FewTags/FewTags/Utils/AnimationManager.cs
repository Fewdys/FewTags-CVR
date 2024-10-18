using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using MelonLoader;
using System.Linq;

namespace FewTags.Utils
{
    public enum AnimationType
    {
        None,
        Rainbow,
        SmoothRainbow,
        LetterByLetter,
        CYLN, // New animation type
    }

    public class AnimationManager : MonoBehaviour
    {
        public TMP_Text textMeshPro;
        public List<AnimationType> animationTypes = new List<AnimationType>();
        public float animationSpeed = 1f; // Adjust speed as needed
        public float rain = 2.5f;
        public string originalText { get; set; }

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
                MelonLogger.Msg("TextMeshPro component is not assigned.");
                return;
            }

            originalText = textMeshPro.text;
            StartAnimation();
        }

        private void OnEnable()
        {
            StartAnimation(); // Restart animations
        }

        private void OnDisable()
        {
            // Optionally, you can stop animations when disabled
            if (colorCoroutine != null)
            {
                StopCoroutine(colorCoroutine);
                colorCoroutine = null; // Clear reference
            }

            if (contentCoroutine != null)
            {
                StopCoroutine(contentCoroutine);
                contentCoroutine = null; // Clear reference
            }
        }

        public void StartAnimation()
        {
            bool hasColorAnimation = animationTypes.Contains(AnimationType.Rainbow) || animationTypes.Contains(AnimationType.SmoothRainbow) || animationTypes.Contains(AnimationType.CYLN);
            bool hasContentAnimation = animationTypes.Contains(AnimationType.LetterByLetter);

            if (hasColorAnimation)
            {
                if (colorCoroutine != null)
                {
                    StopCoroutine(colorCoroutine);
                }

                if (animationTypes.Contains(AnimationType.CYLN))
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
                var parts = ParseTextWithTags(originalText);

                foreach (var part in parts)
                {
                    if (part.IsStyled)
                    {
                        sb.Append(part.Text);
                    }
                    else
                    {
                        for (int i = 0; i < part.Text.Length; i++)
                        {
                            int colorIndex = (i + Mathf.FloorToInt(time * colorCount)) % colorCount;
                            Color32 color = rainbowColors[colorIndex];
                            sb.Append($"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{part.Text[i]}</color>");
                        }
                    }
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
            var parts = ParseTextWithTags(originalText);

            while (true)
            {
                t += Time.deltaTime * hueShiftSpeed;
                if (t > 1) t -= 1;

                sb.Clear();

                foreach (var part in parts)
                {
                    if (part.IsStyled)
                    {
                        sb.Append(part.Text);
                    }
                    else
                    {
                        for (int i = 0; i < part.Text.Length; i++)
                        {
                            float hue = (t + i * 1.0f / part.Text.Length) % 1.0f;
                            Color color = Color.HSVToRGB(hue, 1.0f, 1.0f);
                            sb.Append($"<color=#{ColorUtility.ToHtmlStringRGBA(color)}>{part.Text[i]}</color>");
                        }
                    }
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

        // Parse the text to separate styled and plain text
        private List<TextPart> ParseTextWithTags(string text)
        {
            var parts = new List<TextPart>();
            var matches = Regex.Matches(text, @"<[^>]+>|[^<]+");

            foreach (Match match in matches)
            {
                var part = new TextPart
                {
                    Text = match.Value,
                    IsStyled = match.Value.StartsWith("<") && match.Value.EndsWith(">"),
                };
                parts.Add(part);
                //MelonLogger.Msg($"Part: {part.Text}, IsStyled: {part.IsStyled}");
            }

            return parts;
        }




        private IEnumerator BounceAnimation()
        {
            float bounceSpeed = animationSpeed;
            float delay = 0.007f;
            int length = originalText.Length;
            string redColorHex = ColorUtility.ToHtmlStringRGB(Color.red);
            var parts = ParseTextWithTags(originalText);

            // Extract visible text length, excluding tags
            int visibleTextLength = 0;
            foreach (var part in parts)
            {
                if (!part.IsStyled)
                {
                    visibleTextLength += part.Text.Length;
                }
            }

            while (true)
            {
                for (int i = 0; i < visibleTextLength; i++)
                {
                    StringBuilder sb = new StringBuilder();

                    int charIndex = 0;

                    foreach (var part in parts)
                    {
                        if (part.IsStyled)
                        {
                            sb.Append(part.Text);
                        }
                        else
                        {
                            // Only animate visible characters
                            for (int j = 0; j < part.Text.Length; j++)
                            {
                                if (charIndex == i)
                                {
                                    sb.Append($"<color=#{redColorHex}>{part.Text[j]}</color>");
                                }
                                else
                                {
                                    sb.Append(part.Text[j]);
                                }
                                charIndex++;
                            }
                        }
                    }

                    textMeshPro.text = sb.ToString();
                    yield return new WaitForSeconds(delay);
                }

                for (int i = visibleTextLength - 1; i >= 0; i--)
                {
                    StringBuilder sb = new StringBuilder();

                    int charIndex = 0;

                    foreach (var part in parts)
                    {
                        if (part.IsStyled)
                        {
                            sb.Append(part.Text);
                        }
                        else
                        {
                            // Only animate visible characters
                            for (int j = 0; j < part.Text.Length; j++)
                            {
                                if (charIndex == i)
                                {
                                    sb.Append($"<color=#{redColorHex}>{part.Text[j]}</color>");
                                }
                                else
                                {
                                    sb.Append(part.Text[j]);
                                }
                                charIndex++;
                            }
                        }
                    }

                    textMeshPro.text = sb.ToString();
                    yield return new WaitForSeconds(delay);
                }
            }
        }

        // Helper class to store text parts
        private class TextPart
        {
            public string Text { get; set; }
            public bool IsStyled { get; set; }
        }
    }
}
