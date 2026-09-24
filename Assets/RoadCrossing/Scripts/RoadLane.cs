using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RoadCrossing
{
    public sealed class RoadLane : MonoBehaviour
    {
        private static readonly string[] CarSprites =
        {
            "RoadCrossing/CarRed",
            "RoadCrossing/CarYellow",
            "RoadCrossing/CarPurple",
            "RoadCrossing/CarGreen"
        };
        private readonly List<GameObject> cars = new();
        private int row;
        private float laneY;
        private int direction;
        private float speed;
        private float timer;
        private bool blocked;

        public bool IsBlocked => blocked;

        public void Configure(int laneRow, float y, int travelDirection, float travelSpeed)
        {
            row = laneRow;
            laneY = y;
            direction = travelDirection;
            speed = travelSpeed;
            timer = Random.Range(0.25f, 1.2f);
        }

        private void Update()
        {
            if (blocked || RoadCrossingBootstrap.Instance == null || !RoadCrossingBootstrap.Instance.CanRun) return;
            timer -= Time.deltaTime;
            if (timer <= 0f)
            {
                SpawnCar();
                float baseInterval = Random.Range(1.35f, 2.25f) - Mathf.Min(row * 0.018f, 0.28f);
                timer = baseInterval / RoadCrossingBootstrap.Instance.TrafficSpawnMultiplier;
            }
        }

        private void SpawnCar()
        {
            var car = new GameObject("Car");
            car.transform.SetParent(transform);
            car.transform.position = new Vector3(-direction * 5.3f, laneY, 0f);
            var renderer = car.AddComponent<SpriteRenderer>();
            renderer.sprite = Resources.Load<Sprite>(CarSprites[Random.Range(0, CarSprites.Length)]);
            renderer.sortingOrder = 5;
            renderer.flipX = direction < 0;
            renderer.color = Color.white;
            if (renderer.sprite != null)
            {
                Vector2 bounds = renderer.sprite.bounds.size;
                car.transform.localScale = new Vector3(2.15f / bounds.x, 1.05f / bounds.y, 1f);
            }
            else
            {
                renderer.sprite = RoadCrossingBootstrap.SolidSprite;
                car.transform.localScale = new Vector3(1.7f, 0.72f, 1f);
            }
            var body = car.AddComponent<RoadCar>();
            body.Configure(this, direction, speed + Random.Range(-0.35f, 0.55f));
            cars.Add(car);
        }

        public void Block()
        {
            if (blocked) return;
            blocked = true;
            foreach (var car in cars) if (car != null) Destroy(car);
            cars.Clear();
            Sprite barricadeSprite = Resources.Load<Sprite>("RoadCrossing/RoadBarricade");
            for (int side = -1; side <= 1; side += 2)
            {
                var barrier = new GameObject("Road Closed Barricade");
                barrier.transform.SetParent(transform);
                barrier.transform.position = new Vector3(side * 2.45f, laneY, 0f);
                var renderer = barrier.AddComponent<SpriteRenderer>();
                renderer.sprite = barricadeSprite;
                renderer.sortingOrder = 9;
                if (barricadeSprite != null)
                {
                    Vector2 bounds = barricadeSprite.bounds.size;
                    barrier.transform.localScale = new Vector3(1.8f / bounds.x, 0.9f / bounds.y, 1f);
                }
                else
                {
                    renderer.sprite = RoadCrossingBootstrap.SolidSprite;
                    renderer.color = new Color(0.92f, 0.16f, 0.12f);
                    barrier.transform.localScale = new Vector3(1.8f, 0.35f, 1f);
                }
            }
        }

        public void ResetLane()
        {
            StopAllCoroutines();
            foreach (Transform child in transform) Destroy(child.gameObject);
            cars.Clear();
            blocked = false;
            timer = Random.Range(0.2f, 1.1f);
        }

        public void Forget(GameObject car) => cars.Remove(car);

    }

    public sealed class RoadCar : MonoBehaviour
    {
        private RoadLane lane;
        private int direction;
        private float speed;

        public void Configure(RoadLane owner, int travelDirection, float travelSpeed)
        {
            lane = owner;
            direction = travelDirection;
            speed = travelSpeed;
        }

        private void Update()
        {
            float difficulty = RoadCrossingBootstrap.Instance != null
                ? RoadCrossingBootstrap.Instance.TrafficSpeedMultiplier
                : 1f;
            transform.position += Vector3.right * (direction * speed * difficulty * Time.deltaTime);
            if (Mathf.Abs(transform.position.x) <= 6.2f) return;
            lane?.Forget(gameObject);
            Destroy(gameObject);
        }
    }
}
