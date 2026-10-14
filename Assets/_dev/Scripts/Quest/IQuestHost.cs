using System.Collections;
using UnityEngine;

namespace EduGame
{
    /// <summary>
    /// Layanan yang disuntikkan owner (GameManager) ke Instance Quest. Quest dan komponennya hanya mengenal interface ini,
    /// bukan GameManager, sehingga prefab Quest tidak terikat ke scene tertentu (aturan Rush #5).
    /// </summary>
    public interface IQuestHost
    {
        AudioSource AudioSource { get; }
        AudioSource Music { get; }
        Canvas Canvas { get; }
        Color ColorTheme { get; }

        void Submit(int star);
        void SetNPC(RuntimeAnimatorController controller);
        void SetNPCDialog(string dialog);
        void InitQuest(SO_Quest questData);
        void ShowTutorial(RectTransform endPos, HandTutorial.TutorialMode mode = HandTutorial.TutorialMode.Click);
        void PlayQuestionVoice();
        void PlaySoundQuest();
        Coroutine StartCoroutine(IEnumerator routine);
    }
}
