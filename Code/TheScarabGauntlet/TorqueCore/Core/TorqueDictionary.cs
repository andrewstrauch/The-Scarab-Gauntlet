//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using GarageGames.Torque.Util;



namespace GarageGames.Torque.Core
{
    /// <summary>
    /// A TorqueDictionary holds values associated with TorqueObjects via different key values.  One
    /// can associate a value with a single String key or with a String key and a secondary key
    /// of an arbitrary type.  Only one value can be associated with a given key and secondary key
    /// at any given time.  If the secondary key is a TorqueBase, then the value will be removed
    /// when references are reset on the secondary key (in the case of TorqueObject, this happens when
    /// a TorqueObject is unregistered).  If the value is a TorqueBase, then it will be removed
    /// when references are reset on the value.  Finally, one can retrieve an iterator to iterate
    /// over all values on an object which share a given key (i.e., same key but different secondary
    /// keys).  The prefered interface for accessing a dictionary is by using the following methods 
    /// on TorqueObject: GetValue, SetValue, RemoveValue, RemoveAllValues, and ItrValues.
    /// </summary>
    public class TorqueDictionary
    {
        /// <summary>
        /// A single record in a TorqueDictionary.
        /// </summary>
        class Record
        {
            #region Public properties, operators, constants, and enums

            /// <summary>
            /// Specifies whether or not this record is valid.
            /// </summary>
            public bool IsValid
            {
                get { return !(_valueReference.Initialized && _valueReference.Object == null) && !(_key2Reference.Initialized && _key2Reference.Object == null); }
            }



            /// <summary>
            /// The object value of this record.
            /// </summary>
            public object Value
            {
                get
                {
                    if (_valueReference.Initialized)
                        return _valueReference.Object;
                    else
                        return _value;
                }
                set
                {
                    TorqueBase tb = value as TorqueBase;
                    if (tb == null)
                    {
                        _value = value;

                        // wipe out old ref value
                        _valueReference = new TorqueSafePtr<TorqueBase>();
                    }
                    else
                        _valueReference.Object = tb;
                }
            }



            /// <summary>
            /// The primary key for this record.
            /// </summary>
            public String Key
            {
                get { return _key; }
                set { _key = value; }
            }



            /// <summary>
            /// The optional secondary key for this record.
            /// </summary>
            public object Key2
            {
                get
                {
                    if (_key2Reference.Initialized)
                        return _key2Reference.Object;
                    else
                        return _key2;
                }
                set
                {
                    TorqueBase tb = value as TorqueBase;
                    if (tb == null)
                    {
                        _key2 = value;
                        // wipe out old ref value
                        _key2Reference = new TorqueSafePtr<TorqueBase>();
                    }
                    else
                        _key2Reference.Object = tb;
                }
            }

            #endregion


            #region Private, protected, internal fields

            String _key;

            object _key2;
            TorqueSafePtr<TorqueBase> _key2Reference;

            object _value;
            TorqueSafePtr<TorqueBase> _valueReference;

            internal Record _nextInObject;
            internal Record _prevInObject;
            internal Record _next;
            internal Record _prev;

            #endregion
        }



        /// <summary>
        /// An enumerator for a TorqueDictionary.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public struct TorqueDictionaryEnumerator<T> : IEnumerator<T>
        {
            #region Constructors

            internal TorqueDictionaryEnumerator(TorqueDictionary dictionary, TorqueObject obj, String key)
            {
                _dictionary = dictionary;
                _key = key;
                _object = obj;
                _current = null;
            }

            #endregion


            #region Public properties, operators, constants, and enums

            public T Current
            {
                get { return _current == null ? default(T) : (T)_current.Value; }
            }



            object System.Collections.IEnumerator.Current
            {
                get { return Current; }
            }

            #endregion


            #region Public methods

            public bool MoveNext()
            {
                if (_current == null)
                    _current = _dictionary._GetFirstRecord(_object, _key);
                else
                    _current = _current._next;

                while (_current != null && (!_current.IsValid || !(_current.Value is T)))
                {
                    Record nextRecord = _current._next;

                    if (!_current.IsValid)
                        _dictionary._RemoveRecord(_object, new CompoundKey<TorqueObject, String>(_object, _key), _current);

                    _current = nextRecord;
                }

                return _current != null;
            }



            public void Reset()
            {
                _current = null;
            }



            public void Dispose()
            {
                this._dictionary._dictionary.Clear();
                this._dictionary = null;
                this._current = null;
                this._key = null;
                this._object = null;
            }



            public IEnumerator<T> GetEnumerator()
            {
                return this;
            }

            #endregion


            #region Private, protected, internal fields

            TorqueObject _object;
            String _key;
            Record _current;
            TorqueDictionary _dictionary;

            #endregion
        }



        /// <summary>
        /// Returns an iterator which iterates over all values stored on given object for a given key and type.
        /// Each value retrieved will have been stored using the same key but a different secondary key.
        /// </summary>
        /// <typeparam name="T">Type of values to iterate over.</typeparam>
        /// <param name="obj">Object which iterator will operate on.</param>
        /// <param name="key">Search for values using this key.</param>
        /// <returns>Iterator.</returns>
        public TorqueDictionaryEnumerator<T> Itr<T>(TorqueObject obj, String key)
        {
            return new TorqueDictionaryEnumerator<T>(this, obj, key);
        }


        #region Public methods

        /// <summary>
        /// Set value on this object for the given key.
        /// </summary>
        /// <param name="obj">Object on which to set value.</param>
        /// <typeparam name="T">Type of value.</typeparam>
        /// <param name="key">Key to store value under.</param>
        /// <param name="t">Value to set.</param>
        public void SetValue<T>(TorqueObject obj, String key, T t)
        {
            SetValue<T>(obj, key, null, t);
        }



        /// <summary>
        /// Set value on this object for the given key and secondary key.
        /// </summary>
        /// <param name="obj">Object on which to set value.</param>
        /// <typeparam name="T">Type of value.</typeparam>
        /// <param name="key">Key to store value under.</param>
        /// <param name="secondaryKey">Secondary key to store value under.</param>
        /// <param name="t">Value to set.</param>
        public void SetValue<T>(TorqueObject obj, String key, object secondaryKey, T t)
        {
            Record record = _GetRecord(obj, key, secondaryKey, true);
            record.Value = t;
        }



        /// <summary>
        /// Get value stored on this object for the given key.
        /// </summary>
        /// <param name="obj">Object from which to get value.</param>
        /// <typeparam name="T">Type of value.</typeparam>
        /// <param name="key">Key under which value is stored.</param>
        /// <returns>Stored value.</returns>
        public T GetValue<T>(TorqueObject obj, String key)
        {
            return GetValue<T>(obj, key, null);
        }



        /// <summary>
        /// Get value stored on this object for the given key and secondary key.
        /// </summary>
        /// <typeparam name="T">Type of value.</typeparam>
        /// <param name="obj">Object from which to get value.</param>
        /// <param name="key">Key under which value is stored.</param>
        /// <param name="secondaryKey">Secondary key under which value is stored.</param>
        /// <returns>Stored value.</returns>
        public T GetValue<T>(TorqueObject obj, String key, object secondaryKey)
        {
            T val;

            if (GetValue(obj, key, secondaryKey, out val))
                return val;

            return default(T);
        }



        /// <summary>
        /// Get value stored on this object for the given key.
        /// </summary>
        /// <typeparam name="T">Type of value.</typeparam>
        /// <param name="obj">Object from which to get value.</param>
        /// <param name="key">Key under which value is stored.</param>
        /// <param name="val">Stored value.</param>
        /// <returns>True if value retrieved.</returns>
        public bool GetValue<T>(TorqueObject obj, String key, out T val)
        {
            return GetValue(obj, key, null, out val);
        }



        /// <summary>
        /// Get value stored on this object for the given key and secondary key.
        /// </summary>
        /// <typeparam name="T">Type of value.</typeparam>
        /// <param name="obj">Object from which to get value.</param>
        /// <param name="key">Key under which value is stored.</param>
        /// <param name="secondaryKey">Secondary key under which value is stored.</param>
        /// <param name="val">Stored value.</param>
        /// <returns>True if value retrieved.</returns>
        public bool GetValue<T>(TorqueObject obj, String key, object secondaryKey, out T val)
        {
            Record record = _GetRecord(obj, key, secondaryKey, false);

            if (record == null || !(record.Value is T))
            {
                val = default(T);
                return false;
            }

            val = (T)record.Value;

            return true;
        }



        /// <summary>
        /// Remove value stored on this object associated with key but not associated with a secondary key.
        /// </summary>
        /// <param name="obj">Object from which to remove value.</param>
        /// <param name="key">Key to remove stored value from.</param>
        public void RemoveValue(TorqueObject obj, String key)
        {
            RemoveValue(obj, key, null);
        }



        /// <summary>
        /// Remove value stored on this object associated with key and secondary key.
        /// </summary>
        /// <param name="obj">Object from which to remove value.</param>
        /// <param name="key">Key from which to remove stored value.</param>
        /// <param name="secondaryKey">Secondary key to remove stored value from.</param>
        public void RemoveValue(TorqueObject obj, String key, object secondaryKey)
        {
            Record record = _GetRecord(obj, key, secondaryKey, false);

            if (record != null)
            {
                Assert.Fatal(key == record.Key, "TorqueDictionary.RemoveValue - Dictionary error: Given key and entry key mismatch.");
                _RemoveRecord(obj, new CompoundKey<TorqueObject, string>(obj, key), record);
            }
        }



        /// <summary>
        /// Remove all values stored on this object associated with specified key.
        /// </summary>
        /// <param name="obj">Object from which to remove value.</param>
        /// <param name="key">Key from which to remove stored value.</param>
        public void RemoveAllValues(TorqueObject obj, String key)
        {
            Record record = _GetFirstRecord(obj, key);

            if (record != null)
            {
                while (record._next != null)
                {
                    Assert.Fatal(key == record._next.Key, "TorqueDictionary.RemoveAllValues - Dictionary error: Next entry's key matches current key. Duplicate entry or corrupt key data.");
                    _RemoveRecord(obj, new CompoundKey<TorqueObject, string>(obj, key), record._next);
                }

                Assert.Fatal(key == record.Key, "TorqueDictionary.RemoveAllValues - Dictionary error: Given key and entry key mismatch.");
                _RemoveRecord(obj, new CompoundKey<TorqueObject, string>(obj, key), record);
            }
        }



        /// <summary>
        /// Remove all values associated with this object from the dictionary.
        /// </summary>
        /// <param name="obj">Object from which to remove value.</param>
        public void RemoveAllValues(TorqueObject obj)
        {
            _ClearObject(obj);
        }

        #endregion


        #region Private, protected, internal methods

        void _ClearObject(TorqueObject obj)
        {
            Record record;

            if (_objectDictionary.TryGetValue(obj, out record))
            {
                while (record._nextInObject != null)
                    _RemoveRecord(obj, new CompoundKey<TorqueObject, string>(obj, record._nextInObject.Key), record._nextInObject);

                _RemoveRecord(obj, new CompoundKey<TorqueObject, string>(obj, record.Key), record);
            }
        }



        void _RemoveRecord(TorqueObject obj, CompoundKey<TorqueObject, String> compoundKey, Record record)
        {
            if (record._prev == null)
            {
                // we are the first entry for this obj+key combo...
                if (record._next == null)
                {
                    // we are the only entry for this obj+key combo...
                    _dictionary.Remove(compoundKey);
                }
                else
                {
                    // entry next record as the base entry
                    _dictionary[compoundKey] = record._next;
                }
            }
            else
            {
                Assert.Fatal(record._prev._next == record, "TorqueDictionary._RemoveRecord - Dictionary error: corrupt entries detected.");
                record._prev._next = record._next;

                if (record._next != null)
                {
                    Assert.Fatal(record._next._prev == record, "TorqueDictionary._RemoveRecord - Dictionary error: corrupt entries detected.");
                    record._next._prev = record._prev;
                }
            }

            // remove this record from object dictionary too
            if (record._prevInObject == null)
            {
                // we are first entry in the object dictionary for this object
                if (record._nextInObject == null)
                {
                    // we are the only entry...
                    _objectDictionary.Remove(obj);
                }
                else
                {
                    // make next entry the first entry
                    record._nextInObject._prevInObject = null;
                    _objectDictionary[obj] = record._nextInObject;
                }
            }
            else
            {
                Assert.Fatal(record._prevInObject._nextInObject == record, "TorqueDictionary._RemoveRecord - Dictionary error: corrupt entries detected.");
                record._prevInObject._nextInObject = record._nextInObject;

                if (record._nextInObject != null)
                {
                    Assert.Fatal(record._nextInObject._prevInObject == record, "TorqueDictionary._RemoveRecord - Dictionary error: corrupt entries detected.");
                    record._nextInObject._prevInObject = record._prevInObject;
                }
            }

            // recycle record
            record._next = null;
            record._prev = null;
            record._nextInObject = null;
            record._prevInObject = null;
        }



        Record _GetFirstRecord(TorqueObject obj, String key)
        {
            CompoundKey<TorqueObject, String> compoundKey = new CompoundKey<TorqueObject, string>(obj, key);
            Record record;

            if (_dictionary.TryGetValue(compoundKey, out record))
                return record;

            return null;
        }



        Record _GetRecord(TorqueObject obj, String key, object key2, bool addIfNotFound)
        {
            CompoundKey<TorqueObject, String> compoundKey = new CompoundKey<TorqueObject, string>(obj, key);
            Record record;

            if (_dictionary.TryGetValue(compoundKey, out record))
            {
                while (record != null)
                {
                    Record nextRecord = record._next;

                    if (!record.IsValid)
                        _RemoveRecord(obj, compoundKey, record);
                    else if (object.Equals(key2, record.Key2)) // not same as ==, object.Equals works for value types too
                        return record;

                    record = nextRecord;
                }
            }
            if (addIfNotFound)
            {
                record = new Record();
                record.Key = key;
                record.Key2 = key2;
                Record objRecord;

                if (_objectDictionary.TryGetValue(obj, out objRecord))
                {
                    record._nextInObject = objRecord._nextInObject;
                    record._prevInObject = objRecord;

                    if (objRecord._nextInObject != null)
                        objRecord._nextInObject._prevInObject = record;

                    objRecord._nextInObject = record;
                }
                else
                {
                    _objectDictionary[obj] = record;
                }

                Record startRecord;

                if (_dictionary.TryGetValue(compoundKey, out startRecord))
                {
                    record._next = startRecord._next;
                    record._prev = startRecord;

                    if (startRecord._next != null)
                        startRecord._next._prev = record;

                    startRecord._next = record;
                }
                else
                {
                    _dictionary[compoundKey] = record;
                }

                return record;
            }

            return null;
        }

        #endregion


        #region Private, protected, internal fields

        Dictionary<CompoundKey<TorqueObject, String>, Record> _dictionary = new Dictionary<CompoundKey<TorqueObject, string>, Record>();
        Dictionary<TorqueObject, Record> _objectDictionary = new Dictionary<TorqueObject, Record>();

        #endregion
    }
}