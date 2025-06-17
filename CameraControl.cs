using Godot;

namespace Starlitter;

public partial class CameraControl : Camera2D
{
    [Export]
    public float MoveSpeed = 400f;

    [Export]
    public float ZoomSpeed = 0.1f;

    [Export]
    public float MinZoom = 0.2f;

    [Export]
    public float MaxZoom = 4.0f;

    public override void _Process(double delta)
    {
        Vector2 inputVector = Vector2.Zero;
        if (Input.IsActionPressed("ui_right"))
            inputVector.X += 1;
        if (Input.IsActionPressed("ui_left"))
            inputVector.X -= 1;
        if (Input.IsActionPressed("ui_down"))
            inputVector.Y += 1;
        if (Input.IsActionPressed("ui_up"))
            inputVector.Y -= 1;

        if (inputVector != Vector2.Zero)
            Position += inputVector.Normalized() * MoveSpeed * (float)delta;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseEvent && mouseEvent.Pressed)
        {
            if (mouseEvent.ButtonIndex == MouseButton.WheelDown)
            {
                float newZoom = Mathf.Clamp(Zoom.X - ZoomSpeed, MinZoom, MaxZoom);
                Zoom = new Vector2(newZoom, newZoom);
            }
            else if (mouseEvent.ButtonIndex == MouseButton.WheelUp)
            {
                float newZoom = Mathf.Clamp(Zoom.X + ZoomSpeed, MinZoom, MaxZoom);
                Zoom = new Vector2(newZoom, newZoom);
            }
        }
    }
}
