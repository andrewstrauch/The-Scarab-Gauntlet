//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.Serialization; // for some of its attributes.
using System.Globalization;
using System.Reflection;
using GarageGames.Torque.Util;
using GarageGames.Torque.XNA;



namespace GarageGames.Torque.Core.Xml
{
    /// <summary>
    /// Class for deserializing .txscene files, or any kind of XML file that represents serialized C# objects.  We wrote our own deserializer,
    /// instead of using the .Net deserializer, so that we could implement certain features that we wanted, such as nameRefs, objTypeRefs,
    /// valueOf, and aggregates.  Generally you do not use this class directly, but instead use a wrapper class like TorqueSceneData which does
    /// deserialization as well as higher level processing.
    /// </summary>
    public class TorqueXmlDeserializer
    {
        #region Constructors

        /// <summary>
        /// Creates the deserializer and adds its default types.
        /// </summary>
        public TorqueXmlDeserializer()
        {
            _AddDefaultTypes();
        }

        #endregion


        #region Public properties, operators, constants, and enums

        /// <summary>
        /// Default type map provides a way to map base names to type, that is less comprehensive then LoadTypesFromAssemblies, but faster.
        /// You can add unqualified names to this map, and whenever the deserializer needs to resolve that name, it will check the map
        /// to see if there is a mapping for it.  If so, then there is no need to specify a type attribute.
        /// </summary>
        public Dictionary<string, Type> DefaultTypeMap
        {
            get { return _defaultTypeMap; }
            set { _defaultTypeMap = value; }
        }



        /// <summary>
        /// List of assemblies that will be searched for types.  By default this list will be populated with the executable assembly, the Torque 
        /// assembly, and the XNA framework assembly.  The caller can add more assemblies to this list prior to deserialization.  The default
        /// assemblies will not be placed into the list until deserialization begins (i.e., Process() is called).
        /// </summary>
        public List<Assembly> Assemblies
        {
            get { return _assemblies; }
            set { _assemblies = value; }
        }



        /// <summary>
        /// If true, deserializer will assert when an error occurs.  Default is true.
        /// </summary>
        public bool AssertOnError
        {
            get { return _assertOnError; }
            set { _assertOnError = value; }
        }



        /// <summary>
        /// If true, deserializer will assert when a warning occurs.  Default is false.
        /// </summary>
        public bool AssertOnWarn
        {
            get { return _assertOnWarn; }
            set { _assertOnWarn = value; }
        }



        /// <summary>
        /// If true, deserializer will print messages to the Console when an error, warning, or informational event occurs.  Default is 
        /// true.
        /// </summary>
        public bool ConsoleSpew
        {
            get { return _consoleSpew; }
            set { _consoleSpew = value; }
        }


        /// <summary>
        /// Enable or disable ConsoleSpew, AssertOnWarn, and AssertOnError
        /// </summary>
        public bool Strict
        {
            set
            {
                AssertOnError = value;
                AssertOnWarn = value;
                ConsoleSpew = value;
            }
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Deserialize file into new object instance whose type is specified by type attribute on xml root element.
        /// </summary>
        /// <param name="levelFile">File to deserialize.</param>
        /// <returns>New object instance.</returns>
        public object Process(string levelFile)
        {
            return Process(levelFile, null);
        }



        /// <summary>
        /// Deserialize file into target instance.
        /// </summary>
        /// <param name="levelFile">File to deserialize.</param>
        /// <param name="target">Target instance to deserialize in to.</param>
        /// <returns>Target instance.</returns>
        public object Process(string levelFile, object target)
        {
#if DEBUG
            Profiler.Instance.StartBlock("TorqueXmlDeserializer.Process");
#endif

#if TORQUE_CONSOLE
            int startLoad = System.Environment.TickCount;
            TorqueConsole.Echo("\nLoading from XML: {0}", levelFile);
#endif
            // populate list of assemblies that will be used to find types
            TypeUtil.PopulateAssemblyList(_assemblies);

            // add our types to the default type map
            _AddDefaultTypes();

#if DEBUG
            Profiler.Instance.StartBlock("TorqueXmlDeserializer.Process.LoadXml");
#endif

            XmlDocument d = new XmlDocument();
            d.Load(levelFile);

#if DEBUG
            Profiler.Instance.EndBlock("TorqueXmlDeserializer.Process.LoadXml");
#endif

#if DEBUG
            _Validate(d);
#endif

#if DEBUG
            Profiler.Instance.StartBlock("TorqueXmlDeserializer.Process.DeserializeXml");
#endif

            // get root element
            XmlElement root = d.DocumentElement;

            // copyOf feature was disabled for 1.0 because we don't have time to get support for it into the editor.
            // and because it would require a lot of testing to make sure it actually works with sufficiently varied inputs.
#if ALLOW_COPYOF
            now = System.Environment.TickCount;
            _ProcessCopyOf(root);
            _Info("Took " + (System.Environment.TickCount - now) + " process copyOfs");
#endif

            TypeInfo ti;
            if (target == null)
                // get destination object
                _MakeNewObject(root, out target, out ti);
            else
                // get type info for existing object
                ti = TypeUtil.FindTypeInfo(target.GetType().FullName);

            if (target == null)
            {
                if (ti == null)
                    _Error("TorqueXmlDeserializer.Process - Unable to find type for root object.");
                else
                    _Error("TorqueXmlDeserializer.Process - Unable to create instance of type {0} for root object.", ti.Type.FullName);
            }
            else
                _Recurse(root, ref target, ti);

            // do post process actions
            foreach (IXmlPostProcessAction action in _postProcessActions)
                action.Process();

            _postProcessActions.Clear();

#if DEBUG
            Profiler.Instance.EndBlock("TorqueXmlDeserializer.Process.DeserializeXml");

            Profiler.Instance.EndBlock("TorqueXmlDeserializer.Process");
#endif

#if TORQUE_CONSOLE
            TorqueConsole.Echo("Took {0} ms to load.", System.Environment.TickCount - startLoad);
#endif
            return target;
        }

        #endregion


        #region Private, protected, internal methods

        /// <summary>
        /// Verifies that all elements contain only attributes in the "known set".  It does not verify that the attributes are actually used 
        /// correctly in context.
        /// </summary>
        /// <param name="d">The XmlDocument to validate.</param>
        void _Validate(XmlDocument d)
        {
            XmlNode root = d.DocumentElement;

            // right now we just validate that the attributes on elements below the root are in the "known set".  this does not actually validate
            // that the attributes are contextually used correctly.  It's really just typo prevention.
            string query = "descendant::*[@*]";
            XmlNodeList list = root.SelectNodes(query);

            foreach (XmlNode e in list)
            {
                if (e.NodeType != XmlNodeType.Element)
                    continue;

                if (e.Attributes.Count == 0)
                    continue;

                foreach (XmlAttribute attr in e.Attributes)
                {
                    bool ok = false;
                    switch (attr.LocalName)
                    {
                        case "nameRef":
                        case "objTypeRef":
                        case "name":
                        case "valueOf":
                        case "copyOf":
                        case "inPlace":
                        case "type":
                            ok = true;
                            break;
                    }

                    if (!ok)
                        _Warn("TorqueXmlDeserializer._Validate - Invalid attribute {0} on element {1}.", attr.LocalName, e.LocalName);
                }
            }
        }



        /// <summary>
        /// Create a new instance of an object.
        /// </summary>
        /// <param name="typeName">The full type name of the object.</param>
        /// <param name="o">Receives the new object instance.</param>
        /// <param name="t">Receives the TypeInfo for the new object</param>
        /// <param name="childCount">If the type is a list, this count will be used to set the size for the list.</param>
        void _MakeNewObject(string typeName, out object o, out TypeInfo t, int childCount)
        {
            o = null;
            t = null;

            t = TypeUtil.FindTypeInfo(typeName);

            if (t == null)
                return;

            if (t.Type.IsArray)
            {
                Assert.Fatal(childCount > -1, "TorqueXMLDeserializer._MakeNewObject - Array type specified and child count is negative somehow... Whad did you do?");
                o = Array.CreateInstance(t.Type.GetElementType(), childCount);
            }
            else
            {
                o = Activator.CreateInstance(t.Type);
            }
        }



        /// <summary>
        /// Create a new instance of an object.
        /// </summary>
        /// <param name="typeName">The full type name of the object.</param>
        /// <param name="o">Receives the new object instance.</param>
        /// <param name="t">Receives the TypeInfo for the new object.</param>
        void _MakeNewObject(string typeName, out object o, out TypeInfo t)
        {
            _MakeNewObject(typeName, out o, out t, -1);
        }



        /// <summary>
        /// Create a new instance of an object.
        /// </summary>
        /// <param name="element">The element to create a type for.  The code will look for a "type" attribute on the element to find the type
        /// name.  If no type attribute is specified, the element local name will be used.  This may be sufficient if the local name has been
        /// mapped in the default type map.</param>
        /// <param name="o">Receives the new object instance.</param>
        /// <param name="t">Receives the TypeInfo for the new object.</param>
        void _MakeNewObject(XmlNode element, out object o, out TypeInfo t)
        {
            _MakeNewObject(element, out o, out t, -1);
        }



        /// <summary>
        /// Create a new instance of an object.
        /// </summary>
        /// <param name="element">The element to create a type for.  The code will look for a "type" attribute on the element to find the type
        /// name.  If no type attribute is specified, the element local name will be used.  This may be sufficient if the local name has been
        /// mapped in the default type map.</param>
        /// <param name="o">Receives the new object instance.</param>
        /// <param name="t">Receives the TypeInfo for the new object.</param>
        /// <param name="childCount">If the type is a list, this count will be used to set the size for the list.</param>
        void _MakeNewObject(XmlNode element, out object o, out TypeInfo t, int childCount)
        {
            o = null;
            t = null;
            string typeName = _GetObjectTypeName(element);

            _MakeNewObject(typeName, out o, out t, childCount);
        }



        /// <summary>
        /// Given an element, try to determine the full type name for the element.  Generally this involves checking for a type attribute, and if 
        /// that is missing, looking up the type in the default type map using the element's local name.  
        /// </summary>
        /// <param name="element">The element whose type is being looked up.</param>
        /// <returns>The full type name, or the element's local name if no type could be found.</returns>
        string _GetObjectTypeName(XmlNode element)
        {
            string typeName = null;
            XmlNode typeNode = element.Attributes.GetNamedItem("type");

            if (typeNode != null)
                typeName = typeNode.Value;
            else
            {
                // do we have a type mapping for this object?
                if (_defaultTypeMap != null)
                {
                    Type mapped = null;

                    if (_defaultTypeMap.TryGetValue(element.LocalName, out mapped))
                        typeName = mapped.FullName;
                }

                if (typeName == null)
                {
                    // oh well, use the element name, even though it probably won't work.
                    typeName = element.LocalName;
                }
            }
            return typeName;
        }



        /// <summary>
        /// Check to see if the element has a valueOf attribute and process it appropriately if so.
        /// </summary>
        /// <param name="element"></param>
        /// <returns>The appropriate value if it has valueOf, null otherwise.</returns>
        object _CheckValueOf(XmlNode element)
        {
            // check for valueOf attribute
            XmlNode valueOfAttr = element.Attributes.GetNamedItem("valueOf");

            if (valueOfAttr == null)
                return null;

            string typeRef = valueOfAttr.Value;
            string baseType;
            string target;

            if (!TypeUtil.ParseType(typeRef, out baseType, out target))
            {
                _Error("Unable to parse type for valueOf: " + typeRef);
                return null;
            }

            // find type
            TypeInfo t = TypeUtil.FindTypeInfo(baseType);

            if (t == null)
            {
                // game over man!
                _Error("Unable to look up type for valueOf: " + typeRef);
                return null;
            }

            IFieldOrProperty fieldOrProperty = t.GetStaticFieldOrProperty(target);

            if (fieldOrProperty != null)
                return fieldOrProperty.GetValue(null); // null because its static

            // try to find static, zero argument function
            MethodInfo func = t.GetStaticMethod(target);

            if (func != null)
                return func.Invoke(null, null);

            _Error("Found type {0}, but cannot find static field, property, or zero-argument method named {1}", typeRef, target);

            return null;
        }



        /// <summary>
        /// Check to see if the element has an aggregate attribute and process it appropriately if so.  
        /// </summary>
        /// <param name="fieldOrProperty">The IFieldOrProperty instance where the aggregate value will be set.</param>
        /// <param name="element">The element to check.</param>
        /// <param name="o">The object instance to set the field or property value on.</param>
        /// <returns>true if there is an aggregate, false otherwise</returns>
        bool _CheckAggregate(IFieldOrProperty fieldOrProperty, XmlNode element, ref object o)
        {
            // if the element has no child nodes, it cannot be an aggregate
            if (element.ChildNodes.Count == 0)
                return false;

            TypeInfo ti = TypeUtil.FindTypeInfo(fieldOrProperty.DeclaredType.FullName);

            if (ti == null)
                return false;

            bool isAggregate = false;
            string aggregateFunction = string.Empty;

            if (ti.Type == typeof(TorqueObjectType))
            {
                isAggregate = true;
                aggregateFunction = "Aggregate";
            }

            // allow override with aggregate attribute
            XmlNode aggAttr = element.Attributes.GetNamedItem("aggregateWith");

            if (aggAttr != null && aggAttr.Value != null && aggAttr.Value.Trim() != string.Empty)
            {
                isAggregate = true;
                aggregateFunction = aggAttr.Value.Trim();
            }

            if (!isAggregate)
                return false;


            Type aggFuncType = null;
            string baseType = string.Empty;
            string target = string.Empty;

            if (aggregateFunction.IndexOf(".") != -1)
            {
                // its possible to aggregate data using an arbitrary function type.  lookup that type
                if (!TypeUtil.ParseType(aggregateFunction, out baseType, out target))
                {
                    _Error("Unable to parse aggregate function: " + aggregateFunction);
                    return false;
                }

                if (baseType != string.Empty)
                {
                    TypeInfo ati = TypeUtil.FindTypeInfo(baseType);
                    if (ati == null)
                    {
                        _Error("Unable to find aggregate type: " + baseType);
                        return false;
                    }
                    aggFuncType = ati.Type;
                }
            }
            else
            {
                // no "." in name, so use unqualifed function name on the declared type
                target = aggregateFunction;
            }

            _RecurseAggregate(aggFuncType, target, fieldOrProperty, element, ti, ref o);
            return true;
        }



        /// <summary>
        /// For debugging purposes, return a name for the specified object.  Used to report names of objects in error messages.
        /// </summary>
        /// <param name="o"></param>
        static string _GetObjectName(object o)
        {
            if (o == null)
                return "null";

            string objName = null;

            // use torque base name if possible
            if (o is TorqueBase)
            {
                objName = (o as TorqueBase).Name;
                // if it is a type, use type fule name
            }
            else if (o is Type)
            {
                objName = (o as Type).FullName;
            }

            // if no name, use type full name
            if (string.IsNullOrEmpty(objName))
                objName = o.GetType().FullName;

            return objName;
        }



        /// <summary>
        /// Set the value of the input field or property using the element.  This function will examine the inputs to determine the 
        /// best match for the element.  In some cases this may trigger a recursive examination of the element's children.
        /// </summary>
        /// <param name="fieldOrProperty">Field or property whose value will be set.</param>
        /// <param name="element">Element to obtain the value from.</param>
        /// <param name="o">Object instance whose field or property will be set.</param>
        void _SetFieldOrProperty(IFieldOrProperty fieldOrProperty, XmlNode element, ref object o)
        {
            // first check for valueOf 
            object value = _CheckValueOf(element);

            if (value != null)
            {
                fieldOrProperty.SetValue(o, value);
                // done
                return;
            }

            // check for aggregate
            if (_CheckAggregate(fieldOrProperty, element, ref o))
                // no need to set value; values of aggregates are set as post process action.
                return;

            string objName = _GetObjectName(o);

            // if its a string we can just set it
            if (fieldOrProperty.DeclaredType == typeof(System.String))
            {
                fieldOrProperty.SetValue(o, element.InnerText);
            }
            else if (TypeUtil.IsPrimitiveType(fieldOrProperty.DeclaredType))
            {
                if (element.InnerText == null || element.InnerText.Trim() == string.Empty)
                {
                    _Error("Empty primitive value for type, won't set it: element: " + element.LocalName + ", type: " + fieldOrProperty.DeclaredType.FullName + ", object: " + objName);
                }
                else
                {
                    // extract primitive value and set it
                    try
                    {
                        fieldOrProperty.SetValue(o, TypeUtil.GetPrimitiveValue(fieldOrProperty.DeclaredType, element.InnerText));
                    }
                    catch (Exception)
                    {
                        _Error("TorqueXmlDeserializer._SetFieldOrProperty - Unable to parse format string {0} for type {1} for element {2} in object {3}.", element.InnerText, fieldOrProperty.DeclaredType.FullName, element.LocalName, objName);
                    }
                }
            }
            // if it is a reference type, we can deserialize in to it
            else if (fieldOrProperty.DeclaredType.IsClass || fieldOrProperty.DeclaredType.IsInterface || fieldOrProperty.DeclaredType.IsAbstract)
            {
                bool isInstantiable = !(fieldOrProperty.DeclaredType.IsInterface || fieldOrProperty.DeclaredType.IsAbstract);

                object subObj;
                TypeInfo subType;

                // check for name ref
                if (_BindNameRef(element, fieldOrProperty, ref o) != null)
                    // done with this field
                    return;

                // for reference types, allow deserializing into the existing object, if the attribute is present 
                // and there is a valid object.
                subObj = fieldOrProperty.GetValue(o);
                bool inPlace = false;
                if (subObj != null)
                {
                    // instance exists, check for attribute - use the property/field type for this, not
                    // declared type.  
                    object[] attributes = fieldOrProperty.GetCustomAttributes(true);
                    foreach (object attr in attributes)
                        if (attr is TorqueXmlDeserializeInPlace)
                        {
                            inPlace = true;
                            break;
                        }

                    if (!inPlace)
                    {
                        // check for xml attribute
                        XmlNode inPlaceAttribute = element.Attributes.GetNamedItem("inPlace");
                        if (inPlaceAttribute != null && Boolean.Parse(inPlaceAttribute.Value))
                            inPlace = true;
                    }
                }

                if (subObj != null && inPlace)
                {
                    // ok to deserialize in place; get the typeinfo object for later use.  
                    subType = TypeUtil.FindTypeInfo(subObj.GetType().FullName);
                }
                else
                {
                    // log if we are replacing, but there was an instance already
                    //if (subObj != null)
                    //    _Info("Field has object instance but TorqueXmlDeserializeInPlace/inPlace attribute not specified: creating new object:" + element.LocalName);

                    // in case we are making a fixed size list, pass the number of children down to the object maker
                    int childCount = element.ChildNodes.Count;

                    // if type is not instantiable, we might be able to come up with an instantiable type by looking for type mappings
                    if (!isInstantiable)
                        _MakeNewObject(element, out subObj, out subType, childCount);
                    else
                        // instantiable type, so make object used declared type
                        _MakeNewObject(fieldOrProperty.DeclaredType.FullName, out subObj, out subType, childCount);

                    if (subObj == null)
                    {
                        _Error("Unable to create new instance for element {0} on object {1}", element.LocalName, objName);
                        return;
                    }
                }

                // if it is a list of objects, handle it specially
                if (subType.IsList)
                {
                    DeserializedList dlist = new DeserializedList(subObj);
                    _RecurseList(element, ref dlist, subType);
                }
                else
                    _Recurse(element, ref subObj, subType);

                // set value
                // it is important to do this after the element has been deserialized - some property accessors
                // assume this (such as lists of link points and object types).
                fieldOrProperty.SetValue(o, subObj);
            }
            // if it is a value type, then it must be a struct (because we handled primitives earlier) 
            else if (fieldOrProperty.DeclaredType.IsValueType)
            {
                // look for object type ref
                if (_BindObjectTypeRef(element, fieldOrProperty, ref o) != null)
                    return;

                object subObj;
                TypeInfo subType;
                _MakeNewObject(fieldOrProperty.DeclaredType.FullName, out subObj, out subType);
                if (subType == null) // check type for null instead of object, since this is a value type
                {
                    _Error("Unable to create new instance for element {0} on object {1}", element.LocalName, objName);
                    return;
                }

                _Recurse(element, ref subObj, subType);

                // set value
                fieldOrProperty.SetValue(o, subObj);
            }
            else
                _Error("Don't know how to set field or property of type {0} for element {1} on object: {2}", fieldOrProperty.DeclaredType.FullName, element.LocalName, objName);

        }



        /// <summary>
        /// Create an object type ref lookup action for the specified element, if applicable.
        /// </summary>
        /// <param name="element"></param>
        /// <returns>The object type lookup action, or null if no action is required.</returns>
        IXmlPostProcessAction _LookupObjectTypeRef(XmlNode element)
        {
            object dummy = null;
            return _CheckNameRef("objTypeRef", true, true, element, null, ref dummy);
        }



        /// <summary>
        /// Create an object type ref bind action for the specified element, if applicable.
        /// </summary>
        /// <param name="element"></param>
        /// <returns>The object type bind action, or null if no action is required.</returns>
        IXmlPostProcessAction _BindObjectTypeRef(XmlNode element, IFieldOrProperty fieldOrProperty, ref object o)
        {
            IXmlPostProcessAction action = _CheckNameRef("objTypeRef", false, true, element, fieldOrProperty, ref o);
            // cannot bind these on value types, because we cannot store a ref reference.
            Assert.Fatal(action == null || !(o is ValueType), "Cannot bind object type refs on valuetypes");
            return action;
        }



        /// <summary>
        /// Create an name ref lookup action for the specified element, if applicable.
        /// </summary>
        /// <param name="element"></param>
        /// <returns>The name ref lookup action, or null if no action is required.</returns>
        IXmlPostProcessAction _LookupNameRef(XmlNode element)
        {
            object dummy = null;
            return _CheckNameRef("nameRef", true, false, element, null, ref dummy);
        }



        /// <summary>
        /// Create an name ref lookup action for the specified element, if applicable.
        /// </summary>
        /// <param name="element"></param>
        /// <returns>The name ref lookup action, or null if no action is required.</returns>
        IXmlPostProcessAction _BindNameRef(XmlNode element, IFieldOrProperty fieldOrProperty, ref object o)
        {
            IXmlPostProcessAction action = _CheckNameRef("nameRef", false, false, element, fieldOrProperty, ref o);
            // cannot bind these on value types, because we cannot store a ref reference.
            Assert.Fatal(action == null || !(o is ValueType), "Cannot bind name refs on valuetypes");
            return action;
        }



        /// <summary>
        /// Process the specified name or object type ref.
        /// </summary>
        /// <param name="nameRefAttribute">the attribute to look for</param>
        /// <param name="lookupOnly">whether this is just a lookup (not a bind).  if true, fieldOrProperty and object can be null.</param>
        /// <param name="isObjectTypeRef">true if this is an object type ref lookup, false if it is a nameref lookup</param>
        /// <param name="element">the element to examine</param>
        /// <param name="fieldOrProperty">the field or property to set the value on</param>
        /// <param name="o">the object instance to set the value on</param>
        /// <returns>A post process action, or null if none is appropriate.</returns>
        IXmlPostProcessAction _CheckNameRef(string nameRefAttribute, bool lookupOnly, bool isObjectTypeRef, XmlNode element, IFieldOrProperty fieldOrProperty, ref object o)
        {
            Assert.Fatal(lookupOnly || (fieldOrProperty != null && o != null), "field and instance required for name bind operations");

            // see if the element has a nameRef
            IXmlPostProcessAction action = null;
            XmlNode nameRefAttr = element.Attributes[nameRefAttribute];

            if (nameRefAttr != null)
            {
                if (lookupOnly)
                    action = new LookupNameRefAction(isObjectTypeRef, nameRefAttr.Value, this);
                else
                    action = new BindNameRefAction(isObjectTypeRef, nameRefAttr.Value, fieldOrProperty, ref o, this);
                _postProcessActions.Add(action);
                return action;
            }

            return action;
        }



        /// <summary>
        /// Examine the children of the specifid list element and add its values to the input list.
        /// </summary>
        /// <param name="element">Element to examine</param>
        /// <param name="list">List that will receive the values</param>
        /// <param name="ti">TypeInfo of the list that is receiving the values.</param>
        void _RecurseList(XmlNode element, ref DeserializedList list, TypeInfo ti)
        {
            // get the type of the list.  if it is a specific type (i.e., not "object"), we can use that type
            // to create instances for children that do not specify a type attribute.
            Type listType = list.GetListType();
            bool listTypeInstantiable = TypeUtil.IsInstantiable(listType);

            foreach (XmlNode child in element.ChildNodes)
            {
                if (child.NodeType != XmlNodeType.Element)
                    continue;

                TypeInfo subType = null;
                object subObj = null;

                // does the element have inPlace specified?  If so, search for an existing instance of the same type
                // in the list.  if it exists, deserialize into that instance, instead of into a new object, 
                bool inPlace = false;
                XmlNode inPlaceAttribute = child.Attributes.GetNamedItem("inPlace");

                if (inPlaceAttribute != null && Boolean.Parse(inPlaceAttribute.Value))
                {
                    // has attribute.  lookup the type
                    string typeName = _GetObjectTypeName(child);
                    subType = TypeUtil.FindTypeInfo(typeName);
                    // try to find an instance of the type in the list (will be null if unsuccessful)
                    subObj = list.GetFirstInstanceOfType(subType);
                    inPlace = subObj != null;
                }

                if (!inPlace)
                    // create new object
                    _MakeNewObject(child, out subObj, out subType);

                if (subObj == null && subType == null && listTypeInstantiable)
                {
                    // couldn't find a mapping for this type, but the list itself has an instantiable type that we could use.
                    // if we are creating a String, just use the text value from the xml instead of creating a new object.
                    // we can't actual create a new object anyway because String has no zero argument constructor.
                    if (listType == typeof(String))
                    {
                        subObj = child.InnerText;
                        subType = TypeUtil.FindTypeInfo(typeof(String).FullName);
                    }
                    else
                    {
                        // create the new object using the list's type.
                        // sometimes a warning but sometimes what the user intends.  we'll call it _Info
                        // ok, this is just excessive console spam.  don't worry about this case.
                        //_Info("List subelement '{0}' has no default type mapping or type attribute specified.  Using the type of the list container, '{1}', to create list element instance.", child.LocalName, listType.FullName);
                        _MakeNewObject(listType.FullName, out subObj, out subType);
                    }
                }

                // still no object? warn about it and continue
                if (subObj == null)
                {
                    _Warn("Ignoring List subelement '{0}': no default type mapping, no type attribute, and list type '{1}' is not instantiable", child.LocalName, listType.FullName);
                    continue;
                }

                // if the subType is a primitive or enum, do not recurse on it - instead just set the value directly
                if (TypeUtil.IsPrimitiveType(subType.Type))
                {
                    try
                    {
                        subObj = TypeUtil.GetPrimitiveValue(subType.Type, child.InnerText);
                    }
                    catch (Exception)
                    {
                        _Error("TorqueXmlDeserializer._SetFieldOrProperty - Unable to parse format string {0} for type {1}.", child.InnerText, subType.Type.FullName);
                    }
                }
                else
                {
                    _Recurse(child, ref subObj, subType);
                }

                if (!inPlace)
                    list.Add(subObj);
            }
        }



        /// <summary>
        /// Examine the children of the specified element and process them as an aggregate.  An aggregate is essentially a "reduce" operation
        /// that combines a series of inputs into a single value.
        /// </summary>
        /// <param name="aggFuncType"></param>
        /// <param name="aggFunc"></param>
        /// <param name="fieldOrProperty"></param>
        /// <param name="element"></param>
        /// <param name="ti"></param>
        /// <param name="o"></param>
        void _RecurseAggregate(Type aggFuncType, string aggFunc, IFieldOrProperty fieldOrProperty, XmlNode element, TypeInfo ti, ref object o)
        {
            // create list for storage of aggregate data
            List<object> data = new List<object>();

            foreach (XmlNode child in element.ChildNodes)
            {
                if (child.NodeType != XmlNodeType.Element)
                    continue;

                // check for obj type ref
                IXmlPostProcessAction action = null;
                action = _LookupObjectTypeRef(child);

                if (action != null)
                {
                    data.Add(action);
                    continue;
                }

                // check for name ref
                action = _LookupNameRef(child);

                if (action != null)
                {
                    data.Add(action);
                    continue;
                }

                // recurse on type
                TypeInfo subType = null;
                object subObj = null;

                // create new object
                _MakeNewObject(ti.Type.FullName, out subObj, out subType);

                if (subObj == null)
                {
                    _Warn("Ignoring aggregate subelement: " + child.LocalName);
                    continue;
                }

                // if the subType is a primitive or enum, do not recurse on it - instead just set the value directly
                if (TypeUtil.IsPrimitiveType(subType.Type))
                {
                    try
                    {
                        subObj = TypeUtil.GetPrimitiveValue(subType.Type, child.InnerText);
                    }
                    catch (Exception)
                    {
                        _Error("TorqueXmlDeserializer._SetFieldOrProperty - Unable to parse format string {0} for type {1}.", child.InnerText, subType.Type.FullName);
                    }
                }
                else
                {
                    _Recurse(child, ref subObj, subType);
                }

                data.Add(subObj);
            }

            // if we have data, create a post process task to handle the aggregation and set the final value
            if (data.Count > 0)
            {
                IXmlPostProcessAction action = new ProcessAggregateAction(aggFuncType, aggFunc, data, fieldOrProperty, ref o);
                _postProcessActions.Add(action);
            }
        }



        /// <summary>
        /// Deserializer the specified element into the specified object instance.
        /// </summary>
        /// <param name="element"></param>
        /// <param name="o"></param>
        /// <param name="ti"></param>
        void _Recurse(XmlNode element, ref object o, TypeInfo ti)
        {
            // to help catch errors, make sure name attribute and name subelement have same value (if present)
            XmlNode nameAttr = element.Attributes.GetNamedItem("name");

            if (nameAttr == null)
                nameAttr = element.Attributes.GetNamedItem("Name");

            XmlNode nameChild = element.SelectSingleNode("child::Name");

            if (nameAttr != null && nameChild != null)
            {
                if (nameAttr.Value != nameChild.InnerText)
                    throw new Exception(String.Format("Name attribute/element mismatch on element {0}, attr: {1}/element: {2}", element.LocalName, nameAttr.Value, nameChild.InnerText));
            }

            // if we have the name attribute, we can set the name using that, and skip the name element.
            bool skipNameElement = false;

            if (nameAttr != null)
            {
                string name = nameAttr.Value.Trim();

                if (name != string.Empty)
                {
                    IFieldOrProperty fieldOrProperty = ti.GetFieldOrProperty("Name");

                    if (fieldOrProperty != null)
                    {
                        // since this is an attribute, we can't use the _SetFieldOrProperty function, which 
                        // expects an element.  So just so the value directly.
                        fieldOrProperty.SetValue(o, name);
                        skipNameElement = true;
                    }
                }
            }

            // for each child, if there is a property/field with the same name, deserialize 
            foreach (XmlNode child in element.ChildNodes)
            {
                if (child.NodeType != XmlNodeType.Element)
                    continue;

                if (skipNameElement && child.LocalName.Trim() == "Name")
                    continue;

                IFieldOrProperty fieldOrProperty = ti.GetFieldOrProperty(child.LocalName);

                if (fieldOrProperty != null)
                {
                    _SetFieldOrProperty(fieldOrProperty, child, ref o);
                    continue;
                }
            }

        }



        /// <summary>
        /// Log an informational message.
        /// </summary>
        /// <param name="message"></param>
        internal void _Info(string message)
        {
            if (_consoleSpew)
                TorqueConsole.Echo(message);
        }



        /// <summary>
        /// Log an informational message.
        /// </summary>
        /// <param name="message"></param>
        internal void _Info(string format, params object[] args)
        {
            string message = String.Format(format, args);
            _Info(message);
        }



        /// <summary>
        /// Log a warning.
        /// </summary>
        /// <param name="message"></param>
        internal void _Warn(string message)
        {
            if (_consoleSpew)
                TorqueConsole.Warn(message);

            if (_assertOnWarn)
                Assert.Fatal(false, message);
        }



        /// <summary>
        /// Log a warning.
        /// </summary>
        /// <param name="message"></param>
        internal void _Warn(string format, params object[] args)
        {
            string message = String.Format(format, args);
            _Warn(message);
        }



        /// <summary>
        /// Log an error.
        /// </summary>
        /// <param name="message"></param>
        internal void _Error(string message)
        {
            if (_consoleSpew)
                TorqueConsole.Error(message);

            Assert.Fatal(_assertOnError, message);
        }



        /// <summary>
        /// Log an error.
        /// </summary>
        /// <param name="message"></param>
        internal void _Error(string format, params object[] args)
        {
            string message = String.Format(format, args);
            _Error(message);
        }



#if ALLOW_COPYOF
        void _ProcessCopyOf(XmlNode root)
        {
            string query = "descendant::*[@copyOf]";
            XmlNodeList list = root.SelectNodes(query);

            foreach (XmlNode n in list)
                if (n.SelectNodes(query).Count > 0)
                    throw new Exception("Invalid XML file: nested copyOf attributes are not permitted.  Base element: " + n.LocalName);

            // walk the list of nodes.  when we find a node that doesn't have any copyOfs, insert it in
            // our dictionary of available trees.  then process the other nodes that require that tree.
            // need to check for infinite loop here
            Dictionary<string, XmlNode> nameTrees = new Dictionary<string, XmlNode>();
            List<XmlNode> copyNodes = new List<XmlNode>();

            // build a modifiable list of copyOf nodes
            foreach (XmlNode n in list)
                copyNodes.Add(n);

            // while we still have nodes left to process
            while (copyNodes.Count > 0)
            {
                // find a node that we can process
                bool processedNode = false;

                for (int i = 0; i < copyNodes.Count; ++i)
                {
                    XmlNode n = copyNodes[i];

                    // get the name of the tree that it wants to be a copy of
                    string copyOfVal = n.Attributes.GetNamedItem("copyOf").Value.Trim();
                    // do we already have a copy of that tree?
                    XmlNode tree;

                    if (!nameTrees.TryGetValue(copyOfVal, out tree))
                    {
                        // no, but it should exist in the document.  find it
                        tree = root.SelectSingleNode("descendant::Name[. = \"" + copyOfVal + "\"]");

                        if (tree == null)
                            // should not happen
                            throw new Exception("Unable to find named element for copyOf source: " + copyOfVal);
                        if (tree.ParentNode == null)
                            // should not happen
                            throw new Exception("Error, Name node has no parent: " + copyOfVal);

                        // we selected the name node, but we're actually interested in its parent node.
                        tree = tree.ParentNode;

                        // does the tree have any copyOf attributes?  
                        XmlNodeList copyOfs = tree.SelectNodes("descendant-or-self::*[@copyOf]");

                        if (copyOfs.Count > 0)
                            // yes, so we can't process this tree yet.  don't put it into the dictionary either,
                            // we only put "finished" trees (i.e. no copyOfs) in there.
                            continue;

                        // this tree is ready to be used, add it to dictionary
                        nameTrees.Add(copyOfVal, tree);
                    }

                    // we have the source tree, so overlay our tree on it to make new tree
                    XmlNode newTree = DeserializerUtil._TreeOverlay(tree, n);

                    // if our node has a name element, then store our new tree using that name
                    XmlNode name = n.SelectSingleNode("child::Name");

                    if (name != null)
                    {
                        // remove copyOf
                        newTree.Attributes.RemoveNamedItem("copyOf");
                        nameTrees.Add(name.InnerText.Trim(), newTree);
                    }

                    // for debugging.
                    XmlDocument d = new XmlDocument();
                    d.AppendChild(d.ImportNode(tree, true));
                    d.Save("source.xml");
                    d = new XmlDocument();
                    d.AppendChild(d.ImportNode(newTree, true));
                    d.Save("dest.xml");

                    // remove node from list and break out of inner loop
                    copyNodes.RemoveAt(i);
                    processedNode = true;
                    break;
                }

                if (!processedNode)
                    throw new Exception("Couldn't finish processing copyOfs, there is probably a cycle in the copyOf references");
            }            
        }
#endif


        /// <summary>
        /// Add the default types to the map.
        /// </summary>
        void _AddDefaultTypes()
        {
            if (_defaultTypeMap == null)
                _defaultTypeMap = new Dictionary<string, Type>();

            if (!_defaultTypeMap.ContainsKey("ObjectTypeDeclaration"))
                _defaultTypeMap.Add("ObjectTypeDeclaration", typeof(ObjectTypeDeclaration));
        }

        #endregion


        #region Private, protected, internal fields

        List<Assembly> _assemblies = new List<Assembly>();

        // arguments for the Parse(string) function, used to unpack strings into primitives
        Type[] _parseTypes = { typeof(String) };
        object[] _parseArgs = new object[1];
        Type[] _numericParseTypes = { typeof(String), typeof(System.Globalization.NumberStyles) };
        object[] _numericParseArgs = new object[2];
        Dictionary<string, Type> _defaultTypeMap;
        List<IXmlPostProcessAction> _postProcessActions = new List<IXmlPostProcessAction>();

        bool _assertOnError = true;
        bool _assertOnWarn = false;
        bool _consoleSpew = true;

        #endregion
    }
}
