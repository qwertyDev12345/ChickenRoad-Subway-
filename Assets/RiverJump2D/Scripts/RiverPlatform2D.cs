using UnityEngine;

namespace RiverJump
{
    public sealed class RiverPlatform2D : MonoBehaviour
    {
        public float speed = 1.5f;
        public float wrapEdge = 5.4f;
        public float wrapDistance = 10.8f;
        public int difficulty;
        public float safeWindowSeconds;

        private void Update()
        {
            transform.position += Vector3.right * speed * Time.deltaTime;
            if (speed > 0f && transform.position.x > wrapEdge) transform.position += Vector3.left * wrapDistance;
            else if (speed < 0f && transform.position.x < -wrapEdge) transform.position += Vector3.right * wrapDistance;
        }
    }
}
