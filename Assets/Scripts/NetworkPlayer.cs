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
    Manager networkManager;
    CharacterController cc;
    Canvas uiCanvas;
    GameObject inventoryGO;
    GameObject itemDraggingGO;

    #region Player Control
    //Motion
    Vector3 motionFromClient;
    Vector3 motionFromServer;
    Vector3 clientPosition;
    Vector3 serverPosition;

    //Rotation
    float rotationFromClient;
    float rotationFromServer;
    Quaternion clientRotation;
    Quaternion serverRotation;

    [SerializeField]
    float moveSpeed = 0.25f;
    [SerializeField]
    float rotateSpeed = 5.0f;
    [SerializeField]
    float gravity = -0.5f;

    #endregion

    #region Player Input
    bool isSpaceBar = false;
    #endregion

    #region Monobehaviors
    void Awake()
    {
        networkManager = GameObject.Find("NetworkManager").GetComponent<Manager>();
    }

    void Start()
    {
        cc = GetComponent<CharacterController>();
        uiCanvas = GameObject.FindObjectOfType<Canvas>();
        
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
            SendDataToServer();
        }

        if (isServer)
        {
            SendDataToClient();
            CorrectClientData();
        }
    }
    #endregion

    #region private helpers
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

        //Player Rotation
        if (Input.GetMouseButton(1))
            rotationFromClient = Input.GetAxis("Mouse X");
        else
            rotationFromClient = 0;

        //UI
        if (Input.GetKeyDown(KeyCode.I))
            inventoryGO.SetActive(!inventoryGO.activeSelf);

        //TEST
        if (Input.GetKeyDown(KeyCode.Space))
        {
            CmdSpawnItemsOnServer();
        }
    }
    void SendDataToServer()
    {
        CmdSendMotionDataToServer(motionFromClient);
        CmdSendPositionToServer(transform.position);
        CmdSendRotationDataToServer(rotationFromClient);
        CmdSendRotationToServer(transform.rotation);
    }
    void SendDataToClient()
    {
        RpcSendMotionDataToClient(motionFromServer);
        RpcSendPositionToClient(transform.position);
        RpcSendRotationDataToClient(rotationFromServer);
        RpcSendRotationToClient(transform.rotation);
    }
    void CorrectClientData()
    {
        CorrectClientPosition();
        CorrectClientRotation();
    }
    void CorrectClientPosition()
    {
        //If the client desynchs position in any way larger than 0.1f, we snap to the server position.
        if ((Mathf.Abs(clientPosition.x - serverPosition.x) > 0.1f) || (Mathf.Abs(clientPosition.z - serverPosition.z) > 0.1f))
        {
            if (isClient)
            {
                transform.position = Vector3.Lerp(transform.position, serverPosition, 0.5f);
            }
        }
    }
    void CorrectClientRotation()
    {
        //If the client desynchs rotation in any way larger than 0.1f, we snap to the server rotation.
        if ((Mathf.Abs(clientRotation.y - serverRotation.y) > 0.1f))
        {
            if (isClient)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, serverRotation, 0.5f);
            }
        }
    }
    #endregion

    #region Commands and RPCs
    //Item Spawning
    [Command]
    void CmdSpawnItemsOnServer()
    {
        foreach (GameObject go in networkManager.GetComponent<NetworkManager>().spawnPrefabs)
        {
            var prefab = Instantiate(go, new Vector3(Random.Range(0,5), Random.Range(0, 5), Random.Range(0, 5)), Quaternion.identity) as GameObject;
            prefab.name = networkManager.UpdateItemName(prefab);
            networkManager.spawnedItems.Add(prefab);

            //NetworkServer.Spawn(prefab);
            NetworkServer.SpawnWithClientAuthority(prefab, gameObject);
        }
    }

    //Motion Input
    [Command]
    void CmdSendMotionDataToServer(Vector3 motionFromClient)
    {
        motionFromServer = motionFromClient * moveSpeed;
        cc.Move(motionFromServer);
        serverPosition = transform.position;
    }

    [ClientRpc]
    void RpcSendMotionDataToClient(Vector3 motionFromServer)
    {
        cc.Move(motionFromServer);
        clientPosition = transform.position;
    }

    //Position
    [Command]
    void CmdSendPositionToServer(Vector3 positionFromClient)
    {
        clientPosition = positionFromClient;
    }

    [ClientRpc]
    void RpcSendPositionToClient(Vector3 positionFromServer)
    {
        serverPosition = positionFromServer;
    }

    //Rotation Input
    [Command]
    void CmdSendRotationDataToServer(float rotationFromClient)
    {
        rotationFromServer = rotationFromClient * rotateSpeed;
        transform.Rotate(new Vector3(0, rotationFromServer, 0));
        serverRotation = transform.rotation;
    }

    [ClientRpc]
    void RpcSendRotationDataToClient(float rotationFromServer)
    {
        transform.Rotate(new Vector3(0, rotationFromServer, 0));
        clientRotation = transform.rotation;
    }

    //Rotation
    [Command]
    void CmdSendRotationToServer(Quaternion rotationFromClient)
    {
        clientRotation = rotationFromClient;
    }

    [ClientRpc]
    void RpcSendRotationToClient(Quaternion rotationFromServer)
    {
        serverRotation = rotationFromServer;
    }
    #endregion
}