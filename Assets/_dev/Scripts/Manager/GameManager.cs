using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

namespace EduGame
{
    public class GameManager : MonoBehaviour
    {
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

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;

        [Header("Data")]
        [SerializeField] private Quest questPrefab;
        
        private Image[] stars;
        private Quest quest;
        private float recordedTime;
        private float startTime;
        private IEnumerator timerCo;
        
        public Canvas Canvas { get; private set; }
        public AudioSource AudioSource => audioSource;
        public float ResultTime => recordedTime;
        public Color ColorTheme { get; private set; }
        
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
            if (questPrefab)
            {
                quest = InstantiateTemplate(questPrefab);
                quest.gameObject.SetActive(true);

                StartTimer();
            }
            else
                Debug.LogError("Failed to load data, challenge will return empty");
        }

        void StopTimer()
        {
            if(timerCo != null)
            {
                recordedTime = Time.time - startTime;

                StopCoroutine(timerCo);
                timerCo = null;  
            }
        }

        void StartTimer()
        {
            if(timerCo == null)
            {
                startTime = Time.time;

                timerCo = TimerCo();
                StartCoroutine(timerCo);
            }
        }

        IEnumerator TimerCo()
        {
            if(timer)
            {
                while (true)
                {
                    float elapsedTime = Time.time - startTime;

                    float m = Mathf.FloorToInt(elapsedTime/60);
                    float s = Mathf.FloorToInt(elapsedTime%60);

                    string timerText = m.ToString().PadLeft(2, '0') + ":" + s.ToString().PadLeft(2, '0');

                    timer.text = timerText;

                    yield return new WaitForSeconds(1);
                }
            }

            yield return null;
        }

        Quest InstantiateTemplate(Quest templatePrefab)
        {
            Quest template = Instantiate(templatePrefab, transform);

            template.transform.localPosition = Vector3.zero;
            template.transform.localScale = Vector3.one;

            ColorTheme = template.Color;

            if(background && template.Background)
                background.sprite = template.Background;

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
            StopTimer();

            ShowResult(good);
        }

        public virtual void ResetQuest()
        {
            CloseResult();
            StopTimer();
            StartTimer();

            quest.Reset();
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
        }
    }
}