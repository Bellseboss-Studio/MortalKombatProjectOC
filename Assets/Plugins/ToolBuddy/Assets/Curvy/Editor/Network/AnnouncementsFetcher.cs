// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

//#define CURVY_SHOW_ALL_ANNOUNCEMENTS

using System;
using System.Collections.Generic;
using System.Linq;
using FluffyUnderware.Curvy;
using FluffyUnderware.Curvy.Utils;
using FluffyUnderware.DevTools;
using FluffyUnderware.DevTools.Extensions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace FluffyUnderware.CurvyEditor.Network
{
    /// <summary>
    /// Fetches announcements from server and display them if not previously displayed
    /// </summary>
    [InitializeOnLoad]
    internal class AnnouncementsFetcher
    {
        [Serializable]
        private class Announcement
        {
#pragma warning disable 0649
            public string Id;
            public string Title;
            public string Content;
#pragma warning restore 0649
        }

        private UnityWebRequest WebRequest { get; set; }

        static AnnouncementsFetcher()
        {
            if (CurvyProject.Instance.EnableAnnouncements == false)
                return;

            const string preferenceName = "LastFetchedAnnouncementDate";
            int utcNowHours = (int)(DateTime.UtcNow.Ticks / (10000L * 1000L * 3600L));

            if (ShouldFetch(
                    preferenceName,
                    utcNowHours
                ))
            {
                new AnnouncementsFetcher().Fetch();
                CurvyProject.Instance.SetEditorPrefs(
                    preferenceName,
                    utcNowHours
                );
            }
            else
            {
#if CURVY_DEBUG
                Debug.Log("Ignored news fetching");
#endif
            }
        }

        private void Fetch()
        {
            string url = "https://announcements.curvyeditor.com/?version=" + AssetInformation.Version;

#if CURVY_DEBUG
            Debug.Log(url);
#endif

            WebRequest = UnityWebRequest.Get(url);
            WebRequest.SendWebRequest();
            EditorApplication.update += CheckWebRequest;
        }

        private void CheckWebRequest()
        {
            if (WebRequest.isDone)
            {
                EditorApplication.update -= CheckWebRequest;
                if (WebRequest.IsError())
                {
                    WebRequest.Dispose();
#if CURVY_DEBUG
                    Debug.LogError("Error: " + WebRequest.error);
#endif
                }
                else
                {
                    string downloadHandlerText = WebRequest.downloadHandler.text;
                    WebRequest.Dispose();
#if CURVY_DEBUG
                    Debug.Log("Received: " + downloadHandlerText);
#endif
                    if (String.IsNullOrEmpty(downloadHandlerText) == false)
                        ProcessAnnouncements(downloadHandlerText);
                }
            }
        }

        private static void ProcessAnnouncements(string responseText)
        {
            const string preferenceName = "ProcessedAnnouncements";
            try
            {
                SerializableArray<Announcement> announcements =
                    JsonUtility.FromJson<SerializableArray<Announcement>>(responseText);
                string[] shownAnnouncements = CurvyProject.Instance.GetEditorPrefs(preferenceName);
                IEnumerable<Announcement> reversedAnnouncements =
                    announcements.Array.Reverse(); //Reversed so that the first announcement's window is shown first
                int newsIndex = 0;
                foreach (Announcement announcement in reversedAnnouncements)
                    if (ShouldShowAnnouncement(
                            shownAnnouncements,
                            announcement
                        ))
                    {
                        AnnouncementWindow.Open(
                            announcement.Title,
                            announcement.Content,
                            new Vector2(
                                newsIndex * 20,
                                newsIndex * 20
                            )
                        );
                        DTLog.Log(
                            String.Format(
                                "[Curvy] Announcement: {0}: {1}",
                                announcement.Title,
                                announcement.Content
                            )
                        );
                        newsIndex++;
                        CurvyProject.Instance.SetEditorPrefs(
                            preferenceName,
                            shownAnnouncements.Add(announcement.Id)
                        );
                    }
                    else
                    {
#if CURVY_DEBUG
                        Debug.Log("Already shown announcement " + announcement.Id);
#endif
                    }
            }

#if CURVY_DEBUG
            catch (ArgumentException e) // exception can be thrown by JsonUtility.FromJson
            {
                Debug.LogException(e);
            }
#else
            catch (ArgumentException) // exception can be thrown by JsonUtility.FromJson
            { }
#endif
        }

        private static bool ShouldFetch(string preferenceName, int utcNowHours)
        {
#if CURVY_SHOW_ALL_ANNOUNCEMENTS
            return true;
#else
            int lastFetchedAnnouncementDate =
                CurvyProject.Instance.GetEditorPrefs(
                    preferenceName,
                    17522856
                ); // is the number of hours in the DateTime equivalent to the 1th of January 2000
            int deltaHours = utcNowHours - lastFetchedAnnouncementDate;
            bool shouldFetch = deltaHours > 24;
            return shouldFetch;
#endif
        }

        private static bool ShouldShowAnnouncement(string[] shownAnnouncements, Announcement announcement)
        {
#if CURVY_SHOW_ALL_ANNOUNCEMENTS
            return true;
#else
            return shownAnnouncements.Contains(announcement.Id) == false;
#endif
        }
    }
}