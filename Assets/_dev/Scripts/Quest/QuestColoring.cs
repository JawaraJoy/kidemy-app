using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;

namespace EduGame
{
    public class QuestColoring : Quest
    {
        public static Color EmptyColor => new Color(0, 0, 0, 0);

        [Header("Palettes")]
        [SerializeField] [FormerlySerializedAs("colorOptions")] private List<Color> m_ColorOptions;
        [SerializeField] [FormerlySerializedAs("brush")] private Material m_Brush;
        [SerializeField] [FormerlySerializedAs("brushSize")] private float m_BrushSize = 0.1f;

        [Header("Components")]
        [SerializeField] [FormerlySerializedAs("palettesContainer")] private RectTransform m_PalettesContainer;
        [SerializeField] [FormerlySerializedAs("canvasContainer")] private RectTransform m_CanvasContainer;

        [Header("Prefab")]
        [SerializeField] [FormerlySerializedAs("palettePrefab")] private QuestColoringPalette m_PalettePrefab;

        [Header("Cursor")]
        [SerializeField] [FormerlySerializedAs("bindedCursor")] private Sprite m_BindedCursor;

        public Color SelectedColor => m_BindedColor;
        public Material Brush => m_Brush;
        public float BrushSize => m_BrushSize;

        private Color m_BindedColor = EmptyColor;

        private QuestColoringCursor m_ColoringCursor;
        private QuestColoringPalette[] m_Palettes;
        private QuestColoringField[] m_Fields;

        private SO_QuestColoring m_DataQuestColoring;

        void Awake()
        {
            m_ColoringCursor = GetComponent<QuestColoringCursor>();

            if (!m_CanvasContainer)
                Debug.LogError("Canvas not set");

            if (!m_Brush)
                Debug.LogError("Brush not set");

            if (!m_PalettesContainer)
                Debug.LogError("Palettes Container is not found");
        }

        protected override void Start()
        {
            base.Start();

            m_DataQuestColoring = m_Data as SO_QuestColoring;

            if (!m_DataQuestColoring)
                Debug.LogError("Quest data on '" + gameObject.name + "' is not valid, please assign the one with SO_QuestColoring");

            if (m_PalettesContainer && m_PalettePrefab)
            {
                m_Palettes = new QuestColoringPalette[m_ColorOptions.Count];

                for (int i = 0; i < m_ColorOptions.Count; i++)
                {
                    m_Palettes[i] = InstantiatePalette(m_ColorOptions[i]);
                    m_Palettes[i].SetPaletteColor(m_ColorOptions[i]);
                }

                Invoke("RecalculatePalettesContainer", 0.5f);
            }

            if (m_CanvasContainer)
            {
                InstantiateColoringCanvas();

                m_Fields = m_CanvasContainer.GetComponentsInChildren<QuestColoringField>();
            }

            SetDialog();
        }

        public void AddColor(Color color)
        {
            if (!m_ColorOptions.Contains(color))
                m_ColorOptions.Add(color);
        }

        public void BindColor(Color color)
        {
            m_BindedColor = color;

            // Assign SelectedColor to brush
            m_Brush.SetColor("_BrushColor", m_BindedColor);
            m_Brush.SetFloat("_BrushSize", m_BrushSize);

            if (m_ColoringCursor)
                m_ColoringCursor.SetToCustom(color);

        }

        QuestColoringPalette InstantiatePalette(Color color)
        {
            QuestColoringPalette palette = Instantiate(m_PalettePrefab);

            palette.transform.SetParent(m_PalettesContainer);
            palette.transform.localPosition = Vector3.zero;
            palette.Rect.localScale = Vector3.one;

            return palette;
        }

        RectTransform InstantiateColoringCanvas()
        {
            RectTransform canvas = Instantiate(m_DataQuestColoring.Canvas, m_CanvasContainer);

            canvas.transform.localPosition = Vector3.zero;
            canvas.localScale = Vector3.one;

            return canvas;
        }


        void RecalculatePalettesContainer()
        {
            if (m_Palettes[0].Rect.sizeDelta.x > 0 && m_Palettes[0].Rect.sizeDelta.y > 0)
            {
                GridLayoutGroup gridLayoutGroup = m_PalettesContainer.GetComponent<GridLayoutGroup>();
                if (gridLayoutGroup)
                    gridLayoutGroup.cellSize = m_Palettes[0].Rect.sizeDelta;
            }
        }

        void OnGUI()
        {
            if (m_BindedColor != EmptyColor && m_BindedCursor)
            {
                Vector2 mousePosition = Input.mousePosition;

                Cursor.visible = false;

                GUI.color = m_BindedColor;
                GUI.DrawTexture(new Rect(mousePosition.x - 26, Screen.height - mousePosition.y - 26, 32, 32), m_BindedCursor.texture);
            }
            else
            {
                Cursor.visible = true;
                GUI.color = Color.white;
            }
        }

        public override void Submit(int star = 1)
        {
            if (m_ColoringCursor)
                m_ColoringCursor.ResetToDefault();

            float appraisal = 0;

            if (m_Fields.Length > 0)
            {
                foreach (var field in m_Fields)
                    appraisal += field.Result;

                appraisal = appraisal / m_Fields.Length;
            }

            if (appraisal >= 0.85)
                star = 3;
            else if (appraisal >= 0.5)
                star = 2;

            base.Submit(star);
        }

        public override void Reset()
        {
            if (m_Fields != null && m_Fields.Length > 0)
            {
                foreach (var field in m_Fields)
                    field.Reset();
            }

            base.Reset();
        }

        void SetDialog()
        {
            if (!string.IsNullOrEmpty(m_DataQuestColoring.Text))
                GameManager.Instance.SetNPCDialog(m_DataQuestColoring.Text);
        }
    }
}