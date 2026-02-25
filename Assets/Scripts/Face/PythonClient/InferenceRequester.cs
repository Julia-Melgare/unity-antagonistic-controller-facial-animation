using System;
using System.Threading;
using AsyncIO;
using NetMQ;
using NetMQ.Sockets;
using UnityEngine;

public class InferenceRequester : RunAbleThread
{
    private RequestSocket client;

    private Action<byte[]> onOutputReceived;
    private Action<Exception> onFail;

    private bool needReply = false;

    private int failCount = 0;
    public bool NeedReset = false;

    private int failThreshold = 3;

    private string socketID;

    public InferenceRequester(string socketID) : base()
    {
        this.socketID = socketID;
    }
    protected override void Run()
    {
        ForceDotNet.Force();
        client = new RequestSocket();
        client.Connect("tcp://localhost:"+socketID);
        while (Running)
        {
            if (needReply)
            {
                try
                {
                    if (client.TryReceiveFrameBytes(
                            TimeSpan.FromMilliseconds(100),
                            out var outputBytes))
                    {
                        //Debug.Log("message received!");
                        onOutputReceived?.Invoke(outputBytes);
                        needReply = false;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError(e.Message);
                }
            }
            else
            {
                Thread.Sleep(1); // prevent CPU spinning
            }
        }
        client.Close();
        client.Dispose();
        NetMQConfig.Cleanup();
    }

    public void SendInput(byte[] input)
    {
        try
        {
            var byteArray = new byte[input.Length];
            Buffer.BlockCopy(input, 0, byteArray, 0, byteArray.Length);
            client.SendFrame(byteArray);
            needReply = true;
            failCount = 0;
        }
        catch (Exception e)
        {
            onFail(e);
            failCount++;
            //Debug.Log("NetMQ send fail count: "+failCount);
            if (failCount >= failThreshold)
            {
                NeedReset = true;
                failCount = 0;
            }
        }

    }

    public void SetOnOutputReceivedListener(Action<byte[]> onOutputReceived, Action<Exception> fallback)
    {
        this.onOutputReceived = onOutputReceived;
        onFail = fallback;
    }
}
