using System;
using UnityEngine;

public class InferenceClient : MonoBehaviour
{
    private InferenceRequester inferenceRequester;

    private void Start() => InitializeServer();

    private void Update()
    {
        if (inferenceRequester != null && inferenceRequester.NeedReset)
        {
            ResetServer();
            return;
        }
    }

    public void InitializeServer()
    {
        inferenceRequester = new InferenceRequester();
        inferenceRequester.Start();
    }

    public void Infer(byte[] input, Action<byte[]> onOutputReceived, Action<Exception> fallback)
    {
        inferenceRequester.SetOnOutputReceivedListener(onOutputReceived, fallback);
        inferenceRequester.SendInput(input);        
    }

    private void ResetServer()
    {
        Debug.Log("NetMQ socket crash detected - resetting request socket");
        inferenceRequester.Stop();
        inferenceRequester = new InferenceRequester();
        inferenceRequester.Start();
    }

    private void OnDestroy()
    {
        inferenceRequester.Stop();
    }
}
