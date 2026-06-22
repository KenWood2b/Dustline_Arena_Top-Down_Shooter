using UnityEngine;

namespace DustlineArena.Runtime.Common
{
    public sealed class TeamMember : MonoBehaviour
    {
        [SerializeField] private TeamId team = TeamId.Neutral;

        public TeamId Team => team;
    }
}
