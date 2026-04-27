using System.Collections;
using UnityEngine;

namespace EduGame
{
    [RequireComponent(typeof(Collider2D))]
    [RequireComponent(typeof(Rigidbody2D))]

    public class QuestRythimNote : QuestComponent
    {   
        [SerializeField] protected int point;
        
        public int Point => point;

        private Rigidbody2D rb;

        private bool isPlaying;
        private bool isDone;
        
        private float speed = 1;
        private float delay = 0;
        private AudioClip clip;

        private Vector3 originalPosition;

        protected override void Awake()
        {
            base.Awake();

            rb = GetComponent<Rigidbody2D>();

            if(rb)
                rb.gravityScale = 0;
            else
                Debug.LogError("Rigidbody not found");
                
            originalPosition = rb.position;
        }

        public override void Init()
        {
            base.Init();

            Rect.localScale = Vector3.one;
            Rect.anchoredPosition = Vector2.zero;
        }

        public void Setup(float speed, float delay, AudioClip clip = null)
        {
            this.speed = speed;
            this.delay = delay;
            this.clip = clip;
        }

        public void Play()
        {
            if(!isPlaying)
            {
                isPlaying = true;

                StartCoroutine(PlayCo());
            }
        }

        IEnumerator PlayCo()
        {
            if(delay > 0)
                yield return new WaitForSeconds(delay);

            while (!isDone)
            {
                rb.MovePosition(transform.position + Vector3.left * speed);
                yield return new WaitForSeconds(Time.deltaTime);
            }

            isPlaying = false;
            isDone = false;
        }
        
        public void OnBeat(bool success = true)
        {
            isDone = true;

            if(success)
            {
                if(clip && GameManager.Instance.AudioSource)
                    GameManager.Instance.AudioSource.PlayOneShot(clip);
                
                quest.OnAnswered(true);
            }

            rb.MovePosition(originalPosition);
        }

        void OnTriggerEnter2D(Collider2D collision)
        {
            if(collision.tag == "NoteEnd")
            {
                quest.OnAnswered(false);

                OnBeat(false);
            }
        }

        public override void Reset()
        {
            isPlaying = false;
            isDone = false;
        }
    }
}