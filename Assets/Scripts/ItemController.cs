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

    bool isMouseButton1 = false;
    bool isInRangeOfPlayer = false;
    float moveSpeed = 50.0f;

    Vector3 serverPosition;

    #region Monobehaviors
    void Start()
    {
        serverPosition = transform.position;

        rb = GetComponent<Rigidbody>();
        itemTransform = GetComponent<Transform>();
        itemMaterial = GetComponentInChildren<MeshRenderer>().material;
        itemHighlightMaterial = Resources.Load("Materials/ItemHighlight") as Material;
        notificationController = GameObject.Find("Notification Text").GetComponent<NotificationTextController>();
    }

    void LateUpdate()
    {
        if (isClient)
        {
            CmdSendDataToServer(transform.position);
        }
        if (isServer)
        {
            transform.position = serverPosition;
        }    
    }

    void OnMouseEnter()
    {
        var meshRenderers = GetComponentsInChildren<MeshRenderer>();
        for (int i = 0; i < meshRenderers.Length; i++)
        {
            meshRenderers[i].material = itemHighlightMaterial;
        }
    }
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

        isInRangeOfPlayer = _isPlayerInRange();
        if (isInRangeOfPlayer)
        {
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

            //Perform raycast down from item to get terrain Y
            RaycastHit hit;
            if (Physics.Raycast(gameObject.transform.position, Vector3.down, out hit))
            {
                //var newPosition = new Vector3(0, hit.point.y + 0.25f, 0);
                //transform.position += newPosition;
                transform.position = new Vector3(transform.position.x, hit.point.y + 1.25f, transform.position.z);
                CmdSendDataToServer(new Vector3(transform.position.x, hit.point.y + 1.25f, transform.position.z));
            }
        }
        else
        {
            notificationController.AddMessage("That's too far away!");
        }
    }
    void OnMouseDrag()
    {
        if (!isMouseButton1 && isInRangeOfPlayer)
        {
            float deltaX = Input.GetAxis("Mouse X") * moveSpeed * Time.deltaTime;
            float deltaY = Input.GetAxis("Mouse Y") * moveSpeed * Time.deltaTime;
            Vector3 newPosition = (playerGO.transform.forward * deltaY) + (playerGO.transform.right * deltaX);
            transform.position += newPosition;
            CmdSendDataToServer(transform.position);
        }
    }
    void OnMouseUp()
    {
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
    #endregion

    #region Private Helpers
    private bool _isPlayerInRange()
    {
        float distance = Vector3.Distance(transform.position, playerGO.transform.position);
        return distance <= 5;
    }
    #endregion

    #region Commands and RPCs
    [Command]
    void CmdSendDataToServer(Vector3 newPosition)
    {
        //transform.position = newPosition;
        serverPosition = newPosition;
    }

    //[ClientRpc]
    //void RpcSendDataToClient(Vector3 newPosition)
    //{
    //    itemTransform.Translate(newPosition);
    //    //GameObject item = Instantiate(Resources.Load("Prefabs/Items/Fishing Rod")) as GameObject;
    //    //item.transform.Translate(new Vector3(2, 3, 2));
    //}
    #endregion 





}