using UnityEngine;

namespace CosTamarindo.Gameplay
{
    /// <summary>
    /// Escala el fondo para cubrir toda la cámara ortográfica sin deformarlo
    /// (recorta lo que sobra) y se recentra si cambia la resolución o el aspecto.
    /// Se usa con el fondo de playa exportado de Figma.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    [DisallowMultipleComponent]
    public sealed class BackgroundFitter : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;
        [Tooltip("Margen extra para que nunca asome el color de fondo de la cámara.")]
        [SerializeField] private float overscan = 1.02f;
        [Tooltip("Distancia al plano de la cámara. Positivo = detrás del resto de sprites.")]
        [SerializeField] private float depth = 5f;

        private SpriteRenderer spriteRenderer;
        private int lastWidth;
        private int lastHeight;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (targetCamera == null) targetCamera = Camera.main;
            Fit();
        }

        private void LateUpdate()
        {
            if (Screen.width == lastWidth && Screen.height == lastHeight) return;
            Fit();
        }

        /// <summary>Recalcula escala y posición. Es público para poder forzarlo desde un test.</summary>
        public void Fit()
        {
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
            if (targetCamera == null) targetCamera = Camera.main;
            if (spriteRenderer == null || spriteRenderer.sprite == null || targetCamera == null) return;

            lastWidth = Screen.width;
            lastHeight = Screen.height;

            float worldHeight = targetCamera.orthographicSize * 2f;
            float worldWidth = worldHeight * targetCamera.aspect;

            Vector2 spriteSize = spriteRenderer.sprite.bounds.size;
            if (spriteSize.x <= 0.001f || spriteSize.y <= 0.001f) return;

            // "Cover": se toma el mayor de los dos factores para no dejar huecos.
            float scale = Mathf.Max(worldWidth / spriteSize.x, worldHeight / spriteSize.y) * overscan;

            Vector3 cameraPosition = targetCamera.transform.position;
            transform.position = new Vector3(cameraPosition.x, cameraPosition.y, depth);
            transform.localScale = new Vector3(scale, scale, 1f);
        }
    }
}
