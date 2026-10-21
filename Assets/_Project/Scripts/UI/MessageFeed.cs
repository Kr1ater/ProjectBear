using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace DeadAir.UI
{
    // Лента коротких сообщений в углу экрана: подбор, перегруз, разборка.
    // Новые строки снизу, старые гаснут сами. Строки берутся из пула.
    public sealed class MessageFeed : MonoBehaviour
    {
        [SerializeField] private TMP_Text linePrefab;
        [SerializeField] private RectTransform container;
        [SerializeField, Min(1)] private int maxLines = 4;
        [SerializeField] private float lifetime = 3.5f;
        [SerializeField] private float fadeDuration = 0.6f;

        private readonly List<(TMP_Text text, float shownAt)> active = new();
        private readonly Stack<TMP_Text> pool = new();

        public void Show(string message, bool attention = false)
        {
            if (active.Count >= maxLines)
                Recycle(0);

            var line = pool.Count > 0 ? pool.Pop() : Instantiate(linePrefab, container);
            line.text = message;
            line.color = attention ? UiPalette.Amber : UiPalette.Text;
            line.transform.SetAsLastSibling();
            line.gameObject.SetActive(true);
            active.Add((line, Time.unscaledTime));
        }

        private void Update()
        {
            float now = Time.unscaledTime;
            for (int i = active.Count - 1; i >= 0; i--)
            {
                var (text, shownAt) = active[i];
                float age = now - shownAt;
                if (age >= lifetime)
                {
                    Recycle(i);
                    continue;
                }
                text.alpha = 1f - Mathf.InverseLerp(lifetime - fadeDuration, lifetime, age);
            }
        }

        private void Recycle(int index)
        {
            var text = active[index].text;
            active.RemoveAt(index);
            text.gameObject.SetActive(false);
            pool.Push(text);
        }
    }
}
