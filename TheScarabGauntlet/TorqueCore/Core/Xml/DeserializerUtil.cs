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
using GarageGames.Torque.XNA;



namespace GarageGames.Torque.Core.Xml
{
    /// <summary>
    /// Utility class used by TorqueXmlDeserializer during the deserialization process.
    /// </summary>
    public class DeserializerUtil
    {
        #region Static methods

        /// <summary>
        /// Returns the element type of elements in the list type specified by T.
        /// </summary>
        /// <param name="t">The list type to check.  Must be a list processable by DeserializedList</param>
        /// <returns>The element type of the list, or null if the type has no element type.</returns>
        public static Type GetListType(Type t)
        {
            if (t == typeof(TorqueComponentContainer))
            {
                return typeof(TorqueComponent);
            }
            else if (t.IsGenericType)
            {
                Type[] types = t.GetGenericArguments();

                Assert.Fatal(types.Length == 1, "DeserializerUtil.GetListType - Unsupported generic list type: " + t.FullName);

                if (types.Length != 1)
                    return null;

                return types[0];
            }
            else
            {
                Type elementType = t.GetElementType();

                // handle multi dimensional arrays
                while (elementType != null && elementType.HasElementType)
                    elementType = elementType.GetElementType();

                Assert.Fatal(elementType != null, "DeserializerUtil.GetListType - Null element type in list type: " + t.FullName);

                return elementType;
            }
        }



        /// <summary>
        /// Copy elements from source to overlay.  Only elements that are missing in overlay are actually copied.
        /// The result of this operation is a new tree which looks like source with overlay's replacements pasted
        /// on top of it. This function is internal because the feature that uses it (copyOf) was cut for 1.0, and
        /// the function has not been heavily tested. Use at your own risk.
        /// </summary>
        /// <param name="source">The source tree.</param>
        /// <param name="overlay">The overlay tree. This tree will be modified in place.</param>
        /// <returns>The new tree</returns>
        internal static XmlNode _TreeOverlay(XmlNode source, XmlNode overlay)
        {
            foreach (XmlNode sourceChild in source.ChildNodes)
            {
                if (sourceChild.NodeType != XmlNodeType.Element)
                    continue;

                // how many nodes with this name are in the source?
                XmlNodeList sourceNodes = source.SelectNodes(sourceChild.LocalName);

                // and how many in the overlay?
                XmlNodeList overlayNodes = overlay.SelectNodes(sourceChild.LocalName);

                // for the first n nodes common to both trees, we will just traverse 
                // the subchildren.  we don't copy nodes, because the overlay's data
                // takes precidence
                int numToTraverse = Math.Min(overlayNodes.Count, sourceNodes.Count);

                // for the remaining nodes that exist in source, but not overlay, copy
                // them over and traverse them
                int numToCopy = sourceNodes.Count - overlayNodes.Count;

                // traverse nodes first
                int i = 0;

                for (i = 0; i < numToTraverse; ++i)
                {
                    _TreeOverlay(sourceNodes[i], overlayNodes[i]);
                }

                // now copy nodes
                int offset = i;

                for (i = 0; i < numToCopy; ++i)
                {
                    XmlNode copy = sourceNodes[i + offset].CloneNode(true);
                    _TreeOverlay(sourceNodes[i + offset], copy);
                    overlay.AppendChild(copy);
                }
            }

            // reorder nodes in overlay to match source
            for (int i = 0; i < source.ChildNodes.Count; ++i)
            {
                XmlNode sourceNode = source.ChildNodes[i];
                if (sourceNode.LocalName != overlay.ChildNodes[i].LocalName)
                {
                    for (int j = i; j < overlay.ChildNodes.Count; ++j)
                    {
                        if (sourceNode.LocalName == overlay.ChildNodes[j].LocalName)
                        {
                            overlay.InsertBefore(overlay.ChildNodes[j], overlay.ChildNodes[i]);
                            break;
                        }
                    }
                }
            }

            return overlay;
        }

        #endregion
    }



    /// <summary>
    /// This is used to predeclare a set of TorqueObjectTypes in an XML file. After the types are installed, this
    /// object can be configured to lock the type database so that future type lookups on unknown types will cause
    /// an assert. This is useful for preventing typos, since type looks are done by string.
    /// </summary>
    public class ObjectTypeDeclaration
    {
        #region Public properties

        /// <summary>
        /// Whether or not the type database is locked.
        /// </summary>
        public bool LockTypes
        {
            get { return _lockTypes; }
            set
            {
                _lockTypes = value;
                _UpdateTypes();
            }
        }



        /// <summary>
        /// The object types in the type database.
        /// </summary>
        public List<string> ObjectTypes
        {
            get { return _objectTypes; }
            set
            {
                // we rely on the fact that the deserializer will build the list with subelements before
                // setting this property
                _objectTypes = value;
                _UpdateTypes();
            }
        }

        #endregion


        #region Private, protected, and internal methods

        void _UpdateTypes()
        {
            if (_lockTypes && _objectTypes != null)
            {
                // unlock type database
                TorqueObjectDatabase.Instance.ObjectTypesLocked = false;

                // add any types we have accumulated so far
                foreach (object objType in _objectTypes)
                    TorqueObjectDatabase.Instance.GetObjectType(objType.ToString());

                // now lock type database so no more types can be added
                TorqueObjectDatabase.Instance.ObjectTypesLocked = true;
            }
        }

        #endregion


        #region Private, protected, and internal fields

        bool _lockTypes = true;
        List<string> _objectTypes;

        #endregion
    }



    /// <summary>
    /// This is a wrapper class which provides a common interface for List objects that are deserialized. Ideally 
    /// we would actually break this out into separate subclasses instead of having one nasty object 
    /// that represents different kinds of lists.
    /// </summary>
    internal class DeserializedList
    {
        #region Constructors

        public DeserializedList(object list)
        {
            // need to check for array first, because it implements the ICollection interface but throws an
            // exception if you call Add on it so we don't want to treat it as an ICollection
            if (list.GetType().IsArray)
                _array = (Array)list;
            else
            {
                _container = list as TorqueComponentContainer;
                _list = list as System.Collections.IList;
            }

            _listObj = list;
        }

        #endregion


        #region Public methods

        /// <summary>
        /// Add the specified object to this list.
        /// </summary>
        /// <param name="o"></param>
        public void Add(object o)
        {
            if (_container != null)
            {
                _container.AddComponent((TorqueComponent)o);
            }
            else if (_list != null)
            {
                _list.Add(o);
            }
            else if (_array != null)
            {
                MethodInfo setInfo = _array.GetType().GetMethod("SetValue", setTypes);
                Assert.Fatal(setInfo != null, "DeserializedList.Add - No SetValue method on array!");

                setParams[0] = o;
                setParams[1] = _currArrayIndex++;
                setInfo.Invoke(_array, setParams);
            }
        }



        public Type GetListType()
        {
            return DeserializerUtil.GetListType(_listObj.GetType());
        }



        /// <summary>
        /// Get the first instance of an object which matches the specified typeInfo in this list.
        /// </summary>
        /// <param name="typeInfo"></param>
        /// <returns></returns>
        public object GetFirstInstanceOfType(TypeInfo typeInfo)
        {
            if (typeInfo == null || typeInfo.Type == null)
                return null;

            if (_list != null)
            {
                foreach (object obj in _list)
                {
                    if (obj.GetType() == typeInfo.Type)
                        return obj;
                }
            }
            else if (_array != null)
            {
                foreach (object obj in _array)
                {
                    if (obj.GetType() == typeInfo.Type)
                        return obj;
                }
            }
            else if (_container != null)
            {
                foreach (TorqueComponent obj in _container)
                {
                    if (obj.GetType() == typeInfo.Type)
                        return obj;
                }
            }

            return null;
        }

        #endregion


        #region Private, protected, and internal fields

        TorqueComponentContainer _container;
        System.Collections.IList _list;
        Array _array;
        object _listObj;
        int _currArrayIndex = 0;

        static object[] setParams = new object[2];
        static Type[] setTypes = { typeof(object), typeof(int) };

        #endregion
    }
}
