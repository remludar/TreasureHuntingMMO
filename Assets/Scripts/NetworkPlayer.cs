using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using UnityEngine.EventSystems;
using System.Linq;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Canvas))]
public class NetworkPlayer : NetworkBehaviour
{
    NetManager networkManager;
    CharacterController cc;
    Canvas uiCanvas;
    GameObject inventoryGO;

    NotificationTextController notificationController;

    //Motion
    Vector3 motionFromClient;
    Vector3 serverPosition;

    //Rotation
    float rotationFromClient;
    Quaternion serverRotation;

    [SerializeField]
    float moveSpeed = 0.25f;
    [SerializeField]
    float rotateSpeed = 5.0f;
    [SerializeField]
    float gravity = -0.5f;

    void Awake()
    {
        networkManager = GameObject.Find("NetworkManager").GetComponent<NetManager>();
    }
    void Start()
    {
        cc = GetComponent<CharacterController>();
        uiCanvas = GameObject.FindObjectOfType<Canvas>();
        notificationController = GameObject.Find("Notification Text").GetComponent<NotificationTextController>();
        if (isLocalPlayer)
        {
            SetupCamera();
            SetupInventory();
        }
    }
    void Update()
    {
        if (isClient)
        {
            HandleInput();
        }
    }

    void FixedUpdate()
    {
        if (isClient)
        {
            if (isLocalPlayer)
            {
                cc.Move(motionFromClient);
                transform.Rotate(new Vector3(0, rotationFromClient, 0));
            }
            SendDataToServer();
        }

        if (isServer)
        {
            SendDataToClient();
        }
    }



    void SetupCamera()
    {
        //Parent player to camera and set camera starting position
        Camera.main.transform.SetParent(transform);
        Camera.main.transform.position = transform.position + new Vector3(0, 13, -13);
    }
    void SetupInventory()
    {
        //Spawn inventory UI, rotate, scale, and make UICanvas the parent.  
        inventoryGO = Instantiate(Resources.Load("Prefabs/InventoryPanel"), Vector3.zero, Quaternion.identity) as GameObject;
        inventoryGO.transform.SetParent(uiCanvas.gameObject.transform);
        inventoryGO.transform.localScale = new Vector3(1, 1, 1);
        inventoryGO.transform.localPosition = new Vector3(250, -125, 0);
        inventoryGO.SetActive(false);
    }
    void HandleInput()
    {
        //Player Movement
        var horizontal = Input.GetAxis("Horizontal");
        var veritcal = Input.GetAxis("Vertical");
        var cameraForward = new Vector3(Camera.main.transform.forward.x, 0, Camera.main.transform.forward.z);
        var cameraRight = Camera.main.transform.right;
        motionFromClient = ((cameraRight * horizontal) + (cameraForward * veritcal));
        motionFromClient.y = gravity;
        motionFromClient *= moveSpeed;

        //Player Rotation
        if (Input.GetMouseButton(1))
            rotationFromClient = Input.GetAxis("Mouse X");
        else
            rotationFromClient = 0;
        rotationFromClient *= rotateSpeed;

        //UI
        if (Input.GetKeyDown(KeyCode.I))
            inventoryGO.SetActive(!inventoryGO.activeSelf);
    }
    void SendDataToServer()
    {
        CmdSendMotionDataToServer(motionFromClient);
        CmdSendRotationDataToServer(rotationFromClient);
    }
    void SendDataToClient()
    {
        RpcSendPositionToClient(transform.position);
        RpcSendRotationToClient(transform.rotation);
    }

    [Command]
    void CmdSpawnItemsOnServer()
    {
        foreach (GameObject go in networkManager.GetComponent<NetworkManager>().spawnPrefabs)
        {
            var prefab = Instantiate(go, new Vector3(Random.Range(0,5), Random.Range(0, 5), Random.Range(0, 5)), Quaternion.identity) as GameObject;
            prefab.name = networkManager.UpdateItemName(prefab);
            networkManager.itemsToSpawn.Add(prefab);

            //NetworkServer.Spawn(prefab);
            NetworkServer.SpawnWithClientAuthority(prefab, gameObject);
        }
    }

    [Command]
    void CmdSendMotionDataToServer(Vector3 motionFromClient)
    {
        cc.Move(motionFromClient);
        serverPosition = transform.position;    
    }
    [Command]
    void CmdSendRotationDataToServer(float rotationFromClient)
    {
        transform.Rotate(new Vector3(0, rotationFromClient, 0));
        serverRotation = transform.rotation;
    }

    [Command]
    void CmdSendMessageToServer(string message)
    {
        notificationController.AddMessage(message);
    }

    [Command]
    void CmdGetNetworkAuthority(NetworkInstanceId toId)
    {
        GameObject client = NetworkServer.FindLocalObject(toId);
        var conn = client.GetComponent<NetworkIdentity>().connectionToClient;
        var result = GetComponent<NetworkIdentity>().AssignClientAuthority(conn);

        notificationController.AddMessage(client.ToString());
        notificationController.AddMessage(conn.ToString());
        notificationController.AddMessage(result.ToString());
    }

    [Command]
    void CmdSendItemMotionDataToServer(NetworkInstanceId id, Vector3 clientMotion)
    {
        ItemController itemController= null;
        var itemControllers = FindObjectsOfType<ItemController>();
        foreach (ItemController item in itemControllers)
        {
            if (item.netId.Value == id.Value)
            {
                itemController = item;
            }
        }
        itemController.transform.Translate(clientMotion);
        //serverPosition = transform.position;
    }

    [Command]
    void CmdSetItemPropertiesOnServer(NetworkInstanceId playerId, NetworkInstanceId itemId, bool isPlayerControlled, Vector3 clientMotion)
    {
        NetworkPlayer networkPlayer = null;
        var networkPlayers = FindObjectsOfType<NetworkPlayer>();
        foreach (NetworkPlayer player in networkPlayers)
        {
            if (player.netId.Value == playerId.Value)
            {
                networkPlayer = player;
            }
        }

        ItemController itemController = null;
        var itemControllers = FindObjectsOfType<ItemController>();
        foreach (ItemController item in itemControllers)
        {
            if (item.netId.Value == itemId.Value)
            {
                itemController = item;
            }
        }

        if (isPlayerControlled)
        {
            clientMotion = new Vector3(0, 1.25f, 0);
            var colliders = GetComponentsInChildren<CapsuleCollider>();
            for (int i = 0; i < colliders.Length; i++)
            {
                colliders[i].enabled = false;
            }
            itemController.rb.useGravity = false;
            itemController.rb.freezeRotation = true;
            itemController.rb.isKinematic = true;
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
        else
        {
            clientMotion = new Vector3(0, 0, 0);
            var colliders = GetComponentsInChildren<CapsuleCollider>();
            for (int i = 0; i < colliders.Length; i++)
            {
                colliders[i].enabled = true;
            }
            itemController.rb.useGravity = true;
            itemController.rb.freezeRotation = false;
            itemController.rb.isKinematic = false;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }

    [ClientRpc]
    void RpcSendPositionToClient(Vector3 positionFromServer)
    {
        transform.position = Vector3.Lerp(transform.position, positionFromServer, 0.5f);
    }
    [ClientRpc]
    void RpcSendRotationToClient(Quaternion rotationFromServer)
    {
        transform.rotation = Quaternion.Lerp(transform.rotation, rotationFromServer, 0.5f);
    }




    public void SendMessageToServer(string message)
    {
        CmdSendMessageToServer(message);
    }

    public void GetNetworkAuthority(NetworkInstanceId id)
    {
        CmdGetNetworkAuthority(id);
    }

    public void SendItemMotionDataToServer(NetworkInstanceId id, Vector3 clientMotion)
    {
        CmdSendItemMotionDataToServer(id, clientMotion);
    }

    public void SetItemPropertiesOnServer(NetworkInstanceId playerId, NetworkInstanceId itemId, bool isPlayerControlled, Vector3 clientMotion)
    {
        CmdSetItemPropertiesOnServer(playerId, itemId, isPlayerControlled, clientMotion);
    }
}