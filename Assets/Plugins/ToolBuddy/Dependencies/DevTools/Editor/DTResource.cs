// =====================================================================
// Copyright 2013-2017 Fluffy Underware
// All rights reserved
// 
// http://www.fluffyunderware.com
// =====================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using System.Reflection;
using FluffyUnderware.DevTools;
using FluffyUnderware.DevTools.Extensions;

namespace FluffyUnderware.DevToolsEditor
{
    /// <summary>
    /// Class for loading image resources
    /// </summary>
    public abstract class DTResource
    {
        public Assembly ResourceDLL;
        public string ResourceNamespace = string.Empty;

        /// <summary>
        /// Load an image defined by a packed string either from the Resource DLL or the Editor/Resources folder
        /// </summary>
        /// <param name="packedstring">string formed name (including namespace),width,height. E.g. "Assets/Editor/myIcon,16,16"</param>
        /// <returns>a Texture</returns>
        public virtual Texture2D LoadPacked(string packedstring,Assembly assembly=null, string resourcePath="")
        {
            if (!string.IsNullOrEmpty(packedstring))
            {
                string[] s = packedstring.Split(',');
                if (s.Length == 3)
                {
                    try
                    {
                        int w = int.Parse(s[1], System.Globalization.CultureInfo.InvariantCulture);
                        int h = int.Parse(s[2], System.Globalization.CultureInfo.InvariantCulture);
                        return Load(s[0], w, h, assembly, resourcePath);
                    }
                    catch (Exception e) { Debug.LogException(e);}
                }
            }
            return null;
        }
        
        /// <summary>
        /// Load an image either from a Resource DLL or the Editor/Resources folder
        /// </summary>
        /// <param name="assembly">the resource DLL assembly</param>
        /// <param name="resourcePath">the resource path(namespace)</param>
        /// <param name="resourceName">name of the resource file (without extension)</param>
        /// <param name="width">width of the image (can be omitted when loading from texture file)</param>
        /// <param name="height">height of the image (can be omitted when loading from texture file)</param>
        /// <returns>a Texture</returns>
        public virtual Texture2D Load(
            string resourceName,
            int width = 0,
            int height = 0,
            Assembly assembly = null,
            string resourcePath = "")
        {
            if (assembly == null)
                assembly = ResourceDLL;
            if (string.IsNullOrEmpty(resourcePath))
                resourcePath = ResourceNamespace;

            if (string.IsNullOrEmpty(Path.GetExtension(resourceName)))
                resourceName = $"{resourceName}.png";

            //if (!resourcePath.EndsWith("."))
            //    resourcePath += ".";

            Texture2D texture = null;
            if (assembly != null)
                using (Stream myStream = assembly.GetManifestResourceStream($"{resourcePath}{resourceName}"))
                {
                    if (myStream != null)
                    {
                        texture = new Texture2D(
                            width,
                            height,
                            TextureFormat.ARGB32,
                            false
                        );
                        texture.LoadImage(ReadToEnd(myStream));
                    }
                }

            if (texture == null)
                // Try from file sys:
                texture = (Texture2D)Resources.Load($"Editor/{Path.GetFileNameWithoutExtension(resourceName)}");

            if (texture == null)
                DTLog.LogWarning("Missing resource: " + resourcePath + resourceName);
            else
                texture.hideFlags = HideFlags.DontSave;


            return texture;
        }

        protected static byte[] ReadToEnd(System.IO.Stream stream)
        {
            long originalPosition = stream.Position;
            stream.Position = 0;

            try
            {
                byte[] readBuffer = new byte[4096];

                int totalBytesRead = 0;
                int bytesRead;

                while ((bytesRead = stream.Read(readBuffer, totalBytesRead, readBuffer.Length - totalBytesRead)) > 0)
                {
                    totalBytesRead += bytesRead;

                    if (totalBytesRead == readBuffer.Length)
                    {
                        int nextByte = stream.ReadByte();
                        if (nextByte != -1)
                        {
                            byte[] temp = new byte[readBuffer.Length * 2];
                            Buffer.BlockCopy(readBuffer, 0, temp, 0, readBuffer.Length);
                            Buffer.SetByte(temp, totalBytesRead, (byte)nextByte);
                            readBuffer = temp;
                            totalBytesRead++;
                        }
                    }
                }

                byte[] buffer = readBuffer;
                if (readBuffer.Length != totalBytesRead)
                {
                    buffer = new byte[totalBytesRead];
                    Buffer.BlockCopy(readBuffer, 0, buffer, 0, totalBytesRead);
                }
                return buffer;
            }
            finally
            {
                stream.Position = originalPosition;
            }
        }

        public static Assembly FindResourceDLL(string name)
        {
            //BUG will return the wrong dll if there is another dll that starts with the same name
            return TypeExt.GetLoadedAssemblies().FirstOrDefault(asm => asm.FullName.StartsWith(name));
        }
    }

}
