namespace ImGui.Forms.Controls.Menu;

public class MenuBarSplitter : MenuBarItem
{
    public override int Height => 4;

    protected override void UpdateInternal()
    {
        ImGuiNET.ImGui.Separator();
    }

    protected override void UpdateEventsInternal()
    {
    }
}