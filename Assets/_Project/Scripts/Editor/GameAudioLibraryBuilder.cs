using DustlineArena.Runtime.Audio;
using UnityEditor;
using UnityEngine;

namespace DustlineArena.Editor
{
    public static class GameAudioLibraryBuilder
    {
        private const string LibraryFolder = "Assets/_Project/Resources/Audio";
        private const string LibraryPath = LibraryFolder + "/GameAudioLibrary.asset";

        [MenuItem("Dustline Arena/Build Game Audio Library")]
        public static void Build()
        {
            EnsureFolder("Assets/_Project", "Resources");
            EnsureFolder("Assets/_Project/Resources", "Audio");

            GameAudioLibrary library = AssetDatabase.LoadAssetAtPath<GameAudioLibrary>(LibraryPath);
            if (library == null)
            {
                library = ScriptableObject.CreateInstance<GameAudioLibrary>();
                AssetDatabase.CreateAsset(library, LibraryPath);
            }

            SerializedObject serializedLibrary = new SerializedObject(library);
            SetClip(serializedLibrary, "menuMusic", "Assets/_Project/Audio/Music/title.ogg");
            SetClip(serializedLibrary, "gameplayMusic", "Assets/_Project/Audio/Music/sector.ogg");
            SetClip(serializedLibrary, "arena01Music", "Assets/_Project/Audio/Music/sector.ogg");
            SetClip(serializedLibrary, "arena02Music", "Assets/_Project/Audio/Music/dark_place.ogg");
            SetClip(serializedLibrary, "arena03Music", "Assets/_Project/Audio/Music/pulse.ogg");
            SetClip(serializedLibrary, "arena04Music", "Assets/_Project/Audio/Music/urgent.ogg");
            SetClip(serializedLibrary, "arena05Music", "Assets/_Project/Audio/Music/transmission.ogg");

            SetClip(serializedLibrary, "uiSelect", "Assets/_Project/Audio/SFX/UI/KenneyInterface/select_001.ogg");
            SetClip(serializedLibrary, "uiConfirm", "Assets/_Project/Audio/SFX/UI/KenneyInterface/confirmation_001.ogg");
            SetClip(serializedLibrary, "uiBack", "Assets/_Project/Audio/SFX/UI/KenneyInterface/back_001.ogg");
            SetClip(serializedLibrary, "uiError", "Assets/_Project/Audio/SFX/UI/KenneyInterface/error_001.ogg");

            SetClip(serializedLibrary, "pistolFire", "Assets/Scifi Guns SFX Pack/Gun1_1.wav");
            SetClip(serializedLibrary, "smgFire", "Assets/Scifi Guns SFX Pack/Gun3_1.wav");
            SetClip(serializedLibrary, "shotgunFire", "Assets/Scifi Guns SFX Pack/Gun5_1.wav");
            SetClip(serializedLibrary, "akFire", "Assets/Scifi Guns SFX Pack/Gun4_1.wav");
            SetClip(serializedLibrary, "reloadStart", "Assets/Scifi Guns SFX Pack/Gun1_Load.wav");
            SetClip(serializedLibrary, "reloadComplete", "Assets/_Project/Audio/SFX/UI/KenneyInterface/confirmation_002.ogg");
            SetClip(serializedLibrary, "emptyWeapon", "Assets/_Project/Audio/SFX/UI/KenneyInterface/error_001.ogg");

            SetClip(serializedLibrary, "pickup", "Assets/_Project/Audio/SFX/UI/KenneyUIAudio/switch12.ogg");
            SetClip(serializedLibrary, "playerHit", "Assets/_Project/Audio/SFX/Impacts/KenneyImpact/impactPunch_medium_000.ogg");
            SetClip(serializedLibrary, "dodge", "Assets/_Project/Audio/SFX/Impacts/KenneyImpact/footstep_concrete_000.ogg");
            SetClip(serializedLibrary, "grenadeThrow", "Assets/_Project/Audio/SFX/UI/KenneyInterface/drop_001.ogg");
            SetClip(serializedLibrary, "grenadeBounce", "Assets/_Project/Audio/SFX/Impacts/KenneyImpact/impactMetal_light_000.ogg");
            SetClip(serializedLibrary, "grenadeExplosion", "Assets/Scifi Guns SFX Pack/Gun5.wav");
            SetClip(serializedLibrary, "grenadeExplosionTail", "Assets/_Project/Audio/SFX/Impacts/KenneyImpact/impactMetal_heavy_000.ogg");
            SetClip(serializedLibrary, "zombieMoan", "Assets/_Project/Audio/SFX/Zombies/zombie_moans.ogg");
            SetClip(serializedLibrary, "zombiePain", "Assets/_Project/Audio/SFX/Zombies/zombie_pain.wav");
            SetClip(serializedLibrary, "zombieAttack", "Assets/_Project/Audio/SFX/Zombies/zombie_pain.wav");
            SetClip(serializedLibrary, "zombieDeath", "Assets/_Project/Audio/SFX/Zombies/zombie_moans.ogg");
            SetClip(serializedLibrary, "waveStarted", "Assets/_Project/Audio/SFX/UI/KenneyInterface/open_002.ogg");
            SetClip(serializedLibrary, "waveCleared", "Assets/_Project/Audio/SFX/UI/KenneyInterface/confirmation_003.ogg");
            SetClip(serializedLibrary, "arenaCleared", "Assets/_Project/Audio/Music/victory.ogg");
            SetClip(serializedLibrary, "gameOver", "Assets/_Project/Audio/SFX/UI/KenneyInterface/close_004.ogg");
            SetClips(
                serializedLibrary,
                "hardSurfaceFootsteps",
                "Assets/_Project/Audio/SFX/Impacts/KenneyImpact/footstep_concrete_000.ogg",
                "Assets/_Project/Audio/SFX/Impacts/KenneyImpact/footstep_concrete_001.ogg",
                "Assets/_Project/Audio/SFX/Impacts/KenneyImpact/footstep_concrete_002.ogg");
            SetClips(
                serializedLibrary,
                "softSurfaceFootsteps",
                "Assets/_Project/Audio/SFX/Impacts/KenneyImpact/footstep_grass_000.ogg",
                "Assets/_Project/Audio/SFX/Impacts/KenneyImpact/footstep_grass_001.ogg",
                "Assets/_Project/Audio/SFX/Impacts/KenneyImpact/footstep_grass_002.ogg");

            serializedLibrary.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(library);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Dustline Arena: game audio library built.");
        }

        public static void BuildFromCommandLine()
        {
            Build();
        }

        private static void SetClip(SerializedObject serializedObject, string propertyName, string assetPath)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                Debug.LogWarning($"Dustline Arena: missing audio library property {propertyName}.");
                return;
            }

            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
            if (clip == null)
            {
                Debug.LogWarning($"Dustline Arena: missing audio clip at {assetPath}.");
            }

            property.objectReferenceValue = clip;
        }

        private static void SetClips(SerializedObject serializedObject, string propertyName, params string[] assetPaths)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null || !property.isArray)
            {
                Debug.LogWarning($"Dustline Arena: missing audio library array {propertyName}.");
                return;
            }

            property.arraySize = assetPaths.Length;
            for (int i = 0; i < assetPaths.Length; i++)
            {
                AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPaths[i]);
                if (clip == null)
                {
                    Debug.LogWarning($"Dustline Arena: missing audio clip at {assetPaths[i]}.");
                }

                property.GetArrayElementAtIndex(i).objectReferenceValue = clip;
            }
        }

        private static void EnsureFolder(string parent, string child)
        {
            string path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }
    }
}
