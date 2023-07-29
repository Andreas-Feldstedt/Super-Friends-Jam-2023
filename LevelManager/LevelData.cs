using Godot;

namespace Managers;

public partial class LevelData : Resource
{
    [Export] public SongData SongStart;
    [Export] public SongData SongLoop;
    [Export] public SongData SongEnd;
}
