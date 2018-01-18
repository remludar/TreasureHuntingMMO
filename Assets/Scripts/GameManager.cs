using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class GameManager : NetworkBehaviour
{
    NetManager networkManager;

    void Awake()
    {
        networkManager = GameObject.FindObjectOfType<NetworkManager>().GetComponent<NetManager>();
    }

    void Start()
    {
        foreach (GameObject go in networkManager.spawnPrefabs)
        {
            var prefab = Instantiate(go, new Vector3(Random.Range(0, 5), Random.Range(1, 5), Random.Range(0, 5)), Quaternion.identity) as GameObject;
            prefab.name = networkManager.UpdateItemName(prefab);
            networkManager.itemsToSpawn.Add(prefab);
        }
    }

}
