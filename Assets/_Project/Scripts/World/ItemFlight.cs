using UnityEngine;

namespace DeadAir.World
{
    public sealed class ItemFlight : MonoBehaviour
    {
        private enum Direction
        {
            ToBackpack,
            ToGround
        }

        private Direction direction;
        private Transform backpack;
        private Vector3 startPosition;
        private Vector3 groundPosition;
        private Vector3 normalScale;
        private float duration;
        private float arcHeight;
        private float timePassed;
        private Collider[] colliders;

        // Запускает полёт к рюкзаку. Рюкзак движется вместе с игроком, поэтому цель берётся каждый кадр.
        public static void ToCollector(GameObject item, Transform collector, float duration = 0.45f, float arcHeight = 0.35f)
        {
            ItemFlight flight = StartFlight(item, duration, arcHeight);
            flight.direction = Direction.ToBackpack;
            flight.backpack = collector;
        }

        // Запускает полёт из точки from (рюкзак) в точку to (земля).
        public static void Drop(GameObject item, Vector3 from, Vector3 to, float duration = 0.35f, float arcHeight = 0.4f)
        {
            ItemFlight flight = StartFlight(item, duration, arcHeight);
            flight.direction = Direction.ToGround;
            flight.startPosition = from;
            flight.groundPosition = to;

            item.transform.position = from;
            item.transform.localScale = Vector3.zero;
        }

        private static ItemFlight StartFlight(GameObject item, float duration, float arcHeight)
        {
            ItemFlight flight = item.AddComponent<ItemFlight>();
            flight.startPosition = item.transform.position;
            flight.normalScale = item.transform.localScale;
            flight.duration = duration;
            flight.arcHeight = arcHeight;

            flight.colliders = item.GetComponentsInChildren<Collider>();
            foreach (Collider collider in flight.colliders)
                collider.enabled = false;

            return flight;
        }

        private void Update()
        {
            timePassed += Time.deltaTime;

            // progress: 0 — полёт только начался, 1 — полёт закончен.
            float progress = Mathf.Clamp01(timePassed / duration);

            if (direction == Direction.ToBackpack)
                UpdateFlightToBackpack(progress);
            else
                UpdateFlightToGround(progress);
        }

        private void UpdateFlightToBackpack(float progress)
        {
            // Игрок мог исчезнуть вместе с рюкзаком — тогда просто убираем предмет.
            if (backpack == null)
            {
                Destroy(gameObject);
                return;
            }

            // Квадрат прогресса: сначала предмет движется медленно, потом всё быстрее — будто его притягивает.
            float pathProgress = progress * progress;
            Vector3 position = Vector3.Lerp(startPosition, backpack.position, pathProgress);
            position.y += GetArcHeight(progress);
            transform.position = position;

            // Первые 65% пути размер обычный, на последних 35% предмет уменьшается до нуля.
            // InverseLerp(0.65, 1, progress) даёт 0 при progress = 0.65 и 1 при progress = 1.
            float shrinkProgress = Mathf.InverseLerp(0.65f, 1f, progress);
            transform.localScale = normalScale * (1f - Smooth(shrinkProgress));

            if (progress >= 1f)
                Destroy(gameObject);
        }

        private void UpdateFlightToGround(float progress)
        {
            // Обратная кривая: в начале быстро, в конце медленно — предмет мягко опускается на землю.
            float pathProgress = 1f - (1f - progress) * (1f - progress);
            Vector3 position = Vector3.Lerp(startPosition, groundPosition, pathProgress);
            position.y += GetArcHeight(progress);
            transform.position = position;

            // За первые 40% пути предмет вырастает из нуля до обычного размера.
            float growProgress = Mathf.InverseLerp(0f, 0.4f, progress);
            transform.localScale = normalScale * Smooth(growProgress);

            if (progress < 1f)
                return;

            // Предмет лёг на землю: снова включаем коллайдеры, чтобы его можно было подобрать.
            foreach (Collider collider in colliders)
                collider.enabled = true;
            Destroy(this);
        }

        // Высота над прямой линией полёта: 0 в начале, максимум в середине, 0 в конце (половина синусоиды).
        private float GetArcHeight(float progress)
        {
            return arcHeight * Mathf.Sin(progress * Mathf.PI);
        }

        // Плавная кривая от 0 до 1: медленно в начале и в конце, быстро в середине.
        private static float Smooth(float value)
        {
            return Mathf.SmoothStep(0f, 1f, value);
        }
    }
}
