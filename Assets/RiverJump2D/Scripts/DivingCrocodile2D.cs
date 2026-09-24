using UnityEngine;

namespace RiverJump
{
    public sealed class DivingCrocodile2D : MonoBehaviour
    {
        public float phase;
        private RiverPlatform2D platform;
        private Collider2D platformCollider;
        private SpriteRenderer spriteRenderer;
        private ParticleSystem warningBubbles;
        private Material bubbleMaterial;
        private Vector3 surfacePosition;
        private bool wasWarning;

        private void Start()
        {
            platform = GetComponent<RiverPlatform2D>();
            platformCollider = GetComponent<Collider2D>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            surfacePosition = transform.position;
            CreateWarningBubbles();
        }

        private void Update()
        {
            surfacePosition.x = transform.position.x;
            float cycle = Mathf.Repeat(Time.time + phase, 6.2f);
            bool warning = cycle > 3.7f && cycle <= 4.65f;
            bool submerged = cycle > 4.65f;
            if (warning && !wasWarning) RiverJumpBootstrap.Instance?.PlayCrocodileWarningSound();
            wasWarning = warning;
            if (platform != null) platform.enabled = !submerged;
            if (platformCollider != null) platformCollider.enabled = !submerged;
            if (spriteRenderer != null)
            {
                var color = spriteRenderer.color;
                float warningPulse = warning ? (Mathf.Sin(Time.time * 14f) * 0.5f + 0.5f) : 0f;
                Color warningTint = Color.Lerp(Color.white, new Color(1f, 0.58f, 0.16f), warningPulse * 0.72f);
                color.r = warningTint.r;
                color.g = warningTint.g;
                color.b = warningTint.b;
                color.a = Mathf.MoveTowards(color.a, submerged ? 0.22f : 1f, Time.deltaTime * 3.8f);
                spriteRenderer.color = color;
            }
            bool showBubbles = warning || submerged;
            if (warningBubbles != null)
            {
                if (showBubbles && !warningBubbles.isPlaying) warningBubbles.Play();
                else if (!showBubbles && warningBubbles.isPlaying) warningBubbles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
            var position = transform.position;
            float warningShake = warning ? Mathf.Sin(Time.time * 15f + phase) * 0.055f : 0f;
            position.y = surfacePosition.y + (submerged ? -0.2f : Mathf.Sin(Time.time * 2.1f + phase) * 0.025f + warningShake);
            transform.position = position;
        }

        private void CreateWarningBubbles()
        {
            var bubbleObject = new GameObject("Warning Bubbles");
            bubbleObject.transform.SetParent(transform, false);
            bubbleObject.transform.localPosition = new Vector3(0f, 0.35f, -0.1f);
            warningBubbles = bubbleObject.AddComponent<ParticleSystem>();
            warningBubbles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = warningBubbles.main;
            main.playOnAwake = false;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.45f, 0.85f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.25f, 0.55f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.12f);
            main.startColor = new Color(0.78f, 0.96f, 1f, 0.88f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 24;
            var emission = warningBubbles.emission;
            emission.rateOverTime = 9f;
            var shape = warningBubbles.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(1.5f, 0.08f, 0.05f);
            var particleRenderer = warningBubbles.GetComponent<ParticleSystemRenderer>();
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
            bubbleMaterial = new Material(shader) { color = new Color(0.78f, 0.96f, 1f, 0.88f) };
            particleRenderer.material = bubbleMaterial;
            particleRenderer.sortingOrder = 11;
        }

        private void OnDisable()
        {
            if (warningBubbles != null) warningBubbles.Pause();
        }

        private void OnDestroy()
        {
            if (bubbleMaterial != null) Destroy(bubbleMaterial);
        }
    }
}
