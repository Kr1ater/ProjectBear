using UnityEngine;

namespace DeadAir.World
{
    // Короткий полёт предмета. При подборе предмет притягивается к рюкзаку и в конце сжимается в ноль,
    // при выбросе вылетает из рюкзака на землю и вырастает из нуля. Пока предмет летит, его нельзя подобрать.
    public sealed class ItemFlight : MonoBehaviour
    {
        private bool toCollector;
        private Transform collector;
        private Vector3 from;
        private Vector3 to;
        private Vector3 baseScale;
        private float duration;
        private float arcHeight;
        private float elapsed;
        private Collider[] colliders;

        // Подбор: цель — точка на рюкзаке, она движется вместе с игроком. В конце объект уничтожается.
        public static void ToCollector(GameObject item, Transform collector, float duration = 0.45f, float arcHeight = 0.35f)
        {
            var flight = Begin(item, duration, arcHeight);
            flight.toCollector = true;
            flight.collector = collector;
        }

        // Выброс: из точки from на землю в точку to. В конце коллайдеры снова включаются.
        public static void Drop(GameObject item, Vector3 from, Vector3 to, float duration = 0.35f, float arcHeight = 0.4f)
        {
            var flight = Begin(item, duration, arcHeight);
            flight.from = from;
            flight.to = to;
            item.transform.position = from;
            item.transform.localScale = Vector3.zero;
        }

        private static ItemFlight Begin(GameObject item, float duration, float arcHeight)
        {
            var flight = item.AddComponent<ItemFlight>();
            flight.from = item.transform.position;
            flight.baseScale = item.transform.localScale;
            flight.duration = duration;
            flight.arcHeight = arcHeight;
            flight.colliders = item.GetComponentsInChildren<Collider>();
            foreach (var collider in flight.colliders)
                collider.enabled = false;
            return flight;
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float arc = arcHeight * Mathf.Sin(t * Mathf.PI);

            if (!toCollector)
                FlyToGround(t, arc);
            else if (collector != null)
                FlyToCollector(t, arc);
            else
                Destroy(gameObject); // рюкзак исчез вместе с игроком
        }

        private void FlyToCollector(float t, float arc)
        {
            // Квадратичный разгон: сначала предмет нехотя отрывается, потом его тянет всё быстрее.
            float pull = t * t;
            transform.position = Vector3.Lerp(from, collector.position, pull) + Vector3.up * arc;
            // Последнюю треть пути предмет сжимается в ноль.
            transform.localScale = baseScale * Mathf.SmoothStep(1f, 0f, Mathf.InverseLerp(0.65f, 1f, t));

            if (t >= 1f)
                Destroy(gameObject);
        }

        private void FlyToGround(float t, float arc)
        {
            float ease = 1f - (1f - t) * (1f - t);
            transform.position = Vector3.Lerp(from, to, ease) + Vector3.up * arc;
            transform.localScale = baseScale * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0f, 0.4f, t));

            if (t < 1f)
                return;
            foreach (var collider in colliders)
                collider.enabled = true;
            Destroy(this);
        }
    }
}
