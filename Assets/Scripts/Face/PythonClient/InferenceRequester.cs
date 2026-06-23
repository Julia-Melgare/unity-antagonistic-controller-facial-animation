using System;
using System.Threading;
using AsyncIO;
using NetMQ;
using NetMQ.Sockets;

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

    // Timeout for TryReceive — short enough to check Running frequently
    private static readonly TimeSpan ReceiveTimeout = TimeSpan.FromMilliseconds(150);

    public InferenceRequester(string socketID) : base()
    {
        this.socketID = socketID;
    }

    protected override void Run()
    {
        ForceDotNet.Force();
        using (RequestSocket client = new RequestSocket())
        {
            this.client = client;
            client.Connect("tcp://localhost:" + socketID);

            while (Running)
            {
                if (!needReply)
                {
                    // Nothing to do — yield the thread briefly instead of spinning hot
                    Thread.Sleep(1);
                    continue;
                }

                // Non-blocking receive with timeout so we can check Running each cycle
                bool received = client.TryReceiveFrameBytes(ReceiveTimeout, out byte[] outputBytes);

                if (!received)
                {
                    // Timed out — loop back and check Running / needReply again
                    continue;
                }

                var output = new byte[outputBytes.Length];
                Buffer.BlockCopy(outputBytes, 0, output, 0, outputBytes.Length);
                onOutputReceived?.Invoke(output);
                needReply = false;
            }
            // Socket is disposed here by the using block — safe because we exited the loop
        }
        // false = don't block waiting for in-flight messages to drain
        NetMQConfig.Cleanup(false);
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
            onFail?.Invoke(e);
            failCount++;
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