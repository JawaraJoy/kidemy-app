using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RushRefactor
{
    /// <summary>
    /// Tahap 4: penerap patch berbasis spec teks (RushBaseline/&lt;name&gt;.spec). Dry-run tidak menulis apa pun.
    ///   @E file line        ganti: baris '-' (teks lama, harus muncul TEPAT 1 kali di baris itu) -> baris '+' (teks baru)
    ///   @I file afterLine   sisip semua baris '+' setelah baris itu (nomor baris = nomor di file asli)
    /// Karakter ` (backtick) di spec dibaca sebagai tanda kutip ganda. @C file Kelas: sisipkan baris '+' di awal kelas itu. Backup file asli: RushBaseline/backup/&lt;batch&gt;/&lt;path&gt;.
    /// Hasil: RushBaseline/stage4_&lt;batch&gt;_dryrun.txt atau _applied.txt (sebelum/sesudah per baris).
    /// </summary>
    public static class RushPatch
    {
        sealed class Op
        {
            public char Kind;
            public string File;
            public int Line;
            public string Old;
            public List<string> New = new List<string>();
        }

        public static string Run(string batch, string specName, bool dryRun)
        {
            string root = Path.GetDirectoryName(UnityEngine.Application.dataPath);
            string specPath = Path.Combine(root, "RushBaseline/" + specName);
            if (!File.Exists(specPath)) return "SPEC TIDAK ADA: " + specPath;
            var ops = Parse(File.ReadAllText(specPath, Encoding.UTF8).Replace('`', '"'));
            var report = new StringBuilder();
            var files = new SortedSet<string>();
            foreach (Op o in ops) files.Add(o.File);

            var oldBytes = new Dictionary<string, byte[]>();
            var newBytes = new Dictionary<string, byte[]>();
            int nE = 0, nI = 0, errors = 0;
            foreach (string rel in files)
            {
                string full = Path.Combine(root, rel);
                if (!File.Exists(full)) { report.AppendLine("GAGAL file tidak ada: " + rel); errors++; continue; }
                byte[] raw = File.ReadAllBytes(full);
                oldBytes[rel] = raw;
                bool bom = raw.Length >= 3 && raw[0] == 0xEF && raw[1] == 0xBB && raw[2] == 0xBF;
                string text = Encoding.UTF8.GetString(raw, bom ? 3 : 0, raw.Length - (bom ? 3 : 0));
                bool crlf = text.Contains("\r\n");
                var lines = new List<string>(text.Split('\n'));

                foreach (Op o in ops)
                {
                    if (o.File != rel || o.Kind != 'E') continue;
                    int idx = o.Line - 1;
                    if (idx < 0 || idx >= lines.Count) { report.AppendLine("GAGAL " + rel + ":" + o.Line + " di luar file"); errors++; continue; }
                    string l = lines[idx];
                    int cnt = 0, at = 0;
                    while ((at = l.IndexOf(o.Old, at, StringComparison.Ordinal)) >= 0) { cnt++; at += Math.Max(1, o.Old.Length); }
                    if (cnt != 1) { report.AppendLine("GAGAL " + rel + ":" + o.Line + " cocok=" + cnt + " untuk: " + o.Old); errors++; continue; }
                    string repl = o.New.Count > 0 ? o.New[0] : "";
                    lines[idx] = l.Replace(o.Old, repl);
                    nE++;
                    report.AppendLine(Path.GetFileName(rel) + ":" + o.Line);
                    report.AppendLine("  - " + l.Trim());
                    report.AppendLine("  + " + lines[idx].Trim());
                }

                foreach (Op o in ops)
                {
                    if (o.File != rel || o.Kind != 'C') continue;
                    int at2 = ResolveClass(lines, o.Old);
                    if (at2 < 0) { report.AppendLine("GAGAL kelas tidak ketemu: " + rel + " " + o.Old); errors++; o.Kind = 'X'; continue; }
                    string ind = IndentAfter(lines, at2);
                    for (int k = 0; k < o.New.Count; k++) if (o.New[k].Length > 0) o.New[k] = ind + o.New[k];
                    o.New.Add("");
                    o.Line = at2 + 1;
                    o.Kind = 'I';
                }
                var ins = new List<Op>();
                foreach (Op o in ops) if (o.File == rel && o.Kind == 'I') ins.Add(o);
                ins.Sort((a, b) => b.Line.CompareTo(a.Line));
                foreach (Op o in ins)
                {
                    if (o.Line < 0 || o.Line > lines.Count) { report.AppendLine("GAGAL sisip " + rel + ":" + o.Line + " di luar file"); errors++; continue; }
                    var nl = new List<string>();
                    foreach (string t in o.New) nl.Add(t + (crlf ? "\r" : ""));
                    lines.InsertRange(o.Line, nl);
                    nI += nl.Count;
                    report.AppendLine(Path.GetFileName(rel) + ": sisip " + nl.Count + " baris setelah baris " + o.Line);
                    foreach (string t in o.New) report.AppendLine("  + " + t.Trim());
                }

                byte[] body = Encoding.UTF8.GetBytes(string.Join("\n", lines.ToArray()));
                if (bom)
                {
                    var wb = new byte[body.Length + 3];
                    wb[0] = 0xEF; wb[1] = 0xBB; wb[2] = 0xBF;
                    Array.Copy(body, 0, wb, 3, body.Length);
                    body = wb;
                }
                newBytes[rel] = body;
            }

            string head = "batch=" + batch + " spec=" + specName + " file=" + files.Count + " ganti=" + nE + " sisip=" + nI + " error=" + errors + " dry=" + dryRun;
            bool wrote = false;
            if (!dryRun && errors == 0)
            {
                foreach (string rel in files)
                {
                    string bk = Path.Combine(root, "RushBaseline/backup/" + batch + "/" + rel);
                    Directory.CreateDirectory(Path.GetDirectoryName(bk));
                    File.WriteAllBytes(bk, oldBytes[rel]);
                    File.WriteAllBytes(Path.Combine(root, rel), newBytes[rel]);
                }
                wrote = true;
            }
            string outName = "RushBaseline/stage4_" + batch + (wrote ? "_applied.txt" : "_dryrun.txt");
            File.WriteAllText(Path.Combine(root, outName), head + "\n" + report, new UTF8Encoding(false));
            return head + (wrote ? "\nDITULIS. Backup: RushBaseline/backup/" + batch + "/\n" : (errors > 0 ? "\nTIDAK ADA YANG DITULIS (ada error)\n" : "\n")) + "\n" + report;
        }

        static int ResolveClass(List<string> lines, string cls)
        {
            var rx = new System.Text.RegularExpressions.Regex(@"\b(class|struct)\s+" + System.Text.RegularExpressions.Regex.Escape(cls) + @"\b");
            for (int i = 0; i < lines.Count; i++)
            {
                if (lines[i].TrimStart().StartsWith("//") || !rx.IsMatch(lines[i])) continue;
                for (int j = i; j < lines.Count && j < i + 6; j++)
                    if (lines[j].Contains("{")) return j;
                return -1;
            }
            return -1;
        }

        static string IndentAfter(List<string> lines, int braceIdx)
        {
            for (int i = braceIdx + 1; i < lines.Count; i++)
            {
                string t = lines[i].TrimEnd('\r');
                if (t.Trim().Length == 0) continue;
                int n = 0;
                while (n < t.Length && (t[n] == ' ' || t[n] == '\t')) n++;
                return t.Substring(0, n);
            }
            return "    ";
        }

        static List<Op> Parse(string spec)
        {
            var ops = new List<Op>();
            Op cur = null;
            foreach (string raw in spec.Replace("\r", "").Split('\n'))
            {
                if (raw.StartsWith("#") || raw.Length == 0) continue;
                if (raw[0] == '@')
                {
                    string[] p = raw.Substring(1).Split(' ');
                    cur = new Op { Kind = p[0][0], File = p[1] };
                    if (cur.Kind == 'C') cur.Old = p[2]; else cur.Line = int.Parse(p[2]);
                    ops.Add(cur);
                }
                else if (cur != null && raw[0] == '-') cur.Old = raw.Substring(1);
                else if (cur != null && raw[0] == '+') cur.New.Add(raw.Substring(1));
            }
            return ops;
        }
    }
}
