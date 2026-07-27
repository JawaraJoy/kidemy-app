using UnityEngine;

namespace Rush
{
    public interface IUpdater 
    {
        void Tick();
        bool IsActive { get; }
    }
    public interface IFixedUpdater 
    {
        void FixedTick();
        bool IsActive { get; }
    }
    public interface ILateUpdater 
    {
        void LateTick();
        bool IsActive { get; }
    }
}
