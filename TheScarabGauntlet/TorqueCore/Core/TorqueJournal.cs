//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using GarageGames.Torque.Util;



namespace GarageGames.Torque.Core
{
    /// <summary>
    /// Torque class for writing sequences of event to disk for later playback.
    /// </summary>
    public class TorqueJournal
    {
        #region Public properties, operators, constants, and enums

        /// <summary>
        /// Enum representing whether journal is reading/writing or inactive.
        /// </summary>
        public enum JournalMode
        {
            None = 0, Play, Record
        }



        /// <summary>
        /// BinaryReader used when reading a journal.  Data can be read from the journal using
        /// this but the data must have been written at a corresponding point in code when writing
        /// the journal.
        /// </summary>
        public BinaryReader Reader
        {
            get { return _reader; }
        }



        /// <summary>
        /// BinaryWriter used when writer a journal.  Data can be written to the journal
        /// using this but the data must then be read back at a synchronized time during playback.
        /// </summary>
        public BinaryWriter Writer
        {
            get { return _writer; }
        }



        /// <summary>
        /// True if journal is in read mode.
        /// </summary>
        public bool IsReading
        {
            get { return _reader != null; }
        }



        /// <summary>
        /// True if journal is in write mode.
        /// </summary>
        public bool IsWriting
        {
            get { return _writer != null; }
        }



        /// <summary>
        /// Current read/write state of journal.
        /// </summary>
        public JournalMode Mode
        {
            get
            {
                if (IsReading)
                    return JournalMode.Play;
                else if (IsWriting)
                    return JournalMode.Record;
                else
                    return JournalMode.None;
            }
        }

        #endregion


        #region Public methods

        /// <summary>
        /// Read a journal from specified file.
        /// </summary>
        /// <param name="filename">Filename of journal to read.</param>
        /// <returns>True if opened with no errors.</returns>
        public bool OpenForRead(String filename)
        {
            if (filename == null || filename == String.Empty)
                // not even trying to be a real file...
                return false;

            try
            {
                _file = new FileStream(filename, FileMode.Open);
            }
            catch
            {
                return false;
            }

            _reader = new BinaryReader(_file);

            // set random seeds to match when journal was written
            int seed1 = _reader.ReadInt32();
            uint seed2 = _reader.ReadUInt32();
            TorqueUtil.SetRandomSeed(seed1);
            TorqueUtil.SetFastRandomSeed(seed2);

            return true;
        }



        /// <summary>
        /// Write a journal to the specified file.
        /// </summary>
        /// <param name="filename">Filename of journal.</param>
        /// <returns>True if opened with no errors.</returns>
        public bool OpenForWrite(String filename)
        {
            if (filename == null || filename == String.Empty)
                // not even trying to be a real file...
                return false;

            _file = new FileStream(filename, FileMode.Create);
            _writer = new BinaryWriter(_file);

            // save random seeds
            int seed = TorqueUtil.GetRandomInt();
            TorqueUtil.SetRandomSeed(seed);
            _writer.Write(seed);
            _writer.Write(TorqueUtil.GetFastRandomSeed());

            return true;
        }



        /// <summary>
        /// Read an event from the journal.  If the end of a block of
        /// events is reached a null event is returned.
        /// </summary>
        /// <returns>Read event.</returns>
        public TorqueEventManager.TorqueEventBase ReadEvent()
        {
            Assert.Fatal(_reader != null, "TorqueJournal.ReadEvent - Journal not open for reading.");

            if (_reader == null)
                return null;

            int id = _reader.ReadInt32();

            if (id < 0)
                return null;

            String eventName;
            Type type;

            if (id < _eventTypes.Count)
            {
                eventName = _eventNames[id];
                type = _eventTypes[id];
            }
            else
            {
                eventName = _reader.ReadString();
                String typeName = _reader.ReadString();
                type = Type.GetType(typeName);
                _eventNames[id] = eventName;
                _eventTypes.Add(type);
            }

            TorqueEventManager.TorqueEventBase ev = (TorqueEventManager.TorqueEventBase)type.GetConstructor(Torque.Util.TorqueUtil.EmptyTypes).Invoke(null);
            ev.Name = eventName;
            ev._ReadEventData(_reader);

            return ev;
        }



        /// <summary>
        /// Write an event to the journal.
        /// </summary>
        /// <param name="ev">Event to write.</param>
        public void WriteEvent(TorqueEventManager.TorqueEventBase ev)
        {
            Assert.Fatal(_writer != null, "TorqueJournal.WriteEvent - Journal not open for writing.");

            if (_writer == null)
                return;

            String eventName = ev.Name;
            int id;

            if (!_eventIds.TryGetValue(eventName, out id))
            {
                id = _eventTypes.Count;
                _eventTypes.Add(ev.GetType());
                _eventIds[eventName] = id;

                _writer.Write(id);
                _writer.Write(eventName);
                _writer.Write(ev.GetType().FullName);
            }
            else
            {
                _writer.Write(id);
            }

            ev._WriteEventData(_writer);
            _writer.Flush();
        }

        #endregion


        #region Private, protected, internal fields

        Dictionary<String, int> _eventIds = new Dictionary<string, int>();
        Dictionary<int, String> _eventNames = new Dictionary<int, string>();
        List<Type> _eventTypes = new List<Type>();

        FileStream _file;
        BinaryWriter _writer;
        BinaryReader _reader;

        #endregion
    }
}