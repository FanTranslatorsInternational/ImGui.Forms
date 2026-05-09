namespace ImGui.Forms.Controls.Text.Editor;

internal struct EditorState
{
    public Coordinate SelectionStart { get; set; }
    public Coordinate SelectionEnd { get; set; }
    public Coordinate CursorPosition { get; set; }
}