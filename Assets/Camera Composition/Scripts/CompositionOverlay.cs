/*  This file is part of the "Camera Composition" editor tool by Jordan Cassady.
 *  You are only permitted to use this software if purchased and downloaded from
 *  the Unity Asset Store. You shall not sell, license, transfer, distribute or
 *  otherwise make this software available to any third party.
 */

 // You are only permitted to use this package if purchased and downloaded 

using UnityEngine;
using UnityEngine.UI;

namespace JordanCassady
{
    /// <summary>
    /// Provide access to the composition overlay properties and methods for
    /// manipulating the Image component from the editor window.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class CompositionOverlay : MonoBehaviour
    {
        #region PROPERTIES
        public bool IsActive { get { return GetComponent<Image>().enabled; } }
        public float Opacity { get { return GetComponent<Image>().color.a; } }
        #endregion

        public void Activate(bool activate)
        {
            GetComponent<Image>().enabled = activate;
        }

        /// <summary>
        /// Invert line color from white to black or vice versa.
        /// </summary>
        /// <param name="invert"></param>
        /// <returns></returns>
        public bool InvertLineColor(bool invert)
        {
            if (invert)
            {
                GetComponent<Image>().color = Color.black;
            } else {
                GetComponent<Image>().color = Color.white;
            }
            return invert;
        }

        /// <summary>
        /// Update the grid overlay opacity by changing the Image alpha value.
        /// </summary>
        /// <param name="alpha"></param>
        public void UpdateOpacity(float alpha)
        {
            var image = GetComponent<Image>();
            GetComponent<Image>().color = new Color(image.color.r, image.color.g, image.color.b, alpha);
        }

        /// <summary>
        /// Update the orientation of the overlay image.
        /// </summary>
        /// <param name="orientation"></param>/
        public void Position(string orientation)
        {
            if (orientation == "Bottom Right")
            {
                GetComponent<Image>().transform.rotation = Quaternion.Euler(0, 0, 0);
            }
            else if (orientation == "Bottom Left")
            {
                GetComponent<Image>().transform.rotation = Quaternion.Euler(-180, 0, -180);
            }
            else if (orientation == "Top Right")
            {
                GetComponent<Image>().transform.rotation = Quaternion.Euler(-180, 0, 0);
            }
            else if (orientation == "Top Left")
            {
                GetComponent<Image>().transform.rotation = Quaternion.Euler(0, 0, -180);
            }
        }
    }
}
