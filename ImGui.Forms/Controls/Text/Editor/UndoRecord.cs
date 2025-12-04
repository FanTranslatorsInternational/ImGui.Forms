using System;

namespace ImGui.Forms.Controls.Text.Editor;

struct UndoRecord
{
    public string mAdded = string.Empty;
    public Coordinate mAddedStart;
    public Coordinate mAddedEnd;

    public string mRemoved = string.Empty;
    public Coordinate mRemovedStart;
    public Coordinate mRemovedEnd;

    public EditorState mBefore;
    public EditorState mAfter;

    public UndoRecord() { }

    public UndoRecord(string aAdded, Coordinate aAddedStart, Coordinate aAddedEnd, string aRemoved,
        Coordinate aRemovedStart, Coordinate aRemovedEnd, EditorState aBefore, EditorState aAfter)
    {
        mAdded = aAdded;
        mAddedStart = aAddedStart;
        mAddedEnd = aAddedEnd;
        mRemoved = aRemoved;
        mRemovedStart = aRemovedStart;
        mRemovedEnd = aRemovedEnd;
        mBefore = aBefore;
        mAfter = aAfter;

        if (mAddedStart <= mAddedEnd) throw new InvalidOperationException("Added range invalid.");
        if (mRemovedStart <= mRemovedEnd) throw new InvalidOperationException("Remove range invalid.");
    }

    public void Undo(TextEditor aEditor)
    {
        if (mAdded.Length > 0)
        {
            aEditor.DeleteRange(mAddedStart, mAddedEnd);
            aEditor.Colorize(mAddedStart.Line - 1, mAddedEnd.Line - mAddedStart.Line + 2);
        }

        if (mRemoved.Length > 0)
        {
            var start = mRemovedStart;
            aEditor.InsertTextAt(ref start, mRemoved);
            aEditor.Colorize(mRemovedStart.Line - 1, mRemovedEnd.Line - mRemovedStart.Line + 2);
        }

        aEditor.mState = mBefore;
        aEditor.EnsureCursorVisible();
    }

    public void Redo(TextEditor aEditor)
    {
        if (mRemoved.Length > 0)
        {
            aEditor.DeleteRange(mRemovedStart, mRemovedEnd);
            aEditor.Colorize(mRemovedStart.Line - 1, mRemovedEnd.Line - mRemovedStart.Line + 1);
        }

        if (mAdded.Length > 0)
        {
            var start = mAddedStart;
            aEditor.InsertTextAt(ref start, mAdded);
            aEditor.Colorize(mAddedStart.Line - 1, mAddedEnd.Line - mAddedStart.Line + 1);
        }

        aEditor.mState = mAfter;
        aEditor.EnsureCursorVisible();
    }
}