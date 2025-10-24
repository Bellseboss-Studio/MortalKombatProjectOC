using _Scripts.Interact;
using UnityEngine.SceneManagement;

public class FinalGame : Activable
{    
    public override void Activate()
    {
        SceneManager.LoadScene(3);
    }
}