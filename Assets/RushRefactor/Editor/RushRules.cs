using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace RushRefactor
{
    /// <summary>
    /// Tahap 5: aturan arsitektur yang dijaga setelah refactor (read-only, tidak mengubah aset).
    /// RC-5a/b/c: tidak ada Instance yang membaca GameManager / FeedbackManager / AssetManager lewat global ("X.Instance"),
    ///            kecuali berkas pada daftar pengecualian terdokumentasi (E-7: FeedbackManager menunggu paket Feel; E-5: kode Editor-only).
    /// RC-5d: tidak ada pencarian global di kode runtime (Find*, SendMessage, BroadcastMessage, transform.root).
    /// Komentar dan literal string diabaikan; kode Editor (folder Editor), _Archives, dan RushRefactor tidak dipindai.
    /// Batas yang diketahui: isi lubang string interpolasi ($"{...}") ikut diabaikan.
    /// </summary>
    public static class RushRules
    {
        const string c_OutFile = "rules_current.txt";
        const string c_AllowId = "E-7";
        const string c_AllowIdEditor = "E-5";

        sealed class Rule
        {
            public string Id;
            public Regex Pattern;
        }

        sealed class Hit
        {
            public string Rule;
            public int Line;
            public string Text;
        }

        static readonly Rule[] s_Rules =
        {
            new Rule { Id = "RC-5a", Pattern = new Regex(@"\bGameManager\s*\.\s*Instance\b", RegexOptions.Compiled) },
            new Rule { Id = "RC-5b", Pattern = new Regex(@"\bFeedbackManager\s*\.\s*Instance\b", RegexOptions.Compiled) },
            new Rule { Id = "RC-5c", Pattern = new Regex(@"\bAssetManager\s*\.\s*Instance\b", RegexOptions.Compiled) },
            new Rule { Id = "RC-5d", Pattern = new Regex(@"\b(FindObjectOfType|FindObjectsOfType|FindFirstObjectByType|FindAnyObjectByType|FindObjectsByType|FindWithTag|FindGameObjectsWithTag|SendMessageUpwards|SendMessage|BroadcastMessage)\b|\bGameObject\s*\.\s*Find\b|\btransform\s*\.\s*root\b", RegexOptions.Compiled) }
        };

        // pengecualian terdokumentasi: "aturan|nama berkas" -> jumlah pemakaian maksimum
        static readonly Dictionary<string, int> s_Allow = new Dictionary<string, int>
        {
            { "RC-5a|FeedbackItemAudio.cs", 3 },
            { "RC-5b|QuestShapeIt.cs", 3 },
            { "RC-5b|QuestMultipleChoicesItem.cs", 3 },
            // E-5: kode Editor-only (#if UNITY_EDITOR) di folder runtime
            { "RC-5d|QuestAudioPopulatorWindow.cs", 2 },
            { "RC-5d|SceneButtonGenerator.cs", 1 }
        };

        static readonly string[] s_Roots = { "Assets/_dev", "Assets/_dev2" };

        [MenuItem("Tools/Rush Refactor/Rules/Check")]
        static void MenuCheck() { Debug.Log("[Rush] " + Check()); }

        [MenuItem("Tools/Rush Refactor/Rules/Self Test")]
        static void MenuSelfTest() { Debug.Log("[Rush] " + SelfTest()); }

        public static string Check()
        {
            var allowed = new List<string>();
            int files;
            List<string> violations = Scan(allowed, out files);
            var sb = new StringBuilder();
            foreach (string v in violations) sb.AppendLine(v);
            foreach (string a in allowed) sb.AppendLine(a);
            File.WriteAllText(RushCommon.FileIn(c_OutFile), sb.ToString(), new UTF8Encoding(false));
            string head = violations.Count == 0 ? "RULES PASS" : "RULES FAIL";
            return head + " (files=" + files + ", violations=" + violations.Count + ", documented exceptions=" + allowed.Count + ")" +
                   (violations.Count > 0 ? "\n" + string.Join("\n", violations.Take(20).ToArray()) : "");
        }

        public static List<string> Scan(List<string> allowed, out int fileCount)
        {
            var res = new List<string>();
            fileCount = 0;
            string proj = RushCommon.ProjectRoot.Replace('\\', '/').TrimEnd('/');
            foreach (string r in s_Roots)
            {
                string abs = Path.Combine(proj, r);
                if (!Directory.Exists(abs)) continue;
                string[] files = Directory.GetFiles(abs, "*.cs", SearchOption.AllDirectories);
                Array.Sort(files, StringComparer.Ordinal);
                foreach (string f in files)
                {
                    string norm = f.Replace('\\', '/');
                    if (norm.Contains("/_Archives/") || norm.Contains("/Editor/")) continue;
                    string rel = norm.StartsWith(proj + "/", StringComparison.Ordinal) ? norm.Substring(proj.Length + 1) : norm;
                    fileCount++;
                    res.AddRange(Evaluate(rel, File.ReadAllText(f), allowed));
                }
            }
            return res;
        }

        public static List<string> Evaluate(string relPath, string raw, List<string> allowed)
        {
            string file = Path.GetFileName(relPath);
            var res = new List<string>();
            foreach (IGrouping<string, Hit> g in FindHits(Strip(raw)).GroupBy(h => h.Rule))
            {
                int allow;
                s_Allow.TryGetValue(g.Key + "|" + file, out allow);
                int count = g.Count();
                if (allow > 0 && count <= allow)
                {
                    if (allowed != null) allowed.Add("ALLOWED|" + (g.Key == "RC-5d" ? c_AllowIdEditor : c_AllowId) + "|" + g.Key + "|" + relPath + "|count=" + count + "/" + allow);
                }
                else
                {
                    foreach (Hit h in g) res.Add("VIOLATION|" + h.Rule + "|" + relPath + ":" + h.Line + "|" + h.Text);
                }
            }
            return res;
        }

        static List<Hit> FindHits(string code)
        {
            var hits = new List<Hit>();
            foreach (Rule r in s_Rules)
            {
                foreach (Match m in r.Pattern.Matches(code))
                {
                    hits.Add(new Hit { Rule = r.Id, Line = LineOf(code, m.Index), Text = Regex.Replace(m.Value, @"\s+", "") });
                }
            }
            return hits;
        }

        static int LineOf(string code, int index)
        {
            int line = 1;
            for (int i = 0; i < index && i < code.Length; i++) if (code[i] == '\n') line++;
            return line;
        }

        // ganti komentar dan isi literal string/char dengan spasi (baris baru dipertahankan supaya nomor baris tetap)
        public static string Strip(string s)
        {
            var sb = new StringBuilder(s.Length);
            int i = 0, n = s.Length;
            while (i < n)
            {
                char c = s[i];
                char d = i + 1 < n ? s[i + 1] : '\0';
                if (c == '/' && d == '/')
                {
                    while (i < n && s[i] != '\n') { sb.Append(' '); i++; }
                }
                else if (c == '/' && d == '*')
                {
                    sb.Append("  ");
                    i += 2;
                    while (i < n && !(s[i] == '*' && i + 1 < n && s[i + 1] == '/')) { sb.Append(s[i] == '\n' ? '\n' : ' '); i++; }
                    if (i < n) { sb.Append("  "); i += 2; }
                }
                else if (c == '"')
                {
                    bool verbatim = (i >= 1 && s[i - 1] == '@') || (i >= 2 && s[i - 1] == '$' && s[i - 2] == '@');
                    sb.Append('"');
                    i++;
                    while (i < n)
                    {
                        if (verbatim)
                        {
                            if (s[i] == '"')
                            {
                                if (i + 1 < n && s[i + 1] == '"') { sb.Append("  "); i += 2; continue; }
                                break;
                            }
                        }
                        else
                        {
                            if (s[i] == '\\' && i + 1 < n) { sb.Append(s[i + 1] == '\n' ? " \n" : "  "); i += 2; continue; }
                            if (s[i] == '"' || s[i] == '\n') break;
                        }
                        sb.Append(s[i] == '\n' ? '\n' : ' ');
                        i++;
                    }
                    if (i < n && s[i] == '"') { sb.Append('"'); i++; }
                }
                else if (c == '\'')
                {
                    sb.Append('\'');
                    i++;
                    while (i < n && s[i] != '\'' && s[i] != '\n')
                    {
                        if (s[i] == '\\' && i + 1 < n) { sb.Append("  "); i += 2; }
                        else { sb.Append(' '); i++; }
                    }
                    if (i < n && s[i] == '\'') { sb.Append('\''); i++; }
                }
                else { sb.Append(c); i++; }
            }
            return sb.ToString();
        }

        public static string SelfTest()
        {
            var sb = new StringBuilder();
            bool ok = true;

            ok &= Expect(sb, "kode biasa => VIOLATION RC-5a", Count("Foo.cs", "void A(){ GameManager.Instance.Play(); }", false) == 1);
            ok &= Expect(sb, "spasi di sekitar titik tetap terdeteksi", Count("Foo.cs", "var g = GameManager . Instance;", false) == 1);
            ok &= Expect(sb, "komentar baris diabaikan", Count("Foo.cs", "// GameManager.Instance.Play();", false) == 0);
            ok &= Expect(sb, "komentar blok diabaikan", Count("Foo.cs", "/* GameManager.Instance\n.Play(); */ int x;", false) == 0);
            ok &= Expect(sb, "string biasa diabaikan", Count("Foo.cs", "Debug.Log(\"GameManager.Instance is null\");", false) == 0);
            ok &= Expect(sb, "string verbatim diabaikan", Count("Foo.cs", "var s = @\"a \"\"GameManager.Instance\"\" b\";", false) == 0);
            ok &= Expect(sb, "kode setelah string tetap dipindai", Count("Foo.cs", "var s = \"x\"; GameManager.Instance.Play();", false) == 1);
            ok &= Expect(sb, "FeedbackManager.Instance => RC-5b", Count("Foo.cs", "FeedbackManager.Instance.ButtonOver.Play();", false) == 1);
            ok &= Expect(sb, "AssetManager.Instance => RC-5c", Count("Foo.cs", "var a = AssetManager.Instance;", false) == 1);
            ok &= Expect(sb, "FindObjectOfType => RC-5d", Count("Foo.cs", "var x = FindObjectOfType<Foo>();", false) == 1);
            ok &= Expect(sb, "transform.root => RC-5d", Count("Foo.cs", "var r = transform.root;", false) == 1);
            ok &= Expect(sb, "nomor baris benar", Line("Foo.cs", "int a;\n// c\nint b = GameManager.Instance.X;") == 3);

            string fiaOk = "a GameManager.Instance.A; b GameManager.Instance.B; c GameManager.Instance.C;";
            ok &= Expect(sb, "pengecualian FeedbackItemAudio 3 pemakaian => diizinkan", Count("FeedbackItemAudio.cs", fiaOk, false) == 0);
            ok &= Expect(sb, "pengecualian FeedbackItemAudio 4 pemakaian => VIOLATION", Count("FeedbackItemAudio.cs", fiaOk + " GameManager.Instance.D;", false) == 4);
            ok &= Expect(sb, "pengecualian tidak berlaku untuk berkas lain", Count("Other.cs", fiaOk, false) == 3);
            ok &= Expect(sb, "pengecualian GameManager tidak membuka FeedbackManager", Count("FeedbackItemAudio.cs", "FeedbackManager.Instance.X;", false) == 1);

            sb.Insert(0, ok ? "RULES SELFTEST PASS\n" : "RULES SELFTEST FAIL\n");
            return sb.ToString();
        }

        static int Count(string file, string code, bool unused)
        {
            return Evaluate("Assets/_dev/" + file, code, null).Count;
        }

        static int Line(string file, string code)
        {
            List<string> v = Evaluate("Assets/_dev/" + file, code, null);
            if (v.Count == 0) return -1;
            string loc = v[0].Split('|')[2];
            return int.Parse(loc.Substring(loc.LastIndexOf(':') + 1));
        }

        static bool Expect(StringBuilder sb, string what, bool cond)
        {
            sb.AppendLine((cond ? "  ok   " : "  FAIL ") + what);
            return cond;
        }
    }
}
