using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Bellseboss.Angel.KeyRebinding
{
    public class RebindingKeyManager : MonoBehaviour
    {
        [SerializeField] public InputActionAsset actions;
        [SerializeField] private List<InputActionReference> moveRef;
        [SerializeField] private GameObject content;
        private bool _isOpen = false;

        private void Awake()
        {
            var rebinds = PlayerPrefs.GetString("rebinds");
            if (!string.IsNullOrEmpty(rebinds))
                actions.LoadBindingOverridesFromJson(rebinds);
            
        }

        public void OnOpenCloseKeyBindingMenu(InputAction.CallbackContext ctx)
        {
            if (ctx.performed)
            {
                OpenCloseKeyBindingMenu();
            }
        }

        public void OpenCloseKeyBindingMenu()
        {

            _isOpen = !_isOpen;
            /*content.SetActive(_isOpen);*/
            if (_isOpen)
            {
                foreach (var reference in moveRef)
                {
                    reference.action.Disable();
                }
            }
            else
            {
                foreach (var reference in moveRef)
                {
                    reference.action.Enable();
                }
                var rebinds = actions.SaveBindingOverridesAsJson();
                PlayerPrefs.SetString("rebinds", rebinds);
            }
        }
    }
}