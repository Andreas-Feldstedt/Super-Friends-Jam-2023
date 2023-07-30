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
	private int _jumpCharge = 1;

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

	private PhysicsDirectSpaceState2D _space;
	private RayCast2D _groundRay;
	private RayCast2D _gravityRay;
	private AnimatedSprite2D _sprite;
	private Vector2 _spriteOffset;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_bm = BeatMachine.Instance;
		_bm.SingletonRegisterBeatProcessor(this);
		_gridPosition = Position;
		_groundRay = GetNode<RayCast2D>("GroundRay");
		_gravityRay = GetNode<RayCast2D>("GravityRay");
		_sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");

		_spriteOffset = _sprite.Position;
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

	public override void _PhysicsProcess(double delta)
	{
		base._PhysicsProcess(delta);

		_space = GetWorld2D().DirectSpaceState;
	}

	private void BeatGracePrep()
	{
		_currentBeat++;
		_phase = BeatPhase.PreBeat;
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
		_isGrounded = GroundCheck();
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

	private void ConsumeAction(string action, bool pressed, int beat)
	{
		if (pressed)
		{
			// TODO: Set charge-up animation
			_sprite.Animation = "charging";

			if (action == "left")
			{
				_sprite.FlipH = true;
			}
			else if (action == "right")
			{
				_sprite.FlipH = false;
			}
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
		if (!_isGrounded)
			return;

		Vector2 raycastOrigin = _gridPosition + Vector2.Up * 16;
		Vector2 rayCastDestination = raycastOrigin + (direction * _tileSizePixels);

		PhysicsRayQueryParameters2D query = PhysicsRayQueryParameters2D.Create(raycastOrigin, rayCastDestination);
		Godot.Collections.Dictionary result = _space.IntersectRay(query);

		if (result.Count == 0)
		{
			_gridPosition = _gridPosition + direction * _tileSizePixels * 2;
			Position = _gridPosition;
			_sprite.Position -= direction * _tileSizePixels * 2;
			_sprite.Animation = "run";

			_moveTween?.Kill();
			_moveTween = GetTree().CreateTween();
			_moveTween.TweenProperty(_sprite, "position", _spriteOffset, _moveDurationSeconds).SetTrans(Tween.TransitionType.Expo);
			_moveTween.SetEase(Tween.EaseType.InOut);
			_moveTween.TweenCallback(Callable.From(() => _sprite.Animation = "default")).SetDelay(_moveDurationSeconds);

			_isGrounded = GroundCheck();

			_canAct = false;
		}
	}

	private void Move(Vector2 direction)
	{
		Vector2 raycastOrigin = _gridPosition + Vector2.Up * 16;
		Vector2 rayCastDestination = raycastOrigin + (direction * _tileSizePixels);

		PhysicsRayQueryParameters2D query = PhysicsRayQueryParameters2D.Create(raycastOrigin, rayCastDestination);
		Godot.Collections.Dictionary result = _space.IntersectRay(query);

		if (result.Count == 0)
		{
			_gridPosition = _gridPosition + direction * _tileSizePixels;
			Position = _gridPosition;
			_sprite.Position -= direction * _tileSizePixels;
			_sprite.Animation = "run";

			_moveTween?.Kill();
			_moveTween = GetTree().CreateTween();
			_moveTween.TweenProperty(_sprite, "position", _spriteOffset, _moveDurationSeconds).SetTrans(Tween.TransitionType.Expo);
			_moveTween.SetEase(Tween.EaseType.InOut);
			_moveTween.TweenCallback(Callable.From(() => _sprite.Animation = "default")).SetDelay(_moveDurationSeconds);

			_isGrounded = GroundCheck();

			_canAct = false;
		}
	}

	private void Jump()
	{
		if (_isGrounded)
		{
			_skipGravity = true;

			Vector2 raycastOrigin = _gridPosition + Vector2.Up * _tileSizePixels;
			Vector2 rayCastDestination = raycastOrigin + (Vector2.Up * _tileSizePixels);

			PhysicsRayQueryParameters2D query = PhysicsRayQueryParameters2D.Create(raycastOrigin, rayCastDestination);
			Godot.Collections.Dictionary result = _space.IntersectRay(query);

			if(result.Count == 0)
			{
				_gridPosition = _gridPosition + Vector2.Up * _tileSizePixels;
				Position = _gridPosition;
				_sprite.Position -= Vector2.Up * _tileSizePixels;
				_sprite.Animation = "jump";

				_moveTween?.Kill();
				_moveTween = GetTree().CreateTween();
				_moveTween.TweenProperty(_sprite, "position", _spriteOffset, _moveDurationSeconds).SetTrans(Tween.TransitionType.Expo);
				_moveTween.SetEase(Tween.EaseType.InOut);
				_moveTween.TweenCallback(Callable.From(() => _sprite.Animation = "default")).SetDelay(_moveDurationSeconds);

				_isGrounded = GroundCheck();
			}
		}
		else
		{
			DoubleJump();
		}

		_canAct = false;
	}

	private void DoubleJump()
	{

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
			if (_gravityRay.IsColliding())
			{
				if (_moveTween != null)
				{
					_sprite.Position = _spriteOffset;
					_moveTween.Kill();
				}

				_gridPosition = _gravityRay.GetCollisionPoint();

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
		return _groundRay.IsColliding();
	}

	private enum BeatPhase
	{
		Off,
		PreBeat,
		PostBeat
	}
}
