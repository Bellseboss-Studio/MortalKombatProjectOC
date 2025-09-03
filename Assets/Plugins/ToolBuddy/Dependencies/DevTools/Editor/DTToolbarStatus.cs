using UnityEngine;

namespace FluffyUnderware.DevToolsEditor
{
    public class DTToolbarStatus : DTStatusbar
    {
        protected override void GetColors()
        {
            GUI.contentColor = new Color(
                0,
                0,
                0,
                0.75f
            );
        }

        protected override GUIStyle GetStyle()
        {
            GUIStyle style = base.GetStyle();
            style.alignment = TextAnchor.MiddleCenter;
            Texture2D bgTex = new Texture2D(
                1,
                1
            );
            bgTex.SetPixel(
                0,
                0,
                new Color(
                    1,
                    1,
                    1,
                    0.5f
                )
            );
            bgTex.Apply();
            bgTex.hideFlags = HideFlags.DontSave;
            style.normal.background = bgTex;
            return style;
        }
    }
}