using DG.Tweening;
using UnityEngine;

namespace DustlineArena.Runtime.Feedback
{
    public static class DotweenBootstrap
    {
        private const int TweenCapacity = 600;
        private const int SequenceCapacity = 120;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            DOTween.Init(true, true, LogBehaviour.ErrorsOnly);
            DOTween.SetTweensCapacity(TweenCapacity, SequenceCapacity);
        }
    }
}
