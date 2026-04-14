using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

namespace EduGame
{
    public class QuestColoring : Quest
    {
        public static Color EmptyColor => new Color(0, 0, 0, 0);

        [Header("Palettes")]
        [SerializeField] private List<Color> colorOptions;
        [SerializeField] private Material brush;
        [SerializeField] private float brushSize = 0.1f;

        [Header("Components")]
        [SerializeField] private RectTransform palettesContainer;
        [SerializeField] private RectTransform canvasContainer;

        [Header("Prefab")]
        [SerializeField] private QuestColoringPalette palettePrefab;

        [Header("Cursor")]
        [SerializeField] private Sprite bindedCursor;

        public Color SelectedColor => bindedColor;
        public Material Brush => brush;
        public float BrushSize => brushSize;

        private Color bindedColor = EmptyColor;

        private QuestColoringPalette[] palettes;

        private SO_QuestColoring dataQuestColoring;

        void Awake()
        {
            if (!canvasContainer)
                Debug.LogError("Canvas not set");

            if (!brush)
                Debug.LogError("Brush not set");

            if (!palettesContainer)
                Debug.LogError("Palettes Container is not found");
        }

        protected override void Start()
        {
            base.Start();

            dataQuestColoring = data as SO_QuestColoring;

            if(!dataQuestColoring)
                Debug.LogError("Quest data on '" + gameObject.name + "' is not valid, please assign the one with SO_QuestColoring");

            if (palettesContainer && palettePrefab)
            {
                palettes = new QuestColoringPalette[colorOptions.Count];

                for (int i = 0; i < colorOptions.Count; i++)
                {
                    palettes[i] = InstantiatePalette(colorOptions[i]);
                    palettes[i].SetPaletteColor(colorOptions[i]);
                }

                Invoke("RecalculatePalettesContainer", 0.5f);
            }

            if (canvasContainer)
                InstantiateColoringCanvas();
        }

        public void AddColor(Color color)
        {
            if (!colorOptions.Contains(color))
                colorOptions.Add(color);
        }

        public void BindColor(Color color)
        {
            bindedColor = color;

            // Assign SelectedColor to brush
            brush.SetColor("_BrushColor", bindedColor);
            brush.SetFloat("_BrushSize", brushSize);
        }

        QuestColoringPalette InstantiatePalette(Color color)
        {
            QuestColoringPalette palette = Instantiate(palettePrefab);

            palette.transform.SetParent(palettesContainer);
            palette.transform.localPosition = Vector3.zero;
            palette.Rect.localScale = Vector3.one;

            return palette;
        }

        RectTransform InstantiateColoringCanvas()
        {
            RectTransform canvas = Instantiate(dataQuestColoring.Canvas, canvasContainer);

            canvas.transform.localPosition = Vector3.zero;
            canvas.localScale = Vector3.one;

            return canvas;
        }


        void RecalculatePalettesContainer()
        {
            if (palettes[0].Rect.sizeDelta.x > 0 && palettes[0].Rect.sizeDelta.y > 0)
            {
                GridLayoutGroup gridLayoutGroup = palettesContainer.GetComponent<GridLayoutGroup>();
                if (gridLayoutGroup)
                    gridLayoutGroup.cellSize = palettes[0].Rect.sizeDelta;
            }
        }

        void OnGUI()
        {
            if (bindedColor != EmptyColor && bindedCursor && Mouse.current != null)
            {
                Vector2 mousePosition = Mouse.current.position.ReadValue();

                Cursor.visible = false;

                GUI.color = bindedColor;
                GUI.DrawTexture(new Rect(mousePosition.x - 26, Screen.height - mousePosition.y - 26, 32, 32), bindedCursor.texture);
            }
            else
            {
                Cursor.visible = true;
                GUI.color = Color.white;
            }
        }

        public override void Next()
        {
            base.OnAnswered(true);

            base.Next();
        }
    }
}