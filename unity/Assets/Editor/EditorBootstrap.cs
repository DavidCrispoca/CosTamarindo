using UnityEditor;
using UnityEngine;

namespace CosTamarindo.EditorTools
{
    /// <summary>
    /// Utilidades de arranque del entorno de desarrollo. Se ejecutan en modo batch
    /// con <c>-executeMethod</c> cuando el puente MCP no está disponible.
    /// </summary>
    public static class EditorBootstrap
    {
        /// <summary>Reactiva el arranque automático del puente MCP for Unity.</summary>
        public static void EnableMcpAutoStart()
        {
            EditorPrefs.SetBool("MCPForUnity.AutoStartOnLoad", true);
            Debug.Log("[CosTamarindo] MCPForUnity.AutoStartOnLoad = true");
        }

        /// <summary>Fuerza la serialización en texto y la importación de metadatos visibles.</summary>
        public static void EnsureTextSerialization()
        {
            EditorSettings.serializationMode = SerializationMode.ForceText;
            AssetDatabase.SaveAssets();
            Debug.Log("[CosTamarindo] Serialización en texto forzada.");
        }
    }
}
