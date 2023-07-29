using System.Collections.Generic;
using System.Diagnostics;
using Godot;

namespace Proto;

public partial class BeatMachine : AudioStreamPlayer2D
{
    #region Singleton
    private static BeatMachine _instance;
    public static BeatMachine Instance => _instance;
    #endregion Singleton

    private AudioStreamGeneratorPlayback _playback;

    /// beats per minute
    [Export] private float _bpm = 100;

    /// seconds per beat 
    [Export] private float _spb;

    /// sampling frequency
    [Export] private float _sampleHz = 22050.0f;

    [ExportCategory("Debug")]
    [Export] private int _samplesPerBeat;
    [Export] private float _songPosSecs;
    [Export] private float _songPosBeat;

    /// <summary>
    /// We greedily fill the buffer so that there is always something to play
    /// </summary>
    /// <remarks>
    /// This is NOT the current position of the song for gameplay or other reasons
    /// </remarks>
    [Export] private int _prefilledBeat;

    [ExportCategory("Gameplay Debug")]
    [Export] private int _gameplayBeat;

    #region Beat Processors

    private readonly List<IProcessBeat> _beatProcessors = new();

    private string[] _beatInputs =
    {
        "TestBeatActoin",
        "up",
        "down",
        "left",
        "right"
    };

    private Godot.Collections.Dictionary<string, float> _inputAnticipation;

    #endregion Beat Processors

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        Debug.Assert(_instance == null);
        _instance = this;

        _spb = 60f / _bpm;

        _samplesPerBeat = Mathf.FloorToInt(_sampleHz * _spb);
        _inputAnticipation = new();

        foreach (var key in _beatInputs)
            _inputAnticipation[key] = -1;

    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        _songPosSecs = GetPlaybackPosition()
                       + (float)AudioServer.GetTimeSinceLastMix()
                       - (float)AudioServer.GetOutputLatency();

        _songPosBeat = _songPosSecs / _spb;


        foreach (var beatInput in _beatInputs)
            if (Input.IsActionJustPressed(beatInput))
            {
                _inputAnticipation[beatInput] = Mathf.PosMod(_songPosBeat, 1f);
            }

        if (Mathf.FloorToInt(_songPosBeat) > _gameplayBeat)
        {
            // do gameplay stuff
            ++_gameplayBeat;
            for (var i = 0; i < _beatProcessors.Count; i++)
            {
                var beatProcessor = _beatProcessors[i];
                if (!IsInstanceValid(beatProcessor as GodotObject))
                {
                    _beatProcessors.RemoveAt(i);
                    --i;
                }
                beatProcessor.ProcessBeat(_gameplayBeat);
            }

            foreach (var key in _inputAnticipation.Keys)
                _inputAnticipation[key] = -1;
        }
    }

    #region Singleton Interface

    public int SingletonBeat => _gameplayBeat;
    public float SingletonTimeBeat => _songPosBeat;
    public float SingletonTimeSecs => _songPosSecs;
    public float SingletonBpm => _bpm;

    public void SingletonRegisterBeatProcessor(IProcessBeat processor) { _beatProcessors.Add(processor); }
    public void SingletonUnregisterBeatProcessor(IProcessBeat processor) { _beatProcessors.Remove(processor); }

    public bool SingletonIsActionPressedBeat(string action) => _inputAnticipation[action] > 0;
    public float SingletonGetActionAnticipation(string action) => _inputAnticipation[action];

    #endregion Singleton Interface
}
