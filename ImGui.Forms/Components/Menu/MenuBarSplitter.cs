namespace ImGui.Forms.Components.Menu
{
    public class MenuBarSplitter : MenuBarItem
    {
        public override int Height => 4;

        protected override void UpdateInternal()
        {
            ImGuiNET.ImGui.Separator();
        }
    }
}
