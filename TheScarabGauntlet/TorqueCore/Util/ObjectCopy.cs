//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Reflection;
using GarageGames.Torque.Core;



namespace GarageGames.Torque.Util
{
    /// <summary>
    /// Utility class for determining if an object was copied in entirity (i.e tests to make sure
    /// a CopyTo method copies all necessary fields and properties).
    /// </summary>
    internal class TestObjectCopy
    {
        /// <summary>
        /// Delegate for property get methods.
        /// </summary>
        /// <typeparam name="S">The property type.</typeparam>
        /// <typeparam name="T">The object type containing the property.</typeparam>
        /// <param name="t">The object.</param>
        /// <returns>The property value.</returns>
        delegate S AGetMethod<S, T>(T t);

        /// <summary>
        /// Delegate for testing a property.
        /// </summary>
        /// <param name="to">The object being copied to.</param>
        /// <param name="from">The object being copied from.</param>
        /// <param name="getDelegate">The delegate to get the property value.</param>
        /// <param name="propInfo">Info about the property.</param>
        /// <param name="error">Error information.</param>
        /// <returns>True if the property is the same on both objects.</returns>
        delegate bool TestProperty(object to, object from, Delegate getDelegate, PropertyInfo propInfo, ref ErrorRecord error);

        struct TestPropertyStruct
        {
            #region Static methods, fields, constructors

            /// <summary>
            /// Generates a test structure for a given property.
            /// </summary>
            /// <typeparam name="S"></typeparam>
            /// <typeparam name="T"></typeparam>
            /// <param name="propInfo"></param>
            /// <param name="getsetInfo"></param>
            /// <param name="i"></param>
            public static void MakeTest<S, T>(ref PropertyInfo propInfo, TestPropertyStruct[] getsetInfo, int i)
            {
#if XBOX
                Assert.Fatal(false, "TestObjectCopy.MakeTest - Doesn't work on xbox");
#else
                MethodInfo getMethodInfo = propInfo.GetGetMethod();
                AGetMethod<S, T> getMethod = (AGetMethod<S, T>)Delegate.CreateDelegate(typeof(AGetMethod<S, T>), getMethodInfo);
                getsetInfo[i].getDelegate = getMethod;
                getsetInfo[i].testProperty = TestPropMethod<S, T>;
#endif
            }

            /// <summary>
            /// Generates a test structure for a given structure for deep copy tests.
            /// </summary>
            /// <typeparam name="S"></typeparam>
            /// <typeparam name="T"></typeparam>
            /// <param name="propInfo"></param>
            /// <param name="getsetInfo"></param>
            /// <param name="i"></param>
            public static void MakeTestDeep<S, T>(ref PropertyInfo propInfo, TestPropertyStruct[] getsetInfo, int i)
            {
#if XBOX
                Assert.Fatal(false, "TestObjectCopy.MakeTestDeep - Doesn't work on xbox");
#else
                MethodInfo getMethodInfo = propInfo.GetGetMethod();
                AGetMethod<S, T> getMethod = (AGetMethod<S, T>)Delegate.CreateDelegate(typeof(AGetMethod<S, T>), getMethodInfo);
                getsetInfo[i].getDelegate = getMethod;
                getsetInfo[i].testProperty = TestPropDeepMethod<S, T>;
#endif
            }

            #endregion


            #region Public properties, operators, constants, and enums

            public Delegate getDelegate;
            public TestProperty testProperty;
            public PropertyInfo propInfo;

            #endregion
        }

        /// <summary>
        /// Error information.
        /// </summary>
        struct ErrorRecord
        {
            #region Public properties, operators, constants, and enums

            /// <summary>
            /// The error message.
            /// </summary>
            public String str;

            /// <summary>
            /// The type associated with the error.
            /// </summary>
            public String type;

            #endregion
        }


        #region Static methods, fields, constructors

        /// <summary>
        /// Performs the copy test.
        /// </summary>
        /// <param name="obj1">The first object to test.</param>
        /// <param name="obj2">The second object to test.</param>
        /// <returns>True if obj1 is a valid copy of obj2, false otherwise.</returns>
        public static bool Test(object obj1, object obj2)
        {
            TestPropertyStruct[] tests;
            Type type = obj1.GetType();
            if (!testInfo.TryGetValue(type, out tests))
            {
                // haven't yet seen this type...create list of getset delegates
                PropertyInfo[] properties = obj1.GetType().GetProperties();
                tests = new TestPropertyStruct[properties.Length];
                int count = 0;
                for (int i = 0; i < properties.Length; i++)
                {
                    if (!properties[i].CanRead || !properties[i].CanWrite)
                        // test not possible, skip it...
                        continue;

                    if (properties[i].GetGetMethod().CallingConvention == System.Reflection.CallingConventions.Standard)
                        // static property...
                        continue;

                    object[] attributes = properties[i].GetCustomAttributes(true);
                    bool deep = false;
                    if (attributes != null)
                    {
                        bool ignore = false;
                        foreach (object attr in attributes)
                        {
                            if (attr is TorqueCloneIgnore)
                            {
                                ignore = true;
                                break;
                            }

                            if (attr is TorqueCloneDeep)
                                deep = true;
                        }

                        if (ignore)
                            continue;
                    }

                    // create delegate which will be called in order to generate get and set delegates
                    // This is where the trick happesn, we take a generic and make it specific.
                    Type[] types = { properties[i].PropertyType, obj1.GetType() };
                    _makeTest.MakeGenericMethod(types);
                    System.Reflection.MethodInfo info = deep ? _makeTestDeep.MakeGenericMethod(types) : _makeTest.MakeGenericMethod(types);
                    object[] p = { properties[i], tests, i };
                    info.Invoke(null, p);
                    count++;
                    tests[i].propInfo = properties[i];
                }

                // it's worth the extra one time allocation in order to compress getset array
                TestPropertyStruct[] testsCopy = new TestPropertyStruct[count];
                int j = 0;
                for (int i = 0; i < tests.Length; i++)
                {
                    if (tests[i].testProperty != null)
                        testsCopy[j++] = tests[i];
                }

                tests = testsCopy;
                testInfo[type] = tests;
            }

            // We have all the test delegates, call them
            bool ok = true;
            ErrorRecord error = new ErrorRecord();
            _errors.Clear();
            for (int i = 0; i < tests.Length; i++)
            {
                if (!tests[i].testProperty(obj1, obj2, tests[i].getDelegate, tests[i].propInfo, ref error))
                {
                    _errors.Add(error);
                    ok = false;
                }
            }

            if (!ok)
            {
                String errString = String.Empty;
                for (int i = 0; i < _errors.Count; i++)
                {
                    errString = errString + _errors[i].type +
                                ".CopyTo(TorqueObject obj) needs the following lines of code:\n" +
                                "\n    " + _errors[i].type + " obj2 = " + "(" + _errors[i].type + ")obj;\n";

                    String prev = _errors[i].type;
                    for (int j = i; j < _errors.Count; j++, i++)
                    {
                        if (_errors[j].type == prev)
                        {
                            ErrorRecord errRec = _errors[j];
                            _errors[j] = _errors[_errors.Count - 1];
                            _errors.RemoveAt(_errors.Count - 1);
                            errString = errString + errRec.str;
                        }
                        else
                        {
                            errString = errString + "\n";
                            break;
                        }
                    }
                    i--;
                }

                errString = errString + "\nNote: If the TorqueCloneIgnore attribute is added to one or more of\nthese properties, this error will not be generated for that property.";
                Assert.Fatal(ok, errString);
            }
            return ok;
        }

        /// <summary>
        /// Tests whether or not a property was copied correctly.
        /// </summary>
        /// <typeparam name="S">The type of the object.</typeparam>
        /// <typeparam name="T">The type of the property.</typeparam>
        /// <param name="to">The object being copied to.</param>
        /// <param name="from">The object being copied from.</param>
        /// <param name="getDelegate">The delegate to get the value from the property.</param>
        /// <param name="propInfo">Info about the property.</param>
        /// <param name="error">Error information.</param>
        /// <returns>True if the property is the same on both objects.</returns>
        static bool TestPropMethod<S, T>(object to, object from, Delegate getDelegate, System.Reflection.PropertyInfo propInfo, ref ErrorRecord error)
        {
            AGetMethod<S, T> getMethod = (AGetMethod<S, T>)getDelegate;

            S toProp = getMethod((T)to);
            S fromProp = getMethod((T)from);
            if (Equals(toProp, fromProp))
                return true;

            error.str = "    obj2." + propInfo.Name + " = " + propInfo.Name + ";\n";
            error.type = propInfo.DeclaringType.Name;

            return false;
        }

        /// <summary>
        /// Test whether or not a property and it's fields were copied correctly.
        /// </summary>
        /// <typeparam name="S">The type of the object.</typeparam>
        /// <typeparam name="T">The type of the property.</typeparam>
        /// <param name="to">The object being copied to.</param>
        /// <param name="from">The object being copied from.</param>
        /// <param name="getDelegate">The delegate to get the value from the property.</param>
        /// <param name="propInfo">Info about the property.</param>
        /// <param name="error">Error information.</param>
        /// <returns>True if the property is the same on both objects.</returns>
        static bool TestPropDeepMethod<S, T>(object to, object from, Delegate getDelegate, System.Reflection.PropertyInfo propInfo, ref ErrorRecord error)
        {
            AGetMethod<S, T> getMethod = (AGetMethod<S, T>)getDelegate;

            S toProp = getMethod((T)to);
            S fromProp = getMethod((T)from);
            if (!Equals(toProp, fromProp))
            {
                if (toProp == null && fromProp == null)
                    return true;

                if (toProp != null && fromProp != null && Test(toProp, fromProp))
                    return true;
            }

            error.str = "    obj2." + propInfo.Name + " = " + propInfo.Name + ".Clone();\n";
            error.type = propInfo.DeclaringType.Name;

            return false;
        }

        static List<ErrorRecord> _errors = new List<ErrorRecord>();
        static Dictionary<Type, TestPropertyStruct[]> testInfo = new Dictionary<Type, TestPropertyStruct[]>();
        static System.Reflection.MethodInfo _makeTest = typeof(TestPropertyStruct).GetMethod("MakeTest");
        static System.Reflection.MethodInfo _makeTestDeep = typeof(TestPropertyStruct).GetMethod("MakeTestDeep");

        #endregion
    }
}
