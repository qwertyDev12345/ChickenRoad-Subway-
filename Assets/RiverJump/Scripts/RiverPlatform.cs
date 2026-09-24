using UnityEngine;

namespace RiverJump
{
    public sealed class RiverPlatform : MonoBehaviour
    {
        public float speed = 1.5f;
        public int difficulty;

        private void Update()
        {
            transform.position += Vector3.right * speed * Time.deltaTime;
            if (speed > 0 && transform.position.x > 7.5f) transform.position += Vector3.left * 15f;
            if (speed < 0 && transform.position.x < -7.5f) transform.position += Vector3.right * 15f;
        }
    }
}
