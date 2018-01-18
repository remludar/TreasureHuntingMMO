using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class NetManager : NetworkManager
{
    public int itemID = 0;
    public List<GameObject> itemsToSpawn = new List<GameObject>();

    bool areItemsSpawned = false;

    void Awake()
    {
        _LoadSpawnablePrefabs();
    }

    public override void OnServerConnect(NetworkConnection connection)
    {
        foreach (GameObject go in itemsToSpawn)
        {
            NetworkServer.Spawn(go);
        }
    }

    private void _LoadSpawnablePrefabs()
    {
        //spawnPrefabs.Add(Resources.Load("Prefabs/Items/Fishing Rod") as GameObject);
        spawnPrefabs.Add(Resources.Load("Prefabs/Items/Sphere") as GameObject);

        foreach (GameObject go in spawnPrefabs)
            ClientScene.RegisterPrefab(go);
    }

    public string UpdateItemName(GameObject go)
    {
        return go.name + itemID++;
    }
    public void Output(string message)
    {
        Debug.Log(message);
    }
}
