// =====================================================================
// Copyright 2013-2017 Fluffy Underware
// All rights reserved
// 
// http://www.fluffyunderware.com
// =====================================================================

using UnityEngine;
using UnityEditor;

namespace FluffyUnderware.DevToolsEditor
{

    public class DTToolbarRadioButton : DTToolbarToggleButton
    {
        public string RadioGroup = string.Empty;

        protected bool AllOffAllowed {get; set;}

        public override bool On
        {
            get => base.On;
            set
            {
                if (mOn!=value)
                {
                    if (value == true)
                        DTToolbar.SetRadioGroupState(this);
                    else if (AllOffAllowed)
                    {
                        mOn = false;
                        OnClick();
                    }
                }
                
            }
        }

        public override bool ShowClientArea => On;

        public DTToolbarRadioButton(string radioGroupName, bool defaultOn=false)
        {
            RadioGroup = radioGroupName;
            AllOffAllowed = false;
            mOn = defaultOn;
        }

        
    }

}
