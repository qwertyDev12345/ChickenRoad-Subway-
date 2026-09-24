using UnityEngine;

namespace RiverJump
{
    public sealed class WaterBob : MonoBehaviour
    {
        public float amplitude = 0.04f;
        public float speed = 1f;
        public float phase;
        private float startY;

        private void Awake() => startY = transform.position.y;

        private void Update()
        {
            var position = transform.position;
            position.y = startY + Mathf.Sin(Time.time * speed + phase) * amplitude;
            transform.position = position;
        }
    }
}
