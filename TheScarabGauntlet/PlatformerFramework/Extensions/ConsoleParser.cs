//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Xml;
using System.Xml.Serialization;
using GarageGames.Torque.XNA;
using GarageGames.Torque.Core;
using GarageGames.Torque.Core.Xml;
using GarageGames.Torque.Util;
using Microsoft.Xna.Framework;



namespace GarageGames.Torque.PlatformerFramework
{
    /// <summary>
    /// Helper class for parsing text typed into the console.
    /// </summary>
    public class ConsoleParser
    {
        #region Static methods, fields, constructors

        /// <summary>
        /// Parses a string and executes it. This can be used to get or set the value of a property on the PC only. The format of
        /// the input string should be:
        /// 
        /// Get the value of a property on a named TorqueObject:
        /// [ObjectName].Property
        /// 
        /// Set the value of a property on a named TorqueObject:
        /// [ObjectName].Property = value
        /// 
        /// Get the value of a property on a singleton class:
        /// [ClassName].Instance.Property
        /// 
        /// Set the value of a property on a singleton class:
        /// [ClassName].Instance.Property = value
        /// 
        /// If a property is an object, it can have it's properties set or retrieved also:
        /// [ObjectName].Property.Property = value
        /// 
        /// Rules for what the input strings for 'value' are in the above examples can be found in TypeUtil.GetPrimitiveValue.
        /// 
        /// </summary>
        /// <param name="text">The text to parse.</param>
        /// <param name="error">Any errors that may have happened.</param>
        /// <returns>True if the text was parsed successfully, false otherwise.</returns>
        static public bool ParseText(string text, ref string error)
        {
#if !XBOX
            // disregard spaces
            text = text.Replace(" ", string.Empty);

            // First check if the user is trying to call a custom console routine.
            // Syntax for calling a defined routine is just "routineName(param1, param2)".
            // Any number of params may be added.
            if (text.Contains("("))
            {
                // Make sure an end parenthesis was given
                if (!text.Contains(")") || (text.IndexOf("(") > text.IndexOf(")")) || !char.IsLetter(text[0]))
                {
                    error = "Invalid syntax for calling custom routine.  Correct syntax is 'routineName(param1, param2)'";
                    return false;
                }

                // Break up the parameter list into an array
                int length = text.IndexOf(")") - (text.IndexOf("(") + 1);
                string[] paramList = null;
                if (length > 0)
                    paramList = text.Substring(text.IndexOf("(") + 1, length).Split(',');

                // Call the requested routine
                string routineName = text.Substring(0, text.IndexOf("("));
                return CustomConsoleRoutinePool.Instance.RunRoutine(routineName, out error, paramList);
            }

            // find the first dot
            int firstDot = text.IndexOf('.');

            // no dots, just use the whole string
            if (firstDot == -1)
                firstDot = text.Length;

            object obj = null;
            PropertyInfo prop = null;
            Type t = null;

            // First iteration. Find an object by name or a global singleton object.
            string objectName = text.Substring(0, firstDot);
            TorqueBase torqueObj = TorqueObjectDatabase.Instance.FindObject(objectName);
            if (torqueObj != null)
            {
                obj = (object)torqueObj;
            }
            else
            {
                // didn't find an object, try to find a type
                t = TypeUtil.FindTypeByShortName(objectName);
                if (t == null)
                {
                    error = "No objects or types found with the name " + objectName;
                    return false;
                }

                prop = t.GetProperty("Instance");

                if (prop == null)
                {
                    error = "Static property \"Instance\" not found on type " + t.ToString();
                    return false;
                }

                obj = prop.GetValue(null, null);
            }

            // cut the first object from the string
            if (firstDot + 1 > text.Length)
                firstDot = text.Length - 1;
            text = text.Substring(firstDot + 1);

            if (text == string.Empty)
            {
                if (obj != null)
                {
                    TorqueConsole.Echo(obj.ToString());
                    return true;
                }
                else if (t != null)
                {
                    TorqueConsole.Echo(t.ToString());
                    return true;
                }
            }

            // grab the position of the equals sign, if it exists
            int equals = text.IndexOf('=');

            // Loop through all the '.' in the string. Between each '.' should be either
            // a property that returns an object or a component name on the object.
            for (int dot = text.IndexOf('.'); dot != -1 && ((equals == -1) || dot < equals); text = text.Substring(dot + 1), dot = text.IndexOf('.'), equals = text.IndexOf('='))
            {
                Type tt = t;
                if (obj != null)
                    tt = obj.GetType();

                string property = text.Substring(0, dot);
                prop = tt.GetProperty(property);

                if ((prop == null) && (obj == null))
                {
                    error = "Property " + property + " not found on type " + tt.ToString();
                    return false;
                }

                if (prop != null)
                {
                    obj = prop.GetValue(obj, null);
                    if (obj == null)
                    {
                        error = "Failed to read property " + property;
                        return false;
                    }
                }
                else
                {
                    // not a property, look for a component
                    Type componentType = TypeUtil.FindTypeByShortName(property);

                    // make sure the type is valid and a TorqueComponent
                    if ((componentType == null) || (!componentType.IsSubclassOf(typeof(TorqueComponent))))
                    {
                        error = "Could not find a property with name \"" + property + "\" or a component of type \"" + property + "\"";
                        return false;
                    }

                    TorqueObject tobj = obj as TorqueObject;
                    if (tobj == null)
                    {
                        error = "The specified object is not a TorqueObject and thus can't have components";
                        return false;
                    }

                    // find the component on the object
                    TorqueComponent component = null;
                    foreach (TorqueComponent comp in tobj.Components)
                    {
                        if (comp.GetType() == componentType)
                        {
                            component = comp;
                            break;
                        }
                    }
                    if (component == null)
                    {
                        error = "The object does not have a component of type " + property;
                        return false;
                    }

                    obj = component;
                }
            }

            // the last part of the string has to be a property rather than a component
            string propName = text;
            if (equals != -1)
                propName = text.Substring(0, equals);

            if (obj == null)
                prop = t.GetProperty(propName);
            else
                prop = obj.GetType().GetProperty(propName);

            if (prop == null)
            {
                // not a property, look for a component
                Type componentType = TypeUtil.FindTypeByShortName(propName);

                // make sure the type is valid and a TorqueComponent
                if ((componentType == null) || (!componentType.IsSubclassOf(typeof(TorqueComponent))))
                {
                    error = "Could not find a property with name \"" + propName + "\" or a component of type \"" + propName + "\"";
                    return false;
                }

                TorqueObject tobj = obj as TorqueObject;
                if (tobj == null)
                {
                    error = "The specified object is not a TorqueObject and thus can't have components";
                    return false;
                }

                // find the component on the object
                TorqueComponent component = null;
                foreach (TorqueComponent comp in tobj.Components)
                {
                    if (comp.GetType() == componentType)
                    {
                        component = comp;
                        break;
                    }
                }

                if (component == null)
                {
                    error = "The object does not have a component of type " + propName;
                    return false;
                }

                TorqueConsole.Echo(component.GetType().ToString());
                return true;
            }

            if (prop == null)
            {
                error = "Could not find a property with name \"" + propName + "\" or a component of type \"" + propName + "\"";
                return false;
            }

            try
            {
                // now we have the last object and property before the equals sign or the end of the string
                if (equals < 0)
                {
                    // get and echo the property value
                    object ret = prop.GetValue(obj, null);
                    TorqueConsole.Echo("{0}", ret);
                    return true;
                }
                else
                {
                    // get the value string, which should be everything after the equals
                    string value = text.Substring(equals + 1);
                    Type propType = prop.PropertyType;

                    // convert the string to the primitive type required by the property
                    object valObj;
                    try
                    {
                        valObj = TypeUtil.GetPrimitiveValue(propType, value);
                    }
                    catch (Exception)
                    {
                        TorqueConsole.Error("ConsoleParser.ParseText - Unable to parse format string {0} for type {1}.", value, propType.FullName);
                        return false;
                    }

                    // set it
                    if (valObj != null)
                    {
                        prop.SetValue(obj, valObj, null);
                        return true;
                    }
                    else
                    {
                        TorqueConsole.Error("ConsoleParser.ParseText - Invalid property type.");
                        return false;
                    }
                }
            }
            catch
            {
                error = "Unable to parse the property value for type: " + prop.PropertyType.ToString();
                return false;
            }
#else
            Assert.Fatal(false, "ConsoleParser.ParseText - This can't be used on the XBox!");
            return false;
#endif
        }

        #endregion
    }
}
