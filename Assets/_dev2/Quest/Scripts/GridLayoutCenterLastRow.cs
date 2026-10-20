using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace EduGame
{
    /// <summary>
    /// Pendamping GridLayoutGroup: baris terakhir yang tidak penuh ditaruh di tengah,
    /// bukan menempel di sisi awal grid.
    ///
    ///   _ _ _            _ _ _
    ///   _ _      ->       _ _
    ///
    /// Cara pakai:
    /// - Pasang di GameObject yang sama dengan GridLayoutGroup.
    /// - Letakkan DI BAWAH GridLayoutGroup di Inspector (urutan component menentukan
    ///   siapa yang dihitung duluan; component ini harus setelah grid-nya).
    /// - Child yang non-aktif atau ignoreLayout = true tidak dihitung, sama seperti GridLayoutGroup.
    /// - Kalau Start Axis grid = Vertical, yang ditengahkan adalah kolom terakhir (secara vertikal).
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(GridLayoutGroup))]
    [AddComponentMenu("Layout/Grid Layout Center Last Row")]
    public class GridLayoutCenterLastRow : MonoBehaviour, ILayoutController
    {
        private GridLayoutGroup m_Grid;
        private RectTransform m_Rect;
        private readonly List<RectTransform> m_Items = new List<RectTransform>();
        private readonly List<Component> m_Ignorers = new List<Component>();

        private GridLayoutGroup Grid => m_Grid ? m_Grid : (m_Grid = GetComponent<GridLayoutGroup>());
        private RectTransform Rect => m_Rect ? m_Rect : (m_Rect = transform as RectTransform);

public void SetLayoutHorizontal()
        {
            // GridLayoutGroup baru menaruh posisi X dan Y semua child saat pass vertikal,
            // jadi offset harus diterapkan setelah itu (di SetLayoutVertical).
        }

public void SetLayoutVertical()
        {
            Apply();
        }

        private void OnEnable()
        {
            MarkDirty();
        }

        private void OnDisable()
        {
            // hitung ulang tanpa offset dari component ini
            MarkDirty();
        }

        private void MarkDirty()
        {
            if (Rect != null)
                LayoutRebuilder.MarkLayoutForRebuild(Rect);
        }

private void Apply()
        {
            GridLayoutGroup grid = Grid;
            if (grid == null || !grid.isActiveAndEnabled) return;

            // startAxis Horizontal: baris terakhir digeser di sumbu X
            // startAxis Vertical  : kolom terakhir digeser di sumbu Y
            bool horizontal = grid.startAxis == GridLayoutGroup.Axis.Horizontal;
            int axis = horizontal ? 0 : 1;

            CollectItems();
            int count = m_Items.Count;
            int perLine = GetCellsPerLine(count, horizontal);
            if (count <= perLine) return;            // cuma satu baris/kolom
            int lastCount = count % perLine;
            if (lastCount == 0) return;              // baris terakhir penuh

            float step = axis == 0 ? grid.cellSize.x + grid.spacing.x : grid.cellSize.y + grid.spacing.y;
            float shift = (perLine - lastCount) * step * 0.5f;

            int corner = (int)grid.startCorner;
            float direction;
            if (axis == 0)
                direction = corner % 2 == 0 ? 1f : -1f;   // mulai dari kiri: geser kanan, dari kanan: geser kiri
            else
                direction = corner / 2 == 0 ? -1f : 1f;   // mulai dari atas: geser bawah, dari bawah: geser atas

            for (int i = count - lastCount; i < count; i++)
            {
                Vector2 pos = m_Items[i].anchoredPosition;
                pos[axis] += shift * direction;
                m_Items[i].anchoredPosition = pos;
            }
        }

        // Sama seperti GridLayoutGroup: child non-aktif dan yang ignoreLayout tidak dihitung
        private void CollectItems()
        {
            m_Items.Clear();
            RectTransform rect = Rect;
            for (int i = 0; i < rect.childCount; i++)
            {
                RectTransform child = rect.GetChild(i) as RectTransform;
                if (child == null || !child.gameObject.activeInHierarchy) continue;

                child.GetComponents(typeof(ILayoutIgnorer), m_Ignorers);
                bool included = m_Ignorers.Count == 0;
                for (int j = 0; j < m_Ignorers.Count && !included; j++)
                {
                    if (!((ILayoutIgnorer)m_Ignorers[j]).ignoreLayout)
                        included = true;
                }

                if (included)
                    m_Items.Add(child);
            }
        }

        // Jumlah cell dalam satu baris (startAxis Horizontal) / satu kolom (Vertical),
        // dihitung sama seperti GridLayoutGroup.
        private int GetCellsPerLine(int count, bool horizontal)
        {
            GridLayoutGroup g = Grid;
            Vector2 size = Rect.rect.size;
            Vector2 cell = g.cellSize;
            Vector2 spacing = g.spacing;
            RectOffset pad = g.padding;

            int countX = 1;
            int countY = 1;
            switch (g.constraint)
            {
                case GridLayoutGroup.Constraint.FixedColumnCount:
                    countX = g.constraintCount;
                    if (count > countX) countY = Mathf.CeilToInt(count / (float)countX);
                    break;
                case GridLayoutGroup.Constraint.FixedRowCount:
                    countY = g.constraintCount;
                    if (count > countY) countX = Mathf.CeilToInt(count / (float)countY);
                    break;
                default: // Flexible
                    countX = cell.x + spacing.x <= 0
                        ? int.MaxValue
                        : Mathf.Max(1, Mathf.FloorToInt((size.x - pad.horizontal + spacing.x + 0.001f) / (cell.x + spacing.x)));
                    countY = cell.y + spacing.y <= 0
                        ? int.MaxValue
                        : Mathf.Max(1, Mathf.FloorToInt((size.y - pad.vertical + spacing.y + 0.001f) / (cell.y + spacing.y)));
                    break;
            }

            return Mathf.Max(1, horizontal ? countX : countY);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            Component[] comps = GetComponents<Component>();
            int gridIndex = System.Array.IndexOf(comps, GetComponent<GridLayoutGroup>());
            int myIndex = System.Array.IndexOf(comps, this);
            if (gridIndex > myIndex)
            {
                Debug.LogWarning("GridLayoutCenterLastRow harus berada DI BAWAH GridLayoutGroup di Inspector " +
                                 "(Move Down), kalau tidak posisinya ditimpa lagi oleh grid.", this);
            }
        }
#endif
    }
}
