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

    public class DTToolbarRadioOffButton : DTToolbarRadioButton
    {

        public override bool On
        {
            get => base.On;
            set
            {
                if (mOn != value)
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

        public DTToolbarRadioOffButton(string radioGroupName, bool defaultOn=false)
            : base(radioGroupName,defaultOn)
        {
            AllOffAllowed = true;
        }


    }

}
