//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using GarageGames.Torque.Core;
using GarageGames.Torque.Util;
using GarageGames.Torque.MathUtil;



namespace GarageGames.Torque.T2D
{
    /// <summary>
    /// IKeyGraph Interface.  This interface is used to provide all properties/methods
    /// required to administer a KeyGraph interface.
    /// </summary>
    public interface IKeyGraph
    {
        #region properties

        float ValueScale { get; set; }
        float DefaultValue { get; set; }
        int Count { get; }
        float LastTime { get; }
        float LastValue { get; }
        float this[float time] { get; }

        #endregion


        #region methods

        void ResetKeys();
        void AddPrimaryKey(float value);
        void AddEdgeKey(float time, float value);
        void AddKey(float time, float value);
        void RemoveKey(int keyIndex);
        void SetKeyValue(int keyIndex, float value);
        float GetKeyValue(int keyIndex);
        float GetKeyTime(int keyIndex);

        #endregion
    }



    /// <summary>
    /// Graph Key.
    /// </summary>
    /// <remarks>
    /// We implement the "IComparable" interface so that we can implicitly sort by key-time.
    /// </remarks>
    public class T2DGraphKey : IComparable<T2DGraphKey>
    {
        #region Constructors

        /// <summary>
        /// For use by XML deserializer.
        /// </summary>
        public T2DGraphKey()
        {
        }



        // Constructor.
        public T2DGraphKey(float time, float value)
        {
            _time = time;
            _value = value;
        }

        #endregion


        #region Public properties, operators, constants, and enums

        public float Time
        {
            get { return _time; }
            set { _time = value; }
        }


        public float Value
        {
            get { return _value; }
            set { _value = value; }
        }

        #endregion


        #region Public methods

        // IComparable Interface.
        public int CompareTo(T2DGraphKey other)
        {
            float refTime = other.Time;

            if (_time < refTime)
                return -1;
            else if (_time > refTime)
                return 1;
            else
                return 0;
        }



        // Set Time/Value Helper.
        public void Set(float time, float value)
        {
            Time = time;
            Value = value;
        }

        #endregion


        #region Private, protected, internal fields

        private float _time;
        private float _value;

        #endregion
    }



    /// <summary>
    /// Key-Graph class responsible for storing a sequence of Keys
    /// which are used to drive particle-emitter/effect fields.
    /// </summary>
    public class T2DKeyGraph : IKeyGraph, IDisposable
    {
        #region Static methods, fields, constructors

        static internal readonly float _defaultMaxTime = 10000.0f;

        #endregion


        #region Constructors

        public T2DKeyGraph(float maxTime, float defaultValue)
        {
            // Set Defaults.
            _maxTime = maxTime;
            _defaultValue = defaultValue;
            // Add primary Key.
            mKeyList.Add(new T2DGraphKey(0.0f, _defaultValue));
        }

        #endregion


        #region Public properties, operators, constants, and enums

        /// <summary>
        /// Scale all values retrieved by this.
        /// </summary>
        public float ValueScale
        {
            get { return _valueScale; }
            set
            {
                // Sanity!
                Assert.Fatal(_valueScale > 0.0f, "ValueScale is out of bounds!");

                _valueScale = value;
            }
        }



        /// <summary>
        /// Value of key graph if no keyframes added.
        /// </summary>
        public float DefaultValue
        {
            get { return _defaultValue; }
            set { _defaultValue = value; }
        }



        /// <summary>
        /// Number of keyframes added to this key graph.
        /// </summary>
        [XmlIgnore]
        public int Count
        {
            get
            {
                // Return Key Count.
                return mKeyList.Count;
            }
        }



        /// <summary>
        /// Time of last keyframe added to key graph.
        /// </summary>
        [XmlIgnore]
        public float LastTime
        {
            get
            {
                // Sanity.
                Assert.Fatal(Count > 0, "Graph should ALWAYS contain at least one key!");

                // Return Last Time.
                return mKeyList[mKeyList.Count - 1].Time;
            }
        }



        /// <summary>
        /// Value of last keyframe added to key graph.
        /// </summary>
        [XmlIgnore]
        public float LastValue
        {
            get
            {
                // Sanity.
                Assert.Fatal(Count > 0, "Graph should ALWAYS contain at least one key!");

                // Return Last Value.
                return mKeyList[mKeyList.Count - 1].Value;
            }
        }



        /// <summary>
        /// Value of key graph at specified time.  Time is clamped between 0
        /// and LastTime.
        /// </summary>
        /// <param name="time">Time to lookup value.</param>
        /// <returns>Value of key graph at specified time.</returns>
        [XmlIgnore]
        public float this[float time]
        {
            get
            {
                // Sanity!
                Assert.Fatal(time >= 0.0f, "Invalid key-graph time; must by >0!");

                // Are we using zero-time or we've only one key?
                if (mKeyList.Count < 2 || (time < Epsilon.Value))
                {
                    // Yes, so simply use first entry.
                    return mKeyList[0].Value * _valueScale;
                }

                // Are we on/past the last key time?
                if (time >= LastTime)
                {
                    // Yes, so return last value.
                    return mKeyList[mKeyList.Count - 1].Value * _valueScale;
                }

                // Fetch Key Count.
                int count = Count, index1, index2;
                float refTime = 0.0f;
                for (index1 = 0; index1 < count; ++index1)
                {
                    // Fetch Key Reference Time.
                    refTime = mKeyList[index1].Time;

                    // Are we after the current time?
                    if (refTime > time)
                    {
                        // Yes, so stop searching.
                        break;
                    }

                    // Are we exactly on the current time?
                    if (Epsilon.FloatIsZero(refTime - time))
                    {
                        // Yes, so return this key value.
                        return mKeyList[index1].Value * _valueScale;
                    }
                }

                // Set Adjacent Index.
                index2 = index1--;

                // Fetch Index Times.
                float time1 = mKeyList[index1].Time;
                float time2 = refTime;

                // Calculate Time Differential.
                float timeDelta = (time - time1) / (time2 - time1);

                // Return Lerped Value.
                return ((mKeyList[index1].Value * (1.0f - timeDelta)) + (mKeyList[index2].Value * timeDelta)) * _valueScale;
            }
        }



        /// <summary>
        /// Value of the very first key.
        /// </summary>
        public float PrimaryKey
        {
            get { return GetKeyValue(0); }
            set { AddPrimaryKey(value); }
        }



        /// <summary>
        ///  Internal Key List for deserialization.
        /// </summary>
        public List<T2DGraphKey> Keys
        {
            get { return null; }
            set
            {
                if (value != null)
                {
                    foreach (T2DGraphKey k in value)
                        AddKey(k.Time, k.Value);
                }
            }

        }

        #endregion


        #region Public methods

        /// <summary>
        /// Clear keyframe list and set to default value.
        /// </summary>
        public void ResetKeys()
        {
            // Clear Key List.
            mKeyList.Clear();

            // Add Default-Value Key.
            AddKey(0.0f, _defaultValue);
        }



        /// <summary>
        /// Change value of first keyframe.
        /// </summary>
        /// <param name="value"></param>
        public void AddPrimaryKey(float value)
        {
            // Simply Set Key value at first index.
            SetKeyValue(0, value);
        }



        public void AddEdgeKey(float time, float value)
        {
            // Calculate Preceed time.
            // NOTE:-   This time is twice the 'epsilon' value used as a minimum
            //          seperation between keys.
            float preceedTime = time - (Epsilon.Value * 2.0f);

            // Add Edge value.
            AddKey(preceedTime, this[preceedTime]);

            // Add Transition Value.
            AddKey(time, value);
        }



        public void AddKey(float time, float value)
        {
            // Sanity!
            Assert.Fatal(time >= 0.0f && time <= _maxTime, "Time is out of bounds!");

            // Search for an existing key/time.
            T2DGraphKey key = mKeyList.Find(delegate(T2DGraphKey searchKey) { return Epsilon.FloatIsZero(searchKey.Time - time); });

            // Did we find the key?
            if (key != null)
            {
                // Yes, so set this key.
                key.Value = value;
            }
            else
            {
                // Add new Key.
                mKeyList.Add(new T2DGraphKey(time, value));

                // Sort Key List by Time.
                mKeyList.Sort();
            }
        }



        public void RemoveKey(int keyIndex)
        {
            // Check Key Index.
            CheckKeyIndex(keyIndex);

            // Remove Key.
            mKeyList.RemoveAt(keyIndex);
        }



        public void SetKeyValue(int keyIndex, float value)
        {
            // Check Key Index.
            CheckKeyIndex(keyIndex);

            // Set Key Value.
            mKeyList[keyIndex].Value = value;
        }



        public float GetKeyValue(int keyIndex)
        {
            // Check Key Index.
            CheckKeyIndex(keyIndex);

            // Return Key-Frame.
            return mKeyList[keyIndex].Value;
        }



        public float GetKeyTime(int keyIndex)
        {
            // Check Key Index.
            CheckKeyIndex(keyIndex);

            // Return Key-Frame.
            return mKeyList[keyIndex].Time;
        }



        /// <summary>
        /// Static helper method to calculate value given base and variation key graphs.
        /// Formula for value is: 
        /// value = base + GetRandom(-0.5*varition,0.5*variation).
        /// </summary>
        /// <param name="baseGraph">Base key graph.</param>
        /// <param name="variationGraph">Variation key graph.</param>
        /// <param name="effectAge">Time used to index key graphs.</param>
        /// <returns>Computed value.</returns>
        public static float CalcGraphBV(T2DKeyGraph baseGraph, T2DKeyGraph variationGraph, float effectAge)
        {
            // Fetch Variation Component.
            float varValue = variationGraph[effectAge] * 0.5f;
            // Fast Random Value.
            return baseGraph[effectAge] + TorqueUtil.GetFastRandomFloat(-varValue, varValue);
        }



        /// <summary>
        /// Static helper method to calculate value given base and variation key graphs along
        /// with effect scaling graph.  Formula for value is:
        /// value = base + scale * GetRangom(-0.5*variation,0.5*variation).
        /// </summary>
        /// <param name="baseGraph">Base key graph.</param>
        /// <param name="variationGraph">Variation key graph.</param>
        /// <param name="effectGraph">Effect key graph which scales emitter variation.</param>
        /// <param name="effectAge">Time used to index key graphs.</param>
        /// <returns>Computed value.</returns>
        public static float CalcGraphBVE(T2DKeyGraph baseGraph, T2DKeyGraph variationGraph, T2DKeyGraph effectGraph, float effectAge)
        {
            // Fetch Variation Component.
            float varValue = variationGraph[effectAge] * 0.5f;

            // Fast Random Value.
            return baseGraph[effectAge] + (TorqueUtil.GetFastRandomFloat(-varValue, varValue) * effectGraph[effectAge]);
        }

        #endregion


        #region Private, protected, internal methods

        // Check Key Index.
        private void CheckKeyIndex(int keyIndex)
        {
            // Sanity.
            Assert.Fatal(keyIndex >= 0 && keyIndex < Count, "Key Index out of bounds!");
        }

        #endregion


        #region Private, protected, internal fields

        // Key List.
        private List<T2DGraphKey> mKeyList = new List<T2DGraphKey>();

        // Graph Name.
        private String _name = String.Empty;

        // Value Scaler.
        private float _valueScale = 1.0f;

        // Graph Bounds.
        private float _maxTime;
        private float _defaultValue;

        #endregion

        #region IDisposable Members

        public virtual void Dispose()
        {
            mKeyList.Clear();
            mKeyList = null;
        }

        #endregion
    }



    /// <summary>
    /// Base Key-Graph Helper.
    /// </summary>
    public class T2DKeyGraph_Base : IDisposable
    {

        #region Constructors

        public T2DKeyGraph_Base(float baseDefaultValue)
        {
            // Create Base Graph.
            _keyGraphBase = new T2DKeyGraph(T2DKeyGraph._defaultMaxTime, baseDefaultValue);
        }

        #endregion


        #region Public properties, operators, constants, and enums

        // Base Property.
        public T2DKeyGraph Base
        {
            get { return _keyGraphBase; }
        }

        #endregion


        #region Private, protected, internal fields

        private T2DKeyGraph _keyGraphBase;

        #endregion

        #region IDisposable Members

        public virtual void Dispose()
        {
            _keyGraphBase = null;
        }

        #endregion
    }



    /// <summary>
    /// Scale Key-Graph Helper.
    /// </summary>
    public class T2DKeyGraph_Scale : IDisposable
    {

        #region Constructors

        public T2DKeyGraph_Scale()
        {
            // Create Scale Graph.
            _keyGraphScale = new T2DKeyGraph(1.0f, 1.0f);
        }

        #endregion


        #region Public properties, operators, constants, and enums

        // Scale Property.
        public T2DKeyGraph Scale
        {
            get { return _keyGraphScale; }
        }

        #endregion


        #region Private, protected, internal fields

        private T2DKeyGraph _keyGraphScale;

        #endregion

        #region IDisposable Members

        public virtual void Dispose()
        {
            _keyGraphScale = null;
        }

        #endregion
    }



    /// <summary>
    /// Life Key-Graph Helper.
    /// </summary>
    public class T2DKeyGraph_Life : IDisposable
    {

        #region Constructors

        public T2DKeyGraph_Life()
        {
            // Create Life Graph.
            _keyGraphLife = new T2DKeyGraph(1.0f, 1.0f);
        }

        #endregion


        #region Public properties, operators, constants, and enums

        // Life Property.
        public T2DKeyGraph Life
        {
            get { return _keyGraphLife; }
            internal set { _keyGraphLife = value; }
        }

        #endregion


        #region Private, protected, internal fields

        private T2DKeyGraph _keyGraphLife;

        #endregion

        #region IDisposable Members

        public virtual void Dispose()
        {
            _keyGraphLife = null;
        }

        #endregion
    }



    /// <summary>
    /// Base/Variation Key-Graph Helper.
    /// </summary>
    public class T2DKeyGraph_BaseVariation : IDisposable
    {

        #region Constructors

        public T2DKeyGraph_BaseVariation(float baseDefaultValue)
        {
            // Create Base Graph.
            _keyGraphBase = new T2DKeyGraph(T2DKeyGraph._defaultMaxTime, baseDefaultValue);
            // Create Variation Graph.
            _keyGraphVariation = new T2DKeyGraph(T2DKeyGraph._defaultMaxTime, 0.0f);
        }

        #endregion


        #region Public properties, operators, constants, and enums

        // Base Property.
        public T2DKeyGraph Base
        {
            get { return _keyGraphBase; }
            internal set { _keyGraphBase = value; }
        }



        // Variation Property.
        public T2DKeyGraph Variation
        {
            get { return _keyGraphVariation; }
            internal set { _keyGraphVariation = value; }
        }

        #endregion


        #region Private, protected, internal fields

        private T2DKeyGraph _keyGraphBase;
        private T2DKeyGraph _keyGraphVariation;

        #endregion

        #region IDisposable Members

        public virtual void Dispose()
        {
            _keyGraphBase = null;
            _keyGraphVariation = null;
        }

        #endregion
    }



    /// <summary>
    /// Base/Variation/Life Key-Graph Helper.
    /// </summary>
    public class T2DKeyGraph_BaseVariationLife : IDisposable
    {

        #region Constructors

        public T2DKeyGraph_BaseVariationLife(float baseDefaultValue)
        {
            // Create Base Graph.
            _keyGraphBase = new T2DKeyGraph(T2DKeyGraph._defaultMaxTime, baseDefaultValue);
            // Create Variation Graph.
            _keyGraphVariation = new T2DKeyGraph(T2DKeyGraph._defaultMaxTime, 0.0f);
            // Create Life Graph.
            _keyGraphLife = new T2DKeyGraph(1.0f, 1.0f);
        }

        #endregion


        #region Public properties, operators, constants, and enums

        // Base Property.
        public T2DKeyGraph Base
        {
            get { return _keyGraphBase; }
            internal set { _keyGraphBase = value; }
        }



        // Variation Property.
        public T2DKeyGraph Variation
        {
            get { return _keyGraphVariation; }
            internal set { _keyGraphVariation = value; }
        }



        // Life Property.
        public T2DKeyGraph Life
        {
            get { return _keyGraphLife; }
            internal set { _keyGraphLife = value; }
        }

        #endregion


        #region Private, protected, internal fields

        private T2DKeyGraph _keyGraphBase;
        private T2DKeyGraph _keyGraphVariation;
        private T2DKeyGraph _keyGraphLife;

        #endregion

        #region IDisposable Members

        public virtual void Dispose()
        {
            _keyGraphBase = null;
            _keyGraphLife = null;
            _keyGraphVariation = null;
        }

        #endregion
    }
}
