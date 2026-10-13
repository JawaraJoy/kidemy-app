using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace RushRefactor
{
    /// <summary>
    /// Stage 2 / rule #3. Three modes, spec format "Mode|Assets/path/File.cs|ClassName|MethodName" (Mode may be left out = P):
    ///   P  plain public method X      -> public X(params) wrapper + private XInternal(params without defaults)
    ///   V  public virtual method X    -> public X(params) non-virtual wrapper + protected virtual XInternal(params, defaults kept)
    ///   O  public override method X   -> protected override XInternal(params, defaults kept)
    /// In every mode the calls to X inside the class that is processed (unqualified, this. and base. receivers) go to XInternal.
    /// Calls on any other receiver keep calling the public X. Run(..., dryRun=true) only reports.
    /// Originals are copied to RushBaseline/backup/&lt;batch&gt;/ before writing.
    /// </summary>
    public static class RushSplit
    {
        sealed class Decl
        {
            public int LineStart;
            public int NameStart;
            public int ParenOpen;
            public int ParenClose;
            public int BodyOpen;
            public int BodyClose;
            public string Mods;
            public string Ret;
            public string Indent;
        }

        public static string Run(string batch, string[] specs, bool dryRun)
        {
            var report = new StringBuilder();
            var perFile = new Dictionary<string, List<string[]>>();
            foreach (string spec in specs)
            {
                string[] p = spec.Split('|');
                if (p.Length == 3) p = new[] { "P", p[0], p[1], p[2] };
                if (p.Length != 4 || (p[0] != "P" && p[0] != "V" && p[0] != "O")) return "BAD SPEC " + spec;
                List<string[]> list;
                if (!perFile.TryGetValue(p[1], out list)) { list = new List<string[]>(); perFile[p[1]] = list; }
                list.Add(p);
            }

            var outputs = new Dictionary<string, string>();
            var boms = new Dictionary<string, bool>();
            bool ok = true;
            foreach (KeyValuePair<string, List<string[]>> kv in perFile)
            {
                string abs = Path.Combine(RushCommon.ProjectRoot, kv.Key);
                byte[] raw = File.ReadAllBytes(abs);
                bool bom = raw.Length >= 3 && raw[0] == 0xEF && raw[1] == 0xBB && raw[2] == 0xBF;
                string text = new UTF8Encoding(false).GetString(raw, bom ? 3 : 0, raw.Length - (bom ? 3 : 0));
                foreach (string[] spec in kv.Value)
                {
                    report.AppendLine(spec[0] + " " + spec[1] + " | " + spec[2] + "." + spec[3]);
                    string err;
                    string next = SplitOne(text, spec[2], spec[3], spec[0], report, out err);
                    if (next == null) { report.AppendLine("  FAIL: " + err); ok = false; continue; }
                    text = next;
                }
                outputs[kv.Key] = text;
                boms[kv.Key] = bom;
            }
            if (!ok) return "ABORT, nothing written\n" + report;
            if (dryRun) return "DRY RUN OK\n" + report;

            string bdir = Path.Combine(RushCommon.ProjectRoot, "RushBaseline/backup/" + batch);
            foreach (KeyValuePair<string, string> kv in outputs)
            {
                string abs = Path.Combine(RushCommon.ProjectRoot, kv.Key);
                string bp = Path.Combine(bdir, kv.Key);
                Directory.CreateDirectory(Path.GetDirectoryName(bp));
                File.Copy(abs, bp, true);
                File.WriteAllText(abs, kv.Value, new UTF8Encoding(boms[kv.Key]));
                report.AppendLine("written " + kv.Key);
            }
            return "APPLIED\n" + report;
        }

        static string SplitOne(string text, string className, string method, string mode, StringBuilder report, out string error)
        {
            error = "";
            string nl = text.Contains("\r\n") ? "\r\n" : "\n";
            string masked = Mask(text);

            int classOpen, classClose;
            if (!FindClassBody(masked, className, out classOpen, out classClose)) { error = "class not found"; return null; }

            // ---- declarations
            var rx = new Regex(@"(?<![\w.])public[ \t]+(?<mods>(?:(?:static|new|sealed|virtual|override|abstract|async)[ \t]+)*)(?<ret>[\w.<>\[\],? ]+?)[ \t]+(?<name>" + Regex.Escape(method) + @")[ \t]*\(");
            var decls = new List<Decl>();
            foreach (Match m in rx.Matches(masked))
            {
                if (m.Index < classOpen || m.Index > classClose) continue;
                if (Depth(masked, classOpen, m.Index) != 1) continue;
                string mods = m.Groups["mods"].Value;
                bool isVirtual = Regex.IsMatch(mods, @"\bvirtual\b");
                bool isOverride = Regex.IsMatch(mods, @"\boverride\b");
                bool isOther = Regex.IsMatch(mods, @"\b(abstract|async)\b");
                bool fits = !isOther && ((mode == "P" && !isVirtual && !isOverride) || (mode == "V" && isVirtual) || (mode == "O" && isOverride));
                if (!fits) { report.AppendLine("  skip   declaration at line " + LineOf(text, m.Index) + " (modifiers '" + mods.Trim() + "' do not fit mode " + mode + ")"); continue; }
                var d = new Decl();
                d.NameStart = m.Groups["name"].Index;
                d.ParenOpen = m.Index + m.Length - 1;
                d.ParenClose = MatchClose(masked, d.ParenOpen, '(', ')');
                if (d.ParenClose < 0) { error = "unbalanced parentheses"; return null; }
                int q = d.ParenClose + 1;
                while (q < masked.Length && char.IsWhiteSpace(masked[q])) q++;
                if (q >= masked.Length || masked[q] != '{') { error = "method has no block body (expression body or constraint) at line " + LineOf(text, m.Index); return null; }
                d.BodyOpen = q;
                d.BodyClose = MatchClose(masked, q, '{', '}');
                if (d.BodyClose < 0) { error = "unbalanced braces"; return null; }
                int ls = text.LastIndexOf('\n', m.Index) + 1;
                string before = text.Substring(ls, m.Index - ls);
                if (before.Trim().Length != 0) { error = "declaration shares its line with something else: [" + before + "]"; return null; }
                d.LineStart = ls;
                d.Indent = before;
                d.Mods = mods;
                d.Ret = m.Groups["ret"].Value.Trim();
                decls.Add(d);
            }
            if (decls.Count == 0) { error = "no declaration of " + method + " fits mode " + mode; return null; }

            // ---- call sites inside the class (not the declaration names themselves)
            var declNames = new HashSet<int>(decls.Select(x => x.NameStart));
            var callEdits = new List<KeyValuePair<int, int>>(); // start, end of the name token
            var callRx = new Regex(@"(?<![\w@])" + Regex.Escape(method) + @"(?=[ \t]*\()");
            foreach (Match m in callRx.Matches(masked))
            {
                if (m.Index < classOpen || m.Index > classClose) continue;
                if (declNames.Contains(m.Index)) continue;
                int b = m.Index - 1;
                while (b >= 0 && (masked[b] == ' ' || masked[b] == '\t')) b--;
                if (b >= 0 && masked[b] == '.')
                {
                    int c = b - 1;
                    while (c >= 0 && (masked[c] == ' ' || masked[c] == '\t')) c--;
                    bool okRecv = EndsWithWord(masked, c, "this") || (mode != "P" && EndsWithWord(masked, c, "base"));
                    if (!okRecv) { report.AppendLine("  keep   line " + LineOf(text, m.Index) + " (other receiver, stays on the public wrapper): " + LineText(text, m.Index).Trim()); continue; }
                }
                else if (b >= 0 && (char.IsLetterOrDigit(masked[b]) || masked[b] == '_') && !PrecededByKeyword(masked, b))
                {
                    report.AppendLine("  REVIEW line " + LineOf(text, m.Index) + " (word before the name): " + LineText(text, m.Index).Trim());
                    continue;
                }
                callEdits.Add(new KeyValuePair<int, int>(m.Index, m.Index + method.Length));
                report.AppendLine("  call   line " + LineOf(text, m.Index) + ": " + LineText(text, m.Index).Trim() + "   ->  " + method + "Internal");
            }
            var groupRx = new Regex(@"(?<![\w.@])" + Regex.Escape(method) + @"(?![\w(])(?![ \t]*\()");
            foreach (Match m in groupRx.Matches(masked))
            {
                if (m.Index < classOpen || m.Index > classClose) continue;
                if (declNames.Contains(m.Index)) continue;
                report.AppendLine("  REVIEW line " + LineOf(text, m.Index) + " (name without call: method group or another member?): " + LineText(text, m.Index).Trim());
            }

            // ---- apply call edits (descending) and remember how positions shift
            string result = text;
            var shifts = new List<KeyValuePair<int, int>>(); // original position, delta
            foreach (KeyValuePair<int, int> e in callEdits.OrderByDescending(x => x.Key))
            {
                result = result.Substring(0, e.Key) + method + "Internal" + result.Substring(e.Value);
                shifts.Add(new KeyValuePair<int, int>(e.Key, method.Length + "Internal".Length - (e.Value - e.Key)));
            }
            Func<int, int> map = pos =>
            {
                int delta = 0;
                foreach (KeyValuePair<int, int> s in shifts) if (s.Key < pos) delta += s.Value;
                return pos + delta;
            };

            // ---- declarations (descending, so earlier positions stay valid)
            foreach (Decl d in decls.OrderByDescending(x => x.LineStart))
            {
                int ls = map(d.LineStart);
                int po = map(d.ParenOpen);
                int pc = map(d.ParenClose);
                int bo = map(d.BodyOpen);
                int bc = map(d.BodyClose);
                string gap = result.Substring(pc + 1, bo - (pc + 1));
                string body = result.Substring(bo, bc + 1 - bo);
                string rawParams = result.Substring(po + 1, pc - po - 1);
                List<string> args;
                string stripped = StripDefaults(rawParams, Mask(rawParams), out args);
                string call = (d.Ret == "void" ? "" : "return ") + method + "Internal(" + string.Join(", ", args.ToArray()) + ");";
                string replacement;
                if (mode == "O")
                {
                    string head = d.Indent + "protected " + d.Mods + d.Ret + " " + method + "Internal(" + rawParams + ")";
                    replacement = head + gap + body;
                    report.AppendLine("  move   line " + LineOf(text, d.NameStart) + ": public " + d.Mods + d.Ret + " " + method + "(" + rawParams.Trim() + ")  =>  protected " + d.Mods + d.Ret + " " + method + "Internal(...)");
                }
                else
                {
                    string wrapperMods = d.Mods.Replace("virtual", "").Replace("  ", " ").TrimStart();
                    string wrapperHead = d.Indent + "public " + wrapperMods + d.Ret + " " + method + "(" + rawParams + ")";
                    string wrapper = wrapperHead + nl + d.Indent + "{" + nl + d.Indent + "    " + call + nl + d.Indent + "}";
                    string internalHead;
                    if (mode == "V") internalHead = d.Indent + "protected virtual " + wrapperMods + d.Ret + " " + method + "Internal(" + rawParams + ")";
                    else internalHead = d.Indent + "private " + d.Mods + d.Ret + " " + method + "Internal(" + stripped + ")";
                    replacement = wrapper + nl + nl + internalHead + gap + body;
                    report.AppendLine("  split  line " + LineOf(text, d.NameStart) + ": public " + d.Mods + d.Ret + " " + method + "(" + rawParams.Trim() + ")  =>  wrapper + " + (mode == "V" ? "protected virtual " : "private ") + method + "Internal");
                }
                result = result.Substring(0, ls) + replacement + result.Substring(bc + 1);
            }
            return result;
        }

        // ---------------------------------------------------------------- helpers

        static bool EndsWithWord(string masked, int end, string word)
        {
            int start = end - word.Length + 1;
            if (start < 0 || masked.Substring(start, word.Length) != word) return false;
            return start - 1 < 0 || !(char.IsLetterOrDigit(masked[start - 1]) || masked[start - 1] == '_');
        }

        static string StripDefaults(string raw, string masked, out List<string> args)
        {
            args = new List<string>();
            var parts = new List<string>();
            int depth = 0, start = 0;
            for (int i = 0; i < masked.Length; i++)
            {
                char c = masked[i];
                if (c == '(' || c == '<' || c == '[' || c == '{') depth++;
                else if (c == ')' || c == '>' || c == ']' || c == '}') depth--;
                else if (c == ',' && depth == 0) { parts.Add(raw.Substring(start, i - start)); start = i + 1; }
            }
            if (raw.Substring(start).Trim().Length > 0) parts.Add(raw.Substring(start));
            var kept = new List<string>();
            foreach (string part in parts)
            {
                int eq = IndexOfTopLevelEquals(part);
                string decl = (eq >= 0 ? part.Substring(0, eq) : part).Trim();
                string[] toks = decl.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                string name = toks[toks.Length - 1];
                string prefix = toks.Length > 1 && (toks[0] == "ref" || toks[0] == "out" || toks[0] == "in") ? toks[0] + " " : "";
                args.Add(prefix + name);
                kept.Add(decl);
            }
            return string.Join(", ", kept.ToArray());
        }

        static int IndexOfTopLevelEquals(string s)
        {
            int depth = 0;
            bool inStr = false;
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (c == '"') inStr = !inStr;
                if (inStr) continue;
                if (c == '(' || c == '<' || c == '[') depth++;
                else if (c == ')' || c == '>' || c == ']') depth--;
                else if (c == '=' && depth == 0) return i;
            }
            return -1;
        }

        static bool PrecededByKeyword(string masked, int b)
        {
            int s = b;
            while (s > 0 && (char.IsLetterOrDigit(masked[s - 1]) || masked[s - 1] == '_')) s--;
            string w = masked.Substring(s, b + 1 - s);
            return w == "else" || w == "return" || w == "yield" || w == "do";
        }

        static int LineOf(string t, int pos)
        {
            int n = 1;
            for (int i = 0; i < pos && i < t.Length; i++) if (t[i] == '\n') n++;
            return n;
        }

        static string LineText(string t, int pos)
        {
            int s = t.LastIndexOf('\n', Math.Max(0, pos - 1)) + 1;
            int e = t.IndexOf('\n', pos);
            if (e < 0) e = t.Length;
            return t.Substring(s, e - s).TrimEnd('\r');
        }

        static bool FindClassBody(string masked, string className, out int open, out int close)
        {
            open = close = -1;
            var rx = new Regex(@"(?<![\w.])(class|struct)[ \t]+" + Regex.Escape(className) + @"(?![\w])");
            Match m = rx.Match(masked);
            if (!m.Success) return false;
            int q = masked.IndexOf('{', m.Index);
            if (q < 0) return false;
            int c = MatchClose(masked, q, '{', '}');
            if (c < 0) return false;
            open = q;
            close = c;
            return true;
        }

        static int Depth(string masked, int open, int pos)
        {
            int d = 0;
            for (int i = open; i < pos; i++)
            {
                if (masked[i] == '{') d++;
                else if (masked[i] == '}') d--;
            }
            return d;
        }

        static int MatchClose(string masked, int open, char o, char c)
        {
            int d = 0;
            for (int i = open; i < masked.Length; i++)
            {
                if (masked[i] == o) d++;
                else if (masked[i] == c) { d--; if (d == 0) return i; }
            }
            return -1;
        }

        /// <summary>Blanks out comments, string and char literals (same length, newlines kept). Interpolation holes stay visible.</summary>
        public static string Mask(string t)
        {
            char[] m = t.ToCharArray();
            int n = t.Length;
            int i = 0;
            while (i < n)
            {
                char c = t[i];
                if (c == '/' && i + 1 < n && t[i + 1] == '/')
                {
                    while (i < n && t[i] != '\n') { if (t[i] != '\r') m[i] = ' '; i++; }
                    continue;
                }
                if (c == '/' && i + 1 < n && t[i + 1] == '*')
                {
                    m[i] = ' '; m[i + 1] = ' '; i += 2;
                    while (i < n && !(t[i] == '*' && i + 1 < n && t[i + 1] == '/')) { if (t[i] != '\n' && t[i] != '\r') m[i] = ' '; i++; }
                    if (i < n) { m[i] = ' '; if (i + 1 < n) m[i + 1] = ' '; i += 2; }
                    continue;
                }
                if (c == '\'')
                {
                    int j = i + 1;
                    while (j < n && t[j] != '\'' && t[j] != '\n') { if (t[j] == '\\') j++; j++; }
                    for (int k = i; k <= j && k < n; k++) if (t[k] != '\n' && t[k] != '\r') m[k] = ' ';
                    i = j + 1;
                    continue;
                }
                bool verbatim = false, interp = false;
                int q = i;
                if (c == '$' && q + 2 < n && t[q + 1] == '@' && t[q + 2] == '"') { verbatim = true; interp = true; q += 2; }
                else if (c == '@' && q + 2 < n && t[q + 1] == '$' && t[q + 2] == '"') { verbatim = true; interp = true; q += 2; }
                else if (c == '@' && q + 1 < n && t[q + 1] == '"') { verbatim = true; q += 1; }
                else if (c == '$' && q + 1 < n && t[q + 1] == '"') { interp = true; q += 1; }
                else if (c != '"') { i++; continue; }
                for (int k = i; k <= q; k++) m[k] = ' ';
                int p = q + 1;
                while (p < n)
                {
                    char x = t[p];
                    if (!verbatim && x == '\\') { m[p] = ' '; if (p + 1 < n && t[p + 1] != '\n' && t[p + 1] != '\r') m[p + 1] = ' '; p += 2; continue; }
                    if (verbatim && x == '"' && p + 1 < n && t[p + 1] == '"') { m[p] = ' '; m[p + 1] = ' '; p += 2; continue; }
                    if (x == '"') { m[p] = ' '; p++; break; }
                    if (!verbatim && x == '\n') break;
                    if (interp && x == '{')
                    {
                        if (p + 1 < n && t[p + 1] == '{') { m[p] = ' '; m[p + 1] = ' '; p += 2; continue; }
                        int depth = 1;
                        m[p] = ' ';
                        p++;
                        while (p < n && depth > 0)
                        {
                            if (t[p] == '{') depth++;
                            else if (t[p] == '}') { depth--; if (depth == 0) { m[p] = ' '; p++; break; } }
                            p++;
                        }
                        continue;
                    }
                    if (interp && x == '}' && p + 1 < n && t[p + 1] == '}') { m[p] = ' '; m[p + 1] = ' '; p += 2; continue; }
                    if (x != '\n' && x != '\r') m[p] = ' ';
                    p++;
                }
                i = p;
            }
            return new string(m);
        }
    }
}
