using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GarageGames.Torque.PlatformerFramework
{
    public delegate bool CustomConsoleRoutineDelegate(out string error, string[] parameters);

    public class CustomConsoleRoutinePool
    {
        #region Private Members

        private Dictionary<string, CustomConsoleRoutineDelegate> customRoutines;
        private static CustomConsoleRoutinePool instance;
        
        #endregion

        #region Public Properties

        public static CustomConsoleRoutinePool Instance
        {
            get
            {
                if (instance == null)
                    instance = new CustomConsoleRoutinePool();

                return instance;
            }
        }

        #endregion

        #region Public Routines

        /// <summary>
        /// Runs the given Routine with the inputted parameters.  Sets the error upon any
        /// error condition.
        /// </summary>
        /// <param name="RoutineName">The name of the routine to run.</param>
        /// <param name="error">The error condition that encountered, otherwise null.</param>
        /// <param name="parameters">The parameters to feed into the given routine.</param>
        /// <returns>True if routine is found and run, false otherwise.</returns>
        public bool RunRoutine(string routineName, out string error, string[] parameters)
        {
            error = null;
            if (routineName == "")
                return false;

            if (!customRoutines.ContainsKey(routineName))
            {
                error = routineName + " is not a registered Routine.";
                return false;
            }

            return customRoutines[routineName](out error, parameters);
        }

        /// <summary>
        /// Adds the routine to the pool.
        /// </summary>
        /// <param name="routine">The routine delegate to register.</param>
        public void RegisterMethod(CustomConsoleRoutineDelegate routine)
        {
            if (routine == null)
                return;

            if (customRoutines.ContainsKey(routine.Method.Name))
                return;

            customRoutines.Add(routine.Method.Name, routine);
        }

        /// <summary>
        /// Removes the routine from the pool.
        /// </summary>
        /// <param name="routine">The routine delegate to remove.</param>
        public void UnRegisterMethod(CustomConsoleRoutineDelegate routine)
        {
            if (routine == null)
                return;

            if (!customRoutines.ContainsKey(routine.Method.Name))
                return;

            customRoutines.Remove(routine.Method.Name);
        }

        #endregion

        #region Private Routines

        private CustomConsoleRoutinePool()
        {
            customRoutines = new Dictionary<string, CustomConsoleRoutineDelegate>();
        }

        #endregion
    }
}
