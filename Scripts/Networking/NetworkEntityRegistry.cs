using System.Collections.Generic;
using Godot;

public sealed class NetworkEntityRegistry
{
    private readonly Dictionary<int, Node> _entities = new();
    private int _nextEntityId = 1;
    public int AllocateId() => _nextEntityId++;
    public bool Register(int entityId, Node node)
    {
        if (entityId <= 0 || node == null || _entities.ContainsKey(entityId)) return false;
        _entities.Add(entityId, node); return true;
    }
    public void Unregister(int entityId) { _entities.Remove(entityId); }
    public bool Contains(int entityId) => entityId > 0 && _entities.ContainsKey(entityId);
    public bool TryResolve(int entityId, out Node node)
    {
        if (_entities.TryGetValue(entityId, out node) && GodotObject.IsInstanceValid(node)) return true;
        _entities.Remove(entityId); node = null; return false;
    }
    public void Clear() { _entities.Clear(); _nextEntityId = 1; }
}
