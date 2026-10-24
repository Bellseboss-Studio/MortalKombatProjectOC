using UnityEngine;

public class TooltipToInteractable : MonoBehaviour
{
    [SerializeField] private GameObject tooltipGameObject;
    private bool _isTooltipActive;
    [SerializeField] private float limitLeft;
    [SerializeField] private float limitRight;
    [SerializeField] private bool canUse;

    public void Configurate(InteractiveObjectWithButton interactiveManager)
    {
        tooltipGameObject.SetActive(false);
        if (!canUse) return;
        interactiveManager.OnActionEnter += ShowTooltip;
        interactiveManager.OnActionExit += HideTooltip;
    }

    private void HideTooltip()
    {
        _isTooltipActive = false;
        tooltipGameObject.SetActive(false);
    }

    private void ShowTooltip()
    {
        _isTooltipActive = true;
        tooltipGameObject.SetActive(true);
    }

    private void Update()
    {
        if (!_isTooltipActive) return;
        //lerp -45 to 45 gradus in axis Y
        tooltipGameObject.transform.rotation = Quaternion.Euler(90, Mathf.Lerp(limitLeft, limitRight, Mathf.PingPong(Time.time, 1)), 0);
        
    }
}