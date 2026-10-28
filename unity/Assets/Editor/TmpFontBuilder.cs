using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace CosTamarindo.EditorTools
{
    /// <summary>
    /// Genera los TMP Font Assets (SDF) a partir de las tipografías del diseño.
    /// Se ejecuta en batchmode con <c>-executeMethod</c>.
    /// </summary>
    public static class TmpFontBuilder
    {
        private const string FontsFolder = "Assets/Art/Fonts";

        // ASCII imprimible + caracteres del español + signos de apertura + comillas tipográficas.
        private const string Charset =
            " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`" +
            "abcdefghijklmnopqrstuvwxyz{|}~" +
            "ÁÉÍÓÚÜÑáéíóúüñ¿¡«»…—–";

        private static readonly (string source, string output)[] Fonts =
        {
            ("LilitaOne-Regular.ttf", "LilitaOne SDF"),
            ("PatrickHand-Regular.ttf", "PatrickHand SDF"),
        };

        [MenuItem("CosTamarindo/Generar fuentes TMP SDF", priority = 20)]
        public static void Generate()
        {
            foreach (var (source, output) in Fonts)
                Build($"{FontsFolder}/{source}", $"{FontsFolder}/{output}.asset");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void Build(string sourcePath, string outputPath)
        {
            if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(outputPath) != null)
            {
                Debug.Log($"[CosTamarindo] {outputPath} ya existe, se omite.");
                return;
            }

            var font = AssetDatabase.LoadAssetAtPath<Font>(sourcePath);
            if (font == null)
            {
                Debug.LogError($"[CosTamarindo] No se encontró la fuente {sourcePath}");
                return;
            }

            var asset = TMP_FontAsset.CreateFontAsset(
                font,
                samplingPointSize: 90,
                atlasPadding: 9,
                renderMode: GlyphRenderMode.SDFAA,
                atlasWidth: 1024,
                atlasHeight: 1024,
                atlasPopulationMode: AtlasPopulationMode.Dynamic,
                enableMultiAtlasSupport: true);

            asset.name = Path.GetFileNameWithoutExtension(outputPath);
            asset.TryAddCharacters(Charset, out string missing);

            AssetDatabase.CreateAsset(asset, outputPath);

            asset.material.name = asset.name + " Material";
            AssetDatabase.AddObjectToAsset(asset.material, asset);

            foreach (var atlas in asset.atlasTextures)
            {
                atlas.name = asset.name + " Atlas";
                AssetDatabase.AddObjectToAsset(atlas, asset);
            }

            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();

            string missingNote = string.IsNullOrEmpty(missing) ? "sin faltantes" : $"faltan: {missing}";
            Debug.Log($"[CosTamarindo] Fuente generada: {outputPath} ({missingNote})");
        }
    }
}
