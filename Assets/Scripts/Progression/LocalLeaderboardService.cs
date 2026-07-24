using System;
using System.Collections.Generic;
using UnityEngine;

namespace VelocityRush.Progression
{
    [Serializable]
    public class LocalLeaderboardEntry
    {
        public string playerName;
        public float value;
        public long utcTicks;
    }

    [Serializable]
    internal class LocalLeaderboardBoard
    {
        public List<LocalLeaderboardEntry> entries = new List<LocalLeaderboardEntry>();
    }

    /// <summary>
    /// Offline leaderboard fallback. It keeps the top ten per board in PlayerPrefs and can be
    /// replaced by a Unity Gaming Services adapter without changing game-mode code.
    /// </summary>
    public class LocalLeaderboardService : MonoBehaviour
    {
        private const string Prefix = "vr.leaderboard.";
        [SerializeField, Range(1, 50)] private int maximumEntries = 10;
        public static LocalLeaderboardService Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void SubmitHighScore(string boardId, string playerName, float score)
        {
            Submit(boardId, playerName, score, false);
        }

        public void SubmitBestTime(string boardId, string playerName, float seconds)
        {
            if (seconds > 0f) Submit(boardId, playerName, seconds, true);
        }

        public IReadOnlyList<LocalLeaderboardEntry> GetEntries(string boardId, bool lowerIsBetter)
        {
            LocalLeaderboardBoard board = Load(boardId);
            Sort(board.entries, lowerIsBetter);
            return board.entries;
        }

        private void Submit(string boardId, string playerName, float value, bool lowerIsBetter)
        {
            if (string.IsNullOrEmpty(boardId) || value < 0f) return;
            LocalLeaderboardBoard board = Load(boardId);
            board.entries.Add(new LocalLeaderboardEntry
            {
                playerName = string.IsNullOrEmpty(playerName) ? "YOU" : playerName,
                value = value,
                utcTicks = DateTime.UtcNow.Ticks
            });
            Sort(board.entries, lowerIsBetter);
            if (board.entries.Count > maximumEntries) board.entries.RemoveRange(maximumEntries, board.entries.Count - maximumEntries);
            PlayerPrefs.SetString(Prefix + boardId, JsonUtility.ToJson(board));
            PlayerPrefs.Save();
        }

        private static LocalLeaderboardBoard Load(string boardId)
        {
            string json = PlayerPrefs.GetString(Prefix + boardId, string.Empty);
            if (string.IsNullOrEmpty(json)) return new LocalLeaderboardBoard();
            LocalLeaderboardBoard board = JsonUtility.FromJson<LocalLeaderboardBoard>(json);
            return board ?? new LocalLeaderboardBoard();
        }

        private static void Sort(List<LocalLeaderboardEntry> entries, bool lowerIsBetter)
        {
            entries.Sort((a, b) =>
            {
                int compare = lowerIsBetter ? a.value.CompareTo(b.value) : b.value.CompareTo(a.value);
                return compare != 0 ? compare : a.utcTicks.CompareTo(b.utcTicks);
            });
        }
    }
}
