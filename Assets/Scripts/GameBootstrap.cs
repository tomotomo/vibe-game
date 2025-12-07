using UnityEngine;

namespace Daifugo
{
    public static class GameBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void InitializeGame()
        {
            if (GameObject.FindObjectOfType<DaifugoGame>() == null)
            {
                GameObject gameObj = new GameObject("DaifugoGame");
                gameObj.AddComponent<DaifugoGame>();
            }
        }
    }
}
