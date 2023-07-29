using Godot;
using Proto;

public partial class PlayerBehaviour : Node2D, IProcessBeat
{
    [Export] private int _tileSizePixels = 32;
    [Export] private float _tweenBeatFraction = 0.5f;

    private BeatMachine _bm;
    private bool _isJumping;
    private Tween _moveTween;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        _bm = BeatMachine.Instance;
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {

    }

    private void MoveTween()
    {

    }

    public void ProcessBeat(int beat)
    {
        string input = GetInput();

        if (!string.IsNullOrEmpty(input))
        {
            switch (input)
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
            // Do we want to do something when idle?
        }
    }

    /// <summary>
    ///  Retrieves a single input from the beat machine.
    /// </summary>
    /// <returns>A single input action as a string.</returns>
    private string GetInput()
    {
        if (_bm.SingletonIsActionPressedBeat("left"))
            return "left";

        if (_bm.SingletonIsActionPressedBeat("right"))
            return "right";

        if (_bm.SingletonIsActionPressedBeat("up"))
            return "up";

        if (_bm.SingletonIsActionPressedBeat("down"))
            return "down";

        return null;
    }

    private void Move(Vector2 direction)
    {
        Vector2 targetPosition = Position + direction * _tileSizePixels;

        if (_moveTween != null)
            _moveTween.Kill();

        _moveTween = GetTree().CreateTween();
        _moveTween.TweenProperty(this, "position", targetPosition, 1);
    }

    private void Jump()
    {

    }

    private void HandleGravity()
    {

    }
}
