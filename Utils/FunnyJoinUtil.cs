using Steamworks.Data;
using System.Collections;
using UnityEngine;

namespace CompatibilityChecker.Utils
{
    class FunnyJoinUtil
    {
        public static IEnumerator StartHost()
        {
            yield return new WaitUntil(() => ModNotifyBase.loadedMods);
            GameNetworkManager.Instance.StartHost();
            yield break;
        }

        public static IEnumerator JoinLobbyAfterVerifying(Lobby lobby)
        {
            MenuManager menuManager = Object.FindObjectOfType<MenuManager>();
            menuManager?.serverListUIContainer.SetActive(false);
            menuManager?.menuButtons.SetActive(true);
            yield return new WaitUntil(() => ModNotifyBase.loadedMods);
            LobbySlot.JoinLobbyAfterVerifying(lobby, lobby.Id);
            yield break;
        }
    }
}
