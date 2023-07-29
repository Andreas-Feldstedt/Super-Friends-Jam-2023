using Godot;
using Proto;
using System;

public partial class PlayerBehaviour : Node2D, IProcessBeat
{
	[Export] private float _errorThreshold = 0.1f;
	[Export] private int _tileSizePixels = 32;
	[Export] private float _tweenBeatFraction = 0.5f;

	private BeatMachine _bm;
	private int _currentBeat;
	private int _actedBeat;
	private Tween _moveTween;
	private Vector2 _gridPosition;

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
		HandleInput();
	}

	public void ProcessBeat(int beat)
	{
		_currentBeat = beat;
		GD.Print(beat);
	}

	private void HandleInput()
	{
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

		if (_actedBeat == _currentBeat)
			return;

		float error = GetError();
		_actedBeat = error > 0 ? _currentBeat - 1 : _currentBeat;

		if (Mathf.Abs(error) < _errorThreshold)
		{
			ConsumeAction(action);
		}
		else
		{
			Stumble();
		}
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

		float duration = 1 / (_bm.SingletonBpm / 60);

		_moveTween = GetTree().CreateTween();
		_moveTween.TweenProperty(this, "position", _gridPosition, duration).SetTrans(Tween.TransitionType.Expo);
		_moveTween.SetEase(Tween.EaseType.InOut);
	}

	private void Jump()
	{

	}

	private void HandleGravity()
	{

	}
}
