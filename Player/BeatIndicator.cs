using Godot;
using Proto;
using System;

public partial class BeatIndicator : Sprite2D, IProcessBeat
{
	public override void _Ready()
	{
	}

	public override void _Process(double delta)
	{
	}

    public void ProcessBeat(int beat)
    {
        throw new NotImplementedException();
    }
}
