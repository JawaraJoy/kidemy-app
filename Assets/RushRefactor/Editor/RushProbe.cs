using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace RushRefactor
{
    /// <summary>
    /// Tahap 5: probe perilaku untuk jalur yang TIDAK disentuh smoke test (drag, SFX zona, rotate ShapeIt, klik AudioPlayer,
    /// ducking musik, warna tema, tutorial, agent suara, panel hasil). Dijalankan di Play Mode pada sekumpulan scene wakil.
    /// Semua akses lewat refleksi supaya tetap bisa dijalankan setelah anggota kelas game berubah.
    /// Baseline probe BARU (probe_baseline.txt) ditangkap pada kode LAMA sebelum Tahap 5 mengubah apa pun.
    /// </summary>
    [InitializeOnLoad]
    public static class RushProbe
    {
        const string c_Active = "RushProbe.Active";
        const string c_Phase = "RushProbe.Phase";
        const string c_Index = "RushProbe.Index";
        const string c_Queue = "RushProbe.Queue";
        const string c_Out = "RushProbe.Out";
        const string c_Original = "RushProbe.Original";
        const string c_PrevMute = "RushProbe.PrevMute";
        const string c_Since = "RushProbe.Since";
        const string c_Step = "RushProbe.Step";
        const string c_Info = "RushProbe.Info";
        const string c_Status = "RushProbe.Status";
        const string c_Errors = "RushProbe.Errors";
        const string c_ErrMsgs = "RushProbe.ErrMsgs";
        const string c_Started = "RushProbe.Started";
        const string c_RunStarted = "RushProbe.RunStarted";

        const string c_BaselineFile = "probe_baseline.txt";
        const string c_CurrentFile = "probe_current.txt";
        const string c_DiffFile = "probe_diff.txt";

        const double c_EnterTimeout = 90.0;
        const double c_StopTimeout = 90.0;

        // detik menunggu SEBELUM langkah ke-i (langkah 0 = setelah masuk Play Mode)
        static readonly double[] s_GapBefore = { 3.0, 0.4, 2.4, 4.0 };

        static readonly string[] s_SceneNames =
        {
            "Dev_Frontend", "Dev_001_FrogsCount_A_rev", "Dev_005_SoundQuest_A_rev", "Dev_006_LetterHunt_A_rev",
            "Dev_007_WordBuilder_A_rev", "Dev_009_ColortheIsland_A_rev", "Dev_010_ShapeIt_A_rev", "Dev_010_ShapeIt_A_rewrite", "Dev_010_ShapeIt_B", "Dev_010_ShapeIt_C",
            "Dev_011_TuneTheMusic_A_rev", "Dev_012_RythimMaker_A_rev", "Dev_012_RythimMaker_A_rewrite",
            "Dev_017_DailyRoutine_A_rev", "Dev_018_DailySchedule_A_rev", "Dev_020_StepSequence_A_rev",
            "OtherDev_Sound Quest_A_001", "OtherDev_HowManyFrog_A_001", "OtherDev_BigOrSmall_A_001", "OtherDev_Find the friend_A_001"
        };

        static RushProbe()
        {
            Application.logMessageReceived -= OnLog;
            Application.logMessageReceived += OnLog;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }

        // ---------------------------------------------------------------- menu / API

        [MenuItem("Tools/Rush Refactor/Probe/Run (baseline)")]
        static void MenuBaseline() { Debug.Log("[Rush] " + BeginBaseline()); }

        [MenuItem("Tools/Rush Refactor/Probe/Run (current) And Compare")]
        static void MenuCurrent() { Debug.Log("[Rush] " + BeginCurrent()); }

        [MenuItem("Tools/Rush Refactor/Probe/Status")]
        static void MenuStatus() { Debug.Log("[Rush] " + Status()); }

        [MenuItem("Tools/Rush Refactor/Probe/Abort")]
        static void MenuAbort() { Debug.Log("[Rush] " + Abort()); }

        public static string BeginBaseline() { return Begin(c_BaselineFile, Scenes()); }

        public static string BeginCurrent() { return Begin(c_CurrentFile, Scenes()); }

        public static string[] Scenes()
        {
            var list = new List<string>();
            foreach (string name in s_SceneNames)
            {
                string found = null;
                foreach (string g in AssetDatabase.FindAssets("t:Scene " + name, RushCommon.AssetRoots))
                {
                    string p = AssetDatabase.GUIDToAssetPath(g);
                    if (RushCommon.IsProjectPath(p) && Path.GetFileNameWithoutExtension(p) == name) { found = p; break; }
                }
                if (found != null) list.Add(found);
            }
            return list.ToArray();
        }

        public static string Begin(string outFileName, string[] scenePaths)
        {
            if (SessionState.GetString(c_Active, "") == "1") return "ALREADY RUNNING: " + Status();
            if (EditorApplication.isPlayingOrWillChangePlaymode) return "ABORT: editor is in play mode";
            if (EditorApplication.isCompiling) return "ABORT: editor is compiling";
            if (PrefabStageUtility.GetCurrentPrefabStage() != null) return "ABORT: a prefab stage is open, close it first";
            if (scenePaths == null || scenePaths.Length == 0) return "ABORT: no scenes given";
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene s = SceneManager.GetSceneAt(i);
                if (s.isDirty || string.IsNullOrEmpty(s.path)) return "ABORT: scene '" + s.name + "' is unsaved or modified";
            }

            string outPath = RushCommon.FileIn(outFileName);
            File.WriteAllText(outPath, "", new UTF8Encoding(false));
            SessionState.SetString(c_Out, outPath);
            SessionState.SetString(c_Queue, string.Join("\n", scenePaths));
            SessionState.SetInt(c_Index, 0);
            SessionState.SetString(c_Original, SceneManager.GetActiveScene().path);
            SessionState.SetBool(c_PrevMute, EditorUtility.audioMasterMute);
            SessionState.SetString(c_RunStarted, EditorApplication.timeSinceStartup.ToString("R", RushCommon.Inv));
            EditorUtility.audioMasterMute = true;
            SessionState.SetString(c_Phase, "OPEN");
            SessionState.SetString(c_Active, "1");
            return "STARTED: " + scenePaths.Length + " scenes -> " + outPath;
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
            string outPath = SessionState.GetString(c_Out, "");
            if (outPath.Length > 0 && File.Exists(outPath))
            {
                string[] lines = File.ReadAllLines(outPath);
                sb.AppendLine("recorded=" + lines.Count(l => l.StartsWith("SCENE\t", StringComparison.Ordinal)) + " scenes, file=" + outPath);
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

            SessionState.SetInt(c_Errors, 0);
            SessionState.SetString(c_ErrMsgs, "");
            SessionState.SetString(c_Info, "");
            SessionState.SetInt(c_Step, 0);
            SessionState.SetString(c_Status, "OK");
            SessionState.SetString(c_Started, now.ToString("R", RushCommon.Inv));

            if (!OpenOnly(queue[idx]))
            {
                SetStatus("FAIL:OPEN");
                Advance(now);
                return;
            }
            SessionState.SetString(c_Phase, "ENTER");
            SessionState.SetString(c_Since, now.ToString("R", RushCommon.Inv));
            EditorApplication.isPlaying = true;
        }

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
            int step = SessionState.GetInt(c_Step, 0);
            double since = ParseD(SessionState.GetString(c_Since, "0"));
            if (now - since < s_GapBefore[step]) return;

            RunStep(step);
            step++;
            SessionState.SetInt(c_Step, step);
            SessionState.SetString(c_Since, now.ToString("R", RushCommon.Inv));
            if (step >= s_GapBefore.Length) StopPlay(now);
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
            string info = SessionState.GetString(c_Info, "");
            if (info.Length == 0) info = "none=1";
            string line = "SCENE\t" + path + "\t" + SessionState.GetString(c_Status, "OK") + "\t" + info + "errors=" + SessionState.GetInt(c_Errors, 0);
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

        // ---------------------------------------------------------------- probe actions

        static readonly string[] s_Empty = new string[0];

        static void RunStep(int step)
        {
            object gm = GetGm();
            if (step == 0) Step0(gm);
            else if (step == 1) Step1(gm);
            else if (step == 2) Step2(gm);
            else Step3(gm);
        }

        static void Step0(object gm)
        {
            Add("gm", gm != null);
            if (gm == null) return;

            Add("theme", ColorText(Member(gm, "ColorTheme")));
            Add("npc", NpcName(gm));
            Add("dialog", DialogText(gm));
            Add("quest", FirstActiveName("Quest"));
            Add("shadow", ShadowSummary());
            Add("x_anim", AnimSummary(gm));

            AudioSource sfx = Member(gm, "AudioSource") as AudioSource;
            AudioSource music = Member(gm, "Music") as AudioSource;
            Component quest = ActiveOf("Quest").FirstOrDefault();

            // ducking musik lewat jalur suara pertanyaan
            if (quest != null && sfx != null)
            {
                sfx.Stop();
                if (music != null) music.volume = 1f;
                AudioClip clip = AudioClip.Create("probe", 4410, 1, 44100, false);
                string e = InvokeWith(quest, "PlayQuestionVoice", new Type[] { typeof(AudioClip) }, clip);
                Add("duck", e + ":" + (music != null ? music.volume.ToString("F2", RushCommon.Inv) : "-") + ":" + sfx.isPlaying);
                if (music != null) music.volume = 1f;
                sfx.Stop();
            }

            // klik AudioPlayer
            if (sfx != null)
            {
                sfx.Stop();
                List<Component> aps = ActiveOf("AudioPlayer").ToList();
                var ex = new SortedSet<string>(StringComparer.Ordinal);
                foreach (Component ap in aps) ex.Add(Invoke(ap, "OnClick"));
                Add("ap", aps.Count + ":" + string.Join("/", ex.ToArray()) + ":" + sfx.isPlaying);
                sfx.Stop();
            }

            // suara terima / tolak pada zona drag
            if (sfx != null)
            {
                List<Component> zones = ActiveOf("QuestDragNDropZone").ToList();
                int acc = 0, rej = 0;
                var ex = new SortedSet<string>(StringComparer.Ordinal);
                foreach (Component z in zones)
                {
                    sfx.Stop();
                    ex.Add(Invoke(z, "OnAccept"));
                    if (sfx.isPlaying) acc++;
                    sfx.Stop();
                    ex.Add(Invoke(z, "OnReject"));
                    if (sfx.isPlaying) rej++;
                    sfx.Stop();
                }
                Add("zone", zones.Count + ":" + acc + ":" + rej + ":" + string.Join("/", ex.ToArray()));
            }

            // rotate ShapeIt
            Component shape = ActiveOf("QuestShapeIt").FirstOrDefault();
            if (shape != null && sfx != null)
            {
                sfx.Stop();
                string e = Invoke(shape, "Rotate");
                Add("rot", e + ":" + sfx.isPlaying);
                sfx.Stop();
            }

            // drag satu item: reparent ke canvas + skala
            Component item = ActiveOf("QuestDragNDropItem").FirstOrDefault();
            if (item != null)
            {
                var ped = new PointerEventData(EventSystem.current);
                ped.delta = new Vector2(10f, 5f);
                RectTransform rt = item.GetComponent<RectTransform>();
                Vector2 before = rt != null ? rt.anchoredPosition : Vector2.zero;
                string e1 = Invoke(item, "OnBeginDrag", ped);
                Canvas canvas = Member(gm, "Canvas") as Canvas;
                bool underCanvas = canvas != null && item.transform.parent == canvas.transform;
                string e2 = Invoke(item, "OnDrag", ped);
                Vector2 d = rt != null ? rt.anchoredPosition - before : Vector2.zero;
                Add("drag", e1 + "+" + e2 + ":" + underCanvas + ":" + d.x.ToString("F3", RushCommon.Inv) + "," + d.y.ToString("F3", RushCommon.Inv));
            }

            // tutorial tangan
            {
                Canvas canvas = Member(gm, "Canvas") as Canvas;
                int c0 = canvas != null ? canvas.transform.childCount : -1;
                string e = quest != null ? InvokeWith(quest, "ShowTutorial", s_NoTypes) : "-";
                Add("tut", e + ":" + (canvas != null ? canvas.transform.childCount - c0 : 0));
            }

            // agent suara (UnityEvent -> GameManager)
            {
                List<Component> agents = ActiveOf("GameManagerAgent").ToList();
                var ex = new SortedSet<string>(StringComparer.Ordinal);
                foreach (Component a in agents) { ex.Add(Invoke(a, "PlayQuestionVoice")); ex.Add(Invoke(a, "PlaySoundQuest")); }
                Add("agent", agents.Count + ":" + string.Join("/", ex.ToArray()));
            }

            // ketukan ritme (suara keluar setelah 0.1 detik, diperiksa di langkah 1)
            {
                Component beat = ActiveOf("QuestRythimBeat").FirstOrDefault();
                if (beat != null && sfx != null)
                {
                    sfx.Stop();
                    string e = Invoke(beat, "Onclick");
                    Add("beat", e);
                }
            }

            // panel hasil + bintang (selesai ~1.6 detik, diperiksa di langkah 2)
            {
                object rp = Member(gm, "ResultPanel");
                UnityEngine.Object rpo = rp as UnityEngine.Object;
                Type tCtx = Ty("ResultContext");
                if (rpo != null && tCtx != null)
                {
                    Invoke(gm, "SetRightAsnwer", 2);
                    object ctx = Activator.CreateInstance(tCtx, new object[] { 1, 2 });
                    Add("result", Invoke(rp, "ShowResult", ctx));
                }
            }
        }

        static readonly Type[] s_NoTypes = new Type[0];

        static void Step1(object gm)
        {
            if (gm == null) return;
            AudioSource sfx = Member(gm, "AudioSource") as AudioSource;
            if (SessionInfoHas("beat=") && sfx != null) Add("beatPlay", sfx.isPlaying);
            Add("x_orph", OrphanSummary());
            Add("x_beatSrc", BeatSrc(gm));
        }

        static void Step2(object gm)
        {
            if (gm == null) return;
            Add("x_anim2", AnimSummary(gm));
            if (SessionInfoHas("result="))
            {
                Add("right", Convert.ToString(Member(gm, "RightAnswerCount"), RushCommon.Inv));
                object rp = Member(gm, "ResultPanel");
                if (rp != null)
                {
                    object label = Member(rp, "m_LabelText");
                    Add("label", Clip(Convert.ToString(Member(label, "text"), RushCommon.Inv), 40));
                    UnityEngine.UI.Image icon = Member(rp, "m_NPCIcon") as UnityEngine.UI.Image;
                    Add("npcIcon", icon != null && icon.sprite != null ? icon.sprite.name : "-");
                }
            }
        }

        static void Step3(object gm)
        {
            if (gm == null) return;
            Add("x_anim3", AnimSummary(gm));
            if (SessionInfoHas("result="))
            {
                Add("x_right2", Convert.ToString(Member(gm, "RightAnswerCount"), RushCommon.Inv));
                // jalankan iterator StarPresenter.SetActiveStars secara sinkron (menjalankan baris reset jumlah benar)
                object sp = Member(Member(gm, "ResultPanel"), "m_StarPresenter");
                string e = "-";
                if (sp != null)
                {
                    MethodInfo mi = sp.GetType().GetMethod("SetActiveStars", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                    if (mi != null)
                    {
                        Invoke(gm, "SetRightAsnwer", 2);
                        try
                        {
                            var it = (System.Collections.IEnumerator)mi.Invoke(sp, new object[] { true });
                            int guard = 0;
                            while (it.MoveNext() && guard++ < 50) { }
                            e = "ok";
                        }
                        catch (Exception ex) { e = "EXC(" + (ex is TargetInvocationException && ex.InnerException != null ? ex.InnerException.GetType().Name : ex.GetType().Name) + ")"; }
                    }
                }
                Add("x_drive", e + ":" + Convert.ToString(Member(gm, "RightAnswerCount"), RushCommon.Inv));
            }
        }

        // ---------------------------------------------------------------- reflection helpers

        static Type Ty(string name) { return RushCommon.FindType("EduGame." + name); }

        static string AnimSummary(object gm)
        {
            List<Component> hs = ActiveOf("AnimationHandler").ToList();
            AudioSource music = Member(gm, "Music") as AudioSource;
            return hs.Count + ":" + (music != null ? music.volume.ToString("F2", RushCommon.Inv) : "-");
        }

        static object GetGm()
        {
            Type t = Ty("GameManager");
            if (t == null) return null;
            PropertyInfo p = t.GetProperty("Instance", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (p == null) return null;
            object o = p.GetValue(null, null);
            UnityEngine.Object uo = o as UnityEngine.Object;
            return uo == null ? null : o;
        }

        static string PathOf(UnityEngine.Object o)
        {
            Component c = o as Component;
            if (c == null) return o.name;
            var parts = new List<string>();
            Transform t = c.transform;
            while (t != null) { parts.Insert(0, t.name); t = t.parent; }
            return string.Join("/", parts.ToArray());
        }

        static IEnumerable<Component> AllOf(string typeName)
        {
            Type t = Ty(typeName);
            if (t == null) return new Component[0];
            UnityEngine.Object[] arr = UnityEngine.Object.FindObjectsByType(t, FindObjectsInactive.Include, FindObjectsSortMode.None);
            return arr.OfType<Component>().OrderBy(c => PathOf(c), StringComparer.Ordinal).ToList();
        }

        static IEnumerable<Component> ActiveOf(string typeName)
        {
            return AllOf(typeName).Where(c => c != null && c.gameObject.activeInHierarchy);
        }

        static string FirstActiveName(string typeName)
        {
            Component c = ActiveOf(typeName).FirstOrDefault();
            return c == null ? "-" : c.GetType().Name;
        }

        // properti atau field (publik maupun privat) pada tipe itu atau induknya
        static object Member(object o, string name)
        {
            if (o == null) return null;
            const BindingFlags f = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
            for (Type t = o.GetType(); t != null; t = t.BaseType)
            {
                PropertyInfo p = t.GetProperty(name, f);
                if (p != null && p.GetIndexParameters().Length == 0)
                {
                    try { return p.GetValue(o, null); } catch (Exception) { return null; }
                }
                FieldInfo fi = t.GetField(name, f);
                if (fi != null)
                {
                    try { return fi.GetValue(o); } catch (Exception) { return null; }
                }
            }
            return null;
        }

        static string Invoke(object o, string method, params object[] args)
        {
            if (o == null) return "NULL";
            const BindingFlags f = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
            for (Type t = o.GetType(); t != null; t = t.BaseType)
            {
                foreach (MethodInfo m in t.GetMethods(f))
                {
                    if (m.Name != method) continue;
                    ParameterInfo[] ps = m.GetParameters();
                    if (ps.Length != args.Length) continue;
                    return Call(m, o, args);
                }
            }
            return "NOMETHOD";
        }

        static string InvokeWith(object o, string method, Type[] types, params object[] args)
        {
            if (o == null) return "NULL";
            const BindingFlags f = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
            for (Type t = o.GetType(); t != null; t = t.BaseType)
            {
                MethodInfo m = t.GetMethod(method, f, null, types, null);
                if (m != null) return Call(m, o, args);
            }
            return "NOMETHOD";
        }

        static string Call(MethodInfo m, object o, object[] args)
        {
            try { m.Invoke(o, args); return "ok"; }
            catch (TargetInvocationException e) { return "EXC(" + (e.InnerException ?? e).GetType().Name + ")"; }
            catch (Exception e) { return "EXC(" + e.GetType().Name + ")"; }
        }

        // audit hierarki (tidak bergantung kode): komponen aktif yang TIDAK punya Quest induk (yatim)
        static string OrphanSummary()
        {
            string[][] kinds =
            {
                new[] { "ap", "AudioPlayer", "Quest" }, new[] { "qc", "QuestContainer", "Quest" }, new[] { "it", "QuestDragNDropItem", "Quest" },
                new[] { "zn", "QuestDragNDropZone", "QuestZonePairing" }, new[] { "bt", "QuestRythimBeat", "Quest" },
                new[] { "an", "AnimationHandler", "Quest" }, new[] { "ag", "GameManagerAgent", "Quest" }
            };
            var parts = new List<string>();
            foreach (string[] k in kinds)
            {
                Type anc = Ty(k[2]);
                int total = 0, orphan = 0;
                foreach (Component c in ActiveOf(k[1]))
                {
                    total++;
                    if (anc == null || c.GetComponentInParent(anc, true) == null) orphan++;
                }
                parts.Add(k[0] + ":" + total + "/" + orphan);
            }
            return string.Join(",", parts.ToArray());
        }

        static string BeatSrc(object gm)
        {
            Component beat = ActiveOf("QuestRythimBeat").FirstOrDefault();
            if (beat == null) return "-";
            AudioSource src = Member(beat, "m_AudioSource") as AudioSource;
            AudioSource gms = Member(gm, "AudioSource") as AudioSource;
            return src == null ? "null" : (src == gms ? "gm" : "other");
        }

        static string ColorText(object c)
        {
            if (!(c is Color)) return "-";
            Color col = (Color)c;
            return col.r.ToString("F3", RushCommon.Inv) + "," + col.g.ToString("F3", RushCommon.Inv) + "," +
                   col.b.ToString("F3", RushCommon.Inv) + "," + col.a.ToString("F3", RushCommon.Inv);
        }

        static string NpcName(object gm)
        {
            RectTransform npc = Member(gm, "m_Npc") as RectTransform;
            if (npc == null) return "-";
            Animator anim;
            if (!npc.TryGetComponent<Animator>(out anim) || anim.runtimeAnimatorController == null) return "-";
            return anim.runtimeAnimatorController.name;
        }

        static string DialogText(object gm)
        {
            object dlg = Member(gm, "m_NpcDialog");
            if (dlg == null) return "-";
            return Clip(Convert.ToString(Member(dlg, "text"), RushCommon.Inv), 40);
        }

        static string ShadowSummary()
        {
            var colors = new List<string>();
            foreach (Component c in AllOf("QuestContainer"))
            {
                Transform sh = c.transform.Find("Shadow");
                if (sh == null) continue;
                UnityEngine.UI.Image img;
                if (!sh.TryGetComponent<UnityEngine.UI.Image>(out img)) continue;
                colors.Add(ColorText(img.color));
            }
            colors.Sort(StringComparer.Ordinal);
            int distinct = colors.Distinct().Count();
            return colors.Count + ":" + distinct + ":" + Hash8(string.Join("|", colors.ToArray()));
        }

        static string Hash8(string s)
        {
            using (var md5 = MD5.Create())
            {
                byte[] h = md5.ComputeHash(Encoding.UTF8.GetBytes(s));
                var sb = new StringBuilder();
                for (int i = 0; i < 4; i++) sb.Append(h[i].ToString("x2"));
                return sb.ToString();
            }
        }

        static string Clip(string s, int max)
        {
            if (s == null) return "";
            s = s.Replace('\n', ' ').Replace('\r', ' ').Replace('\t', ' ');
            return s.Length > max ? s.Substring(0, max) : s;
        }

        static bool SessionInfoHas(string key) { return SessionState.GetString(c_Info, "").Contains(key); }

        static void Add(string key, object value)
        {
            string v = Convert.ToString(value, RushCommon.Inv) ?? "";
            v = v.Replace(';', '_').Replace('=', '_').Replace('\t', ' ').Replace('\n', ' ').Replace('\r', ' ');
            SessionState.SetString(c_Info, SessionState.GetString(c_Info, "") + key + "=" + v + ";");
        }

        // ---------------------------------------------------------------- log capture

        static void OnLog(string condition, string stackTrace, LogType type)
        {
            if (SessionState.GetString(c_Active, "") != "1") return;
            string phase = SessionState.GetString(c_Phase, "");
            if (phase.Length == 0 || phase == "OPEN") return;
            if (type == LogType.Log || type == LogType.Warning) return;
            SessionState.SetInt(c_Errors, SessionState.GetInt(c_Errors, 0) + 1);
            AddUnique(Normalize(condition));
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
            return q.Length == 0 ? s_Empty : q.Split('\n');
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

        // ---------------------------------------------------------------- compare

        sealed class ProbeFile
        {
            public readonly Dictionary<string, Dictionary<string, string>> Scenes = new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);
            public readonly HashSet<string> Errors = new HashSet<string>(StringComparer.Ordinal);
        }

        static ProbeFile Parse(IEnumerable<string> lines)
        {
            var f = new ProbeFile();
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
                else if (c[0] == "ERR" && c.Length >= 3) f.Errors.Add(c[1] + " | " + c[2]);
            }
            return f;
        }

        public static List<string> CompareLines(IEnumerable<string> baseline, IEnumerable<string> current)
        {
            ProbeFile b = Parse(baseline);
            ProbeFile c = Parse(current);
            var reg = new List<string>();
            foreach (KeyValuePair<string, Dictionary<string, string>> kv in b.Scenes)
            {
                Dictionary<string, string> cur;
                if (!c.Scenes.TryGetValue(kv.Key, out cur)) { reg.Add("MISSING SCENE " + kv.Key); continue; }
                foreach (string key in kv.Value.Keys.Union(cur.Keys).OrderBy(k => k, StringComparer.Ordinal))
                {
                    string bv, cv;
                    kv.Value.TryGetValue(key, out bv);
                    cur.TryGetValue(key, out cv);
                    if (bv != cv && !(bv == null && key.StartsWith("x_", StringComparison.Ordinal))) reg.Add("FIELD " + kv.Key + " " + key + ": " + bv + " -> " + cv);
                }
            }
            foreach (string e in c.Errors) if (!b.Errors.Contains(e)) reg.Add("NEW ERROR " + e);
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
            return "PROBE " + (reg.Count == 0 ? "PASS" : "FAIL") + " (regressions=" + reg.Count + ")\n" + string.Join("\n", reg.Take(15).ToArray());
        }
    }
}
