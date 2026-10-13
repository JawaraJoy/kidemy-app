using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace RushRefactor
{
    /// <summary>
    /// Stage 3, rule #8: turns the plain statement forms
    ///     lhs = [recv.]GetComponent&lt;T&gt;();            ->   [recv.]TryGetComponent&lt;T&gt;(out lhs);
    ///     Type x = [recv.]GetComponent&lt;T&gt;();         ->   [recv.]TryGetComponent&lt;T&gt;(out Type x);
    /// Every other shape (inline in a condition, chained, cast, null-conditional) is listed as MANUAL and left alone.
    /// The out argument is null when the component is missing, exactly like GetComponent returns null.
    /// </summary>
    public static partial class RushRename
    {
        sealed class TgSite
        {
            public Model M;
            public int Start;
            public int End;
            public string Old;
            public string New;
            public string Type;
            public string Method;
            public string BaseMethod;
            public int Line;
            public string Note;
        }

        static readonly HashSet<string> s_TgKeywords = new HashSet<string>(StringComparer.Ordinal)
        {
            "return", "else", "new", "throw", "case", "do", "yield", "await", "in", "is", "as", "out", "ref", "goto", "if", "while", "for", "foreach", "switch", "using", "lock"
        };

        static bool TgBoundary(List<Tok> t, int idx)
        {
            if (idx < 0) return false;
            string s = t[idx].T;
            return s == ";" || s == "{" || s == "}" || s == ")" || s == "else" || s == ":";
        }

        // start of the member-access / call / index chain that ends at token 'last' (Ident, ')' or ']'), or -1
        static int TgChainStart(Model m, int last)
        {
            List<Tok> t = m.Toks;
            int j = last;
            int guard = 0;
            while (guard++ < 200)
            {
                if (j < 0) return -1;
                Tok x = t[j];
                if (x.T == ")" || x.T == "]")
                {
                    int open = m.Match[j];
                    if (open <= 0) return -1;
                    j = open - 1;
                    if (t[j].T == ">") return -1;
                    if (t[j].Kind != TK.Ident && t[j].T != ")" && t[j].T != "]") return -1;
                    if (t[j].Kind == TK.Ident && s_TgKeywords.Contains(t[j].T)) return -1;
                    continue;
                }
                if (x.Kind != TK.Ident) return -1;
                if (j >= 1 && t[j - 1].T == ".")
                {
                    if (j < 2) return -1;
                    j -= 2;
                    continue;
                }
                return j;
            }
            return -1;
        }

        // start of a type that ends at token 'last' (Name, Ns.Name, Name<..>, Name[]), or -1
        static int TgTypeStart(Model m, int last)
        {
            List<Tok> t = m.Toks;
            int j = last;
            if (j < 0) return -1;
            if (t[j].T == "]")
            {
                int open = m.Match[j];
                if (open <= 0) return -1;
                j = open - 1;
                if (j < 0) return -1;
            }
            if (t[j].T == ">")
            {
                int depth = 0;
                int k = j;
                while (k >= 0)
                {
                    if (t[k].T == ">") depth++;
                    else if (t[k].T == "<") { depth--; if (depth == 0) break; }
                    else if (t[k].T == ";" || t[k].T == "{" || t[k].T == "}") return -1;
                    k--;
                }
                if (k <= 0) return -1;
                j = k - 1;
            }
            if (t[j].Kind != TK.Ident || s_TgKeywords.Contains(t[j].T)) return -1;
            while (j >= 2 && t[j - 1].T == "." && t[j - 2].Kind == TK.Ident) j -= 2;
            return j;
        }

        public static string TryGetRun(string batch, bool dryRun)
        {
            List<Model> models = LoadProjectModels();
            var mapRules = RushIL.LoadMap();
            var sites = new List<TgSite>();
            var manual = new List<string>();
            int total = 0;

            foreach (Model m in models)
            {
                if (m.Problems.Count > 0) continue;
                List<Tok> t = m.Toks;
                for (int k = 1; k + 1 < t.Count; k++)
                {
                    if (t[k].Kind != TK.Ident || t[k].T != "GetComponent" || t[k + 1].T != "<") continue;
                    TypeDecl td = m.OwnerType[k];
                    if (td == null) continue;
                    total++;
                    int line = LineOf(m, t[k].S);
                    string where = m.Rel + ":" + line;
                    string lineText = RushCommon.Clip(S3Line(m, t[k].S), 140);

                    int genEnd = -1;
                    int depth = 0;
                    for (int j = k + 1; j < t.Count && j < k + 40; j++)
                    {
                        if (t[j].T == "<") depth++;
                        else if (t[j].T == ">") { depth--; if (depth == 0) { genEnd = j; break; } }
                        else if (t[j].T == ";") break;
                    }
                    if (genEnd < 0 || genEnd + 3 >= t.Count || t[genEnd + 1].T != "(" || t[genEnd + 2].T != ")" || t[genEnd + 3].T != ";")
                    {
                        manual.Add(where + "\tnot a plain call statement\t" + lineText);
                        continue;
                    }

                    int recvStart = k;
                    if (t[k - 1].T == ".")
                    {
                        recvStart = TgChainStart(m, k - 2);
                        if (recvStart < 0) { manual.Add(where + "\treceiver is not a simple member chain\t" + lineText); continue; }
                    }
                    else if (t[k - 1].T == "?.")
                    {
                        manual.Add(where + "\tnull-conditional receiver\t" + lineText);
                        continue;
                    }
                    if (recvStart < 2 || t[recvStart - 1].T != "=")
                    {
                        manual.Add(where + "\tused inline (not assigned in its own statement)\t" + lineText);
                        continue;
                    }

                    int lhsLast = recvStart - 2;
                    int lhsStart = TgChainStart(m, lhsLast);
                    if (lhsStart < 0) { manual.Add(where + "\tleft side is not a simple variable/field\t" + lineText); continue; }

                    bool isDecl = false;
                    int stmtStart = lhsStart;
                    string typeText = null;
                    if (lhsStart == lhsLast && lhsStart >= 1)
                    {
                        int ty = TgTypeStart(m, lhsStart - 1);
                        if (ty >= 0 && TgBoundary(t, ty - 1))
                        {
                            isDecl = true;
                            stmtStart = ty;
                            typeText = m.Text.Substring(t[ty].S, t[lhsStart - 1].E - t[ty].S);
                        }
                    }
                    if (!isDecl && !TgBoundary(t, lhsStart - 1))
                    {
                        manual.Add(where + "\tunrecognised statement start\t" + lineText);
                        continue;
                    }

                    int semi = genEnd + 3;
                    string oldText = m.Text.Substring(t[stmtStart].S, t[semi].E - t[stmtStart].S);
                    if (oldText.Contains("//") || oldText.Contains("/*") || oldText.Contains("#"))
                    {
                        manual.Add(where + "\tcomment or directive inside the statement\t" + lineText);
                        continue;
                    }

                    string argT = m.Text.Substring(t[k + 2].S, t[genEnd - 1].E - t[k + 2].S);
                    if (isDecl && typeText != "var" && typeText != argT)
                    {
                        manual.Add(where + "\tdeclared type '" + typeText + "' differs from <" + argT + ">\t" + lineText);
                        continue;
                    }
                    string recvText = recvStart < k ? m.Text.Substring(t[recvStart].S, t[k - 1].E - t[recvStart].S) : "";
                    string lhsText = m.Text.Substring(t[lhsStart].S, t[lhsLast].E - t[lhsStart].S);
                    string newText = isDecl
                        ? recvText + "TryGetComponent<" + argT + ">(out " + (typeText == "var" ? argT : typeText) + " " + lhsText + ");"
                        : recvText + "TryGetComponent<" + argT + ">(out " + lhsText + ");";

                    Member mem = m.MemberAt[k];
                    if (mem == null || mem.Kind != "Method" || mem.Term <= 0)
                    {
                        manual.Add(where + "\tnot inside a method body\t" + lineText);
                        continue;
                    }
                    string mname = t[mem.Term - 1].T;
                    string bname = mname;
                    if (mname.EndsWith("Internal", StringComparison.Ordinal))
                    {
                        string stem = mname.Substring(0, mname.Length - 8);
                        if (mapRules.Any(r => (r.Kind == "Split" || r.Kind == "Move") && r.Type == td.FullName && r.A == stem)) bname = stem;
                    }
                    string note = "";
                    for (int j = mem.Term; j < k; j++) if (t[j].T == "=>" || t[j].T == "delegate") { note = "after a lambda/delegate token in the same method (check generated method names)"; break; }
                    sites.Add(new TgSite { M = m, Start = t[stmtStart].S, End = t[semi].E, Old = oldText, New = newText, Type = td.FullName, Method = mname, BaseMethod = bname, Line = line, Note = note });
                }
            }

            var rep = new StringBuilder();
            rep.AppendLine("# TryGetComponent conversion " + (dryRun ? "DRY RUN" : "APPLIED") + " batch=" + batch);
            rep.AppendLine("# GetComponent<T>() calls seen: " + total + " | convertible: " + sites.Count + " | manual: " + manual.Count);
            rep.AppendLine();
            foreach (TgSite s in sites.OrderBy(x => x.M.Rel, StringComparer.Ordinal).ThenBy(x => x.Line))
            {
                rep.AppendLine("SITE " + s.M.Rel + ":" + s.Line + "  " + s.Type + "." + s.Method + (s.Note.Length > 0 ? "   NOTE: " + s.Note : ""));
                rep.AppendLine("  - " + s.Old);
                rep.AppendLine("  + " + s.New);
            }
            rep.AppendLine();
            rep.AppendLine("# MANUAL (left unchanged)");
            foreach (string s in manual) rep.AppendLine(s);

            var rules = sites.Select(s => "Reviewed|" + s.Type + "|" + s.BaseMethod + "|").Distinct().OrderBy(x => x, StringComparer.Ordinal).ToList();
            rep.AppendLine();
            rep.AppendLine("# IL map rules (" + rules.Count + ")");
            foreach (string r in rules) rep.AppendLine(r);
            File.WriteAllText(RushCommon.FileIn(dryRun ? "stage3_" + batch + "_dryrun.txt" : "stage3_" + batch + "_applied.txt"), rep.ToString(), new UTF8Encoding(false));

            if (!dryRun)
            {
                string backupDir = Path.Combine(RushCommon.BaselineDir, "backup", batch);
                Directory.CreateDirectory(backupDir);
                foreach (var grp in sites.GroupBy(x => x.M))
                {
                    Model m = grp.Key;
                    string text = m.Text;
                    File.WriteAllText(Path.Combine(backupDir, m.Rel.Replace('/', '_') + ".bak"), text, new UTF8Encoding(false));
                    foreach (TgSite s in grp.OrderByDescending(x => x.Start))
                    {
                        if (text.Substring(s.Start, s.End - s.Start) != s.Old) throw new InvalidOperationException("text moved under " + m.Rel + ":" + s.Line);
                        text = text.Substring(0, s.Start) + s.New + text.Substring(s.End);
                    }
                    File.WriteAllText(m.Path, text, new UTF8Encoding(false));
                }
                string mp = RushCommon.FileIn("il_map.txt");
                string map = File.ReadAllText(mp);
                var existing = new HashSet<string>(RushIL.LoadMap().Where(r => r.Kind == "Reviewed").Select(r => r.Type + "|" + r.A), StringComparer.Ordinal);
                var add = new StringBuilder();
                add.AppendLine();
                add.AppendLine("# Stage 3 batch 4: GetComponent<T>() -> TryGetComponent<T>(out ..), one reviewed line per changed method");
                foreach (string r in rules)
                {
                    string[] p = r.Split('|');
                    if (!existing.Contains(p[1] + "|" + p[2])) add.AppendLine(r);
                }
                File.WriteAllText(mp, map.TrimEnd() + "\n" + add, new UTF8Encoding(false));
            }

            return "total=" + total + " convertible=" + sites.Count + " manual=" + manual.Count + " rules=" + rules.Count + " files=" + sites.Select(x => x.M.Rel).Distinct().Count();
        }
    }
}
