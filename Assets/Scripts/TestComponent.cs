using System;
using System.Threading;
using UnityEngine;

public class TestComponent : MonoBehaviour
{
    private UnityTransportClientSignaling client;
    private UnityTransportServerSignaling server;

    private void Awake()
    {
        var context = SynchronizationContext.Current;
        client = new UnityTransportClientSignaling("localhost", 5.0f, context);
        server = new UnityTransportServerSignaling("localhost", 5.0f, context);        
    }

    private void Start()
    {
        client.Start();
        server.Start();
    }

    private void Update()
    {
        client.Update();
        server.Update();
    }

    private void OnDestroy()
    {
        client.Stop();
        server.Stop();
    }
}