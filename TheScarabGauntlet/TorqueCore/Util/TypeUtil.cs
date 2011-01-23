//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Xml;
using System.Xml.Serialization;
using System.Text;

using GarageGames.Torque.Core;
using GarageGames.Torque.XNA;
using Microsoft.Xna.Framework;



namespace GarageGames.Torque.Util
{
    /// <summary>
    /// Helper interface for defining a common interface for properties and fields, which have 
    /// different interfaces for similar functionality. 
    /// </summary>
    public interface IFieldOrProperty
    {
        #region Public properties, operators, constants, and enums

        /// <summary>
        /// The type of the "return" type of the property, or declared type of the field 
        /// </summary>
        Type DeclaredType
        {
            get;
        }

        /// <summary>
        /// Name of this field or property in the XML.  Only populated when exporting torque XML schemas.
        /// </summary>
        string XmlName
        {
            get;
            set;
        }

        /// <summary>
        /// Is this field or property static.
        /// </summary>
        bool IsStatic
        {
            get;
        }

        #endregion


        #region Public methods

        /// <summary>
        /// Get the value of this field or property using the specified instance.
        /// </summary>
        /// <param name="o">The object to get the property or field value off of.</param>
        /// <returns>The value of the property or field.</returns>
        object GetValue(object o);

        /// <summary>
        /// Set the value of this field or property using the specified instance.
        /// </summary>
        /// <param name="o">The object to set the property or field value on.</param>
        /// <param name="value">The value to set.</param>
        void SetValue(object o, object value);

        /// <summary>
        /// Gets the custom attributes of the field.
        /// </summary>
        /// <param name="inherit">Whether to inherit custom attributes from parent classes.</param>
        /// <returns>Array of custom attributes.</returns>
        object[] GetCustomAttributes(bool inherit);

        #endregion
    }



    /// <summary>
    /// Implementation of IFieldOrProperty for field types. Wraps a field for simple lookup of
    /// type information.
    /// </summary>
    public class FieldWrapper : IFieldOrProperty
    {
        #region Public properties, operators, constants, and enums

        /// <summary>
        /// Creates a wrapper for the specified field.
        /// </summary>
        /// <param name="field">The field to wrap.</param>
        public FieldWrapper(FieldInfo field)
        {
            Assert.Fatal(field != null, "FieldWrapper.FieldWrapper - FieldWrapper must be instanciated with a valid field!");
            _field = field;
        }

        public Type DeclaredType
        {
            get { return _field.FieldType; }
        }

        public string XmlName
        {
            get { return _xmlName; }
            set { _xmlName = value; }
        }

        public bool IsStatic
        {
            get { return _field.IsStatic; }
        }

        #endregion


        #region Public methods

        public object[] GetCustomAttributes(bool inherit)
        {
            return _field.GetCustomAttributes(inherit);
        }

        public object GetValue(object obj)
        {
            return _field.GetValue(obj);
        }

        public void SetValue(object obj, object value)
        {
            _field.SetValue(obj, value);
        }

        #endregion


        #region Private, protected, internal fields

        FieldInfo _field;
        string _xmlName = string.Empty;

        #endregion
    }



    public class PropertyWrapper : IFieldOrProperty
    {

        #region Public properties, operators, constants, and enums

        /// <summary>
        /// Creates a wrapper for the specified property.
        /// </summary>
        /// <param name="property">The property to wrap.</param>
        public PropertyWrapper(PropertyInfo property)
        {
            Assert.Fatal(property != null, "PropertyWrapper.PropertyWrapper - PropertyWrapper must be instanciated with a valid property!");
            _property = property;
        }

        public Type DeclaredType
        {
            get { return _property.PropertyType; }
        }

        public string XmlName
        {
            get { return _xmlName; }
            set { _xmlName = value; }
        }

        public bool IsStatic
        {
            get
            {
                bool getStatic = _property.GetGetMethod() != null && _property.GetGetMethod().IsStatic;
                bool setStatic = _property.GetSetMethod() != null && _property.GetSetMethod().IsStatic;
                return getStatic || setStatic;
            }
        }

        #endregion


        #region Public methods

        public object[] GetCustomAttributes(bool inherit)
        {
            return _property.GetCustomAttributes(inherit);
        }

        public object GetValue(object obj)
        {
            // indexed properties not supported
            return _property.GetValue(obj, null);
        }

        public void SetValue(object obj, object value)
        {
            // indexed properties not supported
            _property.SetValue(obj, value, null);
        }

        #endregion


        #region Private, protected, internal fields

        PropertyInfo _property;
        string _xmlName = string.Empty;

        #endregion
    }



    /// <summary>
    /// Helper class that wraps a Type and caches information about it, and provides some convenient lookup 
    /// functions.
    /// </summary>
    public class TypeInfo
    {
        #region Public properties, operators, constants, and enums

        /// <summary>
        /// The Type contained in this type info. Do not change this once it is set.
        /// </summary>
        public Type Type
        {
            get { return _type; }
            set { _type = value; }
        }

        /// <summary>
        /// Returns true if the object is something that we would consider to be a "list" object (IList,
        /// TorqueComponentContainer, or array).
        /// </summary>
        public bool IsList
        {
            get
            {
                if (_isList != null)
                    return (bool)_isList;
                _isList = false;

                if (Type == null)
                    _isList = false;
                else if (Type == typeof(TorqueComponentContainer))
                    _isList = true;
                else if (Type.IsArray)
                    _isList = true;
#if !XBOX
                else if (Type.GetInterface(typeof(System.Collections.IList).Name) != null)
                    _isList = true;
                else
                    _isList = false;

#else
                else
                {
                    // do it slow way
                    Type[] interfaces = Type.GetInterfaces();
                    foreach (Type iface in interfaces)
                    {
                        if (iface == typeof(System.Collections.IList))
                        {
                            _isList = true;
                            break;
                        }
                    }
                }
#endif

                return (bool)_isList;
            }
        }

        /// <summary>
        /// Returns any deserializable fields and properties of the current type, as well as public static fields
        /// and properties (which can be the targets of ValueOf statements).
        /// </summary>
        public List<IFieldOrProperty> FieldsAndProperties
        {
            get
            {
                if (_fieldsAndProperties != null)
                    return _fieldsAndProperties;

                _fieldsAndProperties = new List<IFieldOrProperty>();
                _LoadFields();
                _LoadProperties();

                foreach (string key in _deserializableProperties.Keys)
                {
                    PropertyInfo prop = _deserializableProperties[key];
                    PropertyWrapper pw = new PropertyWrapper(prop);
                    pw.XmlName = key;
                    _fieldsAndProperties.Add(pw);
                }

                foreach (string key in _deserializableFields.Keys)
                {
                    FieldInfo field = _deserializableFields[key];
                    FieldWrapper fw = new FieldWrapper(field);
                    fw.XmlName = key;
                    _fieldsAndProperties.Add(fw);
                }

                // also include public static fields and properties
                FieldInfo[] fields = Type.GetFields(BindingFlags.Public | BindingFlags.Static);
                foreach (FieldInfo f in fields)
                {
                    IFieldOrProperty fieldOrProperty = new FieldWrapper(f);
                    fieldOrProperty.XmlName = f.Name;
                    _fieldsAndProperties.Add(fieldOrProperty);
                }

                PropertyInfo[] props = Type.GetProperties(BindingFlags.Public | BindingFlags.Static);
                foreach (PropertyInfo p in props)
                {
                    IFieldOrProperty fieldOrProperty = new PropertyWrapper(p);
                    fieldOrProperty.XmlName = p.Name;
                    _fieldsAndProperties.Add(fieldOrProperty);
                }

                return _fieldsAndProperties;
            }
        }

        #endregion


        #region Public methods

        /// <summary>
        /// Get the property of the type with the specified name.  
        /// </summary>
        /// <param name="propertyName">The property name</param>
        /// <returns>The PropertyInfo for the property, or null if no deserializable property exists.</returns>
        public PropertyInfo GetProperty(string propertyName)
        {
            if (Type == null)
                return null;

            if (_deserializableProperties == null)
                _LoadProperties();

            PropertyInfo p = null;
            _deserializableProperties.TryGetValue(propertyName, out p);
            return p;
        }

        /// <summary>
        /// Get the field of the type with the specified name.
        /// </summary>
        /// <param name="fieldName">The field name</param>
        /// <returns>The FieldInfo for the field, or null if no deserializable field exists.</returns>
        public FieldInfo GetField(string fieldName)
        {
            if (Type == null)
                return null;

            if (_deserializableFields == null)
                _LoadFields();

            FieldInfo p = null;
            _deserializableFields.TryGetValue(fieldName, out p);
            return p;
        }

        /// <summary>
        /// Get an IFieldOrProperty instance for the specified field or property.
        /// </summary>
        /// <param name="name">The field or property name</param>
        /// <returns>The IFieldOrProperty instance, or null if no deserializable field or property exists.</returns>
        public IFieldOrProperty GetFieldOrProperty(string name)
        {
            // look for property first
            PropertyInfo p = GetProperty(name);
            if (p != null)
                return new PropertyWrapper(p);

            // look for field
            FieldInfo f = GetField(name);
            if (f != null)
                return new FieldWrapper(f);

            return null;
        }

        /// <summary>
        /// Get the static function with the specified name.
        /// </summary>
        /// <param name="name">The name of the function.</param>
        /// <returns>The method info of the function.</returns>
        public MethodInfo GetStaticMethod(string name)
        {
            if (Type == null)
                return null;

            if (_staticMethods == null)
                _staticMethods = new Dictionary<string, MethodInfo>();

            MethodInfo func = null;
            if (_staticMethods.TryGetValue(name, out func))
                return func;

            func = Type.GetMethod(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, TorqueUtil.EmptyTypes, null);

            _staticMethods[name] = func;
            return func;
        }

        /// <summary>
        /// Get an IFieldOrProperty instance for the specified static field or property.
        /// </summary>
        /// <param name="name">The field or property name</param>
        /// <returns>The IFieldOrProperty instance, or null if no deserializable static field or property exists.</returns>
        public IFieldOrProperty GetStaticFieldOrProperty(string name)
        {
            if (Type == null)
                return null;

            if (_staticProperties == null)
                _staticProperties = new Dictionary<string, IFieldOrProperty>();

            IFieldOrProperty fieldOrProperty = null;
            if (_staticProperties.TryGetValue(name, out fieldOrProperty))
                return fieldOrProperty;

            // check fields
            FieldInfo[] fields = Type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            foreach (FieldInfo f in fields)
            {
                if (f.Name == name)
                {
                    fieldOrProperty = new FieldWrapper(f);
                    _staticProperties[name] = fieldOrProperty;
                    return fieldOrProperty;
                }
            }

            // check properties
            PropertyInfo[] props = Type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            foreach (PropertyInfo p in props)
            {
                if (p.Name == name)
                {
                    fieldOrProperty = new PropertyWrapper(p);
                    _staticProperties[name] = fieldOrProperty;
                    return fieldOrProperty;
                }
            }

            return fieldOrProperty;
        }

        #endregion


        #region Private, protected, internal methods

        /// <summary>
        /// Load the deserializable properties for this type.
        /// </summary>
        void _LoadProperties()
        {
            if (_deserializableProperties != null)
                return;

            _deserializableProperties = new Dictionary<string, PropertyInfo>();

            if (Type == null)
                return;

            // we have to do separate passes on both the non public and public properties, because 
            // there is no way to distinguish them in the result array if you combine it into one query.
            PropertyInfo[] properties = Type.GetProperties(BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (PropertyInfo property in properties)
                _CheckProperty(property, false);

            properties = Type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (PropertyInfo property in properties)
                _CheckProperty(property, true);
        }

        /// <summary>
        /// Examine the specified property and add it to the deserializable property list if it is valid.
        /// </summary>
        /// <param name="property">The property to examine</param>
        /// <param name="isPublic">Whether this property is public.  This information does not seem to be available from the PropertyInfo</param>
        void _CheckProperty(PropertyInfo property, bool isPublic)
        {
            if (!property.CanRead || !property.CanWrite)
                return;

            if (property.GetGetMethod(true).CallingConvention == System.Reflection.CallingConventions.Standard)
                // static property
                return;

            bool ignored = false;
            string nameRemap = property.Name;
            bool hasXmlInclude = false;

            _CheckAttributes(property.GetCustomAttributes(true), ref ignored, ref nameRemap, ref hasXmlInclude);
            if (ignored)
                return;

            // only include non public fields if they have the xml include attribute
            if (!isPublic && !hasXmlInclude)
                return;

            if (_deserializableProperties.ContainsKey(nameRemap))
                TorqueConsole.Warn("TypeUtil._CheckProperty - Already have name mapping for field, not remapping: {0} {0}", nameRemap, Type.FullName);
            else
                _deserializableProperties.Add(nameRemap, property);
        }

        /// <summary>
        /// Load the deserializable fields for this type.
        /// </summary>
        void _LoadFields()
        {
            if (_deserializableFields != null)
                return;

            _deserializableFields = new Dictionary<string, FieldInfo>();

            if (Type == null)
                return;

            FieldInfo[] fields = Type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            foreach (FieldInfo field in fields)
            {
                if (field.IsSpecialName || field.IsLiteral || field.IsInitOnly || field.IsStatic)
                    continue;

                bool ignored = false;
                string nameRemap = field.Name;
                bool hasXmlInclude = false;

                _CheckAttributes(field.GetCustomAttributes(true), ref ignored, ref nameRemap, ref hasXmlInclude);
                if (ignored)
                    continue;

                // only include non public fields if they have the xml include attribute
                if (!field.IsPublic && !hasXmlInclude)
                    continue;

                if (_deserializableFields.ContainsKey(nameRemap))
                    TorqueConsole.Warn("TypeInfo._LoadFields - Name mapping {0} already exists for type {0}.", nameRemap, Type.FullName);
                else
                    _deserializableFields.Add(nameRemap, field);
            }
        }

        /// <summary>
        /// Examines the specified Attribute array and fills out the output parameters with information 
        /// extracted from the attributes.
        /// </summary>
        /// <param name="attributes">An array of Attribute instances.  Note, these are C# attributes, not XML attributes.</param>
        /// <param name="ignored">If set to true on return, the attribute array specifies that the object should be ignored.</param>
        /// <param name="nameRemap">If set to true on return, contains the element name remap for the object.</param>
        /// <param name="hasXmlInclude">If set to true on return, the object wants to be included in the deserialization
        /// process (even though it might normally be excluded for other reasons).</param>
        void _CheckAttributes(object[] attributes, ref bool ignored, ref string nameRemap, ref bool hasXmlInclude)
        {
            ignored = false;
            hasXmlInclude = false;

            foreach (Attribute attr in attributes)
            {
                if (attr is XmlIgnoreAttribute)
                {
                    ignored = true;
                    return;
                }
                if (attr is XmlElementAttribute)
                {
                    XmlElementAttribute a = attr as XmlElementAttribute;
                    if (a.ElementName != null && a.ElementName != string.Empty)
                        nameRemap = a.ElementName;
                }
                if (attr is TorqueXmlDeserializeInclude)
                {
                    hasXmlInclude = true;
                }
            }
        }

        #endregion


        #region Private, protected, internal fields

        Type _type;
        Dictionary<string, PropertyInfo> _deserializableProperties = null;
        Dictionary<string, FieldInfo> _deserializableFields = null;
        Dictionary<string, IFieldOrProperty> _staticProperties = null;
        Dictionary<string, MethodInfo> _staticMethods = null;
        List<IFieldOrProperty> _fieldsAndProperties = null;

        // this is an object so that it can be null
        object _isList;

        #endregion
    }



    /// <summary>
    /// Utility functions for types. Mostly used by the console parser and deserializer.
    /// </summary>
    public class TypeUtil
    {
        #region Static methods, fields, constructors

        /// <summary>
        /// Returns the full name of non generic types, and the base name of generic types. In some
        /// cases the full name of generic types is undesired given that it includes the framework
        /// version and some other unnecessary information.
        /// </summary>
        /// <param name="t">The type</param>
        /// <returns>The full name of the type.</returns>
        public static string GetTypeFullName(Type t)
        {
            if (!t.IsGenericType)
                return t.FullName;

            else
            {
                // mangle the name to just contain the base type names
                string fullName = t.Name;
                string genericArgs = string.Empty;

                Type[] types = t.GetGenericArguments();
                foreach (Type arg in types)
                {
                    if (genericArgs == string.Empty)
                        genericArgs = "_Of_";
                    else
                        genericArgs += "_And_";

                    Assert.Fatal(t != arg, "TypeUtil.GetTypeFullName - Not expecting to find type (" + t.FullName + ") in generic arg list!");
                    if (t != arg)
                        genericArgs += GetTypeFullName(arg);
                }

                return fullName + genericArgs;
            }
        }

        /// <summary>
        /// Returns whether or not a type is instantiable.
        /// </summary>
        /// <param name="t">Type to check</param>
        /// <returns>True if t is instantiable</returns>
        public static bool IsInstantiable(Type t)
        {
            return t != null && t != typeof(object) && t != typeof(TorqueComponent) && !(t.IsInterface || t.IsAbstract);
        }

        /// <summary>
        /// Returns a list of all the subclasses of the specified type. This is likely very
        /// slow since it has to iterate over all types in all assemblies.
        /// </summary>
        /// <param name="baseType">The type whose subclasses are being retrieved.</param>
        /// <param name="subclasses">The list to fill with the type's subclasses.</param>
        public static void FindSubclasses(Type baseType, List<Type> subclasses)
        {
            List<Assembly> assemblies = new List<Assembly>();
            PopulateAssemblyList(assemblies);

            // iterate over all the assemblies
            foreach (Assembly a in assemblies)
            {
                Type[] types = a.GetTypes();

                // iterate over all types in the assembly
                foreach (Type t in types)
                {
                    if (t.IsSubclassOf(baseType))
                    {
                        subclasses.Add(t);
                        continue;
                    }

                    // special handling for interfaces
                    if (baseType.IsInterface)
                    {
#if !XBOX
                        if (t.GetInterface(baseType.FullName) != null)
                        {
                            subclasses.Add(t);
                            continue;
                        }
#else
                        Type[] interfaces = t.GetInterfaces();
                        foreach (Type iface in interfaces)
                        {
                            if (iface == baseType)
                            {
                                subclasses.Add(t);
                                break;
                            }
                        }
#endif
                    }
                }
            }
        }

        /// <summary>
        /// Populates the specified list with several useful assemblies that are commonly used by the
        /// engine. This includes the engine assembly, xna assembly, and game assembly. Additional
        /// assemblies can be registered with TorqueEngineComponent.Instance.RegisterAssembly.
        /// </summary>
        /// <param name="assemblies">The list to populate.</param>
        public static void PopulateAssemblyList(List<Assembly> assemblies)
        {
            // engine assembly
            if (!assemblies.Contains(typeof(GarageGames.Torque.Core.Xml.TorqueXmlDeserializer).Assembly))
                assemblies.Add(typeof(GarageGames.Torque.Core.Xml.TorqueXmlDeserializer).Assembly);

            // xna assembly
            if (!assemblies.Contains(typeof(Microsoft.Xna.Framework.Vector3).Assembly))
                assemblies.Add(typeof(Microsoft.Xna.Framework.Vector3).Assembly);

            // .net assembly
            if (!assemblies.Contains(typeof(System.Collections.IList).Assembly))
                assemblies.Add(typeof(System.Collections.IList).Assembly);

            if (TorqueEngineComponent.Instance != null)
            {
                // game assembly
                if (!assemblies.Contains(TorqueEngineComponent.Instance.ExecutableAssembly))
                    assemblies.Add(TorqueEngineComponent.Instance.ExecutableAssembly);

                // other user specified assemblies
                foreach (Assembly a in TorqueEngineComponent.Instance.RegisteredAssemblies)
                {
                    if (!assemblies.Contains(a))
                        assemblies.Add(a);
                }
            }
        }

        /// <summary>
        /// Return the TypeInfo for the specified type.
        /// </summary>
        /// <param name="name">The fully qualified name of the type to find.</param>
        /// <returns>The type info.</returns>
        public static TypeInfo FindTypeInfo(string name)
        {
            TypeInfo ti = null;

            // just grab the type if we've looked it up before.
            if (_typeLookup.TryGetValue(name, out ti))
            {
                // It is possible to find a type info with a null type. This happens when the
                // type has been searched for previously, but wasn't found. We'll search for it
                // again in case additional assemblies have been added.
                if (ti.Type != null)
                    return ti;
                else
                    _typeLookup.Remove(name);
            }

            // search assemblies
            Type t = FindType(name);
            ti = new TypeInfo();

            // t may be null at this point if not found
            ti.Type = t;

            // add to the type lookup so it can be quickly found the next time
            _typeLookup.Add(name, ti);

            // return null if type is null
            if (ti.Type == null)
                return null;
            else
                return ti;
        }

        /// <summary>
        /// Finds a type, searching all register assemblies, with the given name.
        /// </summary>
        /// <param name="name">The fully qualified name of the type to find.</param>
        /// <returns>The type, or null if it wasn't found.</returns>
        public static Type FindType(string name)
        {
            // Get the assembly list.
            List<Assembly> assemblies = new List<Assembly>();
            PopulateAssemblyList(assemblies);

#if !XBOX

            ResolveEventHandler handler = new ResolveEventHandler(

                delegate(object sender, ResolveEventArgs args)
                {
                    AssemblyName assName = new AssemblyName(args.Name);

                    foreach (Assembly assembly in assemblies)
                    {
                        var otherName = assembly.GetName();
                        if (otherName.Name == assName.Name &&
                                otherName.Version == assName.Version)
                            return assembly;
                    }

                    return null;
                }
            );

            AppDomain.CurrentDomain.AssemblyResolve += handler;

#endif // !XBOX

            // First try the easy way.
            Type t = Type.GetType(name);

#if !XBOX

            AppDomain.CurrentDomain.AssemblyResolve -= handler;

#endif // !XBOX

            if (t != null)
                return t;

            // Ok... try each assembly individually then.
            foreach (Assembly a in assemblies)
            {
                t = a.GetType(name);
                if (t != null)
                    return t;
            }

            return null;
        }

        /// <summary>
        /// Finds a type, searching all registered assemblies, with the given name. Keep in mind, this
        /// could be ambiguous if the same type is defined in multiple assemblies or namespaces. This
        /// method simply returns the first one it happens to find.
        /// </summary>
        /// <param name="name">The short name (just the class) of the type to find.</param>
        /// <returns>The type.</returns>
        public static Type FindTypeByShortName(string name)
        {
            List<Assembly> assemblies = new List<Assembly>();
            PopulateAssemblyList(assemblies);

            foreach (Assembly a in assemblies)
            {
                Type[] types = a.GetTypes();
                foreach (Type t in types)
                {
                    if (t.Name == name)
                        return t;
                }
            }

            return null;
        }

        /// <summary>
        /// Returns true if the specified type is a "primitive", at least as far as the deserializer
        /// is concerned. Types are c# primitives, DateTime, TimeSpan, Guid, and any enum.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns>Whether or not the type is considered a primitive.</returns>
        public static bool IsPrimitiveType(Type type)
        {
            return type.IsPrimitive || type == typeof(DateTime) || type == typeof(TimeSpan) ||
                   type == typeof(Guid) || type.IsEnum;
        }

        /// <summary>
        /// Returns true if the specified type is parsable by the GetPrimitiveValue method
        /// See GetPrimitiveValue for a list of the supported types.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns>Whether or not the type is parsable.</returns>
        public static bool IsParsableType(Type type)
        {
            return type.IsPrimitive || type == typeof(DateTime) || type == typeof(TimeSpan) ||
                   type == typeof(Guid) || type == typeof(Vector2) || type == typeof(Vector3) ||
                   type == typeof(Vector4) || type == typeof(Quaternion) || type.IsEnum;
        }

        /// <summary>
        /// Converts the input string into an appropriate primitive value.
        /// 
        /// Supported types and the expected format:
        /// Single - "1.0" or "-1.0"
        /// Double - "1.0" or "-1.0"
        /// SByte - "1" or "-1"
        /// Int16 - "1" or "-1"
        /// Int32 - "1" or "-1"
        /// Int64 - "1" or "-1"
        /// Byte - "1"
        /// UInt16 - "1"
        /// UInt32 - "1"
        /// UInt64 - "1"
        /// Boolean - "true" or "false"
        /// Char - "a"
        /// Guid - "F9168C5E-CEB2-4faa-B6BF-329BF39FA1E4"
        /// DateTime - "2007-08-17"
        /// TimeSpan - "00:03:08.5"
        /// Vector2 - "1.0, 1.0"
        /// Vector3 - "1.0, 1.0, 1.0"
        /// Vector4 - "1.0, 1.0, 1.0, 1.0"
        /// Quaternion - "1.57, 0.0, 0.0, 1.57"
        /// Enum - "EnumValue"
        /// 
        /// </summary>
        /// <param name="primitiveType">The type to convert to.</param>
        /// <param name="strValue">The input string.</param>
        /// <returns>An instance of the type containing the value, or null if the value can not be converted.</returns>
        public static object GetPrimitiveValue(Type primitiveType, string strValue)
        {
            Assert.Fatal(IsParsableType(primitiveType), "TypeUtil.GetPrimitiveValue - Attempt to get primitive value on type that is not a primitive.");

            // use XmlConvert to do the primitive parsing
            if (primitiveType == typeof(Single))
                return XmlConvert.ToSingle(strValue);

            else if (primitiveType == typeof(Double))
                return XmlConvert.ToDouble(strValue);

            else if (primitiveType == typeof(SByte))
                return XmlConvert.ToSByte(strValue);

            else if (primitiveType == typeof(Int16))
                return XmlConvert.ToInt16(strValue);

            else if (primitiveType == typeof(Int32))
                return XmlConvert.ToInt32(strValue);

            else if (primitiveType == typeof(Int64))
                return XmlConvert.ToInt64(strValue);

            else if (primitiveType == typeof(Byte))
                return XmlConvert.ToByte(strValue);

            else if (primitiveType == typeof(UInt16))
                return XmlConvert.ToUInt16(strValue);

            else if (primitiveType == typeof(UInt32))
                return XmlConvert.ToUInt32(strValue);

            else if (primitiveType == typeof(UInt64))
                return XmlConvert.ToUInt64(strValue);

            else if (primitiveType == typeof(Boolean))
                return XmlConvert.ToBoolean(strValue);

            else if (primitiveType == typeof(Char))
                return XmlConvert.ToChar(strValue);

            else if (primitiveType == typeof(Guid))
                return XmlConvert.ToGuid(strValue);

            else if (primitiveType == typeof(DateTime))
                return Convert.ToDateTime(strValue);

            // XmlConvert does not seem to understand values produced by TimeSpan.ToString(). Documentation
            // does not specify what format it expects.
            else if (primitiveType == typeof(TimeSpan))
                return TimeSpan.Parse(strValue);

            // Vectors can have each of their elements parsed separately, but this makes things easier, particularly
            // in the console.
            else if (primitiveType == typeof(Vector2))
            {
                char[] delim = { ',' };
                string[] split = strValue.Split(delim);
                if (split.Length != 2)
                    throw new FormatException();

                return new Vector2(XmlConvert.ToSingle(split[0]), XmlConvert.ToSingle(split[1]));
            }
            else if (primitiveType == typeof(Vector3))
            {
                char[] delim = { ',' };
                string[] split = strValue.Split(delim);
                if (split.Length != 3)
                    throw new FormatException();

                return new Vector3(XmlConvert.ToSingle(split[0]), XmlConvert.ToSingle(split[1]), XmlConvert.ToSingle(split[2]));
            }
            else if (primitiveType == typeof(Vector4))
            {
                char[] delim = { ',' };
                string[] split = strValue.Split(delim);
                if (split.Length != 4)
                    throw new FormatException();

                return new Vector4(XmlConvert.ToSingle(split[0]), XmlConvert.ToSingle(split[1]), XmlConvert.ToSingle(split[2]), XmlConvert.ToSingle(split[3]));
            }
            else if (primitiveType == typeof(Quaternion))
            {
                char[] delim = { ',' };
                string[] split = strValue.Split(delim);
                if (split.Length != 3)
                    throw new FormatException();

                Quaternion quat = Quaternion.CreateFromYawPitchRoll(XmlConvert.ToSingle(split[0]), XmlConvert.ToSingle(split[1]), XmlConvert.ToSingle(split[2]));
                return quat;
            }
            else if (primitiveType.IsEnum)
                return GetEnumValue(primitiveType, strValue);

            return null;
        }

        /// <summary>
        /// Converts the input string into an appropriate enum value.
        /// </summary>
        /// <param name="enumType">The type of the enum to use for the conversion.</param>
        /// <param name="strValue">The input enum string.  This should match the name in the enum definition.</param>
        /// <returns>The result of calling Enum.Parse on the input type with the specified string.</returns>
        public static object GetEnumValue(Type enumType, string strValue)
        {
            Assert.Fatal(enumType.IsEnum, "TypeUtil.GetEnumValue - Attempting to get enum value on type that is not an enum.");
            return Enum.Parse(enumType, strValue, false);
        }

        /// <summary>
        /// Given a type string, try to parse the base type and target type.  
        /// </summary>
        /// <param name="typeRef">The type string to parse.</param>
        /// <param name="baseType">The base type of the type string. This might be a namespace, or a class if the target is an inner class.</param>
        /// <param name="target">The name of the inner class or member field.</param>
        /// <returns>True if the type was parsed successfully.</returns>
        public static bool ParseType(string typeRef, out string baseType, out string target)
        {
            baseType = string.Empty;
            target = string.Empty;

            typeRef = typeRef.Trim();
            int dotIdx = typeRef.LastIndexOf(".");

            if (string.IsNullOrEmpty(typeRef) || typeRef.StartsWith(".") || typeRef.EndsWith(".") || (dotIdx == -1))
                return false;

            target = typeRef.Substring(dotIdx + 1).Trim();
            baseType = typeRef.Substring(0, dotIdx).Trim();
            return true;
        }

        static Dictionary<string, TypeInfo> _typeLookup = new Dictionary<string, TypeInfo>();

        #endregion
    }
}
