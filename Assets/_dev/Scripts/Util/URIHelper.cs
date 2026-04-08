using System.Collections.Generic;
using UnityEngine;

namespace EduGame
{
    public class URIHelper
    {
        public static Dictionary<string, string> GetParameters(string url)
        {
            Dictionary<string, string> res = new Dictionary<string, string>();

            string[] d1 = url.Split("?");

            if(d1.Length > 0)
            {
                string v = d1[d1.Length - 1];
                string[] d2 = v.Split("&");

                if(d2.Length > 0)
                {
                    foreach (var d in d2)
                    {
                        string[] d3 = d.Split("=");

                        if(d3.Length > 1)
                            res.Add(d3[0], d3[1]);
                    }
                }
            }

            return res;
        }
    }
}