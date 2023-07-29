using Godot;

namespace Proto;

public partial class Metronome : Sprite2D
{
	[Export] private float _scaleRatio = 1.3f;
	[Export] private float _beatDuration = 1;
	
	private Vector2 _a;
	private Vector2 _b;

	public override void _Ready()
	{
		_a = Scale * 1.3f;
		_b = Scale;
	}
	
	public override void _Process(double delta)
	{
		// gets fractional part of a beat
		float t = Mathf.PosMod(BeatMachine.Instance.SingletonTimeBeat, _beatDuration);
		t *= 1 - Mathf.Pow(1 - t, 2);
			
		Scale = _a.Lerp(_b, t);
	}
}
