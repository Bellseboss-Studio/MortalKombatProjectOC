using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;

public class UIEventsTest : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public void OnMouseOver()
    {
       // Debug.Log("MouseOver");
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // Debug.Log("MouseEnter");
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // Debug.Log("MouseExit");
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        throw new System.NotImplementedException();
    }
}
