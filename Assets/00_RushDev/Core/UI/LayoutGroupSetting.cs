using UnityEngine;
using UnityEngine.UI;

namespace Rush
{
    [System.Serializable]
    public class LayoutGroupSetting
    {
        public enum LayoutMode
        {
            None,
            Vertical,
            Horizontal,
            Grid,
        }

        [SerializeField]
        private LayoutMode m_LayoutMode = LayoutMode.None;
        [SerializeField]
        private TextAnchor m_ChildAlignment = TextAnchor.UpperLeft;
        [SerializeField]
        private HorizontalVerticalSetting m_HorizontalVerticalSetting;
        [SerializeField]
        private GridSetting m_GridSetting;
        private HorizontalLayoutGroup m_RealHorizontalGroup;
        private VerticalLayoutGroup m_VerticalGroup;
        public void Config(Extenable extenable)
        {
            if (extenable.HasExtention(out LayoutGroup layoutGroup))
            {
                layoutGroup.childAlignment = m_ChildAlignment;

                switch(m_LayoutMode)
                {
                    case LayoutMode.None:
                        GameObject.Destroy(layoutGroup);
                        break;
                    case LayoutMode.Horizontal:
                        AddHorizontalGroup(layoutGroup);
                        break;
                    case LayoutMode.Vertical:
                        AddVerticalGroup(layoutGroup);
                        break;
                    case LayoutMode.Grid:
                        break;
                }
            }
        }

        
        private void AddHorizontalGroup(LayoutGroup layout)
        {
            if (layout is HorizontalLayoutGroup horizontal)
            {
                m_RealHorizontalGroup = horizontal;
                
            }
            else
            {
                GameObject newLayout = layout.gameObject;
                GameObject.Destroy(layout);
                m_RealHorizontalGroup = newLayout.AddComponent<HorizontalLayoutGroup>();
            }
            ControlChild(m_RealHorizontalGroup);
        }
        
        private void AddVerticalGroup(LayoutGroup layout)
        {
            if (layout is VerticalLayoutGroup vertical)
            {
                m_VerticalGroup = vertical;
            }
            else
            {
                GameObject newLayout = layout.gameObject;
                GameObject.Destroy(layout);
                m_VerticalGroup = newLayout.AddComponent<VerticalLayoutGroup>();
            }
            ControlChild(m_VerticalGroup);
        }
        
        private void ControlChild(HorizontalOrVerticalLayoutGroup layout)
        {
            layout.spacing = m_HorizontalVerticalSetting.Space;
            layout.childControlWidth = m_HorizontalVerticalSetting.ControlChildSizeWidth;
            layout.childControlHeight = m_HorizontalVerticalSetting.ControlChildSizeHeight;
            layout.childForceExpandWidth = m_HorizontalVerticalSetting.ChildForceToExpandWidth;
            layout.childForceExpandHeight = m_HorizontalVerticalSetting.ChildForceToExpandHeight;
        }
        private void AddGridGroup(LayoutGroup layout)
        {
            if (layout is  GridLayoutGroup grid)
            {

            }
        }

        
    }

    [System.Serializable]
    public class HorizontalVerticalSetting
    {
        [SerializeField]
        private RectOffset m_RectOffset;
        [SerializeField]
        private float m_Space = 0f;
        [SerializeField]
        private bool m_ControlChildSizeWidth = false;
        [SerializeField]
        private bool m_ControlChildSizeHeight = false;
        [SerializeField]
        private bool m_ChildForceToExpandWidth = false;
        [SerializeField]
        private bool m_ChildForceToExpandHeight = false;

        public RectOffset RectOffset => m_RectOffset;
        public float Space => m_Space;
        public bool ControlChildSizeWidth => m_ControlChildSizeWidth;
        public bool ControlChildSizeHeight => m_ControlChildSizeHeight;
        public bool ChildForceToExpandWidth => m_ChildForceToExpandWidth;
        public bool ChildForceToExpandHeight => m_ChildForceToExpandHeight;
    }
    [System.Serializable]
    public class GridSetting
    {
        [SerializeField]
        private Vector2 m_CellSize;
        [SerializeField]
        private Vector2 m_CellSpacing;
        [SerializeField]
        private GridLayoutGroup.Constraint m_Constraint;
        [SerializeField]
        private GridLayoutGroup.Axis m_Axis;
    }
}
