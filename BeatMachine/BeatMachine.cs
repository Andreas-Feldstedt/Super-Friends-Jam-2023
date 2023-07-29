using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Godot;

namespace Proto;

public partial class Lane : Resource
{
	[Export] public float pulseHz;
	[Export] public string pattern;
	[Export] public int nHarmonics = 4;
}
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
	
	// [ExportCategory("Lanes")]
	[Export] private Lane[] _lanes;
	
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
	
	private Vector2[] _buffer;
	private bool _bufferReady;
	
	private Thread _threadFillBuffer;
	
	#region Beat Processors
	
	private readonly List<IProcessBeat> _beatProcessors = new();
	
	public void SingletonRegisterBeatProcessor(IProcessBeat processor) { _beatProcessors.Add(processor); }
	public void SingletonUnregisterBeatProcessor(IProcessBeat processor) { _beatProcessors.Remove(processor); }
	
	private string[] _beatInputs =
	{
		"TestBeatActoin"
	};
		
	private Godot.Collections.Dictionary<string, float> _inputAnticipation;
	public bool SingletonIsActionPressedBeat(string action) => _inputAnticipation.ContainsKey(action);
	
	#endregion Beat Processors
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Debug.Assert(_instance == null);
		_instance = this;
		
		Stream = new AudioStreamGenerator();
		(Stream as AudioStreamGenerator).MixRate = _sampleHz;
	
		// TMP array of resources is acting up
		_lanes = new Lane[]{
			// new() { pulseHz = 440.000f, pattern = "---X---X---X---X", nHarmonics = 256 },
			new() { pulseHz = 440.000f, pattern = "X---X---X---X---", nHarmonics = 256 },
			new() { pulseHz = 493.883f, pattern = "X---------------", nHarmonics = 256 }
		};
		
		_spb = 60f / _bpm;
	
		_samplesPerBeat = Mathf.FloorToInt(_sampleHz * _spb);
	
		_buffer = new Vector2[_samplesPerBeat];
		
		// need to start playing before filling the buffer
		Play();
		_playback = GetStreamPlayback() as AudioStreamGeneratorPlayback;
		
		// prefill
		FillBuffer();
	
		_threadFillBuffer = new Thread(() =>
		{
			while (true)
				if (_playback.GetFramesAvailable() > _samplesPerBeat)
				{
					ClearBuffer();
					FillBuffer();
					_bufferReady = true;
				}
		});
		_threadFillBuffer.Start();
	
		_inputAnticipation = new();
	}
	
	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		_songPosSecs = GetPlaybackPosition()
					   + (float)AudioServer.GetTimeSinceLastMix()
					   - (float)AudioServer.GetOutputLatency();
	
		_songPosBeat = _songPosSecs / _spb;
		
	
		foreach (var beatInput in _beatInputs)
			if(Input.IsActionJustPressed(beatInput))
				if (_inputAnticipation.ContainsKey(beatInput))
				{
					// multipress, invalidate
					_inputAnticipation[beatInput] = -1;
				}
				else
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
	
			_inputAnticipation.Clear();
		}
	}
	
	private void PlayNote(float pulseHz, int harmonics = 1)
	{
		for (int h = 1; h <= harmonics; ++h)
		{
			float phase = 0f;
			float increment = pulseHz * h / _sampleHz;
			Vector2 unit = (Vector2.One / h) / 2f;
			for (int i = 0; i < _samplesPerBeat; ++i)
			{
				_buffer[i] += unit * Mathf.Sin(phase * Mathf.Tau);
				phase = Mathf.PosMod(phase + increment, 1.0f);
			}
		}
	}
	
	private void FillBuffer()
	{
		++_prefilledBeat;
		foreach (var lane in _lanes)
		{
			int patternPosition = _prefilledBeat % lane.pattern.Length;
			switch (lane.pattern[patternPosition])
			{
				case 'X': PlayNote(lane.pulseHz, lane.nHarmonics); break;
			}
		}
		
		
		_playback.PushBuffer(_buffer);
	}
	
	private void ClearBuffer()
	{
		for (int i = 0; i < _samplesPerBeat; ++i)
			_buffer[i] = Vector2.Zero;
	}
	
	#region Singleton Interface
	
	public int SingletonTimeBeat => _gameplayBeat;
	public float SingletonTimeSecs => _songPosSecs;
	
	#endregion Singleton Interface
}
