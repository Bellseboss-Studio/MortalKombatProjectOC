using _Scripts.Interact;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FinalGame : Activable
{    
    [SerializeField] private int sceneIndex;
    public override void Activate()
    {
        SceneManager.LoadScene(sceneIndex);
    }
}