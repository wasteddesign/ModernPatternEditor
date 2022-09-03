using BuzzGUI.Common;
using BuzzGUI.Common.InterfaceExtensions;
using BuzzGUI.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using WDE.ModernPatternEditor.MPEStructures;

namespace WDE.ModernPatternEditor
{
    public static class PatternEditorUtils
    {
        static readonly byte PATTERNXP_DATA_VERSION = 3;
        static readonly byte NOT_PATTERNXP_DATA = 255;
        static readonly byte MODERN_PATTERN_EDITOR_DATA_VERSION = 1;

        public enum InternalParameter
        {
            SPGlobalTrigger = -10,
            SPGlobalEffect1 = -11,
            SPGlobalEffect1Data = -12,

            //FirstInternalTrackParameter = -101,
            SPTrackTrigger = -110,
            SPTrackEffect1 = -111,
            SPTrackEffect1Data = -112,

            //FirstMidiTrackParameter = -128,
            MidiNote = -128,
            MidiVelocity = -129,
            MidiNoteDelay = -130,
            MidiNoteCut = -131,
            MidiPitchWheel = -132,
            MidiCC = -133
        };

        public static readonly int LastMidiTrackParameter = -133;


        public static List<MPEPattern> ProcessEditorData(PatternEditor editor, byte[] data)
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
            List<MPEPattern> patterns = new List<MPEPattern>();
            int numpat = BCReadInt(machinePatternsData, ref index);

            for (int i = 0; i < numpat; i++)
            {
                string name = GetStringFromByteArray(machinePatternsData, ref index);
                MPEPattern mpePattern = new MPEPattern(editor, name);
                mpePattern.PatternName = name;
                LoadPattern(mpePattern, machinePatternsData, ref index, version);

                patterns.Add(mpePattern);
            }

            return patterns;
        }

        private static void LoadPattern(MPEPattern pat, byte[] machinePatternsData, ref int index, byte ver)
        {
            int rowsPerBeat = 4;
            if (ver > 1)
                rowsPerBeat = BCReadInt(machinePatternsData, ref index);

            int count = BCReadInt(machinePatternsData, ref index);

            pat.RowsPerBeat = rowsPerBeat;

            for (int i = 0; i < count; i++)
            {
                LoadColumn(pat, machinePatternsData, ref index, ver);
            }
        }

        public static void LoadColumn(MPEPattern pat, byte[] machinePatternsData, ref int index, byte ver)
        {
            MPEPatternColumn mpeColumn = new MPEPatternColumn(pat);

            string machineName = GetStringFromByteArray(machinePatternsData, ref index);
            int paramIndex = BCReadInt(machinePatternsData, ref index); // Can be negative if internal param
            int paramTrack = BCReadInt(machinePatternsData, ref index);
            bool graphical = false;

            if (ver >= 3)
            {
                graphical = BitConverter.ToBoolean(machinePatternsData, index);
                index += sizeof(bool);
            }

            mpeColumn.Machine = Global.Buzz.Song.Machines.FirstOrDefault(x => x.Name == machineName);
            mpeColumn.ParamTrack = paramTrack;
            mpeColumn.Graphical = graphical;

            var parameter = GetParameter(mpeColumn.Machine, paramIndex, paramTrack);
            mpeColumn.Parameter = parameter;

            if (parameter != null)
            {
                mpeColumn.GroupType = parameter.Group.Type;
            }

            int count = BCReadInt(machinePatternsData, ref index);

            List<PatternEvent> events = new List<PatternEvent>();
            for (int i = 0; i < count; i++)
            {
                int first = BCReadInt(machinePatternsData, ref index);
                int second = BCReadInt(machinePatternsData, ref index);

                PatternEvent pe = new PatternEvent();
                pe.Time = first * PatternEvent.TimeBase;
                pe.Value = second;

                events.Add(pe);
            }

            mpeColumn.SetEvents(events.ToArray(), true);
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

            mpeColumn.Machine = Global.Buzz.Song.Machines.FirstOrDefault(x => x.Name == machineName);
            mpeColumn.ParamTrack = paramTrack;
            mpeColumn.Graphical = graphical;

            var parameter = GetParameter(mpeColumn.Machine, paramIndex, paramTrack);
            mpeColumn.GroupType = parameter.Group.Type;
            mpeColumn.Parameter = parameter;

            int count = BCReadInt(machinePatternsData, ref index);

            List<PatternEvent> events = new List<PatternEvent>();
            for (int i = 0; i < count; i++)
            {
                int first = BCReadInt(machinePatternsData, ref index);
                int second = BCReadInt(machinePatternsData, ref index);

                PatternEvent pe = new PatternEvent();
                pe.Time = first;
                pe.Value = second;
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

            for (int i = 0; i < count; i++)
            {
                SaveColumn(pat.MPEPatternColumns[i], data, ver, pat.RowsPerBeat);
            }

        }

        public static void SaveColumn(MPEPatternColumn mpeColumn, List<byte> data, byte ver, int rpb)
        {
            data.AddRange(Encoding.ASCII.GetBytes(mpeColumn.Machine.Name));
            data.Add(0);
            data.AddRange(BitConverter.GetBytes(GetParamIndex(mpeColumn.Parameter, mpeColumn.ParamTrack)));
            data.AddRange(BitConverter.GetBytes(mpeColumn.ParamTrack));
            data.Add(Convert.ToByte(mpeColumn.Graphical));

            IEnumerable<PatternEvent> events = mpeColumn.GetEventsQuantized(0, int.MaxValue, rpb);
            data.AddRange(BitConverter.GetBytes(events.Count()));

            foreach (var e in events)
            {
                data.AddRange(BitConverter.GetBytes((int)(e.Time / PatternEvent.TimeBase)));
                data.AddRange(BitConverter.GetBytes(e.Value));
            }
        }
        #endregion

        #region Modern Pattern Editor Data Write

        private static IParameter GetParameter(IMachine machine, int paramIndex, int paramTrack)
        {
            IParameter ret = null;


            if (paramIndex == (int)InternalParameter.MidiNote)
            {
                ret = MPEInternalParameter.GetInternalParameter(machine, InternalParameter.MidiNote);
            }
            else if (paramIndex == (int)InternalParameter.MidiVelocity)
            {
                ret = MPEInternalParameter.GetInternalParameter(machine, InternalParameter.MidiVelocity);
            }
            else if (paramIndex == (int)InternalParameter.MidiNoteDelay)
            {
                ret = MPEInternalParameter.GetInternalParameter(machine, InternalParameter.MidiNoteDelay);
            }
            else if (paramIndex == (int)InternalParameter.MidiNoteCut)
            {
                ret = MPEInternalParameter.GetInternalParameter(machine, InternalParameter.MidiNoteCut);
            }
            else if (paramIndex == (int)InternalParameter.MidiPitchWheel)
            {
                ret = MPEInternalParameter.GetInternalParameter(machine, InternalParameter.MidiPitchWheel);
            }
            else if (paramIndex == (int)InternalParameter.MidiCC)
            {
                ret = MPEInternalParameter.GetInternalParameter(machine, InternalParameter.MidiCC);
            }
            else if (paramIndex >= 0)
            {
                int gourp0ParamsCount = machine.ParameterGroups[0].Parameters.Count;
                int gourp1ParamsCount = machine.ParameterGroups[1].Parameters.Count;
                int gourp2ParamsCount = machine.ParameterGroups[2].Parameters.Count;

                var parametersAndTracks = machine.AllParametersAndTracks();

                if (paramIndex < gourp1ParamsCount && paramTrack == 0)
                {
                    ret = parametersAndTracks.ElementAt(paramIndex).Item1 as IParameter;
                }
                else
                {
                    int index = paramIndex + gourp2ParamsCount * paramTrack;
                    ret = parametersAndTracks.ElementAt(index).Item1 as IParameter;
                }
            }

            return ret;
        }

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
            data.AddRange(BitConverter.GetBytes((int)(pat.Pattern.Length + PatternControl.BUZZ_TICKS_PER_BEAT - 1) / PatternControl.BUZZ_TICKS_PER_BEAT));
            data.AddRange(BitConverter.GetBytes(pat.RowsPerBeat));

            int count = pat.MPEPatternColumns.Count();
            data.AddRange(BitConverter.GetBytes(count));

            for (int i = 0; i < count; i++)
            {
                SaveMPEColumn(pat.MPEPatternColumns[i], data, ver);
            }
        }

        public static void SaveMPEColumn(MPEPatternColumn mpeColumn, List<byte> data, byte ver)
        {
            data.AddRange(Encoding.ASCII.GetBytes(mpeColumn.Machine.Name));
            data.Add(0);
            data.AddRange(BitConverter.GetBytes(GetParamIndex(mpeColumn.Parameter, mpeColumn.ParamTrack)));
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

            foreach (var beatRow in mpeColumn.BeatRowsList)
            {
                data.AddRange(BitConverter.GetBytes(beatRow));
            }
        }
        #endregion

        #region Read Write Utils
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

        public static IEnumerable<IParameter> GetInternalParameters(IMachine mac)
        {
            List<IParameter> pars = new List<IParameter>();

            pars.Add(MPEInternalParameter.GetInternalParameter(mac, InternalParameter.MidiNote));
            pars.Add(MPEInternalParameter.GetInternalParameter(mac, InternalParameter.MidiVelocity));
            pars.Add(MPEInternalParameter.GetInternalParameter(mac, InternalParameter.MidiNoteDelay));
            pars.Add(MPEInternalParameter.GetInternalParameter(mac, InternalParameter.MidiNoteCut));
            pars.Add(MPEInternalParameter.GetInternalParameter(mac, InternalParameter.MidiPitchWheel));
            pars.Add(MPEInternalParameter.GetInternalParameter(mac, InternalParameter.MidiCC));

            return pars;
        }

        public static int GetParamIndex(IParameter par, int track)
        {
            int index = 0;
            IMachine machine = par.Group.Machine;
            int group = machine.ParameterGroups.IndexOf(par.Group);

            if (par is MPEInternalParameter)
            {
                index = par.IndexInGroup;
            }
            else if (group == 1)
            {
                index = par.IndexInGroup;
            }
            else if (group == 2)
            {
                int group1Size = machine.ParameterGroups[1].Parameters.Count;
                index = group1Size + par.IndexInGroup;
            }
            return index;
        }

        #endregion

        #region MIDI Import Export
        public static byte[] ExportMidiEvents(MPEPattern pattern)
        {
            const int MidiTimeBase = 960;

            List<Tuple<int, int, int>> midiEvents = new List<Tuple<int, int, int>>();

            // Todo: delay & cut

            foreach (var mpeColumn in pattern.MPEPatternColumns)
            {
                if (mpeColumn.Parameter.Type == ParameterType.Note)
                {
                    int previousNote = 0;

                    foreach (var e in mpeColumn.GetEvents(0, int.MaxValue).OrderBy(x => x.Time).ToList())
                    {
                        int midiTime = (e.Time / PatternEvent.TimeBase) * MidiTimeBase;
                        if (e.Value != BuzzNote.Off)
                        {
                            int newNote = BuzzNote.ToMIDINote(e.Value);
                            if (previousNote != 0)
                            {
                                // Add note off before new note
                                var msgOff = MIDI.EncodeNoteOff(previousNote);
                                midiEvents.Add(new Tuple<int, int, int>(midiTime, msgOff, previousNote));
                            }
                            var msg = MIDI.EncodeNoteOn(newNote, 70);
                            midiEvents.Add(new Tuple<int, int, int>(midiTime, msg, newNote));
                            previousNote = newNote;
                        }
                        else
                        {
                            var msg = MIDI.EncodeNoteOff(previousNote);
                            midiEvents.Add(new Tuple<int, int, int>(midiTime, msg, previousNote));
                            previousNote = 0;
                        }
                    }
                }
            }

            midiEvents = midiEvents.OrderBy(x => x.Item1).ToList();
            List<int> ordereEvents = new List<int>();
            foreach (var t in midiEvents)
            {
                ordereEvents.Add(t.Item1);
                ordereEvents.Add(t.Item2);
            }
            ordereEvents.Add(-1);
            int[] intArray = ordereEvents.ToArray();
            byte[] result = new byte[intArray.Length * sizeof(int)];
            Buffer.BlockCopy(intArray, 0, result, 0, result.Length);

            return result;
        }

        // Is anybody really using this?
        public static bool ImportMidiEvents(MPEPattern pattern, byte [] data)
        {
            return false;
            /*
            const int MidiTimeBase = 960;

            List<Tuple<int, int, int>> midiEvents = new List<Tuple<int, int, int>>();

            // Todo: delay & cut

            foreach (var mpeColumn in pattern.MPEPatternColumns)
            {
                if (mpeColumn.Parameter.Type == ParameterType.Note)
                {
                   
                }
            }
            */
        }

        #endregion

        #region Other

        internal static ResourceDictionary GetBuzzThemeSettingsWindowResources()
        {
            ResourceDictionary skin = new ResourceDictionary();

            try
            {
                string selectedTheme = Global.Buzz.SelectedTheme == "<default>" ? "Default" : Global.Buzz.SelectedTheme;
                string skinPath = Global.BuzzPath + "\\Themes\\" + selectedTheme + "\\ModernPatternEditor\\PatternPropertiesWindow.xaml";
                //string skinPath = "..\\..\\..\\Themes\\" + selectedTheme + "\\ModernPatternEditor\\ModernPatternEditor.xaml";

                //skin.Source = new Uri(skinPath, UriKind.Absolute);
                skin = (ResourceDictionary)XamlReaderEx.LoadHack(skinPath);
            }
            catch (Exception)
            {
                string skinPath = Global.BuzzPath + "\\Themes\\Default\\ModernPatternEditor\\PatternPropertiesWindow.xaml";
                skin.Source = new Uri(skinPath, UriKind.Absolute);
            }

            return skin;
        }

        #endregion

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

                SendMessage(GetParent(dlgwnd), WM_SIZE, false, 0);
                InvalidateRect(GetParent(dlgwnd), IntPtr.Zero, false);
            }
        }
        #endregion
    }

}
