using UnityEditor;
using UnityEngine;

namespace CosTamarindo.EditorTools
{
    /// <summary>
    /// Utilidades de arranque del entorno de desarrollo. Se ejecutan en modo batch
    /// con <c>-executeMethod</c> para preparar el proyecto sin abrir el editor.
    ///
    /// El antiguo autoarranque del puente MCP se retiró junto con el paquete
    /// <c>com.coplaydev.unity-mcp</c> (FE-14/SEC-14): el proyecto entregado no
    /// depende de herramientas de agente.
    /// </summary>
    public static class EditorBootstrap
    {
        /// <summary>Fuerza la serialización en texto y la importación de metadatos visibles.</summary>
        public static void EnsureTextSerialization()
        {
            EditorSettings.serializationMode = SerializationMode.ForceText;
            AssetDatabase.SaveAssets();
            Debug.Log("[CosTamarindo] Serialización en texto forzada.");
        }
    }
}
