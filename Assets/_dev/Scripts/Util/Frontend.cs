using UnityEngine;
using UnityEngine.SceneManagement;

public class Frontend : MonoBehaviour
{
    public void Goto(string scene)
    {
        SceneManager.LoadScene(scene);
    }
}
