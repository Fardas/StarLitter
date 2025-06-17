using System;
using System.Drawing;
using Godot;

namespace Starlitter;

public partial class FallingStarAnchor : Line2D, IAnchor
{
    [Export]
    public float Logarithmic { get; set; } = 0;

    [Export]
    public int StarCount { get; set; } = 10;
    public int CurrentStarCount { get; set; } = 0;

    [Export]
    public float MinDistance { get; set; } = 20f;
    public int GroupId { get; set; }
}
