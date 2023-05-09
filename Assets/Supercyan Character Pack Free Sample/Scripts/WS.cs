using WebSocketSharp;
using UnityEngine;
using System.Collections;
using System.Text;
using System.Collections.Generic;

[System.Serializable]
public class JsonData
{
    [SerializeField]
    public string type;

    [SerializeField]
    public Data data;
}

// La classe correspondant à la clé "data" dans la structure JSON
[System.Serializable]
public class Data
{
    [SerializeField]
    public float x;

    [SerializeField]
    public float y;
}

public class WS : MonoBehaviour
{
    WebSocket ws;

    string jsonString = "{\"type\":\"INIT\",\"data\":{\"name\":\"unity\"}}";
    [SerializeField] SimpleSampleCharacterControl character;
    void Start()
    {
        Debug.Log("strat");
        ws = new WebSocket("ws://192.168.43.109:3000");
        // ws = new WebSocket("ws://localhost:3000");

        ws.OnMessage += OnMessage;

        ws.Connect();

        ws.Send(jsonString);

    }

    void Update()
    {
        if (ws == null)
        {
            return;
        }

    }

    void OnMessage(object sender, MessageEventArgs e)
    {
        JsonData jsonData = JsonUtility.FromJson<JsonData>(e.Data);
        character.sendData(jsonData.data);
    }


    private void OnDestroy()
    {
        if (ws != null && ws.IsAlive)
        {
            ws.Close();
        }
    }


}
