using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace RushRefactor
{
    /// <summary>
    /// Stage 1 tool. Renames non-public fields to the Rush convention (m_ / c_ / s_) and adds [FormerlySerializedAs]
    /// for serialized ones. It lexes and parses C# (it is not a blind text replace): it understands strings, interpolation
    /// holes, comments, #if regions, scopes, shadowing by locals/parameters, named arguments, object initializers and
    /// derived classes in other files. It never edits anything on DryRun; Apply makes a backup first.
    /// The IL gate (RushIL) is the safety net for anything this tool could still get wrong.
    /// </summary>
    public static partial class RushRename
    {
        enum TK { Ident, Punct, Str, Chr, Num }

        sealed class Tok
        {
            public TK Kind;
            public int S;
            public int E;
            public string T;
            public bool Cond;
        }

        sealed class Dir
        {
            public int S;
            public string T;
        }

        sealed class Member
        {
            public string Kind = "Other"; // Field, Method, Property, Type, Event
            public int Start;
            public int End;
            public int HeaderStart;
            public int Term;
            public bool HasEvent;
            public readonly List<int[]> Attrs = new List<int[]>();
            public readonly List<string> Mods = new List<string>();
            public readonly List<int> Decls = new List<int>();
        }

        sealed class TypeDecl
        {
            public string Name;
            public string Kind;
            public string FullName;
            public int NameTok;
            public int Open;
            public int Close;
            public Model File;
            public TypeDecl Outer;
            public readonly List<string> Bases = new List<string>();
            public readonly List<Member> Members = new List<Member>();
        }

        sealed class Model
        {
            public string Path;
            public string Rel;
            public string Text;
            public string Namespace = "";
            public readonly List<Tok> Toks = new List<Tok>();
            public readonly List<Dir> Dirs = new List<Dir>();
            public readonly List<TypeDecl> Types = new List<TypeDecl>();
            public readonly List<string> Problems = new List<string>();
            public int[] Match;
            public int[] Encl;
            public TypeDecl[] OwnerType;
            public Member[] MemberAt;
            public int[] LineStarts;
            public readonly Dictionary<Member, HashSet<string>> DeclCache = new Dictionary<Member, HashSet<string>>();
        }

        sealed class Target
        {
            public Model File;
            public TypeDecl Type;
            public Member Member;
            public int DeclTok;
            public string Old;
            public string New;
            public string Vis;   // private | protected
            public string Shape; // instance | static | const
            public bool Serialized;
            public string Manual; // reason when it cannot be done automatically
            public int Renamed;
            public int ShadowSkipped;
            public int ThisForced;
            public int InCond;
            public readonly List<string> Notes = new List<string>();
            public readonly HashSet<string> TouchedFiles = new HashSet<string>();
        }

        sealed class Edit
        {
            public int Start;
            public int End;
            public string Text;
        }

        sealed class PlanResult
        {
            public readonly List<Model> Models = new List<Model>();
            public readonly List<Target> Targets = new List<Target>();
            public readonly Dictionary<Model, List<Edit>> Edits = new Dictionary<Model, List<Edit>>();
            public readonly List<string> Warnings = new List<string>();
        }

        static readonly HashSet<string> s_Keywords = new HashSet<string>
        {
            "abstract","as","base","bool","break","byte","case","catch","char","checked","class","const","continue","decimal",
            "default","delegate","do","double","else","enum","event","explicit","extern","false","finally","fixed","float","for",
            "foreach","goto","if","implicit","in","int","interface","internal","is","lock","long","namespace","new","null","object",
            "operator","out","override","params","private","protected","public","readonly","ref","return","sbyte","sealed","short",
            "sizeof","stackalloc","static","string","struct","switch","this","throw","true","try","typeof","uint","ulong",
            "unchecked","unsafe","ushort","using","virtual","void","volatile","while",
            "var","await","yield","async","when","where","and","or","not","with","from","select","let","into","orderby","join",
            "on","equals","by","ascending","descending","dynamic"
        };

        static readonly HashSet<string> s_TypeKeywords = new HashSet<string>
        {
            "bool","byte","char","decimal","double","float","int","long","object","sbyte","short","string","uint","ulong","ushort","var","dynamic"
        };

        static readonly HashSet<string> s_Mods = new HashSet<string>
        {
            "public","private","protected","internal","static","readonly","const","volatile","new","unsafe","override","virtual",
            "abstract","extern","sealed","partial","async","implicit","explicit","fixed","required"
        };

        static readonly HashSet<string> s_Two = new HashSet<string>
        {
            "=>","==","!=","<=",">=","&&","||","??","?.","::","++","--","+=","-=","*=","/=","%=","|=","&=","^=","->"
        };

        static HashSet<string> s_AllTypeNames;

        // ================================================================ menu

        [MenuItem("Tools/Rush Refactor/Rename/Dry Run (all files)")]
        static void MenuDry() { Debug.Log("[Rush] " + DryRun("")); }

        [MenuItem("Tools/Rush Refactor/Rename/Self Test")]
        static void MenuSelfTest() { Debug.Log("[Rush] " + SelfTest()); }

        // ================================================================ public API

        public static string DryRun(string filePrefix)
        {
            PlanResult plan = BuildPlan(LoadProjectModels(), filePrefix);
            string report = Report(plan, true);
            File.WriteAllText(RushCommon.FileIn("stage1_plan.txt"), report, new UTF8Encoding(false));
            return Summary(plan);
        }

        public static string Apply(string batchName, string filePrefix)
        {
            if (EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode) return "ABORT: editor busy";
            PlanResult plan = BuildPlan(LoadProjectModels(), filePrefix);
            int manual = plan.Targets.Count(t => t.Manual != null);
            var sb = new StringBuilder();
            sb.AppendLine(Summary(plan));
            if (plan.Targets.Count == 0) return "NOTHING TO DO\n" + sb;
            if (plan.Warnings.Count > 0) return "ABORT: plan has warnings (see stage1_plan.txt)\n" + string.Join("\n", plan.Warnings.ToArray());

            string backupRoot = Path.Combine(RushCommon.BaselineDir, "backup/" + batchName);
            if (Directory.Exists(backupRoot) && Directory.GetFiles(backupRoot, "*", SearchOption.AllDirectories).Length > 0)
                return "ABORT: backup folder already exists for batch '" + batchName + "' (choose a new name)";

            // every file must round-trip before anything is written
            foreach (KeyValuePair<Model, List<Edit>> kv in plan.Edits)
            {
                if (kv.Value.Count == 0) continue;
                byte[] orig = File.ReadAllBytes(kv.Key.Path);
                if (!new UTF8Encoding(false).GetBytes(kv.Key.Text).SequenceEqual(orig))
                    return "ABORT: file does not round-trip as UTF-8 without BOM: " + kv.Key.Rel;
            }

            var written = new List<string>();
            foreach (KeyValuePair<Model, List<Edit>> kv in plan.Edits)
            {
                Model m = kv.Key;
                if (kv.Value.Count == 0) continue;
                string newText = ApplyEdits(m, kv.Value);
                string bak = Path.Combine(backupRoot, m.Rel);
                Directory.CreateDirectory(Path.GetDirectoryName(bak));
                File.WriteAllBytes(bak, File.ReadAllBytes(m.Path));
                File.WriteAllBytes(m.Path, new UTF8Encoding(false).GetBytes(newText));
                written.Add(m.Rel);
            }
            sb.AppendLine("WRITTEN " + written.Count + " files (backup in RushBaseline/backup/" + batchName + ")");
            foreach (string w in written) sb.AppendLine("  " + w);
            if (manual > 0) sb.AppendLine("NOTE: " + manual + " field(s) need manual handling, see stage1_plan.txt");
            File.WriteAllText(RushCommon.FileIn("stage1_plan_applied_" + batchName + ".txt"), Report(plan, true), new UTF8Encoding(false));
            return sb.ToString();
        }

        public static string Restore(string batchName)
        {
            string backupRoot = Path.Combine(RushCommon.BaselineDir, "backup/" + batchName);
            if (!Directory.Exists(backupRoot)) return "no backup named " + batchName;
            int n = 0;
            foreach (string f in Directory.GetFiles(backupRoot, "*", SearchOption.AllDirectories))
            {
                string rel = f.Substring(backupRoot.Length + 1).Replace('\\', '/');
                File.WriteAllBytes(Path.Combine(RushCommon.ProjectRoot, rel), File.ReadAllBytes(f));
                n++;
            }
            return "RESTORED " + n + " files from batch " + batchName;
        }

        // ================================================================ loading

        static List<Model> LoadProjectModels()
        {
            var list = new List<Model>();
            foreach (string root in RushCommon.AssetRoots)
            {
                string abs = Path.Combine(RushCommon.ProjectRoot, root);
                foreach (string f in Directory.GetFiles(abs, "*.cs", SearchOption.AllDirectories).OrderBy(x => x, StringComparer.Ordinal))
                {
                    string rel = f.Substring(RushCommon.ProjectRoot.Length + 1).Replace('\\', '/');
                    list.Add(Build(rel, f, File.ReadAllText(f, new UTF8Encoding(false))));
                }
            }
            return list;
        }

        static Model Build(string rel, string path, string text)
        {
            var m = new Model { Path = path, Rel = rel, Text = text };
            try
            {
                Lex(text, 0, text.Length, m.Toks, m.Dirs, true);
                MarkCond(m);
                CheckCond(m);
                if (ComputeMatch(m))
                {
                    FindNamespace(m);
                    FindTypes(m);
                    foreach (TypeDecl td in m.Types)
                        if (td.Kind == "class" || td.Kind == "struct") ParseMembers(m, td);
                    AssignOwners(m);
                }
            }
            catch (Exception e)
            {
                m.Problems.Add("exception while parsing: " + e.GetType().Name + " " + e.Message);
            }
            return m;
        }

        // ================================================================ lexer

        static void Lex(string s, int begin, int to, List<Tok> toks, List<Dir> dirs, bool top)
        {
            int i = begin;
            bool lineStart = top;
            while (i < to)
            {
                char c = s[i];
                if (c == '\n') { lineStart = top; i++; continue; }
                if (c == ' ' || c == '\t' || c == '\r') { i++; continue; }
                if (top && lineStart && c == '#')
                {
                    int e = i;
                    while (e < to && s[e] != '\n') e++;
                    dirs.Add(new Dir { S = i, T = s.Substring(i, e - i).Trim() });
                    i = e;
                    continue;
                }
                lineStart = false;
                if (c == '/' && i + 1 < to && s[i + 1] == '/') { while (i < to && s[i] != '\n') i++; continue; }
                if (c == '/' && i + 1 < to && s[i + 1] == '*')
                {
                    int e = s.IndexOf("*/", i + 2, StringComparison.Ordinal);
                    i = (e < 0 || e >= to) ? to : e + 2;
                    continue;
                }
                if (c == '"') { i = LexString(s, i, i, to, false, false, toks, dirs); continue; }
                if (c == '@' && i + 1 < to && s[i + 1] == '"') { i = LexString(s, i, i + 1, to, true, false, toks, dirs); continue; }
                if (c == '$' && i + 1 < to)
                {
                    if (s[i + 1] == '"') { i = LexString(s, i, i + 1, to, false, true, toks, dirs); continue; }
                    if (s[i + 1] == '@' && i + 2 < to && s[i + 2] == '"') { i = LexString(s, i, i + 2, to, true, true, toks, dirs); continue; }
                }
                if (c == '@' && i + 2 < to && s[i + 1] == '$' && s[i + 2] == '"') { i = LexString(s, i, i + 2, to, true, true, toks, dirs); continue; }
                if (c == '\'')
                {
                    int e = SkipChar(s, i, to);
                    toks.Add(new Tok { Kind = TK.Chr, S = i, E = e, T = "'" });
                    i = e;
                    continue;
                }
                if (char.IsDigit(c) || (c == '.' && i + 1 < to && char.IsDigit(s[i + 1])))
                {
                    int e = i;
                    bool hex = c == '0' && i + 1 < to && (s[i + 1] == 'x' || s[i + 1] == 'X');
                    while (e < to)
                    {
                        char d = s[e];
                        if (char.IsLetterOrDigit(d) || d == '_')
                        {
                            if (!hex && (d == 'e' || d == 'E') && e + 1 < to && (s[e + 1] == '+' || s[e + 1] == '-')) { e += 2; continue; }
                            e++;
                            continue;
                        }
                        if (d == '.' && e + 1 < to && char.IsDigit(s[e + 1])) { e++; continue; }
                        break;
                    }
                    toks.Add(new Tok { Kind = TK.Num, S = i, E = e, T = s.Substring(i, e - i) });
                    i = e;
                    continue;
                }
                if (char.IsLetter(c) || c == '_' || (c == '@' && i + 1 < to && (char.IsLetter(s[i + 1]) || s[i + 1] == '_')))
                {
                    int st = i;
                    if (c == '@') i++;
                    int nameStart = i;
                    while (i < to && (char.IsLetterOrDigit(s[i]) || s[i] == '_')) i++;
                    toks.Add(new Tok { Kind = TK.Ident, S = st, E = i, T = s.Substring(nameStart, i - nameStart) });
                    continue;
                }
                if (i + 1 < to)
                {
                    string two = s.Substring(i, 2);
                    if (s_Two.Contains(two))
                    {
                        toks.Add(new Tok { Kind = TK.Punct, S = i, E = i + 2, T = two });
                        i += 2;
                        continue;
                    }
                }
                toks.Add(new Tok { Kind = TK.Punct, S = i, E = i + 1, T = c.ToString() });
                i++;
            }
        }

        static int LexString(string s, int tokStart, int q, int to, bool verbatim, bool interp, List<Tok> toks, List<Dir> dirs)
        {
            var str = new Tok { Kind = TK.Str, S = tokStart, T = "\"" };
            toks.Add(str);
            int i = q + 1;
            while (i < to)
            {
                char c = s[i];
                if (verbatim)
                {
                    if (c == '"')
                    {
                        if (i + 1 < to && s[i + 1] == '"') { i += 2; continue; }
                        i++;
                        break;
                    }
                }
                else
                {
                    if (c == '\\') { i += 2; continue; }
                    if (c == '"') { i++; break; }
                }
                if (interp && c == '{')
                {
                    if (i + 1 < to && s[i + 1] == '{') { i += 2; continue; }
                    int j = i + 1;
                    int depth = 0;
                    int exprEnd = -1;
                    while (j < to)
                    {
                        char d = s[j];
                        if (d == '"') { j = SkipPlain(s, j, to); continue; }
                        if (d == '@' && j + 1 < to && s[j + 1] == '"') { j = SkipVerbatim(s, j + 1, to); continue; }
                        if (d == '\'') { j = SkipChar(s, j, to); continue; }
                        if (d == '(' || d == '[' || d == '{') depth++;
                        else if (d == ')' || d == ']') depth--;
                        else if (d == '}')
                        {
                            if (depth == 0) { exprEnd = j; break; }
                            depth--;
                        }
                        else if (depth == 0 && (d == ':' || d == ',')) { exprEnd = j; break; }
                        j++;
                    }
                    if (exprEnd < 0) { str.E = to; return to; }
                    toks.Add(new Tok { Kind = TK.Punct, S = i, E = i, T = "#hole" }); Lex(s, i + 1, exprEnd, toks, dirs, false); toks.Add(new Tok { Kind = TK.Punct, S = exprEnd, E = exprEnd, T = "#hole" });
                    if (s[exprEnd] != '}')
                    {
                        j = exprEnd;
                        while (j < to && s[j] != '}') j++;
                        exprEnd = j;
                    }
                    i = exprEnd + 1;
                    continue;
                }
                i++;
            }
            str.E = i;
            return i;
        }

        static int SkipPlain(string s, int j, int to)
        {
            j++;
            while (j < to)
            {
                if (s[j] == '\\') { j += 2; continue; }
                if (s[j] == '"') return j + 1;
                j++;
            }
            return to;
        }

        static int SkipVerbatim(string s, int j, int to)
        {
            j++;
            while (j < to)
            {
                if (s[j] == '"')
                {
                    if (j + 1 < to && s[j + 1] == '"') { j += 2; continue; }
                    return j + 1;
                }
                j++;
            }
            return to;
        }

        static int SkipChar(string s, int j, int to)
        {
            j++;
            if (j < to && s[j] == '\\') j += 2; else j++;
            while (j < to && s[j] != '\'') j++;
            return Math.Min(j + 1, to);
        }

        // ================================================================ structure

        static void MarkCond(Model m)
        {
            int di = 0;
            int depth = 0;
            foreach (Tok t in m.Toks)
            {
                while (di < m.Dirs.Count && m.Dirs[di].S < t.S)
                {
                    string d = m.Dirs[di].T;
                    if (d.StartsWith("#if", StringComparison.Ordinal)) depth++;
                    else if (d.StartsWith("#endif", StringComparison.Ordinal)) depth--;
                    di++;
                }
                t.Cond = depth > 0;
            }
        }

        static int Delta(Tok t)
        {
            if (t.Kind != TK.Punct) return 0;
            if (t.T == "{" || t.T == "(" || t.T == "[") return 1;
            if (t.T == "}" || t.T == ")" || t.T == "]") return -1;
            return 0;
        }

        static void CheckCond(Model m)
        {
            var frames = new List<int>();
            int di = 0;
            Action<string> directive = d =>
            {
                if (d.StartsWith("#if", StringComparison.Ordinal)) frames.Add(0);
                else if (d.StartsWith("#elif", StringComparison.Ordinal) || d.StartsWith("#else", StringComparison.Ordinal))
                {
                    if (frames.Count > 0)
                    {
                        if (frames[frames.Count - 1] != 0) m.Problems.Add("unbalanced brackets inside a #if branch");
                        frames[frames.Count - 1] = 0;
                    }
                }
                else if (d.StartsWith("#endif", StringComparison.Ordinal))
                {
                    if (frames.Count > 0)
                    {
                        if (frames[frames.Count - 1] != 0) m.Problems.Add("unbalanced brackets inside a #if branch");
                        frames.RemoveAt(frames.Count - 1);
                    }
                }
            };
            foreach (Tok t in m.Toks)
            {
                while (di < m.Dirs.Count && m.Dirs[di].S < t.S) { directive(m.Dirs[di].T); di++; }
                int dl = Delta(t);
                if (dl != 0) for (int f = 0; f < frames.Count; f++) frames[f] += dl;
            }
            while (di < m.Dirs.Count) { directive(m.Dirs[di].T); di++; }
        }

        static bool Pair(string o, string c)
        {
            return (o == "(" && c == ")") || (o == "[" && c == "]") || (o == "{" && c == "}");
        }

        static bool ComputeMatch(Model m)
        {
            int n = m.Toks.Count;
            m.Match = new int[n];
            m.Encl = new int[n];
            for (int i = 0; i < n; i++) { m.Match[i] = -1; m.Encl[i] = -1; }
            var st = new Stack<int>();
            for (int i = 0; i < n; i++)
            {
                Tok t = m.Toks[i];
                if (t.Kind == TK.Punct && (t.T == "(" || t.T == "[" || t.T == "{"))
                {
                    m.Encl[i] = st.Count > 0 ? st.Peek() : -1;
                    st.Push(i);
                }
                else if (t.Kind == TK.Punct && (t.T == ")" || t.T == "]" || t.T == "}"))
                {
                    if (st.Count == 0) { m.Problems.Add("unmatched closing bracket at token " + i); return false; }
                    int o = st.Pop();
                    if (!Pair(m.Toks[o].T, t.T)) { m.Problems.Add("mismatched brackets at token " + i); return false; }
                    m.Match[o] = i;
                    m.Match[i] = o;
                    m.Encl[i] = st.Count > 0 ? st.Peek() : -1;
                }
                else m.Encl[i] = st.Count > 0 ? st.Peek() : -1;
            }
            if (st.Count > 0) { m.Problems.Add("unclosed bracket"); return false; }
            return m.Problems.Count == 0;
        }

        static void FindNamespace(Model m)
        {
            List<Tok> toks = m.Toks;
            for (int i = 0; i + 1 < toks.Count; i++)
            {
                if (toks[i].Kind == TK.Ident && toks[i].T == "namespace")
                {
                    var parts = new List<string>();
                    int j = i + 1;
                    while (j < toks.Count && (toks[j].Kind == TK.Ident || toks[j].T == "."))
                    {
                        if (toks[j].Kind == TK.Ident) parts.Add(toks[j].T);
                        j++;
                    }
                    m.Namespace = string.Join(".", parts.ToArray());
                    return;
                }
            }
        }

        static bool IsTypeKw(string t)
        {
            return t == "class" || t == "struct" || t == "interface" || t == "enum";
        }

        static void FindTypes(Model m)
        {
            List<Tok> toks = m.Toks;
            for (int i = 0; i + 1 < toks.Count; i++)
            {
                Tok t = toks[i];
                if (t.Kind != TK.Ident || !IsTypeKw(t.T)) continue;
                Tok n = toks[i + 1];
                if (n.Kind != TK.Ident || s_Keywords.Contains(n.T)) continue;
                if (i > 0)
                {
                    string p = toks[i - 1].T;
                    if (p == ":" || p == "," || p == "<" || p == "(") continue;
                }
                int j = i + 2;
                int open = -1;
                while (j < toks.Count)
                {
                    Tok x = toks[j];
                    if (x.Kind == TK.Punct)
                    {
                        if (x.T == "{") { open = j; break; }
                        if (x.T == ";") break;
                        if (x.T == "(" || x.T == "[") { j = m.Match[j] + 1; continue; }
                    }
                    j++;
                }
                if (open < 0) continue;
                var td = new TypeDecl { Name = n.T, Kind = t.T, NameTok = i + 1, Open = open, Close = m.Match[open], File = m };
                CollectBases(m, td);
                m.Types.Add(td);
            }
            m.Types.Sort((a, b) => a.Open.CompareTo(b.Open));
            foreach (TypeDecl td in m.Types)
            {
                TypeDecl best = null;
                foreach (TypeDecl o in m.Types)
                    if (o != td && o.Open < td.Open && o.Close > td.Close && (best == null || o.Open > best.Open)) best = o;
                td.Outer = best;
            }
            foreach (TypeDecl td in m.Types)
            {
                var chain = new List<string>();
                for (TypeDecl c = td; c != null; c = c.Outer) chain.Insert(0, c.Name);
                td.FullName = (m.Namespace.Length > 0 ? m.Namespace + "." : "") + string.Join("+", chain.ToArray());
            }
        }

        static void CollectBases(Model m, TypeDecl td)
        {
            List<Tok> toks = m.Toks;
            int i = td.NameTok + 1;
            int depth = 0;
            int colon = -1;
            for (; i < td.Open; i++)
            {
                Tok t = toks[i];
                if (t.Kind != TK.Punct) { if (t.Kind == TK.Ident && t.T == "where" && depth == 0) break; continue; }
                if (t.T == "<") depth++;
                else if (t.T == ">") depth--;
                else if (t.T == ":" && depth == 0) { colon = i; break; }
            }
            if (colon < 0) return;
            depth = 0;
            string last = null;
            for (i = colon + 1; i < td.Open; i++)
            {
                Tok t = toks[i];
                if (t.Kind == TK.Ident)
                {
                    if (t.T == "where" && depth == 0) break;
                    if (depth == 0) last = t.T;
                }
                else if (t.Kind == TK.Punct)
                {
                    if (t.T == "<") depth++;
                    else if (t.T == ">") depth--;
                    else if (t.T == "," && depth == 0) { if (last != null) td.Bases.Add(last); last = null; }
                }
            }
            if (last != null) td.Bases.Add(last);
        }

        static int GenericClose(Model m, int i, int end)
        {
            int depth = 0;
            for (int k = i; k < end; k++)
            {
                Tok t = m.Toks[k];
                if (t.Kind == TK.Punct)
                {
                    switch (t.T)
                    {
                        case "<": depth++; break;
                        case ">":
                            depth--;
                            if (depth == 0) return k;
                            break;
                        case ",": case ".": case "?": case "::": case "[": case "]": break;
                        case "(": k = m.Match[k]; break;
                        default: return -1;
                    }
                }
                else if (t.Kind != TK.Ident) return -1;
            }
            return -1;
        }

        static bool IsGenericCloser(Model m, int idx)
        {
            int depth = 0;
            for (int k = idx; k >= 0 && k > idx - 80; k--)
            {
                Tok t = m.Toks[k];
                if (t.Kind == TK.Punct)
                {
                    switch (t.T)
                    {
                        case ">": depth++; break;
                        case "<":
                            depth--;
                            if (depth == 0) return k > 0 && m.Toks[k - 1].Kind == TK.Ident && GenericClose(m, k, idx + 1) == idx;
                            break;
                        case ",": case ".": case "?": case "::": case "[": case "]": break;
                        case ")": k = m.Match[k]; break;
                        default: return false;
                    }
                }
                else if (t.Kind != TK.Ident) return false;
            }
            return false;
        }

        static int SkipToSemicolon(Model m, int from, int end)
        {
            int j = from;
            while (j < end)
            {
                Tok x = m.Toks[j];
                if (x.Kind == TK.Punct)
                {
                    if (x.T == "(" || x.T == "[" || x.T == "{") { j = m.Match[j] + 1; continue; }
                    if (x.T == ";") return j;
                }
                j++;
            }
            return end - 1;
        }

        static int SkipInitializer(Model m, int from, int end)
        {
            int j = from;
            while (j < end)
            {
                Tok x = m.Toks[j];
                if (x.Kind == TK.Punct)
                {
                    if (x.T == "(" || x.T == "[" || x.T == "{") { j = m.Match[j] + 1; continue; }
                    if (x.T == "<" && j > 0 && m.Toks[j - 1].Kind == TK.Ident)
                    {
                        int g = GenericClose(m, j, end);
                        if (g > 0) { j = g + 1; continue; }
                    }
                    if (x.T == "," || x.T == ";") return j;
                }
                j++;
            }
            return end;
        }

        static void ParseMembers(Model m, TypeDecl td)
        {
            List<Tok> toks = m.Toks;
            int p = td.Open + 1;
            int end = td.Close;
            while (p < end)
            {
                var mem = new Member { Start = p };
                int i = p;
                while (i < end && toks[i].Kind == TK.Punct && toks[i].T == "[")
                {
                    int c = m.Match[i];
                    mem.Attrs.Add(new[] { i, c });
                    i = c + 1;
                }
                mem.HeaderStart = i;
                while (i < end && toks[i].Kind == TK.Ident && s_Mods.Contains(toks[i].T)) { mem.Mods.Add(toks[i].T); i++; }

                if (i + 1 < end && toks[i].Kind == TK.Ident && IsTypeKw(toks[i].T) && toks[i + 1].Kind == TK.Ident)
                {
                    int j = i + 2;
                    int open = -1;
                    while (j < end)
                    {
                        Tok x = toks[j];
                        if (x.Kind == TK.Punct && x.T == "{") { open = j; break; }
                        if (x.Kind == TK.Punct && (x.T == "(" || x.T == "[")) { j = m.Match[j] + 1; continue; }
                        j++;
                    }
                    if (open < 0) { m.Problems.Add("nested type without body in " + td.Name); return; }
                    mem.Kind = "Type";
                    mem.End = m.Match[open];
                    td.Members.Add(mem);
                    p = mem.End + 1;
                    continue;
                }

                int term = -1;
                while (i < end)
                {
                    Tok t = toks[i];
                    if (t.Kind == TK.Punct)
                    {
                        if (t.T == "[") { i = m.Match[i] + 1; continue; }
                        if (t.T == "<" && i > 0 && toks[i - 1].Kind == TK.Ident)
                        {
                            int g = GenericClose(m, i, end);
                            if (g > 0) { i = g + 1; continue; }
                        }
                        if (t.T == "(" || t.T == "{" || t.T == "=>" || t.T == "=" || t.T == ";" || t.T == ",") { term = i; break; }
                    }
                    else if (t.Kind == TK.Ident && t.T == "event") mem.HasEvent = true;
                    i++;
                }
                if (term < 0) { m.Problems.Add("member without terminator in " + td.Name + " near token " + p); return; }
                mem.Term = term;
                string tt = toks[term].T;
                if (tt == "(")
                {
                    mem.Kind = "Method";
                    int j = m.Match[term] + 1;
                    bool done = false;
                    while (j < end && !done)
                    {
                        Tok x = toks[j];
                        if (x.Kind == TK.Punct)
                        {
                            if (x.T == "{") { mem.End = m.Match[j]; done = true; break; }
                            if (x.T == "=>") { mem.End = SkipToSemicolon(m, j + 1, end); done = true; break; }
                            if (x.T == ";") { mem.End = j; done = true; break; }
                            if (x.T == "(" || x.T == "[") { j = m.Match[j] + 1; continue; }
                        }
                        j++;
                    }
                    if (!done) { m.Problems.Add("method without body end in " + td.Name); return; }
                }
                else if (tt == "{")
                {
                    mem.Kind = "Property";
                    int e = m.Match[term];
                    if (e + 1 < end && toks[e + 1].Kind == TK.Punct && toks[e + 1].T == "=") e = SkipToSemicolon(m, e + 2, end);
                    mem.End = e;
                }
                else if (tt == "=>")
                {
                    mem.Kind = "Property";
                    mem.End = SkipToSemicolon(m, term + 1, end);
                }
                else
                {
                    mem.Kind = mem.HasEvent ? "Event" : "Field";
                    int j = term;
                    if (term - 1 >= 0 && toks[term - 1].Kind == TK.Ident) mem.Decls.Add(term - 1);
                    else { m.Problems.Add("field without a name in " + td.Name + " near token " + p); return; }
                    bool ok = false;
                    while (j < end)
                    {
                        if (toks[j].Kind == TK.Punct && toks[j].T == "=") j = SkipInitializer(m, j + 1, end);
                        if (j >= end) break;
                        if (toks[j].Kind == TK.Punct && toks[j].T == ",")
                        {
                            if (j + 1 < end && toks[j + 1].Kind == TK.Ident) { mem.Decls.Add(j + 1); j += 2; continue; }
                            break;
                        }
                        if (toks[j].Kind == TK.Punct && toks[j].T == ";") { mem.End = j; ok = true; }
                        break;
                    }
                    if (!ok) { m.Problems.Add("field declaration not understood in " + td.Name + " near token " + p); return; }
                }
                td.Members.Add(mem);
                p = mem.End + 1;
            }
        }

        static void AssignOwners(Model m)
        {
            int n = m.Toks.Count;
            m.OwnerType = new TypeDecl[n];
            m.MemberAt = new Member[n];
            foreach (TypeDecl td in m.Types)
            {
                for (int k = td.Open + 1; k < td.Close; k++) m.OwnerType[k] = td;
                foreach (Member mem in td.Members)
                {
                    if (mem.Kind == "Type") continue;
                    for (int k = mem.Start; k <= mem.End && k < n; k++) m.MemberAt[k] = mem;
                }
            }
        }

        // ================================================================ scope analysis

        static bool TypeLike(Model m, int idx)
        {
            if (idx < 0) return false;
            Tok p = m.Toks[idx];
            if (p.Kind == TK.Ident) return !s_Keywords.Contains(p.T) || s_TypeKeywords.Contains(p.T);
            if (p.Kind != TK.Punct) return false;
            if (p.T == "]") return true;
            if (p.T == ">") return IsGenericCloser(m, idx);
            if (p.T == "?") return idx > 0 && m.Toks[idx - 1].Kind == TK.Ident && TypeLike(m, idx - 1);
            return false;
        }

        static HashSet<string> DeclaredNames(Model m, Member mem)
        {
            HashSet<string> set;
            if (m.DeclCache.TryGetValue(mem, out set)) return set;
            set = new HashSet<string>(StringComparer.Ordinal);
            m.DeclCache[mem] = set;
            List<Tok> toks = m.Toks;
            int a = mem.Start;
            int b = Math.Min(mem.End, toks.Count - 1);
            for (int k = a; k <= b; k++)
            {
                Tok t = toks[k];
                if (t.Kind != TK.Ident || s_Keywords.Contains(t.T)) continue;
                if (mem.Kind == "Field" && mem.Decls.Contains(k)) continue;
                Tok nextTok = k + 1 < toks.Count ? toks[k + 1] : null;
                string next = nextTok != null ? nextTok.T : "";
                string prev = k > 0 ? toks[k - 1].T : "";
                bool decl = false;

                if (nextTok != null && nextTok.Kind == TK.Punct && next == "=>") decl = true;
                else if ((prev == "(" || prev == ",") && m.Encl[k] >= 0 && toks[m.Encl[k]].T == "(")
                {
                    int close = m.Match[m.Encl[k]];
                    if (close + 1 < toks.Count && toks[close + 1].T == "=>") decl = true;
                }
                if (!decl && TypeLike(m, k - 1))
                {
                    bool follow = nextTok != null &&
                        ((nextTok.Kind == TK.Punct && (next == "=" || next == ";" || next == "," || next == ")" || next == "&&" || next == "||")) ||
                         (nextTok.Kind == TK.Ident && next == "in"));
                    if (follow) decl = true;
                }
                if (!decl && nextTok != null && nextTok.Kind == TK.Punct && next == ":" && k >= 2 &&
                    (toks[k - 2].T == "case" || (k >= 3 && toks[k - 3].T == "case")) && TypeLike(m, k - 1)) decl = true;
                if (decl) set.Add(t.T);
            }
            return set;
        }

        static bool IsInitializerBrace(Model m, int open)
        {
            List<Tok> toks = m.Toks;
            int k = open - 1;
            if (k < 0) return false;
            if (toks[k].Kind == TK.Punct && toks[k].T == ")") k = m.Match[k] - 1;
            int guard = 0;
            while (k >= 0 && guard++ < 64)
            {
                Tok t = toks[k];
                if (t.Kind == TK.Ident && t.T == "new") return true;
                if (t.Kind == TK.Ident) { k--; continue; }
                if (t.Kind == TK.Punct && (t.T == "." || t.T == "<" || t.T == ">" || t.T == "," || t.T == "?" || t.T == "::")) { k--; continue; }
                if (t.Kind == TK.Punct && (t.T == "]" || t.T == ")")) { k = m.Match[k] - 1; continue; }
                return false;
            }
            return false;
        }

        static bool InAttribute(Member mem, int k)
        {
            if (mem == null) return false;
            foreach (int[] g in mem.Attrs) if (k > g[0] && k < g[1]) return true;
            return false;
        }

        // ================================================================ naming

        public static string NewName(string old, string shape)
        {
            string prefix = shape == "const" ? "c_" : shape == "static" ? "s_" : "m_";
            string n = old.TrimStart('_');
            if (n.Length == 0) return null;
            if (shape == "instance" && n.Length >= 2 && n[0] == 'm' && char.IsUpper(n[1])) n = n.Substring(1);
            if (shape == "static" && n.Length >= 2 && n[0] == 's' && char.IsUpper(n[1])) n = n.Substring(1);
            if (n.IndexOf('_') >= 0)
            {
                bool allCaps = n == n.ToUpperInvariant();
                var sb = new StringBuilder();
                foreach (string part in n.Split('_'))
                {
                    if (part.Length == 0) continue;
                    string rest = allCaps ? part.Substring(1).ToLowerInvariant() : part.Substring(1);
                    sb.Append(char.ToUpperInvariant(part[0])).Append(rest);
                }
                n = sb.ToString();
            }
            else if (shape == "const" && n == n.ToUpperInvariant() && n.Length > 1 && n.Any(char.IsLetter))
            {
                n = char.ToUpperInvariant(n[0]) + n.Substring(1).ToLowerInvariant();
            }
            if (n.Length == 0) return null;
            return prefix + char.ToUpperInvariant(n[0]) + n.Substring(1);
        }

        static HashSet<string> AllTypeNames()
        {
            if (s_AllTypeNames != null) return s_AllTypeNames;
            s_AllTypeNames = new HashSet<string>(StringComparer.Ordinal);
            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try { types = asm.GetTypes(); }
                catch (ReflectionTypeLoadException e) { types = e.Types.Where(x => x != null).ToArray(); }
                catch (Exception) { continue; }
                foreach (Type t in types) s_AllTypeNames.Add(t.Name);
            }
            return s_AllTypeNames;
        }

        static HashSet<string> LoadForce()
        {
            var set = new HashSet<string>(StringComparer.Ordinal);
            string f = RushCommon.FileIn("rename_force.txt");
            if (!File.Exists(f)) return set;
            foreach (string line in File.ReadAllLines(f))
            {
                string l = line.Trim();
                if (l.Length > 0 && !l.StartsWith("#", StringComparison.Ordinal)) set.Add(l);
            }
            return set;
        }

        // ================================================================ planning

        static PlanResult BuildPlan(List<Model> models, string filePrefix)
        {
            var plan = new PlanResult();
            plan.Models.AddRange(models);
            foreach (Model m in models) plan.Edits[m] = new List<Edit>();
            HashSet<string> force = LoadForce();

            var typesByName = new Dictionary<string, List<TypeDecl>>(StringComparer.Ordinal);
            foreach (Model m in models)
                foreach (TypeDecl td in m.Types)
                {
                    List<TypeDecl> l;
                    if (!typesByName.TryGetValue(td.Name, out l)) { l = new List<TypeDecl>(); typesByName[td.Name] = l; }
                    l.Add(td);
                }

            // string literals that equal a field name (FindProperty, reflection, NaughtyAttributes ...) and JsonUtility targets
            var literals = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            var jsonTypes = new HashSet<string>(StringComparer.Ordinal);
            foreach (Model m in models)
            {
                List<Tok> toks = m.Toks;
                for (int i = 0; i < toks.Count; i++)
                {
                    Tok t = toks[i];
                    if (t.Kind == TK.Str && t.E - t.S >= 2 && m.Text[t.S] == '"' && !(i >= 2 && toks[i - 1].T == "(" && toks[i - 2].T == "FormerlySerializedAs"))
                    {
                        string v = m.Text.Substring(t.S + 1, t.E - t.S - 2);
                        List<string> l;
                        if (!literals.TryGetValue(v, out l)) { l = new List<string>(); literals[v] = l; }
                        l.Add(m.Rel + ":" + LineOf(m, t.S));
                    }
                    if (t.Kind == TK.Ident && t.T == "JsonUtility" && i + 5 < toks.Count && toks[i + 1].T == "." && toks[i + 2].T == "FromJson" &&
                        toks[i + 3].T == "<" && toks[i + 4].Kind == TK.Ident)
                        jsonTypes.Add(toks[i + 4].T);
                }
            }

            // 1. collect targets
            foreach (Model m in models)
            {
                if (filePrefix.Length > 0 && !m.Rel.StartsWith(filePrefix, StringComparison.Ordinal)) continue;
                foreach (TypeDecl td in m.Types)
                {
                    if (td.Kind != "class" && td.Kind != "struct") continue;
                    foreach (Member mem in td.Members)
                    {
                        if (mem.Kind != "Field") continue;
                        string vis = mem.Mods.Contains("public") ? "public" : mem.Mods.Contains("protected") ? "protected" :
                                     mem.Mods.Contains("internal") ? "internal" : "private";
                        if (vis == "public" || vis == "internal") continue;
                        string shape = mem.Mods.Contains("const") ? "const" : mem.Mods.Contains("static") ? "static" : "instance";
                        foreach (int declTok in mem.Decls)
                        {
                            string old = m.Toks[declTok].T;
                            string prefix = shape == "const" ? "c_" : shape == "static" ? "s_" : "m_";
                            if (old.StartsWith(prefix, StringComparison.Ordinal)) continue;
                            var tg = new Target { File = m, Type = td, Member = mem, DeclTok = declTok, Old = old, Vis = vis, Shape = shape };
                            tg.New = NewName(old, shape);
                            tg.Serialized = shape == "instance" && mem.Attrs.Any(g => TokensIn(m, g[0], g[1]).Any(x => x.Kind == TK.Ident && (x.T == "SerializeField" || x.T == "SerializeReference")));
                            plan.Targets.Add(tg);
                        }
                    }
                }
            }

            // 2. validate each target
            foreach (Target tg in plan.Targets)
            {
                if (tg.File.Problems.Count > 0) { tg.Manual = "file could not be parsed safely: " + tg.File.Problems[0]; continue; }
                if (tg.New == null) { tg.Manual = "cannot derive a new name"; continue; }
                if (tg.Member.Decls.Count > 1 && tg.Serialized) { tg.Manual = "serialized field declared together with others (a, b)"; continue; }
                if (AllTypeNames().Contains(tg.Old) && !force.Contains(tg.Type.Name + "." + tg.Old)) { tg.Manual = "name equals a type name (Color Color pattern)"; continue; }
                string clash = NameClash(tg, plan.Targets);
                if (clash != null) { tg.Manual = clash; continue; }

                string key = tg.Type.Name + "." + tg.Old;
                List<string> refs;
                if (literals.TryGetValue(tg.Old, out refs))
                {
                    foreach (string r in refs.Take(3)) tg.Notes.Add("string literal \"" + tg.Old + "\" at " + r);
                    if (!force.Contains(key)) { tg.Manual = "a string literal equals the field name (FindProperty / reflection / attribute argument?): " + refs[0]; continue; }
                }
                if (tg.Serialized && !force.Contains(key))
                {
                    for (TypeDecl o = tg.Type; o != null; o = o.Outer)
                        if (jsonTypes.Contains(o.Name)) { tg.Manual = "class " + o.Name + " is used with JsonUtility.FromJson (keys map to field names)"; break; }
                    if (tg.Manual != null) continue;
                }
            }

            // 3. build edits
            foreach (Target tg in plan.Targets)
            {
                if (tg.Manual != null) continue;
                var scope = new List<TypeDecl> { tg.Type };
                if (tg.Vis == "protected")
                {
                    foreach (TypeDecl d in DerivedOf(tg.Type, typesByName))
                    {
                        if (d.File.Problems.Count > 0) { tg.Manual = "derived class in an unparsable file: " + d.File.Rel; break; }
                        if (DeclaresName(d, tg.Old)) { tg.Manual = "derived class " + d.Name + " declares a member with the same name"; break; }
                        if (!scope.Contains(d)) scope.Add(d);
                    }
                    if (tg.Manual != null) continue;
                }
                if (tg.Shape != "instance")
                {
                    // nested types may use an outer static / const by its bare name
                    foreach (TypeDecl nested in tg.File.Types.ToList())
                        if (nested != tg.Type && IsInside(nested, tg.Type) && !DeclaresName(nested, tg.Old) && !scope.Contains(nested)) scope.Add(nested);
                }
                foreach (TypeDecl s in scope) Collect(plan, tg, s);

                if (tg.Serialized)
                {
                    Tok first = tg.File.Toks[tg.Member.HeaderStart];
                    plan.Edits[tg.File].Add(new Edit { Start = first.S, End = first.S, Text = "[FormerlySerializedAs(\"" + tg.Old + "\")] " });
                }
            }

            // 4. using directives, de-duplication, overlap check
            foreach (Model m in models)
            {
                List<Edit> edits = plan.Edits[m];
                if (edits.Count == 0) continue;
                bool needsUsing = plan.Targets.Any(t => t.Manual == null && t.Serialized && t.File == m);
                if (needsUsing && m.Text.IndexOf("using UnityEngine.Serialization;", StringComparison.Ordinal) < 0)
                    edits.Add(UsingEdit(m));
                edits.Sort((a, b) => a.Start != b.Start ? a.Start.CompareTo(b.Start) : a.End.CompareTo(b.End));
                for (int i = edits.Count - 1; i >= 1; i--)
                    if (edits[i].Start == edits[i - 1].Start && edits[i].End == edits[i - 1].End && edits[i].Text == edits[i - 1].Text)
                    {
                        plan.Warnings.Add("duplicate edit removed in " + m.Rel + " at offset " + edits[i].Start);
                        edits.RemoveAt(i);
                    }
                for (int i = 1; i < edits.Count; i++)
                    if (edits[i].Start < edits[i - 1].End)
                        plan.Warnings.Add("overlapping edits in " + m.Rel + " at offset " + edits[i].Start);
            }
            return plan;
        }

        static bool IsInside(TypeDecl inner, TypeDecl outer)
        {
            for (TypeDecl o = inner.Outer; o != null; o = o.Outer) if (o == outer) return true;
            return false;
        }

        static IEnumerable<Tok> TokensIn(Model m, int a, int b)
        {
            for (int i = a; i <= b && i < m.Toks.Count; i++) yield return m.Toks[i];
        }

        static bool DeclaresName(TypeDecl td, string name)
        {
            Model m = td.File;
            foreach (Member mem in td.Members)
            {
                foreach (int d in mem.Decls) if (m.Toks[d].T == name) return true;
                if ((mem.Kind == "Method" || mem.Kind == "Property") && mem.Term > 0 && m.Toks[mem.Term - 1].T == name) return true;
            }
            return false;
        }

        static string NameClash(Target tg, List<Target> all)
        {
            string nn = tg.New;
            if (DeclaresName(tg.Type, nn)) return "new name '" + nn + "' already exists in " + tg.Type.Name;
            foreach (Target o in all)
                if (o != tg && o.Type == tg.Type && o.New == nn) return "two fields would both become '" + nn + "'";
            Type t = RushCommon.FindType(tg.Type.FullName);
            if (t != null)
            {
                const BindingFlags f = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
                if (t.GetMember(nn, f).Length > 0) return "new name '" + nn + "' already exists on " + t.Name + " or a base type";
            }
            return null;
        }

        static IEnumerable<TypeDecl> DerivedOf(TypeDecl root, Dictionary<string, List<TypeDecl>> byName)
        {
            var seen = new HashSet<TypeDecl>();
            var queue = new Queue<string>();
            queue.Enqueue(root.Name);
            var names = new HashSet<string> { root.Name };
            var result = new List<TypeDecl>();
            while (queue.Count > 0)
            {
                string baseName = queue.Dequeue();
                foreach (List<TypeDecl> l in byName.Values)
                    foreach (TypeDecl d in l)
                        if (d != root && !seen.Contains(d) && d.Bases.Contains(baseName))
                        {
                            seen.Add(d);
                            result.Add(d);
                            if (names.Add(d.Name)) queue.Enqueue(d.Name);
                        }
            }
            return result;
        }

        static void Collect(PlanResult plan, Target tg, TypeDecl scope)
        {
            Model m = scope.File;
            List<Tok> toks = m.Toks;
            for (int k = scope.Open + 1; k < scope.Close; k++)
            {
                Tok t = toks[k];
                if (t.Kind != TK.Ident || t.T != tg.Old) continue;
                if (m.OwnerType[k] != scope) continue; // tokens of nested types belong to them

                bool isDecl = (m == tg.File && k == tg.DeclTok);
                bool forced = false;
                if (!isDecl)
                {
                    Tok prev = k > 0 ? toks[k - 1] : null;
                    Tok next = k + 1 < toks.Count ? toks[k + 1] : null;
                    if (prev != null && prev.Kind == TK.Punct && (prev.T == "?." || prev.T == "::" || prev.T == "->")) continue;
                    if (prev != null && prev.Kind == TK.Punct && prev.T == ".")
                    {
                        Tok q = k >= 2 ? toks[k - 2] : null;
                        if (q != null && q.Kind == TK.Ident && (q.T == "this" || q.T == "base")) forced = true;
                        else if (q != null && q.Kind == TK.Ident && tg.Shape != "instance" && q.T == tg.Type.Name) forced = true;
                        else continue;
                    }
                    if (!forced)
                    {
                        // named argument  f(name: 1)
                        if (prev != null && prev.Kind == TK.Punct && (prev.T == "(" || prev.T == ",") && next != null && next.Kind == TK.Punct &&
                            next.T == ":" && m.Encl[k] >= 0 && toks[m.Encl[k]].T == "(") continue;
                        // object initializer member  new X { name = ... }
                        if (prev != null && prev.Kind == TK.Punct && (prev.T == "{" || prev.T == ",") && next != null && next.Kind == TK.Punct &&
                            next.T == "=" && m.Encl[k] >= 0 && toks[m.Encl[k]].T == "{" && IsInitializerBrace(m, m.Encl[k])) continue;
                        // attribute named argument  [Foo(name = 1)]
                        if (next != null && next.Kind == TK.Punct && next.T == "=" && InAttribute(m.MemberAt[k], k) &&
                            prev != null && (prev.T == "(" || prev.T == ",")) continue;
                        Member mem = m.MemberAt[k];
                        if (mem != null && DeclaredNames(m, mem).Contains(tg.Old))
                        {
                            tg.ShadowSkipped++;
                            if (tg.Notes.Count < 6) tg.Notes.Add("shadowed in " + m.Rel + ":" + LineOf(m, t.S));
                            continue;
                        }
                    }
                }
                plan.Edits[m].Add(new Edit { Start = t.S, End = t.E, Text = (t.S < t.E && m.Text[t.S] == '@') ? "@" + tg.New : tg.New });
                tg.Renamed++;
                if (forced) tg.ThisForced++;
                if (t.Cond) tg.InCond++;
                tg.TouchedFiles.Add(m.Rel);
            }
        }

        static int LineOf(Model m, int offset)
        {
            if (m.LineStarts == null)
            {
                var ls = new List<int> { 0 };
                for (int i = 0; i < m.Text.Length; i++) if (m.Text[i] == '\n') ls.Add(i + 1);
                m.LineStarts = ls.ToArray();
            }
            int idx = Array.BinarySearch(m.LineStarts, offset);
            if (idx < 0) idx = ~idx - 1;
            return idx + 1;
        }

        static Edit UsingEdit(Model m)
        {
            string eol = m.Text.Contains("\r\n") ? "\r\n" : "\n";
            int lastUsingEnd = -1;
            int pos = 0;
            while (pos < m.Text.Length)
            {
                int nl = m.Text.IndexOf('\n', pos);
                int lineEnd = nl < 0 ? m.Text.Length : nl + 1;
                string line = m.Text.Substring(pos, lineEnd - pos).Trim();
                if (line.StartsWith("using ", StringComparison.Ordinal) && line.EndsWith(";", StringComparison.Ordinal) && !line.Contains("("))
                    lastUsingEnd = lineEnd;
                else if (line.StartsWith("namespace ", StringComparison.Ordinal) || line.StartsWith("public ", StringComparison.Ordinal) ||
                         line.StartsWith("class ", StringComparison.Ordinal) || line.StartsWith("[", StringComparison.Ordinal))
                    break;
                if (nl < 0) break;
                pos = lineEnd;
            }
            if (lastUsingEnd < 0) return new Edit { Start = 0, End = 0, Text = "using UnityEngine.Serialization;" + eol };
            string before = m.Text.Substring(0, lastUsingEnd);
            string add = (before.EndsWith("\n", StringComparison.Ordinal) ? "" : eol) + "using UnityEngine.Serialization;" + eol;
            return new Edit { Start = lastUsingEnd, End = lastUsingEnd, Text = add };
        }

        static string ApplyEdits(Model m, List<Edit> edits)
        {
            var sb = new StringBuilder(m.Text);
            foreach (Edit e in edits.OrderByDescending(x => x.Start).ThenByDescending(x => x.End))
            {
                sb.Remove(e.Start, e.End - e.Start);
                sb.Insert(e.Start, e.Text);
            }
            return sb.ToString();
        }

        // ================================================================ reporting

        static string Summary(PlanResult plan)
        {
            int auto = plan.Targets.Count(t => t.Manual == null);
            int manual = plan.Targets.Count - auto;
            int ser = plan.Targets.Count(t => t.Manual == null && t.Serialized);
            int prot = plan.Targets.Count(t => t.Manual == null && t.Vis == "protected");
            int files = plan.Edits.Count(kv => kv.Value.Count > 0);
            int unsafeFiles = plan.Models.Count(x => x.Problems.Count > 0);
            int edits = plan.Edits.Sum(kv => kv.Value.Count);
            int shadow = plan.Targets.Sum(t => t.ShadowSkipped);
            int cond = plan.Targets.Sum(t => t.InCond);
            return "RENAME PLAN: targets=" + plan.Targets.Count + " (auto=" + auto + ", manual=" + manual + "), serialized=" + ser +
                   ", protected=" + prot + ", files to edit=" + files + ", edits=" + edits + ", shadow-skipped=" + shadow +
                   ", inside #if=" + cond + ", unparsable files=" + unsafeFiles + ", warnings=" + plan.Warnings.Count;
        }

        static string Report(PlanResult plan, bool detailed)
        {
            var sb = new StringBuilder();
            sb.AppendLine(Summary(plan));
            foreach (string w in plan.Warnings) sb.AppendLine("WARNING " + w);
            foreach (Model m in plan.Models.Where(x => x.Problems.Count > 0)) sb.AppendLine("UNPARSABLE " + m.Rel + " : " + string.Join("; ", m.Problems.ToArray()));
            sb.AppendLine();
            sb.AppendLine("=== MANUAL ===");
            foreach (Target t in plan.Targets.Where(x => x.Manual != null))
                sb.AppendLine(t.File.Rel + " | " + t.Type.Name + "." + t.Old + " | " + (t.Serialized ? "serialized | " : "") + t.Manual);
            sb.AppendLine();
            sb.AppendLine("=== AUTOMATIC ===");
            foreach (IGrouping<string, Target> g in plan.Targets.Where(x => x.Manual == null).GroupBy(x => x.File.Rel).OrderBy(x => x.Key, StringComparer.Ordinal))
            {
                sb.AppendLine(g.Key);
                foreach (Target t in g)
                {
                    sb.AppendLine("  " + t.Type.Name + "." + t.Old + " -> " + t.New + "  [" + t.Vis + " " + t.Shape + (t.Serialized ? " serialized" : "") +
                                  "] renamed=" + t.Renamed + " this/base-forced=" + t.ThisForced + " shadow-skipped=" + t.ShadowSkipped +
                                  (t.InCond > 0 ? " inIf=" + t.InCond : "") + (t.TouchedFiles.Count > 1 ? " files=" + t.TouchedFiles.Count : ""));
                    if (detailed) foreach (string n in t.Notes) sb.AppendLine("      " + n);
                }
            }
            return sb.ToString();
        }

        // ================================================================ self test

        static string RunOnText(string src, string extraSrc, out string extraOut)
        {
            Model a = Build("Assets/_dev/Test/A.cs", "A.cs", src);
            Model b = extraSrc != null ? Build("Assets/_dev/Test/B.cs", "B.cs", extraSrc) : null;
            var models = new List<Model> { a };
            if (b != null) models.Add(b);
            PlanResult plan = BuildPlan(models, "");
            extraOut = b != null ? ApplyEdits(b, plan.Edits[b]) : null;
            return ApplyEdits(a, plan.Edits[a]);
        }

        public static string SelfTest()
        {
            var sb = new StringBuilder();
            bool ok = true;
            ok &= Expect(sb, "NewName camel -> m_", NewName("popResult", "instance") == "m_PopResult");
            ok &= Expect(sb, "NewName leading underscore", NewName("_count", "instance") == "m_Count");
            ok &= Expect(sb, "NewName Hungarian m", NewName("mTarget", "instance") == "m_Target");
            ok &= Expect(sb, "NewName const caps", NewName("API_URL", "const") == "c_ApiUrl");
            ok &= Expect(sb, "NewName static", NewName("cachedAnimator", "static") == "s_CachedAnimator");
            ok &= Expect(sb, "NewName keeps acronym tail", NewName("APIInfoURL", "instance") == "m_APIInfoURL");

            string srcA =
                "using System;\r\nusing UnityEngine;\r\nnamespace T\r\n{\r\n" +
                "  public class Base : MonoBehaviour\r\n  {\r\n" +
                "    [SerializeField] protected int speed;\r\n" +
                "    private float timer;\r\n" +
                "    private static int counter;\r\n" +
                "    private const string NAME = \"x\";\r\n" +
                "    private string label = $\"{timer:F1} / {speed}\";\r\n" +
                "    void Update() { timer += Time.deltaTime; speed++; Debug.Log($\"t={timer}\"); }\r\n" +
                "    public void Set(int speed) { this.speed = speed; }\r\n" +
                "    void Local() { float timer = 3; timer++; this.timer = timer; }\r\n" +
                "    void Init() { var o = new Other { timer = timer }; Foo(timer: 3); }\r\n" +
                "    void Foo(float timer) { }\r\n" +
                "    public static int Next() { return counter++; }\r\n" +
                "    string Name() { return NAME + nameof(timer) + \"timerX\"; }\r\n" +
                "    void Lam() { Action<int> a = timer => { }; var l = new System.Collections.Generic.List<int>(); if (l.Count > counter) { } }\r\n" +
                "    class Nested { int Use() { return counter + 1; } }\r\n" +
                "  }\r\n" +
                "  class Other { public float timer; }\r\n}\r\n";
            string srcB =
                "namespace T\r\n{\r\n  public class Derived : Base\r\n  {\r\n" +
                "    void Go() { speed = 5; base.speed = 6; this.speed += 1; }\r\n  }\r\n}\r\n";
            string outB;
            string outA = RunOnText(srcA, srcB, out outB);

            ok &= Expect(sb, "protected serialized renamed + FSA", outA.Contains("[SerializeField] [FormerlySerializedAs(\"speed\")] protected int m_Speed;"));
            ok &= Expect(sb, "using inserted once", outA.Split(new[] { "using UnityEngine.Serialization;" }, StringSplitOptions.None).Length == 2);
            ok &= Expect(sb, "interpolation holes renamed", outA.Contains("$\"{m_Timer:F1} / {m_Speed}\"") && outA.Contains("$\"t={m_Timer}\""));
            ok &= Expect(sb, "plain use renamed", outA.Contains("m_Timer += Time.deltaTime; m_Speed++;"));
            ok &= Expect(sb, "parameter shadow keeps bare, this. renamed", outA.Contains("Set(int speed) { this.m_Speed = speed; }"));
            ok &= Expect(sb, "local shadow keeps bare, this. renamed", outA.Contains("float timer = 3; timer++; this.m_Timer = timer;"));
            ok &= Expect(sb, "object initializer member kept, value renamed", outA.Contains("new Other { timer = m_Timer }"));
            ok &= Expect(sb, "named argument kept", outA.Contains("Foo(timer: 3)"));
            ok &= Expect(sb, "parameter declaration kept", outA.Contains("void Foo(float timer) { }"));
            ok &= Expect(sb, "other class field untouched", outA.Contains("class Other { public float timer; }"));
            ok &= Expect(sb, "static renamed", outA.Contains("private static int s_Counter;") && outA.Contains("return s_Counter++;"));
            ok &= Expect(sb, "static used from nested type", outA.Contains("return s_Counter + 1;"));
            ok &= Expect(sb, "const renamed", outA.Contains("private const string c_Name = \"x\";") && outA.Contains("return c_Name +"));
            ok &= Expect(sb, "nameof renamed, plain string kept", outA.Contains("nameof(m_Timer)") && outA.Contains("\"timerX\""));
            ok &= Expect(sb, "lambda parameter shadow kept", outA.Contains("Action<int> a = timer => { };"));
            ok &= Expect(sb, "comparison with static renamed", outA.Contains("l.Count > s_Counter"));
            ok &= Expect(sb, "CRLF preserved", !outA.Replace("\r\n", "").Contains("\n"));
            ok &= Expect(sb, "derived class (other file) renamed", outB.Contains("m_Speed = 5; base.m_Speed = 6; this.m_Speed += 1;"));

            // a second pass over already converted code changes nothing
            string again;
            string outA2 = RunOnText(outA, outB, out again);
            ok &= Expect(sb, "idempotent", outA2 == outA && again == outB);

            string srcL = "namespace T { class L { private int zeta; void F() { Find(\"zeta\"); zeta = 1; } } }\r\n"; string unusedB; ok &= Expect(sb, "string literal equal to field name is left alone", RunOnText(srcL, null, out unusedB) == srcL);
            // unbalanced #if branch must be refused
            string bad = "namespace T { class C { private int a;\r\n#if X\r\n void M() {\r\n#else\r\n void M() {\r\n#endif\r\n a = 1; } } }\r\n";
            Model bm = Build("Assets/_dev/Test/C.cs", "C.cs", bad);
            ok &= Expect(sb, "unbalanced #if branch flagged", bm.Problems.Count > 0);
            sb.Insert(0, ok ? "RENAME SELFTEST PASS\n" : "RENAME SELFTEST FAIL\n");
            if (!ok) { sb.AppendLine("--- A:"); sb.AppendLine(outA); sb.AppendLine("--- B:"); sb.AppendLine(outB); }
            return sb.ToString();
        }

        static bool Expect(StringBuilder sb, string what, bool cond)
        {
            sb.AppendLine((cond ? "  ok   " : "  FAIL ") + what);
            return cond;
        }
    }
}
