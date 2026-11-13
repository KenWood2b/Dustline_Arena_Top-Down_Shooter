using System;
using UnityEngine;

namespace DustlineArena.Runtime.Progression
{
    public readonly struct ArenaRunResult
    {
        public ArenaRunResult(int arenaIndex, int kills, float damageTaken, float duration)
        {
            ArenaIndex = arenaIndex;
            Kills = Mathf.Max(0, kills);
            DamageTaken = Mathf.Max(0f, damageTaken);
            Duration = Mathf.Max(0f, duration);
        }

        public int ArenaIndex { get; }
        public int Kills { get; }
        public float DamageTaken { get; }
        public float Duration { get; }
    }

    public readonly struct OperationTotals
    {
        public OperationTotals(int arenasCleared, int kills, float damageTaken, float duration)
        {
            ArenasCleared = arenasCleared;
            Kills = kills;
            DamageTaken = damageTaken;
            Duration = duration;
        }

        public int ArenasCleared { get; }
        public int Kills { get; }
        public float DamageTaken { get; }
        public float Duration { get; }
    }

    public static class OperationProgress
    {
        public const int TotalArenas = 5;

        private const string ArenaScenePrefix = "Dustline_Arena_";

        private static readonly string[] SectorNames =
        {
            string.Empty,
            "FREIGHT YARD",
            "STORAGE GRID",
            "GREEN PERIMETER",
            "SCRAP FIELD",
            "TERMINAL YARD"
        };

        private static ArenaRunResult?[] results = new ArenaRunResult?[TotalArenas + 1];
        private static bool operationActive;

        public static bool IsOperationActive => operationActive;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            results = new ArenaRunResult?[TotalArenas + 1];
            operationActive = false;
        }

        public static void BeginNewOperation()
        {
            Array.Clear(results, 0, results.Length);
            operationActive = true;
        }

        public static void EnsureOperation()
        {
            if (!operationActive)
            {
                BeginNewOperation();
            }
        }

        public static int GetArenaIndex(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName) ||
                !sceneName.StartsWith(ArenaScenePrefix, StringComparison.Ordinal) ||
                !int.TryParse(sceneName.Substring(ArenaScenePrefix.Length), out int index))
            {
                return 1;
            }

            return Mathf.Clamp(index, 1, TotalArenas);
        }

        public static string GetSectorName(int arenaIndex)
        {
            return arenaIndex >= 1 && arenaIndex < SectorNames.Length
                ? SectorNames[arenaIndex]
                : "UNKNOWN SECTOR";
        }

        public static void RecordArenaResult(int arenaIndex, int kills, float damageTaken, float duration)
        {
            EnsureOperation();
            if (arenaIndex < 1 || arenaIndex > TotalArenas)
            {
                return;
            }

            results[arenaIndex] = new ArenaRunResult(arenaIndex, kills, damageTaken, duration);
        }

        public static bool TryGetArenaResult(int arenaIndex, out ArenaRunResult result)
        {
            if (arenaIndex >= 1 && arenaIndex < results.Length && results[arenaIndex].HasValue)
            {
                result = results[arenaIndex].Value;
                return true;
            }

            result = default;
            return false;
        }

        public static OperationTotals GetTotals()
        {
            int arenasCleared = 0;
            int kills = 0;
            float damageTaken = 0f;
            float duration = 0f;

            for (int i = 1; i < results.Length; i++)
            {
                if (!results[i].HasValue)
                {
                    continue;
                }

                ArenaRunResult result = results[i].Value;
                arenasCleared++;
                kills += result.Kills;
                damageTaken += result.DamageTaken;
                duration += result.Duration;
            }

            return new OperationTotals(arenasCleared, kills, damageTaken, duration);
        }
    }
}
