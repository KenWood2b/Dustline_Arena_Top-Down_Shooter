using DustlineArena.Runtime.Config;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DustlineArena.Runtime.Audio
{
    public sealed class GameAudio : MonoBehaviour
    {
        private const string LibraryResourcePath = "Audio/GameAudioLibrary";
        private const string MasterVolumeKey = "Dustline.Audio.Master";
        private const string MusicVolumeKey = "Dustline.Audio.Music";
        private const string SfxVolumeKey = "Dustline.Audio.SFX";
        private const string UiVolumeKey = "Dustline.Audio.UI";

        private static GameAudio instance;

        [SerializeField] private GameAudioLibrary library;
        [SerializeField, Range(0f, 1f)] private float masterVolume = 1f;
        [SerializeField, Range(0f, 1f)] private float musicVolume = 0.55f;
        [SerializeField, Range(0f, 1f)] private float sfxVolume = 0.85f;
        [SerializeField, Range(0f, 1f)] private float uiVolume = 0.8f;

        private AudioSource musicSource;
        private AudioSource sfxSource;
        private AudioSource uiSource;

        public static GameAudio Instance
        {
            get
            {
                EnsureInstance();
                return instance;
            }
        }

        public float MasterVolume
        {
            get => masterVolume;
            set
            {
                masterVolume = Mathf.Clamp01(value);
                PlayerPrefs.SetFloat(MasterVolumeKey, masterVolume);
                ApplyVolumes();
            }
        }

        public float MusicVolume
        {
            get => musicVolume;
            set
            {
                musicVolume = Mathf.Clamp01(value);
                PlayerPrefs.SetFloat(MusicVolumeKey, musicVolume);
                ApplyVolumes();
            }
        }

        public float SfxVolume
        {
            get => sfxVolume;
            set
            {
                sfxVolume = Mathf.Clamp01(value);
                PlayerPrefs.SetFloat(SfxVolumeKey, sfxVolume);
                ApplyVolumes();
            }
        }

        public float UiVolume
        {
            get => uiVolume;
            set
            {
                uiVolume = Mathf.Clamp01(value);
                PlayerPrefs.SetFloat(UiVolumeKey, uiVolume);
                ApplyVolumes();
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            EnsureInstance();
        }

        public static void PlayWeaponFire(WeaponVisualId visual, Vector3 position)
        {
            GameAudio audio = Instance;
            audio.PlaySfxAt(audio.library == null ? null : audio.library.GetFireClip(visual), position, 0.75f, RandomPitch(0.035f));
        }

        public static void PlayReloadStart(Vector3 position)
        {
            GameAudio audio = Instance;
            audio.PlaySfxAt(audio.library == null ? null : audio.library.ReloadStart, position, 0.48f, RandomPitch(0.025f));
        }

        public static void PlayReloadComplete(Vector3 position)
        {
            GameAudio audio = Instance;
            audio.PlaySfxAt(audio.library == null ? null : audio.library.ReloadComplete, position, 0.42f, RandomPitch(0.025f));
        }

        public static void PlayPickup(Vector3 position)
        {
            GameAudio audio = Instance;
            audio.PlaySfxAt(audio.library == null ? null : audio.library.Pickup, position, 0.65f, RandomPitch(0.05f));
        }

        public static void PlayDodge(Vector3 position)
        {
            GameAudio audio = Instance;
            audio.PlaySfxAt(audio.library == null ? null : audio.library.Dodge, position, 0.38f, RandomPitch(0.04f));
        }

        public static void PlayPlayerHit(Vector3 position)
        {
            GameAudio audio = Instance;
            audio.PlaySfxAt(audio.library == null ? null : audio.library.PlayerHit, position, 0.55f, RandomPitch(0.035f));
        }

        public static void PlayZombiePain(Vector3 position)
        {
            GameAudio audio = Instance;
            audio.PlaySfxAt(audio.library == null ? null : audio.library.ZombiePain, position, 0.58f, RandomPitch(0.06f));
        }

        public static void PlayUiSelect()
        {
            GameAudio audio = Instance;
            audio.PlayUi(audio.library == null ? null : audio.library.UiSelect, 0.65f);
        }

        public static void PlayUiConfirm()
        {
            GameAudio audio = Instance;
            audio.PlayUi(audio.library == null ? null : audio.library.UiConfirm, 0.78f);
        }

        public static void PlayUiBack()
        {
            GameAudio audio = Instance;
            audio.PlayUi(audio.library == null ? null : audio.library.UiBack, 0.7f);
        }

        private static void EnsureInstance()
        {
            if (instance != null)
            {
                return;
            }

            GameObject audioObject = new GameObject("Game_Audio", typeof(GameAudio));
            DontDestroyOnLoad(audioObject);
            instance = audioObject.GetComponent<GameAudio>();
            instance.Initialize();
        }

        private static float RandomPitch(float range)
        {
            return Random.Range(1f - range, 1f + range);
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            Initialize();
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void Initialize()
        {
            if (library == null)
            {
                library = Resources.Load<GameAudioLibrary>(LibraryResourcePath);
            }

            musicSource = EnsureSource("Music_Source", true);
            sfxSource = EnsureSource("SFX_Source", false);
            uiSource = EnsureSource("UI_Source", false);

            masterVolume = PlayerPrefs.GetFloat(MasterVolumeKey, masterVolume);
            musicVolume = PlayerPrefs.GetFloat(MusicVolumeKey, musicVolume);
            sfxVolume = PlayerPrefs.GetFloat(SfxVolumeKey, sfxVolume);
            uiVolume = PlayerPrefs.GetFloat(UiVolumeKey, uiVolume);
            ApplyVolumes();
            RefreshMusicForScene(SceneManager.GetActiveScene().name);
        }

        private AudioSource EnsureSource(string sourceName, bool loop)
        {
            Transform existing = transform.Find(sourceName);
            AudioSource source = existing == null
                ? new GameObject(sourceName).AddComponent<AudioSource>()
                : existing.GetComponent<AudioSource>();

            source.transform.SetParent(transform, false);
            source.loop = loop;
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            return source;
        }

        private void ApplyVolumes()
        {
            if (musicSource != null)
            {
                musicSource.volume = masterVolume * musicVolume;
            }

            if (sfxSource != null)
            {
                sfxSource.volume = masterVolume * sfxVolume;
            }

            if (uiSource != null)
            {
                uiSource.volume = masterVolume * uiVolume;
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            RefreshMusicForScene(scene.name);
        }

        private void RefreshMusicForScene(string sceneName)
        {
            if (library == null || musicSource == null)
            {
                return;
            }

            AudioClip targetClip = sceneName.IndexOf("Menu", System.StringComparison.OrdinalIgnoreCase) >= 0
                ? library.MenuMusic
                : library.GameplayMusic;

            if (targetClip == null || musicSource.clip == targetClip)
            {
                return;
            }

            musicSource.clip = targetClip;
            musicSource.Play();
        }

        private void PlayUi(AudioClip clip, float volumeScale)
        {
            if (clip == null || uiSource == null)
            {
                return;
            }

            uiSource.pitch = 1f;
            uiSource.PlayOneShot(clip, volumeScale);
        }

        private void PlaySfxAt(AudioClip clip, Vector3 position, float volumeScale, float pitch)
        {
            if (clip == null || sfxSource == null)
            {
                return;
            }

            sfxSource.transform.position = position;
            sfxSource.pitch = pitch;
            sfxSource.PlayOneShot(clip, volumeScale);
        }
    }
}
