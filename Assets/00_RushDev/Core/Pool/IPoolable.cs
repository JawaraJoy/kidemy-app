namespace Rush
{
    public interface IPoolable
    {
        void OnPoolGet();
        void OnPoolRelease();
    }
}