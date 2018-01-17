using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Networking;

public class ItemController : NetworkBehaviour
{
    Rigidbody rb;
    Transform itemTransform;
    Material itemMaterial;
    Material itemHighlightMaterial;
    GameObject playerGO;
    NotificationTextController notificationController;
    NetworkIdentity netId;

    bool isMouseButton1 = false;
    bool isInRangeOfPlayer = false;
    float moveSpeed = 50.0f;

    Vector3 clientMotion;
    Vector3 serverPosition;

    void Start()
    {
        serverPosition = transform.position;

        rb = GetComponent<Rigidbody>();
        itemTransform = GetComponent<Transform>();
        itemMaterial = GetComponentInChildren<MeshRenderer>().material;
        itemHighlightMaterial = Resources.Load("Materials/ItemHighlight") as Material;
        notificationController = GameObject.Find("Notification Text").GetComponent<NotificationTextController>();
        netId = GetComponent<NetworkIdentity>();
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
            SendDataToClient();
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
        playerGO = GameObject.FindGameObjectWithTag("Player");

        isInRangeOfPlayer = IsPlayerInRange();
        if (isInRangeOfPlayer)
        {
            SetItemProperties(true);
            CmdSetItemProperties(true);
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
        CmdSetItemProperties(false);
    }

    private bool IsPlayerInRange()
    {
        float distance = Vector3.Distance(transform.position, playerGO.transform.position);
        return distance <= 5;
    }
    private void SendDataToServer()
    {
        CmdSendMotionDataToServer(clientMotion);
    }
    private void SendDataToClient()
    {
        RpcSendPositionToClient(transform.position);
    }
    private void SetItemProperties(bool isPlayerControlled)
    {
        if (isPlayerControlled)
        {
            //var id = GetComponent<NetworkIdentity>().netId;
            //CmdAssignNetworkAuthority(id);

            notificationController.AddMessage("Turning gravity off");
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
            notificationController.AddMessage("Turning gravity on");
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

    [Command]
    void CmdSendMotionDataToServer(Vector3 motionFromClient)
    {
        transform.Translate(motionFromClient);
        serverPosition = transform.position;
    }

    [Command]
    void CmdSetItemProperties(bool isPlayerControlled)
    {
        SetItemProperties(isPlayerControlled);
    }

    [Command]
    void CmdPrintMessageOnServer(string message)
    {
        notificationController.AddMessage(message);
    }

    [Command]
    void CmdAssignNetworkAuthority(NetworkInstanceId toId)
    {


        GameObject client = NetworkServer.FindLocalObject(toId);
        var conn = client.GetComponent<NetworkIdentity>().connectionToClient;
        var result = GetComponent<NetworkIdentity>().AssignClientAuthority(conn);

        notificationController.AddMessage(client.ToString());
        notificationController.AddMessage(conn.ToString());
        notificationController.AddMessage(result.ToString());

    }

    [ClientRpc]
    void RpcSendPositionToClient(Vector3 positionFromServer)
    {
        transform.position = positionFromServer;
    }





}