using UnityEngine;
using TMPro;
using System.Collections;

namespace FewTags.Utils
{
    public enum AnimationType
    {
        Rainbow,
        SmoothRainbow,
        LetterByLetter,
        SmoothRainbowAndLetterByLetter,
        RainbowAndLetterByLetter // New animation type
    }

    public class AnimationManager : MonoBehaviour
    {
        public TextMeshProUGUI textMeshPro;
        public AnimationType currentAnimationType;
        public float animationSpeed = 0.1f; // Adjust speed as needed
        public float gradientSpeed = 0.5f; // Speed of the gradient transition
        public Color32[] rainbowColors;

        private Coroutine currentAnimation;

        private void Start()
        {
            StartAnimation();
        }

        public void StartAnimation()
        {
            // Stop any currently running animation
            if (currentAnimation != null)
            {
                StopCoroutine(currentAnimation);
            }

            switch (currentAnimationType)
            {
                case AnimationType.Rainbow:
                    currentAnimation = StartCoroutine(RainbowTagAnimation());
                    break;
                case AnimationType.SmoothRainbow:
                    currentAnimation = StartCoroutine(SmoothRainbowAnimation());
                    break;
                case AnimationType.LetterByLetter:
                    currentAnimation = StartCoroutine(LetterByLetterAnimation());
                    break;
                case AnimationType.SmoothRainbowAndLetterByLetter:
                    currentAnimation = StartCoroutine(SmoothRainbowAndLetterByLetterAnimation());
                    break;
                case AnimationType.RainbowAndLetterByLetter:
                    currentAnimation = StartCoroutine(RainbowAndLetterByLetterAnimation());
                    break;
            }
        }

        public IEnumerator RainbowTagAnimation()
        {
            while (true)
            {
                for (int i = 0; i < textMeshPro.text.Length; i++)
                {
                    string coloredText = "";
                    for (int j = 0; j < textMeshPro.text.Length; j++)
                    {
                        var color = rainbowColors[(i + j) % rainbowColors.Length];
                        coloredText += $"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{textMeshPro.text[j]}</color>";
                    }
                    textMeshPro.text = coloredText;
                    yield return new WaitForSeconds(animationSpeed);
                }
            }
        }

        public IEnumerator SmoothRainbowAnimation()
        {
            float time = 0f;
            while (true)
            {
                time += Time.deltaTime * gradientSpeed;
                string coloredText = "";
                float gradientPosition = time % 1f; // Loop the gradient

                for (int i = 0; i < textMeshPro.text.Length; i++)
                {
                    float position = (i / (float)textMeshPro.text.Length + gradientPosition) % 1f;
                    Color color = Color.HSVToRGB(position, 1f, 1f);
                    coloredText += $"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{textMeshPro.text[i]}</color>";
                }

                textMeshPro.text = coloredText;
                yield return null;
            }
        }

        public IEnumerator LetterByLetterAnimation()
        {
            string fullText = textMeshPro.text;
            while (true)
            {
                // Remove letters one by one, keeping at least one letter
                for (int i = fullText.Length; i > 1; i--)
                {
                    textMeshPro.text = fullText.Substring(0, i - 1);
                    yield return new WaitForSeconds(animationSpeed);
                }

                // Ensure at least one letter is visible
                textMeshPro.text = fullText.Substring(0, 1);
                yield return new WaitForSeconds(animationSpeed);

                // Add back letters one by one
                for (int i = 1; i <= fullText.Length; i++)
                {
                    textMeshPro.text = fullText.Substring(0, i);
                    yield return new WaitForSeconds(animationSpeed);
                }
            }
        }

        public IEnumerator SmoothRainbowAndLetterByLetterAnimation()
        {
            string fullText = textMeshPro.text;

            while (true)
            {
                // Remove letters one by one, keeping at least one letter
                for (int i = fullText.Length; i > 1; i--)
                {
                    textMeshPro.text = fullText.Substring(0, i - 1);
                    ApplySmoothRainbowEffect();
                    yield return new WaitForSeconds(animationSpeed);
                }

                // Ensure at least one letter is visible
                textMeshPro.text = fullText.Substring(0, 1);
                ApplySmoothRainbowEffect();
                yield return new WaitForSeconds(animationSpeed);

                // Add back letters one by one
                for (int i = 1; i <= fullText.Length; i++)
                {
                    textMeshPro.text = fullText.Substring(0, i);
                    ApplySmoothRainbowEffect();
                    yield return new WaitForSeconds(animationSpeed);
                }
            }
        }

        public IEnumerator RainbowAndLetterByLetterAnimation()
        {
            string fullText = textMeshPro.text;
            int textLength = fullText.Length;

            while (true)
            {
                // Remove letters one by one
                for (int i = textLength; i > 1; i--)
                {
                    textMeshPro.text = fullText.Substring(0, i - 1);
                    ApplyRainbowEffect();
                    yield return new WaitForSeconds(animationSpeed);
                }

                // Ensure at least one letter is visible
                textMeshPro.text = fullText.Substring(0, 1);
                ApplyRainbowEffect();
                yield return new WaitForSeconds(animationSpeed);

                // Add back letters one by one
                for (int i = 1; i <= textLength; i++)
                {
                    textMeshPro.text = fullText.Substring(0, i);
                    ApplyRainbowEffect();
                    yield return new WaitForSeconds(animationSpeed);
                }
            }
        }

        public void ApplyRainbowEffect()
        {
            float time = Time.time * gradientSpeed;
            string coloredText = "";
            for (int i = 0; i < textMeshPro.text.Length; i++)
            {
                Color color = Color.HSVToRGB((time + i / (float)textMeshPro.text.Length) % 1f, 1f, 1f);
                coloredText += $"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{textMeshPro.text[i]}</color>";
            }
            textMeshPro.text = coloredText;
        }

        public void ApplySmoothRainbowEffect()
        {
            float time = Time.time * gradientSpeed;
            string coloredText = "";
            for (int i = 0; i < textMeshPro.text.Length; i++)
            {
                Color color = Color.HSVToRGB((time + i / (float)textMeshPro.text.Length) % 1f, 1f, 1f);
                coloredText += $"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{textMeshPro.text[i]}</color>";
            }
            textMeshPro.text = coloredText;
        }
    }

}
