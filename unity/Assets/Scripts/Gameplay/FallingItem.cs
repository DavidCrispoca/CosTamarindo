using UnityEngine;
using CosTamarindo.Core;

namespace CosTamarindo.Gameplay
{
    /// <summary>
    /// Ítem que cae. El avance lo impulsa <see cref="ItemSpawner"/> para que toda la
    /// física del juego se resuelva en un único punto y sea determinista.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class FallingItem : MonoBehaviour
    {
        private SpriteRenderer spriteRenderer;
        private float spinSpeed;

        public ItemDef Definition { get; private set; }
        public float FallSpeed { get; private set; }
        public float PreviousY { get; private set; }

        private void Awake() => spriteRenderer = GetComponent<SpriteRenderer>();

        public void Initialize(ItemDef def, float speed, float spin)
        {
            Definition = def;
            FallSpeed = speed;
            spinSpeed = spin;
            PreviousY = transform.position.y;

            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
            spriteRenderer.color = TintFor(def.Kind);
        }

        /// <summary>Avanza un fotograma. Devuelve true si el ítem salió por abajo.</summary>
        public bool Advance(float deltaTime, float despawnY)
        {
            PreviousY = transform.position.y;
            transform.position += Vector3.down * (FallSpeed * deltaTime);
            transform.Rotate(0f, 0f, spinSpeed * deltaTime);
            return transform.position.y < despawnY;
        }

        private static Color TintFor(ItemKind kind) => kind switch
        {
            ItemKind.Danger => new Color(1f, 0.80f, 0.80f),
            ItemKind.Trash => new Color(0.82f, 0.82f, 0.82f),
            _ => Color.white
        };
    }
}
