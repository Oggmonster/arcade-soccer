using Godot;

public partial class FieldRenderer : Node2D
{
    public override void _Draw()
    {
        var fieldRect = PocketPitchConfig.FieldRect;
        var grass = new Color("2b8c52");
        var stripe = new Color("349d5e");
        var line = new Color("f4fff7");

        DrawRect(fieldRect, grass, true);

        const float stripeHeight = 120f;
        var stripeCount = Mathf.CeilToInt(fieldRect.Size.Y / stripeHeight);
        for (var i = 0; i < stripeCount; i++)
        {
            if (i % 2 == 0)
            {
                continue;
            }

            var y = fieldRect.Position.Y + (i * stripeHeight);
            DrawRect(new Rect2(fieldRect.Position.X, y, fieldRect.Size.X, stripeHeight), stripe, true);
        }

        DrawRect(fieldRect, line, false, 5f);
        DrawLine(new Vector2(fieldRect.Position.X, 0f), new Vector2(fieldRect.End.X, 0f), line, 4f);
        DrawArc(Vector2.Zero, 86f, 0f, Mathf.Tau, 48, line, 4f);
        DrawCircle(Vector2.Zero, 6f, line);

        var topBox = PocketPitchConfig.PenaltyAreaRect(TeamSide.Away);
        var bottomBox = PocketPitchConfig.PenaltyAreaRect(TeamSide.Home);
        DrawRect(topBox, line, false, 4f);
        DrawRect(bottomBox, line, false, 4f);

        DrawGoal(true, line);
        DrawGoal(false, line);
    }

    private void DrawGoal(bool topGoal, Color line)
    {
        var y = topGoal
            ? PocketPitchConfig.FieldRect.Position.Y - PocketPitchConfig.GoalDepth
            : PocketPitchConfig.FieldRect.End.Y;
        var rect = new Rect2(
            -PocketPitchConfig.GoalWidth * 0.5f,
            y,
            PocketPitchConfig.GoalWidth,
            PocketPitchConfig.GoalDepth);
        DrawRect(rect, new Color(1f, 1f, 1f, 0.12f), true);
        DrawRect(rect, line, false, 3f);
    }
}
