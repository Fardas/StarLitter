using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Godot;

namespace Starlitter;

public partial class StarGenerator : Control
{
    [Export]
    public int SmallStars { get; set; } = 300;

    [Export]
    public int MidStars { get; set; } = 200;

    [Export]
    public int LargeStars { get; set; } = 30;

    public List<Star> Stars { get; private set; } = [];
    public readonly int MaxGridX = 6;
    public readonly int MaxGridY = 4;
    public float GridSizeX { get; set; }
    public float GridSizeY { get; set; }
    public List<Node2D> Gizmos { get; set; } = [];
    public int IdealGroupCount { get; set; }

    [Export]
    public Area2D[] Areas { get; private set; }
    public float BoundMinX { get; private set; }
    public float BoundMinY { get; private set; }
    public float BoundWidth { get; private set; }
    public float BoundHeight { get; private set; }

    [Export]
    public Node2D AnchorCollection { get; private set; }
    public List<IAnchor> Anchors { get; private set; } = [];

    [Export]
    public Label GroupLabel { get; private set; }
    public static int GroupToggle { get; private set; } = -1;

    [Export]
    public int RandomSeed { get; private set; } = 0;
    public Random Random { get; private set; } = new Random();
    public Dictionary<int, double> RandomShiftPerGroupId { get; private set; } = [];

    [Export]
    public float AnchorMaxDistance { get; set; } = 100f;

    public static bool RenderLabel { get; set; } = false;

    public Dictionary<(int, int), Vector2> GridPositions { get; set; } = [];

    public override void _Ready()
    {
        if (RandomSeed != 0)
        {
            Random = new Random(RandomSeed);
        }

        if (Areas.Length == 0)
        {
            GD.PrintErr("StarGenerator: Area is not set. Please assign an Area2D node.");
            return;
        }
        for (int i = 0; i < MaxGridX * MaxGridY; ++i)
        {
            RandomShiftPerGroupId[i] = Random.NextDouble() * Math.PI / 2;
        }
        (BoundMinX, BoundMinY, BoundWidth, BoundHeight) = GetBounds();

        GridSizeX = BoundWidth / MaxGridX;
        GridSizeY = BoundHeight / MaxGridY;
        for (int i = 0; i < MaxGridX; ++i)
        {
            for (int j = 0; j < MaxGridY; ++j)
            {
                GridPositions[(i, j)] = new(
                    i * GridSizeX + GridSizeX / 2 + BoundMinX,
                    j * GridSizeY + GridSizeY / 2 + BoundMinY
                );
                Gizmos.Add(new Node2D());

                MeshInstance2D mesh = new()
                {
                    Mesh = new SphereMesh
                    {
                        RadialSegments = 20,
                        Radius = 20.0f,
                        Height = 40.0f,
                    },
                    Position = GridPositions[(i, j)],
                };
                Gizmos.Last().AddChild(mesh);
                Gizmos.Last().Visible = false;
                AddChild(Gizmos.Last());
            }
        }

        IdealGroupCount = (int)((SmallStars + MidStars + LargeStars) / (GridSizeX * GridSizeY));

        CollectAnchors();

        var fallingStarAnchors = Anchors.Where(anchor =>
            anchor.GetType() == typeof(FallingStarAnchor)
        );
        GD.Print("FALLINGSTARANCHOR COUNT:", fallingStarAnchors.Count());
        foreach (FallingStarAnchor fallingStar in fallingStarAnchors.Cast<FallingStarAnchor>())
        {
            var totalLength = fallingStar.TotalLength();
            var logBase = Math.Log(totalLength, fallingStar.StarCount);
            for (int i = 0; i < fallingStar.StarCount; ++i)
            {
                var pointDistance = Math.Pow(i, logBase);

                var star = new Star(StarSize.Large)
                {
                    Position = fallingStar.FixedPoint((float)pointDistance),
                };

                star.GroupId = fallingStar.GroupId;
                Stars.Add(star);
            }
        }
        Anchors.RemoveAll(anchor => anchor is FallingStarAnchor);

        var smallStars = SmallStars;
        var midStars = MidStars;
        var largeStars = LargeStars;
        while (0 < smallStars && 0 < midStars && 0 < largeStars)
        {
            var selectedStarSize = StarSize.Large;
            var randomStar = Random.Next(smallStars + midStars + largeStars);
            if (randomStar <= smallStars)
            {
                selectedStarSize = StarSize.Small;
            }
            else if (randomStar <= smallStars + midStars)
            {
                selectedStarSize = StarSize.Medium;
            }
            var success = false;
            while (success == false)
            {
                success = GenerateStar(selectedStarSize);
            }
        }

        for (var i = 0; i < SmallStars; ++i)
        {
            var success = false;
            while (success == false)
            {
                success = GenerateStar(StarSize.Small);
            }
        }

        for (var i = 0; i < MidStars; ++i)
        {
            var success = false;
            while (success == false)
            {
                success = GenerateStar(StarSize.Medium);
            }
        }

        for (var i = 0; i < LargeStars; ++i)
        {
            var success = false;
            while (success == false)
            {
                success = GenerateStar(StarSize.Large);
            }
        }

        foreach (var star in Stars)
        {
            AddChild(star);
        }
    }

    // public List<int> Get3NearestGroups(Vector2 starPosition)
    // {
    //     var nearestGridCoordinates = (
    //         Math.Min(
    //             MaxGridX - 1,
    //             Math.Max(
    //                 0,
    //                 (int)Math.Round((starPosition.X - BoundMinX - GridSizeX / 2) / GridSizeX)
    //             )
    //         ),
    //         Math.Min(
    //             MaxGridY - 1,
    //             Math.Max(
    //                 0,
    //                 (int)Math.Round((starPosition.Y - BoundMinY - GridSizeY / 2) / GridSizeY)
    //             )
    //         )
    //     );

    //     var nearestGridPosition = GridPositions[nearestGridCoordinates];

    //     var xDistance = starPosition.X - nearestGridPosition.X;
    //     var yDistance = starPosition.Y - nearestGridPosition.Y;
    //     var grid1XOffset = xDistance < 0 ? -1 : 1;
    //     var grid2YOffset = yDistance < 0 ? -1 : 1;

    //     (int second, int third) =
    //         Math.Abs(xDistance) < Math.Abs(yDistance)
    //             ? (
    //                 GroupIdFromGridPos(
    //                     Math.Max(
    //                         0,
    //                         Math.Min(MaxGridX - 1, nearestGridCoordinates.Item1 + grid1XOffset)
    //                     ),
    //                     nearestGridCoordinates.Item2
    //                 ),
    //                 GroupIdFromGridPos(
    //                     nearestGridCoordinates.Item1,
    //                     Math.Max(
    //                         0,
    //                         Math.Min(MaxGridY - 1, nearestGridCoordinates.Item2 + grid2YOffset)
    //                     )
    //                 )
    //             )
    //             : (
    //                 GroupIdFromGridPos(
    //                     nearestGridCoordinates.Item1,
    //                     Math.Max(
    //                         0,
    //                         Math.Min(MaxGridY - 1, nearestGridCoordinates.Item2 + grid2YOffset)
    //                     )
    //                 ),
    //                 GroupIdFromGridPos(
    //                     Math.Max(
    //                         0,
    //                         Math.Min(MaxGridX - 1, nearestGridCoordinates.Item1 + grid1XOffset)
    //                     ),
    //                     nearestGridCoordinates.Item2
    //                 )
    //             );

    //     return
    //     [
    //         GroupIdFromGridPos(nearestGridCoordinates.Item1, nearestGridCoordinates.Item2),
    //         second,
    //         third,
    //     ];
    // }

    // private int GroupIdFromGridPos(int x, int y)
    // {
    //     return y * MaxGridX + x;
    // }

    // public void PickGroup(Star star)
    // {
    //     var nearestGroupIds = Get3NearestGroups(star.Position);
    //     if (nearestGroupIds.Any(groupId => groupId > 24))
    //     {
    //         GD.Print(nearestGroupIds[0], ":", nearestGroupIds[1], ":", nearestGroupIds[2]);
    //     }

    //     var randomTake = Random.Next(105);
    //     star.GroupId =
    //         randomTake < 15 ? nearestGroupIds[2]
    //         : randomTake < 45 ? nearestGroupIds[1]
    //         : nearestGroupIds[0];
    // }

    public override void _Process(double delta)
    {
        if (Input.IsActionJustPressed("ui_label_toggle"))
        {
            RenderLabel = !RenderLabel;
        }
        if (Input.IsActionJustPressed("ui_group_toggle_up"))
        {
            GroupToggle = ++GroupToggle;
            var groupCount =
                GroupToggle == -1 ? Stars.Count
                : Star.GroupCounts.ContainsKey(GroupToggle) ? Star.GroupCounts[GroupToggle]
                : 0;
            GroupLabel.Text =
                GroupToggle == -1 ? $"All: {groupCount}" : $"{GroupToggle}: {groupCount}";
        }
        if (Input.IsActionJustPressed("ui_group_toggle_down"))
        {
            GroupToggle = Math.Max(-1, --GroupToggle);
            var groupCount =
                GroupToggle == -1 ? Stars.Count
                : Star.GroupCounts.ContainsKey(GroupToggle) ? Star.GroupCounts[GroupToggle]
                : 0;
            GroupLabel.Text =
                GroupToggle == -1 ? $"All: {groupCount}" : $"{GroupToggle}: {groupCount}";
        }
        if (Input.IsActionJustPressed("ui_animation_toggle"))
        {
            Star.AnimationOn = !Star.AnimationOn;
        }
        if (Input.IsActionJustPressed("ui_gizmo_toggle"))
        {
            ToggleGizmos();
        }

        var time = Time.GetTicksMsec();
        if (Star.AnimationOn)
        {
            foreach (var key in Star.GroupAlphas.Keys.OrderBy(key => key))
            {
                Star.GroupAlphas[key] = (float)
                    Math.Sin(2 * Math.PI * ((time / 1500.0) + RandomShiftPerGroupId[key]));
            }
        }
    }

    private void ToggleGizmos()
    {
        Gizmos.ForEach(gizmo => gizmo.Visible = !gizmo.Visible);
    }

    public bool GenerateStar(StarSize starSize)
    {
        var star = new Star(starSize);

        IAnchor selectedAnchor = null;

        if (Anchors.Any(anchor => anchor.CurrentStarCount < anchor.StarCount))
        {
            selectedAnchor = Anchors[Random.Next(0, Anchors.Count)];

            ++selectedAnchor.CurrentStarCount;
        }
        if (selectedAnchor != null && selectedAnchor.CurrentStarCount == selectedAnchor.StarCount)
        {
            Anchors.Remove(selectedAnchor);
            GD.Print($"Removed anchor {selectedAnchor} because it reached its star count limit.");
        }

        var c = 100;
        do
        {
            if (selectedAnchor != null)
            {
                star.GroupId = selectedAnchor.GroupId;
                if (selectedAnchor is LineAnchor line)
                {
                    var randomPointOnLine = line.RandomPoint(Random);
                    star.Position = randomPointOnLine.RandomVector2InDistance(
                        line.MaxDistance,
                        line.Logarithmic,
                        Random
                    );
                }
                else if (selectedAnchor is PointAnchor point)
                {
                    star.Position = point.Position.RandomVector2InDistance(
                        point.MaxDistance,
                        point.Logarithmic,
                        Random
                    );
                }
            }
            else
            {
                star.Position = new Vector2(
                    (float)Random.NextDouble() * BoundWidth + BoundMinX,
                    (float)Random.NextDouble() * BoundHeight + BoundMinY
                );
            }
            --c;
        } while (
            (
                !InsideAreas(star.Position)
                || Stars.Any(otherStar =>
                    star.Position.DistanceTo(otherStar.Position)
                    < (selectedAnchor?.MinDistance ?? 20f)
                )
            )
            && c != 0
        );
        if (c == 0 && selectedAnchor != null)
        {
            Anchors.Remove(selectedAnchor);
            GD.Print($"Failed to generate star for anchor {selectedAnchor}. Anchor removed.");
            return false;
        }
        else if (c == 0)
        {
            GD.PrintErr(
                "Failed to generate star after 100 attempts. This should not happen, please check your area and anchor settings."
            );
            throw new Exception(
                "Failed to generate star after 100 attempts. This should not happen, please check your area and anchor settings."
            );
        }

        // PickGroup(star);
        Stars.Add(star);

        return true;
    }

    public bool InsideAreas(Vector2 position)
    {
        foreach (var area in Areas)
        {
            var collisionPolygon = area.GetNode<CollisionPolygon2D>("CollisionPolygon2D");
            if (collisionPolygon != null && collisionPolygon.Polygon.Length != 0)
            {
                // Transform the star's position to the local space of the area
                if (
                    Geometry2D.IsPointInPolygon(
                        area.ToLocal(position),
                        area.GetNode<CollisionPolygon2D>("CollisionPolygon2D").Polygon
                    )
                )
                {
                    return true;
                }
            }
        }
        return false;
    }

    public (float MinX, float MinY, float Width, float Height) GetBounds()
    {
        float minX = float.MaxValue;
        float maxX = float.MinValue;
        float minY = float.MaxValue;
        float maxY = float.MinValue;

        foreach (var area in Areas)
        {
            var polygon = area.GetNode<CollisionPolygon2D>("CollisionPolygon2D").Polygon;

            foreach (var point in polygon)
            {
                if (point.X < minX)
                    minX = point.X;
                if (point.X > maxX)
                    maxX = point.X;
                if (point.Y < minY)
                    minY = point.Y;
                if (point.Y > maxY)
                    maxY = point.Y;
            }
        }
        return (minX, minY, maxX - minX, maxY - minY);
    }

    public void CollectAnchors()
    {
        var groupId = 1;
        if (AnchorCollection == null)
        {
            // GD.PrintErr("Anchors node not found in the scene tree root.");
            return;
        }

        foreach (var child in AnchorCollection.GetChildren())
        {
            if (child is LineAnchor line)
            {
                Anchors.Add(line);
                line.GroupId = groupId;
                line.Visible = false;
                Gizmos.Add(line);
            }
            else if (child is PointAnchor marker)
            {
                Anchors.Add(marker);
                marker.GroupId = groupId;
                marker.Visible = false;
                Gizmos.Add(marker);
            }
            else if (child is FallingStarAnchor fallingLine)
            {
                Anchors.Add(fallingLine);
                fallingLine.GroupId = groupId;
                fallingLine.Visible = false;
                Gizmos.Add(fallingLine);
            }
            ++groupId;
        }
    }
}

public static class Extensions
{
    public static float TotalLength(this Line2D line)
    {
        float totalLength = 0;
        for (int i = 0; i < line.Points.Length - 1; i++)
        {
            var p1 = line.Points[i];
            var p2 = line.Points[i + 1];

            totalLength += p1.DistanceTo(p2);
        }
        return totalLength;
    }

    public static Vector2 FixedPoint(this Line2D line, float fixedLength)
    {
        var totalLength = line.TotalLength();

        var currentLength = 0f;
        for (int i = 0; i < line.Points.Length - 1; i++)
        {
            var p1 = line.Points[i];
            var p2 = line.Points[i + 1];

            if (fixedLength <= currentLength + p1.DistanceTo(p2))
            {
                return line.ToGlobal((p2 - p1).Normalized() * (fixedLength - currentLength) + p1);
            }

            currentLength += p1.DistanceTo(p2);
        }

        GD.PrintErr(
            $"Couldn't generate random point on Line2D. TotalLength: {totalLength}, FixedLength: {fixedLength}"
        );
        throw new Exception(
            $"Couldn't generate random point on Line2D. TotalLength: {totalLength}, FixedLength: {fixedLength}"
        );
    }

    public static Vector2 RandomPoint(this Line2D line, Random random)
    {
        var totalLength = line.TotalLength();
        var randomLength = (float)random.NextDouble() * totalLength;

        var currentLength = 0f;
        for (int i = 0; i < line.Points.Length - 1; i++)
        {
            var p1 = line.Points[i];
            var p2 = line.Points[i + 1];

            if (randomLength <= currentLength + p1.DistanceTo(p2))
            {
                return line.ToGlobal((p2 - p1).Normalized() * (randomLength - currentLength) + p1);
            }

            currentLength += p1.DistanceTo(p2);
        }

        GD.PrintErr(
            $"Couldn't generate random point on Line2D. TotalLength: {totalLength}, RandomLength: {randomLength}"
        );
        throw new Exception(
            $"Couldn't generate random point on Line2D. TotalLength: {totalLength}, RandomLength: {randomLength}"
        );
    }

    public static Vector2 RandomVector2InDistance(
        this Vector2 origin,
        float maxDistance,
        float logarithmic,
        Random random
    )
    {
        // Pick a random angle (0 to 2π)
        double angle = random.NextDouble() * Math.PI * 2.0;
        // Pick a random distance (0 to maxDistance)
        float distance = 0f;
        if (logarithmic != 0)
        {
            distance = LogRandomDistance(maxDistance, logarithmic, random);
            // GD.Print($"Logarithmic distance: {distance} for maxDistance: {maxDistance}");
        }
        else
        {
            distance = LinearRandomDistance(maxDistance, random);
            // GD.Print($"Linear distance: {distance} for maxDistance: {maxDistance}");
        }
        // Calculate the offset
        var offset = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * distance;
        return origin + offset;
    }

    public static float LogRandomDistance(float maxDistance, float logarithmic, Random random)
    {
        double u = random.NextDouble();
        return (float)(maxDistance * Math.Pow(u, logarithmic));
    }

    public static float LinearRandomDistance(float maxDistance, Random random)
    {
        return (float)(random.NextDouble() * maxDistance);
    }
}
