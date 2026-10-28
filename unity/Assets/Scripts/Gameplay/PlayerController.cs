using UnityEngine;
using UnityEngine.InputSystem;
using CosTamarindo.Core;

namespace CosTamarindo.Gameplay
{
    /// <summary>
    /// Controla la canasta del jugador: teclado (A/D, flechas), ratón y pantalla táctil.
    /// El movimiento es horizontal y se limita al ancho visible de la cámara.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerController : MonoBehaviour
    {
        [Header("Movimiento")]
        [SerializeField] private float keyboardSpeed = 14f;
        [SerializeField] private float followSharpness = 18f;
        [SerializeField] private float margin = 0.9f;

        [Header("Captura")]
        [Tooltip("Semi-ancho de la zona de captura, en unidades de mundo.")]
        [SerializeField] private float catchHalfWidth = 0.95f;

        private Camera cam;
        private float minX = -8f;
        private float maxX = 8f;
        private float targetX;
        private bool hasTarget;

        public float CatchHalfWidth => catchHalfWidth;
        /// <summary>Altura a la que un ítem se considera capturado.</summary>
        public float CatchHeight => transform.position.y + 0.55f;

        private void Awake()
        {
            cam = Camera.main;
            RecalculateBounds();
            targetX = transform.position.x;

            // El sprite de relleno se genera en tiempo de ejecución: un Sprite creado
            // por código no se puede serializar dentro de la escena.
            var renderer = GetComponent<SpriteRenderer>();
            if (renderer != null && renderer.sprite == null)
                renderer.sprite = PlaceholderSprites.For(ItemKind.Good);
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
            p.x = Mathf.Lerp(p.x, Mathf.Clamp(targetX, minX, maxX), 1f - Mathf.Exp(-followSharpness * Time.deltaTime));
            transform.position = p;
        }

        private void OnEnable() => RecalculateBounds();

#if UNITY_EDITOR
        private void OnValidate()
        {
            catchHalfWidth = Mathf.Max(0.1f, catchHalfWidth);
        }
#endif
    }
}
