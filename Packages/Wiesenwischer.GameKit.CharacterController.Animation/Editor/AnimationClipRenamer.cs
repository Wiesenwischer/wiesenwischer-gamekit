using UnityEditor;
using UnityEngine;

namespace Wiesenwischer.GameKit.CharacterController.Animation.Editor
{
    public static class AnimationClipRenamer
    {
        private const string ClipBasePath = "Assets/Animations/Locomotion/";

        private static readonly string[] FbxFiles =
        {
            "Anim_Idle",
            "Anim_Walk",
            "Anim_Run",
            "Anim_Sprint",
            "Anim_Jump",
            "Anim_Fall",
            "Anim_Land"
        };

        [MenuItem("Wiesenwischer/GameKit/Animation/Rename Animation Clips", false, 104)]
        public static void RenameAllClips()
        {
            int renamed = 0;

            foreach (var fbxName in FbxFiles)
            {
                var fbxPath = ClipBasePath + fbxName + ".fbx";
                var importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;

                if (importer == null)
                {
                    Debug.LogWarning($"[AnimationClipRenamer] FBX nicht gefunden: {fbxPath}");
                    continue;
                }

                // Clip-Name aus Dateinamen ableiten: "Anim_Idle" → "Idle"
                var clipName = fbxName.Replace("Anim_", "");

                var clips = importer.clipAnimations;

                // Falls keine Custom Clips definiert sind, von Default übernehmen
                if (clips.Length == 0)
                    clips = importer.defaultClipAnimations;

                if (clips.Length == 0)
                {
                    Debug.LogWarning($"[AnimationClipRenamer] Keine Clips in: {fbxPath}");
                    continue;
                }

                bool changed = false;
                for (int i = 0; i < clips.Length; i++)
                {
                    if (clips[i].name != clipName)
                    {
                        Debug.Log($"[AnimationClipRenamer] {fbxPath}: '{clips[i].name}' → '{clipName}'");
                        clips[i].name = clipName;
                        changed = true;
                    }
                }

                if (changed)
                {
                    importer.clipAnimations = clips;
                    importer.SaveAndReimport();
                    renamed++;
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[AnimationClipRenamer] {renamed} FBX-Dateien umbenannt.");
        }
    }
}
