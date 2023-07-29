using Godot;

namespace Proto;

public partial class TestBeatMachine : Node2D, IProcessBeat
{
    [Export] private Vector2 _offset;
    
    private Vector2 _start;

    public override void _Ready()
    {
        _start = Position;
    }

    public override void _Process(double delta)
    {
        // gets fractional part of a beat
        float t = Mathf.PosMod(BeatMachine.Instance.SingletonTimeBeat, 1);
        
        bool reverseBeat = BeatMachine.Instance.SingletonBeat % 2 == 0;
        
        Vector2 a = reverseBeat ?  _start : _start + _offset;
        Vector2 b = reverseBeat ?  _start + _offset : _start;

        Position = a.Lerp(b, t);
    }

    public void ProcessBeat(int beat)
    {
        GD.Print("beat");
    }
}
