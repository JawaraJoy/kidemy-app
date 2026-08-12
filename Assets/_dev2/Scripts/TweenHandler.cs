using NaughtyAttributes;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Rush
{
    public enum TweenForm
    {
        None,
        FadeCanvas,
        RectPosition,
        Position,
        RotateAroundX,
        RotateAroundY,
        RotateAroundZ,
        Rotating,
        Scale,
        ColorImage,
        ColorMaterial,
    }
    public enum LoopForm
    {
        Once,
        Clamp,
        PingPong,
    }

    [Serializable]
    public struct FadeTweenStruct
    {
        public float Start;
        public float End;
    }
    [Serializable]
    public struct RectPosStruct
    {
        public Vector2 StartRectPosition;
        public Vector2 EndRectPosition;
    }
    [Serializable]
    public struct PositionStruct
    {
        public Vector3 StartPosition;
        public Vector3 EndPosition;
    }
    [Serializable]
    public struct ScaleStruct
    {
        public Vector3 StartScale;
        public Vector3 EndScale;
    }
    [Serializable]
    public struct ColorStruct
    {
        public Color StartColor;
        public Color EndColor;
    }
    [Serializable]
    public class LoopTweenField
    {
        [SerializeField] 
        private LoopForm m_Type;
        [SerializeField] 
        private bool m_SetCompleteOnRepeat;
        [Tooltip("Set to -1 to infinity loop")]
        [SerializeField] private int _repeatCount = 1;
        public void LoopControl(LTDescr tween)
        {
            switch (m_Type)
            {
                case LoopForm.Once:
                    tween.setLoopOnce();
                    break;
                case LoopForm.Clamp:
                    tween.setLoopClamp();
                    break;
                case LoopForm.PingPong:
                    tween.setLoopPingPong();
                    break;
            }

            if (_repeatCount > 0 )
            {
                tween.setRepeat(_repeatCount);
            }
            tween.setOnCompleteOnRepeat(m_SetCompleteOnRepeat);
        }
    }
    [Serializable]
    public class TweenField
    {
        [SerializeField] 
        private string m_Id;
        [SerializeField] 
        private string m_Group;
        [SerializeField] 
        private GameObject m_Content;
        [SerializeField] 
        private TweenForm m_TweenForm = TweenForm.None;
        [SerializeField] 
        private LeanTweenType m_EaseType = LeanTweenType.linear;
        [SerializeField]
        private float m_Delay = 0.1f;
        [SerializeField] 
        private float m_EaseDuration = 0.3f;

        [SerializeField] 
        private bool m_IsLooping;
        [ShowIf(nameof(m_IsLooping))]
        [AllowNesting]
        [SerializeField] 
        private LoopTweenField m_LoopField;

        [ShowIf(nameof(m_TweenForm), TweenForm.FadeCanvas)]
        [AllowNesting]
        [SerializeField] 
        private FadeTweenStruct m_FadeCanvas;

        [ShowIf(nameof(m_TweenForm), TweenForm.RectPosition)]
        [AllowNesting]
        [SerializeField] 
        private RectPosStruct m_RectPos;

        [ShowIf(nameof(m_TweenForm), TweenForm.Position)]
        [AllowNesting]
        [SerializeField] 
        private PositionStruct m_Position;

        [ShowIf(nameof(m_TweenForm), TweenForm.RotateAroundX)]
        [AllowNesting]
        [SerializeField] 
        private float m_RotationX;

        [ShowIf(nameof(m_TweenForm), TweenForm.RotateAroundY)]
        [AllowNesting]
        [SerializeField] 
        private float m_RotationY;

        [ShowIf(nameof(m_TweenForm), TweenForm.RotateAroundZ)]
        [AllowNesting]
        [SerializeField] 
        private float m_RotationZ;

        [ShowIf(nameof(m_TweenForm), TweenForm.Scale)]
        [AllowNesting]
        [SerializeField] 
        private ScaleStruct m_Scale;

        [ShowIf(nameof(m_TweenForm), TweenForm.ColorImage)]
        [AllowNesting]
        [SerializeField] 
        private ColorStruct m_ColorImage;

        [ShowIf(nameof(m_TweenForm), TweenForm.ColorMaterial)]
        [AllowNesting]
        [SerializeField] 
        private ColorStruct m_ColorMaterial;

        [SerializeField] 
        private UnityEvent m_OnStartTweening = new UnityEvent();
        [SerializeField] 
        private UnityEvent m_OnCompleteTweening = new UnityEvent();

        private bool m_IsReverse = false;
        //private bool _isComplete = false;
        private CanvasGroup m_CanvasGroup;
        private Image m_Image;
        private List<Material> m_Materials;
        private RectTransform m_Rect;

        private LTDescr m_AlphaTween;
        private LTDescr m_ScaleTween;
        private LTDescr m_RectPositionTween;
        private LTDescr m_PositionTween;
        private LTDescr m_RotationAroundXTween;
        private LTDescr m_RotationAroundYTween;
        private LTDescr m_RotationAroundZTween;
        private LTDescr m_ColorTween;

        private int m_LeanTweenId = -1;

        public string GetId() => m_Id;
        public string GetGroup() => m_Group;
        public void SetIsReverse(bool set) => m_IsReverse = set;
        public void Init()
        {
            switch(m_TweenForm)
            {
                case TweenForm.FadeCanvas:
                    if (m_Content.TryGetComponent(out CanvasGroup cg))
                    {
                        m_CanvasGroup = cg;
                    }
                    else
                    {
                        m_Content.AddComponent<CanvasGroup>();
                    }
                    break;
                case TweenForm.ColorMaterial:
                    if (m_Content.TryGetComponent(out Renderer ren))
                    {
                        m_Materials = new List<Material>(ren.materials);
                    }
                    break;
                case TweenForm.ColorImage:
                    if (m_Content.TryGetComponent(out Image im))
                    {
                        m_Image = im;
                    }
                    else
                    {
                        m_Content.AddComponent<Image>();
                    }
                    break;
                case TweenForm.RectPosition:
                    if (m_Content.TryGetComponent(out RectTransform rt))
                    {
                        m_Rect = rt;
                    }
                    break;
            }

            if (m_Group == string.Empty)
            {
                int random = UnityEngine.Random.Range(1000, 9999);
                m_Group = random.ToString();
            }
        }

        public void OnStartTween()
        {
            //_isComplete = false;
            m_OnStartTweening?.Invoke();
            Debug.Log($"{m_Id} is StartTween");
        }

        public void OnCompleteTween()
        {
            m_LeanTweenId = -1;
            m_OnCompleteTweening?.Invoke();
            //_isComplete = true;
            Debug.Log($"{m_Id} is CompleteTween");
        }

        public void CancelTween()
        {
            if(m_LeanTweenId != -1)
                LeanTween.cancel(m_LeanTweenId, true);

            /*_alphaTween = null;
            _colorTween = null;
            _positionTween = null;
            _rotationXTween = null;
            _rotationYTween = null;
            _rotationZTween = null;
            _colorTween = null;*/
        }

        public void PlayTween()
        {
            m_Content.SetActive(true);
            OnStartTween();
            switch(m_TweenForm)
            {
                case TweenForm.None:
                    break;
                case TweenForm.FadeCanvas:
                    FadeCanvasTween();
                    break;
                case TweenForm.Position:
                    PositionTween();
                    break;
                case TweenForm.RectPosition:
                    RectPositionTween();
                    break;
                case TweenForm.RotateAroundX:
                    RotationAroundXTween();
                    break;
                case TweenForm.RotateAroundY:
                    RotationAroundYTween();
                    break;
                case TweenForm.RotateAroundZ:
                    RotationAroundZTween();
                    break;
                case TweenForm.Scale:
                    ScaleTween();
                    break;
                case TweenForm.ColorImage:
                    ColorImageTween();
                    break;
                case TweenForm.ColorMaterial:
                    ColorMaterialTween();
                    break;
            }
        }

        private void FadeCanvasTween()
        {
            if (!m_IsReverse)
            {
                m_CanvasGroup.alpha = m_FadeCanvas.Start;
                m_AlphaTween = LeanTween.alphaCanvas(m_CanvasGroup, m_FadeCanvas.End, m_EaseDuration);
            }
            else
            {
                m_CanvasGroup.alpha = m_FadeCanvas.End;
                m_AlphaTween = LeanTween.alphaCanvas(m_CanvasGroup, m_FadeCanvas.Start, m_EaseDuration);
            }

            LTDescrControl(m_AlphaTween);
        }
        private void PositionTween()
        {
            if (!m_IsReverse)
            {
                m_Content.transform.localPosition = m_Position.StartPosition;
                m_PositionTween = LeanTween.moveLocal(m_Content, m_Position.EndPosition, m_EaseDuration);
            }
            else
            {
                m_Content.transform.localPosition = m_Position.EndPosition;
                m_PositionTween = LeanTween.moveLocal(m_Content, m_Position.StartPosition, m_EaseDuration);
            }

            LTDescrControl(m_PositionTween);
        }
        private void RectPositionTween()
        {
            if (!m_IsReverse)
            {
                m_Rect.anchoredPosition = m_RectPos.StartRectPosition;
                m_RectPositionTween = LeanTween.move(m_Rect, m_RectPos.EndRectPosition, m_EaseDuration);
            }
            else
            {
                m_Rect.anchoredPosition = m_RectPos.EndRectPosition;
                m_RectPositionTween = LeanTween.move(m_Rect, m_RectPos.StartRectPosition, m_EaseDuration);
            }

            LTDescrControl(m_RectPositionTween);
        }
        private void ScaleTween()
        {
            if (!m_IsReverse)
            {
                m_Content.transform.localScale = m_Scale.StartScale;
                m_ScaleTween = LeanTween.scale(m_Content, m_Scale.EndScale, m_EaseDuration);
            }
            else
            {
                m_Content.transform.localScale = m_Scale.EndScale;
                m_ScaleTween = LeanTween.scale(m_Content, m_Scale.StartScale, m_EaseDuration);
            }
            LTDescrControl(m_ScaleTween);
        }
        private void RotationAroundXTween()
        {
            if (!m_IsReverse)
            {
                m_RotationAroundXTween = LeanTween.rotateAroundLocal(m_Content, Vector3.right, m_RotationX, m_EaseDuration);
            }
            else
            {
                m_RotationAroundXTween = LeanTween.rotateAroundLocal(m_Content, Vector3.left, m_RotationX, m_EaseDuration);
            }

            LTDescrControl(m_RotationAroundXTween);
        }
        private void RotationAroundYTween()
        {
            if (!m_IsReverse)
            {
                m_RotationAroundYTween = LeanTween.rotateAroundLocal(m_Content, Vector3.up, m_RotationY, m_EaseDuration);
            }
            else
            {
                m_RotationAroundYTween = LeanTween.rotateAroundLocal(m_Content, Vector3.down, m_RotationY, m_EaseDuration);
            }

            LTDescrControl(m_RotationAroundYTween);
        }
        private void RotationAroundZTween()
        {

            if (!m_IsReverse)
            {
                m_RotationAroundZTween = LeanTween.rotateAroundLocal(m_Content, Vector3.forward, m_RotationZ, m_EaseDuration);
            }
            else
            {
                m_RotationAroundZTween = LeanTween.rotateAroundLocal(m_Content, Vector3.back, m_RotationZ, m_EaseDuration);
            }
            LTDescrControl(m_RotationAroundZTween);
        }
        private void ColorImageTween()
        {
            m_ColorTween = LeanTween.value(m_Content, 0.1f, 1f, m_EaseDuration);
            if (!m_IsReverse)
            {
                ColorImageControl(m_ColorTween, m_Image, m_ColorImage.StartColor, m_ColorImage.EndColor);
            }
            else
            {
                ColorImageControl(m_ColorTween, m_Image, m_ColorImage.EndColor, m_ColorImage.StartColor);
            }
            LTDescrControl(m_ColorTween);
        }
        private void ColorMaterialTween()
        {
            m_ColorTween = LeanTween.value(m_Content, 0f, 1f, m_EaseDuration);
            if (!m_IsReverse)
            {
                foreach(Material m in m_Materials)
                {
                    ColorMaterialControl(m_ColorTween, m, m_ColorMaterial.StartColor, m_ColorMaterial.EndColor);
                }
            }
            else
            {
                foreach (Material m in m_Materials)
                {
                    ColorMaterialControl(m_ColorTween, m, m_ColorMaterial.EndColor, m_ColorMaterial.StartColor);
                }
            }
            LTDescrControl(m_ColorTween);
        }

        private void LTDescrControl(LTDescr tweenDescr)
        {
            m_LeanTweenId = tweenDescr.id;
            tweenDescr.setDelay(m_Delay);
            tweenDescr.setEase(m_EaseType);

            if (m_IsLooping)
            {
                m_LoopField.LoopControl(tweenDescr);
            }

            tweenDescr.setOnComplete(OnCompleteTween);
        }
        private void ColorImageControl(LTDescr tween, Image target, Color start, Color end)
        {
            tween.setOnUpdate((value) =>
            {
                target.fillAmount = value;
                target.color = Color.Lerp(start, end, value);
            });
        }
        private void ColorMaterialControl(LTDescr tween, Material target, Color start, Color end)
        {
            tween.setOnUpdate((value) =>
            {
                Color color = target.color;
                target.color = Color.Lerp(start, end, value);
            });
        }
    }
    [Serializable]
    public class TweenHandlerField
    {
        [SerializeField] private List<TweenField> m_TweenList;

        public void Init()
        {
            foreach (var tween in m_TweenList)
            {
                tween.Init();
            }
        }
        private TweenField GetTween(string id)
        {
            TweenField tw = m_TweenList.Find(x => x.GetId().Equals(id));
            return tw;
        }

        public void PlayTween(string id, bool isReverse)
        {
            TweenField tw = GetTween(id);
            foreach (var tween in m_TweenList)
            {
                if (tween.GetGroup() == tw.GetGroup())
                {
                    tween.SetIsReverse(isReverse);
                    tween.PlayTween();
                }
            }
            tw.PlayTween();
        }
        public void CancelTween(string id)
        {
            TweenField tw = GetTween(id);
            tw.CancelTween();
        }
        public void CancelAllTween()
        {
            foreach(var tween in m_TweenList)
            {
                tween.CancelTween();
            }
        }
    }
    public class TweenHandler : MonoBehaviour
    {
        [SerializeField] private TweenHandlerField m_TweenHandlerField;

        private void Awake()
        {
            m_TweenHandlerField.Init();
        }
        public void StartTween(string id)
        {
            CancelAllTweenInternal();
            m_TweenHandlerField.PlayTween(id, false);
        }
        public void ReverseTween(string id)
        {
            m_TweenHandlerField.PlayTween(id, true);
        }
        public void CancelTween(string id)
        {
            m_TweenHandlerField.CancelTween(id);
        }
        public void CancelAllTween()
        {
            CancelAllTweenInternal();
        }
        private void CancelAllTweenInternal()
        {
            m_TweenHandlerField.CancelAllTween();
        }
    }
}

