using UnityEngine;

namespace EduGame
{
    public class Global
    {
        private const int c_FalseVectorScale = 100000;

        private static Vector2 s_FalseVector = Vector2.one * c_FalseVectorScale;
        public static Vector2 FALSE_VECTOR => s_FalseVector;
    }
}