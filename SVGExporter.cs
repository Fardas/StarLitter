using System.Collections.Generic;
using System.IO;
using System.Linq;
using Godot;
using Microsoft.Win32.SafeHandles;

namespace Starlitter;

public partial class SVGExporter : Button
{
    [Export]
    public TextEdit FileNameField { get; set; }

    [Export]
    public StarGenerator StarGenerator { get; set; }

    public void OnButtonReleased()
    {
        ExportPositionsToSVG($"./{FileNameField.Text}");
        GD.Print("Saved starmap to: ", FileNameField.Text);
    }

    public void ExportPositionsToSVG(string filePath)
    {
        // Start SVG content
        var svg =
            $"<svg viewBox=\"0 0 {StarGenerator.BoundWidth + 100} {StarGenerator.BoundHeight + 100}\" xmlns=\"http://www.w3.org/2000/svg\" width=\"{StarGenerator.BoundWidth + 100}\" height=\"{StarGenerator.BoundHeight + 100}\">\n";
        foreach (var star in StarGenerator.Stars)
        {
            float radius = (int)star.StarSize / 200f / 2f;
            float cx = star.Position.X - StarGenerator.BoundMinX + 50 + radius;
            float cy = star.Position.Y - StarGenerator.BoundMinY + 50 + radius;
            svg +=
                $"  <circle cx=\"{(int)cx}\" cy=\"{(int)cy}\" r=\"{radius}\" fill=\"{ColorsByGroupIds[star.GroupId % ColorsByGroupIds.Count]}\" />\n";
            svg +=
                $"  <text font-size=\"8\" x=\"{(int)(cx - radius)}\" y=\"{(int)(cy - radius)}\" fill=\"{ColorsByGroupIds[star.GroupId % ColorsByGroupIds.Count]}\">{star.GroupId}</text>\n";
            svg +=
                $"  <text font-size=\"8\" x=\"{(int)(cx + radius)}\" y=\"{(int)(cy + radius * 4)}\" fill=\"{ColorsByGroupIds[star.GroupId % ColorsByGroupIds.Count]}\">{star.StarSize.ToString()[0]}</text>\n";
        }
        svg += "</svg>";

        // Write to file
        File.WriteAllText(filePath, svg);
    }

    private List<string> ColorsByGroupIds =
    [
        "black",
        "red",
        "green",
        "blue",
        "purple",
        "orange",
        "grey",
    ];
}
