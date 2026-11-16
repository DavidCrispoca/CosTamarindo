using System;

namespace CosTamarindo.Net
{
    [Serializable]
    public class SessionStartResponse
    {
        public string session_id;
        public string nonce;
        public string session_key; // hex 64 chars
        public int expires_in;
        public int best_score;
    }

    [Serializable]
    public class ScoreRequest
    {
        public string session_id;
        public int score;
        public int duration_ms;
        public int catches;
        public string nonce;
        public string signature;
    }

    [Serializable]
    public class ScoreResponse
    {
        public bool ok;
        public int score;
        public int best_score;
        public int rank_position; // may be null in JSON but JsonUtility needs default
        public bool in_top5;
    }

    [Serializable]
    public class ClaimRequest
    {
        public string session_id;
        public string username;
        public string pin;
    }

    [Serializable]
    public class ClaimResponse
    {
        public bool ok;
        public string username;
        public int best_score;
        public int rank_position;
    }

    [Serializable]
    public class ApiError
    {
        public string code;
        public string message;
    }

    [Serializable]
    public class LeaderboardEntry
    {
        public int rank_position;
        public string username;
        public int best_score;
        public bool is_me;
    }

    [Serializable]
    public class LeaderboardResponse
    {
        public LeaderboardEntry[] entries;
    }
}