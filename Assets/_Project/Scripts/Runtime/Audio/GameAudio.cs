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
        private const float MusicOutputGain = 0.78f;
        private const float SfxOutputGain = 0.82f;
        private const float UiOutputGain = 0.86f;
        private const float ZombieOutputGain = 0.58f;
        private const float PausedMusicGain = 0.28f;
        private const float ZombieMoanCooldown = 4.5f;
        private const float ZombiePainCooldown = 0.22f;
        private const float ZombieAttackCooldown = 0.32f;
        private const float ZombieDeathCooldown = 0.85f;
        private const int SfxVoiceCount = 16;
        private const int ZombieVoiceCount = 3;

        private static GameAudio instance;
        private static bool isShuttingDown;

        [SerializeField] private GameAudioLibrary library;
        [SerializeField, Range(0f, 1f)] private float masterVolume = 1f;
        [SerializeField, Range(0f, 1f)] private float musicVolume = 0.55f;
        [SerializeField, Range(0f, 1f)] private float sfxVolume = 0.85f;
        [SerializeField, Range(0f, 1f)] private float uiVolume = 0.8f;

        private AudioSource musicSource;
        private AudioSource uiSource;
        private AudioSource[] sfxVoices;
        private AudioSource[] zombieVoices;
        private GameObject[] zombieVoiceOwners;
        private AudioListener fallbackListener;
        private int nextSfxVoice;
        private int nextZombieVoice;
        private bool gameplayPaused;
        private float nextZombieMoanTime;
        private float nextZombiePainTime;
        private float nextZombieAttackTime;
        private float nextZombieDeathTime;

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

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            instance = null;
            isShuttingDown = false;
        }

        public static void PlayWeaponFire(WeaponVisualId visual, Vector3 position)
        {
            GameAudio audio = Instance;
            audio.PlaySfxAt(audio.library == null ? null : audio.library.GetFireClip(visual), position, 0.58f, RandomPitch(0.035f));
        }

        public static void PlayReloadStart(Vector3 position)
        {
            GameAudio audio = Instance;
            audio.PlaySfxAt(audio.library == null ? null : audio.library.ReloadStart, position, 0.34f, RandomPitch(0.025f));
        }

        public static void PlayReloadComplete(Vector3 position)
        {
            GameAudio audio = Instance;
            audio.PlaySfxAt(audio.library == null ? null : audio.library.ReloadComplete, position, 0.38f, RandomPitch(0.025f));
        }

        public static void PlayPickup(Vector3 position)
        {
            GameAudio audio = Instance;
            audio.PlaySfxAt(audio.library == null ? null : audio.library.Pickup, position, 0.48f, RandomPitch(0.05f));
        }

        public static void PlayDodge(Vector3 position)
        {
            GameAudio audio = Instance;
            audio.PlaySfxAt(audio.library == null ? null : audio.library.Dodge, position, 0.28f, RandomPitch(0.04f));
        }

        public static void PlayPlayerHit(Vector3 position)
        {
            GameAudio audio = Instance;
            audio.PlaySfxAt(audio.library == null ? null : audio.library.PlayerHit, position, 0.46f, RandomPitch(0.035f));
        }

        public static void PlayZombiePain(GameObject owner, Vector3 position)
        {
            GameAudio audio = Instance;
            if (audio.TryReserve(ref audio.nextZombiePainTime, ZombiePainCooldown))
            {
                audio.PlayZombieSfxAt(owner, audio.library == null ? null : audio.library.ZombiePain, position, 0.22f, RandomPitch(0.08f), false);
            }
        }

        public static void PlayZombieMoan(GameObject owner, Vector3 position)
        {
            GameAudio audio = Instance;
            if (audio.TryReserve(ref audio.nextZombieMoanTime, ZombieMoanCooldown))
            {
                audio.PlayZombieSfxAt(owner, audio.library == null ? null : audio.library.ZombieMoan, position, 0.13f, Random.Range(0.88f, 1.04f), false);
            }
        }

        public static void PlayZombieAttack(GameObject owner, Vector3 position)
        {
            GameAudio audio = Instance;
            if (audio.TryReserve(ref audio.nextZombieAttackTime, ZombieAttackCooldown))
            {
                audio.PlayZombieSfxAt(owner, audio.library == null ? null : audio.library.ZombieAttack, position, 0.18f, Random.Range(0.98f, 1.12f), false);
            }
        }

        public static void PlayZombieDeath(GameObject owner, Vector3 position)
        {
            GameAudio audio = Instance;
            audio.StopZombieSfxInternal(owner);
            if (audio.TryReserve(ref audio.nextZombieDeathTime, ZombieDeathCooldown))
            {
                audio.PlayZombieSfxAt(owner, audio.library == null ? null : audio.library.ZombieDeath, position, 0.26f, Random.Range(0.76f, 0.92f), true);
            }
        }

        public static void StopZombieSfx(GameObject owner)
        {
            if (instance != null)
            {
                instance.StopZombieSfxInternal(owner);
            }
        }

        public static void PlayGrenadeThrow(Vector3 position)
        {
            GameAudio audio = Instance;
            audio.PlaySfxAt(audio.library == null ? null : audio.library.GrenadeThrow, position, 0.28f, RandomPitch(0.06f));
        }

        public static void PlayGrenadeBounce(Vector3 position, float strength)
        {
            GameAudio audio = Instance;
            float normalizedStrength = Mathf.Clamp01(strength);
            audio.PlaySfxAt(
                audio.library == null ? null : audio.library.GrenadeBounce,
                position,
                Mathf.Lerp(0.1f, 0.28f, normalizedStrength),
                Random.Range(0.9f, 1.08f));
        }

        public static void PlayGrenadeExplosion(Vector3 position)
        {
            GameAudio audio = Instance;
            if (audio.library == null)
            {
                return;
            }

            audio.PlaySfxAt(audio.library.GrenadeExplosion, position, 0.72f, Random.Range(0.82f, 0.92f));
            audio.PlaySfxAt(audio.library.GrenadeExplosionTail, position, 0.32f, Random.Range(0.72f, 0.84f));
        }

        public static void PlayUiSelect()
        {
            GameAudio audio = Instance;
            audio.PlayUi(audio.library == null ? null : audio.library.UiSelect, 0.5f);
        }

        public static void PlayUiConfirm()
        {
            GameAudio audio = Instance;
            audio.PlayUi(audio.library == null ? null : audio.library.UiConfirm, 0.62f);
        }

        public static void PlayUiBack()
        {
            GameAudio audio = Instance;
            audio.PlayUi(audio.library == null ? null : audio.library.UiBack, 0.56f);
        }

        public static void SetGameplayPaused(bool paused)
        {
            if (instance != null)
            {
                instance.SetGameplayPausedInternal(paused);
            }
        }

        private static void EnsureInstance()
        {
            if (instance != null || isShuttingDown || !Application.isPlaying)
            {
                return;
            }

            GameObject audioObject = new GameObject("Game_Audio", typeof(GameAudio));
            instance = audioObject.GetComponent<GameAudio>();
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

        private void OnApplicationQuit()
        {
            isShuttingDown = true;
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                isShuttingDown = true;
                instance = null;
            }
        }

        private void Initialize()
        {
            if (library == null)
            {
                library = Resources.Load<GameAudioLibrary>(LibraryResourcePath);
            }

            musicSource = EnsureSource("Music_Source", true);
            uiSource = EnsureSource("UI_Source", false);
            sfxVoices = EnsureSfxVoices();
            zombieVoices = EnsureVoicePool("Zombie_Voices", "Zombie_Voice", ZombieVoiceCount);
            zombieVoiceOwners = new GameObject[ZombieVoiceCount];
            fallbackListener = GetComponent<AudioListener>();
            if (fallbackListener == null)
            {
                fallbackListener = gameObject.AddComponent<AudioListener>();
            }

            masterVolume = PlayerPrefs.GetFloat(MasterVolumeKey, masterVolume);
            musicVolume = PlayerPrefs.GetFloat(MusicVolumeKey, musicVolume);
            sfxVolume = PlayerPrefs.GetFloat(SfxVolumeKey, sfxVolume);
            uiVolume = PlayerPrefs.GetFloat(UiVolumeKey, uiVolume);
            ApplyVolumes();
            RefreshAudioListener();
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
                float pauseGain = gameplayPaused ? PausedMusicGain : 1f;
                musicSource.volume = masterVolume * musicVolume * MusicOutputGain * pauseGain;
            }

            if (sfxVoices != null)
            {
                for (int i = 0; i < sfxVoices.Length; i++)
                {
                    if (sfxVoices[i] != null)
                    {
                        sfxVoices[i].volume = masterVolume * sfxVolume * SfxOutputGain;
                    }
                }
            }

            if (zombieVoices != null)
            {
                for (int i = 0; i < zombieVoices.Length; i++)
                {
                    if (zombieVoices[i] != null)
                    {
                        zombieVoices[i].volume = masterVolume * sfxVolume * ZombieOutputGain;
                    }
                }
            }

            if (uiSource != null)
            {
                uiSource.volume = masterVolume * uiVolume * UiOutputGain;
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            StopAllZombieSfx();
            SetGameplayPausedInternal(false);
            RefreshAudioListener();
            ApplyVolumes();
            RefreshMusicForScene(scene.name);
        }

        private void RefreshAudioListener()
        {
            if (fallbackListener == null)
            {
                fallbackListener = GetComponent<AudioListener>();
                if (fallbackListener == null)
                {
                    fallbackListener = gameObject.AddComponent<AudioListener>();
                }
            }

            AudioListener[] listeners = FindObjectsOfType<AudioListener>(true);
            bool hasSceneListener = false;
            for (int i = 0; i < listeners.Length; i++)
            {
                AudioListener listener = listeners[i];
                if (listener != null &&
                    listener != fallbackListener &&
                    listener.enabled &&
                    listener.gameObject.activeInHierarchy)
                {
                    hasSceneListener = true;
                    break;
                }
            }

            fallbackListener.enabled = !hasSceneListener;
        }

        private void RefreshMusicForScene(string sceneName)
        {
            if (library == null || musicSource == null)
            {
                return;
            }

            AudioClip targetClip = library.GetMusicForScene(sceneName);

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
            if (gameplayPaused || Time.timeScale <= 0f || clip == null || sfxVoices == null || sfxVoices.Length == 0)
            {
                return;
            }

            AudioSource voice = sfxVoices[nextSfxVoice];
            nextSfxVoice = (nextSfxVoice + 1) % sfxVoices.Length;
            if (voice == null)
            {
                return;
            }

            voice.transform.position = position;
            voice.pitch = pitch;
            voice.PlayOneShot(clip, volumeScale);
        }

        private void PlayZombieSfxAt(GameObject owner, AudioClip clip, Vector3 position, float volumeScale, float pitch, bool allowSteal)
        {
            if (gameplayPaused || Time.timeScale <= 0f || owner == null || clip == null || zombieVoices == null || zombieVoices.Length == 0)
            {
                return;
            }

            AudioSource voice = null;
            int voiceIndex = -1;
            for (int i = 0; i < zombieVoices.Length; i++)
            {
                int index = (nextZombieVoice + i) % zombieVoices.Length;
                if (zombieVoices[index] != null && !zombieVoices[index].isPlaying)
                {
                    voice = zombieVoices[index];
                    voiceIndex = index;
                    nextZombieVoice = (index + 1) % zombieVoices.Length;
                    break;
                }
            }

            if (voice == null && allowSteal)
            {
                voiceIndex = nextZombieVoice;
                voice = zombieVoices[voiceIndex];
                nextZombieVoice = (nextZombieVoice + 1) % zombieVoices.Length;
                voice?.Stop();
            }

            if (voice == null)
            {
                return;
            }

            voice.transform.position = position;
            voice.pitch = pitch;
            zombieVoiceOwners[voiceIndex] = owner;
            voice.PlayOneShot(clip, volumeScale);
        }

        private void StopZombieSfxInternal(GameObject owner)
        {
            if (owner == null || zombieVoices == null || zombieVoiceOwners == null)
            {
                return;
            }

            for (int i = 0; i < zombieVoices.Length; i++)
            {
                if (zombieVoiceOwners[i] != owner)
                {
                    continue;
                }

                zombieVoices[i]?.Stop();
                zombieVoiceOwners[i] = null;
            }
        }

        private void StopAllZombieSfx()
        {
            if (zombieVoices == null)
            {
                return;
            }

            for (int i = 0; i < zombieVoices.Length; i++)
            {
                zombieVoices[i]?.Stop();
                if (zombieVoiceOwners != null && i < zombieVoiceOwners.Length)
                {
                    zombieVoiceOwners[i] = null;
                }
            }
        }

        private AudioSource[] EnsureSfxVoices()
        {
            return EnsureVoicePool("SFX_Voices", "SFX_Voice", SfxVoiceCount);
        }

        private AudioSource[] EnsureVoicePool(string rootName, string voicePrefix, int voiceCount)
        {
            Transform root = transform.Find(rootName);
            if (root == null)
            {
                root = new GameObject(rootName).transform;
                root.SetParent(transform, false);
            }

            AudioSource[] voices = new AudioSource[voiceCount];
            for (int i = 0; i < voices.Length; i++)
            {
                string voiceName = $"{voicePrefix}_{i:00}";
                Transform existing = root.Find(voiceName);
                AudioSource voice = existing == null
                    ? new GameObject(voiceName).AddComponent<AudioSource>()
                    : existing.GetComponent<AudioSource>();
                voice.transform.SetParent(root, false);
                voice.loop = false;
                voice.playOnAwake = false;
                voice.spatialBlend = 0f;
                voices[i] = voice;
            }

            return voices;
        }

        private void SetGameplayPausedInternal(bool paused)
        {
            if (gameplayPaused == paused)
            {
                return;
            }

            gameplayPaused = paused;
            SetVoicePoolPaused(sfxVoices, paused);
            SetVoicePoolPaused(zombieVoices, paused);
            ApplyVolumes();
        }

        private static void SetVoicePoolPaused(AudioSource[] voices, bool paused)
        {
            if (voices == null)
            {
                return;
            }

            for (int i = 0; i < voices.Length; i++)
            {
                AudioSource voice = voices[i];
                if (voice == null)
                {
                    continue;
                }

                if (paused)
                {
                    voice.Pause();
                }
                else
                {
                    voice.UnPause();
                }
            }
        }

        private bool TryReserve(ref float nextAllowedTime, float cooldown)
        {
            if (Time.unscaledTime < nextAllowedTime)
            {
                return false;
            }

            nextAllowedTime = Time.unscaledTime + Mathf.Max(0f, cooldown);
            return true;
        }
    }
}
