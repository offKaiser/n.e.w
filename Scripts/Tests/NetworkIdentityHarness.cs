using Godot;
public partial class NetworkIdentityHarness : Node
{
 public override void _Ready(){ var r=new NetworkEntityRegistry(); var n=new Node(); int a=r.AllocateId(),b=r.AllocateId(); GD.Print($"[IdentityTest] ids={a},{b} register={r.Register(a,n)} resolve={r.TryResolve(a,out _)} missing={r.TryResolve(999,out _)} duplicate={r.Register(a,new Node())}"); r.Unregister(a); GD.Print($"[IdentityTest] removed={!r.Contains(a)} nonzero={a>0&&b>0}"); }
}
