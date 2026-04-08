using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

namespace EduGame
{
    public class ChallengeManager : MonoBehaviour
    {
        [Header("Frame")]
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text category;
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

        [Header("Score Page")]
        [SerializeField] private RectTransform scoreScreen;
        [SerializeField] private TMP_Text score;
        [SerializeField] private RectTransform badScore;
        [SerializeField] private RectTransform goodScore;
        [SerializeField] private RectTransform perfectScore;

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

        private List<Quest> initiatedQuests = new List<Quest>();
        private int currentIndex = 0;
        private int accumulatedScore = 0;
        private Image[] stars;

        public int CurrentQuestNumber => currentIndex + 1;
        public Canvas Canvas { get; private set; }

        public static ChallengeManager Instance { get; private set; }

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

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            //string fullURL = Application.absoluteURL;
            string fullURL = "http://127.0.0.1/SaitoonShooter/?cid=0001&cd=3&tl=id";

            Dictionary<string, string> parameters = URIHelper.GetParameters(fullURL);

            if (parameters.ContainsKey("cid"))
            {
                string dataPath = Global.CHALLENGE_DATA_DIR + Global.CHALLENGE_DATA_NAME.Replace("$$", parameters["cid"]);
                AssetLoader<SO_Challenge>.Load(dataPath, OnDataLoaded);
            }
            else
                Debug.LogError("Data is not set, challenge will return empty");
        }

        protected virtual void OnDataLoaded(AsyncOperationHandle<SO_Challenge> loadHandler)
        {
            if (loadHandler.Status == AsyncOperationStatus.Succeeded)
            {
                SO_Challenge data = loadHandler.Result;

                if (!data || data.Quests.Length == 0)
                    Debug.LogError("Data is not set, challenge will return empty");

                foreach (var quest in data.Quests)
                {
                    if (quest.Active)
                    {
                        if (!quest.Quest)
                        {
                            Debug.LogError("Quest data is not set, quest will be skipped");
                            continue;
                        }

                        if (!quest.Template)
                        {
                            Debug.LogError("Template is not set, quest will be skipped");
                            continue;
                        }

                        Quest template = InstantiateTemplate(quest.Template);
                        template.Init(quest.Quest);
                        template.gameObject.SetActive(false);

                        initiatedQuests.Add(template);
                    }
                }

                if (scoreScreen)
                    scoreScreen.gameObject.SetActive(false);

                if (badScore)
                    badScore.gameObject.SetActive(false);
                
                if (goodScore)
                    goodScore.gameObject.SetActive(false);
                
                if (perfectScore)
                    perfectScore.gameObject.SetActive(false);
                    
                SetPage(0);
            }
            else
                Debug.LogError("Failed to load data, challenge will return empty");
        }

        Quest InstantiateTemplate(Quest templatePrefab)
        {
            Quest template = Instantiate(templatePrefab, transform);

            template.transform.localPosition = Vector3.zero;
            template.transform.localScale = Vector3.one;

            return template;
        }

        public virtual void SetNPCDialog(string dialog)
        {
            if(npcDialog)
                npcDialog.text = dialog;
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

        public virtual void Submit(bool good, int point = 0)
        {
            if(good && point > 0)
                AddScore(point);

            ShowResult(good);
        }

        public virtual void ResetQuest()
        {
            CloseResult();

            initiatedQuests[currentIndex].Reset();
        }

        public virtual void Previous()
        {
            SetPage(currentIndex, false);

            currentIndex--;

            SetPage(currentIndex, true);
        }

        public virtual void ShowResult(bool good)
        {
            if (popResult)
                popResult.gameObject.SetActive(true);

            if (good)
            {
                if(correctFeedback)
                    correctFeedback.Play(npc? npc : transform);
                
                if(npcDialog)
                    SetNPCDialog("Yaaay! Amazing!");
                
                correctTitle.gameObject.SetActive(true);
                correctNote.gameObject.SetActive(true);
            }
            else
            {
                if(wrongFeedback)
                    wrongFeedback.Play(npc? npc : transform);

                if(npcDialog)
                    SetNPCDialog("Oh no!");

                wrongTitle.gameObject.SetActive(true);
                wrongNote.gameObject.SetActive(true);
            }

            if (stars != null && stars.Length > 0)
            {
                for (int i = 0; i < stars.Length; i++)
                {
                    stars[i].gameObject.SetActive(true);
                    
                    if(good || (!good && i == 0))
                        stars[i].color = Color.white;
                    else
                        stars[i].color = Color.black;
                }
            }
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

        public virtual void Next()
        {
            CloseResult();

            SetPage(currentIndex, false);

            currentIndex++;

            if (!SetPage(currentIndex, true))
                Finish();
        }

        public virtual bool SetPage(int index, bool status = true)
        {
            bool res = false;

            if (initiatedQuests.Count > index && index >= 0)
            {
                if (status)
                    initiatedQuests[index].Enabled();
                else
                    initiatedQuests[index].Disabled();

                res = true;
            }

            return res;
        }

        public virtual void Finish()
        {
            if (scoreScreen)
                scoreScreen.gameObject.SetActive(true);

            if (score)
                score.text = accumulatedScore.ToString();

            if (accumulatedScore < 30 && badScore)
            {
                if(wrongFeedback)
                    wrongFeedback.Play(transform);

                badScore.gameObject.SetActive(true);
            }
            else if (accumulatedScore <= 40 && goodScore)
            {
                if(correctFeedback)
                    correctFeedback.Play(transform);

                goodScore.gameObject.SetActive(true);
            }
            else if (accumulatedScore == 50 && perfectScore)
            {
                if(correctFeedback)
                    correctFeedback.Play(transform);

                perfectScore.gameObject.SetActive(true);
            }
        }

        public virtual void AddScore(int score)
        {
            accumulatedScore += score;
        }
    }
}