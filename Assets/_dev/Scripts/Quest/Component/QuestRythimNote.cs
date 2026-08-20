using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

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

        protected override void Awake()
        {
            base.Awake();        
        }

        public override void Init()
        {
            base.Init();

            rb = GetComponent<Rigidbody2D>();

            if(rb)
                rb.gravityScale = 0;
            else
                Debug.LogError("Rigidbody not found");

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
                // 1. Calculate the next target position using fixedDeltaTime
                Vector2 currentPosition = rb.position;
                Vector2 targetPosition = currentPosition + (Vector2.left * (speed * 5000) * Time.fixedDeltaTime);

                // 2. Teleport the physics body smoothly to the new position
                rb.MovePosition(targetPosition);

                // 3. CRITICAL: Wait exactly for the next physics loop calculation
                yield return new WaitForFixedUpdate();
            }

            isPlaying = false;
            isDone = false;
        }
        
        public void OnBeat(bool success = true)
        {
            isDone = true;
            StopAllCoroutines();

            if(success)
            {
                if(clip && QuestRythimMaker.AudioSource)
                    QuestRythimMaker.AudioSource.PlayOneShot(clip);
                
                quest.OnAnswered(true);
            }

            
            Rect.anchoredPosition = new Vector2(0, 0);
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