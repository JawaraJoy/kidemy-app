using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace EduGame
{
    public class GameManager : MonoBehaviour
    {
        // Import the JavaScript functions
        [DllImport("__Internal")]
        private static extern string GetParentURL();

        [DllImport("__Internal")]
        private static extern string GetSelfURL();

        [DllImport("__Internal")]
        private static extern void RedirectParentWindow(string url);

        [DllImport("__Internal")]
        private static extern void LogToBrowser(string message);

        [Header("Frame")]
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text category;
        [SerializeField] private TMP_Text timer;
        [SerializeField] private Image background;
        [SerializeField] private RectTransform npc;
        [SerializeField] private TMP_Text npcDialog;

        [Header("Result Pop")]
        [SerializeField] private RectTransform popResult;
        [SerializeField] private RectTransform correctTitle;
        [SerializeField] private RectTransform wrongTitle;
        [SerializeField] private RectTransform correctNote;
        [SerializeField] private RectTransform wrongNote;
        [SerializeField] private RectTransform starContainer;
        [SerializeField] private Image starPrefab;

        [Header("Button")]
        [SerializeField] private Button buttonClue;
        [SerializeField] private Button buttonBack;
        [SerializeField] private Button buttonNext;
        [SerializeField] private Button buttonHome;
        [SerializeField] private Button buttonRestart;
        [SerializeField] private Button buttonCharacter;

        [Header("Feedback")]
        [SerializeField] private Feedback correctFeedback;
        [SerializeField] private Feedback wrongFeedback;
        [SerializeField] private Feedback idleFeedback;
        [SerializeField] private Feedback thinkFeedback;

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;

        [Header("Data")]
        [SerializeField] private Quest questPrefab;
        [SerializeField] private Quest[] questPrefabs;

        private Image[] stars;
        private Quest[] quests;
        private int currentIndex = 0;
        private float recordedTime;
        private float startTime;
        private IEnumerator timerCo;
        private string baseURL;
        private string frameURL;
        private string sessionId;
        private string islandId;
        private ResultItem[] result;
        private APIManager apiManager;
        private AssetManager assetManager;
        private string APIInfoURL = "https://stg-be.kimee.io/api/v1/external/games";
        private string APIResultURL = "https://stg-be.kimee.io/api/v1/external/games/progress";
        private string gameURL = "https://stg.kimee.io/en/island-map/games?islandId=[ISLAND_ID]";

        public Canvas Canvas { get; private set; }
        public AudioSource AudioSource => audioSource;
        public float ResultTime => recordedTime;
        public Color ColorTheme { get; private set; }
        public APIManager API => apiManager;
        public AssetManager Asset => assetManager;

        public static GameManager Instance { get; private set; }

        void Awake()
        {
            Canvas = GetComponentInParent<Canvas>();

            if (!Instance)
                Instance = this;

            if (!Canvas)
                Debug.LogError("Canvas is not found");

            if (starPrefab)
                InstantiateStar();
        }

        void Start()
        {
            apiManager = GetComponent<APIManager>();
            assetManager = GetComponent<AssetManager>();

#if !UNITY_EDITOR && UNITY_WEBGL
                baseURL = GetParentURL();
                frameURL = GetSelfURL();
#else
            baseURL = "";
            frameURL = "";
#endif

#if !UNITY_EDITOR && UNITY_WEBGL
                LogToBrowser("Base URL: " + baseURL);
                LogToBrowser("Frame URL: " + frameURL);
#endif

#if !UNITY_EDITOR && UNITY_WEBGL
                if(!string.IsNullOrEmpty(frameURL))
                {
                    Dictionary<string, string> parameters = URIHelper.GetParameters(frameURL);
                    if(parameters.ContainsKey("redirect_url"))
                    {
                        baseURL = parameters["redirect_url"];
                    }
                }
                LogToBrowser("Home URL: " + baseURL);
#endif

            if (!string.IsNullOrEmpty(frameURL))
            {
                Dictionary<string, string> param = URIHelper.GetParameters(frameURL);

                if (param.ContainsKey("session_id"))
                    sessionId = param["session_id"];
                else
                    sessionId = "67e05cdd-c077-4d84-8c49-15767bf13ef2";
            }
            else
                sessionId = "67e05cdd-c077-4d84-8c49-15767bf13ef2";

            if (questPrefabs.Length > 0)
            {
                quests = new Quest[questPrefabs.Length];

                for (int i = 0; i < questPrefabs.Length; i++)
                {
                    quests[i] = InstantiateTemplate(questPrefabs[i]);
                    quests[i].gameObject.SetActive(false);
                }

            }
            else if (questPrefab)
            {
                quests = new Quest[1];

                quests[0] = InstantiateTemplate(questPrefab);
            }
            else
                Debug.LogError("Failed to load data, challenge will return empty");

            //quests[0].gameObject.SetActive(true);

            if (background)
                background.sprite = quests[0].Background;

            StartTimer();

            foreach (var quest in quests)
                quest.Setup();

            assetManager.DownloadMissingVoices();

            result = new ResultItem[quests.Length];
        }

        void StopTimer()
        {
            if (timerCo != null)
            {
                recordedTime = Time.time - startTime;

                StopCoroutine(timerCo);
                timerCo = null;
            }
        }

        void StartTimer()
        {
            if (timerCo == null)
            {
                startTime = Time.time;

                timerCo = TimerCo();
                StartCoroutine(timerCo);
            }
        }

        IEnumerator TimerCo()
        {
            if (timer)
            {
                while (true)
                {
                    float elapsedTime = Time.time - startTime;

                    float m = Mathf.FloorToInt(elapsedTime / 60);
                    float s = Mathf.FloorToInt(elapsedTime % 60);

                    string timerText = m.ToString().PadLeft(2, '0') + ":" + s.ToString().PadLeft(2, '0');

                    if (s % 10 == 0)
                        idleFeedback.Play(npc ? npc : transform);
                    else if (s % 5 == 0)
                        thinkFeedback.Play(npc ? npc : transform);

                    timer.text = timerText;

                    yield return new WaitForSeconds(1);
                }
            }

            yield return null;
        }

        public virtual void SetNPC(RuntimeAnimatorController controller)
        {
            Animator animator = npc.GetComponent<Animator>();

            if (animator)
                animator.runtimeAnimatorController = controller;
        }

        public virtual void SetNPCDialog(string dialog)
        {
            if (npcDialog)
                npcDialog.text = dialog;
        }
        
        Quest InstantiateTemplate(Quest templatePrefab)
        {
            Quest template = Instantiate(templatePrefab, transform);

            template.transform.localPosition = Vector3.zero;
            template.transform.localScale = Vector3.one;

            ColorTheme = template.Color;

            return template;
        }

        protected virtual void InstantiateStar()
        {
            stars = new Image[3];

            stars[0] = Instantiate(starPrefab, starContainer);
            stars[0].gameObject.SetActive(false);

            stars[1] = Instantiate(stars[0], starContainer);
            stars[1].gameObject.SetActive(false);

            stars[2] = Instantiate(stars[0], starContainer);
            stars[2].gameObject.SetActive(false);
        }

        public virtual void InitQuest(SO_Quest questData)
        {
            if (title != null && questData.Title != null)
                title.text = questData.Title;

            if (category != null)
                category.text = questData.Category.ToString().Replace('_', ' ');
        }

        public void PlayQuestionVoice()
        {
            quests[currentIndex].PlayQuestionVoice();
        }

        public void Play()
        {
            currentIndex = -1;
            Next();
        }

        void Next()
        {
            int nextIndex = currentIndex + 1;

            if (quests.Length > 0 && nextIndex < quests.Length)
            {
                if (currentIndex >= 0)
                    quests[currentIndex].gameObject.SetActive(false);

                quests[nextIndex].gameObject.SetActive(true);
                
                if (background)
                    background.sprite = quests[nextIndex].Background;

                currentIndex = nextIndex;

                Reset();
            }
        }

        public virtual void Submit(int star = 0)
        {
            StopTimer();

            if (star < 1)
                star = 1;
            else if (star > 3)
                star = 3;

            if (star == 3 && quests[currentIndex].Data.TresholdTime > 0 && recordedTime > quests[currentIndex].Data.TresholdTime)
                star = 2;

            if (currentIndex < result.Length)
                result[currentIndex] = new ResultItem { stars = star, time = Mathf.RoundToInt(recordedTime) };

            ShowResult(star);
        }

        public virtual void Reset()
        {
            CloseResult();
            StopTimer();
            StartTimer();

            PlayQuestionVoice();
        }

        public virtual void ResetQuest()
        {
            Reset();

            quests[currentIndex].Reset();
        }

        public virtual void ShowResult(int star)
        {
            if (popResult)
                popResult.gameObject.SetActive(true);

            if (star > 1)
            {
                if (correctFeedback)
                    correctFeedback.Play(npc ? npc : transform);

                if (npcDialog)
                    SetNPCDialog("Yaaay! Amazing!");

                correctTitle.gameObject.SetActive(true);
                correctNote.gameObject.SetActive(true);
            }
            else
            {
                if (wrongFeedback)
                    wrongFeedback.Play(npc ? npc : transform);

                if (npcDialog)
                    SetNPCDialog("Oh no!");

                wrongTitle.gameObject.SetActive(true);
                wrongNote.gameObject.SetActive(true);
            }

            if (stars != null && stars.Length > 0)
            {
                for (int i = 0; i < stars.Length; i++)
                {
                    stars[i].gameObject.SetActive(true);

                    if (i < star)
                        stars[i].color = Color.white;
                    else
                        stars[i].color = Color.black;
                }
            }

            if (currentIndex + 1 == quests.Length)
            {
                buttonNext.gameObject.SetActive(false);
                SendResult();
            }
            else
                buttonNext.gameObject.SetActive(true);

        }

        public virtual void GoHome()
        {
            if (!string.IsNullOrEmpty(baseURL))
            {
#if !UNITY_EDITOR && UNITY_WEBGL
                    RedirectParentWindow(baseURL);
#endif
            }
        }

        public virtual void SendResult()
        {
            Result compiledResult = new Result
            {
                session_id = sessionId,
                result = result
            };

            string json = JsonUtility.ToJson(compiledResult);
            Debug.Log(json);

            if (apiManager)
                apiManager.SendData(APIResultURL, json);
        }


        public virtual void CloseResult()
        {
            if (popResult)
                popResult.gameObject.SetActive(false);

            correctTitle.gameObject.SetActive(false);
            correctNote.gameObject.SetActive(false);
            wrongTitle.gameObject.SetActive(false);
            wrongNote.gameObject.SetActive(false);

            if (stars != null && stars.Length > 0)
            {
                for (int i = 0; i < stars.Length; i++)
                    stars[i].gameObject.SetActive(false);
            }
        }
    }
}