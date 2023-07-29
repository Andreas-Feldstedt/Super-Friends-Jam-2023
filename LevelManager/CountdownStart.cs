using Godot;
using Proto;

namespace Managers;

public partial class CountdownStart : RichTextLabel, IProcessBeat
{
    [Export] private float _scaleRatio = 1.3f;
    [Export] private int _beatDuration;
    [Export] private int _numCountdowns = 3;

    private int _texCurrent;
    private int _lastBeatSwap = -1;

    private Vector2 _a;
    private Vector2 _b;

    public override void _Ready()
    {
        _a = Scale * 1.3f;
        _b = Scale;
        
        Text = $"{_numCountdowns}";
        
        BeatMachine.Instance.SingletonRegisterBeatProcessor(this);
    }

    public void Start()
    {
        _lastBeatSwap = BeatMachine.Instance.SingletonBeat;;
    }
    
    public override void _Process(double delta)
    {
        // if (_lastBeatSwap == -1)
        //     return;
        
        // gets fractional part of a beat
        float t = Mathf.PosMod(_lastBeatSwap - BeatMachine.Instance.SingletonTimeBeat, _beatDuration);
        
        Position = _a.Lerp(_b, t);
    }

    public void ProcessBeat(int beat)
    {
        // if (_lastBeatSwap == -1)
        //     return;

        if (_lastBeatSwap + _beatDuration != beat)
            return;
        
        _lastBeatSwap = beat;
        ++_texCurrent;
        Text = $"{_numCountdowns - _texCurrent}";
    }
}