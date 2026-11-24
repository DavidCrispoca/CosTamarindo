using UnityEngine;

namespace CosTamarindo.Core
{
    /// <summary>
    /// Tokens visuales del juego (FE-04).
    ///
    /// Los 17 colores y los 5 estilos de texto salen del archivo de Figma
    /// <c>qI8jr7abLu6tIpL0MnL8Ol</c>. Mientras no exista <c>FIGMA_TOKEN</c> para
    /// exportar los estilos como variables nativas, estos valores son la copia
    /// versionada de la paleta acordada: cualquier ajuste se hace aquí y no en
    /// los constructores de escena ni en los controladores de UI.
    ///
    /// Regla: ningún color de la paleta se escribe con <c>new Color(...)</c> en
    /// el resto del código. Solo se permiten derivados con <see cref="WithAlpha"/>.
    /// </summary>
    public static class Theme
    {
        // ── 17 colores de la paleta ──────────────────────────────────────
        // Arena / papel
        public static readonly Color Sand        = Hex(0xF5DEB5);
        public static readonly Color SandDark    = Hex(0xE4C48C);
        public static readonly Color Cream       = Hex(0xFFF6E6);

        // Madera / tinta
        public static readonly Color Wood        = Hex(0x734A2E);
        public static readonly Color WoodDark    = Hex(0x4E301C);
        public static readonly Color Ink         = Hex(0x3D291A);
        public static readonly Color InkSoft     = Hex(0x6B5744);

        // Mar
        public static readonly Color Sea         = Hex(0x6BC7E6);
        public static readonly Color SeaDeep     = Hex(0x2E8FB5);

        // Acentos
        public static readonly Color Coral       = Hex(0xE8654F);
        public static readonly Color CoralDark   = Hex(0xB8442F);
        public static readonly Color Palm        = Hex(0x2F7D4F);
        public static readonly Color PalmLight   = Hex(0x57A96F);
        public static readonly Color Gold        = Hex(0xE7B23C);
        public static readonly Color GoldDark    = Hex(0xB8862A);

        // Neutros de estado
        public static readonly Color White       = Hex(0xFFFFFF);
        public static readonly Color Error       = Hex(0xB8292E);

        // ── 5 estilos de texto ───────────────────────────────────────────
        /// <summary>Tamaños por estilo. Las fuentes viven en <c>Assets/Art/Fonts</c>.</summary>
        public static class TextStyle
        {
            /// <summary>Lilita One · títulos de pantalla.</summary>
            public const float Title = 76f;
            /// <summary>Lilita One · subtítulos y marcadores grandes.</summary>
            public const float Subtitle = 48f;
            /// <summary>Patrick Hand · cuerpo y textos de apoyo.</summary>
            public const float Body = 32f;
            /// <summary>Patrick Hand · etiquetas de botón.</summary>
            public const float Button = 44f;
            /// <summary>Lilita One · puntaje, tiempo y vidas del HUD.</summary>
            public const float Hud = 72f;
        }

        /// <summary>Fondo translúcido de los paneles modales.</summary>
        public static Color Scrim => WithAlpha(Hex(0x000000), 0.55f);

        /// <summary>Campo de texto sobre la tarjeta de arena.</summary>
        public static Color Field => WithAlpha(White, 0.92f);

        /// <summary>Placeholder de un campo de texto.</summary>
        public static Color Placeholder => WithAlpha(Ink, 0.45f);

        /// <summary>
        /// Fondo del bocadillo de la Palenquera: translúcido a propósito para que
        /// se vean los ítems que caen detrás mientras lee el comentario.
        /// </summary>
        public static Color DialogueBubble => WithAlpha(Ink, 0.55f);

        /// <summary>Historial del bocadillo, reforzado sobre el fondo translúcido.</summary>
        public static Color DialogueHistory => WithAlpha(Cream, 0.82f);

        /// <summary>Puntos de pensamiento, más ligeros que el bocadillo.</summary>
        public static Color DialogueDot => WithAlpha(Ink, 0.72f);

        /// <summary>Devuelve el mismo color con otra transparencia.</summary>
        public static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }

        private static Color Hex(uint rgb)
        {
            return new Color(
                ((rgb >> 16) & 0xFF) / 255f,
                ((rgb >> 8) & 0xFF) / 255f,
                (rgb & 0xFF) / 255f,
                1f);
        }
    }
}
