using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class FriendLobbyListUI : MonoBehaviour
{
    [SerializeField] private LobbyManager lobbyManager;
    [SerializeField] private Transform contentRoot; // a VerticalLayoutGroup
    [SerializeField] private Button buttonPrefab;   // has a Text or TMP_Text child

    private readonly List<Button> pool = new();

    public void Refresh()
    {
        var lobbies = lobbyManager.GetJoinableFriendLobbies();

        // grow pool if needed
        while (pool.Count < lobbies.Count)
            pool.Add(Instantiate(buttonPrefab, contentRoot));

        // bind rows
        int i = 0;
        for (; i < lobbies.Count; i++)
        {
            var entry = lobbies[i];
            var btn = pool[i];
            btn.gameObject.SetActive(true);

            var tmp = btn.GetComponentInChildren<TMPro.TMP_Text>();
            if (tmp) tmp.text = $"Join {entry.FriendName}";
            var legacy = btn.GetComponentInChildren<Text>();
            if (legacy) legacy.text = $"Join {entry.FriendName}";

            var lobbyId = entry.LobbyId; // capture for onClick
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => lobbyManager.JoinLobby(lobbyId));
        }

        // hide extras
        for (; i < pool.Count; i++)
            pool[i].gameObject.SetActive(false);
    }
}
