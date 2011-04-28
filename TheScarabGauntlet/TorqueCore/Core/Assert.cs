//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;



namespace GarageGames.Torque.Core
{
    /// <summary>
    /// Wrapper class for Trace.Assert functionality (used internally by Torque X).
    /// </summary>
    public class Assert
    {
        #region Static methods, fields, constructors

        /// <summary>
        /// Assert that given condition is true.  If condition is false, execution
        /// of the program will stop.  This method is compiled out when the TRACE option
        /// is not on (e.g., release builds).
        /// </summary>
        /// <param name="condition">Condition to test.</param>
        /// <param name="message">Message to display if condition is false.</param>
        [Conditional("TRACE")]
        public static void Fatal(bool condition, string message)
        {
            Trace.Assert(condition, message);
        }



        /// <summary>
        /// Assert that given condition is true, printing error to console log but
        /// not stopping program execution if false.  This method is compiled out when
        /// the TRACE option is not on (e.g., release builds).
        /// </summary>
        /// <param name="condition">Condition to test.</param>
        /// <param name="message">Message to display if condition is false.</param>
        [Conditional("TRACE")]
        public static void Warn(bool condition, string message)
        {
            if (!condition)
                Console.WriteLine(message);
        }



        /// <summary>
        /// Assert that given condition is true, stopping program execution if
        /// false even if TRACE option is not on (i.e., stops execution "in
        /// shipping version" or ISV).  Note:  currently Assert.ISV does not stop
        /// execution when TRACE not defined.
        /// </summary>
        /// <param name="condition">Condition to test.</param>
        /// <param name="message">Message to display if condition is false.</param>
        public static void ISV(bool condition, string message)
        {
            Assert.Fatal(condition, message);
        }

        #endregion
    }
}
