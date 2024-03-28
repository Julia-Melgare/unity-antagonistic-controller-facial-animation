using System;
using AsyncIO;
using NetMQ;
using NetMQ.Sockets;
using UnityEngine;
using UnityEngine.Profiling;

public class InferenceRequester : RunAbleThread
{
    private RequestSocket client;

    private Action<byte[]> onOutputReceived;
    private Action<Exception> onFail;

    private bool needReply = false;

    private int failCount = 0;
    public bool NeedReset = false;

    private int failThreshold = 3;
    protected override void Run()
    {
        ForceDotNet.Force();
        using (RequestSocket client = new RequestSocket())
        {
            this.client = client;
            client.Connect("tcp://localhost:5555");
            while (Running)
            {
                if (needReply)
                {
                    byte[] outputBytes = new byte[0];
                    
                    try
                    {
                        outputBytes = client.ReceiveFrameBytes();
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(e.Message);
                    }
                        
                    Debug.Log("message received!");
                    var output = new byte[outputBytes.Length];
                    Buffer.BlockCopy(outputBytes, 0, output, 0, outputBytes.Length);
                    onOutputReceived?.Invoke(output);
                    needReply = false;
                }
                
            }
        }

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
