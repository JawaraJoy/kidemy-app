using UnityEngine;

namespace EduGame
{
    public class Global
    {
        private const int c_FalseVectorScale = 100000;

        private static Vector2 s_FalseVector = Vector2.one * c_FalseVectorScale;
        public static Vector2 FALSE_VECTOR => s_FalseVector;
        private const string c_ChallengeDataDir = "Assets/_dev/SO/Challenge/";
        private const string c_ChallengeDataName = "SO_Challenge_$$.asset";
        public static string CHALLENGE_DATA_DIR => c_ChallengeDataDir;
        public static string CHALLENGE_DATA_NAME => c_ChallengeDataName;
    }
}