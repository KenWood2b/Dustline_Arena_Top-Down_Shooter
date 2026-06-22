using UnityEngine;

namespace DustlineArena.Runtime.Common
{
    public readonly struct DamageInfo
    {
        public DamageInfo(float amount, GameObject source, TeamId sourceTeam, Vector3 point, Vector3 direction)
        {
            Amount = amount;
            Source = source;
            SourceTeam = sourceTeam;
            Point = point;
            Direction = direction;
        }

        public float Amount { get; }
        public GameObject Source { get; }
        public TeamId SourceTeam { get; }
        public Vector3 Point { get; }
        public Vector3 Direction { get; }
    }
}
