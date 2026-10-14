using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine.Events;
using EasyTextEffects;
using UnityEngine.Serialization;

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

        [DllImport("__Internal")]
        private static extern void JS_OnVoiceDownloadComplete();

        [DllImport("__Internal")]
        private static extern void ReloadIframe();

        [Header("Frame")]
        [SerializeField] [FormerlySerializedAs("title")] private TMP_Text m_Title;
        [SerializeField] [FormerlySerializedAs("category")] private TMP_Text m_Category;
        [SerializeField] [FormerlySerializedAs("timer")] private TMP_Text m_Timer;
        [SerializeField] [FormerlySerializedAs("background")] private Image m_Background;
        [SerializeField] [FormerlySerializedAs("backgroundMusic")] private AudioClip m_BackgroundMusic;
        [SerializeField] [FormerlySerializedAs("npc")] private RectTransform m_Npc;
        [SerializeField] [FormerlySerializedAs("npcDialog")] private TMP_Text m_NpcDialog;

        [Header("Result Pop")]
        [SerializeField] [FormerlySerializedAs("popResult")] private RectTransform m_PopResult;
        [SerializeField] [FormerlySerializedAs("correctTitle")] private RectTransform m_CorrectTitle;
        [SerializeField] [FormerlySerializedAs("wrongTitle")] private RectTransform m_WrongTitle;
        [SerializeField] [FormerlySerializedAs("correctNote")] private RectTransform m_CorrectNote;
        [SerializeField] [FormerlySerializedAs("wrongNote")] private RectTransform m_WrongNote;
        [SerializeField] [FormerlySerializedAs("starContainer")] private RectTransform m_StarContainer;
        [SerializeField] [FormerlySerializedAs("starPrefab")] private Image m_StarPrefab;

        [Header("Button")]
        [SerializeField] [FormerlySerializedAs("buttonClue")] private Button m_ButtonClue;
        [SerializeField] [FormerlySerializedAs("buttonBack")] private Button m_ButtonBack;
        [SerializeField] [FormerlySerializedAs("buttonNext")] private Button m_ButtonNext;
        [SerializeField] [FormerlySerializedAs("buttonHome")] private Button m_ButtonHome;
        [SerializeField] [FormerlySerializedAs("buttonRestart")] private Button m_ButtonRestart;
        [SerializeField] [FormerlySerializedAs("buttonCharacter")] private Button m_ButtonCharacter;

        [Header("Feedback")]
        [SerializeField] [FormerlySerializedAs("correctFeedback")] private Feedback m_CorrectFeedback;
        [SerializeField] [FormerlySerializedAs("wrongFeedback")] private Feedback m_WrongFeedback;
        [SerializeField] [FormerlySerializedAs("idleFeedback")] private Feedback m_IdleFeedback;
        [SerializeField] [FormerlySerializedAs("thinkFeedback")] private Feedback m_ThinkFeedback;

        [Header("Audio")]
        [SerializeField] [FormerlySerializedAs("audioSource")] private AudioSource m_AudioSource;
        [SerializeField]
        private AudioSource m_Music;
        public AudioSource Music => m_Music;

        [Header("Data")]
        [SerializeField] [FormerlySerializedAs("questPrefab")] private Quest m_QuestPrefab;
        [SerializeField] [FormerlySerializedAs("questPrefabs")] private Quest[] m_QuestPrefabs;
        [SerializeField] private ParticleSystem m_ConffetyVFXPrefab;
        [SerializeField] private ReactionPanel m_ReactionPanelPrefab;
        [SerializeField] private ResultPanel m_ResultPanelPrefab;
        [SerializeField] private ReactionConfig m_RightReaction;
        [SerializeField] private ReactionConfig m_WrongReaction;
        [SerializeField] private ReactionConfig m_NeutralReaction;
        
        [Header("Other Dev")]
        [SerializeField]
        private ChallengeConfig m_ChallengeConfig;
        [SerializeField]
        private Transform m_ChallengeContainer;
        [SerializeField]
        private Transform m_PanelContainer;
        [SerializeField]
        private AudioClip m_RightSFX;
        [SerializeField]
        private AudioClip m_WrongSFX;
        [SerializeField]
        private AudioClip m_NeutralSFX;
        [SerializeField]
        private AudioSource m_SFXSource;
        private ReactionPanel m_ReactionPanel;
        private ResultPanel m_ResultPanel;
        [SerializeField]
        private UnityEvent m_OnQuestStart;
        [SerializeField]
        private UnityEvent m_OnQuestNext; // dimana tepatnya event ini dipanggil, apakah di submit atau next quest
        [SerializeField]
        private UnityEvent m_OnQuestEnd;

        private ParticleSystem m_ConffetyVFX;
        private int m_RightAnswerCount;
        public int RightAnswerCount => m_RightAnswerCount;
        public int QuestsCount => m_Quests.Length;
        public ResultPanel ResultPanel => m_ResultPanel;

        private Image[] m_Stars;
        private Quest[] m_Quests;
        private int m_CurrentIndex = 0;
        private float m_RecordedTime;
        private float m_StartTime;
        private IEnumerator m_TimerCo;
        private string m_BaseURL;
        private string m_FrameURL;
        private string m_SessionId;
        private string m_IslandId;
        private ResultItem[] m_Result;
        private APIManager m_ApiManager;
        private AssetManager m_AssetManager;
        private string m_APIInfoURL = "https://be.kimee.io/api/v1/external/games";
        private string m_APIResultURL = "https://be.kimee.io/api/v1/external/games/progress";
        private string m_GameURL = "https://kimee.io/en/island-map/games?islandId=[ISLAND_ID]";
        private HandTutorial m_HandTutorial;
        private Canvas m_Canvas;
        private Color m_ColorTheme;
        private static GameManager s_Instance;

        public Canvas Canvas => m_Canvas;
        public AudioSource AudioSource => m_AudioSource;
        public float ResultTime => m_RecordedTime;
        public Color ColorTheme => m_ColorTheme;
        public APIManager API => m_ApiManager;
        public AssetManager Asset => m_AssetManager;

        public static GameManager Instance => s_Instance;
        public ChallengeConfig ChallengeConfig => m_ChallengeConfig;

        void Awake()
        {
            m_Canvas = GetComponentInParent<Canvas>();

            m_HandTutorial = GetComponentInChildren<HandTutorial>();

            if (!Instance)
                s_Instance = this;

            if (!Canvas)
                Debug.LogError("Canvas is not found");

            if (m_StarPrefab)
                InstantiateStar();
            
            if (m_ConffetyVFXPrefab)
                m_ConffetyVFX = Instantiate(m_ConffetyVFXPrefab, m_ChallengeContainer, false);
            else if (m_ChallengeConfig && m_ChallengeConfig.ConffetyVFXPrefab)
                m_ConffetyVFX = Instantiate(m_ChallengeConfig.ConffetyVFXPrefab, m_ChallengeContainer, false);
            
            m_RightAnswerCount = 0;
        }

        
        void Start()
        {
            if(m_BackgroundMusic && m_Music)
            {
                m_Music.PlayOneShot(m_BackgroundMusic);

            }

            TryGetComponent<APIManager>(out m_ApiManager);
            TryGetComponent<AssetManager>(out m_AssetManager);

#if !UNITY_EDITOR && UNITY_WEBGL
                m_BaseURL = GetParentURL();
                m_FrameURL = GetSelfURL();
#else
            m_BaseURL = "";
            m_FrameURL = "";
#endif

#if !UNITY_EDITOR && UNITY_WEBGL
                LogToBrowser("Base URL: " + m_BaseURL);
                LogToBrowser("Frame URL: " + m_FrameURL);
#endif

#if !UNITY_EDITOR && UNITY_WEBGL
                if(!string.IsNullOrEmpty(m_FrameURL))
                {
                    Dictionary<string, string> parameters = URIHelper.GetParameters(m_FrameURL);
                    if(parameters.ContainsKey("redirect_url"))
                    {
                        m_BaseURL = parameters["redirect_url"];
                    }

                    if(parameters.ContainsKey("bgm_volume"))
                    {
                        int volume = int.Parse(parameters["bgm_volume"]);
                        m_Music.volume = volume / 10f;
                        m_AudioSource.volume = volume / 10f;
                    }
                    else
                    {
                        m_Music.volume = 1f;
                        m_AudioSource.volume = 1f;
                    }
                }
                LogToBrowser("Home URL: " + m_BaseURL);
#endif

            if (!string.IsNullOrEmpty(m_FrameURL))
            {
                Dictionary<string, string> param = URIHelper.GetParameters(m_FrameURL);

                if (param.ContainsKey("session_id"))
                    m_SessionId = param["session_id"];
                else
                    m_SessionId = "67e05cdd-c077-4d84-8c49-15767bf13ef2";
            }
            else
                m_SessionId = "67e05cdd-c077-4d84-8c49-15767bf13ef2";

            if (m_ChallengeConfig)
            {
                if (m_ChallengeConfig.QuestConfigs.Length > 0)
                {
                    m_Quests = new Quest[m_ChallengeConfig.QuestConfigs.Length];
                    for (int i = 0; i < m_ChallengeConfig.QuestConfigs.Length; i++)
                    {
                        m_Quests[i] = InstantiateTemplate(m_ChallengeConfig.QuestLayoutPrefabs);
                        m_Quests[i].SetData(m_ChallengeConfig.QuestConfigs[i]);
                        m_Quests[i].SetChallenge(m_ChallengeConfig);
                        m_Quests[i].SetBackground(m_ChallengeConfig.Background);
                        m_Quests[i].gameObject.SetActive(false);
                    }
                }
                else
                {
                    Debug.LogError("Failed to load data, challenge will return empty");
                }
                
                m_ResultPanel = Instantiate(m_ChallengeConfig.ResultPanelPrefab, m_PanelContainer, false);
                m_ReactionPanel = Instantiate(m_ChallengeConfig.ReactionPanelPrefab, m_PanelContainer, false);

                if (m_ReactionPanel)
                {
                    m_ReactionPanel.Init(this);
                }
                
            }
            else
            {
                if (m_QuestPrefabs.Length > 0)
                {
                    m_Quests = new Quest[m_QuestPrefabs.Length];

                    for (int i = 0; i < m_QuestPrefabs.Length; i++)
                    {
                        m_Quests[i] = InstantiateTemplate(m_QuestPrefabs[i]);
                        m_Quests[i].gameObject.SetActive(false);
                    }

                }
                else if (m_QuestPrefab)
                {
                    m_Quests = new Quest[1];

                    m_Quests[0] = InstantiateTemplate(m_QuestPrefab);
                }
                else
                    Debug.LogError("Failed to load data, challenge will return empty");

                if(m_ResultPanelPrefab) m_ResultPanel = Instantiate(m_ResultPanelPrefab, m_PanelContainer, false);
                    
                if(m_ReactionPanelPrefab) m_ReactionPanel = Instantiate(m_ReactionPanelPrefab, m_PanelContainer, false);

                if (m_ReactionPanel) m_ReactionPanel.Init(this);
            }
            

            //quests[0].gameObject.SetActive(true);

            Debug.Log(m_Background);

            if (m_Background)
                m_Background.sprite = m_Quests[0].Background;

            StartTimer();

            foreach (var quest in m_Quests)
                quest.Setup();

            m_Result = new ResultItem[m_Quests.Length];

#if !UNITY_EDITOR && UNITY_WEBGL
            JS_OnVoiceDownloadComplete();
#endif

            PlayInternal();
        }

        void StopTimer()
        {
            if (m_TimerCo != null)
            {
                m_RecordedTime = Time.time - m_StartTime;

                StopCoroutine(m_TimerCo);
                m_TimerCo = null;
            }
        }

        void StartTimer()
        {
            if (m_TimerCo == null)
            {
                m_StartTime = Time.time;

                m_TimerCo = TimerCo();
                if (m_TimerCo != null)
                {
                    StartCoroutine(m_TimerCo);
                }
            }
        }

        IEnumerator TimerCo()
        {
            if (m_Timer)
            {
                while (true)
                {
                    float elapsedTime = Time.time - m_StartTime;

                    float m = Mathf.FloorToInt(elapsedTime / 60);
                    float s = Mathf.FloorToInt(elapsedTime % 60);

                    string timerText = m.ToString().PadLeft(2, '0') + ":" + s.ToString().PadLeft(2, '0');

                    if (s % 10 == 0)
                        m_IdleFeedback.Play(m_Npc ? m_Npc : transform);
                    else if (s % 5 == 0)
                        m_ThinkFeedback.Play(m_Npc ? m_Npc : transform);

                    m_Timer.text = timerText;

                    yield return new WaitForSeconds(1);
                }
            }

            yield return null;
        }

        public virtual void SetNPC(RuntimeAnimatorController controller)
        {
            m_Npc.TryGetComponent<Animator>(out Animator animator);

            if (animator)
                animator.runtimeAnimatorController = controller;
        }

        public void SetNPCDialog(string dialog)
        {
            SetNPCDialogInternal(dialog);
        }

        protected virtual void SetNPCDialogInternal(string dialog)
        {
            if (m_NpcDialog)
            {
                m_NpcDialog.text = StringHelper.RemoveUnsupportedSymbols(dialog);
                if (m_NpcDialog.TryGetComponent(out TextEffect textEffect))
                {
                    textEffect.Refresh();
                }
            }
        }
        
        Quest InstantiateTemplate(Quest templatePrefab)
        {
            Quest template = Instantiate(templatePrefab, m_ChallengeContainer != null ? m_ChallengeContainer.transform : transform);

            template.transform.localPosition = Vector3.zero;
            template.transform.localScale = Vector3.one;

            m_ColorTheme = template.Color;

            return template;
        }

        protected virtual void InstantiateStar()
        {
            m_Stars = new Image[3];

            
            /*stars[0] = Instantiate(starPrefab, starContainer);
            stars[0].gameObject.SetActive(false);

            stars[1] = Instantiate(stars[0], starContainer);
            stars[1].gameObject.SetActive(false);

            stars[2] = Instantiate(stars[0], starContainer);
            stars[2].gameObject.SetActive(false);*/

            // you should use for loop
            for (int i = 0; i < m_Stars.Length; i++)
            {
                Image star = Instantiate(m_StarPrefab, m_StarContainer);
                m_Stars[i] = star;
            }
        }

        public virtual void InitQuest(SO_Quest questData)
        {
            if (m_Title != null && questData.Title != null)
                m_Title.text = questData.Title;

            if (m_Category != null)
                m_Category.text = questData.Category.ToString().Replace('_', ' ');
            OnQuestStartInvoke();
        }

        public void PlayQuestionVoice()
        {
            m_Quests[m_CurrentIndex].PlayQuestionVoice();
            StartCoroutine(WaitToPlaySoundQuest());
            PlayHowManySound(m_Quests[m_CurrentIndex]);
        }
        private void PlayHowManySound(Quest quest)
        {
            if (quest is QuestMultipleChoices multipleChoices)
            {
                if (multipleChoices.QuestAnimationHandler == null) return;
                multipleChoices.QuestAnimationHandler.PlayAnimation();
            }
        }
        public void PlaySoundQuest()
        {
            PlaySoundQuestInternal();
        }

        private void PlaySoundQuestInternal()
        {
            m_Quests[m_CurrentIndex].PlaySoundQuest();
        }

        public void Play()
        {
            PlayInternal();
        }

        private void PlayInternal()
        {
            m_CurrentIndex = -1;
            NextInternal();
        }

        public void Next()
        {
            NextInternal();
        }

        private void NextInternal()
        {
            int nextIndex = m_CurrentIndex + 1;

            if (m_Quests.Length > 0 && nextIndex < m_Quests.Length)
            {
                if (m_CurrentIndex >= 0)
                    m_Quests[m_CurrentIndex].gameObject.SetActive(false);

                m_Quests[nextIndex].gameObject.SetActive(true);
                
                if (m_Background)
                    m_Background.sprite = m_Quests[nextIndex].Background;

                m_CurrentIndex = nextIndex;

                ResetQuestInternal();
            }
        }
        public void SetRightAsnwer(int set)
        {
            m_RightAnswerCount = set;
        }
        private void OnQuestStartInvoke()
        {
            m_OnQuestStart?.Invoke();
            Debug.Log("OnQuestStartInvoke called");
            StartCoroutine(WaitToPlaySoundQuest());
        }

        private IEnumerator WaitToPlaySoundQuest()
        {
            yield return new WaitForSeconds(2f);
            PlaySoundQuestInternal();
        }
        private void OnQuestEndInvoke()
        {
            m_OnQuestEnd?.Invoke();
        }
        private void OnQuestNextInvoke()
        {
            m_OnQuestNext?.Invoke();
            Debug.Log("OnQuestNextInvoke called");
        }
        public virtual void Submit(int star = 0)
        {
            if(m_HandTutorial)
                m_HandTutorial.Disable();
            
            StopTimer();

            if (star < 1)
                star = 1;
            else if (star > 3)
                star = 3;

            if (star == 3 && m_Quests[m_CurrentIndex].Data.TresholdTime > 0 && m_RecordedTime > m_Quests[m_CurrentIndex].Data.TresholdTime)
                star = 2;

            if (m_CurrentIndex < m_Result.Length)
            {
                Debug.Log($"Submitting result for quest {m_CurrentIndex}: stars={star}, time={Mathf.RoundToInt(m_RecordedTime)}");
                m_Result[m_CurrentIndex] = new ResultItem { stars = star, time = Mathf.RoundToInt(m_RecordedTime) };
            }
                
            ShowResultInternal(star);
        }

        public void Reset()
        {
            ResetInternal();
        }

        protected virtual void ResetInternal()
        {
            CloseResultInternal();
            StopTimer();
            StartTimer();
        }

        public void ResetQuest()
        {
            ResetQuestInternal();
        }

        protected virtual void ResetQuestInternal()
        {
            ResetInternal();

            m_Quests[m_CurrentIndex].Reset();
        }

        public void ShowResult(int star)
        {
            ShowResultInternal(star);
        }

        protected virtual void ShowResultInternal(int star)
        {
            if (m_ReactionPanel)
            {
                if (m_PopResult)
                    m_PopResult.gameObject.SetActive(false);
            }
            else
            {
                if (m_PopResult)
                    m_PopResult.gameObject.SetActive(true);
            }
            
            if (m_NeutralReaction && m_Quests[m_CurrentIndex].name.IndexOf("ColortheIsland") > -1)
            {
                if(star > 1)
                    m_RightAnswerCount++;
                
                m_ReactionPanel.ShowReaction(m_NeutralReaction);
                m_SFXSource.PlayOneShot(m_NeutralSFX);
            }
            else if (star > 1) // jika bintang lebih dari 1 maka dianggap jawaban benar
            {
                m_RightAnswerCount++;
                if (m_ChallengeConfig)
                {
                    m_ReactionPanel.ShowReaction(m_ChallengeConfig.RightReaction);
                    if (m_ConffetyVFX)
                    {
                        m_ConffetyVFX.Play();
                    }    
                }
                else if (m_RightReaction)
                {
                    m_ReactionPanel.ShowReaction(m_RightReaction);
                    if (m_ConffetyVFX)
                    {
                        m_ConffetyVFX.Play();
                    }   
                }
                else
                {
                    if (m_CorrectFeedback)
                        m_CorrectFeedback.Play(m_Npc ? m_Npc : transform);

                    if (m_NpcDialog)
                        SetNPCDialogInternal("Yaaay! Amazing!");

                    m_CorrectTitle.gameObject.SetActive(true);
                    m_CorrectNote.gameObject.SetActive(true);
                }
                m_SFXSource.PlayOneShot(m_RightSFX);
            }
            else
            {
                if (m_ChallengeConfig)
                {
                    m_ReactionPanel.ShowReaction(m_ChallengeConfig.WrongReaction);
                }
                else if (m_WrongReaction)
                {
                    m_ReactionPanel.ShowReaction(m_WrongReaction);
                }
                else
                {
                    if (m_WrongFeedback)
                        m_WrongFeedback.Play(m_Npc ? m_Npc : transform);

                    if (m_NpcDialog)
                        SetNPCDialogInternal("Oh no!");

                    m_WrongTitle.gameObject.SetActive(true);
                    m_WrongNote.gameObject.SetActive(true);
                }
                m_SFXSource.PlayOneShot(m_WrongSFX);
            }

            if (m_Stars != null && m_Stars.Length > 0)
            {
                for (int i = 0; i < m_Stars.Length; i++)
                {
                    m_Stars[i].gameObject.SetActive(true);

                    if (i < star)
                        m_Stars[i].color = Color.white;
                    else
                        m_Stars[i].color = Color.black;
                }
            }

            if (IsLastQuestInternal())
            {
                m_ButtonNext.gameObject.SetActive(false);
                SendResultInternal();
            }
            else
                m_ButtonNext.gameObject.SetActive(true);

        }

        private bool IsLastQuestInternal()
        {
            return m_CurrentIndex + 1 == m_Quests.Length;
        }
        public bool IsLastQuest()
        {
            return IsLastQuestInternal();
        }

        public virtual void GoHome()
        {
            if (!string.IsNullOrEmpty(m_BaseURL))
            {
#if !UNITY_EDITOR && UNITY_WEBGL
                    RedirectParentWindow(m_BaseURL);
#endif
            }
        }

        public void SendResult()
        {
            SendResultInternal();
        }

        protected virtual void SendResultInternal()
        {
            Result compiledResult = new Result
            {
                session_id = m_SessionId,
                result = m_Result
            };

            string json = JsonUtility.ToJson(compiledResult);

            if (m_ApiManager)
                m_ApiManager.SendData(m_APIResultURL, json);
        }


        public void CloseResult()
        {
            CloseResultInternal();
        }

        protected virtual void CloseResultInternal()
        {
            if (m_PopResult)
                m_PopResult.gameObject.SetActive(false);

            m_CorrectTitle.gameObject.SetActive(false);
            m_CorrectNote.gameObject.SetActive(false);
            m_WrongTitle.gameObject.SetActive(false);
            m_WrongNote.gameObject.SetActive(false);

            if (m_Stars != null && m_Stars.Length > 0)
            {
                for (int i = 0; i < m_Stars.Length; i++)
                    m_Stars[i].gameObject.SetActive(false);
            }
        }

        public virtual void ReloadScene()
        {
            ReloadIframe();
        }
        public void PlaySFX(AudioClip audioClip)
        {
            m_SFXSource.PlayOneShot(audioClip);
        }

        public void ShowTutorial(RectTransform endPos, HandTutorial.TutorialMode mode = HandTutorial.TutorialMode.Click)
        {
            ShowTutorialInternal(endPos, mode);
        }

        private void ShowTutorialInternal(RectTransform endPos, HandTutorial.TutorialMode mode)
        {
            ShowTutorialInternal(m_NpcDialog.rectTransform, endPos, mode);
        }

        public void ShowTutorial(RectTransform startPos, RectTransform endPos, HandTutorial.TutorialMode mode = HandTutorial.TutorialMode.Click)
        {
            ShowTutorialInternal(startPos, endPos, mode);
        }

        private void ShowTutorialInternal(RectTransform startPos, RectTransform endPos, HandTutorial.TutorialMode mode)
        {
            if (m_HandTutorial && m_CurrentIndex == 0)
            {
                m_HandTutorial.PlayTutorial(startPos, endPos, Canvas.transform, mode);
            }
        }
    }
}