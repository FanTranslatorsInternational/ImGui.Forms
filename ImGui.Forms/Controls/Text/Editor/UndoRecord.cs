using System;

namespace ImGui.Forms.Controls.Text.Editor;

internal struct UndoRecord
{
    public string MAdded = string.Empty;
    public Coordinate MAddedStart;
    public Coordinate MAddedEnd;

    public string MRemoved = string.Empty;
    public Coordinate MRemovedStart;
    public Coordinate MRemovedEnd;

    public EditorState MBefore;
    public EditorState MAfter;

    public UndoRecord() { }

    public UndoRecord(string aAdded, Coordinate aAddedStart, Coordinate aAddedEnd, string aRemoved,
        Coordinate aRemovedStart, Coordinate aRemovedEnd, EditorState aBefore, EditorState aAfter)
    {
        MAdded = aAdded;
        MAddedStart = aAddedStart;
        MAddedEnd = aAddedEnd;
        MRemoved = aRemoved;
        MRemovedStart = aRemovedStart;
        MRemovedEnd = aRemovedEnd;
        MBefore = aBefore;
        MAfter = aAfter;

        if (MAddedStart <= MAddedEnd) throw new InvalidOperationException("Added range invalid.");
        if (MRemovedStart <= MRemovedEnd) throw new InvalidOperationException("Remove range invalid.");
    }

    public void Undo(TextEditor aEditor)
    {
        if (MAdded.Length > 0)
        {
            aEditor.DeleteRange(MAddedStart, MAddedEnd);
            aEditor.Colorize(MAddedStart.Line - 1, MAddedEnd.Line - MAddedStart.Line + 2);
        }

        if (MRemoved.Length > 0)
        {
            var start = MRemovedStart;
            aEditor.InsertTextAt(ref start, MRemoved);
            aEditor.Colorize(MRemovedStart.Line - 1, MRemovedEnd.Line - MRemovedStart.Line + 2);
        }

        aEditor.State = MBefore;
        aEditor.EnsureCursorVisible();
    }

    public void Redo(TextEditor aEditor)
    {
        if (MRemoved.Length > 0)
        {
            aEditor.DeleteRange(MRemovedStart, MRemovedEnd);
            aEditor.Colorize(MRemovedStart.Line - 1, MRemovedEnd.Line - MRemovedStart.Line + 1);
        }

        if (MAdded.Length > 0)
        {
            var start = MAddedStart;
            aEditor.InsertTextAt(ref start, MAdded);
            aEditor.Colorize(MAddedStart.Line - 1, MAddedEnd.Line - MAddedStart.Line + 1);
        }

        aEditor.State = MAfter;
        aEditor.EnsureCursorVisible();
    }
}