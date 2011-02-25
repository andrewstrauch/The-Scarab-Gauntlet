//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;



namespace GarageGames.Torque.Core
{
    /// <summary>
    /// Called whenever the associated TorqueEvent triggers.
    /// </summary>
    /// <typeparam name="T">Type of data payload.</typeparam>
    /// <param name="eventName">The name of the associated TorqueEvent.</param>
    /// <param name="data">The data payload.</param>
    public delegate void TorqueEventDelegate<T>(String eventName, T data);



    /// <summary>
    /// Interface to read/write event data to/from a stream.
    /// </summary>
    interface IEventReadWrite
    {
        void WriteEventData(BinaryWriter writer);
        object ReadEventData(BinaryReader reader);
    }



    /// <summary>
    /// A TorqueEvent which has a data payload of type T.
    /// </summary>
    /// <typeparam name="T">Type of data payload.</typeparam>
    public class TorqueEvent<T> : TorqueEventManager.TorqueEventBase
    {
        #region Constructors

        public TorqueEvent() : base(String.Empty) { }



        public TorqueEvent(String name) : base(name) { }



        public TorqueEvent(String name, bool doJournal) : base(name, doJournal) { }

        #endregion


        #region Public properties, operators, constants, and enums

        /// <summary>
        /// The value of the event.
        /// </summary>
        public T Value
        {
            get { return _value; }
        }

        #endregion


        #region Private, protected, internal methods

        internal TorqueEvent<T> _Create(T data)
        {
            TorqueEvent<T> val = Util.ObjectPooler.CreateObject<TorqueEvent<T>>();
            val._eventName = Name;
            val._value = data;
            return val;
        }



        internal override void _Trigger(Delegate d)
        {
            if (d == null)
                return;

            TorqueEventDelegate<T> trigger = d as TorqueEventDelegate<T>;
            trigger(Name, _value);
        }



        internal override void _WriteEventData(BinaryWriter writer)
        {
            // we assume T is either a primitive or implements IEventReadWrite
            // Note: this is a quick and dirty persist scheme.  We might refine
            // it later to be more general but we might not.  We use this rather
            // than the built in .Net serialization methods due to concerns over
            // size of the resulting output.
            if (_value is IEventReadWrite)
            {
                (_value as IEventReadWrite).WriteEventData(writer);
            }
            else if (typeof(T) == typeof(int))
            {
                writer.Write(Convert.ToInt32(_value));
            }
            else if (typeof(T) == typeof(float))
            {
                writer.Write(Convert.ToSingle(_value));
            }
            else if (typeof(T) == typeof(bool))
            {
                writer.Write(Convert.ToBoolean(_value));
            }
            else
            {
                Assert.Fatal(false, "TorqueEvent._WriteEventData - Event data type did not implement IEventReadWrite.");
            }
        }



        internal override void _ReadEventData(BinaryReader reader)
        {
            // we assume T is either a primitive or implements IEventReadWrite
            // Note: this is a quick and dirty persist scheme.  We might refine
            // it later to be more general but we might not.  We use this rather
            // than the built in .Net serialization methods due to concerns over
            // size of the resulting output.
            if (_value is IEventReadWrite)
            {
                _value = (T)(_value as IEventReadWrite).ReadEventData(reader);
            }
            else if (typeof(T) == typeof(int))
            {
                _value = (T)(object)reader.ReadInt32();
            }
            else if (typeof(T) == typeof(float))
            {
                _value = (T)(object)reader.ReadSingle();
            }
            else if (typeof(T) == typeof(bool))
            {
                _value = (T)(object)reader.ReadBoolean();
            }
            else
            {
                Assert.Fatal(false, "TorqueEvent._WriteEventData - Event data type did not implement IEventReadWrite.");
            }
        }

        #endregion


        #region Private, protected, internal fields

        T _value;

        #endregion
    }



    /// <summary>
    /// TorqueEventManager is the central hub for event processing.  Unlike .NET
    /// events TorqueEvents must be triggered via the event manager.  Like .NET events
    /// TorqueEvents can be subscribed to (see ListenEvents).  When posting an event
    /// it can either be processed immediately (TriggerEvent) or added to the
    /// queue for later processing (PostEvent).
    /// </summary>
    public class TorqueEventManager
    {
        class ConsumerNode
        {
            #region Static methods, fields, constructors

            public static ConsumerNode Alloc()
            {
                if (_pool == null)
                    return new ConsumerNode();
                ConsumerNode node = _pool;
                _pool = _pool._next;
                return node;
            }



            public static void Dealloc(ConsumerNode node)
            {
                node._consumer = null;
                node._next = _pool;
                _pool = node;
            }

            #endregion


            #region Private, protected, internal fields

            internal Delegate _consumer;
            internal ConsumerNode _next;
            internal object _key;

            static ConsumerNode _pool;

            #endregion
        }



        /// <summary>
        /// Base class providing name and journaling facilities for Torque driven events.
        /// See TorqueEvent and TorqueEventManager.
        /// </summary>
        public class TorqueEventBase
        {
            #region Constructors

            public TorqueEventBase(String name)
            {
                Assert.Fatal(name != null, "TorqueEventBase Constructor - Null event name not allowed.");

                if (name == null)
                    name = "nullEvent";

                _eventName = name;
            }



            public TorqueEventBase(String name, bool doJournal)
                : this(name)
            {
                _doJournal = doJournal;
            }

            #endregion


            #region Public properties, operators, constants, and enums

            public String Name
            {
                get { return _eventName; }
                internal set { _eventName = value; }
            }



            public bool DoJournal
            {
                get { return _doJournal; }
                set { _doJournal = value; }
            }

            #endregion


            #region Private, protected, internal methods

            internal virtual void _Trigger(Delegate d) { }
            internal virtual void _WriteEventData(BinaryWriter writer) { }
            internal virtual void _ReadEventData(BinaryReader reader) { }

            #endregion


            #region Private, protected, internal fields

            protected String _eventName;
            bool _doJournal = true;

            #endregion
        }


        #region Static methods, fields, constructors

        /// <summary>
        /// Returns the instance of the TorqueEventManager. Only one TorqueEventManager
        /// will exist.
        /// </summary>
        static public TorqueEventManager Instance
        {
            get
            {
                if (_instance == null)
                    // this is one way of doing singletons
                    // protected constructor and create on first access
                    _instance = new TorqueEventManager();

                return _instance;
            }
        }



        /// <summary>
        /// Post an event to the event queue.
        /// </summary>
        /// <typeparam name="T">Type of data payload.</typeparam>
        /// <param name="ke">Event to fire.</param>
        /// <param name="data">Data payload.</param>
        static public void PostEvent<T>(TorqueEvent<T> ke, T data)
        {
            Instance.MgrPostEvent(ke, data);
        }



        /// <summary>
        /// Processes all events queued up since last ProcessEvents call.
        /// </summary>
        /// <returns>True if any events were processed.</returns>
        static public bool ProcessEvents()
        {
            return Instance.MgrProcessEvents();
        }



        /// <summary>
        /// Immediately fire an event without putting it in the event queue.
        /// </summary>
        /// <typeparam name="T">Type of data payload.</typeparam>
        /// <param name="ke">Event to trigger.</param>
        /// <param name="data">Data payload.</param>
        static public void TriggerEvent<T>(TorqueEvent<T> ke, T data)
        {
            Instance.MgrTriggerEvent(ke, data);
        }



        /// <summary>
        /// Register to receive callbacks to a particular event.
        /// </summary>
        /// <typeparam name="T">Type of data payload.</typeparam>
        /// <param name="listenToThese">Event to listen to.</param>
        /// <param name="withThisDelegate">Delegate to register.</param>
        /// <param name="key">Key to later use for removing delegate.</param>
        static public void ListenEvents<T>(TorqueEvent<T> listenToThese, TorqueEventDelegate<T> withThisDelegate, object key)
        {
            Instance.MgrListenEvents(listenToThese, withThisDelegate, key);
        }



        /// <summary>
        /// Register to receive callbacks to a particular event.
        /// </summary>
        /// <typeparam name="T">Type of data payload.</typeparam>
        /// <param name="listenToThese">Event to listen to.</param>
        /// <param name="withThisDelegate">Delegate to register.</param>
        static public void ListenEvents<T>(TorqueEvent<T> listenToThese, TorqueEventDelegate<T> withThisDelegate)
        {
            Instance.MgrListenEvents(listenToThese, withThisDelegate, null);
        }



        /// <summary>
        /// Stop callbacks to passed delegate and given key.
        /// </summary>
        /// <typeparam name="T">Type of data payload.</typeparam>
        /// <param name="silenceThese">Event to stop listening to.</param>
        /// <param name="forThisDelegate">Delegate to unregister.</param>
        /// <param name="key">Key to match.</param>
        static public void SilenceEvents<T>(TorqueEvent<T> silenceThese, TorqueEventDelegate<T> forThisDelegate, object key)
        {
            Instance.MgrSilenceEvents(silenceThese, forThisDelegate, key);
        }



        /// <summary>
        /// Stop callbacks to passed delegate and given key.
        /// </summary>
        /// <typeparam name="T">Type of data payload.</typeparam>
        /// <param name="silenceThese">Event to stop listening to.</param>
        /// <param name="forThisDelegate">Delegate to unregister.</param>
        static public void SilenceEvents<T>(TorqueEvent<T> silenceThese, TorqueEventDelegate<T> forThisDelegate)
        {
            Instance.MgrSilenceEvents(silenceThese, forThisDelegate, null);
        }

        #endregion


        #region Constructors

        protected TorqueEventManager()
        {
            Assert.Fatal(_instance == null, "TorqueEventManager Constructor - KernelEventManger already exists.");
            _instance = this;
        }

        #endregion


        #region Public properties, operators, constants, and enums

        /// <summary>
        /// The event journal.
        /// </summary>
        public TorqueJournal Journal
        {
            get { return _journal; }
            set { _journal = value; }
        }



        /// <summary>
        /// Whether the event journal is reading events from a file.
        /// </summary>
        public bool IsReadingJournal
        {
            get { return _journal != null && _journal.IsReading; }
        }



        /// <summary>
        /// Whether the event journal is writing events to a file.
        /// </summary>
        public bool IsWritingJournal
        {
            get { return _journal != null && _journal.IsWriting; }
        }

        #endregion


        #region Public methods

        /// <summary>
        /// Post an event to the event queue.
        /// </summary>
        /// <typeparam name="T">Type of data payload.</typeparam>
        /// <param name="ke">Event to fire.</param>
        /// <param name="data">Data payload.</param>
        public void MgrPostEvent<T>(TorqueEvent<T> ke, T data)
        {
            // would lock here...
            _events.Add(ke._Create(data));
            // would unlock here...
        }



        /// <summary>
        /// Processes all events queued up since last ProcessEvents call.
        /// </summary>
        /// <returns>True if any events were processed.</returns>
        public bool MgrProcessEvents()
        {
            //Stop events from processing recursively.
            if (_inProcessEvents)
                return false;

            if (_events.Count == 0)
                // processed no events
                return false;

            _inProcessEvents = true;

            // double buffer so new events are processed next time (and multi-threading can minimize lock time)

            // would lock here...
            List<TorqueEventBase> current = _events;
            _events = _nextEvents;
            _nextEvents = current;
            // would unlock here...

            foreach (TorqueEventBase ke in current)
            {
                _TriggerEvent(ke);

                // done with event, so get rid of it
                Util.ObjectPooler.RecycleObject(ke);
            }

            current.Clear();

            // processed some events
            _inProcessEvents = false;

            return true;
        }



        /// <summary>
        /// Immediately fire an event without putting it in the event queue.
        /// </summary>
        /// <typeparam name="T">Type of data payload.</typeparam>
        /// <param name="ke">Event to trigger.</param>
        /// <param name="data">Data payload.</param>
        public void MgrTriggerEvent<T>(TorqueEvent<T> ke, T data)
        {
            ke = ke._Create(data);
            _TriggerEvent(ke);
            // created event above so recycle it here
            Util.ObjectPooler.RecycleObject(ke);
        }



        /// <summary>
        /// Register to receive callbacks to a particular event.
        /// </summary>
        /// <typeparam name="T">Type of data payload.</typeparam>
        /// <param name="listenToThese">Event to listen to.</param>
        /// <param name="withThisDelegate">Delegate to register.</param>
        /// <param name="key">Key to later use for removing delegate.</param>
        public void MgrListenEvents<T>(TorqueEvent<T> listenToThese, TorqueEventDelegate<T> withThisDelegate, object key)
        {
            Assert.Fatal(listenToThese != null, "TorqueEventManager.MgrListenEvents - ListenTo event is null.");
            ConsumerNode exists;
            ConsumerNode node = ConsumerNode.Alloc();
            node._consumer = withThisDelegate;
            node._key = key;

            if (_eventConsumers.TryGetValue(listenToThese.Name, out exists))
                node._next = exists;

            _eventConsumers[listenToThese.Name] = node;
        }



        /// <summary>
        /// Stop callbacks to passed delegate and given key.
        /// </summary>
        /// <typeparam name="T">Type of data payload.</typeparam>
        /// <param name="silenceThese">Event to stop listening to.</param>
        /// <param name="forThisDelegate">Delegate to unregister.</param>
        /// <param name="key">Key to match.</param>
        public void MgrSilenceEvents<T>(TorqueEvent<T> silenceThese, TorqueEventDelegate<T> forThisDelegate, object key)
        {
            ConsumerNode exists;

            if (_eventConsumers.TryGetValue(silenceThese.Name, out exists))
            {
                ConsumerNode toss = null;

                if (exists._consumer == (Delegate)forThisDelegate)
                {
                    _eventConsumers[silenceThese.Name] = exists._next;
                    toss = exists;
                }
                else
                {
                    while (exists._next != null)
                    {
                        if (exists._next._consumer == (Delegate)forThisDelegate && exists._next._key == key)
                        {
                            toss = exists._next;
                            exists._next = toss._next;
                            break;
                        }

                        exists = exists._next;
                    }
                }

                if (toss != null)
                {
                    toss._next = null;
                    toss._consumer = null;
                    toss._key = null;
                    ConsumerNode.Dealloc(toss);
                }
            }
        }



        /// <summary>
        /// Mark the end of a block of journaled events.
        /// </summary>
        public void MarkJournalEventBlockEnd()
        {
            if (IsWritingJournal)
                _journal.Writer.Write((int)-1);
        }



        /// <summary>
        /// Read an event from the journal and post it immediately.  If the end of a block of events is reached
        /// the method returns false, otherwise it returns true.
        /// </summary>
        /// <returns>True if there was an event to post, false otherwise.</returns>
        public bool PostJournaledEvent()
        {
            Assert.Fatal(IsReadingJournal, "TorqueEventManager.PostJournalEvent - Cannot post journaled event if not reading journal.");

            TorqueEventBase ev = _journal.ReadEvent();

            if (ev == null)
                // reached mark
                return false;

            ev.DoJournal = false; // this forces the event to be triggered
            _TriggerEvent(ev);

            return true;
        }

        #endregion


        #region Private, protected, internal methods

        private void _TriggerEvent(TorqueEventBase ev)
        {
            if (_journal != null && ev.DoJournal)
            {
                if (_journal.IsReading)
                {
                    return;
                }
                else if (_journal.IsWriting)
                {
                    _journal.WriteEvent(ev);
                }
            }

            ConsumerNode node;

            if (_eventConsumers.TryGetValue(ev.Name, out node))
            {
                while (node != null)
                {
                    if (node._consumer != null)
                        ev._Trigger(node._consumer);

                    node = node._next;
                }
            }
        }

        #endregion


        #region Private, protected, internal fields

        List<TorqueEventBase> _events = new List<TorqueEventBase>();
        List<TorqueEventBase> _nextEvents = new List<TorqueEventBase>();
        Dictionary<String, ConsumerNode> _eventConsumers = new Dictionary<string, ConsumerNode>();
        TorqueJournal _journal;
        bool _inProcessEvents;

        static TorqueEventManager _instance;

        #endregion
    }
}
