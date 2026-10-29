using UnityEngine;
using UnityEngine.InputSystem;
using CosTamarindo.Core;

namespace CosTamarindo.Gameplay
{
    /// <summary>
    /// Controla a la Palenquera: teclado (A/D, flechas), ratón y pantalla táctil.
    /// El movimiento es horizontal y se limita al ancho visible de la cámara.
    /// La zona de captura se coloca a la altura de la canasta que lleva en la cabeza.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerController : MonoBehaviour
    {
        [Header("Movimiento")]
        [SerializeField] private float keyboardSpeed = 14f;
        [SerializeField] private float followSharpness = 18f;
        [SerializeField] private float margin = 1.35f;

        [Header("Captura")]
        [Tooltip("Semi-ancho de la zona de captura, en unidades de mundo.")]
        [SerializeField] private float catchHalfWidth = 0.75f;
        [Tooltip("Distancia bajo el borde superior del sprite: deja la línea de captura en la canasta.")]
        [SerializeField] private float catchInset = 0.28f;

        [Header("Referencias")]
        [Tooltip("Sprite del personaje. Si falta, se busca el hijo 'Visual'.")]
        [SerializeField] private SpriteRenderer visual;

        private Camera cam;
        private float minX = -8f;
        private float maxX = 8f;
        private float targetX;
        private bool hasTarget;
        private float catchHeight = -2.5f;

        public float CatchHalfWidth => catchHalfWidth;
        /// <summary>Altura a la que un ítem se considera capturado (borde de la canasta).</summary>
        public float CatchHeight => catchHeight;
        /// <summary>Velocidad horizontal actual, en unidades de mundo por segundo.</summary>
        public float HorizontalVelocity { get; private set; }

        private void Awake()
        {
            cam = Camera.main;
            RecalculateBounds();
            targetX = transform.position.x;

            // El arte va en el hijo "Visual"; si falta, se usa el relleno procedural
            // (un Sprite creado por código no se puede serializar en la escena).
            // Se busca por nombre para no confundirlo con la sombra, que también
            // es un SpriteRenderer hijo.
            if (visual == null)
            {
                var visualTransform = transform.Find("Visual");
                visual = visualTransform != null
                    ? visualTransform.GetComponent<SpriteRenderer>()
                    : GetComponentInChildren<SpriteRenderer>();
            }

            if (visual != null && visual.sprite == null)
                visual.sprite = PlaceholderSprites.For(ItemKind.Good);
        }

        private void Start()
        {
            RecalculateCatchLine();
        }

        /// <summary>
        /// La línea de captura sale del sprite real, así que si el arte cambia de
        /// tamaño la canasta sigue quedando en el sitio correcto.
        /// </summary>
        private void RecalculateCatchLine()
        {
            if (visual != null && visual.sprite != null)
                catchHeight = visual.bounds.max.y - catchInset;
            else
                catchHeight = transform.position.y + 0.55f;
        }

        private void RecalculateBounds()
        {
            if (cam == null) return;

            float halfHeight = cam.orthographicSize;
            float halfWidth = halfHeight * cam.aspect;
            minX = -halfWidth + margin;
            maxX = halfWidth - margin;
        }

        private void Update()
        {
            if (GameManager.Instance != null && GameManager.Instance.State != GameState.Playing)
                return;

            HandleInput();
            ApplyMovement();
        }

        private void HandleInput()
        {
            var keyboard = Keyboard.current;
            float axis = 0f;
            bool keyboardUsed = false;

            if (keyboard != null)
            {
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) { axis -= 1f; keyboardUsed = true; }
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) { axis += 1f; keyboardUsed = true; }
            }

            if (keyboardUsed)
            {
                targetX = Mathf.Clamp(transform.position.x + axis * keyboardSpeed * Time.deltaTime, minX, maxX);
                hasTarget = true;
                return;
            }

            // Ratón sostenido o dedo en pantalla: la canasta sigue al puntero.
            Vector2? pointer = null;
            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.isPressed)
                pointer = mouse.position.ReadValue();

            var touch = Touchscreen.current;
            if (touch != null && touch.primaryTouch.press.isPressed)
                pointer = touch.primaryTouch.position.ReadValue();

            if (pointer.HasValue && cam != null)
            {
                Vector3 world = cam.ScreenToWorldPoint(new Vector3(pointer.Value.x, pointer.Value.y, Mathf.Abs(cam.transform.position.z)));
                targetX = Mathf.Clamp(world.x, minX, maxX);
                hasTarget = true;
            }
            else if (!hasTarget)
            {
                targetX = transform.position.x;
            }
        }

        private void ApplyMovement()
        {
            var p = transform.position;
            float previousX = p.x;
            p.x = Mathf.Lerp(p.x, Mathf.Clamp(targetX, minX, maxX), 1f - Mathf.Exp(-followSharpness * Time.deltaTime));
            transform.position = p;

            float dt = Time.deltaTime;
            HorizontalVelocity = dt > 0f ? (p.x - previousX) / dt : 0f;
        }

        private void OnEnable() => RecalculateBounds();

#if UNITY_EDITOR
        private void OnValidate()
        {
            catchHalfWidth = Mathf.Max(0.1f, catchHalfWidth);
            catchInset = Mathf.Max(0f, catchInset);
        }
#endif
    }
}
