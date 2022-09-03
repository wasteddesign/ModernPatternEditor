using System;
using System.Collections.Generic;

namespace WDE.ModernPatternEditor
{
    public interface IGUICallbacks
    {
        void WriteDC(string text);
        int GetPlayPosition();
        bool GetPlayNotesState();
        void MidiNote(int note, int velocity);
        int GetBaseOctave();
        bool IsMidiNoteImplemented();
        void GetGUINotes();
        void GetRecordedNotes();
        int GetStateFlags();
        bool TargetSet();
        void SetStatusBarText(int pane, String text);
        void PlayNoteEvents(IEnumerable<NoteEvent> notes);
        bool IsEditorWindowVisible();
        string GetThemePath();
        string GetTargetMachine();
        string GetEditorMachine();
        void SetPatternEditorMachine(string editorMachine);
        void SetPatternName(string machine, string oldName, string newName);

        void ControlChange(string machine, int group, int track, int param, int value);

        IntPtr GetEditorHWND();

    }
}
