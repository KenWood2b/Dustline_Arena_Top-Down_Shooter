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
        [SerializeField] private AudioClip zombieMoan;
        [SerializeField] private AudioClip zombiePain;

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
        public AudioClip ZombieMoan => zombieMoan;
        public AudioClip ZombiePain => zombiePain;

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
