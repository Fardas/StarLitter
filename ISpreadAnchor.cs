using System;
using System.Drawing;
using Godot;

namespace Starlitter;

public interface ISpreadAnchor : IAnchor
{
    public float MaxDistance { get; set; }
}
