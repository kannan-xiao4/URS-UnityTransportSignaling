using System;
using System.Linq;
using System.Threading;
using Unity.Networking.Transport;
using Unity.RenderStreaming.Signaling;
using Unity.WebRTC;
using UnityEngine;

public class UnityTransportClientSignaling : ISignaling
{
    private SynchronizationContext m_mainThreadContext;
    private bool m_running;
    private Thread m_signalingThread;
    private ushort port;

    public UnityTransportClientSignaling(string url, float timeout, SynchronizationContext mainThreadContext)
    {
        var portStr = url.Split(':').LastOrDefault();
        port = string.IsNullOrEmpty(portStr) ? (ushort) 9000 : UInt16.Parse(portStr);
        m_mainThreadContext = mainThreadContext;
    }

    private NetworkDriver m_Driver;
    private NetworkConnection m_Connection;
    private bool m_Done;

    public void Start()
    {
        if (m_running)
            throw new InvalidOperationException("This object is already started.");

        m_running = true;
        m_signalingThread = new Thread(Update);
        m_signalingThread.Start();
    }

    public void Stop()
    {
        if (m_running)
        {
            m_running = false;
            m_signalingThread?.Join();
            m_signalingThread = null;
        }
    }

    private void Update()
    {
        m_Driver = NetworkDriver.Create();
        m_Connection = default(NetworkConnection);

        var endpoint = NetworkEndPoint.LoopbackIpv4;
        endpoint.Port = port;
        m_Connection = m_Driver.Connect(endpoint);


        while (m_running)
        {
            m_Driver.ScheduleUpdate().Complete();

            if (!m_Connection.IsCreated)
            {
                if (!m_Done)
                    Debug.Log("Something went wrong during connect");
                continue;
            }

            DataStreamReader stream;
            NetworkEvent.Type cmd;

            while ((cmd = m_Connection.PopEvent(m_Driver, out stream)) != NetworkEvent.Type.Empty)
            {
                if (cmd == NetworkEvent.Type.Connect)
                {
                    Debug.Log("We are now connected to the server");

                    uint value = 1;
                    var writer = m_Driver.BeginSend(m_Connection);
                    writer.WriteUInt(value);
                    m_Driver.EndSend(writer);
                }
                else if (cmd == NetworkEvent.Type.Data)
                {
                    uint value = stream.ReadUInt();
                    Debug.Log("Got the value = " + value + " back from the server");
                    m_Done = true;
                    m_Connection.Disconnect(m_Driver);
                    m_Connection = default(NetworkConnection);
                }
                else if (cmd == NetworkEvent.Type.Disconnect)
                {
                    Debug.Log("Client got disconnected from server");
                    m_Connection = default(NetworkConnection);
                }
            }
        }

        m_Driver.Dispose();
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