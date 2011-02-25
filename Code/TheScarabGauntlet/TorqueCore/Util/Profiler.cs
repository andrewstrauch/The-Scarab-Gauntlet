//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Collections;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using GarageGames.Torque.Core;
using GarageGames.Torque.XNA;
using GarageGames.Torque.GUI;
using GarageGames.Torque.Sim;
using GarageGames.Torque.MathUtil;
using GarageGames.Torque.Platform;



namespace GarageGames.Torque.Util
{
#if TORQUE_PROFILE
    /// <summary>
    /// ProfilerCodeBlock keeps track of a single code block and holds references to each unique instance of that code 
    /// block on the call stack. These are normally created automatically by the profiler while it runs, but you can 
    /// optionally create a static ProfilerCodeBlock for a method and pass that to your StartBlock and EndBlock calls 
    /// rather than a string name. You would want to do this specifically for high-traffic code that gets called many
    /// times per tick to skip the profiler's hashtable lookups for a slightly faster profile.
    /// </summary>
    public class ProfilerCodeBlock : IComparable
    {
    #region Constructors

        public ProfilerCodeBlock(string name)
        {
            _name = name;
            _nextCodeBlock = Profiler.Instance._codeBlockList;
            Profiler.Instance._codeBlockList = this;
        }

    #endregion


    #region Public properties, operators, constants, and enums

        /// <summary>
        /// The name of this code block.
        /// </summary>
        public string Name
        {
            get { return _name; }
        }

        /// <summary>
        /// The total time so far that this code block has executed.
        /// </summary>
        public double TotalTime
        {
            get { return _totalTime; }
            set { _totalTime = value; }
        }

        /// <summary>
        /// The total time spent in child calls of this code block's instances.
        /// </summary>
        public double SubTime
        {
            get { return _subTime; }
            set { _subTime = value; }
        }

        /// <summary>
        /// The total time spent in just this code block's instances, and not in their children (own time).
        /// </summary>
        public double NonSubTime
        {
            get { return _totalTime - _subTime; }
        }

        /// <summary>
        /// The number of times this code block was called.
        /// </summary>
        public int InvokeCount
        {
            get { return _invokeCount; }
            set { _invokeCount = value; }
        }

    #endregion


    #region Public Methods

        public int CompareTo(object obj)
        {
            ProfilerCodeBlock obj2 = obj as ProfilerCodeBlock;

            if (obj2 == null)
                throw new ArgumentException("Object is not a ProfilerCodeBlock");

            return obj2.NonSubTime.CompareTo(NonSubTime);
        }

    #endregion


    #region Private, protected, internal methods

        /// <summary>
        /// Reset this code block. Clears the instances and resets total time, sub time, and invoke count to zero.
        /// This should only be called when the profiler itself is resetting.
        /// </summary>
        internal void _ResetCodeBlock()
        {
            _totalTime = 0;
            _subTime = 0;
            _invokeCount = 0;
        }

    #endregion


    #region Private, protected, internal fields

        internal ProfilerCodeBlock _nextCodeBlock;
        private string _name;
        private double _totalTime;
        private double _subTime;
        private int _invokeCount;

    #endregion

    }
#else
    public class ProfilerCodeBlock
    {
        public ProfilerCodeBlock(string name) { }
    }
#endif
    /// <summary>
    /// ProfilerCodeBlockInstance is a structure to keep track of data about a code block when called from a specific other 
    /// code block. The profiler will create one code block instance for each entry into a code block with a unique parent. 
    /// Code block instances are stored on their corresponding ProfilerCodeBlock and root instances (instances called when the 
    /// stack is empty) are stored on the profiler itself. Each instance also has references to it's parent, children, and 
    /// the ProfilerCodeBlock that it is an instance of.
    /// </summary>
    internal class ProfilerCodeBlockInstance : IComparable
    {
        #region Constructors

        public ProfilerCodeBlockInstance(ProfilerCodeBlock instanceOf, ProfilerCodeBlockInstance parent)
        {
            _instanceOf = instanceOf;
            _parent = parent;
        }

        #endregion


        #region Public properties, operators, constants, and enums

        /// <summary>
        /// The ProfilerCodeBlock that this ProfilerCodeBlockInstance is an instance of.
        /// </summary>
        public ProfilerCodeBlock InstanceOf
        {
            get { return _instanceOf; }
        }

        /// <summary>
        /// The total time this instance has run.
        /// </summary>
        public double TotalTime
        {
            get { return _totalTime; }
            set { _totalTime = value; }
        }

        /// <summary>
        /// The total time spent in children of this code block instance.
        /// </summary>
        public double SubTime
        {
            get { return _subTime; }
            set { _subTime = value; }
        }

        /// <summary>
        /// The total time spent in this code block and not in its children (own time).
        /// </summary>
        public double NonSubTime
        {
            get { return _totalTime - _subTime; }
        }

        /// <summary>
        /// The total number of times this instance was called.
        /// </summary>
        public int InvokeCount
        {
            get { return _invokeCount; }
            set { _invokeCount = value; }
        }

        /// <summary>
        /// Used by the profiler to keep track of recursion. When the profiler is active, this will be the number of times this code 
        /// block has called itsef unresolved at the moment. This depth will increase from zero when recursion begins and 
        /// decrease back to zero as recursion resolves. When not profiling (i.e. at dump time, etc.) this property should 
        /// always be zero.
        /// </summary>
        public int RecursionDepth
        {
            get { return _recursionDepth; }
            set { _recursionDepth = value; }
        }

        /// <summary>
        /// The elapsed system ticks at the moment this code block instance was reached on this pass. When the code block is ended
        /// this will be used to calculate the elapsed time spent in the code block instance. Recursive code blocks don't recalculate
        /// time untill recursion completely resolves, so start time does not neccesarily get reset for each StartBlock call.
        /// </summary>
        public double PassStartTime
        {
            get { return _passStartTime; }
            set { _passStartTime = value; }
        }

        /// <summary>
        /// The ProfilerCodeBlockInstance that was active when this instance was started. In other words, the code block that called
        /// this one.
        /// </summary>
        public ProfilerCodeBlockInstance Parent
        {
            get { return _parent; }
        }

        /// <summary>
        /// The last seen child instance of this code block instance. This is used for fast lookups to eliminate about 90% of the 
        /// hash lookups for instances.
        /// </summary>
        public ProfilerCodeBlockInstance LastSeenInstance
        {
            get { return _lastSeenInstance; }
            set { _lastSeenInstance = value; }
        }

        /// <summary>
        /// A list of all the instances called directly from this instance. This is used mainly at dump time to sort the data colleced.
        /// </summary>
        public List<ProfilerCodeBlockInstance> Children
        {
            get { return _children; }
        }

        #endregion


        #region Public Methods

        public int CompareTo(object obj)
        {
            ProfilerCodeBlockInstance obj2 = obj as ProfilerCodeBlockInstance;

            if (obj2 == null)
                throw new ArgumentException("Object is not a ProfilerCodeBlockInstance");

            return obj2.TotalTime.CompareTo(TotalTime);
        }

        #endregion


        #region Private, protected, internal fields

        private ProfilerCodeBlock _instanceOf;
        private double _totalTime;
        private double _subTime;
        private int _invokeCount;
        private int _recursionDepth;
        private double _passStartTime;
        private ProfilerCodeBlockInstance _parent;
        private ProfilerCodeBlockInstance _lastSeenInstance;
        private List<ProfilerCodeBlockInstance> _children = new List<ProfilerCodeBlockInstance>();
        internal Hashtable _childHash = new Hashtable();

        #endregion
    }



    /// <summary>
    /// A class to help profile your code. Use Profiler.Instance.StartBlock("name") and Profiler.Instance.EndBlock("name") to define the areas
    /// you want the profiler to report about. The StartBlock and EndBlock calls must match up by name, so make sure you put EndBlock calls at the
    /// end of each code path after a StartBlock call (for example, if you have multiple return statements in a method). Call StartProfiling() to 
    /// start profiling and then DumpProfile() to dump the data. Use the properties to define how the dump will take place. By default it will 
    /// dump to the GUI and from there you can dump to a file (file dump is PC-only due to restrictions on the XBox), but you can also make it 
    /// automatically dump to a file or the console instead.
    /// </summary>
    public class Profiler
    {
        #region Static methods, fields, constructors

#if TORQUE_PROFILE

        /// <summary>
        /// True if the profiler has been initialized.
        /// </summary>
        public static bool IsInitialized
        {
            get { return _instance != null; }
        }
        
        /// <summary>
        /// The singleton instance of the profiler.
        /// </summary>
        public static Profiler Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new Profiler();

                if (InputManager.Instance.HasDevices && !_inputInitialized)
                {
                    _inputInitialized = true;
                    _instance._SetupInput();
                }

                return _instance;
            }
        }

        private static bool _inputInitialized = false;
        private static Profiler _instance;

#else

        public static bool IsInitialized
        {
            get { return false; }
        }

        public static Profiler Instance
        {
            get { return null; }
        }

#endif

#if TORQUE_PROFILE

#if !XBOX
        [DllImport("kernel32.dll")]
        private static extern int QueryPerformanceCounter(out Int64 lpPerformanceCount);

        [DllImport("kernel32.dll")]
        private static extern int QueryPerformanceFrequency(out Int64 lpPerformanceCount);
#else
        private static int QueryPerformanceCounter(out Int64 lpPerformanceCount)
        {
            lpPerformanceCount = _timer.ElapsedTicks;
            return 0;
        }

        private static int QueryPerformanceFrequency(out Int64 lpPerformanceCount)
        {
            lpPerformanceCount = Stopwatch.Frequency;
            return 0;
        }

        private static Stopwatch _timer = new Stopwatch();
#endif // !XBOX

#endif // TORQUE_PROFILE
        #endregion

#if TORQUE_PROFILE

        #region Public properties, operators, constants, and enums

        /// <summary>
        /// Returns true if the profiler is currently in the process of profiling.
        /// </summary>
        public bool IsProfiling
        {
            get { return _isProfiling; }
        }

        /// <summary>
        /// Returns true if the profiler is currently dumping or waiting to dump.
        /// </summary>
        public bool IsDumping
        {
            get { return _wantsToDump; }
        }

        /// <summary>
        /// Specifies whether or not the profiler will dump the profile results to the console aswell.
        /// </summary>
        public bool DumpsToConsole
        {
            get { return _dumpsToConsole; }
            set { _dumpsToConsole = value; }
        }

        #endregion

#endif // TORQUE_PROFILE


        #region Public Methods
#if TORQUE_PROFILE
        /// <summary>
        /// Tells the profiler to start profiling as soon as possible.
        /// </summary>
        [Conditional("TORQUE_PROFILE")]
        public void StartProfiling()
        {
            // turn the "wants to profile" flag on
            _wantsToProfile = true;
        }

        /// <summary>
        /// Tells the profiler to stop profiling as soon as possible.
        /// </summary>
        [Conditional("TORQUE_PROFILE")]
        public void StopProfiling()
        {
            // turn the "wants to profile" flag off
            _wantsToProfile = false;
        }

        /// <summary>
        /// Tells the profiler to dump as soon as possible.
        /// </summary>
        [Conditional("TORQUE_PROFILE")]
        public void DumpProfile()
        {
            // turn the "wants to dump" flag on
            _wantsToDump = true;
        }

        /// <summary>
        /// Resets the profiler.
        /// </summary>
        [Conditional("TORQUE_PROFILE")]
        public void ResetProfiler()
        {
            // get a list of all code blocks
            List<ProfilerCodeBlock> codeBlocks = _GetCodeBlocksList();

            // reset each code block
            foreach (ProfilerCodeBlock block in codeBlocks)
                block._ResetCodeBlock();

            _isProfiling = false;
            _totalTime = 0.0f;
            _currentBlockInstance = null;
            _rootBlockInstanceHash.Clear();

#if XBOX
            _timer.Reset();
#endif // XBOX
        }
#endif // TORQUE_PROFILE

        /// <summary>
        /// Tells the profiler where a certain code block starts.
        /// </summary>
        /// <param name="name">The name of the code block to profile.</param>
        [Conditional("TORQUE_PROFILE")]
        public void StartBlock(string name)
        {
#if TORQUE_PROFILE
            // validate name
            if (name == null || name.Length < 1)
                return;

            // get the profiler code block object for this name
            ProfilerCodeBlock thisBlock = _managedCodeBlocks[name] as ProfilerCodeBlock;

            // create a profiler code block for this root if none exists
            if (thisBlock == null)
            {
                // create a new code block and hash it
                thisBlock = new ProfilerCodeBlock(name);
                _managedCodeBlocks.Add(name, thisBlock);
            }

            // start this code block
            StartBlock(thisBlock);
#endif
        }

        /// <summary>
        /// Tells the profiler where a certain code block starts. This method is faster due to the fact that 
        /// it doesn't have to generate a code block object. Use this for high-traffic code.
        /// </summary>
        /// <param name="thisBlock">A pre-made static code block object to use when profiling this specific section.</param>
        [Conditional("TORQUE_PROFILE")]
        public void StartBlock(ProfilerCodeBlock thisBlock)
        {
#if TORQUE_PROFILE
            // increment stack depth
            _stackDepth++;

            // assert that stack depth is not huge
            if (_stackDepth >= _maxStackDepth)
            {
                // find the code block with the most invocations
                List<ProfilerCodeBlock> codeBlocks = _GetCodeBlocksList();
                ProfilerCodeBlock block = codeBlocks[0];

                foreach (ProfilerCodeBlock codeBlock in codeBlocks)
                    if (codeBlock.InvokeCount > block.InvokeCount)
                        block = codeBlock;

                Assert.Fatal(false, "Profiler.StartBlock - Stack overflow in profiler. Most likely culprit is: " + block.Name + " (" + block.InvokeCount.ToString() + " on stack)\n\nPlease make sure your StartBlock and EndBlock calls match up!");
            }

            // make sure we're profiling
            if (!_isProfiling || thisBlock == null)
                return;

            // get the profiler code block instance object for this particular code path
            ProfilerCodeBlockInstance thisBlockInstance = null;

            // check for recursion
            if (_currentBlockInstance != null && _currentBlockInstance.InstanceOf == thisBlock)
            {
                // recursion: use the current code block rather than creating another instance
                thisBlockInstance = _currentBlockInstance;

                // increase the recursion depth of this instance
                thisBlockInstance.RecursionDepth++;
            }
            else
            {
                // check if this is a root instance
                bool isRoot = _currentBlockInstance == null;

                // check for the last seen instance
                if (!isRoot && _currentBlockInstance != null && _currentBlockInstance.LastSeenInstance != null
                    && _currentBlockInstance.LastSeenInstance.InstanceOf == thisBlock)
                {
                    // get the last seen instance on this code block
                    thisBlockInstance = _currentBlockInstance.LastSeenInstance;
                }
                else
                {
                    // get the existing block instance
                    if (!isRoot)
                        thisBlockInstance = _currentBlockInstance._childHash[thisBlock.Name] as ProfilerCodeBlockInstance;
                    else
                        thisBlockInstance = _rootBlockInstanceHash[thisBlock.Name] as ProfilerCodeBlockInstance;

                    // create a code block instance for this section if none exists
                    if (thisBlockInstance == null)
                    {
                        // create a new instance and hash it on the code block
                        thisBlockInstance = new ProfilerCodeBlockInstance(thisBlock, _currentBlockInstance);

                        // hash the instance on either the current code block, or the root hash
                        if (!isRoot)
                            _currentBlockInstance._childHash.Add(thisBlock.Name, thisBlockInstance);
                        else
                            _rootBlockInstanceHash.Add(thisBlock.Name, thisBlockInstance);
                    }
                }

                // set the start time of the current pass
                long ticks;
                QueryPerformanceCounter(out ticks);

                thisBlockInstance.PassStartTime = ((double)ticks / (double)_tickFrequency) * 1000.0;

                // validate the current code block instance
                Assert.Fatal(_ValidateCodeBlockInstance(thisBlockInstance), "Profiler.StartBlock - Profiler doesn't support multiple code paths to the same child-parent code block pair. Email Adam Larson for details: adaml@garagegames.com");

                // record the last seen instance for fast lookups next time
                if (_currentBlockInstance != null)
                    _currentBlockInstance.LastSeenInstance = thisBlockInstance;

                // push the new codeblock onto the stack
                _currentBlockInstance = thisBlockInstance;
            }

            // increment the invoke count
            thisBlock.InvokeCount++;
            thisBlockInstance.InvokeCount++;
#endif // TORQUE_PROFILE
        }

        /// <summary>
        /// Tells the profiler where a certain code block ends.
        /// </summary>
        /// <param name="name">The name of the code block to stop profiling.</param>
        [Conditional("TORQUE_PROFILE")]
        public void EndBlock(string name)
        {
#if TORQUE_PROFILE
            // call the normal EndBlock method with the proper name
            if (!_managedCodeBlocks.Contains(name) ||
                (_currentBlockInstance != null && name != _currentBlockInstance.InstanceOf.Name))
            {
                Assert.Fatal(false, "Profiler.EndBlock - Specified name (" + _currentBlockInstance.InstanceOf.Name + ") not found.\n\nPlease make sure your StartBlock and EndBlock calls match up!");
                return;
            }

            EndBlock(_managedCodeBlocks[name] as ProfilerCodeBlock);
#endif // TORQUE_PROFILE
        }

        /// <summary>
        /// Tells the profiler where a certain code block ends. This method is faster due to the fact that 
        /// it doesn't have to generate a code block object. Use this for high-traffic code.
        /// </summary>
        /// <param name="thisBlock">A pre-made static code block object to use when profiling this specific section.</param>
        [Conditional("TORQUE_PROFILE")]
        public void EndBlock(ProfilerCodeBlock thisBlock)
        {
#if TORQUE_PROFILE
            // decrement stack depth
            _stackDepth--;

            // assert that stack depth is not negative!
            Assert.Fatal(_stackDepth >= 0, "Profiler.EndBlock - Stack underflow in profiler. \n\nPlease make sure your StartBlock and EndBlock calls match up!");

            // validate name
            if (thisBlock.Name == null || thisBlock.Name.Length < 1)
                return;

            // make sure we're enabled
            if (_isProfiling)
            {
                // check for recursion depth
                if (_currentBlockInstance.RecursionDepth > 0)
                {
                    // current code block was called by itself, decrement 
                    // recursion depth and return
                    _currentBlockInstance.RecursionDepth--;
                    return;
                }

                // get the elapsed time since this instance was started
                long ticks;
                QueryPerformanceCounter(out ticks);

                double elapsed = (((double)ticks / (double)_tickFrequency) * 1000.0) - _currentBlockInstance.PassStartTime;

                // add to this stack's total time
                _currentBlockInstance.TotalTime += elapsed;
                _currentBlockInstance.InstanceOf.TotalTime += elapsed;

                // inrement this stack's sub time from the parent up
                if (_currentBlockInstance.Parent != null)
                {
                    _currentBlockInstance.Parent.SubTime += elapsed;
                    _currentBlockInstance.Parent.InstanceOf.SubTime += elapsed;
                }

                // pop the current block instance from the stack
                _currentBlockInstance = _currentBlockInstance.Parent;
            }

            // process flags: check if we if we are ready to start/stop profiling or dump our data
            if (_stackDepth == 0)
            {
                // if we were waiting on a chance to dump, do so now
                if (_wantsToDump && _isProfiling)
                {
                    // dump the profile
                    _DumpProfile();

                    // turn the 'wants to dump' flag off
                    _wantsToDump = false;
                }

                // now check for switch to profiling or not profiling
                if (_isProfiling != _wantsToProfile)
                {
                    // get the desired profiling value
                    _isProfiling = _wantsToProfile;

#if XBOX
                    if (_isProfiling)
                        _timer.Start();
#endif // XBOX

                    // make sure that if we're going to start profiling we have a tick frequency!
                    if (_isProfiling && _tickFrequency == 0)
                        QueryPerformanceFrequency(out _tickFrequency);
                }
            }
#endif // TORQUE_PROFILE
        }

        #endregion

#if TORQUE_PROFILE


        #region Private, protected, internal methods

        private void _SetupInput()
        {
            int keyboardId = InputManager.Instance.FindDevice("keyboard");
            if (keyboardId >= 0)
            {
                InputMap.Global.BindAction(keyboardId, (int)Microsoft.Xna.Framework.Input.Keys.F1, _OnToggleGui);
                InputMap.Global.BindAction(keyboardId, (int)Microsoft.Xna.Framework.Input.Keys.F2, _OnStartProfiling);
            }
        }

        private List<ProfilerCodeBlock> _GetCodeBlocksList()
        {
            List<ProfilerCodeBlock> blocks = new List<ProfilerCodeBlock>();

            ProfilerCodeBlock block = _codeBlockList;

            while (block != null)
            {
                if (!blocks.Contains(block))
                    blocks.Add(block);

                block = block._nextCodeBlock;
            }

            return blocks;
        }

        private void _DumpProfile()
        {
            // make sure the profiler doesn't do anything in the meantime
            _stackDepth++;

            string output = _GenerateOutputString();

            _profileDumps.Add(output);

            ProfileGUI.Instance.Profile = _profileDumps.Count - 1;

            if (_dumpsToConsole)
                TorqueConsole.Echo(output);

            // decrement stack depth and reset the profiler
            _stackDepth--;

            ResetProfiler();
        }

        [Conditional("TORQUE_PROFILE")]
        internal void _FileDump(int profileNum)
        {
#if !XBOX
            // make sure the profile exists
            if (profileNum < 0 || profileNum > _profileDumps.Count - 1)
                return;

            // get the appropriate filename
            string fileName = "PROFILE" + profileNum + ".log";

            // create a file with that name
            if (!File.Exists(fileName))
                File.Create(fileName).Close();

            // create a text writer for the file
            TextWriter file = new StreamWriter(fileName);

            // write the profile dump to the file
            file.Write(_profileDumps[profileNum]);

            // close the file
            file.Close();
#endif // !XBOX
        }

        private string _GenerateOutputString()
        {
            // get a list of all code blocks
            List<ProfilerCodeBlock> codeBlocks = _GetCodeBlocksList();

            // reset total time
            _totalTime = 0;

            // iterate over the externally added code blocks
            foreach (ProfilerCodeBlock block in codeBlocks)
            {
                // subtract this code block's sub time from our known total time
                _totalTime += block.TotalTime - block.SubTime;
            }

            // sort the list
            // (defined by the IComparable implementation.
            // see ProfilerCodeBlock.CompareTo)
            codeBlocks.Sort();

            // write the header
            string output = "PROFILER DUMP:\n";
            output += string.Format("Total Time: {0:0.0000} ms\n\n", _totalTime); ;
            output += "(Ordered by total non-sub time)\n";
            output += "%NSTime  %Total Invoke #  AvgTime Name\n";

            double nsTime, totalTime, avgTime;

            // write out the sorted list
            foreach (ProfilerCodeBlock block in codeBlocks)
            {
                nsTime = (block.NonSubTime / _totalTime) * 100;
                totalTime = (block.TotalTime / _totalTime) * 100;
                avgTime = block.TotalTime / block.InvokeCount;

                output += string.Format("{0,7:0.000} {1,7:0.000} {2,8} {3,8:0.000} {4}\n", nsTime, totalTime, block.InvokeCount, avgTime, block.Name);
            }

            // write header and root node
            output += "\n(Ordered by stack trace total time)\n";
            output += " %Total %NSTime Invoke #  AvgTime Name\n";
            output += string.Format("{0,7:0.000} {1,7:0.000} {2,8} {3,8} {4}\n", 100.0f, 0.0f, 0, 0, "ROOT");

            // sort the root code block instances
            // (defined in ProfilerCodeBlockInstance.CompareTo)
            IDictionaryEnumerator enumerator = _rootBlockInstanceHash.GetEnumerator();
            List<ProfilerCodeBlockInstance> rootInstances = new List<ProfilerCodeBlockInstance>();

            while (enumerator.MoveNext())
                rootInstances.Add(enumerator.Value as ProfilerCodeBlockInstance);

            rootInstances.Sort();

            // initialize depth to zero
            int depth = 0;

            // dump each instance node
            foreach (ProfilerCodeBlockInstance instance in rootInstances)
                _GenerateOutputStringRecursive(instance, ref depth, ref output);

            return output;
        }

        private void _GenerateOutputStringRecursive(ProfilerCodeBlockInstance instance, ref int depth, ref string output)
        {
            // generate spacing string to visually represent the hierarchy based on depth
            string depthSpace = " ";

            for (int i = 0; i < depth; ++i)
                depthSpace += "   ";

            // get non-sub time and total time percentages
            double nsTime = (instance.NonSubTime / _totalTime) * 100;
            double totalTime = (instance.TotalTime / _totalTime) * 100;
            double avgTime = instance.TotalTime / instance.InvokeCount;

            // write this block instance
            output += string.Format("{0,7:0.000} {1,7:0.000} {2,8} {3,8:0.000} {4} {5}\n", totalTime, nsTime, instance.InvokeCount, avgTime, depthSpace, instance.InstanceOf.Name);

            IDictionaryEnumerator enumerator = instance._childHash.GetEnumerator();
            List<ProfilerCodeBlockInstance> children = new List<ProfilerCodeBlockInstance>();

            while (enumerator.MoveNext())
                children.Add(enumerator.Value as ProfilerCodeBlockInstance);

            // check if this instance has any children
            if (children.Count > 0)
            {
                // sort the list of children
                // (defined by the IComparable implementation.
                // see ProfilerCodeBlockInstance.CompareTo)
                children.Sort();

                //increment depth for child calls
                depth++;

                // recurse through children
                foreach (ProfilerCodeBlockInstance child in children)
                    _GenerateOutputStringRecursive(child, ref depth, ref output);

                // decrement depth for sibling calls
                depth--;
            }
        }

        internal string _GetProfileString(int profileNum)
        {
            // make sure the profile exists
            if (profileNum < 0 || profileNum > _profileDumps.Count)
                return null;

            return _profileDumps[profileNum];
        }

        void _OnStartProfiling(float val)
        {
            if (val > 0.0f)
            {
                StartProfiling();
                DumpProfile();
            }
        }

        void _OnProfileTrigger(float val)
        {
            if (val > 0.0f && !_wasTriggerDown)
            {
                _wasTriggerDown = true;
                ResetProfiler();
                StartProfiling();
            }
            else if (val == 0.0f && _wasTriggerDown)
            {
                _wasTriggerDown = false;
                DumpProfile();
            }
        }

        void _OnToggleGui(float val)
        {
            if (val > 0.0f)
                ProfileGUI.Instance.Visible = !ProfileGUI.Instance.Visible;
        }

        /// <summary>
        /// This method was used to debug the profiler. It can tell you whether or not a code block instance that you want 
        /// to add to the profiler's call stack is valid.
        /// </summary>
        /// <param name="blockInstance">The code block instance you're about to assign to _currentBlockInstance.</param>
        /// <returns>True if the code block instance is valid</returns>
        private bool _ValidateCodeBlockInstance(ProfilerCodeBlockInstance blockInstance)
        {
            ProfilerCodeBlockInstance mine, theirs;

            theirs = blockInstance.Parent;
            mine = _currentBlockInstance;

            while (theirs != null)
            {
                if (mine == null || mine.InstanceOf.Name != theirs.InstanceOf.Name)
                    return false;

                theirs = theirs.Parent;
                mine = mine.Parent;
            }

            if (mine == null)
                return true;

            return false;
        }

        #endregion


        #region Private, protected, internal fields

        private double _totalTime;
        private long _tickFrequency;

        private int _stackDepth;
        private int _maxStackDepth = 256;

        private bool _isProfiling;
        private bool _wantsToProfile;
        private bool _wantsToDump;
        private bool _dumpsToConsole;

        private bool _wasTriggerDown;

        internal ProfilerCodeBlock _codeBlockList; // linked list of code blocks
        private Hashtable _managedCodeBlocks = new Hashtable(); // hash table of code blocks passed as strings
        private ProfilerCodeBlockInstance _currentBlockInstance; // reference to the current code block
        private Hashtable _rootBlockInstanceHash = new Hashtable(); // a hash of root block instances to help us form a hierarchy later

        private List<string> _profileDumps = new List<string>();

        internal int _Profiles
        {
            get { return _profileDumps.Count; }
        }

        #endregion
#endif // TORQUE_PROFILE
    }


#if TORQUE_PROFILE
    /// <summary>
    /// Profile viewer gui class used by the profiler to allow viewing of profiles from within the engine. The point is to be able to
    /// get profiler data without a debugger attached (on the XBox, for example).
    /// </summary>
    public class ProfileGUI
    {

    #region Static methods, fields, constructors

        public static ProfileGUI Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new ProfileGUI();

                return _instance;
            }
        }

        public static bool IsInitialized
        {
            get { return _instance != null; }
        }

        public static ProfileGUI _instance;

    #endregion


    #region Constructors

        private ProfileGUI()
        {
            // define gui styles
            GUIStyle rootStyle = new GUIStyle();
            rootStyle.IsOpaque = false;

            GUIStyle scrollerStyle = new GUIStyle();
            scrollerStyle.IsOpaque = true;
            scrollerStyle.FillColor[CustomColor.ColorBase] = new Color(0, 0, 0, 150);
            scrollerStyle.HasBorder = true;
            scrollerStyle.BorderColor[CustomColor.ColorBase] = new Color(0, 0, 0, 200);
            scrollerStyle.Focusable = true;

            GUIMLTextStyle bodyStyle = new GUIMLTextStyle();
            bodyStyle.Alignment = TextAlignment.JustifyLeft;
            bodyStyle.TextColor[CustomColor.ColorBase] = Color.White;
            bodyStyle.SizeToText = true;
            bodyStyle.AutoSizeHeightOnly = true;
            bodyStyle.FontType = "Courier14";

            GUITextStyle textStyle = new GUITextStyle();
            textStyle.Alignment = TextAlignment.JustifyRight;
            textStyle.TextColor[CustomColor.ColorBase] = Color.White;
            textStyle.SizeToText = true;
            textStyle.FontType = "Courier14";

            // init gui controls
            _root = new GUIControl();
            _root.Style = rootStyle;
            _root.HorizSizing = HorizSizing.Width;
            _root.VertSizing = VertSizing.Height;

            _scroll = new GUIScroll();
            _scroll.Style = scrollerStyle;
            _scroll.HorizSizing = HorizSizing.Relative;
            _scroll.VertSizing = VertSizing.Relative;
            _scroll.Position = new Vector2(10.0f, 10.0f);
            _scroll.Size = new Vector2(GUICanvas.Instance.Size.X - 20.0f, GUICanvas.Instance.Size.Y - 20.0f);
            _scroll.FocusOnWake = true;
            _scroll.Visible = true;
            _scroll.Folder = _root;
            _scroll.InputMap = new InputMap();

            int gamepadId = InputManager.Instance.FindDevice("gamepad0");
            _scroll.InputMap.BindAction(gamepadId, (int)XGamePadDevice.GamePadObjects.Left, _OnLeft);
            _scroll.InputMap.BindAction(gamepadId, (int)XGamePadDevice.GamePadObjects.Right, _OnRight);
            _scroll.InputMap.BindAction(gamepadId, (int)XGamePadDevice.GamePadObjects.Start, _OnStart);

            _page = new GUIText();
            _page.Style = textStyle;
            _page.HorizSizing = HorizSizing.Relative;
            _page.VertSizing = VertSizing.Relative;
            _page.Position = new Vector2(GUICanvas.Instance.Size.X - 180.0f, 40.0f);
            _page.Text = string.Empty;
            _page.Visible = true;
            _page.Folder = _root;

            _saveToFile = new GUIText();
            _saveToFile.Style = textStyle;
            _saveToFile.HorizSizing = HorizSizing.Relative;
            _saveToFile.VertSizing = VertSizing.Relative;
            _saveToFile.Position = new Vector2(GUICanvas.Instance.Size.X - 260, 20.0f);
            _saveToFile.Text = string.Empty;
#if !XBOX
            _saveToFile.Visible = true;
            _saveToFile.Folder = _root;
#endif // !XBOX

            _text = new GUIMLText();
            _text.Style = bodyStyle;
            _text.HorizSizing = HorizSizing.Width;
            _text.VertSizing = VertSizing.Height;
            _text.Position = new Vector2(3.0f, 3.0f);
            _text.Size = new Vector2(GUICanvas.Instance.Size.X - 40.0f, GUICanvas.Instance.Size.Y - 20.0f);
            _text.Text = "Torque X Profiler\n\nNo profiles have been taken yet.\nPress (F2) to take a snapshot since the last profile.\n\nIf the profiler doesn't seem to be responding, there may be a stack overflow. Make sure that \nfor each StartBlock call there is an EndBlock call. If the code you're profiling has multiple \nreturn paths you will need multiple EndBlock calls: one before each return statement. If \nthis is an issue, try profiling with a debug build to catch the overflow assert (you might \nneed to run your app for a while before you get it, depending on the cause of the overflow).\n\nEnjoy! ;)";
            _text.Visible = true;
            _text.Folder = _scroll;
        }

    #endregion


    #region Public properties, operators, constants, and enums

        /// <summary>
        /// The index of the profile being viewed.
        /// </summary>
        public int Profile
        {
            get { return _profile; }
            set
            {
                if (_saveToFile.Text == string.Empty)
                    _saveToFile.Text = "Press (Start) to save";

                _profile = value % Profiler.Instance._Profiles;

                if (_profile < 0)
                    _profile = Profiler.Instance._Profiles - 1;

                if (Profiler.Instance._Profiles > 0)
                    _text.Text = Profiler.Instance._GetProfileString(_profile);

                _page.Text = "Profile < " + _profile.ToString() + " >";
            }
        }

        /// <summary>
        /// Whether or not the profiler gui is visible.
        /// </summary>
        public bool Visible
        {
            get { return _root.Awake; }
            set
            {
                if (value)
                {
                    GUICanvas.Instance.PushDialogControl(_root, 0);
                    //GUICanvas.Instance.SetFocusControl(_scroll);
                }
                else
                {
                    GUICanvas.Instance.PopDialogControl(_root);
                }
            }
        }

    #endregion


    #region Public Methods

        /// <summary>
        /// Scrolls the gui.
        /// </summary>
        /// <param name="amount">The amount to scroll.</param>
        public void Scroll(Vector2 amount)
        {
            _scroll.Scroll(amount);
        }

    #endregion


    #region Private, protected, internal methods

        void _OnLeft(float val)
        {
            if (val > 0.0f && ProfileGUI.IsInitialized)
                Profile--;
        }

        void _OnRight(float val)
        {
            if (val > 0.0f && ProfileGUI.IsInitialized)
                Profile++;
        }

        void _OnStart(float val)
        {
            if (val > 0.0f && ProfileGUI.IsInitialized && Profiler.IsInitialized)
                Profiler.Instance._FileDump(_profile);
        }

    #endregion


    #region Private, protected, internal fields

        private GUIControl _root;
        private GUIScroll _scroll;
        private GUIMLText _text;
        private GUIText _page;
        private GUIText _saveToFile;

        private int _profile;

    #endregion
    }
#endif // TORQUE_PROFILE
}
