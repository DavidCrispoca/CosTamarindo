using UnityEngine;
using CosTamarindo.Core;

namespace CosTamarindo.Gameplay
{
    /// <summary>
    /// Sprites de relleno generados en memoria mientras el arte definitivo de Figma
    /// no está disponible. Permiten jugar y probar el bucle completo sin depender
    /// de la cuota de exportación del diseño.
    /// </summary>
    public static class PlaceholderSprites
    {
        private const int Size = 128;

        private static Sprite good;
        private static Sprite danger;
        private static Sprite trash;

        public static Sprite For(ItemKind kind)
        {
            switch (kind)
            {
                case ItemKind.Danger:
                    return danger != null ? danger : (danger = Circle(new Color(0.88f, 0.32f, 0.32f)));
                case ItemKind.Trash:
                    return trash != null ? trash : (trash = Circle(new Color(0.45f, 0.45f, 0.45f)));
                default:
                    return good != null ? good : (good = Circle(new Color(0.98f, 0.78f, 0.28f)));
            }
        }

        private static Sprite Circle(Color color)
        {
            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                name = "Placeholder_" + ColorUtility.ToHtmlStringRGB(color)
            };

            float radius = Size * 0.5f - 1f;
            var center = new Vector2(Size * 0.5f, Size * 0.5f);
            var pixels = new Color[Size * Size];

            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                    float alpha = Mathf.Clamp01(radius - d); // borde suavizado de 1 px
                    pixels[y * Size + x] = new Color(color.r, color.g, color.b, alpha);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();

            return Sprite.Create(tex, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), Size);
        }
    }
}
