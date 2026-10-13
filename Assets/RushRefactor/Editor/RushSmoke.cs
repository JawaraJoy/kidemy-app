using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RushRefactor
{
    /// <summary>
    /// Stage 0 behaviour gate. Plays every scene in the Editor for a few seconds, records what the game looks like
    /// (GameManager present, quest counts, active quests, missing scripts, errors) and drives the quest API
    /// (voice, submit, next, reset) so a refactor that breaks gameplay shows up as a changed line.
    /// Safety: the LAST quest is never submitted, because that POSTs the result to the real API.
    /// The run survives domain reloads (state lives in SessionState) and restores the original scene when done.
    /// </summary>
    [InitializeOnLoad]
    public static class RushSmoke
    {
        const string c_Active = "RushSmoke.Active";
        const string c_Phase = "RushSmoke.Phase";
        const string c_Index = "RushSmoke.Index";
        const string c_Queue = "RushSmoke.Queue";
        const string c_Out = "RushSmoke.Out";
        const string c_Original = "RushSmoke.Original";
        const string c_PrevMute = "RushSmoke.PrevMute";
        const string c_Since = "RushSmoke.Since";
        const string c_Started = "RushSmoke.Started";
        const string c_RunStarted = "RushSmoke.RunStarted";
        const string c_L2 = "RushSmoke.L2";
        const string c_L2Step = "RushSmoke.L2Step";
        const string c_L2Log = "RushSmoke.L2Log";
        const string c_Info = "RushSmoke.Info";
        const string c_Status = "RushSmoke.Status";
        const string c_Errors = "RushSmoke.Errors";
        const string c_Warnings = "RushSmoke.Warnings";
        const string c_ErrMsgs = "RushSmoke.ErrMsgs";

        const string c_BaselineFile = "smoke_baseline.txt";
        const string c_CurrentFile = "smoke_current.txt";
        const string c_DiffFile = "smoke_diff.txt";

        const double c_Dwell = 2.5;
        const double c_Gap = 0.4;
        const double c_EnterTimeout = 90.0;
        const double c_StopTimeout = 90.0;

        static readonly string[] s_CompareFields = { "gm", "quests", "active", "missing", "l2" };

        static RushSmoke()
        {
            Application.logMessageReceived -= OnLog;
            Application.logMessageReceived += OnLog;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }

        // ---------------------------------------------------------------- menu

        [MenuItem("Tools/Rush Refactor/Smoke/Run All Scenes (baseline)")]
        static void MenuBaseline() { Debug.Log("[Rush] " + BeginAll(c_BaselineFile, true)); }

        [MenuItem("Tools/Rush Refactor/Smoke/Run All Scenes (current) And Compare")]
        static void MenuCurrent() { Debug.Log("[Rush] " + BeginAll(c_CurrentFile, true)); }

        [MenuItem("Tools/Rush Refactor/Smoke/Status")]
        static void MenuStatus() { Debug.Log("[Rush] " + Status()); }

        [MenuItem("Tools/Rush Refactor/Smoke/Abort")]
        static void MenuAbort() { Debug.Log("[Rush] " + Abort()); }

        // ---------------------------------------------------------------- public API

        public static string BeginAll(string outFileName, bool l2)
        {
            var scenes = new HashSet<string>(StringComparer.Ordinal);
            foreach (string g in AssetDatabase.FindAssets("t:Scene", RushCommon.AssetRoots))
            {
                string p = AssetDatabase.GUIDToAssetPath(g);
                if (RushCommon.IsProjectPath(p)) scenes.Add(p);
            }
            var list = scenes.ToList();
            list.Sort(StringComparer.Ordinal);
            return Begin(outFileName, list.ToArray(), l2);
        }

        public static string Begin(string outFileName, string[] scenePaths, bool l2)
        {
            if (SessionState.GetString(c_Active, "") == "1") return "ALREADY RUNNING: " + Status();
            if (EditorApplication.isPlayingOrWillChangePlaymode) return "ABORT: editor is in play mode";
            if (EditorApplication.isCompiling) return "ABORT: editor is compiling";
            if (PrefabStageUtility.GetCurrentPrefabStage() != null) return "ABORT: a prefab stage is open, close it first";
            if (scenePaths == null || scenePaths.Length == 0) return "ABORT: no scenes given";
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene s = SceneManager.GetSceneAt(i);
                if (s.isDirty || string.IsNullOrEmpty(s.path))
                    return "ABORT: scene '" + s.name + "' is unsaved or modified; save or discard it first";
            }

            string outPath = RushCommon.FileIn(outFileName);
            File.WriteAllText(outPath, "", new UTF8Encoding(false));

            SessionState.SetString(c_Out, outPath);
            SessionState.SetString(c_Queue, string.Join("\n", scenePaths));
            SessionState.SetInt(c_Index, 0);
            SessionState.SetString(c_Original, SceneManager.GetActiveScene().path);
            SessionState.SetBool(c_PrevMute, EditorUtility.audioMasterMute);
            SessionState.SetString(c_L2, l2 ? "1" : "0");
            SessionState.SetString(c_RunStarted, EditorApplication.timeSinceStartup.ToString("R", RushCommon.Inv));
            EditorUtility.audioMasterMute = true;
            SessionState.SetString(c_Phase, "OPEN");
            SessionState.SetString(c_Active, "1");
            return "STARTED: " + scenePaths.Length + " scenes, l2=" + l2 + " -> " + outPath;
        }

        public static string Abort()
        {
            if (SessionState.GetString(c_Active, "") != "1") return "nothing running";
            SessionState.SetString(c_Phase, "ABORTING");
            if (EditorApplication.isPlaying) EditorApplication.isPlaying = false;
            return "aborting";
        }

        public static bool IsRunning { get { return SessionState.GetString(c_Active, "") == "1"; } }

        public static string Status()
        {
            var sb = new StringBuilder();
            bool active = SessionState.GetString(c_Active, "") == "1";
            string[] queue = Queue();
            int idx = SessionState.GetInt(c_Index, 0);
            sb.AppendLine("active=" + active + " phase=" + SessionState.GetString(c_Phase, "") + " scene=" +
                          Math.Min(idx + 1, Math.Max(queue.Length, 1)) + "/" + queue.Length);
            if (active && idx < queue.Length) sb.AppendLine("current=" + queue[idx]);
            double started = ParseD(SessionState.GetString(c_RunStarted, "0"));
            if (started > 0) sb.AppendLine("elapsed=" + (EditorApplication.timeSinceStartup - started).ToString("F0") + "s");
            string outPath = SessionState.GetString(c_Out, "");
            if (outPath.Length > 0 && File.Exists(outPath))
            {
                string[] lines = File.ReadAllLines(outPath);
                int scenes = lines.Count(l => l.StartsWith("SCENE\t", StringComparison.Ordinal));
                sb.AppendLine("recorded=" + scenes + " lines, file=" + outPath);
                foreach (string l in lines.Where(x => x.StartsWith("SCENE\t", StringComparison.Ordinal)).Reverse().Take(3).Reverse())
                    sb.AppendLine("  " + RushCommon.Clip(l, 260));
                foreach (string l in lines.Where(x => x.StartsWith("DONE\t", StringComparison.Ordinal))) sb.AppendLine("  " + l);
            }
            return sb.ToString();
        }

        // ---------------------------------------------------------------- state machine

        static void Tick()
        {
            if (SessionState.GetString(c_Active, "") != "1") return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            string phase = SessionState.GetString(c_Phase, "");
            double now = EditorApplication.timeSinceStartup;
            try
            {
                switch (phase)
                {
                    case "OPEN": DoOpen(now); break;
                    case "ENTER": DoEnter(now); break;
                    case "RUN": DoRun(now); break;
                    case "L2": DoL2(now); break;
                    case "WAIT_STOP": DoWaitStop(now); break;
                    case "ABORTING": DoAborting(); break;
                }
            }
            catch (Exception e)
            {
                SetStatus("FAIL:EXC " + e.GetType().Name + " " + RushCommon.Clip(e.Message, 120));
                if (EditorApplication.isPlaying) StopPlay(now);
                else if (phase != "OPEN") Advance(now);
                else SessionState.SetInt(c_Index, SessionState.GetInt(c_Index, 0) + 1);
            }
        }

        static void DoOpen(double now)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            string[] queue = Queue();
            int idx = SessionState.GetInt(c_Index, 0);
            if (idx >= queue.Length) { Finish("DONE"); return; }

            string path = queue[idx];
            SessionState.SetInt(c_Errors, 0);
            SessionState.SetInt(c_Warnings, 0);
            SessionState.SetString(c_ErrMsgs, "");
            SessionState.SetString(c_Info, "");
            SessionState.SetString(c_L2Log, "");
            SessionState.SetInt(c_L2Step, 0);
            SessionState.SetString(c_Status, "OK");
            SessionState.SetString(c_Started, now.ToString("R", RushCommon.Inv));

            if (!OpenOnly(path))
            {
                SetStatus("FAIL:OPEN");
                Advance(now);
                return;
            }
            SessionState.SetString(c_Phase, "ENTER");
            SessionState.SetString(c_Since, now.ToString("R", RushCommon.Inv));
            EditorApplication.isPlaying = true;
        }

        // Opens the scene additively, makes it active and closes everything else, so a dirty scene can never trigger a save prompt.
        static bool OpenOnly(string path)
        {
            Scene target = SceneManager.GetSceneByPath(path);
            if (!target.IsValid() || !target.isLoaded)
            {
                try { target = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive); }
                catch (Exception) { return false; }
            }
            if (!target.IsValid()) return false;
            SceneManager.SetActiveScene(target);
            for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
            {
                Scene s = SceneManager.GetSceneAt(i);
                if (s != target) EditorSceneManager.CloseScene(s, true);
            }
            return true;
        }

        static void DoEnter(double now)
        {
            double since = ParseD(SessionState.GetString(c_Since, "0"));
            if (EditorApplication.isPlaying)
            {
                SessionState.SetString(c_Phase, "RUN");
                SessionState.SetString(c_Since, now.ToString("R", RushCommon.Inv));
                return;
            }
            if (now - since > c_EnterTimeout)
            {
                SetStatus("FAIL:ENTER_TIMEOUT");
                SessionState.SetString(c_Phase, "WAIT_STOP");
                SessionState.SetString(c_Since, now.ToString("R", RushCommon.Inv));
            }
        }

        static void DoRun(double now)
        {
            if (!EditorApplication.isPlaying)
            {
                SetStatus("FAIL:LEFT_PLAYMODE");
                SessionState.SetString(c_Phase, "WAIT_STOP");
                SessionState.SetString(c_Since, now.ToString("R", RushCommon.Inv));
                return;
            }
            double since = ParseD(SessionState.GetString(c_Since, "0"));
            if (now - since < c_Dwell) return;

            SessionState.SetString(c_Info, RecordL1());
            if (SessionState.GetString(c_L2, "") == "1")
            {
                SessionState.SetString(c_Phase, "L2");
                SessionState.SetInt(c_L2Step, 0);
                SessionState.SetString(c_Since, now.ToString("R", RushCommon.Inv));
            }
            else
            {
                StopPlay(now);
            }
        }

        static void DoL2(double now)
        {
            if (!EditorApplication.isPlaying)
            {
                SetStatus("FAIL:LEFT_PLAYMODE");
                SessionState.SetString(c_Phase, "WAIT_STOP");
                SessionState.SetString(c_Since, now.ToString("R", RushCommon.Inv));
                return;
            }
            double since = ParseD(SessionState.GetString(c_Since, "0"));
            if (now - since < c_Gap) return;

            object gm = GetGm();
            if (gm == null)
            {
                AppendL2("nogm");
                StopPlay(now);
                return;
            }
            int step = SessionState.GetInt(c_L2Step, 0);
            switch (step)
            {
                case 0: AppendL2(CallApi(gm, "PlayQuestionVoice")); break;
                case 1: AppendL2(CallApi(gm, "PlaySoundQuest")); break;
                case 2:
                    if (!IsLastQuest(gm)) AppendL2(CallApi(gm, "Submit", 3));
                    else AppendL2("Submit:skipped(last-quest->API)");
                    break;
                case 3:
                    if (!IsLastQuest(gm)) AppendL2(CallApi(gm, "Next"));
                    else AppendL2("Next:skipped(last)");
                    break;
                case 4: AppendL2(CallApi(gm, "ResetQuest")); break;
                default: StopPlay(now); return;
            }
            SessionState.SetInt(c_L2Step, step + 1);
            SessionState.SetString(c_Since, now.ToString("R", RushCommon.Inv));
        }

        static void StopPlay(double now)
        {
            SessionState.SetString(c_Phase, "WAIT_STOP");
            SessionState.SetString(c_Since, now.ToString("R", RushCommon.Inv));
            EditorApplication.isPlaying = false;
        }

        static void DoWaitStop(double now)
        {
            if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                double since = ParseD(SessionState.GetString(c_Since, "0"));
                if (now - since > c_StopTimeout)
                {
                    SetStatus("FAIL:STOP_TIMEOUT");
                    EditorApplication.isPlaying = false;
                    SessionState.SetString(c_Since, now.ToString("R", RushCommon.Inv));
                }
                return;
            }
            Advance(now);
        }

        static void Advance(double now)
        {
            string[] queue = Queue();
            int idx = SessionState.GetInt(c_Index, 0);
            string path = idx < queue.Length ? queue[idx] : "?";
            double started = ParseD(SessionState.GetString(c_Started, "0"));
            string info = SessionState.GetString(c_Info, "");
            if (info.Length == 0) info = "gm=?;quests=?;active=?;missing=?";
            string l2 = SessionState.GetString(c_L2Log, "");
            string line = "SCENE\t" + path + "\t" + SessionState.GetString(c_Status, "OK") + "\t" + info +
                          ";errors=" + SessionState.GetInt(c_Errors, 0) +
                          ";warnings=" + SessionState.GetInt(c_Warnings, 0) +
                          ";l2=" + (l2.Length == 0 ? "-" : l2) +
                          ";sec=" + (started > 0 ? (now - started).ToString("F0") : "?");
            AppendOut(line);
            foreach (string m in SessionState.GetString(c_ErrMsgs, "").Split('\n'))
            {
                if (m.Length > 0) AppendOut("ERR\t" + path + "\t" + m);
            }
            SessionState.SetInt(c_Index, idx + 1);
            SessionState.SetString(c_Phase, "OPEN");
        }

        static void DoAborting()
        {
            if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode) return;
            Finish("ABORTED");
        }

        static void Finish(string why)
        {
            EditorUtility.audioMasterMute = SessionState.GetBool(c_PrevMute, false);
            string original = SessionState.GetString(c_Original, "");
            if (original.Length > 0) OpenOnly(original);
            AppendOut("DONE\t" + why);
            SessionState.SetString(c_Active, "");
            SessionState.SetString(c_Phase, "");
        }

        static void SetStatus(string status) { SessionState.SetString(c_Status, status); }

        // ---------------------------------------------------------------- recording

        static string RecordL1()
        {
            object gm = GetGm();
            string quests = "?";
            if (gm != null)
            {
                PropertyInfo p = gm.GetType().GetProperty("QuestsCount", BindingFlags.Public | BindingFlags.Instance);
                if (p != null) quests = Convert.ToString(p.GetValue(gm, null));
            }
            int active = 0;
            Type qt = RushCommon.FindType("EduGame.Quest");
            if (qt != null)
            {
                foreach (UnityEngine.Object o in UnityEngine.Object.FindObjectsByType(qt, FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    Component c = o as Component;
                    if (c != null && c.gameObject.activeInHierarchy) active++;
                }
            }
            return "gm=" + (gm != null) + ";quests=" + quests + ";active=" + active + ";missing=" + CountMissing();
        }

        static int CountMissing()
        {
            int n = 0;
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene s = SceneManager.GetSceneAt(i);
                if (!s.isLoaded) continue;
                foreach (GameObject root in s.GetRootGameObjects()) n += MissingIn(root);
            }
            return n;
        }

        static int MissingIn(GameObject go)
        {
            int n = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go);
            foreach (Transform child in go.transform) n += MissingIn(child.gameObject);
            return n;
        }

        static object GetGm()
        {
            Type t = RushCommon.FindType("EduGame.GameManager");
            if (t == null) return null;
            PropertyInfo p = t.GetProperty("Instance", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (p == null) return null;
            object o = p.GetValue(null, null);
            UnityEngine.Object uo = o as UnityEngine.Object;
            if (uo == null) return null; // null or destroyed
            return o;
        }

        static bool IsLastQuest(object gm)
        {
            MethodInfo m = gm.GetType().GetMethod("IsLastQuest", BindingFlags.Public | BindingFlags.Instance);
            if (m == null) return true; // when unsure, behave as if it is the last quest (never submit)
            try { return (bool)m.Invoke(gm, null); }
            catch (Exception) { return true; }
        }

        static string CallApi(object target, string method, params object[] args)
        {
            MethodInfo m = null;
            foreach (MethodInfo c in target.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance))
            {
                if (c.Name == method && c.GetParameters().Length == args.Length) { m = c; break; }
            }
            if (m == null) { AddError("L2 " + method + " not found"); return method + ":NOMETHOD"; }
            try
            {
                m.Invoke(target, args);
                return method + ":ok";
            }
            catch (TargetInvocationException e)
            {
                Exception inner = e.InnerException ?? e;
                AddError("L2 " + method + " threw " + inner.GetType().Name);
                return method + ":EXC(" + inner.GetType().Name + ")";
            }
        }

        static void AppendL2(string entry)
        {
            string cur = SessionState.GetString(c_L2Log, "");
            SessionState.SetString(c_L2Log, cur.Length == 0 ? entry : cur + "," + entry);
        }

        static void OnLog(string condition, string stackTrace, LogType type)
        {
            if (SessionState.GetString(c_Active, "") != "1") return;
            string phase = SessionState.GetString(c_Phase, "");
            if (phase.Length == 0 || phase == "OPEN") return;
            if (type == LogType.Log) return;
            if (type == LogType.Warning)
            {
                SessionState.SetInt(c_Warnings, SessionState.GetInt(c_Warnings, 0) + 1);
                return;
            }
            SessionState.SetInt(c_Errors, SessionState.GetInt(c_Errors, 0) + 1);
            AddUnique(Normalize(condition));
        }

        static void AddError(string msg)
        {
            SessionState.SetInt(c_Errors, SessionState.GetInt(c_Errors, 0) + 1);
            AddUnique(Normalize(msg));
        }

        static void AddUnique(string norm)
        {
            if (norm.Length == 0) return;
            string msgs = SessionState.GetString(c_ErrMsgs, "");
            if (msgs.Length > 6000) return;
            if (Array.IndexOf(msgs.Split('\n'), norm) >= 0) return;
            SessionState.SetString(c_ErrMsgs, msgs.Length == 0 ? norm : msgs + "\n" + norm);
        }

        static string Normalize(string msg)
        {
            if (msg == null) return "";
            int nl = msg.IndexOf('\n');
            if (nl >= 0) msg = msg.Substring(0, nl);
            msg = Regex.Replace(msg, @"\d+", "#").Replace('\t', ' ').Trim();
            return msg.Length > 200 ? msg.Substring(0, 200) : msg;
        }

        static string[] Queue()
        {
            string q = SessionState.GetString(c_Queue, "");
            return q.Length == 0 ? new string[0] : q.Split('\n');
        }

        static void AppendOut(string line)
        {
            string path = SessionState.GetString(c_Out, "");
            if (path.Length == 0) return;
            File.AppendAllText(path, line + "\n", new UTF8Encoding(false));
        }

        static double ParseD(string s)
        {
            double d;
            return double.TryParse(s, System.Globalization.NumberStyles.Float, RushCommon.Inv, out d) ? d : 0;
        }

        // ---------------------------------------------------------------- compare / summary

        sealed class SmokeFile
        {
            public readonly Dictionary<string, Dictionary<string, string>> Scenes =
                new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);
            public readonly HashSet<string> Errors = new HashSet<string>(StringComparer.Ordinal);
        }

        static SmokeFile Parse(IEnumerable<string> lines)
        {
            var f = new SmokeFile();
            foreach (string line in lines)
            {
                string[] c = line.Split('\t');
                if (c[0] == "SCENE" && c.Length >= 4)
                {
                    var d = new Dictionary<string, string>(StringComparer.Ordinal);
                    d["status"] = c[2];
                    foreach (string kv in c[3].Split(';'))
                    {
                        int eq = kv.IndexOf('=');
                        if (eq > 0) d[kv.Substring(0, eq)] = kv.Substring(eq + 1);
                    }
                    f.Scenes[c[1]] = d;
                }
                else if (c[0] == "ERR" && c.Length >= 3)
                {
                    f.Errors.Add(c[1] + " | " + c[2]);
                }
            }
            return f;
        }

        public static List<string> CompareLines(IEnumerable<string> baseline, IEnumerable<string> current)
        {
            SmokeFile b = Parse(baseline);
            SmokeFile c = Parse(current);
            var reg = new List<string>();
            foreach (KeyValuePair<string, Dictionary<string, string>> kv in b.Scenes)
            {
                Dictionary<string, string> cur;
                if (!c.Scenes.TryGetValue(kv.Key, out cur)) { reg.Add("MISSING SCENE " + kv.Key); continue; }
                if (kv.Value["status"] != cur["status"])
                    reg.Add("STATUS " + kv.Key + ": " + kv.Value["status"] + " -> " + cur["status"]);
                foreach (string field in s_CompareFields)
                {
                    string bv, cv;
                    kv.Value.TryGetValue(field, out bv);
                    cur.TryGetValue(field, out cv);
                    if (bv != cv) reg.Add("FIELD " + kv.Key + " " + field + ": " + bv + " -> " + cv);
                }
            }
            foreach (string e in c.Errors)
            {
                if (!b.Errors.Contains(e)) reg.Add("NEW ERROR " + e);
            }
            return reg;
        }

        public static string CompareToBaseline()
        {
            string b = RushCommon.FileIn(c_BaselineFile);
            string c = RushCommon.FileIn(c_CurrentFile);
            if (!File.Exists(b)) return "NO BASELINE";
            if (!File.Exists(c)) return "NO CURRENT";
            List<string> reg = CompareLines(File.ReadAllLines(b), File.ReadAllLines(c));
            File.WriteAllText(RushCommon.FileIn(c_DiffFile), string.Join("\n", reg.ToArray()) + "\n", new UTF8Encoding(false));
            return "SMOKE " + (reg.Count == 0 ? "PASS" : "FAIL") + " (regressions=" + reg.Count + ")\n" +
                   string.Join("\n", reg.Take(15).ToArray());
        }

        public static string Summarize(string outFileName)
        {
            string path = RushCommon.FileIn(outFileName);
            if (!File.Exists(path)) return "no file";
            SmokeFile f = Parse(File.ReadAllLines(path));
            var sb = new StringBuilder();
            int ok = f.Scenes.Count(kv => kv.Value["status"] == "OK");
            sb.AppendLine("scenes=" + f.Scenes.Count + " statusOK=" + ok + " statusFail=" + (f.Scenes.Count - ok));
            sb.AppendLine("withGameManager=" + f.Scenes.Count(kv => Get(kv.Value, "gm") == "True") +
                          " withoutGameManager=" + f.Scenes.Count(kv => Get(kv.Value, "gm") != "True"));
            sb.AppendLine("scenesWithErrors=" + f.Scenes.Count(kv => Get(kv.Value, "errors") != "0") +
                          " uniqueErrorMessages=" + f.Errors.Select(e => e.Substring(e.IndexOf('|') + 1)).Distinct().Count());
            sb.AppendLine("scenesWithMissingScripts=" + f.Scenes.Count(kv => Get(kv.Value, "missing") != "0"));
            sb.AppendLine("l2 skipped last-quest submit=" + f.Scenes.Count(kv => Get(kv.Value, "l2").Contains("skipped(last-quest")));
            return sb.ToString();
        }

        static string Get(Dictionary<string, string> d, string key)
        {
            string v;
            return d.TryGetValue(key, out v) ? v : "";
        }

        public static string SelfTest()
        {
            var sb = new StringBuilder();
            bool ok = true;
            var b = new List<string>
            {
                "SCENE\tA.unity\tOK\tgm=True;quests=3;active=1;missing=0;errors=0;warnings=2;l2=Submit:ok;sec=9",
                "SCENE\tB.unity\tOK\tgm=True;quests=2;active=1;missing=0;errors=1;warnings=0;l2=Submit:ok;sec=9",
                "ERR\tB.unity\tKnown problem #"
            };
            ok &= Expect(sb, "identical => no regressions", CompareLines(b, b).Count == 0);

            var warn = new List<string>(b);
            warn[0] = warn[0].Replace("warnings=2", "warnings=9").Replace("sec=9", "sec=30");
            ok &= Expect(sb, "warnings/time change ignored", CompareLines(b, warn).Count == 0);

            var quests = new List<string>(b);
            quests[0] = quests[0].Replace("quests=3", "quests=2");
            ok &= Expect(sb, "quest count change => regression", CompareLines(b, quests).Count == 1);

            var l2 = new List<string>(b);
            l2[1] = l2[1].Replace("Submit:ok", "Submit:EXC(NullReferenceException)");
            ok &= Expect(sb, "L2 exception => regression", CompareLines(b, l2).Count == 1);

            var newErr = new List<string>(b);
            newErr.Add("ERR\tA.unity\tBrand new failure");
            ok &= Expect(sb, "new error => regression", CompareLines(b, newErr).Count == 1);

            var gone = new List<string> { b[0] };
            ok &= Expect(sb, "scene missing => regression", CompareLines(b, gone).Count == 1);

            var fixedErr = new List<string> { b[0], b[1].Replace("errors=1", "errors=0") };
            ok &= Expect(sb, "error disappears => no regression", CompareLines(b, fixedErr).Count == 0);

            ok &= Expect(sb, "normalizer masks digits", Normalize("Index 12 out of range 40\nstack") == "Index # out of range #");

            sb.Insert(0, ok ? "SMOKE SELFTEST PASS\n" : "SMOKE SELFTEST FAIL\n");
            return sb.ToString();
        }

        static bool Expect(StringBuilder sb, string what, bool cond)
        {
            sb.AppendLine((cond ? "  ok   " : "  FAIL ") + what);
            return cond;
        }
    }
}
