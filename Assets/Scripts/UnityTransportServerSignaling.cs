using System;
using System.Linq;
using System.Threading;
using Unity.Collections;
using Unity.Networking.Transport;
using Unity.RenderStreaming.Signaling;
using Unity.WebRTC;
using UnityEngine;
using UnityEngine.Assertions;

public class UnityTransportServerSignaling : ISignaling
{
    private SynchronizationContext m_mainThreadContext;
    private bool m_running;
    private Thread m_signalingThread;

    private ushort port;

    public UnityTransportServerSignaling(string url, float timeout, SynchronizationContext mainThreadContext)
    {
        var portStr = url.Split(':').LastOrDefault();
        if (!ushort.TryParse(portStr, out port))
        {
            port = 9000;
        }

        m_mainThreadContext = mainThreadContext;
    }

    private NetworkDriver m_Driver;
    private NativeList<NetworkConnection> m_Connections;

    public void Start()
    {
        m_Driver = NetworkDriver.Create();
        var endpoint = NetworkEndPoint.AnyIpv4;
        endpoint.Port = port;
        if (m_Driver.Bind(endpoint) != 0)
        {
            Debug.Log("Failed to bind to port 9000");
        }
        else
        {
            m_Driver.Listen();
        }

        m_Connections = new NativeList<NetworkConnection>(16, Allocator.Persistent);
    }

    public void Stop()
    {
        m_Driver.Dispose();
        m_Connections.Dispose();
    }

    public void Update()
    {
        m_Driver.ScheduleUpdate().Complete();

        // CleanUpConnections
        for (int i = 0; i < m_Connections.Length; i++)
        {
            if (!m_Connections[i].IsCreated)
            {
                m_Connections.RemoveAtSwapBack(i);
                --i;
            }
        }

        // AcceptNewConnections
        NetworkConnection c;
        while ((c = m_Driver.Accept()) != default(NetworkConnection))
        {
            m_Connections.Add(c);
            Debug.Log("Accepted a connection");
        }

        DataStreamReader stream;
        for (int i = 0; i < m_Connections.Length; i++)
        {
            Assert.IsTrue(m_Connections[i].IsCreated);

            NetworkEvent.Type cmd;
            while ((cmd = m_Driver.PopEventForConnection(m_Connections[i], out stream)) != NetworkEvent.Type.Empty)
            {
                if (cmd == NetworkEvent.Type.Data)
                {
                    uint number = stream.ReadUInt();

                    Debug.Log("Got " + number + " from the Client adding + 2 to it.");
                    number += 2;

                    var writer = m_Driver.BeginSend(NetworkPipeline.Null, m_Connections[i]);
                    writer.WriteUInt(number);
                    m_Driver.EndSend(writer);
                }
                else if (cmd == NetworkEvent.Type.Disconnect)
                {
                    Debug.Log("Client disconnected from server");
                    m_Connections[i] = default(NetworkConnection);
                }
            }
        }
    }

    public event OnStartHandler OnStart;
    public event OnConnectHandler OnCreateConnection;
    public event OnDisconnectHandler OnDestroyConnection;
    public event OnOfferHandler OnOffer;
    public event OnAnswerHandler OnAnswer;
    public event OnIceCandidateHandler OnIceCandidate;

    public void OpenConnection(string connectionId)
    {
        throw new System.NotImplementedException();
    }

    public void CloseConnection(string connectionId)
    {
        throw new System.NotImplementedException();
    }

    public void SendOffer(string connectionId, RTCSessionDescription offer)
    {
        throw new System.NotImplementedException();
    }

    public void SendAnswer(string connectionId, RTCSessionDescription answer)
    {
        throw new System.NotImplementedException();
    }

    public void SendCandidate(string connectionId, RTCIceCandidate candidate)
    {
        throw new System.NotImplementedException();
    }
}