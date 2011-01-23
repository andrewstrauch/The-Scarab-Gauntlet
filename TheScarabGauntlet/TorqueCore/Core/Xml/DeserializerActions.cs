//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Reflection;
using GarageGames.Torque.Util;



namespace GarageGames.Torque.Core.Xml
{
    /// <summary>
    /// Interface for various xml deserializer actions that happen after the xml
    /// file has been completely loaded.
    /// </summary>
    internal interface IXmlPostProcessAction
    {
        #region Interface methods

        /// <summary>
        /// Processes the action.
        /// </summary>
        /// <returns>The object created by the post process action.</returns>
        object Process();

        #endregion
    }



    /// <summary>
    /// Base class for deserializer actions that need a reference to the deserializer
    /// they are using.
    /// </summary>
    internal class BaseXmlAction
    {
        #region Constructors

        /// <summary>
        /// Sets the deserializer associated with the action when the action is created.
        /// </summary>
        /// <param name="deserializer">The deserializer.</param>
        public BaseXmlAction(TorqueXmlDeserializer deserializer)
        {
            if (deserializer == null)
                throw new Exception("BaseXmlAction Constructor - Invalid deserializer!");

            _deserializer = deserializer;
        }

        #endregion


        #region Public methods

        /// <summary>
        /// The deserializer associated with this action.
        /// </summary>
        public TorqueXmlDeserializer Deserializer
        {
            get { return _deserializer; }
        }

        #endregion


        #region Private, protected, and internal fields

        TorqueXmlDeserializer _deserializer;

        #endregion
    }



    /// <summary>
    /// Xml deserializer action for looking up name refs in the object database.
    /// </summary>
    internal class LookupNameRefAction : BaseXmlAction, IXmlPostProcessAction
    {
        #region Constructors

        /// <summary>
        /// Sets the various properties associated with this action.
        /// </summary>
        /// <param name="isObjTypeRef">True if the name is an object type, false if it is a named object.</param>
        /// <param name="nameRef">The name of the object to look up.</param>
        /// <param name="d">The deserializer.</param>
        public LookupNameRefAction(bool isObjTypeRef, string nameRef, TorqueXmlDeserializer d)
            : base(d)
        {
            Assert.Fatal(nameRef != null && nameRef != string.Empty, "LookupNameRefAction Constructor - Invalid name specified!");

            _isObjTypeRef = isObjTypeRef;
            _nameRef = nameRef;
        }

        #endregion


        #region Public methods

        /// <summary>
        /// Looks up and returns the requested object.
        /// </summary>
        /// <returns>The named torque object, or the object type that was looked up.</returns>
        public object Process()
        {
            object nameRefTarget = null;

            if (_isObjTypeRef)
                nameRefTarget = TorqueObjectDatabase.Instance.GetObjectType(_nameRef);
            else
                nameRefTarget = TorqueObjectDatabase.Instance.FindObject(_nameRef);

            if (nameRefTarget == null)
            {
                string type = _isObjTypeRef ? "object type" : "named object";
                Deserializer._Error("Unable to find " + type + " \"{0}\", check to make sure an object with this name exists in your scene.", _nameRef);
            }

            return nameRefTarget;
        }

        #endregion


        #region Private, protected, and internal fields

        bool _isObjTypeRef;
        string _nameRef;

        #endregion
    }



    /// <summary>
    /// Xml deserializer action that sets the value of nameRef properties.
    /// </summary>
    internal class BindNameRefAction : BaseXmlAction, IXmlPostProcessAction
    {
        #region Constructors

        /// <summary>
        /// Sets the various properties associated with this action.
        /// </summary>
        /// <param name="isObjTypeRef">True if the name is an object type, false if it is a named object.</param>
        /// <param name="nameRef">The name.</param>
        /// <param name="fieldOrProperty">The field or property instance to set.</param>
        /// <param name="targetInstance">The object whose property is being set.</param>
        /// <param name="d">The deserializer.</param>
        public BindNameRefAction(bool isObjTypeRef, string nameRef, IFieldOrProperty fieldOrProperty, ref object targetInstance, TorqueXmlDeserializer d)
            : base(d)
        {
            Assert.Fatal(nameRef != null && nameRef != string.Empty, "BindNameRefAction Constructor - Invalid name specified!");
            Assert.Fatal(fieldOrProperty != null, "BindNameRefAction Constructor - Invalid field or property specified!");
            Assert.Fatal(targetInstance != null, "BindNameRefAction Constructor - Invalid target instance specified!");

            _lookup = new LookupNameRefAction(isObjTypeRef, nameRef, d);
            _fieldOrProperty = fieldOrProperty;
            _targetInstance = targetInstance;
        }

        #endregion


        #region Public methods

        /// <summary>
        /// Looks up the named object with a LookupNameRefAction and sets it on the specified
        /// property.
        /// </summary>
        /// <returns>The named object.</returns>
        public object Process()
        {
            object nameRefTarget = _lookup.Process();

            if (nameRefTarget != null)
                _fieldOrProperty.SetValue(_targetInstance, nameRefTarget);

            return nameRefTarget;
        }

        #endregion


        #region Private, protected, and internal fields

        LookupNameRefAction _lookup;
        IFieldOrProperty _fieldOrProperty;
        object _targetInstance;

        #endregion
    }



    /// <summary>
    /// Xml deserializer action for aggregating xml nodes that consist of multiple references
    /// to some type.
    /// </summary>
    internal class ProcessAggregateAction : IXmlPostProcessAction
    {
        #region Constructors

        /// <summary>
        /// Sets the various properties associated with this action.
        /// </summary>
        /// <param name="aggFuncType">The type of aggregate this is.</param>
        /// <param name="aggFunc">The function to use to aggregate the data.</param>
        /// <param name="data">The list of objects to aggregate.</param>
        /// <param name="fieldOrProperty">The field or property to set.</param>
        /// <param name="targetInstance">The object whose field or property is to be set.</param>
        public ProcessAggregateAction(Type aggFuncType, string aggFunc, List<object> data, IFieldOrProperty fieldOrProperty, ref object targetInstance)
        {
            Assert.Fatal(data != null && data.Count > 0, "ProcessAggregateAction Constructor - Invalid data!");
            Assert.Fatal(fieldOrProperty != null, "ProcessAggregateAction Constructor - Invalid field or property!");
            Assert.Fatal(targetInstance != null, "ProcessAggregateAction Constructor - Invalid target!");

            _aggFuncType = aggFuncType;
            _aggFunc = aggFunc;
            _data = data;
            _fieldOrProperty = fieldOrProperty;
            _targetInstance = targetInstance;
        }

        #endregion


        #region Public methods

        /// <summary>
        /// Processes any actions in the data list, then aggregates the data based on the aggregate methed
        /// and sets the property.
        /// </summary>
        /// <returns></returns>
        public object Process()
        {
            // the data might contain post process actions, do them first and replace the list values with
            // what they return
            for (int i = 0; i < _data.Count; ++i)
            {
                IXmlPostProcessAction action = _data[i] as IXmlPostProcessAction;
                if (action != null)
                    _data[i] = action.Process();
            }

            // try to look up the aggregate function on the aggregate type
            if (_aggFuncType == null)
                _aggFuncType = _fieldOrProperty.DeclaredType;

            _aggregateTypes[0] = _data.GetType();

            MethodInfo aggregate = _aggFuncType.GetMethod(_aggFunc, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, _aggregateTypes, null);
            Assert.Fatal(aggregate != null, "ProcessAggregateAction.Process - No aggregate function named " + _aggFunc + " for type " + _aggFuncType.FullName);

            if (aggregate == null)
                return null;

            // populate args and invoke
            _aggregateArgs[0] = _data;
            object ret = aggregate.Invoke(null, _aggregateArgs);

            // stuff the return value into the field or property
            _fieldOrProperty.SetValue(_targetInstance, ret);

            return ret;
        }

        #endregion


        #region Private, protected, and internal fields

        List<object> _data;
        IFieldOrProperty _fieldOrProperty;
        object _targetInstance;
        Type[] _aggregateTypes = new Type[1];
        object[] _aggregateArgs = new object[1];
        string _aggFunc = null;
        Type _aggFuncType;

        #endregion
    }
}
