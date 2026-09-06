using Godot;
public partial class NetworkPeerOwnershipHarness : Node
{ public override void _Ready(){ var e=new NetworkEntityRegistry();var o=new NetworkPeerOwnership();var n=new Node();e.Register(10,n);GD.Print($"[OwnershipTest] assign={o.Assign(1,10)} resolve={o.TryGetControlledEntityId(1,out var id)&&e.TryResolve(id,out _)} duplicatePeer={o.Assign(1,11)} duplicateEntity={o.Assign(2,10)} invalid={o.Assign(0,1)}");o.Remove(1);GD.Print($"[OwnershipTest] removed={!o.HasPeer(1)}"); } }
