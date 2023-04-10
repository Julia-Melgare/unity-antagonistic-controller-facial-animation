using System;
using AsyncIO;
using NetMQ;
using NetMQ.Sockets;
using UnityEngine;

public class InferenceRequester : RunAbleThread
{
    private RequestSocket client;

    private Action<byte[]> onOutputReceived;
    private Action<Exception> onFail;
    protected override void Run()
    {
        ForceDotNet.Force();
        using (RequestSocket client = new RequestSocket())
        {
            this.client = client;
            client.Connect("tcp://localhost:5555");

            while (Running)
            {
                byte[] outputBytes = new byte[0];
                bool gotMessage = false;
                while (Running)
                {
                    try
                    {
                        gotMessage = client.TryReceiveFrameBytes(out outputBytes);
                        if (gotMessage) break;
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(e.Message);
                    }
                }

                if (gotMessage)
                {
                    var output = new byte[outputBytes.Length];
                    Buffer.BlockCopy(outputBytes, 0, output, 0, outputBytes.Length);
                    onOutputReceived?.Invoke(output);
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
        }
        catch (Exception e)
        {
            onFail(e);
        }
    }

    public void SetOnOutputReceivedListener(Action<byte[]> onOutputReceived, Action<Exception> fallback)
    {
        this.onOutputReceived = onOutputReceived;
        onFail = fallback;
    }
}
