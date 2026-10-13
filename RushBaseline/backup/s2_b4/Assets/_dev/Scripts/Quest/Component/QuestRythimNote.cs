using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

namespace EduGame
{
    [RequireComponent(typeof(Collider2D))]
    [RequireComponent(typeof(Rigidbody2D))]

    public class QuestRythimNote : QuestComponent
    {   
        [SerializeField] [FormerlySerializedAs("point")] protected int m_Point;
        
        public int Point => m_Point;

        private Rigidbody2D m_Rb;

        private bool m_IsPlaying;
        private bool m_IsDone;
        
        private float m_Speed = 1;
        private float m_Delay = 0;
        private AudioClip m_Clip;

        protected override void Awake()
        {
            base.Awake();        
        }

        public override void Init()
        {
            base.Init();

            m_Rb = GetComponent<Rigidbody2D>();

            if(m_Rb)
                m_Rb.gravityScale = 0;
            else
                Debug.LogError("Rigidbody not found");

            Rect.localScale = Vector3.one;
            Rect.anchoredPosition = Vector2.zero;
        }

        public void Setup(float speed, float delay, AudioClip clip = null)
        {
            this.m_Speed = speed;
            this.m_Delay = delay;
            this.m_Clip = clip;
        }

        public void Play()
        {
            if(!m_IsPlaying)
            {
                m_IsPlaying = true;

                StartCoroutine(PlayCo());
            }
        }

        IEnumerator PlayCo()
        {
            if(m_Delay > 0)
                yield return new WaitForSeconds(m_Delay);

            while (!m_IsDone)
            {
                // 1. Calculate the next target position using fixedDeltaTime
                Vector2 currentPosition = m_Rb.position;
                Vector2 targetPosition = currentPosition + (Vector2.left * (m_Speed * 5000) * Time.fixedDeltaTime);

                // 2. Teleport the physics body smoothly to the new position
                m_Rb.MovePosition(targetPosition);

                // 3. CRITICAL: Wait exactly for the next physics loop calculation
                yield return new WaitForFixedUpdate();
            }

            m_IsPlaying = false;
            m_IsDone = false;
        }
        
        public void OnBeat(bool success = true)
        {
            OnBeatInternal(success);
        }

        private void OnBeatInternal(bool success)
        {
            m_IsDone = true;
            StopAllCoroutines();

            if(success)
            {
                if(m_Clip && QuestRythimMaker.AudioSource)
                    QuestRythimMaker.AudioSource.PlayOneShot(m_Clip);
                
                m_Quest.OnAnswered(true);
            }

            
            Rect.anchoredPosition = new Vector2(0, 0);
        }

        void OnTriggerEnter2D(Collider2D collision)
        {
            if(collision.tag == "NoteEnd")
            {
                m_Quest.OnAnswered(false);

                OnBeatInternal(false);
            }
        }

        public override void Reset()
        {
            m_IsPlaying = false;
            m_IsDone = false;
        }
    }
}