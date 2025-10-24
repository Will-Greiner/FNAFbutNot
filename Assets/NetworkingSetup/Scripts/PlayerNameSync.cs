using UnityEngine;
using Unity.Netcode;
using TMPro;
using Unity.Collections;
using Steamworks;

public class PlayerNameSync : NetworkBehaviour
{
    [SerializeField] private TMP_Text nameText;

    //Owner can write; everyone reads. Replicates to all clients
    private NetworkVariable<FixedString64Bytes> displayName = new NetworkVariable<FixedString64Bytes>(writePerm: NetworkVariableWritePermission.Owner);

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            displayName.Value = SteamFriends.GetPersonaName();
            nameText.text = displayName.Value.ToString();
        }
    }
}
