using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace CosTamarindo.EditorTools
{
    /// <summary>
    /// Build WebGL de Release, reproducible por línea de comandos.
    /// <code>
    /// Unity.exe -batchmode -quit -projectPath unity ^
    ///   -executeMethod CosTamarindo.EditorTools.BuildScript.BuildWebGL ^
    ///   -logFile "%TEMP%\webgl-build.log"
    /// </code>
    /// La salida se escribe en <c>builds/webgl</c> en la raíz del repositorio
    /// (excluida de git por <c>[Bb]uilds/</c>).
    /// </summary>
    public static class BuildScript
    {
        private const string OutputFolder = "builds/webgl";

        [MenuItem("CosTamarindo/Build WebGL (Release)", priority = 40)]
        public static void BuildWebGL()
        {
            ConfigurePlayerSettings();

            string[] scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();

            if (scenes.Length == 0)
            {
                throw new Exception("[BuildWebGL] EditorBuildSettings no tiene escenas habilitadas.");
            }

            string projectRoot = Directory.GetParent(Application.dataPath)!.FullName;
            string output = Path.GetFullPath(Path.Combine(projectRoot, "..", OutputFolder))
                .Replace('\\', '/');
            Directory.CreateDirectory(output);

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = output,
                target = BuildTarget.WebGL,
                targetGroup = BuildTargetGroup.WebGL,
                options = BuildOptions.None,
            };

            Debug.Log($"[BuildWebGL] escenas: {string.Join(", ", scenes)}");
            Debug.Log($"[BuildWebGL] destino: {output}");

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            Debug.Log(
                $"[BuildWebGL] resultado={summary.result} " +
                $"tamaño={summary.totalSize / 1048576f:F1} MB " +
                $"duracion={summary.totalTime} " +
                $"errores={summary.totalErrors}");

            if (summary.result != BuildResult.Succeeded)
            {
                throw new Exception($"[BuildWebGL] el build falló con resultado {summary.result}.");
            }
        }

        /// <summary>Ajustes de Player Settings exigidos por <c>Plan.md</c> F3 paso 1.</summary>
        private static void ConfigurePlayerSettings()
        {
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);

            NamedBuildTarget webgl = NamedBuildTarget.WebGL;

            PlayerSettings.SetScriptingBackend(webgl, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetIl2CppCompilerConfiguration(webgl, Il2CppCompilerConfiguration.Release);
            PlayerSettings.SetManagedStrippingLevel(webgl, ManagedStrippingLevel.High);
            PlayerSettings.SetApplicationIdentifier(webgl, "com.costamarindo.game");

            PlayerSettings.stripEngineCode = true;
            PlayerSettings.bundleVersion = "0.3.0";
            PlayerSettings.runInBackground = true;

            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = false;
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.WebGL.threadsSupport = false;
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
            PlayerSettings.WebGL.memorySize = 256;
            PlayerSettings.WebGL.template = "APPLICATION:Default";

            EditorUserBuildSettings.development = false;
            EditorUserBuildSettings.allowDebugging = false;
        }
    }
}
