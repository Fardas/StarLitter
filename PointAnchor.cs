using System;
using System.Drawing;
using Godot;

namespace Starlitter;

public partial class PointAnchor : Marker2D, ISpreadAnchor
{
    [Export]
    public float Logarithmic { get; set; } = 0;

    [Export]
    public float MaxDistance { get; set; } = 100f;

    [Export]
    public int StarCount { get; set; } = 10;
    public int CurrentStarCount { get; set; } = 0;

    [Export]
    public float MinDistance { get; set; } = 20f;
    public int GroupId { get; set; }
}
