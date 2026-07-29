using DustlineArena.Runtime.Config;
using UnityEngine;

namespace DustlineArena.Runtime.Audio
{
    [CreateAssetMenu(menuName = "Dustline Arena/Audio/Game Audio Library", fileName = "GameAudioLibrary")]
    public sealed class GameAudioLibrary : ScriptableObject
    {
        [Header("Music")]
        [SerializeField] private AudioClip menuMusic;
        [SerializeField] private AudioClip gameplayMusic;
        [SerializeField] private AudioClip arena01Music;
        [SerializeField] private AudioClip arena02Music;
        [SerializeField] private AudioClip arena03Music;
        [SerializeField] private AudioClip arena04Music;
        [SerializeField] private AudioClip arena05Music;

        [Header("UI")]
        [SerializeField] private AudioClip uiSelect;
        [SerializeField] private AudioClip uiConfirm;
        [SerializeField] private AudioClip uiBack;
        [SerializeField] private AudioClip uiError;

        [Header("Weapons")]
        [SerializeField] private AudioClip pistolFire;
        [SerializeField] private AudioClip smgFire;
        [SerializeField] private AudioClip shotgunFire;
        [SerializeField] private AudioClip akFire;
        [SerializeField] private AudioClip reloadStart;
        [SerializeField] private AudioClip reloadComplete;

        [Header("Gameplay")]
        [SerializeField] private AudioClip pickup;
        [SerializeField] private AudioClip playerHit;
        [SerializeField] private AudioClip dodge;
        [SerializeField] private AudioClip grenadeThrow;
        [SerializeField] private AudioClip grenadeBounce;
        [SerializeField] private AudioClip grenadeExplosion;
        [SerializeField] private AudioClip grenadeExplosionTail;
        [SerializeField] private AudioClip zombieMoan;
        [SerializeField] private AudioClip zombiePain;
        [SerializeField] private AudioClip zombieAttack;
        [SerializeField] private AudioClip zombieDeath;

        public AudioClip MenuMusic => menuMusic;
        public AudioClip GameplayMusic => gameplayMusic;
        public AudioClip UiSelect => uiSelect;
        public AudioClip UiConfirm => uiConfirm;
        public AudioClip UiBack => uiBack;
        public AudioClip UiError => uiError;
        public AudioClip ReloadStart => reloadStart;
        public AudioClip ReloadComplete => reloadComplete;
        public AudioClip Pickup => pickup;
        public AudioClip PlayerHit => playerHit;
        public AudioClip Dodge => dodge;
        public AudioClip GrenadeThrow => grenadeThrow;
        public AudioClip GrenadeBounce => grenadeBounce;
        public AudioClip GrenadeExplosion => grenadeExplosion;
        public AudioClip GrenadeExplosionTail => grenadeExplosionTail;
        public AudioClip ZombieMoan => zombieMoan;
        public AudioClip ZombiePain => zombiePain;
        public AudioClip ZombieAttack => zombieAttack;
        public AudioClip ZombieDeath => zombieDeath;

        public AudioClip GetMusicForScene(string sceneName)
        {
            if (!string.IsNullOrWhiteSpace(sceneName) &&
                sceneName.IndexOf("Menu", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return menuMusic;
            }

            switch (sceneName)
            {
                case "Dustline_Arena_01":
                    return arena01Music != null ? arena01Music : gameplayMusic;
                case "Dustline_Arena_02":
                    return arena02Music != null ? arena02Music : gameplayMusic;
                case "Dustline_Arena_03":
                    return arena03Music != null ? arena03Music : gameplayMusic;
                case "Dustline_Arena_04":
                    return arena04Music != null ? arena04Music : gameplayMusic;
                case "Dustline_Arena_05":
                    return arena05Music != null ? arena05Music : gameplayMusic;
                default:
                    return gameplayMusic;
            }
        }

        public AudioClip GetFireClip(WeaponVisualId visual)
        {
            switch (visual)
            {
                case WeaponVisualId.Pistol:
                    return pistolFire;
                case WeaponVisualId.SMG:
                    return smgFire;
                case WeaponVisualId.Shotgun:
                    return shotgunFire;
                case WeaponVisualId.AK:
                    return akFire;
                default:
                    return pistolFire;
            }
        }
    }
}
