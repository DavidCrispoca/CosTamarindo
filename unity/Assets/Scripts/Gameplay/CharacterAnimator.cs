using UnityEngine;
using CosTamarindo.Core;

namespace CosTamarindo.Gameplay
{
    /// <summary>
    /// Anima el personaje con el único sprite disponible en Figma (la Palenquera
    /// con la canasta en la cabeza). No hay hoja de sprites, así que el movimiento
    /// se construye en procedimiento: paso, inclinación, aplaste y respiración.
    ///
    /// Vive en el hijo <c>Visual</c> del jugador para no pelear con el movimiento
    /// que aplica <see cref="PlayerController"/> sobre el objeto padre.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CharacterAnimator : MonoBehaviour
    {
        [Header("Referencias")]
        [SerializeField] private PlayerController player;
        [SerializeField] private SpriteRenderer spriteRenderer;

        [Header("Paso")]
        [Tooltip("Pasos por segundo a velocidad máxima.")]
        [SerializeField] private float stepsPerSecond = 2.4f;
        [Tooltip("Cuánto sube el cuerpo en cada paso, en unidades de mundo.")]
        [SerializeField] private float bobHeight = 0.09f;
        [Tooltip("Inclinación máxima hacia el lado del movimiento, en grados.")]
        [SerializeField] private float tiltDegrees = 7f;
        [Tooltip("Aplaste/estirado vertical máximo durante el paso.")]
        [SerializeField] private float squash = 0.035f;
        [Tooltip("Velocidad a la que la animación entra y sale, en 1/s.")]
        [SerializeField] private float motionSharpness = 9f;
        [Tooltip("Velocidad horizontal (u/s) que se considera caminata plena.")]
        [SerializeField] private float fullSpeed = 8f;

        [Header("Reposo")]
        [SerializeField] private float breathAmplitude = 0.012f;
        [SerializeField] private float breathSpeed = 1.15f;

        [Header("Sombra")]
        [Tooltip("Sombra de contacto bajo los pies, generada en runtime.")]
        [SerializeField] private bool castShadow = true;
        [SerializeField] private float shadowWidthFactor = 0.8f;

        private Vector3 baseLocalPosition;
        private Vector3 baseLocalScale;
        private Transform shadow;
        private float phase;
        private float motion;
        private float facing = 1f;

        /// <summary>0 = quieta, 1 = caminando a plena velocidad.</summary>
        public float Motion => motion;

        private void Awake()
        {
            if (player == null) player = GetComponentInParent<PlayerController>();
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();

            baseLocalPosition = transform.localPosition;
            baseLocalScale = transform.localScale;

            if (castShadow) CreateShadow();
        }

        /// <summary>
        /// La sombra cuelga del objeto raíz (no del Visual) para quedarse en el suelo
        /// mientras el cuerpo sube y se inclina.
        /// </summary>
        private void CreateShadow()
        {
            if (shadow != null || spriteRenderer == null || spriteRenderer.sprite == null) return;
            if (transform.parent == null) return;

            Sprite sprite = PlaceholderSprites.Shadow();
            if (sprite == null) return;

            var go = new GameObject("Shadow");
            go.transform.SetParent(transform.parent, false);
            go.transform.localPosition = new Vector3(0f, 0.02f, 0f);

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = spriteRenderer.sortingOrder - 1;

            float width = Mathf.Max(0.2f, spriteRenderer.bounds.size.x * shadowWidthFactor);
            Vector2 size = sprite.bounds.size;
            go.transform.localScale = new Vector3(
                width / Mathf.Max(0.001f, size.x),
                width * 0.34f / Mathf.Max(0.001f, size.y),
                1f);

            shadow = go.transform;
        }

        private void OnEnable()
        {
            baseLocalPosition = transform.localPosition;
            baseLocalScale = transform.localScale;
        }

        /// <summary>LateUpdate para leer la posición ya movida por el padre.</summary>
        private void LateUpdate()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            bool playing = GameManager.Instance == null || GameManager.Instance.State == GameState.Playing;
            float speed = playing && player != null ? Mathf.Abs(player.HorizontalVelocity) : 0f;
            float target = Mathf.Clamp01(speed / Mathf.Max(0.01f, fullSpeed));

            motion = Mathf.Lerp(motion, target, 1f - Mathf.Exp(-motionSharpness * dt));

            if (speed > 0.05f) facing = Mathf.Sign(player.HorizontalVelocity);

            // El paso avanza con la velocidad real: si frena, se detiene en el sitio.
            phase += motion * stepsPerSecond * Mathf.PI * 2f * dt;

            float step = Mathf.Abs(Mathf.Sin(phase));
            float bob = step * bobHeight * motion;
            float tilt = -facing * tiltDegrees * motion;
            float breathe = Mathf.Sin(Time.time * breathSpeed) * breathAmplitude * (1f - motion);

            // Estirar en vertical cuando el cuerpo sube y aplastar al apoyar.
            float stretch = squash * motion * (0.5f - step);

            transform.localPosition = baseLocalPosition + new Vector3(0f, bob, 0f);
            transform.localRotation = Quaternion.Euler(0f, 0f, tilt);
            transform.localScale = new Vector3(
                baseLocalScale.x * (1f - stretch),
                baseLocalScale.y * (1f + stretch + breathe),
                baseLocalScale.z);

            if (spriteRenderer != null) spriteRenderer.flipX = facing < 0f;
        }
    }
}
