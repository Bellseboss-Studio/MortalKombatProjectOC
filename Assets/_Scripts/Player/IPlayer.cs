using UnityEngine;

namespace _Scripts.Player
{
    public interface IPlayer
    {
        GameObject GetGameObject();
        void LoadRageTo(int percentage);
        void LoadLifeTo(int percentage);
    }
}