//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using System.Globalization;
using System.Reflection;
using GarageGames.Torque.XNA;
using GarageGames.Torque.Util;



namespace GarageGames.Torque.Core.Xml
{
    /// <summary>
    /// Class for storing information about a type that is being exported to
    /// the xml schema.
    /// </summary>
    internal class ExportedType : IComparable
    {
        #region Public methods

        public int CompareTo(object other)
        {
            XmlElement thisElem = this.Element;
            XmlElement otherElem = (other as ExportedType).Element;

            // if both types are null, they are equal
            if (thisElem == null && otherElem == null)
                return 0;
            // if one type is null but the other isn't, null types are come before non-null types
            else if (thisElem == null && otherElem != null)
                return -1;
            else if (thisElem != null && otherElem == null)
                return 1;

            // neither are null, compare the names
            string thisName = this.Element.Attributes.GetNamedItem("name").Value;
            string otherName = (other as ExportedType).Element.Attributes.GetNamedItem("name").Value;

            return string.Compare(thisName, otherName, StringComparison.InvariantCultureIgnoreCase);
        }

        #endregion


        #region Private, protected, and internal fields

        /// <summary>
        /// The type info for the type associated with this.
        /// </summary>
        internal TypeInfo TypeInfo;



        /// <summary>
        /// The root xml element for this type.
        /// </summary>
        internal XmlElement Element;



        /// <summary>
        /// True if this is an explicit type, false if it is a referenced type or undetermined type.
        /// </summary>
        internal bool Explicit;



        /// <summary>
        /// True if types derived from this one have already been exported.
        /// </summary>
        internal bool DerivedTypesExported;

        #endregion
    }



    /// <summary>
    /// Defines various constant values to be used as default values for schema types.
    /// </summary>
    public class TorqueXmlSchemaDefaults
    {
        #region Public constants

        public const string Vector2Zero = "0 0";
        public const string BoolTrue = "true";
        public const string BoolFalse = "false";

        #endregion
    }



    /// <summary>
    /// Class for creating the Torque X xml schema for any TorqueXmlSchemaType
    /// attributed classes in all registered assemblies and dumping it to a file.
    /// </summary>
    public class TorqueXmlSchemaExporter
    {
        #region Public methods

        /// <summary>
        /// Builds and exports the xml schema.
        /// </summary>
        /// <param name="outputFileName">The filename to write the schema to.</param>
        public void Process(string outputFileName)
        {
#if DEBUG
            Profiler.Instance.StartBlock("TorqueXmlSchemaExporter.Process");
#endif

            List<Assembly> assemblies = new List<Assembly>();
            TypeUtil.PopulateAssemblyList(assemblies);

            _doc = new XmlDocument();
            _root = _doc.CreateElement("TorqueSchema");
            _doc.AppendChild(_root);

            _explicitTypesNode = _doc.CreateElement("ExplicitTypes");
            _root.AppendChild(_explicitTypesNode);
            _referencedTypesNode = _doc.CreateElement("ReferencedTypes");
            _root.AppendChild(_referencedTypesNode);

            foreach (Assembly a in assemblies)
            {
                Type[] types = a.GetTypes();

                foreach (Type t in types)
                {
                    // skip anthing that isn't an instantiable reference type
                    if (!t.IsClass || t.IsInterface || t.IsAbstract)
                        continue;

                    // skip it unless it has a TorqueXmlSchemaType attribute
                    object[] attrs = t.GetCustomAttributes(false);

                    bool hasSchemaAttr = false;

                    foreach (object attr in attrs)
                    {
                        if (attr is TorqueXmlSchemaType)
                        {
                            hasSchemaAttr = true;
                            break;
                        }
                    }

                    if (!hasSchemaAttr)
                        continue;

                    // export it
                    _Export(t, true, false);
                }
            }

            ExportedType[] exportedTypes = new ExportedType[_processedTypes.Values.Count];
            _processedTypes.Values.CopyTo(exportedTypes, 0);

            // sort the types so that the CRC of the schema won't change unless new types are added
            Array.Sort(exportedTypes);

            // add nodes to the appropriate root - explicit or referenced
            foreach (ExportedType typeInfo in exportedTypes)
            {
                if (typeInfo.Element != null)
                {
                    if (typeInfo.Explicit)
                        _explicitTypesNode.AppendChild(typeInfo.Element);
                    else
                        _referencedTypesNode.AppendChild(typeInfo.Element);
                }
            }

            // save document
            _doc.Save(outputFileName);

#if DEBUG
            Profiler.Instance.EndBlock("TorqueXmlSchemaExporter.Process");
#endif
        }

        #endregion


        #region Private, protected, internal methods

        bool _Export(Type t, bool explicitlyExported, bool exportSubclasses)
        {
            try
            {
                ExportedType exportInfo;
                if (_processedTypes.TryGetValue(t, out exportInfo))
                {
                    // can't make a type un-explicit after it has been explicitized
                    if (explicitlyExported && !exportInfo.Explicit)
                        exportInfo.Explicit = explicitlyExported;

                    if (exportSubclasses)
                    {
                        // if the type was already exported, make sure it has the derived type information
                        if (exportInfo.Element != null)
                            _ExportDerivedTypes(exportInfo);
                    }

                    return exportInfo.Element != null;
                }

                // create the new exported type and mark it as exported
                exportInfo = new ExportedType();

                if (_processedTypes.ContainsKey(t))
                    _processedTypes.Remove(t);

                _processedTypes.Add(t, exportInfo);

                exportInfo.Explicit = explicitlyExported;

                // see if this type has a TorqueXmlSchemaType attribute, not inheriting it from parents
                object[] tCAttrs = t.GetCustomAttributes(typeof(TorqueXmlSchemaType), false);
                TorqueXmlSchemaType xmlTypeAttr = null;

                if (tCAttrs.Length > 0)
                    xmlTypeAttr = tCAttrs[0] as TorqueXmlSchemaType;

                string typeName = t.Name;

                if (xmlTypeAttr != null)
                {
                    // is exporting disabled?
                    if (!xmlTypeAttr.ExportType)
                        return false;

                    // remap type name?
                    if (!string.IsNullOrEmpty(xmlTypeAttr.Name))
                        typeName = xmlTypeAttr.Name;
                }

                // check for hidden fields
                Dictionary<string, bool> fieldExport = new Dictionary<string, bool>();
                // we include field declarations from the parent classes, so that hidden fields do not need to be rehidden in each subclass
                object[] fieldAttrs = t.GetCustomAttributes(typeof(TorqueXmlSchemaField), true);
                // iterate back to front so that the most derived attribute for a given field name is used
                Array.Reverse(fieldAttrs);

                foreach (TorqueXmlSchemaField txsf in fieldAttrs)
                {
                    if (string.IsNullOrEmpty(txsf.Name))
                        throw new Exception("Name required for TorqueXmlSchemaField, type: " + t.FullName);

                    if (!fieldExport.ContainsKey(txsf.Name))
                        fieldExport[txsf.Name] = false;

                    fieldExport[txsf.Name] = txsf.ExportField;
                }

                XmlElement el = null;
                TypeInfo ti = TypeUtil.FindTypeInfo(t.FullName);
                Assert.Fatal(ti != null, "Can't load type " + t.FullName);
                exportInfo.TypeInfo = ti;

                List<IFieldOrProperty> fieldsAndProperties = ti.FieldsAndProperties;

                XmlElement typeNode = _doc.CreateElement("Type");
                exportInfo.Element = typeNode;

                XmlAttribute attr = _doc.CreateAttribute("name");
                attr.Value = typeName;
                typeNode.Attributes.Append(attr);

                attr = _doc.CreateAttribute("fullName");
                attr.Value = TypeUtil.GetTypeFullName(t);
                typeNode.Attributes.Append(attr);

                // write out any dependencies
                el = _doc.CreateElement("Dependencies");
                tCAttrs = t.GetCustomAttributes(true);
                List<String> depTypes = new List<string>();

                foreach (object tcAttr in tCAttrs)
                {
                    if (tcAttr is TorqueXmlSchemaDependency)
                    {
                        TorqueXmlSchemaDependency depAttr = tcAttr as TorqueXmlSchemaDependency;
                        if (depAttr.Type != null)
                        {
                            depTypes.Add(TypeUtil.GetTypeFullName(depAttr.Type));

                        }
                    }
                }

                depTypes.Sort();

                foreach (string depTypeName in depTypes)
                {
                    XmlElement depType = _doc.CreateElement("Type");
                    depType.InnerText = depTypeName;
                    el.AppendChild(depType);
                }

                // append dependency list only if it has something in it
                if (el.ChildNodes.Count > 0)
                    typeNode.AppendChild(el);

                if (exportSubclasses)
                    _ExportDerivedTypes(exportInfo);

                // write out component flag
                if (ti.Type.IsSubclassOf(typeof(TorqueComponent)))
                {
                    el = _doc.CreateElement("IsComponent");
                    el.InnerText = TorqueXmlSchemaDefaults.BoolTrue;
                    typeNode.AppendChild(el);
                }

                // write out delegate flag if its a delegate
                bool isDelegate = ti.Type.IsSubclassOf(typeof(System.Delegate));

                if (isDelegate)
                {
                    el = _doc.CreateElement("IsDelegate");
                    el.InnerText = isDelegate ? TorqueXmlSchemaDefaults.BoolTrue : TorqueXmlSchemaDefaults.BoolFalse;
                    typeNode.AppendChild(el);
                }

                // write out enum flag
                if (ti.Type.IsEnum)
                {
                    el = _doc.CreateElement("IsEnum");
                    el.InnerText = ti.Type.IsEnum ? TorqueXmlSchemaDefaults.BoolTrue : TorqueXmlSchemaDefaults.BoolFalse;
                    typeNode.AppendChild(el);
                }

                // write out instantiable flag, but only if it is not instantiable
                bool isInstantiable = !isDelegate && TypeUtil.IsInstantiable(ti.Type);

                if (!isInstantiable)
                {
                    el = _doc.CreateElement("IsInstantiable");
                    el.InnerText = isInstantiable ? TorqueXmlSchemaDefaults.BoolTrue : TorqueXmlSchemaDefaults.BoolFalse;
                    typeNode.AppendChild(el);
                }

                // write out deserializable elements (fields and properties)
                foreach (IFieldOrProperty fieldOrProperty in fieldsAndProperties)
                {
                    // skip it?
                    bool export = true;

                    if (fieldExport.TryGetValue(fieldOrProperty.XmlName, out export))
                        if (!export)
                            continue;

                    XmlElement elemNode = _doc.CreateElement("Element");
                    typeNode.AppendChild(elemNode);

                    // write static flag
                    if (fieldOrProperty.IsStatic)
                    {
                        el = _doc.CreateElement("IsStatic");
                        el.InnerText = TorqueXmlSchemaDefaults.BoolTrue;
                        elemNode.AppendChild(el);
                    }

                    // retrieve XML schema attribute, if any
                    TorqueXmlSchemaType xmlAttr = null;

                    object[] cAttrs = fieldOrProperty.GetCustomAttributes(true);
                    foreach (object cAttr in cAttrs)
                    {
                        if (cAttr is TorqueXmlSchemaType)
                        {
                            xmlAttr = cAttr as TorqueXmlSchemaType;
                            break;
                        }
                    }

                    attr = _doc.CreateAttribute("name");
                    attr.Value = fieldOrProperty.XmlName;
                    elemNode.Attributes.Append(attr);

                    // is this thingy a list?
                    TypeInfo fieldTI = TypeUtil.FindTypeInfo(fieldOrProperty.DeclaredType.FullName);
                    Assert.Fatal(fieldTI != null, "Can't find type for element: " + fieldOrProperty.DeclaredType.FullName);
                    XmlElement child = null;
                    if (fieldTI != null && fieldTI.IsList)
                    {
                        bool exportedAListType = false;

                        el = _doc.CreateElement("IsList");
                        el.InnerText = TorqueXmlSchemaDefaults.BoolTrue;
                        elemNode.AppendChild(el);

                        // append Type elements for all of the possible types of the list
                        Type listType = DeserializerUtil.GetListType(fieldTI.Type);
                        // if it is a list, don't export it
                        TypeInfo listTI = TypeUtil.FindTypeInfo(listType.FullName);
                        if (listTI != null && !listTI.IsList)
                        {
                            exportedAListType = true;

                            el = _doc.CreateElement("Type");
                            el.InnerText = TypeUtil.GetTypeFullName(listType);

                            if (_Export(listType, false, true))
                                elemNode.AppendChild(el);
                        }

                        Assert.Fatal(exportedAListType, String.Format("Error, did not export any list element types for field {0} in type {1}", fieldOrProperty.XmlName, ti.Type.FullName));
                    }
                    else
                    {
                        child = _doc.CreateElement("Type");
                        child.InnerText = TypeUtil.GetTypeFullName(fieldOrProperty.DeclaredType);
                        elemNode.AppendChild(child);
                    }

                    if (xmlAttr != null)
                    {
                        if (xmlAttr.DefaultValue != null)
                        {
                            child = _doc.CreateElement("DefaultValue");
                            child.InnerText = xmlAttr.DefaultValue;
                            elemNode.AppendChild(child);
                        }

                        if (xmlAttr.IsDefaultValueOf)
                        {
                            child = _doc.CreateElement("DefaultIsValueOf");
                            child.InnerText = TorqueXmlSchemaDefaults.BoolTrue;
                            elemNode.AppendChild(child);
                        }
                    }

                    // don't export list types since we already handled them
                    if (fieldTI == null || !fieldTI.IsList)
                        _Export(fieldOrProperty.DeclaredType, false, false);
                }

                return exportInfo.Element != null;
            }
            catch (Exception)
            {
                return false;
            }
        }



        void _ExportDerivedTypes(ExportedType exportedType)
        {
            if (exportedType == null)
                return;

            if (exportedType.DerivedTypesExported)
                return;

            XmlNode typeNode = exportedType.Element;

            if (typeNode == null)
                return;

            if (exportedType.TypeInfo == null)
                return;

            Type t = exportedType.TypeInfo.Type;

            if (t == null)
                return;

            if (t == typeof(System.Object))
                // don't export all subclasses of System.Object. likely to result in a large list full of irrelevant stuff (ha ha)
                return;

            List<Type> subclasses = new List<Type>();
            TypeUtil.FindSubclasses(t, subclasses);

            if (subclasses.Count == 0)
                return;

            // mark that we are exporting the derived types for this type, so that if we re-enter, we don't get multiple DerivedType nodes
            exportedType.DerivedTypesExported = true;

            // write out derived types (subclasses and interface implementors)
            XmlElement el = _doc.CreateElement("DerivedTypes");

            List<string> derivedTypeNames = new List<string>();
            foreach (Type subClassType in subclasses)
            {
                if (!TypeUtil.IsInstantiable(subClassType)) // skip non-instantiable types, since we can't deserialize them
                    continue;

                string subTypeName = TypeUtil.GetTypeFullName(subClassType);

                // export the derived type 
                if (_Export(subClassType, false, false))
                    derivedTypeNames.Add(subTypeName);
            }

            derivedTypeNames.Sort();
            // append derived types only if it has something in it
            if (derivedTypeNames.Count > 0)
            {
                foreach (string name in derivedTypeNames)
                {
                    XmlElement subTypeEl = _doc.CreateElement("Type");
                    subTypeEl.InnerText = name;
                    el.AppendChild(subTypeEl);
                }

                typeNode.AppendChild(el);
            }
        }

        #endregion


        #region Private, protected, internal fields

        Dictionary<Type, ExportedType> _processedTypes = new Dictionary<Type, ExportedType>();

        XmlDocument _doc;
        XmlElement _root;
        XmlElement _explicitTypesNode;
        XmlElement _referencedTypesNode;

        #endregion
    }
}
