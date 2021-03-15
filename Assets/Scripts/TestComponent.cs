using System;
using System.Linq;
using System.Threading;
using UnityEngine;
using Random = UnityEngine.Random;

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
        client.OnStart += signaling =>
        {
            var id = string.Join("", Enumerable.Range(0, 1300).Select(x => Random.Range(0, 9).ToString()));
            signaling.OpenConnection(id);
        };

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