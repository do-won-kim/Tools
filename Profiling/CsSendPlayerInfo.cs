
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using UnityEngine.Networking.PlayerConnection;
using System;

public class CsSendPlayerInfo : MonoBehaviour
{
    public static readonly Guid kMsgSendEditorToPlayer = new Guid("FD713788-B5AE-49FF-8B2C-F311B9CB0CC1");
    public static readonly Guid kMsgSendPlayerToEditor = new Guid("FD713788-B5AE-49FF-8B2C-F311B9CB0CC2");

    public static readonly Guid kMsgSendDestory = new Guid("FD713788-B5AE-49FF-8B2C-F311B9CB0CC3");
    public static readonly Guid kMsgSendSaveReset = new Guid("FD713788-B5AE-49FF-8B2C-F311B9CB0CC4");

    readonly int c_nDefaultNum = -1;
    readonly float c_flTime = 1.0f;
    readonly string c_str = ":";

    float m_flTime = 0.0f;
    PlayerConnection playerConnection;

    StringBuilder strText = new StringBuilder();

    byte[] aBytePlayerInfo;

	void Awake()
	{
        playerConnection = PlayerConnection.instance;
    }
	void Update()
    {

        if (!playerConnection.isConnected)
		{
            return;
        }
           
        strText.Clear();

        if (Camera.main != null)
        {
            strText.Append(Camera.main.transform.position);
        }
        else
        {
            strText.Append(Vector3.zero);
        }
        strText.Append(c_str);
        if (Camera.main != null)
        {
            strText.Append(Camera.main.transform.rotation.eulerAngles);
        }
        else
        {
            strText.Append(Vector3.zero);
        }

        strText.Append(c_str);
        strText.Append((1.0f / Time.deltaTime).ToString());

        aBytePlayerInfo = Encoding.ASCII.GetBytes(strText.ToString());
        playerConnection.Send(kMsgSendPlayerToEditor, aBytePlayerInfo);
    }

	void OnDestroy()
	{
        playerConnection.Send(kMsgSendDestory, null);
        playerConnection = null;
    }
}
