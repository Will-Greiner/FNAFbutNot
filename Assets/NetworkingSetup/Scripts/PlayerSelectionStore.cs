using System.Collections.Generic;
using UnityEngine;

// This is static data on the server
public static class PlayerSelectionStore
{
    // Server side
    private static readonly Dictionary<ulong, int> chosenPrefabIndex = new();

    public static void SetChosenPrefabIndex(ulong clientId, int index)
    {
        chosenPrefabIndex[clientId] = index;
    }

    public static int GetChosenPrefabIndex(ulong clientId)
    {
        if (chosenPrefabIndex.TryGetValue(clientId, out var index)) 
            return index;

        // Default to 0 if nothing stored
        return 0;

    }

    public static void Clear(ulong clientId)
    {
        chosenPrefabIndex.Remove(clientId);
    }
}
