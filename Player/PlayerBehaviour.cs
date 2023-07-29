using Godot;
using Proto;
using System;

public partial class PlayerBehaviour : Node2D, IProcessBeat
{
	[Export] private float _errorGrace = 0.1f;
	[Export] private int _tileSizePixels = 32;
	[Export] private float _moveDurationSeconds = 0.2f;

	private BeatMachine _bm;
	private int _currentBeat;
	private int _actedBeat;
	private Tween _moveTween;
	private Vector2 _gridPosition;
	private double _elapsedAfterBeat;
	private BeatPhase _phase;
	private bool _canAct = true;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_bm = BeatMachine.Instance;
		_bm.SingletonRegisterBeatProcessor(this);
		_gridPosition = Position;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		_elapsedAfterBeat = Mathf.PosMod(_bm.SingletonTimeBeat, 1);

		if (_phase == BeatPhase.Off && _elapsedAfterBeat >= 1 - _errorGrace)
			BeatGracePrep();

		if (_phase == BeatPhase.PostBeat && _elapsedAfterBeat >= _errorGrace)
			BeatGraceWrap();

		HandleInput();
	}

	private void BeatGracePrep()
	{
		_currentBeat++;
		_phase = BeatPhase.PreBeat;
		_canAct = true;
	}

	public void ProcessBeat(int beat)
	{
		_phase = BeatPhase.PostBeat;
	}

	private void BeatGraceWrap()
	{
		_phase = BeatPhase.Off;
		_canAct = false;
	}

	private void HandleInput()
	{
		if (!_canAct)
		{
			GD.Print(GetError());

			return; // TODO: Stumble?
		}
			

		string action = string.Empty;

		if (Input.IsActionJustPressed("right"))
			action = "right";

		if (Input.IsActionJustPressed("left"))
			action = "left";

		if (Input.IsActionJustPressed("up"))
			action = "up";

		if (Input.IsActionJustPressed("down"))
			action = "down";

		if (string.IsNullOrEmpty(action))
			return;

		_canAct = false;
		ConsumeAction(action);
	}

	/// <summary>
	/// Calculates how far off the beat you currently are.
	/// </summary>
	/// <returns>The distance to the nearest beat in beats, ranges from -0.5 to 0.5.</returns>
	private float GetError()
	{
		float beat = Mathf.PosMod(_bm.SingletonTimeBeat, 1);
		float error = beat > 0.5 ? beat - 1 : beat;

		return error;
	}

	private void ConsumeAction(string action)
	{
		switch (action)
		{
			case "left":
				Move(Vector2.Left);
				break;
			case "right":
				Move(Vector2.Right);
				break;
			case "up":
				Jump();
				break;
			case "down":
				// Does not do anything for the moment
				break;
		}
	}

	private void Stumble()
	{
		GD.Print("Stumble!");
	}

	private void Move(Vector2 direction)
	{
		_gridPosition += direction * _tileSizePixels;

		if (_moveTween != null)
			_moveTween.Kill();

		_moveTween = GetTree().CreateTween();
		_moveTween.TweenProperty(this, "position", _gridPosition, _moveDurationSeconds).SetTrans(Tween.TransitionType.Expo);
		_moveTween.SetEase(Tween.EaseType.InOut);
	}

	private void Jump()
	{

	}

	private void HandleGravity()
	{

	}

	private enum BeatPhase
	{
		Off,
		PreBeat,
		PostBeat
	}
}
