using BuzzGUI.Common;
using BuzzGUI.Interfaces;
using BuzzGUI.Common.InterfaceExtensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sanford.Multimedia.Midi;
using System.Runtime.InteropServices;

namespace WDE.ModernPatternEditor
{
    public static class PatternEditorUtils
    {
        static readonly byte PATTERNXP_DATA_VERSION = 3;
        static readonly byte NOT_PATTERNXP_DATA = 255;
        static readonly byte MODERN_PATTERN_EDITOR_DATA_VERSION = 1;
        static private int ColumnIndex;

        enum InternalParameter
        {
            SPGlobalTrigger = -10,
            SPGlobalEffect1 = -11,
            SPGlobalEffect1Data = -12,

            FirstInternalTrackParameter = -101,
            SPTrackTrigger = -110,
            SPTrackEffect1 = -111,
            SPTrackEffect1Data = -112,

            FirstMidiTrackParameter = -128,
            MidiNote = -128,
            MidiVelocity = -129,
            MidiNoteDelay = -130,
            MidiNoteCut = -131,
            MidiPitchWheel = -132,
            MidiCC = -133

        };

        public static int BCReadInt(byte[] data, ref int index)
        {
            int ret = 0;
            if (index < data.Length)
            {
                ret = BitConverter.ToInt32(data, index);
                index += sizeof(int);
            }

            return ret;
        }

        public static List<MPEPattern> ProcessEditorData(PatternEditor editor, byte [] data)
        {   
            List<MPEPattern> patterns = new List<MPEPattern>();
            if (data.Length == 0)
                return patterns;

            int index = 0;
            // Decode pattern editor data
            byte[] machinePatternsData = data;

            byte version = machinePatternsData[index++];

            if (index >= data.Length)
                return patterns;

            if (version >= 1 && version <= PATTERNXP_DATA_VERSION)
            {
                return ParsePatternXPData(editor, machinePatternsData, index, version);
            }
            else if (version == NOT_PATTERNXP_DATA)
            { 
                byte mpeVersion = machinePatternsData[index++];
                index += sizeof(int); // Skip data size
                if (mpeVersion == MODERN_PATTERN_EDITOR_DATA_VERSION)
                    return ParseModernPatternEditorData(editor, machinePatternsData, index, version);
            }
            return patterns;
        }

        #region PatternXPData Read
        private static List<MPEPattern> ParsePatternXPData(PatternEditor editor, byte[] machinePatternsData, int index, byte version)
        {
            List<MPEPattern> patterns = new List<MPEPattern> ();
            int numpat = BCReadInt(machinePatternsData, ref index);

            for (int i = 0; i < numpat; i++)
            {
                string name = GetStringFromByteArray(machinePatternsData, ref index);

                //IPattern pat = mac.Patterns.FirstOrDefault(p => p.Name == name);
                MPEPattern mpePattern = new MPEPattern(editor, name);
                mpePattern.PatternName = name;
                LoadPattern(mpePattern, machinePatternsData, ref index, version);

                patterns.Add(mpePattern);
            }

            return patterns;
        }

        private static byte[] GetArrayCorrectEndian(byte[] machinePatternsData, int len, int index, bool littleEndian = true)
        {
            byte [] res = new byte[len];

            for (int i = 0; i < len; i++)
            {
                res[i] = littleEndian == BitConverter.IsLittleEndian ? machinePatternsData[index + i] : machinePatternsData[index + len - 1 - i];
            }

            return res;
        }

        private static void LoadPattern(MPEPattern pat, byte[] machinePatternsData, ref int index, byte ver)
        {
            int rowsPerBeat = 4;
            if (ver > 1)
                rowsPerBeat = BCReadInt(machinePatternsData, ref index);
           
            int count = BCReadInt(machinePatternsData, ref index);

            pat.RowsPerBeat = rowsPerBeat;

            ColumnIndex = 0;

            for (int i = 0; i < count; i++)
            {
                //auto pc = make_shared<CColumn>();
                //pc->Read(pi, ver);
                //columns.push_back(pc);
                LoadColumn(pat, machinePatternsData, ref index, ver);
            }
        }

        public static void LoadColumn(MPEPattern pat, byte[] machinePatternsData, ref int index, byte ver)
        {
            MPEPatternColumn mpeColumn = new MPEPatternColumn(pat);

            string machineName = GetStringFromByteArray(machinePatternsData, ref index);
            int paramIndex = BCReadInt(machinePatternsData, ref index);
            int paramTrack = BCReadInt(machinePatternsData, ref index); // Can be negative if internal param
            bool graphical = false;

            if (ver >= 3)
            {
                graphical = BitConverter.ToBoolean(machinePatternsData, index);
                index += sizeof(bool);
            }

            mpeColumn.MachineName = machineName;
            mpeColumn.Machine = Global.Buzz.Song.Machines.FirstOrDefault(x => x.Name == machineName);
            mpeColumn.ParamSaveDataIndex = paramIndex;
            mpeColumn.Index = paramIndex;
            mpeColumn.ParamTrack = paramTrack;
            mpeColumn.Graphical = graphical;

            if (mpeColumn.Machine.ParameterGroups[0].Parameters.Count > mpeColumn.Index)
                mpeColumn.GroupType = ParameterGroupType.Input;
            else if (mpeColumn.Machine.ParameterGroups[1].Parameters.Count > mpeColumn.Index)
                mpeColumn.GroupType = ParameterGroupType.Global;
            else
                mpeColumn.GroupType = ParameterGroupType.Track;

            int count = BCReadInt(machinePatternsData, ref index);

            List<PatternEvent> events = new List<PatternEvent>();
            for (int i = 0; i < count; i++)
            {
                int first = BCReadInt(machinePatternsData, ref index);
                int second = BCReadInt(machinePatternsData, ref index);

                PatternEvent pe = new PatternEvent();
                pe.Time = first * PatternEvent.TimeBase;
                pe.Value = second;
                //pe.Duration = -1;

                events.Add(pe);
            }

            mpeColumn.SetEvents(events.ToArray(), true);

            // Ingnore Buzz internal param types for now
            if (paramIndex >= 0)
                pat.MPEPatternColumns.Add(mpeColumn);
        }

        #endregion

        #region Modern Pattern Editor Data Read
        private static List<MPEPattern> ParseModernPatternEditorData(PatternEditor editor, byte[] machinePatternsData, int index, byte version)
        {
            List<MPEPattern> patterns = new List<MPEPattern>();
            int numpat = BCReadInt(machinePatternsData, ref index);

            for (int i = 0; i < numpat; i++)
            {
                string name = GetStringFromByteArray(machinePatternsData, ref index);

                //IPattern pat = mac.Patterns.FirstOrDefault(p => p.Name == name);
                MPEPattern mpePattern = new MPEPattern(editor, name);
                mpePattern.PatternName = name;
                LoadModernPatternEditorPattern(mpePattern, machinePatternsData, ref index, version);

                patterns.Add(mpePattern);
            }

            return patterns;
        }


        private static void LoadModernPatternEditorPattern(MPEPattern pat, byte[] machinePatternsData, ref int index, byte ver)
        {
            int numberOfBeats = BCReadInt(machinePatternsData, ref index);
            int rowsPerBeat = BCReadInt(machinePatternsData, ref index);

            int count = BCReadInt(machinePatternsData, ref index);
            
            pat.RowsPerBeat = rowsPerBeat;

            ColumnIndex = 0;

            for (int i = 0; i < count; i++)
            {
                LoadModernPatternEditorColumn(pat, machinePatternsData, ref index, ver, numberOfBeats);
            }

        }

        public static void LoadModernPatternEditorColumn(MPEPattern pat, byte[] machinePatternsData, ref int index, byte ver, int numberOfBeats)
        {
            MPEPatternColumn mpeColumn = new MPEPatternColumn(pat);

            string machineName = GetStringFromByteArray(machinePatternsData, ref index);
            int paramIndex = BCReadInt(machinePatternsData, ref index);
            int paramTrack = BCReadInt(machinePatternsData, ref index);
            bool graphical = false;

            if (ver >= 3)
            {
                graphical = BitConverter.ToBoolean(machinePatternsData, index);
                index += sizeof(bool);
            }

            mpeColumn.MachineName = machineName;
            mpeColumn.Machine = Global.Buzz.Song.Machines.FirstOrDefault(x => x.Name == machineName);
            mpeColumn.ParamSaveDataIndex = paramIndex;
            mpeColumn.ParamTrack = paramTrack;
            mpeColumn.Graphical = graphical;


            int count = BCReadInt(machinePatternsData, ref index);

            List<PatternEvent> events = new List<PatternEvent>();
            for (int i = 0; i < count; i++)
            {
                int first = BCReadInt(machinePatternsData, ref index);
                int second = BCReadInt(machinePatternsData, ref index);

                PatternEvent pe = new PatternEvent();
                pe.Time = first;
                pe.Value = second;
                //pe.Duration = -1;

                events.Add(pe);
            }

            mpeColumn.SetEvents(events.ToArray(), true);

            List<int> beatRows = new List<int>();
            // Read rows in beat
            for (int i = 0; i < numberOfBeats; i++)
            {
                beatRows.Add(BCReadInt(machinePatternsData, ref index));
            }

            mpeColumn.SetBeats(beatRows);

            pat.MPEPatternColumns.Add(mpeColumn);
        }

        #endregion
        private static string GetStringFromByteArray(byte[] machinePatternsData, ref int index)
        {
            string result = "";

            while (true)
            {
                if (machinePatternsData[index] == 0)
                    break;
                
                result += (char)(machinePatternsData[index]);
                index++;
            }

            index++;

            return result;
        }

        #region PatternXPData Write
        public static byte[] CreatePatternXPPatternData(IEnumerable<MPEPattern> patterns)
        {   
            // Encode pattern editor data
            List<byte> data = new List<byte>();

            data.Add(PATTERNXP_DATA_VERSION);
            
            int numpat = patterns.Count();
            data.AddRange(BitConverter.GetBytes(numpat));

            for (int i = 0; i < numpat; i++)
            {
                MPEPattern mpePattern = patterns.ElementAt(i);
                data.AddRange(Encoding.ASCII.GetBytes(mpePattern.PatternName));
                data.Add(0);

                CreatePatternData(mpePattern, data, PATTERNXP_DATA_VERSION);
            }
           
            byte[] dataret = data.ToArray();
            return dataret;
        }

        private static void CreatePatternData(MPEPattern pat, List<byte> data, byte ver)
        {   
            if (ver > 1)
                data.AddRange(BitConverter.GetBytes(pat.RowsPerBeat));

            int count = pat.MPEPatternColumns.Count();
            data.AddRange(BitConverter.GetBytes(count));
            
            ColumnIndex = 0;

            for (int i = 0; i < count; i++)
            {
                SaveColumn(pat.MPEPatternColumns[i], data, ver);
            }

        }

        public static void SaveColumn(MPEPatternColumn mpeColumn, List<byte> data, byte ver)
        {
            data.AddRange(Encoding.ASCII.GetBytes(mpeColumn.Machine.Name));
            data.Add(0);
            data.AddRange(BitConverter.GetBytes(mpeColumn.ParamSaveDataIndex));
            data.AddRange(BitConverter.GetBytes(mpeColumn.ParamTrack));
            data.Add(Convert.ToByte(mpeColumn.Graphical));
           
            IEnumerable<PatternEvent> events = mpeColumn.GetEvents(0, int.MaxValue);
            data.AddRange(BitConverter.GetBytes(events.Count()));

            foreach (var e in events)
            {
                data.AddRange(BitConverter.GetBytes((int)(e.Time / PatternEvent.TimeBase)));
                data.AddRange(BitConverter.GetBytes(e.Value));
            }
        }

        #endregion
        #region Modern Pattern Editor Data Write

        public static byte[] CreateModernPatternEditorData(IEnumerable<MPEPattern> patterns)
        {
            // Encode pattern editor data
            List<byte> data = new List<byte>();

            data.Add(NOT_PATTERNXP_DATA);
            data.Add(MODERN_PATTERN_EDITOR_DATA_VERSION);

            int numpat = patterns.Count();
            data.AddRange(BitConverter.GetBytes(numpat));

            for (int i = 0; i < numpat; i++)
            {
                MPEPattern mpePattern = patterns.ElementAt(i);
                data.AddRange(Encoding.ASCII.GetBytes(mpePattern.PatternName));
                data.Add(0);

                CreateMPEPatternData(mpePattern, data, PATTERNXP_DATA_VERSION);
            }

            int dataSize = data.Count() + sizeof(int);
            data.InsertRange(2, BitConverter.GetBytes(dataSize));

            byte[] dataret = data.ToArray();
            return dataret;
        }
        private static void CreateMPEPatternData(MPEPattern pat, List<byte> data, byte ver)
        {
            // MPE Beats
            data.AddRange(BitConverter.GetBytes((int)(pat.Pattern.Length + Global.Buzz.TPB - 1) / Global.Buzz.TPB));
            data.AddRange(BitConverter.GetBytes(pat.RowsPerBeat));

            int count = pat.MPEPatternColumns.Count();
            data.AddRange(BitConverter.GetBytes(count));

            ColumnIndex = 0;

            for (int i = 0; i < count; i++)
            {
                SaveMPEColumn(pat.MPEPatternColumns[i], data, ver);
            }

        }

        public static void SaveMPEColumn(MPEPatternColumn mpeColumn, List<byte> data, byte ver)
        {
            data.AddRange(Encoding.ASCII.GetBytes(mpeColumn.Machine.Name));
            data.Add(0);
            data.AddRange(BitConverter.GetBytes(mpeColumn.ParamSaveDataIndex));
            data.AddRange(BitConverter.GetBytes(mpeColumn.ParamTrack));
            data.Add(Convert.ToByte(mpeColumn.Graphical));

            IEnumerable<PatternEvent> events = mpeColumn.GetEvents(0, int.MaxValue);
            data.AddRange(BitConverter.GetBytes(events.Count()));

            foreach (var e in events)
            {
                data.AddRange(BitConverter.GetBytes((int)(e.Time)));
                data.AddRange(BitConverter.GetBytes(e.Value));
            }

            // MPE Save beats

            foreach(var beatRow in mpeColumn.BeatRowsList)
            {
                data.AddRange(BitConverter.GetBytes(beatRow));
            }

            /*
            PatternVM patternvm = mpeColumn.MPEPattern.Editor.SelectedMachine.Patterns.FirstOrDefault(x => x.Pattern == mpeColumn.MPEPattern.Pattern);

            if (patternvm != null)
            {
                int beatCount = patternvm.BeatCount;
                foreach(var columnSet in patternvm.ColumnSets)
                {
                    var editorColumns = (IList<ParameterColumn>)columnSet.Columns;
                    var thisParameter = mpeColumn.Machine.AllParameters().ElementAt(mpeColumn.Index + mpeColumn.Machine.ParameterGroups[0].Parameters.Count);
                    var parcolumn = editorColumns.FirstOrDefault(x => x.PatternColumn.Parameter == thisParameter);

                    if (parcolumn != null)
                    {
                        for (int i = 0; i < beatCount; i++)
                        {
                            int rows = parcolumn.FetchBeat(i).Rows.Count;
                            data.AddRange(BitConverter.GetBytes(rows));
                        }
                    }
                }
            }
            */
        }
        #endregion


        public static byte[] ExportMidiEvents(MPEPattern pattern)
        {
            const int MidiTimeBase = 960;
            
            List<int> midiEvents = new List<int>();

            foreach(var mpeColumn in pattern.MPEPatternColumns)
            {
                if (mpeColumn.Parameter.Type == ParameterType.Note)
                {
                    int previousNote = 0;
                    foreach (var e in mpeColumn.GetEvents(0, int.MaxValue))
                    {
                        midiEvents.Add((e.Time / PatternEvent.TimeBase) * MidiTimeBase);
                        if (e.Value != BuzzNote.Off)
                        {
                            previousNote = BuzzNote.ToMIDINote(e.Value);
                            var msg = new ChannelMessage(ChannelCommand.NoteOn, 0, previousNote);
                            midiEvents.Add(msg.Message);
                        }
                        else
                        {   
                            var msg = new ChannelMessage(ChannelCommand.NoteOff, 0, previousNote);
                            midiEvents.Add(msg.Message);
                            previousNote = 0;
                        }
                    }
                }
            }

            midiEvents.Add(-1);
            int [] intArray = midiEvents.ToArray();
            byte[] result = new byte[intArray.Length * sizeof(int)];
            Buffer.BlockCopy(intArray, 0, result, 0, result.Length);

            return result;
        }

        #region Registry

        static readonly string regpath = Global.RegistryRoot;
        internal static readonly string regPathBuzzSettings = "Settings";
        internal static readonly string regPathBuzzMachineDefaultPE = "DefaultPE";
        internal static readonly string regDefaultPE = "DefaultPE";
        public static void WriteRegistry<T>(string key, T x, string path)
        {
            try
            {
                var regkey = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(regpath + "\\" + path);
                if (regkey == null) return;
                regkey.SetValue(key, x.ToString());
            }
            catch (Exception) { }

        }

        #endregion

        #region HWND UTILS
        [DllImport("user32.dll", ExactSpelling = true, CharSet = CharSet.Auto)]
        public static extern IntPtr GetParent(IntPtr hWnd);

        [DllImport("user32.dll")]
        static extern IntPtr GetDlgItem(IntPtr hDlg, int nIDDlgItem);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int SendMessage(IntPtr hWnd, Int32 wMsg, bool wParam, Int32 lParam);
        [DllImport("user32.dll")]
        static extern bool InvalidateRect(IntPtr hWnd, IntPtr lpRect, bool bErase);

        private static int EDITOR_MAIN_TOOLBAR = 105;
        private static int EDITOR_MAIN_STATUSBAR = 0xE801;
        private static int SW_HIDE = 0;
        private static int SW_SHOW = 5;
        private static int WM_SIZE = 5;

        public static void BuzzToolbars(IntPtr editorHwnd, bool showBuzzToolbars)
        {
            //editorHwnd = GetParent(editorHwnd);
            if (editorHwnd != IntPtr.Zero)
            {
                IntPtr dlgwnd = GetDlgItem(editorHwnd, EDITOR_MAIN_TOOLBAR);
                IntPtr toolwnd = GetDlgItem(editorHwnd, EDITOR_MAIN_STATUSBAR);

                if (!showBuzzToolbars)
                {
                    ShowWindow(dlgwnd, SW_HIDE);
                    ShowWindow(toolwnd, SW_HIDE);
                }
                else
                {
                    ShowWindow(dlgwnd, SW_SHOW);
                    ShowWindow(toolwnd, SW_SHOW);
                }
                //if (Global.Buzz.ActiveView == BuzzView.PatternView)
                {
                    SendMessage(GetParent(dlgwnd), WM_SIZE, false, 0);
                    InvalidateRect(GetParent(dlgwnd), IntPtr.Zero, false);
                }
            }
        }
        #endregion
    }

}
