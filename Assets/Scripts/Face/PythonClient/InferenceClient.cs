using System;
using System.Threading;
using UnityEngine;

public class InferenceClient : MonoBehaviour
{
    private InferenceRequester inferenceRequester;
    public string socketID = "5555";

    private void Start() => InitializeServer();

    private void Update()
    {
        if (inferenceRequester != null && inferenceRequester.NeedReset)
        {
            ResetServer();
        }
    }

    public void InitializeServer()
    {
        inferenceRequester = new InferenceRequester(socketID);
        inferenceRequester.Start();
    }

    public void Infer(byte[] input, Action<byte[]> onOutputReceived, Action<Exception> fallback)
    {
        inferenceRequester.SetOnOutputReceivedListener(onOutputReceived, fallback);
        inferenceRequester.SendInput(input);
    }

    private void ResetServer()
    {
        Debug.Log("NetMQ socket crash detected - resetting");
        inferenceRequester.Stop();
        inferenceRequester = new InferenceRequester(socketID);
        inferenceRequester.Start();
    }

    private void OnDestroy()
    {
        inferenceRequester?.Stop();
        // Small grace period for the Run() thread to exit its loop and call Cleanup()
        // If InferenceRequester.Run() doesn't finish in time, force it here as a safety net
        Thread.Sleep(200);
    }
}