using System.Collections.Generic;

public sealed class NetworkPeerOwnership
{
    private readonly Dictionary<long, int> _peerToEntity = new();
    private readonly Dictionary<int, long> _entityToPeer = new();
    public bool Assign(long peerId, int entityId)
    {
        if (peerId <= 0 || entityId <= 0 || _peerToEntity.ContainsKey(peerId) || _entityToPeer.ContainsKey(entityId)) return false;
        _peerToEntity.Add(peerId, entityId); _entityToPeer.Add(entityId, peerId); return true;
    }
    public void Remove(long peerId) { if (_peerToEntity.Remove(peerId, out int id)) _entityToPeer.Remove(id); }
    public bool HasPeer(long peerId) => _peerToEntity.ContainsKey(peerId);
    public bool TryGetControlledEntityId(long peerId, out int entityId) => _peerToEntity.TryGetValue(peerId, out entityId);
    public bool TryGetPeerForEntity(int entityId, out long peerId) => _entityToPeer.TryGetValue(entityId, out peerId);
    public void Clear() { _peerToEntity.Clear(); _entityToPeer.Clear(); }
}
