using _Scripts.Interact;

public abstract class ActivableTutorial : Activable
{
    private bool _isFinished;
    public bool IsFinished => _isFinished;

    protected void Finish()
    {
        _isFinished = true;
    }

    public abstract void Deactivate();
}