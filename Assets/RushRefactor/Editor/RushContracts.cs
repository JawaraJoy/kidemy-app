using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace RushRefactor
{
    /// <summary>
    /// Stage 0 contract gate. Records every name-bound hook (UnityEvent bindings, Animation Events,
    /// Invoke("name") strings, JSON DTO fields, WebGL JS bridge, tool-facing GameManager API, build scenes)
    /// once as a baseline, then re-checks after every refactor step. Read-only: never edits project assets.
    /// </summary>
    public static class RushContracts
    {
        const string c_BaselineFile = "contracts_baseline.txt";
        const string c_CurrentFile = "contracts_current.txt";
        const string c_DiffFile = "contracts_diff.txt";

        static readonly HashSet<string> s_YamlExt = new HashSet<string>
        {
            ".unity", ".prefab", ".asset", ".controller", ".overridecontroller", ".playable"
        };

        static readonly string[] s_DtoNames =
        {
            "Result", "ResultItem", "VoiceRequest", "VoiceResult", "VoiceResultError", "VoiceResultErrorDetail", "VoiceResponse"
        };

        static readonly string[] s_ToolApi =
        {
            "get_Instance", "Submit", "Next", "ResetQuest", "PlayQuestionVoice", "PlaySoundQuest", "IsLastQuest"
        };

        static readonly string[] s_PublicSurfaceTypes = { "EduGame.GameManager", "EduGame.GameManagerAgent" };

        static readonly Regex s_UEvent = new Regex(
            @"m_TargetAssemblyTypeName: ([^\r\n]+)\r?\n\s+m_MethodName: ([^\r\n]*)", RegexOptions.Compiled);

        static readonly Regex s_AnimFn = new Regex(@"functionName: ([^\r\n]*)", RegexOptions.Compiled);

        static readonly Regex s_Invoke = new Regex(
            @"(?<![\.\w])(Invoke|InvokeRepeating)\(\s*""([^""]+)""", RegexOptions.Compiled);

        static readonly Regex s_InvokeConst = new Regex(
            @"(?<![\.\w])(Invoke|InvokeRepeating)\(\s*(c_\w+)\s*[,)]", RegexOptions.Compiled);

        static readonly Regex s_ConstString = new Regex(
            @"\bconst\s+string\s+(c_\w+)\s*=\s*""([^""]*)""\s*;", RegexOptions.Compiled);

        static readonly Regex s_JsFn = new Regex(@"^\s*(\w+)\s*:\s*function\s*\(", RegexOptions.Multiline | RegexOptions.Compiled);

        public sealed class DiffResult
        {
            public string Verdict;
            public List<string> Missing = new List<string>();
            public List<string> Added = new List<string>();
        }

        // ---------------------------------------------------------------- menu

        [MenuItem("Tools/Rush Refactor/Contracts/Capture Baseline")]
        static void MenuCapture() { Debug.Log("[Rush] " + CaptureBaseline()); }

        [MenuItem("Tools/Rush Refactor/Contracts/Check Against Baseline")]
        static void MenuCheck() { Debug.Log("[Rush] " + Check()); }

        [MenuItem("Tools/Rush Refactor/Contracts/Self Test")]
        static void MenuSelfTest() { Debug.Log("[Rush] " + SelfTest()); }

        // ---------------------------------------------------------------- public API

        public static string CaptureBaseline()
        {
            List<string> lines = Scan();
            WriteLines(RushCommon.FileIn(c_BaselineFile), lines);
            return "BASELINE SAVED (" + lines.Count + " lines)\n" + Summarize(lines);
        }

        public static string Check()
        {
            string basePath = RushCommon.FileIn(c_BaselineFile);
            if (!File.Exists(basePath)) return "NO BASELINE: run Capture Baseline first";
            List<string> cur = Scan();
            WriteLines(RushCommon.FileIn(c_CurrentFile), cur);
            DiffResult d = Diff(File.ReadAllLines(basePath), cur);
            int allowed = ApplyAllowList(d);
            var sb = new StringBuilder();
            foreach (string s in d.Missing) sb.AppendLine("- " + s);
            foreach (string s in d.Added) sb.AppendLine("+ " + s);
            File.WriteAllText(RushCommon.FileIn(c_DiffFile), sb.ToString(), new UTF8Encoding(false));
            return "CONTRACTS " + d.Verdict + " (missing=" + d.Missing.Count + ", added=" + d.Added.Count + ", allowed=" + allowed + ")\n" +
                   (d.Missing.Count > 0 ? "First missing:\n" + string.Join("\n", d.Missing.Take(15).ToArray()) : "");
        }

        // Penghapusan kontrak yang DISENGAJA (kode mati terbukti tak terpakai) dicatat di RushBaseline/contracts_allow.txt.
        // Baseline tidak diubah; baris yang diizinkan tidak dihitung sebagai 'missing' dan jumlahnya dilaporkan (allowed=n).
        static int ApplyAllowList(DiffResult d)
        {
            string allowPath = RushCommon.FileIn("contracts_allow.txt");
            if (!File.Exists(allowPath)) return 0;
            var allow = new HashSet<string>(StringComparer.Ordinal);
            foreach (string line in File.ReadAllLines(allowPath))
            {
                if (line.Length == 0 || line[0] == '#') continue;
                allow.Add(line);
            }
            int allowed = 0;
            var keep = new List<string>();
            foreach (string s in d.Missing)
            {
                if (allow.Contains(s)) allowed++; else keep.Add(s);
            }
            d.Missing = keep;
            d.Verdict = d.Missing.Count > 0 ? "FAIL" : (d.Added.Count > 0 ? "PASS(with additions)" : "PASS");
            return allowed;
        }

        public static string SelfTest()
        {
            List<string> cur = Scan();
            var sb = new StringBuilder();
            bool ok = true;

            DiffResult same = Diff(cur, cur);
            ok &= Expect(sb, "identical => PASS", same.Verdict == "PASS");

            if (cur.Count > 2)
            {
                var removed = new List<string>(cur);
                removed.RemoveAt(1);
                ok &= Expect(sb, "removed line => FAIL", Diff(cur, removed).Verdict == "FAIL");

                var flipped = new List<string>(cur);
                flipped[0] = flipped[0] + "|TAMPERED";
                ok &= Expect(sb, "changed line => FAIL", Diff(cur, flipped).Verdict == "FAIL");

                var added = new List<string>(cur);
                added.Add("UEVENT|Fake.Type::Fake|count=1|OK");
                ok &= Expect(sb, "added line => PASS(with additions)", Diff(cur, added).Verdict == "PASS(with additions)");
            }

            List<string> second = Scan();
            ok &= Expect(sb, "two scans identical (deterministic)", Diff(cur, second).Verdict == "PASS" && cur.Count == second.Count);

            sb.Insert(0, ok ? "CONTRACT SELFTEST PASS\n" : "CONTRACT SELFTEST FAIL\n");
            return sb.ToString();
        }

        static bool Expect(StringBuilder sb, string what, bool cond)
        {
            sb.AppendLine((cond ? "  ok   " : "  FAIL ") + what);
            return cond;
        }

        public static DiffResult Diff(IList<string> baseline, IList<string> current)
        {
            var r = new DiffResult();
            var b = new HashSet<string>(baseline, StringComparer.Ordinal);
            var c = new HashSet<string>(current, StringComparer.Ordinal);
            foreach (string s in baseline) if (!c.Contains(s)) r.Missing.Add(s);
            foreach (string s in current) if (!b.Contains(s)) r.Added.Add(s);
            r.Verdict = r.Missing.Count > 0 ? "FAIL" : (r.Added.Count > 0 ? "PASS(with additions)" : "PASS");
            return r;
        }

        static void WriteLines(string path, IList<string> lines)
        {
            File.WriteAllText(path, string.Join("\n", lines.ToArray()) + "\n", new UTF8Encoding(false));
        }

        // ---------------------------------------------------------------- scan

        public static List<string> Scan()
        {
            var byFull = new Dictionary<string, Type>(StringComparer.Ordinal);
            var bySimple = new Dictionary<string, List<Type>>(StringComparer.Ordinal);
            var allTypes = new List<Type>();
            foreach (Type t in RushCommon.ProjectTypes())
            {
                allTypes.Add(t);
                if (t.FullName != null && !byFull.ContainsKey(t.FullName)) byFull[t.FullName] = t;
                List<Type> l;
                if (!bySimple.TryGetValue(t.Name, out l)) { l = new List<Type>(); bySimple[t.Name] = l; }
                l.Add(t);
            }

            var lines = new List<string>();
            lines.Add("ENV|buildTarget=" + EditorUserBuildSettings.activeBuildTarget);
            ScanUnityEvents(lines, byFull);
            ScanAnimEvents(lines, allTypes);
            ScanInvokes(lines, bySimple);
            ScanDtos(lines, allTypes);
            ScanJs(lines, allTypes);
            ScanToolApi(lines, byFull);
            ScanBuild(lines);
            lines.Sort(StringComparer.Ordinal);
            return lines;
        }

        static IEnumerable<string> FilesUnderRoots(Func<string, bool> accept)
        {
            foreach (string root in RushCommon.AssetRoots)
            {
                string abs = Path.Combine(RushCommon.ProjectRoot, root);
                if (!Directory.Exists(abs)) continue;
                foreach (string f in Directory.EnumerateFiles(abs, "*", SearchOption.AllDirectories))
                {
                    if (accept(f)) yield return f;
                }
            }
        }

        // ---- UnityEvent persistent calls (bind by type + method name)

        static void ScanUnityEvents(List<string> lines, Dictionary<string, Type> byFull)
        {
            var counts = new Dictionary<string, int>(StringComparer.Ordinal);
            var status = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (string file in FilesUnderRoots(f => s_YamlExt.Contains(Path.GetExtension(f).ToLowerInvariant())))
            {
                string text = File.ReadAllText(file);
                foreach (Match m in s_UEvent.Matches(text))
                {
                    string typeField = m.Groups[1].Value.Trim();
                    string method = m.Groups[2].Value.Trim();
                    if (method.Length == 0) continue;
                    string key;
                    string st = ResolveUnityEvent(typeField, method, byFull, out key);
                    int n;
                    counts.TryGetValue(key, out n);
                    counts[key] = n + 1;
                    status[key] = st;
                }
            }
            foreach (KeyValuePair<string, int> kv in counts)
            {
                lines.Add("UEVENT|" + kv.Key + "|count=" + kv.Value + "|" + status[kv.Key]);
            }
        }

        static string ResolveUnityEvent(string typeField, string method, Dictionary<string, Type> byFull, out string key)
        {
            int comma = typeField.IndexOf(',');
            string full = (comma >= 0 ? typeField.Substring(0, comma) : typeField).Trim();
            string asmName = comma >= 0 ? typeField.Substring(comma + 1).Trim() : "";
            key = full + "::" + method;

            Type t = null;
            string how = "OK";
            try { t = Type.GetType(typeField, false); } catch (Exception) { t = null; }
            if (t == null)
            {
                if (asmName.Length > 0 && !RushCommon.IsProjectAssembly(asmName)) return "EXT";
                Type f;
                if (byFull.TryGetValue(full, out f)) { t = f; how = "FALLBACK"; }
            }
            if (t == null) return "NOTYPE";
            if (!RushCommon.IsProjectAssembly(t.Assembly.GetName().Name)) return "EXT";

            string ms = FindMethodStatus(t, method);
            if (ms != "OK") return ms;
            return how;
        }

        static string FindMethodStatus(Type t, string method)
        {
            const BindingFlags f = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance |
                                   BindingFlags.Static | BindingFlags.DeclaredOnly;
            bool nonPublic = false;
            for (Type c = t; c != null && c != typeof(object); c = c.BaseType)
            {
                MethodInfo[] ms;
                try { ms = c.GetMethods(f); }
                catch (Exception) { continue; }
                foreach (MethodInfo m in ms)
                {
                    if (m.Name != method) continue;
                    if (m.IsPublic) return "OK";
                    nonPublic = true;
                }
            }
            return nonPublic ? "NONPUBLIC" : "NOMETHOD";
        }

        // ---- Animation Events (bind by function name on a MonoBehaviour next to the Animator)

        static void ScanAnimEvents(List<string> lines, List<Type> allTypes)
        {
            var counts = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (string file in FilesUnderRoots(f => f.EndsWith(".anim", StringComparison.OrdinalIgnoreCase)))
            {
                string text = File.ReadAllText(file);
                foreach (Match m in s_AnimFn.Matches(text))
                {
                    string fn = m.Groups[1].Value.Trim();
                    if (fn.Length == 0) continue;
                    int n;
                    counts.TryGetValue(fn, out n);
                    counts[fn] = n + 1;
                }
            }
            if (counts.Count == 0) return;

            var owners = new HashSet<string>(StringComparer.Ordinal);
            const BindingFlags f2 = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;
            foreach (Type t in allTypes)
            {
                if (!typeof(MonoBehaviour).IsAssignableFrom(t)) continue;
                try { foreach (MethodInfo m in t.GetMethods(f2)) owners.Add(m.Name); }
                catch (Exception) { }
            }
            foreach (KeyValuePair<string, int> kv in counts)
            {
                lines.Add("ANIM|" + kv.Key + "|count=" + kv.Value + "|" + (owners.Contains(kv.Key) ? "OK" : "NOOWNER"));
            }
        }

        // ---- Invoke("name") / InvokeRepeating("name") string calls

        static void ScanInvokes(List<string> lines, Dictionary<string, List<Type>> bySimple)
        {
            var counts = new Dictionary<string, int>(StringComparer.Ordinal);
            var status = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (string file in FilesUnderRoots(f => f.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)))
            {
                string cls = Path.GetFileNameWithoutExtension(file);
                string[] allLines = File.ReadAllLines(file);
                var constMap = new Dictionary<string, string>(StringComparer.Ordinal);
                foreach (string cl in allLines)
                    foreach (Match cm in s_ConstString.Matches(cl)) constMap[cm.Groups[1].Value] = cm.Groups[2].Value;
                foreach (string raw in allLines)
                {
                    string line = raw.TrimStart();
                    if (line.StartsWith("//", StringComparison.Ordinal) || line.StartsWith("*", StringComparison.Ordinal) ||
                        line.StartsWith("/*", StringComparison.Ordinal)) continue;
                    foreach (Match m in s_Invoke.Matches(raw))
                    {
                        string name = m.Groups[2].Value;
                        string key = cls + ".cs|" + name;
                        int n;
                        counts.TryGetValue(key, out n);
                        counts[key] = n + 1;
                        if (!status.ContainsKey(key)) status[key] = InvokeStatus(cls, name, bySimple);
                    }
                    foreach (Match m in s_InvokeConst.Matches(raw))
                    {
                        string cname = m.Groups[2].Value;
                        string name;
                        if (!constMap.TryGetValue(cname, out name)) name = "UNRESOLVED:" + cname;
                        string key = cls + ".cs|" + name;
                        int n;
                        counts.TryGetValue(key, out n);
                        counts[key] = n + 1;
                        if (!status.ContainsKey(key)) status[key] = name.StartsWith("UNRESOLVED:", StringComparison.Ordinal) ? "UNRESOLVED" : InvokeStatus(cls, name, bySimple);
                    }
                }
            }
            foreach (KeyValuePair<string, int> kv in counts)
            {
                lines.Add("INVOKE|" + kv.Key + "|count=" + kv.Value + "|" + status[kv.Key]);
            }
        }

        static string InvokeStatus(string cls, string method, Dictionary<string, List<Type>> bySimple)
        {
            List<Type> types;
            if (!bySimple.TryGetValue(cls, out types) || types.Count == 0) return "NOCLASS";
            bool nonPublic = false;
            foreach (Type t in types)
            {
                string s = FindMethodStatus(t, method);
                if (s == "OK") return "OK";
                if (s == "NONPUBLIC") nonPublic = true;
            }
            // Invoke() works on private methods too, so a non-public hit is still a live target.
            return nonPublic ? "OK" : "NOMETHOD";
        }

        // ---- JSON DTOs (JsonUtility maps JSON keys to C# field names, so renaming a field breaks the API)

        static void ScanDtos(List<string> lines, List<Type> allTypes)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (Type t in allTypes)
            {
                if (Array.IndexOf(s_DtoNames, t.Name) < 0) continue;
                if ((t.Attributes & TypeAttributes.Serializable) == 0) continue;
                if (typeof(UnityEngine.Object).IsAssignableFrom(t)) continue;
                seen.Add(t.Name);
                const BindingFlags f = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;
                var fields = new List<string>();
                foreach (FieldInfo fi in t.GetFields(f))
                {
                    bool ser = fi.IsPublic || fi.GetCustomAttributes(typeof(SerializeField), false).Length > 0;
                    if (!ser) continue;
                    fields.Add(fi.Name + ":" + fi.FieldType.Name);
                }
                fields.Sort(StringComparer.Ordinal);
                lines.Add("DTO|" + t.FullName + "|fields=" + string.Join(",", fields.ToArray()) + "|OK");
            }
            foreach (string name in s_DtoNames)
            {
                if (!seen.Contains(name)) lines.Add("DTO|" + name + "|NOTFOUND|MISSING");
            }
        }

        // ---- WebGL JS bridge (extern methods must match .jslib functions by name)

        static void ScanJs(List<string> lines, List<Type> allTypes)
        {
            var jsFunctions = new HashSet<string>(StringComparer.Ordinal);
            foreach (string file in Directory.GetFiles(Application.dataPath, "*.jslib", SearchOption.AllDirectories))
            {
                var names = new List<string>();
                foreach (Match m in s_JsFn.Matches(File.ReadAllText(file)))
                {
                    names.Add(m.Groups[1].Value);
                    jsFunctions.Add(m.Groups[1].Value);
                }
                names.Sort(StringComparer.Ordinal);
                lines.Add("JSLIB|" + Path.GetFileName(file) + "|functions=" + string.Join(",", names.ToArray()));
            }

            const BindingFlags f = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance |
                                   BindingFlags.Static | BindingFlags.DeclaredOnly;
            foreach (Type t in allTypes)
            {
                MethodInfo[] ms;
                try { ms = t.GetMethods(f); }
                catch (Exception) { continue; }
                foreach (MethodInfo m in ms)
                {
                    if ((m.Attributes & MethodAttributes.PinvokeImpl) == 0) continue;
                    lines.Add("EXTERN|" + t.FullName + "." + m.Name + "|" + (jsFunctions.Contains(m.Name) ? "OK" : "NOJS"));
                }
            }
        }

        // ---- Tool-facing API (editor tools, smoke test and possibly the host page call these by name)

        static void ScanToolApi(List<string> lines, Dictionary<string, Type> byFull)
        {
            const BindingFlags f = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance |
                                   BindingFlags.Static | BindingFlags.DeclaredOnly;
            Type gm;
            byFull.TryGetValue("EduGame.GameManager", out gm);
            foreach (string name in s_ToolApi)
            {
                bool any = false;
                if (gm != null)
                {
                    foreach (MethodInfo m in gm.GetMethods(f))
                    {
                        if (m.Name != name) continue;
                        any = true;
                        lines.Add("TOOLAPI|EduGame.GameManager::" + name + "|" + Vis(m) + "|params=" + ParamList(m) + "|ret=" + m.ReturnType.Name);
                    }
                }
                if (!any) lines.Add("TOOLAPI|EduGame.GameManager::" + name + "|MISSING");
            }

            // Public surface of the objects a host page could reach through SendMessage.
            foreach (string typeName in s_PublicSurfaceTypes)
            {
                Type t;
                if (!byFull.TryGetValue(typeName, out t)) { lines.Add("PUBAPI|" + typeName + "|MISSING"); continue; }
                foreach (MethodInfo m in t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                {
                    if (m.IsSpecialName) continue;
                    lines.Add("PUBAPI|" + typeName + "::" + m.Name + "|params=" + ParamList(m));
                }
            }
        }

        static string Vis(MethodInfo m)
        {
            return (m.IsPublic ? "public" : "nonpublic") + (m.IsStatic ? ",static" : "");
        }

        static string ParamList(MethodInfo m)
        {
            return string.Join(",", m.GetParameters().Select(p => p.ParameterType.Name).ToArray());
        }

        // ---- Scenes in the build

        static void ScanBuild(List<string> lines)
        {
            foreach (EditorBuildSettingsScene s in EditorBuildSettings.scenes)
            {
                lines.Add("BUILD|" + s.path + "|enabled=" + s.enabled);
            }
        }

        // ---------------------------------------------------------------- reporting

        static readonly string[] s_ProblemStatuses =
        {
            "NOMETHOD", "NOTYPE", "NONPUBLIC", "FALLBACK", "NOJS", "NOOWNER", "NOCLASS", "MISSING"
        };

        public static string Summarize(IList<string> lines)
        {
            var sb = new StringBuilder();
            foreach (IGrouping<string, string> g in lines.GroupBy(l => l.Substring(0, l.IndexOf('|'))).OrderBy(g2 => g2.Key))
            {
                sb.AppendLine("  " + g.Key + ": " + g.Count() + " lines");
            }

            int total = 0;
            var st = new SortedDictionary<string, int>(StringComparer.Ordinal);
            foreach (string l in lines.Where(x => x.StartsWith("UEVENT|", StringComparison.Ordinal)))
            {
                string[] p = l.Split('|');
                int n = ParseCount(p[2]);
                total += n;
                int cur;
                st.TryGetValue(p[3], out cur);
                st[p[3]] = cur + n;
            }
            sb.AppendLine("  UnityEvent bindings total=" + total + " : " +
                          string.Join(", ", st.Select(kv => kv.Key + "=" + kv.Value).ToArray()));

            var problems = Problems(lines);
            sb.AppendLine("  non-OK lines: " + problems.Count);
            foreach (string p in problems) sb.AppendLine("    " + p);
            return sb.ToString();
        }

        public static List<string> Problems(IEnumerable<string> lines)
        {
            var res = new List<string>();
            foreach (string l in lines)
            {
                string last = l.Substring(l.LastIndexOf('|') + 1);
                if (Array.IndexOf(s_ProblemStatuses, last) >= 0) res.Add(l);
            }
            return res;
        }

        static int ParseCount(string field)
        {
            int n;
            return int.TryParse(field.Replace("count=", ""), out n) ? n : 0;
        }
    }
}
