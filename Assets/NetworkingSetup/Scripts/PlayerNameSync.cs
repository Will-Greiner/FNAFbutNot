using UnityEngine;
using Unity.Netcode;
using TMPro;
using Unity.Collections;
using Steamworks;

public class PlayerNameSync : NetworkBehaviour
{
    [SerializeField] private TMP_Text nameText;

    // Owner writes, everyone reads
    private readonly NetworkVariable<FixedString64Bytes> displayName =
        new(writePerm: NetworkVariableWritePermission.Owner);

    public override void OnNetworkSpawn()
    {
        // 1) Update UI whenever the value changes (on all clients)
        displayName.OnValueChanged += OnNameChanged;

        // 2) Apply the current value right now (handles late joiners)
        OnNameChanged(default, displayName.Value);

        // 3) Owner sets their Steam name once
        if (IsOwner)
        {
            FixedString64Bytes n = SteamFriends.GetPersonaName();
            displayName.Value = n;
        }
    }

    public override void OnNetworkDespawn()
    {
        displayName.OnValueChanged -= OnNameChanged;
    }

    private void OnNameChanged(FixedString64Bytes oldValue, FixedString64Bytes newValue)
    {
        if (nameText != null)
            nameText.text = newValue.ToString();
    }
}
