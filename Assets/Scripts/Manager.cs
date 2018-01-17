using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Manager : NetworkManager
{
    public int itemID = 0;
    public List<GameObject> spawnedItems = new List<GameObject>();

    void Awake()
    {
        _LoadSpawnablePrefabs();
    }

    #region overrides
    public override void OnClientConnect(NetworkConnection connection)
    {
        ClientScene.Ready(connection);
        ClientScene.AddPlayer(0);
    }
    #endregion

    #region private helpers
    private void _LoadSpawnablePrefabs()
    {
        //spawnPrefabs.Add(Resources.Load("Prefabs/Items/Fishing Rod") as GameObject);
        spawnPrefabs.Add(Resources.Load("Prefabs/Items/Sphere") as GameObject);

        foreach (GameObject go in spawnPrefabs)
            ClientScene.RegisterPrefab(go);
    }
    #endregion

    #region public helpers

    public string UpdateItemName(GameObject go)
    {
        return go.name + itemID++;
    }

    public void Output(string message)
    {
        Debug.Log(message);
    }
    #endregion
}
