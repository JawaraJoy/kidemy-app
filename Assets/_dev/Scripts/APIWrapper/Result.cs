using UnityEngine;
using System;

namespace EduGame
{
    [Serializable]
    public class Result
    {
        public string session_id;
        public ResultItem[] result;
    }

    [Serializable]
    public class ResultItem
    {
        public int stars;
        public int time;
    }
}