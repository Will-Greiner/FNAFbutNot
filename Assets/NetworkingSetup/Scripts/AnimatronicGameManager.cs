using System.Collections.Generic;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using UnityEngine;

public class AnimatronicGameManager : NetworkBehaviour
{
    public static AnimatronicGameManager Instance { get; private set; }

    private readonly List<NetworkObject> _aliveAnimatronics = new();

    [SerializeField] private GameObject animatronicDeathFxPrefab;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // Called by an Animatronic component when it spawns/despawns.
    public void RegisterAnimatronic(NetworkObject no)
    {
        if (!IsServer || no == null) return;
        if (!_aliveAnimatronics.Contains(no))
            _aliveAnimatronics.Add(no);
    }

    public void UnregisterAnimatronic(NetworkObject no)
    {
        if (!IsServer || no == null) return;
        _aliveAnimatronics.Remove(no);
    }

    [ClientRpc]
    private void PlayDeathFxClientRpc(Vector3 position)
    {
        if (animatronicDeathFxPrefab == null)
            return;

        Instantiate(animatronicDeathFxPrefab, position, Quaternion.identity);
    }

    /// <summary>
    /// Called by ShotgunAttack on the SERVER when an animatronic is shot.
    /// Handles despawn, camera cycling, and gameover.
    /// </summary>
    public void KillAnimatronic(NetworkObject animNO)
    {
        if (!IsServer || animNO == null) return;

        Vector3 deathPos = animNO.transform.position;

        PlayDeathFxClientRpc(deathPos);

        // Determine index before removal
        int index = _aliveAnimatronics.IndexOf(animNO);
        if (index < 0)
        {
            // Not tracked yet; add then treat as single-element list
            _aliveAnimatronics.Add(animNO);
            index = 0;
        }

        ulong victimClientId = animNO.OwnerClientId;

        // Remove & despawn this animatronic
        _aliveAnimatronics.Remove(animNO);
        if (animNO.IsSpawned)
            animNO.Despawn(true);

        // If any animatronics remain, make the victim spectate the next one
        if (_aliveAnimatronics.Count > 0)
        {
            int nextIndex = index % _aliveAnimatronics.Count; // wraps
            var nextNO = _aliveAnimatronics[nextIndex];
            var targetRef = new NetworkObjectReference(nextNO);

            var rpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new[] { victimClientId }
                }
            };

            SetSpectatorTargetClientRpc(targetRef, rpcParams);
        }
        else
        {
            // No animatronics left – trigger gameover for everyone
            GameOverClientRpc();
        }
    }

    [ClientRpc]
    private void SetSpectatorTargetClientRpc(NetworkObjectReference targetRef, ClientRpcParams clientRpcParams = default)
    {
        if (SpectatorCameraController.Instance == null)
            return;

        if (targetRef.TryGet(out NetworkObject targetNO))
        {
            SpectatorCameraController.Instance.SetTarget(targetNO.transform);
            SpectatorCameraController.Instance.SpawnAndAttachUI();
            SpectatorCameraController.Instance.ShowSpectatorUI(true);
        }
    }

    [ClientRpc]
    private void GameOverClientRpc()
    {
        //if (GameOverUIController.Instance != null)
        //{
        //    GameOverUIController.Instance.ShowGameOver();
        //}

        //if (SceneMusicPlayer.Instance != null)
        //{
        //    SceneMusicPlayer.Instance.PlayEndGameMusic();
        //}

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsClient)
        {
            NetworkManager.Singleton.Shutdown();
        }

        // Load the next scene (everyone runs this because it's a ClientRpc)
        SceneManager.LoadScene("GameOver");
    }
}
