using UnityEditor;
using UnityEngine;

namespace CosTamarindo.EditorTools
{
    /// <summary>
    /// Ajustes de importación del arte exportado desde Figma.
    ///
    /// Se ejecuta en batchmode o desde el menú. Es idempotente: aplicar los ajustes
    /// dos veces deja el asset igual, así que sirve para reconstruir el proyecto en
    /// otra máquina sin tocar el inspector a mano.
    ///
    /// <code>
    /// Unity.exe -batchmode -quit -projectPath unity ^
    ///   -executeMethod CosTamarindo.EditorTools.FigmaArtImporter.ImportAll
    /// </code>
    /// </summary>
    public static class FigmaArtImporter
    {
        public const string CharacterPath = "Assets/Art/Characters/palenquera.png";
        public const string BackgroundPath = "Assets/Art/Backgrounds/bg_playa.png";

        /// <summary>Unidades de mundo por cada 100 px del arte original.</summary>
        public const float PixelsPerUnit = 100f;

        [MenuItem("CosTamarindo/Importar arte de Figma", priority = 30)]
        public static void ImportAll()
        {
            ConfigureCharacter();
            ConfigureBackground();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Sprite character = AssetDatabase.LoadAssetAtPath<Sprite>(CharacterPath);
            Sprite background = AssetDatabase.LoadAssetAtPath<Sprite>(BackgroundPath);

            Debug.Log(
                $"[FigmaArtImporter] personaje={(character != null ? character.name : "FALTA")} " +
                $"fondo={(background != null ? background.name : "FALTA")}");
        }

        /// <summary>
        /// La Palenquera lleva la canasta en la cabeza: el pivote va abajo-centro para
        /// poder apoyarla en el suelo y el mesh es Tight para descartar el borde transparente.
        /// </summary>
        private static void ConfigureCharacter()
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(CharacterPath);
            if (importer == null)
            {
                Debug.LogError($"[FigmaArtImporter] no existe {CharacterPath}");
                return;
            }

            ApplyBase(importer, maxSize: 1024, uncompressed: true);
            ApplySpriteGeometry(importer, new Vector2(0.5f, 0f), SpriteMeshType.Tight);

            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }

        /// <summary>
        /// El fondo se estira con <c>BackgroundFitter</c>: pivote centrado y mesh
        /// Full Rect para que el escalado no dependa de píxeles transparentes.
        /// </summary>
        private static void ConfigureBackground()
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(BackgroundPath);
            if (importer == null)
            {
                Debug.LogError($"[FigmaArtImporter] no existe {BackgroundPath}");
                return;
            }

            ApplyBase(importer, maxSize: 1024, uncompressed: false);
            ApplySpriteGeometry(importer, new Vector2(0.5f, 0.5f), SpriteMeshType.FullRect);

            // El fondo es la pieza más pesada del build. Probado: ni
            // TextureImporterCompression.CompressedHQ ni un override explícito a
            // DXT1 cambian nada — WebGL empaquetó RGB24 en ambos casos (6,3 MB a
            // 2048 px). La palanca que sí funciona es maxTextureSize: 1024 px
            // deja el fondo en 1,7 MB y sigue nítido a 960×600.
            // Sin override por plataforma: se descarta el que dejaran pruebas
            // anteriores para que el .meta no arrastre ajustes que el build ignora.
            var webgl = importer.GetPlatformTextureSettings("WebGL");
            if (webgl.overridden)
            {
                webgl.overridden = false;
                importer.SetPlatformTextureSettings(webgl);
            }

            importer.alphaIsTransparency = false;
            importer.SaveAndReimport();
        }

        /// <summary>
        /// Pivote y tipo de malla viven en <see cref="TextureImporterSettings"/>, no
        /// directamente en el importer.
        /// </summary>
        private static void ApplySpriteGeometry(TextureImporter importer, Vector2 pivot, SpriteMeshType meshType)
        {
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = pivot;
            settings.spriteMeshType = meshType;
            importer.SetTextureSettings(settings);
        }

        private static void ApplyBase(TextureImporter importer, int maxSize, bool uncompressed)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.maxTextureSize = maxSize;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.isReadable = false;

            // El arte es dibujado a mano: el personaje va sin compresión para no
            // dañar el borde alfa; el fondo se comprime (DXT1, ver
            // ConfigureBackground) porque ocupa toda la pantalla y su peso domina
            // el build.
            importer.textureCompression = uncompressed
                ? TextureImporterCompression.Uncompressed
                : TextureImporterCompression.Compressed;
        }
    }
}
