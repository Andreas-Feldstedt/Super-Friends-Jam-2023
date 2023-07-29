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
	private Tween _moveTween;
	private Vector2 _gridPosition;
	private double _elapsedAfterBeat;
	private BeatPhase _phase;
	private bool _canAct = true;
	private bool _skipGravity;
	private bool _isGrounded;

	private string _currentAction = "empty";
	private bool _currentActionPressed = false;
	private int _currentActionBeat = -1;

	private string[] _actions = new string[]
	{
		"up",
		"down",
		"left",
		"right"
	};


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
		_isGrounded = GroundCheck();
		_canAct = true;
	}

	public void ProcessBeat(int beat)
	{
		_currentBeat = beat;
		_phase = BeatPhase.PostBeat;
	}

	private void BeatGraceWrap()
	{
		_phase = BeatPhase.Off;
		HandleGravity();
		_canAct = false;
	}

	private void HandleInput()
	{
		if (!_canAct)
			return; // TODO: Stumble?

		foreach (string action in _actions)
		{
			if (Input.IsActionJustPressed(action))
			{
				ConsumeAction(action, true, _currentBeat);

				return;
			}
			else if (Input.IsActionJustReleased(action))
			{
				ConsumeAction(action, false, _currentBeat);

				return;
			}
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

	private void ConsumeAction(string action, bool pressed, int beat)
	{
		if (pressed)
		{
			// TODO: Set charge-up animation
		}
		else
		{
			if (action == _currentAction && _currentActionPressed)
			{
				if (beat == _currentActionBeat)
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
				else
				{
					switch (action)
					{
						case "left":
							Dash(Vector2.Left);
							break;
						case "right":
							Dash(Vector2.Right);
							break;
						case "up":
						// TODO: Charged jump
						case "down":
						default:
							break;
					}
				}
			}
			else
			{

			}
		}

		_currentAction = action;
		_currentActionPressed = pressed;
		_currentActionBeat = beat;
	}

	private void Dash(Vector2 direction)
	{
		_gridPosition += direction * _tileSizePixels * 2;

		if (_moveTween != null)
			_moveTween.Kill();

		_moveTween = GetTree().CreateTween();
		_moveTween.TweenProperty(this, "position", _gridPosition, _moveDurationSeconds).SetTrans(Tween.TransitionType.Expo);
		_moveTween.SetEase(Tween.EaseType.InOut);

		_canAct = false;
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

		_canAct = false;
	}

	private void Jump()
	{
		_canAct = false;
	}

	private void HandleGravity()
	{
		if (_skipGravity)
		{
			_skipGravity = false;
		}
		else if (_isGrounded)
		{

		}
		else
		{
			PhysicsDirectSpaceState2D spaceState = GetWorld2D().DirectSpaceState;
			PhysicsRayQueryParameters2D query = PhysicsRayQueryParameters2D.Create(Position, Position + Vector2.Down * Mathf.Inf);
			Godot.Collections.Dictionary result = spaceState.IntersectRay(query);

			if (result.Count > 0)
			{
				_gridPosition = (Vector2)result["position"];

				if (_moveTween != null)
					_moveTween.Kill();

				_moveTween = GetTree().CreateTween();
				_moveTween.TweenProperty(this, "position", _gridPosition, _moveDurationSeconds).SetTrans(Tween.TransitionType.Expo);
				_moveTween.SetEase(Tween.EaseType.InOut);
			}
			else
			{
				GD.PrintErr("Bottomless pit!");
			}
		}
	}

	private bool GroundCheck()
	{
		PhysicsDirectSpaceState2D spaceState = GetWorld2D().DirectSpaceState;
		PhysicsRayQueryParameters2D query = PhysicsRayQueryParameters2D.Create(Position, Position + Vector2.Down * 16);
		Godot.Collections.Dictionary result = spaceState.IntersectRay(query);

		return result.Count > 0;
	}

	private enum BeatPhase
	{
		Off,
		PreBeat,
		PostBeat
	}
}
