using MoreMountains.Tools;
using System.Collections.Generic;
using UnityEngine;

namespace Rush
{
    public partial class UpdateBank : Singleton<UpdateBank>
    {
        private readonly Dictionary<GameObject, IFixedUpdater> m_FixedActionTick = new();
        private readonly Dictionary<GameObject, IUpdater> m_UpdateActionTick = new();
        private readonly Dictionary<GameObject, ILateUpdater> m_LateUpdateActionTick = new();

        [SerializeField, MMReadOnly]
        private bool m_ActiveAllTicks = false;
        [SerializeField, MMReadOnly]
        private bool m_ActiveFixedTicks = false;
        [SerializeField, MMReadOnly]
        private bool m_ActiveUpdateTicks = false;
        [SerializeField, MMReadOnly]
        private bool m_ActiveLateUpdateTicks = false;

        private void FixedUpdate()
        {
            ProcessFixedActionTick();
        }

        private void Update()
        {
            ProcessUpdateActionTick();
        }
        private void LateUpdate()
        {
            ProcessLateUpdateActionTick();
        }
        public void SetActiveAllTicks(bool set)
        {
            SetActiveAllTicksInternal(set);
        }
        private void SetActiveAllTicksInternal(bool set)
        {
            m_ActiveAllTicks = set;
        }
        public void SetActiveFixedTicks(bool set)
        {
            m_ActiveFixedTicks = set;
        }
        public void SetActiveUpdateTicks(bool set)
        {
            m_ActiveUpdateTicks = set;
        }
        public void SetActiveLateUpdateTicks(bool set)
        {
            m_ActiveLateUpdateTicks = set;
        }
        private void ProcessFixedActionTick()
        {
            if (m_ActiveAllTicks) return;
            if (m_ActiveFixedTicks) return;
            bool IsAny = m_FixedActionTick.Count > 0;
            if (!IsAny) return;
            foreach (var tick in m_FixedActionTick.Values)
            {
                if (tick.IsActive)
                {
                    tick.FixedTick();
                }
            }
        }
        private void ProcessLateUpdateActionTick()
        {
            if (m_ActiveAllTicks) return;
            if (m_ActiveLateUpdateTicks) return;
            bool IsAny = m_LateUpdateActionTick.Count > 0;
            if (!IsAny) return;
            foreach (var tick in m_LateUpdateActionTick.Values)
            {
                if (tick.IsActive)
                {
                    tick.LateTick();
                }
            }
        }
        private void ProcessUpdateActionTick()
        {
            if (m_ActiveAllTicks) return;
            if (m_ActiveUpdateTicks) return;
            if (m_UpdateActionTick.Count == 0) return;

            var ticks = new List<IUpdater>(m_UpdateActionTick.Values);

            foreach (var tick in ticks)
            {
                if (tick.IsActive)
                {
                    tick.Tick();
                }
            }
        }

        public void RegisterFixedUpdateTick(GameObject key, IFixedUpdater ticker)
        {
            if (!m_FixedActionTick.ContainsKey(key))
            {
                m_FixedActionTick.Add(key, ticker);
            }
        }
        public void RegisterUpdateTick(GameObject key, IUpdater ticker)
        {
            if (!m_UpdateActionTick.ContainsKey(key))
            {
                m_UpdateActionTick.Add(key, ticker);
            }
        }
        public void RegisterLateUpdateTick(GameObject key, ILateUpdater ticker)
        {
            if (!m_LateUpdateActionTick.ContainsKey(key))
            {
                m_LateUpdateActionTick.Add(key, ticker);
            }
        }
        public void UnregisterFixedUpdateTick(GameObject key)
        {
            if (m_FixedActionTick.ContainsKey(key))
            {
                m_FixedActionTick.Remove(key);
            }
        }
        public void UnregisterUpdateTick(GameObject key)
        {
            if (m_UpdateActionTick.ContainsKey(key))
            {
                m_UpdateActionTick.Remove(key);
            }
        }
        public void UnregisterLateUpdateTick(GameObject key)
        {
            if (m_LateUpdateActionTick.ContainsKey(key))
            {
                m_LateUpdateActionTick.Remove(key);
            }
        }
    }
}
