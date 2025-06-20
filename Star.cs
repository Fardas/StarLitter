using System.Collections.Generic;
using Godot;

namespace Starlitter;

public partial class Star(StarSize starSize) : Node2D
{
    public int GroupId { get; set; } = 0;
    public static Dictionary<int, int> GroupCounts { get; set; } = [];
    public static Dictionary<int, float> GroupAlphas { get; set; } = [];
    public StarSize StarSize { get; private set; } = starSize;

    public MeshInstance2D Mesh { get; private set; } = new MeshInstance2D();
    public SphereMesh SphereMesh { get; private set; } = new SphereMesh();
    public Label Label { get; private set; } = new Label();
    public static bool AnimationOn { get; set; } = false;

    public override void _Ready()
    {
        Label.Text = GroupId.ToString();
        if (!GroupCounts.TryGetValue(GroupId, out int value))
        {
            value = 0;
            GroupCounts[GroupId] = value;
            GroupAlphas[GroupId] = 1.0f;
        }
        GroupCounts[GroupId] = ++value;

        Mesh.Mesh = SphereMesh;
        AddChild(Mesh);
        AddChild(Label);
        Mesh.Position = Vector2.Zero;
        // SetProcess(false);
    }

    public override void _Process(double delta)
    {
        if (StarGenerator.GroupToggle == -1 || StarGenerator.GroupToggle == GroupId)
        {
            Visible = true;
        }
        else
        {
            Visible = false;
        }

        SphereMesh.RadialSegments = 20;
        SphereMesh.Radius = (float)StarSize / (StarGenerator.RenderRealisticSize ? 2000f : 250f);
        SphereMesh.Height = (float)StarSize / (StarGenerator.RenderRealisticSize ? 1000f : 125f);

        Label.Visible = StarGenerator.RenderLabel;
        Mesh.Material = null;
        Mesh.Modulate = StarGenerator.RenderAllWhite
            ? new(1.0f, 1.0f, 1.0f)
            : GroupColors[GroupId % GroupColors.Count];
        Mesh._Draw();

        if (AnimationOn)
        {
            Mesh.Modulate = new(
                Mesh.Modulate.R,
                Mesh.Modulate.G,
                Mesh.Modulate.B,
                GroupAlphas[GroupId]
            );
        }
        else
        {
            Mesh.Modulate = new(Mesh.Modulate.R, Mesh.Modulate.G, Mesh.Modulate.B, 1.0f);
        }
    }

    public static List<Color> GroupColors { get; set; } =
        [
            new Color(0.0f, 0.0f, 0.0f),
            new Color(0.0f, 1.0f, 1.0f),
            new Color(0.5f, 0.0f, 0.0f),
            new Color(1.0f, 0.0f, 1.0f),
            new Color(0.0f, 0.5f, 0.0f),
            new Color(0.5f, 0.5f, 0.5f),
            new Color(0.0f, 0.0f, 0.5f),
            new Color(1.0f, 0.0f, 0.0f),
            new Color(1.0f, 1.0f, 1.0f),
            new Color(0.5f, 0.5f, 0.0f),
            new Color(0.0f, 1.0f, 0.0f),
            new Color(0.0f, 0.5f, 0.5f),
            new Color(0.0f, 0.0f, 1.0f),
            new Color(0.5f, 0.0f, 0.5f),
            new Color(1.0f, 1.0f, 0.0f),
        ];
}
