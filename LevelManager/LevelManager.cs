using Godot;
using Proto;

namespace Managers;

public partial class LevelManager : Node
{
	[Export] private LevelData _levelData;
	
	private BeatMachine _beatMachine;
	
	public override void _Ready()
	{
		_beatMachine = GetNode<BeatMachine>("BeatMachine");
		SetSong(_levelData.SongStart);
		_beatMachine.Playing = true;
	}

	public void SetSong(SongData data)
	{
		_beatMachine.Stream = data.Stream;
		_beatMachine.Bpm = data.Bpm;
	}
}
