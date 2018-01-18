using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Networking;

public class ItemController : NetworkBehaviour
{
    public Rigidbody rb;
    Transform itemTransform;
    Material itemMaterial;
    Material itemHighlightMaterial;
    GameObject playerGO;
    NotificationTextController notificationController;
    NetworkPlayer networkPlayer;

    bool isMouseButton1 = false;
    bool isInRangeOfPlayer = false;
    float moveSpeed = 50.0f;

    public Vector3 clientMotion;
    Vector3 serverPosition;

    void Start()
    {
        serverPosition = transform.position;
        rb = GetComponent<Rigidbody>();
        itemTransform = GetComponent<Transform>();
        itemMaterial = GetComponentInChildren<MeshRenderer>().material;
        itemHighlightMaterial = Resources.Load("Materials/ItemHighlight") as Material;
        notificationController = GameObject.Find("Notification Text").GetComponent<NotificationTextController>();
        if (isClient)
        {
            var networkPlayers = FindObjectsOfType<NetworkPlayer>();
            foreach(NetworkPlayer player in networkPlayers)
            {
                if (player.playerControllerId == 0)
                {
                    playerGO = player.gameObject;
                    networkPlayer = player;
                }
            }
        }
    }
    void LateUpdate()
    {
        if (isClient)
        {
            transform.Translate(clientMotion);
            SendDataToServer();
            clientMotion = Vector3.zero;
        }
        if (isServer)
        {
            //SendDataToClient();
        }
    }

    [Client]
    void OnMouseEnter()
    {
        var meshRenderers = GetComponentsInChildren<MeshRenderer>();
        for (int i = 0; i < meshRenderers.Length; i++)
        {
            meshRenderers[i].material = itemHighlightMaterial;
        }
    }
    [Client]
    void OnMouseExit()
    {
        var meshRenderers = GetComponentsInChildren<MeshRenderer>();
        for (int i = 0; i < meshRenderers.Length; i++)
        {
            meshRenderers[i].material = itemMaterial;
        }
    }
    [Client]
    void OnMouseDown()
    {
        isInRangeOfPlayer = IsPlayerInRange();
        if (isInRangeOfPlayer)
        {
            SetItemProperties(true);
            SetItemPropertiesOnServer(true);
        }
        else
        {
            notificationController.AddMessage("That's too far away!");
        }
    }
    [Client]
    void OnMouseDrag()
    {
        if (!isMouseButton1 && isInRangeOfPlayer)
        {
            Cursor.lockState = CursorLockMode.Locked;
            float deltaX = Input.GetAxis("Mouse X") * moveSpeed * Time.deltaTime;
            float deltaY = Input.GetAxis("Mouse Y") * moveSpeed * Time.deltaTime;
            clientMotion = (playerGO.transform.forward * deltaY) + (playerGO.transform.right * deltaX);
        }
    }
    [Client]
    void OnMouseUp()
    {
        SetItemProperties(false);
        SetItemPropertiesOnServer(false);
    }


    private bool IsPlayerInRange()
    {
        float distance = Vector3.Distance(transform.position, playerGO.transform.position);
        return distance <= 5;
    }
    private void SendDataToServer()
    {
        networkPlayer.SendItemMotionDataToServer(netId, clientMotion);
    }
    private void SendDataToClient()
    {
        RpcSendPositionToClient(transform.position);
    }
    private void SetItemProperties(bool isPlayerControlled)
    {
        if (isPlayerControlled)
        {
            networkPlayer.GetNetworkAuthority(networkPlayer.netId);
            clientMotion = new Vector3(0, 1.25f, 0);
            var colliders = GetComponentsInChildren<CapsuleCollider>();
            for (int i = 0; i < colliders.Length; i++)
            {
                colliders[i].enabled = false;
            }
            rb.useGravity = false;
            rb.freezeRotation = true;
            rb.isKinematic = true;
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
            rb.useGravity = true;
            rb.freezeRotation = false;
            rb.isKinematic = false;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }
    private void SetItemPropertiesOnServer(bool isPlayerControlled)
    {
        networkPlayer.SetItemPropertiesOnServer(networkPlayer.netId, netId, isPlayerControlled, clientMotion);
    }




    [ClientRpc]
    void RpcSendPositionToClient(Vector3 positionFromServer)
    {
        transform.position = positionFromServer;
    }

}