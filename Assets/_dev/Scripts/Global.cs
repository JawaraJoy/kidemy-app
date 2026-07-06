using UnityEngine;

namespace EduGame
{
    public class Global
    {
        public static Vector2 FALSE_VECTOR { get; private set; } = Vector2.one * 100000;
        public static string CHALLENGE_DATA_DIR = "Assets/_dev/SO/Challenge/";
        public static string CHALLENGE_DATA_NAME = "SO_Challenge_$$.asset";
    }
}