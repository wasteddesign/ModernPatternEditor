using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BuzzGUI.Common;
using BuzzGUI.Interfaces;
using BuzzGUI.Common.InterfaceExtensions;
using System.ComponentModel;

namespace WDE.ModernPatternEditor
{
    public class MPEPattern
    {
        private int rpb;
        public int RowsPerBeat {
            get { return rpb; }
            internal set
            {
                rpb = value;
                foreach(var c in MPEPatternColumns)
                {
                    c.SetRPB(rpb);
                }
            }
        }

        public Dictionary<int, MPEPatternColumn> MPEPatternColumnsDict = new Dictionary<int, MPEPatternColumn>();

        public List<MPEPatternColumn> MPEPatternColumns = new List<MPEPatternColumn> { };

        public PatternEditor Editor { get; }
        public string PatternName { get; internal set; }

        private IPattern pattern;
        public IPattern Pattern
        {
            get { return pattern; }
            internal set
            {
                if (pattern != null)
                {
                    //pattern.ColumnAdded -= Pattern_ColumnAdded;
                    //pattern.ColumnRemoved -= Pattern_ColumnRemoved;
                    pattern.Machine.PropertyChanged -= Machine_PropertyChanged;
                    pattern.PropertyChanged -= Pattern_PropertyChanged;
                }

                pattern = value;

                if (pattern != null)
                {
                    //pattern.ColumnAdded += Pattern_ColumnAdded;
                    //pattern.ColumnRemoved += Pattern_ColumnRemoved;
                    pattern.Machine.PropertyChanged += Machine_PropertyChanged;
                    pattern.PropertyChanged += Pattern_PropertyChanged;
                    UpdateDataVer2();
                }
            }
        }

        private void Pattern_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {   
            switch (e.PropertyName)
            {
                case "Name":
                    IPattern pattern = (IPattern)sender;
                    // Update mainDB
                    Editor.MPEPatternsDB.PatternRenamed(pattern);
                    break;
                case "Lenght":
                    foreach (var column in MPEPatternColumns)
                        column.UpdateLength();
                    break;
            }
        }

        public MPEPatternColumn GetColumn(IPatternColumn column)
        {
            int key = GetParamIndex(column.Parameter, column.Track);
            if(MPEPatternColumnsDict.ContainsKey(key))
                return MPEPatternColumnsDict[key];
            else
                return null;
        }

        public MPEPatternColumn GetColumn(int num)
        {   
            if (MPEPatternColumnsDict.ContainsKey(num))
                return MPEPatternColumnsDict[num];
            else
                return null;
        }

        private void Machine_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "TrackCount":
                    UpdateDataVer2();
                    break;
            }
        }
        /*
        private void Pattern_ColumnRemoved(IPatternColumn obj)
        {
            if (MPEPatternColumnsDict.ContainsKey(obj))
            {
                var mpecol = MPEPatternColumnsDict[obj];
                MPEPatternColumnsDict.Remove(obj);
                MPEPatternColumns.Remove(mpecol);
            }
        }

        private void Pattern_ColumnAdded(IPatternColumn obj)
        {
            MPEPatternColumn mpecol;
            if (MPEPatternColumnsDict.ContainsKey(obj))
                mpecol = MPEPatternColumnsDict[obj];
            else
            {
                mpecol = new MPEPatternColumn(this);
                MPEPatternColumnsDict[obj] = mpecol;
                MPEPatternColumns.Add(mpecol);
            }
            mpecol.Machine = obj.Machine;
            mpecol.MachineName = obj.Machine.Name;
            mpecol.Graphical = false;
            mpecol.ParamTrack = obj.Track;
            mpecol.ParamIndex = obj.Parameter.IndexInGroup + obj.Machine.ParameterGroups[1].Parameters.Count;


        }
        */

        public void UpdateData1()
        {
            // Don't mess with these when audio thread access them
            lock (Editor.syncLock)
            {
                if (this.RowsPerBeat == 0)
                    this.RowsPerBeat = Global.Buzz.TPB;

                // Update MPEPatternColumnsDict
                // MPEPatternColumnsDict.Clear();
                int gourp0ParamsCount = pattern.Machine.ParameterGroups[0].Parameters.Count;
                int gourp1ParamsCount = pattern.Machine.ParameterGroups[1].Parameters.Count;
                int gourp2ParamsCount = pattern.Machine.ParameterGroups[2].Parameters.Count;

                /*
                for (int i = 0; i < gourp0ParamsCount; i++)
                {
                    var col = MPEPatternColumns.FirstOrDefault(c => c.ParamIndex == i && c.ParamTrack == 0);
                    if (col != null)
                    {
                        IPatternColumn column = pattern.Columns[i];
                        MPEPatternColumnsDict.Add(column, col);
                    }
                    else
                    {
                        IPatternColumn column = pattern.Columns[i];
                        col = new MPEPatternColumn(this);
                        col.ParamTrack = 0;
                        col.Machine = pattern.Machine;
                        col.ParamIndex = i;
                        MPEPatternColumnsDict.Add(column, col);

                    }
                }
                */

                for (int i = 0; i < gourp1ParamsCount; i++)
                {
                    var mpecolumn = MPEPatternColumns.FirstOrDefault(c => c.ParamSaveDataIndex == i && c.ParamTrack == 0);

                    if (mpecolumn != null)
                    {
                        mpecolumn.Machine = Global.Buzz.Song.Machines.FirstOrDefault(x => x.Name == mpecolumn.MachineName);
                        MPEPatternColumnsDict[i] = mpecolumn;
                    }
                    else
                    {
                        IPatternColumn column = pattern.Columns[i];
                        mpecolumn = new MPEPatternColumn(this);
                        mpecolumn.ParamTrack = 0;
                        mpecolumn.Machine = pattern.Machine;
                        mpecolumn.MachineName = pattern.Machine.Name;
                        mpecolumn.ParamSaveDataIndex = i;
                        MPEPatternColumns.Add(mpecolumn);
                        MPEPatternColumnsDict[i] = mpecolumn;
                    }
                    mpecolumn.IndexInGroup = i;
                    mpecolumn.Index = i;
                    mpecolumn.GroupType = ParameterGroupType.Global;
                }

                // All track params
                for (int track = 0; track < pattern.Machine.ParameterGroups[2].TrackCount; track++)
                {
                    for (int i = 0; i < gourp2ParamsCount; i++)
                    {
                        int MPEindex = i + gourp1ParamsCount;
                        int index = i + track * gourp2ParamsCount + gourp1ParamsCount;
                        var mpecolumn = MPEPatternColumns.FirstOrDefault(c => c.ParamSaveDataIndex == MPEindex && c.ParamTrack == track);

                        if (mpecolumn != null)
                        {
                            mpecolumn.Machine = Global.Buzz.Song.Machines.FirstOrDefault(x => x.Name == mpecolumn.MachineName);
                            MPEPatternColumnsDict[index] = mpecolumn;
                        }
                        else
                        {
                            mpecolumn = new MPEPatternColumn(this);
                            mpecolumn.ParamTrack = track;
                            mpecolumn.Machine = pattern.Machine;
                            mpecolumn.MachineName = pattern.Machine.Name;
                            mpecolumn.ParamSaveDataIndex = MPEindex;
                            MPEPatternColumns.Add(mpecolumn);
                            MPEPatternColumnsDict[index] = mpecolumn;
                        }
                        mpecolumn.Index = index;
                        mpecolumn.IndexInGroup = i;
                        mpecolumn.GroupType = ParameterGroupType.Track;
                    }
                }

                foreach (var mpecolumn in MPEPatternColumns)
                {
                    if (mpecolumn.Machine != null)
                    {
                        foreach (var parameterTuple in mpecolumn.Machine.AllParametersAndTracks())
                        {
                            IParameter parameter = parameterTuple.Item1;
                            int parTrack = parameterTuple.Item2;

                            if (parTrack == mpecolumn.ParamTrack &&
                                parameter.Group.Type == mpecolumn.GroupType &&
                                parameter.IndexInGroup == mpecolumn.IndexInGroup)
                            {
                                mpecolumn.Parameter = parameter;
                            }
                        }
                    }
                }


                int trackCount = pattern.Machine.ParameterGroups[2].TrackCount;
                // Remove deleted tracks
                for (int i = gourp1ParamsCount; i < MPEPatternColumns.Count(); i++)
                {
                    var mpecolumn = MPEPatternColumns[i];

                    if (mpecolumn.ParamTrack >= trackCount)
                    {
                        MPEPatternColumns.RemoveAt(i);
                        MPEPatternColumnsDict.Remove(i);
                        i--;
                    }
                }

                /*
                foreach (var col in MPEPatternColumns)
                {
                    // Ignore Buzz Internal parameters
                    if (col.ParamIndex >= 0)
                    {
                        int index = col.ParamIndex + col.ParamTrack * pattern.Machine.ParameterGroups[2].Parameters.Count;
                        IPatternColumn column = pattern.Columns[index];
                        MPEPatternColumnsDict.Add(column, col);
                    }

                }
                */

                // Beat Rows
                foreach (var col in MPEPatternColumns)
                {
                    col.UpdateLength();
                }
            }
        }

        public void UpdateDataVer2()
        {
            // Don't mess with these when audio thread access them
            lock (Editor.syncLock)
            {
                if (this.RowsPerBeat == 0)
                    this.RowsPerBeat = Global.Buzz.TPB;

                // Update MPEPatternColumnsDict
                // MPEPatternColumnsDict.Clear();
                int gourp0ParamsCount = pattern.Machine.ParameterGroups[0].Parameters.Count;
                int gourp1ParamsCount = pattern.Machine.ParameterGroups[1].Parameters.Count;
                int gourp2ParamsCount = pattern.Machine.ParameterGroups[2].Parameters.Count;

                /*
                for (int i = 0; i < gourp0ParamsCount; i++)
                {
                    var col = MPEPatternColumns.FirstOrDefault(c => c.ParamIndex == i && c.ParamTrack == 0);
                    if (col != null)
                    {
                        IPatternColumn column = pattern.Columns[i];
                        MPEPatternColumnsDict.Add(column, col);
                    }
                    else
                    {
                        IPatternColumn column = pattern.Columns[i];
                        col = new MPEPatternColumn(this);
                        col.ParamTrack = 0;
                        col.Machine = pattern.Machine;
                        col.ParamIndex = i;
                        MPEPatternColumnsDict.Add(column, col);

                    }
                }
                */

                foreach (var mpecolumn in MPEPatternColumns)
                {
                    //mpecolumn.Machine = Global.Buzz.Song.Machines.FirstOrDefault(x => x.Name == mpecolumn.MachineName);
                    MPEPatternColumnsDict[mpecolumn.Index] = mpecolumn;

                    //mpecolumn.ParamTrack = 0;
                    //mpecolumn.Machine = pattern.Machine;
                    //mpecolumn.MachineName = mpecolumn.Machine.Name;
                    //mpecolumn.ParamSaveDataIndex = mpecolumn.Index;
                    //mpecolumn.IndexInGroup = mpecolumn.Index;
                    //mpecolumn.Index = i;
                    //mpecolumn.GroupType = ParameterGroupType.Global;
                }


                foreach (var mpecolumn in MPEPatternColumns)
                {
                    if (mpecolumn.Machine != null)
                    {
                        foreach (var parameterTuple in mpecolumn.Machine.AllParametersAndTracks())
                        {
                            IParameter parameter = parameterTuple.Item1;
                            int parTrack = parameterTuple.Item2;

                            if (parTrack == mpecolumn.ParamTrack &&
                                parameter.Group.Type == mpecolumn.GroupType &&
                                parameter.IndexInGroup == mpecolumn.IndexInGroup)
                            {
                                mpecolumn.Parameter = parameter;
                            }
                        }
                    }
                }


                int trackCount = pattern.Machine.ParameterGroups[2].TrackCount;
                // Remove deleted tracks
                for (int i = gourp1ParamsCount; i < MPEPatternColumns.Count(); i++)
                {
                    var mpecolumn = MPEPatternColumns[i];

                    if (mpecolumn.ParamTrack >= trackCount)
                    {
                        MPEPatternColumns.RemoveAt(i);
                        MPEPatternColumnsDict.Remove(i);
                        i--;
                    }
                }

                /*
                foreach (var col in MPEPatternColumns)
                {
                    // Ignore Buzz Internal parameters
                    if (col.ParamIndex >= 0)
                    {
                        int index = col.ParamIndex + col.ParamTrack * pattern.Machine.ParameterGroups[2].Parameters.Count;
                        IPatternColumn column = pattern.Columns[index];
                        MPEPatternColumnsDict.Add(column, col);
                    }

                }
                */

                // Beat Rows
                foreach (var col in MPEPatternColumns)
                {
                    col.UpdateLength();
                }
            }
        }

        internal void SetBeatCount(int beats)
        {
            foreach(var col in MPEPatternColumns)
            {
                col.SetBeatCount(beats);
            }
        }

        internal MPEPatternColumn GetColumnOrCreate(IPatternColumn patternColumn)
        {
            MPEPatternColumn ret;

            int key = GetParamIndex(patternColumn.Parameter, patternColumn.Track);

            if (!MPEPatternColumnsDict.ContainsKey(key))
            {
                int gourp1ParamsCount = pattern.Machine.ParameterGroups[1].Parameters.Count;
                int gourp2ParamsCount = pattern.Machine.ParameterGroups[2].Parameters.Count;

                var mpecolumn = new MPEPatternColumn(this);
                mpecolumn.ParamTrack = patternColumn.Track;
                mpecolumn.Machine = pattern.Machine;
                mpecolumn.MachineName = pattern.Machine.Name;
                mpecolumn.ParamSaveDataIndex = patternColumn.Parameter.IndexInGroup + gourp1ParamsCount;
                MPEPatternColumns.Add(mpecolumn);
                MPEPatternColumnsDict.Add(key, mpecolumn);

                ret = mpecolumn;
            }
            else
                ret = MPEPatternColumnsDict[key];

            return ret;
        }

        public int GetParamIndex(IParameter parameter, int track)
        {
            int ret = 0;
            if (parameter.Group == pattern.Machine.ParameterGroups[1])
                ret = parameter.IndexInGroup;
            else if (parameter.Group == pattern.Machine.ParameterGroups[2])
            {
                ret = parameter.IndexInGroup + pattern.Machine.ParameterGroups[1].Parameters.Count +
                    (pattern.Machine.ParameterGroups[2].Parameters.Count * track);
            }

            return ret;
        }

        public MPEPattern(PatternEditor editor, string name)
        {
            this.Editor = editor;
            this.PatternName = name;
        }

        internal void Quantize()
        {
            foreach(var c in MPEPatternColumns)
            {
                Dictionary<int, PatternEvent> dict = new Dictionary<int, PatternEvent>();  
                var e = c.GetEvents(0, pattern.Length * PatternEvent.TimeBase).ToArray();
                // Clear
                c.SetEvents(e, false);

                for (int i = 0; i < e.Count(); i++)
                {
                    e[i].Time = c.GetTimeQuantized(e[i].Time); // Bug here!
                    dict[e[i].Time] = e[i];
                }

                // Set
                c.SetEvents(dict.Values.ToArray(),true);
            }
        }

        internal class MPEParameterSet
        {
            internal IList<IParameter> parameters;
            internal int track;
            internal string name;
            internal ParameterGroupType groupType;

            internal MPEParameterSet()
            {
                parameters = new List<IParameter>();
                track = -1;
            }
        }

        internal IEnumerable<MPEParameterSet> GetParameterSets()
        {
            IList<MPEParameterSet> parameterSets = new List<MPEParameterSet>();

            MPEParameterSet set = new MPEParameterSet();

            foreach (var column in MPEPatternColumns)
            {
                if (set.groupType != column.GroupType || set.track != column.ParamTrack)
                {
                    if (set.parameters.Count > 0)
                    {
                        parameterSets.Add(set);
                    }

                    set = new MPEParameterSet();
                    set.track = column.ParamTrack;
                    set.name = column.Machine.Name;
                    set.groupType = column.GroupType;
                    if (column.GroupType == ParameterGroupType.Global)
                        set.name += "\nGlobal";
                    else if (column.GroupType == ParameterGroupType.Track)
                        set.name += "\nTrack " + set.track;
                    set.parameters.Add(column.Parameter);
                }
                else
                {
                    set.parameters.Add(column.Parameter);
                }
            }

            if (set.parameters.Count > 0)
            {
                parameterSets.Add(set);
            }

            return parameterSets.ToArray();
        }
    }
}
