using System;
using System.Drawing;
using Godot;

namespace Starlitter;

public interface IAnchor
{
    public float Logarithmic { get; set; }

    public int StarCount { get; set; }
    public int CurrentStarCount { get; set; }
    public float MinDistance { get; set; }
    public int GroupId { get; set; }
}
