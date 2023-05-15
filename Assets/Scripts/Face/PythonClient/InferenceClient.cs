using System;
using UnityEngine;

public class InferenceClient : MonoBehaviour
{
    private InferenceRequester inferenceRequester;

    private void Start() => InitializeServer();

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

    private void OnDestroy()
    {
        inferenceRequester.Stop();
    }
}