//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using GarageGames.Torque.Core;
using GarageGames.Torque.Util;



namespace GarageGames.Torque.Sim
{
    /// <summary>
    /// Interface for objects that can be added to the process list and ticked.
    /// </summary>
    public interface ITickObject
    {
        #region Public methods

        /// <summary>
        /// Called everytime the engine processes a tick. This is guaranteed to happen at the tick rate, though
        /// it may not happen at the exact tick boundaries.
        /// </summary>
        /// <param name="move">The move structure that contains information about user input.</param>
        /// <param name="dt">The amount of time passed. This is always the tick rate.</param>
        void ProcessTick(Move move, float dt);

        /// <summary>
        /// Called between ticks to interpolate objects, simulating a faster update than the tick rate.
        /// </summary>
        /// <param name="k">The percentage of time between ticks from 0.0 to 1.0.</param>
        void InterpolateTick(float k);

        #endregion
    }



    /// <summary>
    /// Interface for objects that can be added to the process list and animated.
    /// </summary>
    public interface IAnimatedObject
    {
        #region Public methods

        /// <summary>
        /// Called every time through the render loop.
        /// </summary>
        /// <param name="dt">The amount of time that has passed since the last call to this method.</param>
        void UpdateAnimation(float dt);

        #endregion
    }



    /// <summary>
    /// The process list allocates time slices to game objects.  Objects register callbacks for tick processing,
    /// tick interpolation, and updating animation and receive processing time when time is advanced on the process
    /// list.  Tick rate can either be constant (in which case inter-tick time updates result in interpolation between
    /// the last two ticks) or variable (in which case no interpolation is required).  A constant tick rate is chosen by
    /// setting TickMS to the desired tick rate while a variable tick rate is chosen by setting TickMS to zero.
    /// Objects can also set the order they are processed relative to other objects via the SetProcessOrder
    /// method (with some restrictions, see SetProcessOrder for more information on restrictions).  Animation time is
    /// advanced just before rendering.  Anything which does not need constant ticks but just needs time updates each
    /// frame can register for animation callbacks and avoid having to implement interpolation logic.
    /// </summary>
    public class ProcessList
    {
        /// <summary>
        /// Node in the process list.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        struct ProcessCallbackNode<T>
        {

            #region Private, protected, internal fields

            internal int _next;
            internal float _order;

            internal T _callback;

            #endregion
        }

        /// <summary>
        /// An object in the process list.
        /// </summary>
        struct ProcessObject
        {

            #region Static methods, fields, constructors

            // Stuff some flags onto tick index
            static int ObjectBit = 1 << 31;
            static int EnabledBit = 1 << 30;
            static int BeforeBit = 1 << 29;
            static int MarkBit = 1 << 28;
            static int MaxTickIndex = MarkBit - 1;
            static int Mask = ObjectBit | EnabledBit | BeforeBit | MarkBit;

            #endregion


            #region Private, protected, internal fields

            internal bool HasObject
            {
                get { return (_tickIndex & ObjectBit) != 0; }
                set { if (value) _tickIndex |= ObjectBit; else _tickIndex &= ~ObjectBit; }
            }

            internal bool IsDead
            {
                // use internal interface to smart pointer to save time and since we have HasObject test
                get { return HasObject && (_object._ref == null || _object._ref.Ref == null); }
            }

            internal bool Enabled
            {
                get { return (_tickIndex & EnabledBit) != 0; }
                set { if (value) _tickIndex |= EnabledBit; else _tickIndex &= ~EnabledBit; }
            }

            internal bool BeforeSomebody
            {
                get { return (_tickIndex & BeforeBit) != 0; }
                set { if (value) _tickIndex |= BeforeBit; else _tickIndex &= ~BeforeBit; }
            }

            internal bool Mark
            {
                get { return (_tickIndex & MarkBit) != 0; }
                set { if (value) _tickIndex |= MarkBit; else _tickIndex &= ~MarkBit; }
            }

            internal int TickIndex
            {
                get
                {
                    // trickery because we share index with some flags...
                    // map MaxTickIndex to -1, which is essentially a null ptr
                    int idx = _tickIndex & ~Mask;
                    return idx != MaxTickIndex ? idx : -1;
                }
                set
                {
                    // trickery because we share index with some flags...
                    // map MaxTickIndex to -1, which is essentially a null ptr
                    _tickIndex &= Mask;
                    if (value == -1)
                        _tickIndex |= MaxTickIndex;
                    else
                        _tickIndex |= value;
                }
            }

            internal int AnimatedIndex
            {
                get { return _animatedIndex; }
                set { _animatedIndex = value; }
            }

            internal int _nextObject;
            internal int _prevObject;
            internal TorqueSafePtr<TorqueObject> _object;
            internal int _afterObject;
            int _tickIndex;
            int _animatedIndex;

            #endregion
        }


        #region Static methods, fields, constructors
        public static ProcessList Instance
        {
            get { return _instance = _instance ?? new ProcessList(); }
        }
        #endregion


        #region Constructors
        public ProcessList()
        {
            TickMS = 32;
            TorqueObjectDatabase.Instance.RegisterCookieType(_processCookieType);
            TorqueObjectDatabase.Instance.RegisterCookieType(_moveManagerCookieType);
        }
        #endregion


        #region Public properties, operators, constants, and enums

        /// <summary>
        /// Duration of a tick in milliseconds.  If TickMS is 0 then variable sized ticks are
        /// used and no interpolation occurs on the process list.
        /// </summary>
        public int TickMS
        {
            get { return _tickMS; }
            set { _tickMS = value; _tickSec = ((float)_tickMS) / 1000.0f; }
        }

        /// <summary>
        /// Duration of a tick in seconds.
        /// </summary>
        public float TickSec
        {
            get { return _tickSec; }
        }

        /// <summary>
        /// If TickMS is non-zero and UseInterpolation is false then game will not interpolate (and will not render between ticks).
        /// </summary>
        public bool UseInterpolation
        {
            get { return _useInterpolation; }
            set { _useInterpolation = value; }
        }

        #endregion


        #region Public methods

        /// <summary>
        /// Get MoveManager associated with TorqueObject.
        /// </summary>
        /// <param name="obj">Object to retrieve MoveManager from.</param>
        /// <returns>MoveManager associated with obj.</returns>
        public MoveManager GetMoveManager(TorqueObject obj)
        {
            TorqueCookie cookie;
            obj.GetCookie(_moveManagerCookieType, out cookie);
            int idx = cookie.Value - 1;
            if (idx >= 0)
            {
                Assert.Fatal(idx < _moveManagers.Count, "Move manager index out of range");
                if (idx < _moveManagers.Count)
                    return _moveManagers[idx];
            }
            return null;
        }

        /// <summary>
        /// Associate a MoveManager with TorqueObject.  If MoveManager already has
        /// an object associated with it this will replace that object.  If TorqueObject
        /// already has a move manager associated with it this will replace that association.
        /// </summary>
        /// <param name="obj">TorqueObject to associate with MoveManager.</param>
        /// <param name="mgr">MoveManager to associate with TorqueObject.</param>
        public void SetMoveManager(TorqueObject obj, MoveManager mgr)
        {
            if (mgr._Consumer == obj)
                // no change
                return;

            if (mgr._Consumer != null)
            {
                // clear consumer
                mgr._Consumer.SetCookie(_moveManagerCookieType, new TorqueCookie());
                mgr._Consumer = null;
            }

            // find move manager in list if already there, add if not
            int idx;
            for (idx = 0; idx < _moveManagers.Count; idx++)
                if (_moveManagers[idx] == mgr)
                    break;
            if (idx == _moveManagers.Count)
                _moveManagers.Add(mgr);

            // associate object with move manager
            obj.SetCookie(_moveManagerCookieType, new TorqueCookie(idx + 1));
            mgr._Consumer = obj;
            obj._InternalNotched = true;
        }

        /// <summary>
        /// Remove association of MoveManager with current consumer.
        /// </summary>
        /// <param name="moveManager">MoveManager to clear.</param>
        public void ClearMoveManager(MoveManager moveManager)
        {
            Assert.Fatal(moveManager != null, "Null MoveManager");
            if (moveManager == null)
                return;

            // adjust cookies of subsequent move managers
            int idx;
            for (idx = 0; idx < _moveManagers.Count; idx++)
            {
                if (_moveManagers[idx] == moveManager)
                {
                    TorqueObject consumer = _moveManagers[idx]._Consumer;
                    if (consumer != null)
                        consumer.SetCookie(_moveManagerCookieType, new TorqueCookie());

                    for (int i = idx + 1; i < _moveManagers.Count; i++)
                    {
                        consumer = _moveManagers[i]._Consumer;
                        if (consumer != null)
                        {
                            TorqueCookie cookie;
                            bool found = consumer.GetCookie(_moveManagerCookieType, out cookie);
                            Assert.Fatal(found && (cookie.Value - 1 == i), "MoveManager/Consumer mis-match");
                            cookie.Value -= 1;
                            consumer.SetCookie(_moveManagerCookieType, cookie);
                        }
                    }
                    _moveManagers.RemoveAt(idx);
                }
            }
        }

        /// <summary>
        /// Remove association of obj with any MoveManager it might be associated with.
        /// </summary>
        /// <param name="obj">Object to clear.</param>
        public void ClearMoveManager(TorqueObject obj)
        {
            TorqueCookie cookie;
            bool found = obj.GetCookie(_moveManagerCookieType, out cookie);
            if (found)
            {
                int idx = cookie.Value - 1;
                if (idx >= 0 && idx < _moveManagers.Count)
                {
                    Assert.Fatal(_moveManagers[idx]._Consumer == obj, "MoveManager/Consumer mis-match");
                    ClearMoveManager(_moveManagers[idx]);
                }
            }
        }

        /// <summary>
        /// Add a tick callback to a TorqueObject.  When a tick interface is registered it will receive
        /// a ProcessTick call whenever the process list is ticked and an InterpolateTick call whenever
        /// the process list is interpolated (i.e., before rendering).  Any number of callbacks can be 
        /// registered for the same TorqueObject.  The order of the callbacks is determined by the order 
        /// parameter.  If order is not passed then an order of 0.5 is assumed.  By convention, a value of
        /// 0 is used for callbacks that need to be processed first and a value of 1 is used for callbacks 
        /// that need to be called last.  Callbacks for a given object are called in a block, with no
        /// intervening object callbacks.
        /// </summary>
        /// <param name="obj">Object to add a callback to.</param>
        /// <param name="tick">Tick interface which receives callback.</param>
        /// <param name="order">Order value for this callback.</param>
        public void AddTickCallback(TorqueObject obj, ITickObject tick, float order)
        {
            _AddTickCallback(_GetProcessObject(obj), tick, order);
        }

        /// <summary>
        /// Add an animation callback to a TorqueObject.  When an animation interface is registered it
        /// will receive an UpdateAnimation call whenver UpdateAnimation is called on the process list
        /// (generally after interpolation and before rendering).Any number of callbacks can be 
        /// registered for the same TorqueObject.  The order of the callbacks is determined by the order 
        /// parameter.  If order is not passed then an order of 0.5 is assumed.  By convention, a value of
        /// 0 is used for callbacks that need to be processed first and a value of 1 is used for callbacks 
        /// that need to be called last.  Callbacks for a given object are called in a block, with no
        /// intervening object callbacks.
        /// </summary>
        /// <param name="obj">Object to add a callback to.</param>
        /// <param name="advance">Animation interface which receives the callback.</param>
        /// <param name="order">Order value for this callback.</param>
        public void AddAnimationCallback(TorqueObject obj, IAnimatedObject advance, float order)
        {
            _AddAnimationCallback(_GetProcessObject(obj), advance, order);
        }

        #region AddTick/AddAnimationCallback calling interface alternatives

        /// <param name="obj">Object to add a callback to.</param>
        /// <param name="tick">Tick interface which receives callback.</param>
        public void AddTickCallback(TorqueObject obj, ITickObject tick)
        {
            _AddTickCallback(_GetProcessObject(obj), tick, 0.5f);
        }

        /// <param name="obj">Object to add a callback to, assuming object is also an ITickObject (error if not).</param>
        public void AddTickCallback(TorqueObject obj)
        {
            _AddTickCallback(_GetProcessObject(obj), obj as ITickObject, 0.5f);
        }

        /// <param name="obj">Object to add a callback to.</param>
        /// <param name="advance">Animation interface which receives the callback.</param>
        public void AddAnimationCallback(TorqueObject obj, IAnimatedObject advance)
        {
            _AddAnimationCallback(_GetProcessObject(obj), advance, 0.5f);
        }

        /// <param name="obj">Object to add a callback to, assuming object is also a IAnimatedObject (error if not).</param>
        public void AddAnimationCallback(TorqueObject obj)
        {
            _AddAnimationCallback(_GetProcessObject(obj), obj as IAnimatedObject, 0.5f);
        }

        #endregion

        /// <summary>
        /// Remove a TorqueObject from the process list.  Note: TorqueObjects are automatically removed
        /// when they are unregistered, so this method does not normallly need to be called.
        /// </summary>
        /// <param name="obj">Object to remove.</param>
        public void RemoveObject(TorqueObject obj)
        {
            TorqueCookie cookie = _GetProcessObject(obj);
            // mark process object as dead
            _processObjects[cookie.Value]._object.Object = null;
            _processObjects[cookie.Value].HasObject = true;
            obj.SetCookie(_processCookieType, new TorqueCookie(0));
        }

        /// <summary>
        /// Enable or disable TorqueObject in process list.  If disabled none of an objects callbacks
        /// are called.
        /// </summary>
        /// <param name="obj">Object to enable/disable</param>
        /// <param name="value">True to enable the object, false to disable.</param>
        public void SetEnabled(TorqueObject obj, bool enable)
        {
            int idx;
            if (enable)
                idx = _GetProcessObject(obj).Value;
            else
                idx = _FindProcessObject(obj).Value;

            // An index of zero means its invalid.
            if (idx == 0)
                return;

            Assert.Fatal(idx < _nextProcessObject, "Bad process object index");
            _processObjects[idx].Enabled = enable;
        }

        /// <summary>
        /// Get enabled status of an object.
        /// </summary>
        /// <param name="obj">Object whose enabled state is being querried.</param>
        /// <returns>True if enabled, false if disabled.</returns>
        public bool GetEnabled(TorqueObject obj)
        {
            int idx = _FindProcessObject(obj).Value;
            return idx == 0 ? false : _processObjects[idx].Enabled;
        }

        /// <summary>
        /// Set the order in which the callbacks for two objects are called.  Note that an
        /// object can only be processed after one other object, so calling SetProcessOrder(B,A)
        /// followed by SetProcessOrder(C,A) will make the first call irrelevant.  If loops are
        /// introduced into the process order it will fail gracefully (but which SetProcessOrder
        /// call is ineffective will not be predictable).
        /// </summary>
        /// <param name="firstThis">The object whose callbacks will be called first.</param>
        /// <param name="thenThis">The object whose callbacks will be called second.</param>
        public void SetProcessOrder(TorqueObject firstThis, TorqueObject thenThis)
        {
            _SetProcessOrder(_GetProcessObject(firstThis), _GetProcessObject(thenThis));
        }

        /// <summary>
        /// Cancel process ordering for an object.  Note: second object in the ordering
        /// tracks the first and not the other way around, so this will only have an effect
        /// if it was the second parameter in a SetProcessOrder call.
        /// </summary>
        /// <param name="obj">Object to clear process order on.</param>
        public void ClearProcessOrder(TorqueObject obj)
        {
            _ClearProcessOrder(_GetProcessObject(obj));
        }

        /// <summary>
        /// Advance time on the tick list and tick as many times as required to
        /// maintain tick rate.  E.g., if TickMS=30; and we advance time by 100 ms,
        /// the process list will be ticked (at least) 3 times.
        /// </summary>
        /// <param name="floatMS">Time in milliseconds to advance process list.</param>
        /// <returns>True if the list was ticked at least once.</returns>
        public bool AdvanceTick(float floatMS)
        {
#if DEBUG
            Profiler.Instance.StartBlock("ProcessList.AdvanceTick");
#endif
            if (_processListDirty)
                _OrderList();

            if (TickMS > 0)
            {
                // constant tick
                floatMS += _remainderMS;
                int ms = (int)floatMS;
                _remainderMS = floatMS - (float)ms;

                int targetTime = _lastTime + ms;
                int targetTick = targetTime - (targetTime % _tickMS);
                int tickCount = targetTick / _tickMS;
                bool ret = tickCount != _lastTick;

                if (ret)
                {
                    // gonna tick, put to last tick position
                    _tickInterpolation = 1.0f;
                    InterpolateTick();
                }

                // interpolation value for next interpolate tick
                _tickInterpolation = (_remainderMS + (float)(targetTime - targetTick)) / (float)_tickMS;

                for (; _lastTick != tickCount; _lastTick++)
                    _TickObjects(TickSec);

                _lastTime = targetTime;

#if DEBUG
                Profiler.Instance.EndBlock("ProcessList.AdvanceTick");
#endif
                return ret;
            }
            else
            {
                // variable sized tick
                _TickObjects(floatMS / 1000.0f);
                _tickInterpolation = 1.0f;
                floatMS += _remainderMS;
                int ms = (int)floatMS;
                _remainderMS = floatMS - (float)ms;
                _lastTime += ms;

#if DEBUG
                Profiler.Instance.EndBlock("ProcessList.AdvanceTick");
#endif
                return true;
            }
        }

        /// <summary>
        /// Interpolate between the last two ticks based on the amount of time which
        /// has passed.  This method assumes that AdvanceTick was called previously
        /// otherwise the interpolation parameter will not be set properly.
        /// </summary>
        public void InterpolateTick()
        {
            if (TickMS == 0 || !UseInterpolation)
                return;

#if DEBUG
            Profiler.Instance.StartBlock("ProcessList.InterpolateTick");
#endif

            if (_processListDirty)
                _OrderList();

            for (int idx = _processObjects[0]._nextObject; idx != 0; idx = _processObjects[idx]._nextObject)
            {
                // if p.o. is dead, just add to the dead list, otherwise interpolate tick
                if (_processObjects[idx].IsDead)
                {
                    Assert.Fatal(idx != 0, "Process object 0 should never be touched");
                    _deadObjects.Add(idx);
                }
                else if (_processObjects[idx].Enabled)
                {
                    int tickIdx = _processObjects[idx].TickIndex;
                    while (tickIdx != -1)
                    {
                        _tickNodes[tickIdx]._callback.InterpolateTick(_tickInterpolation);
                        if (_processObjects[idx].IsDead)
                        {
                            _deadObjects.Add(idx);
                            break;
                        }
                        tickIdx = _tickNodes[tickIdx]._next;
                    }
                }
            }

            // remove all the dead objects encountered in above loop
            _RemoveDeadObjects();

#if DEBUG
            Profiler.Instance.EndBlock("ProcessList.InterpolateTick");
#endif
        }

        /// <summary>
        /// Advance time on the animation list.  This should be called just before
        /// rendering each frame and should be advanced by the amount of time that has
        /// passed since the last render.
        /// </summary>
        /// <param name="ms">Time to advance animation in millseconds.</param>
        public void UpdateAnimation(float ms)
        {
#if DEBUG
            Profiler.Instance.StartBlock("ProcessList.UpdateAnimation");
#endif
            float dt = (float)ms / 1000.0f;

            if (_processListDirty)
                _OrderList();

            for (int idx = _processObjects[0]._nextObject; idx != 0; idx = _processObjects[idx]._nextObject)
            {
                // if p.o. is dead, just add to the dead list, otherwise update animation
                if (_processObjects[idx].IsDead)
                {
                    Assert.Fatal(idx != 0, "Process object 0 should never be touched");
                    _deadObjects.Add(idx);
                }
                else if (_processObjects[idx].Enabled)
                {
                    int advanceIdx = _processObjects[idx].AnimatedIndex;
                    while (advanceIdx != -1)
                    {
                        _animationNodes[advanceIdx]._callback.UpdateAnimation(dt);
                        if (_processObjects[idx].IsDead)
                        {
                            _deadObjects.Add(idx);
                            break;
                        }
                        advanceIdx = _animationNodes[advanceIdx]._next;
                    }
                }
            }

            // remove all the dead objects encountered in above loop
            _RemoveDeadObjects();

#if DEBUG
            Profiler.Instance.EndBlock("ProcessList.UpdateAnimation");
#endif
        }

        #endregion


        #region Private, protected, internal methods

        void _OrderList()
        {
            // about to do complicated sort...we can afford 
            // walking through process list once to cull out dead objects
            // Note1: iterate over dead and live nodes alike so we don't have
            // to follow links.  Probably faster at least in worst case.
            // Note2: nodes on free list won't be marked as dead because they are
            // zeroed out (and dead requires the has object bit to be set).
            // Note3: while we are at it, clear mark on objects, so we don't have 
            // to waste a dword on a tag, just one bit.
            for (int i = 0; i < _nextProcessObject; i++)
            {
                _processObjects[i].Mark = false;
                if (_processObjects[i].IsDead)
                    _deadObjects.Add(i);
            }
            _RemoveDeadObjects();

            // create temp list, moving all but head over to temp list
            int tempListHead = _NewNode(ref _processObjects, ref _freeProcessNodes, ref _nextProcessObject);
            int first = _processObjects[0]._nextObject;
            int last = _processObjects[0]._prevObject;
            _processObjects[tempListHead]._nextObject = first;
            _processObjects[tempListHead]._prevObject = last;
            _processObjects[last]._nextObject = tempListHead;
            _processObjects[first]._prevObject = tempListHead;
            _processObjects[0]._nextObject = 0;
            _processObjects[0]._prevObject = 0;

            while (_processObjects[tempListHead]._nextObject != tempListHead)
            {
                int nextIdx = _processObjects[tempListHead]._nextObject;
                _processObjects[nextIdx].Mark = true;

                // unlink
                _UnlinkProcessNode(nextIdx);

                // build chain of dependent objects
                int afterIdx = _processObjects[nextIdx]._afterObject;
                while (afterIdx != 0 && !_processObjects[afterIdx].Mark)
                {
                    _processObjects[afterIdx].Mark = true;

                    _UnlinkProcessNode(afterIdx);
                    _InsertAfterProcessNodes(nextIdx, afterIdx);
                    nextIdx = afterIdx;
                    afterIdx = _processObjects[nextIdx]._afterObject;
                }

                // insert list after all objects already added (i.e., before head)
                _InsertAfterProcessNodes(nextIdx, _processObjects[0]._prevObject);
            }

            Assert.Fatal(_processObjects[tempListHead]._prevObject == _processObjects[tempListHead]._nextObject, "Temporary head should be removed by now.");
            _RecycleNode(tempListHead, ref _processObjects, ref _freeProcessNodes);
            _processListDirty = false;
        }

        void _TickObjects(float tickSec)
        {
#if DEBUG
            Profiler.Instance.StartBlock("ProcessList._TickObjects");
#endif
            // Iterate through all the process objects (obj 0 is head & tail and is not a real object)
            for (int idx = _processObjects[0]._nextObject; idx != 0; idx = _processObjects[idx]._nextObject)
            {
                // if p.o. is dead, just add to the dead list, otherwise tick the object
                if (_processObjects[idx].IsDead)
                {
                    Assert.Fatal(idx != 0, "Process object 0 should never be touched");
                    _deadObjects.Add(idx);
                }
                else if (_processObjects[idx].Enabled)
                {
                    // quick access to safe ptr contents -- only valid if we know ptr valid, which we do
                    TorqueObject obj = _processObjects[idx]._object._ref.Ref as TorqueObject;
                    MoveManager moveManager = obj._InternalNotched ? GetMoveManager(obj) : null;
                    Move move = moveManager != null ? moveManager.Update(tickSec) : null;

                    int tickIdx = _processObjects[idx].TickIndex;
                    while (tickIdx != -1)
                    {
                        _tickNodes[tickIdx]._callback.ProcessTick(move, tickSec);
                        if (_processObjects[idx].IsDead)
                        {
                            _deadObjects.Add(idx);
                            break;
                        }
                        tickIdx = _tickNodes[tickIdx]._next;
                    }
                }
            }

            // remove all the dead objects encountered in above loop
            _RemoveDeadObjects();

#if DEBUG
            Profiler.Instance.EndBlock("ProcessList._TickObjects");
#endif
        }

        void _RemoveObject(int idx)
        {
            Assert.Fatal(idx != 0, "Trying to remove head process object");

            if (_processObjects[idx].BeforeSomebody)
            {
                // could walk through process list in order (skipping inactive nodes) but 
                // this is probably quicker, and definitely is in the worst case
                for (int i = 0; i < _nextProcessObject; i++)
                {
                    if (_processObjects[i]._afterObject == idx)
                        _processObjects[i]._afterObject = 0;
                }
                _processObjects[idx].BeforeSomebody = false;
            }

            _UnlinkProcessNode(idx);

            // recycle tick callbacks
            int tickIdx = _processObjects[idx].TickIndex;
            while (tickIdx != -1)
            {
                int nextIdx = _tickNodes[tickIdx]._next;
                _RecycleNode(tickIdx, ref _tickNodes, ref _freeTickNodes);
                tickIdx = nextIdx;
            }

            // recycle advance time callbacks
            int advanceIdx = _processObjects[idx].AnimatedIndex;
            while (advanceIdx != -1)
            {
                int nextIdx = _animationNodes[advanceIdx]._next;
                _RecycleNode(advanceIdx, ref _animationNodes, ref _freeAnimationNodes);
                advanceIdx = nextIdx;
            }

            _RecycleNode(idx, ref _processObjects, ref _freeProcessNodes);
        }

        void _RemoveDeadObjects()
        {
            foreach (int i in _deadObjects)
                _RemoveObject(i);
            _deadObjects.Clear();
            TorqueObjectDatabase.Instance.DeleteMarkedObjects();
        }

        TorqueCookie _FindProcessObject(TorqueObject obj)
        {
            TorqueCookie cookie;
            obj.GetCookie(_processCookieType, out cookie);
            return cookie;
        }

        TorqueCookie _GetProcessObject(TorqueObject obj)
        {
            TorqueCookie cookie;
            if (!obj.GetCookie(_processCookieType, out cookie) || cookie.Value == 0)
            {
                cookie = _AddProcessObject();
                _processObjects[cookie.Value]._object.Object = obj;
                obj.SetCookie(_processCookieType, cookie);
            }

            Assert.Fatal(obj == _processObjects[cookie.Value]._object.Object, "Mis-match between object and cookie");
            return cookie;
        }

        protected TorqueCookie _AddProcessObject()
        {
            int idx = _NewNode(ref _processObjects, ref _freeProcessNodes, ref _nextProcessObject);

            // create new process object at the tail of the process list
            ProcessObject po = new ProcessObject();
            int tail = _processObjects[0]._prevObject;
            po._nextObject = 0; // head
            po._prevObject = tail;
            po.TickIndex = -1;
            po.AnimatedIndex = -1;
            po.Enabled = true;
            po.HasObject = true;
            _processObjects[idx] = po;

            _processObjects[tail]._nextObject = idx;
            _processObjects[0]._prevObject = idx;

            return new TorqueCookie(idx);
        }

        protected void _SetProcessOrder(TorqueCookie firstThis, TorqueCookie thenThis)
        {
            // limitation is that we can only explicitly follow one object
            int poIdx1 = firstThis.Value;
            int poIdx2 = thenThis.Value;
            _processObjects[poIdx1].BeforeSomebody = true;
            _processObjects[poIdx2]._afterObject = poIdx1;
            _processListDirty = true;
        }

        protected void _ClearProcessOrder(TorqueCookie cookie)
        {
            int poIdx = cookie.Value;
            if (poIdx > 0)
                _processObjects[poIdx]._afterObject = 0;
        }

        protected void _AddTickCallback(TorqueCookie cookie, ITickObject tick, float order)
        {
            Assert.Fatal(tick != null, "Callback must not be null");
            int tickIndex = _processObjects[cookie.Value].TickIndex;
            _AddCallback(ref tickIndex, order, tick, ref _tickNodes, ref _freeTickNodes, ref _nextTickNode);
            _processObjects[cookie.Value].TickIndex = tickIndex;
        }

        protected void _AddAnimationCallback(TorqueCookie cookie, IAnimatedObject advance, float order)
        {
            Assert.Fatal(advance != null, "Callback must not be null");
            int advanceIndex = _processObjects[cookie.Value].AnimatedIndex;
            _AddCallback(ref advanceIndex, order, advance, ref _animationNodes, ref _freeAnimationNodes, ref _nextAnimationNode);
            _processObjects[cookie.Value].AnimatedIndex = advanceIndex;
        }

        protected void _SetEnabled(TorqueCookie cookie, bool value)
        {
            _processObjects[cookie.Value].Enabled = value;
        }

        protected bool _GetEnabled(TorqueCookie cookie)
        {
            return _processObjects[cookie.Value].Enabled;
        }

        void _UnlinkProcessNode(int idx)
        {
            int before = _processObjects[idx]._prevObject;
            int after = _processObjects[idx]._nextObject;
            _processObjects[before]._nextObject = after;
            _processObjects[after]._prevObject = before;

            _processObjects[idx]._nextObject = idx;
            _processObjects[idx]._prevObject = idx;
        }

        void _InsertAfterProcessNodes(int insertThese, int head)
        {
            int beforeThese = _processObjects[insertThese]._prevObject;
            int afterHead = _processObjects[head]._nextObject;

            _processObjects[head]._nextObject = insertThese;
            _processObjects[afterHead]._prevObject = beforeThese;

            _processObjects[insertThese]._prevObject = head;
            _processObjects[beforeThese]._nextObject = afterHead;

        }

        #region private list management templates

        int _NewNode<T>(ref T[] nodeList, ref List<int> freeNodes, ref int nextFree)
        {
            int idx;
            if (freeNodes.Count != 0)
            {
                idx = freeNodes[freeNodes.Count - 1];
                freeNodes.RemoveAt(freeNodes.Count - 1);
            }
            else
            {
                if (nodeList.Length <= nextFree)
                    // keep doubling size till we have plenty of room
                    TorqueUtil.ResizeArray(ref nodeList, nodeList.Length * 2);
                idx = nextFree++;
            }
            nodeList[idx] = default(T);
            return idx;
        }

        void _RecycleNode<T>(int nodeIdx, ref T[] nodeList, ref List<int> freeNodes)
        {
            freeNodes.Add(nodeIdx);

            // clear out nodeIdx
            nodeList[nodeIdx] = default(T);
        }

        bool _CheckForCallbackAlready<T>(int headIdx, float order, T callback, ProcessCallbackNode<T>[] callbackList) where T : class
        {
            for (int idx = headIdx; idx != -1; idx = callbackList[idx]._next)
                if (callbackList[idx]._callback == callback)
                {
                    Assert.Warn(callbackList[idx]._callback != callback, "Callback already in " + typeof(T) + " list");
                    return true;
                }
            return false;
        }

        void _AddCallback<T>(ref int headIdx, float order, T callback, ref ProcessCallbackNode<T>[] callbackList, ref List<int> freeNodes, ref int nextFree) where T : class
        {
            // make sure we don't add the same callback twice
            if (_CheckForCallbackAlready(headIdx, order, callback, callbackList))
                return;

            if (headIdx < 0 || order < callbackList[headIdx]._order)
            {
                // start the delegate list
                int newHeadIdx = _NewNode(ref callbackList, ref freeNodes, ref nextFree);
                callbackList[newHeadIdx]._next = headIdx;
                callbackList[newHeadIdx]._callback = callback;
                callbackList[newHeadIdx]._order = order;
                headIdx = newHeadIdx;
                return;
            }

            int idx = headIdx;
            while (true)
            {
                int nextNode;
                if (callbackList[idx]._next == -1)
                {
                    // add a node to the tail, set order and return
                    nextNode = _NewNode(ref callbackList, ref freeNodes, ref nextFree);
                    callbackList[idx]._next = nextNode;

                    callbackList[nextNode]._next = -1;
                    callbackList[nextNode]._order = order;
                    callbackList[nextNode]._callback = callback;

                    return;
                }
                nextNode = callbackList[idx]._next;
                if (order < callbackList[nextNode]._order)
                {
                    // order less than next node, insert before
                    int insertNode = _NewNode(ref callbackList, ref freeNodes, ref nextFree);
                    callbackList[insertNode]._next = nextNode;
                    callbackList[insertNode]._order = order;
                    callbackList[insertNode]._callback = callback;
                    callbackList[idx]._next = insertNode;

                    return;
                }
                idx = nextNode;
            }
        }

        #endregion

        #endregion


        #region Private, protected, internal fields

        int _tickMS;
        float _remainderMS;
        float _tickSec;
        int _lastTime;
        int _lastTick;
        float _tickInterpolation;
        bool _useInterpolation;
        bool _processListDirty;

        ProcessObject[] _processObjects = new ProcessObject[100]; // reasonable start size
        ProcessCallbackNode<ITickObject>[] _tickNodes = new ProcessCallbackNode<ITickObject>[200];
        ProcessCallbackNode<IAnimatedObject>[] _animationNodes = new ProcessCallbackNode<IAnimatedObject>[200];
        List<MoveManager> _moveManagers = new List<MoveManager>();

        int _nextProcessObject = 1; // creates single entry list with head/tail pointing at itself (just what we want)
        int _nextTickNode;
        int _nextAnimationNode;
        List<int> _freeProcessNodes = new List<int>();
        List<int> _freeTickNodes = new List<int>();
        List<int> _freeAnimationNodes = new List<int>();
        List<int> _deadObjects = new List<int>();

        TorqueCookieType _processCookieType = new TorqueCookieType("processList");
        TorqueCookieType _moveManagerCookieType = new TorqueCookieType("moveManager");

        static ProcessList _instance;

        #endregion
    }
}
