using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NotificationTextController : MonoBehaviour
{

    Text notificationText;
    float height = 30;
    float width = 160;
    int maxLines = 6;
    List<string> messages = new List<string>();

    void Start ()
    {
        notificationText = GetComponent<Text>();
        
    }

    public void AddMessage(string message)
    {
        messages.Add(message);

        if(messages.Count > maxLines)
        {
            messages.RemoveAt(0);
            maxLines = 0;
        }
        else
        {
            notificationText.GetComponent<RectTransform>().sizeDelta = new Vector2(width, height += 30);
        }

        string newText = string.Empty;
        foreach(string s in messages)
        {
            newText += "\n" + s;
        }

        notificationText.text = newText;
    }
}
