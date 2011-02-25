//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;



namespace GarageGames.Torque.Core
{
    /// <summary>
    /// An attribute place on properties which should not be copied during clone operation.  Note that
    /// copy of properties must be done manually in CopyTo method.  This attribute only guarantees that TestCopy
    /// ignores properties with this attribute on them.
    /// </summary>
    public class TorqueCloneIgnore : Attribute
    {
    }



    /// <summary>
    /// An attribute placed on properties which should be deeply copied during clone operation.  Note that
    /// copy of property must be done manually in CopyTo method.  This attribute only guarantees that TestCopy
    /// properly tests the copy operation for the property.
    /// </summary>
    public class TorqueCloneDeep : Attribute
    {
    }



    /// <summary>
    /// Specifies that if a given object field already has a value prior to xml deserialization (for instance, an
    /// instance set in the constructor), the deserializer should deserialize into that instance and not into
    /// a new instance as it would normally do.  
    /// </summary>
    public class TorqueXmlDeserializeInPlace : Attribute
    {
    }



    /// <summary>
    /// Specifies that the deserialize should include a field or property even if thought it might normally be excluded
    /// due to non-public accessors.
    /// </summary>
    public class TorqueXmlDeserializeInclude : Attribute
    {
    }



    /// <summary>
    /// Specifies that the given type should be included in the Torque schema export process.  Exporting a type
    /// makes it accessible to the TXB editor.  This attribute is valid on classes, fields, and properties.
    /// </summary>
    public class TorqueXmlSchemaType : Attribute
    {
        /// <summary>
        /// String default value of this type.  Only valid for fields and properties.
        /// </summary>
        public string DefaultValue
        {
            get { return _defaultValue; }
            set { _defaultValue = value; }
        }



        /// <summary>
        /// Specifies whether the default value should be processed using a "valueOf" statement in the XML.
        /// </summary>
        public bool IsDefaultValueOf
        {
            get { return _isDefaultValueOf; }
            set { _isDefaultValueOf = value; }
        }



        /// <summary>
        /// Specifies an alternate name for the type.  Only valid on Types, not fields and properties.
        /// </summary>
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }



        /// <summary>
        /// Controls whether this type is exported.  Default is true.  Only valid on Types, not fields and properties
        /// (Use XmlIgnore on a field or property to prevent it from being exported.)
        /// </summary>
        public bool ExportType
        {
            get { return _exportType; }
            set { _exportType = value; }
        }



        string _name;
        string _defaultValue;
        bool _isDefaultValueOf;
        bool _exportType = true;
    }



    /// <summary>
    /// Used to indicate a dependency between types, for instance components can depend on the presence of other
    /// components in order to operate properly.  Specifying dependencies allows the editor to enforce these 
    /// relationships properly.  This attribute is valid on Types only.
    /// </summary>
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    public class TorqueXmlSchemaDependency : Attribute
    {
        /// <summary>
        /// The Type that this Type is dependent on.  
        /// </summary>
        public Type Type
        {
            get { return _type; }
            set { _type = value; }
        }

        Type _type;
    }



    /// <summary>
    /// This attribute can be used to hide fields from export in a class.
    /// 
    /// This attribute is valid when placed on class and interface types.  All other types are ignored (including fields and properties)
    /// 
    /// When placed on a class, any fields of the class whose name equals the value of the Name attribute will be exported only if the Export
    /// property is true.  This allows one to hide fields from a parent class without redeclaring them in a subclass.  If multiple attributes
    /// of this type are placed on a class, multiple fields can be hidden in this manner.  
    /// 
    /// The name attribute will match the Xml Name of the field in the case where the field name has been remapped with an XmlElement tag.
    /// 
    /// If, in a particular inheritance chain, a field has been
    /// exported and unexported multiple times, the most-derived attribute will be used to control the final export status.  For instance, 
    /// given the inheritence chain A <- B <- C <- D (D derives from C which derives from B which derives from A), field X has the following status
    /// A:X = exported, B:X = not exported, C:X = exported, D:X = not specified.  In this case, X will be exported in A, C, and D, but not B.
    /// </summary>
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    public class TorqueXmlSchemaField : Attribute
    {
        /// <summary>
        /// The Name of the field targeted by this attribute.  Ignored when this attribute is placed on a Field or Property.
        /// </summary>
        public string Name
        {
            set { _name = value; if (_name == null) _name = string.Empty; }
            get { return _name; }
        }



        /// <summary>
        /// Whether this field should be exported. Default value is true.
        /// </summary>
        public bool ExportField
        {
            set { _export = value; }
            get { return _export; }
        }



        string _name = string.Empty;
        bool _export = true;
    }
}


// NOTE: The following are dummy classes intended to allow attributes
// which are not supported on the .Net Compact Framework to build on
// the Xbox.

#if XBOX

namespace System.Drawing.Design
{
    public class UITypeEditor
    {
    };
}

namespace System.ComponentModel
{
    public class BrowsableAttribute : Attribute
    {
        public BrowsableAttribute(bool browsable)
        {
        }
    }

    public class CategoryAttribute : Attribute
    {
        public CategoryAttribute(string category)
        {
        }
    }

    public class EditorAttribute : Attribute
    {
        public EditorAttribute(string typeName, Type baseType)
        {
        }
    }
}

#endif // XBOX