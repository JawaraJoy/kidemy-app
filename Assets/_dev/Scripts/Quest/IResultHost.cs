namespace EduGame
{
    /// <summary>
    /// Layanan yang disuntikkan owner (GameManager) ke panel hasil. ResultPanel dan StarPresenter hanya mengenal interface ini.
    /// </summary>
    public interface IResultHost
    {
        ChallengeConfig ChallengeConfig { get; }

        void ReloadScene();
        void GoHome();
        void SetRightAsnwer(int set);
    }
}
