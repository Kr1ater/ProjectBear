using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace DeadAir.UI
{
    // Лента коротких сообщений в углу экрана: подбор, перегруз, разборка, выброс.
    // Строки заранее лежат на сцене выключенными. Скрипт ничего не создаёт: включает свободную строку,
    // пишет в неё текст, а когда она погасла — выключает. Новые строки появляются снизу.
    public sealed class MessageFeed : MonoBehaviour
    {
        [Tooltip("Готовые строки ленты на сцене. Сколько их, столько сообщений видно одновременно")]
        [SerializeField] private TMP_Text[] lines;
        [SerializeField] private float lifetime = 3.5f;
        [SerializeField] private float fadeDuration = 0.6f;

        // Строка на экране и время, когда она появилась.
        private class Line
        {
            public TMP_Text Text;
            public float ShownAt;
        }

        // Видимые строки от самой старой к самой новой.
        private readonly List<Line> visibleLines = new();

        private void Awake()
        {
            foreach (TMP_Text line in lines)
                line.gameObject.SetActive(false);
        }

        // attention = true — сообщение янтарным цветом (перегруз, частичный подбор, ошибка разборки).
        public void Show(string message, bool attention = false)
        {
            TMP_Text text = FindHiddenLine();

            // Все строки заняты — убираем самую старую и пишем в неё.
            if (text == null)
            {
                text = visibleLines[0].Text;
                HideLine(0);
            }

            text.text = message;
            text.alpha = 1f;
            if (attention)
                text.color = UiPalette.Amber;
            else
                text.color = UiPalette.Text;

            // Последний ребёнок контейнера рисуется в самом низу ленты.
            text.transform.SetAsLastSibling();
            text.gameObject.SetActive(true);

            Line line = new Line();
            line.Text = text;
            line.ShownAt = Time.unscaledTime;
            visibleLines.Add(line);
        }

        private void Update()
        {
            // Идём с конца списка, потому что внутри цикла можем убирать строки.
            for (int i = visibleLines.Count - 1; i >= 0; i--)
            {
                Line line = visibleLines[i];
                float age = Time.unscaledTime - line.ShownAt;

                if (age >= lifetime)
                {
                    HideLine(i);
                    continue;
                }

                // Последние fadeDuration секунд жизни строка плавно гаснет от 1 до 0.
                float fadeStart = lifetime - fadeDuration;
                if (age > fadeStart)
                    line.Text.alpha = 1f - (age - fadeStart) / fadeDuration;
            }
        }

        private TMP_Text FindHiddenLine()
        {
            foreach (TMP_Text line in lines)
            {
                if (!line.gameObject.activeSelf)
                    return line;
            }
            return null;
        }

        private void HideLine(int index)
        {
            visibleLines[index].Text.gameObject.SetActive(false);
            visibleLines.RemoveAt(index);
        }
    }
}
