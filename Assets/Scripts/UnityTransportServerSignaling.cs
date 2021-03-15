using System;
using System.Linq;
using System.Threading;
using Unity.Collections;
using Unity.Networking.Transport;
using Unity.RenderStreaming;
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
                    var str = "";
                    while (stream.Length > stream.GetBytesRead())
                    {
                        str += stream.ReadString();
                    }

                    ProcessMessage(str);
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
        //ToDO: decide connections index
        Send(0, $"{{\"type\":\"connect\", \"connectionId\":\"{connectionId}\"}}");
    }

    public void CloseConnection(string connectionId)
    {
        Send(0, $"{{\"type\":\"disconnect\", \"connectionId\":\"{connectionId}\"}}");
    }

    public void SendOffer(string connectionId, RTCSessionDescription offer)
    {
        DescData data = new DescData();
        data.connectionId = connectionId;
        data.sdp = offer.sdp;
        data.type = "offer";

        RoutedMessage<DescData> routedMessage = new RoutedMessage<DescData>();
        routedMessage.from = connectionId;
        routedMessage.data = data;
        routedMessage.type = "offer";

        Send(0, routedMessage);
    }

    public void SendAnswer(string connectionId, RTCSessionDescription answer)
    {
        DescData data = new DescData();
        data.connectionId = connectionId;
        data.sdp = answer.sdp;
        data.type = "answer";

        RoutedMessage<DescData> routedMessage = new RoutedMessage<DescData>();
        routedMessage.from = connectionId;
        routedMessage.data = data;
        routedMessage.type = "answer";

        Send(0, routedMessage);
    }

    public void SendCandidate(string connectionId, RTCIceCandidate candidate)
    {
        CandidateData data = new CandidateData();
        data.connectionId = connectionId;
        data.candidate = candidate.Candidate;
        data.sdpMLineIndex = candidate.SdpMLineIndex.GetValueOrDefault(0);
        data.sdpMid = candidate.SdpMid;

        RoutedMessage<CandidateData> routedMessage = new RoutedMessage<CandidateData>();
        routedMessage.from = connectionId;
        routedMessage.data = data;
        routedMessage.type = "candidate";

        Send(0, routedMessage);
    }

    private void Send(int index, object data)
    {
        var connection = m_Connections[index];
        if (!connection.IsCreated || !m_Driver.IsCreated)
        {
            Debug.LogError("NotReady this client");
            return;
        }

        if (data is string s)
        {
            Debug.Log("Signaling: Sending WS data: " + s);
            var writer = m_Driver.BeginSend(connection);
            var array = s.SubstringAtCount(NativeString64.MaxLength);
            foreach (var t in array)
            {
                writer.WriteString(t);
            }

            m_Driver.EndSend(writer);
        }
        else
        {
            string str = JsonUtility.ToJson(data);
            Debug.Log("Signaling: Sending WS data: " + str);
            var writer = m_Driver.BeginSend(connection);
            var array = str.SubstringAtCount(NativeString64.MaxLength);
            foreach (var t in array)
            {
                writer.WriteString(t);
            }

            m_Driver.EndSend(writer);
        }
    }

    //private void ProcessMessage(byte[] data)
    private void ProcessMessage(string content)
    {
        //var content = Encoding.UTF8.GetString(data);
        Debug.Log($"Signaling: Receiving message: {content}");

        try
        {
            var routedMessage = JsonUtility.FromJson<RoutedMessage<SignalingMessage>>(content);

            SignalingMessage msg;
            if (!string.IsNullOrEmpty(routedMessage.type))
            {
                msg = routedMessage.data;
            }
            else
            {
                msg = JsonUtility.FromJson<SignalingMessage>(content);
            }

            if (!string.IsNullOrEmpty(routedMessage.type))
            {
                if (routedMessage.type == "connect")
                {
                    msg = JsonUtility.FromJson<SignalingMessage>(content);
                    m_mainThreadContext.Post(d => OnCreateConnection?.Invoke(this, msg.connectionId, msg.peerExists),
                        null);
                }
                else if (routedMessage.type == "disconnect")
                {
                    msg = JsonUtility.FromJson<SignalingMessage>(content);
                    m_mainThreadContext.Post(d => OnDestroyConnection?.Invoke(this, msg.connectionId), null);
                }
                else if (routedMessage.type == "offer")
                {
                    DescData offer = new DescData();
                    offer.connectionId = routedMessage.from;
                    offer.sdp = msg.sdp;
                    m_mainThreadContext.Post(d => OnOffer?.Invoke(this, offer), null);
                }
                else if (routedMessage.type == "answer")
                {
                    DescData answer = new DescData
                    {
                        connectionId = routedMessage.from,
                        sdp = msg.sdp
                    };
                    m_mainThreadContext.Post(d => OnAnswer?.Invoke(this, answer), null);
                }
                else if (routedMessage.type == "candidate")
                {
                    CandidateData candidate = new CandidateData
                    {
                        connectionId = routedMessage.@from,
                        candidate = msg.candidate,
                        sdpMLineIndex = msg.sdpMLineIndex,
                        sdpMid = msg.sdpMid
                    };
                    m_mainThreadContext.Post(d => OnIceCandidate?.Invoke(this, candidate), null);
                }
                else if (routedMessage.type == "error")
                {
                    msg = JsonUtility.FromJson<SignalingMessage>(content);
                    Debug.LogError(msg.message);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("Signaling: Failed to parse message: " + ex);
        }
    }
}