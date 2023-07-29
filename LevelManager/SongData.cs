using Godot;

namespace Managers;

public partial class SongData : Resource
{
    [Export] public AudioStream Stream;
    [Export] public float Bpm;
    [Export] public bool Loop;
}