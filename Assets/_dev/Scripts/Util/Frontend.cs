using UnityEngine;
using UnityEngine.SceneManagement;

public class Frontend : MonoBehaviour
{
    // bahaya pake string literal, kalo scene name diubah bakal error, mending pake SceneConfig
    public void Goto(string scene)
    {
        SceneManager.LoadScene(scene);
    }
}
