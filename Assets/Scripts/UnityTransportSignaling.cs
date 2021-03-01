using Unity.RenderStreaming.Signaling;
using Unity.WebRTC;

public class UnityTransportSignaling : ISignaling
{
    public void Start()
    {
        throw new System.NotImplementedException();
    }

    public void Stop()
    {
        throw new System.NotImplementedException();
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