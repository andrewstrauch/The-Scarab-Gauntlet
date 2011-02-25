//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using System.Reflection;
using Microsoft.Xna.Framework;
using GarageGames.Torque.Core;
using GarageGames.Torque.Sim;
using GarageGames.Torque.XNA;


/*  Example Implementation: 
 * A TorqueComponent FSM using the ITickObject interface.

 * It's important to note that the state classes are defined within the IFSMObject class.

public class MyComponent : TorqueComponent, ITickObject, IFSMObject
{
    private FSMState _currentState;
    private FSMState _previousState;

    public MyComponent()
    {
        FSM.Instance.RegisterState<IdleState>(this, "idle");
        FSM.Instance.RegisterState<ActiveState>(this, "active");

        CurrentState = FSM.Instance.GetState(this, "idle");
    }

    public FSMState CurrentState
    {
        get { return _currentState; }
        set { _currentState = value; }
    }

    public FSMState PreviousState
    {
        get { return _previousState; }
        set { _previousState = value; }
    }
 * 
    public void ProcessTick(Move m, float i)
    {
        FSM.Instance.Execute(this);
    }

    public void InterpolateTick(float k) { }

    protected override bool _OnRegister(TorqueObject owner)
    {
       if (!base._OnRegister(owner))
           return false;
 
       ProcessList.Instance.AddTickCallback(owner, this);

       return true;
    }

    public class IdleState : FSMState
    {
        public override void Enter(IFSMObject obj) 
        {
            Console.WriteLine("enter idle");
        }

        public override FSMState Execute(IFSMObject obj) 
        {
            Console.WriteLine("execute idle");
            return FSM.Instance.FindState<ActiveState>();
        }
            
        public override void Exit(IFSMObject obj) 
        {
            Console.WriteLine("exit idle");
        }
    }

    public class ActiveState : FSMState
    {
        public override void Enter(IFSMObject obj)
        {
            Console.WriteLine("enter active");
        }

        public override FSMState Execute(IFSMObject obj)
        {
            Console.WriteLine("execute active");
            return FSM.Instance.FindState<IdleState>();
        }

        public override void Exit(IFSMObject obj) 
        {
            Console.WriteLine("exit active");
        }
    }
}
*/

namespace GarageGames.Torque.Util
{
    /// <summary>
    /// Abstract class that defines the basic State structure for Finite State Machines.
    /// </summary>
    public abstract class FSMState
    {
        #region Public properties, operators, constants, and enums

        /// <summary>
        /// The name of this state.
        /// </summary>
        public string StateName
        {
            get { return _stateName; }
            set { _stateName = value; }
        }

        #endregion


        #region Public methods

        /// <summary>
        /// Optional Enter method that will be called by the Finite State Machine manager when entering this state.
        /// </summary>
        /// <param name="obj">The IFSMObject on which this state is being transitioned to.</param>
        public virtual void Enter(IFSMObject obj) { }

        /// <summary>
        /// Optional Exit method that will be called by the Finite State Machine manager when leaving this state.
        /// </summary>
        /// <param name="obj">The IFSMObject on which this state is being transitioned from.</param>
        public virtual void Exit(IFSMObject obj) { }

        /// <summary>
        /// Required Execute method that defines the rules by which this state will automatically choose another state
        /// to switch to. This method should be overridden and defined to return the name of the state to switch to based
        /// on some criteria.
        /// </summary>
        /// <param name="obj">The IFSMObject on which this state is currently being executed.</param>
        /// <returns>A string containing the state that the specified IFSMObject should switch to.</returns>
        public abstract string Execute(IFSMObject obj);

        #endregion


        #region Private, protected, internal fields

        private string _stateName;

        #endregion
    }

    /// <summary>
    /// Public interface that defines required properties for a Finite State Machine.
    /// </summary>
    public interface IFSMObject
    {
        #region Public properties, operators, constants, and enums

        /// <summary>
        /// The current state of this IFSMObject.
        /// </summary>
        FSMState CurrentState { get; set; }

        /// <summary>
        /// The last valid state that this IFSMObject was in.
        /// </summary>
        FSMState PreviousState { get; set; }

        #endregion
    }

    /// <summary>
    /// The main singleton Finite State Machine manager class. The purpose of this class is to create and manage all 
    /// FSMState states for all IFSMObject Finite State Machines. Individual states are instantiated and hashed on a
    /// per-FSM basis. The FSM class will also handle transitions and call all the neccesary transition callbacks on
    /// based on the specifications of individual states.
    /// </summary>
    public class FSM
    {
        #region Static methods, fields, constructors

        /// <summary>
        /// The static FSM singleton object. Use FSM.Instance to access all FSM functionality.
        /// </summary>
        public static FSM Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new FSM();

                return _instance;
            }
        }

        private static FSM _instance;

        #endregion


        #region Public methods

        /// <summary>
        /// Executes the specified Finite State Machine. Specifically, this executes the CurrentState of the specified IFSMObject
        /// and performs any neccesary transitions via the SetState method.
        /// </summary>
        /// <param name="obj">The IFSMObject to be executed.</param>
        public void Execute(IFSMObject obj)
        {
            if (obj.CurrentState != null)
            {
                // get the hashtable for this IFSMObject
                Hashtable FSMTable = _stateHash[obj.GetType()] as Hashtable;

                // make sure there's a hash table for this IFSMObject
                if (FSMTable == null)
                    return;

                // get the target state
                FSMState targetState = GetState(obj, obj.CurrentState.Execute(obj));

                // set the target state
                SetState(obj, targetState);
            }
        }

        /// <summary>
        /// Attempt to set the CurrentState of the specified IFSMObject to the state specified and call any appropriate state transitions.
        /// State is specified by name. This is the preferred method.
        /// </summary>
        /// <param name="obj">The IFSMObject to set the state of.</param>
        /// <param name="stateName">The name of the desired state to transition to.</param>
        public void SetState(IFSMObject obj, string stateName)
        {
            if (stateName == null)
                return;

            SetState(obj, GetState(obj, stateName));
        }

        /// <summary>
        /// Attempt to set the CurrentState of the specified IFSMObject to the state specified and call any appropriate state transitions.
        /// The actual instance of the desired state is passed. This should normally only be used internally by the FSM class. The preferred
        /// method is SetState(IFSMObject obj, string stateName), but this will remain public for convinience.
        /// </summary>
        /// <param name="obj">The IFSMObject to set the state of.</param>
        /// <param name="state">The actual instance of the desired state to transition to.</param>
        public void SetState(IFSMObject obj, FSMState state)
        {
            // get the hashtable for this IFSMObject
            Hashtable FSMTable = _stateHash[obj.GetType()] as Hashtable;

            // make sure there's a hash table for this IFSMObject
            if (FSMTable == null)
                return;

            // make sure we have all the right data
            if (obj != null && state != null && FSMTable.ContainsValue(state))
            {
                // call exit on the current state
                // (if it exists)
                if (obj.CurrentState != null)
                    obj.CurrentState.Exit(obj);

                // record the current state, then switch states and call the enter function
                obj.PreviousState = obj.CurrentState;
                obj.CurrentState = state;
                obj.CurrentState.Enter(obj);
            }
        }

        /// <summary>
        /// Register a state to be accessible to the specified Finite State Machine. This method will create an instance of the FSMState and hash it
        /// under the specified name in a table created specifically for the specified IFSMObject's class type. The state can later be retrieved by 
        /// name using GetState.
        /// </summary>
        /// <typeparam name="T">The specific type of FSMState to register with this IFSMObject.</typeparam>
        /// <param name="obj">The IFSMObject to register the state for.</param>
        /// <param name="stateName">The name by wich to index the state in the state hash.</param>
        public T RegisterState<T>(IFSMObject obj, string stateName) where T : FSMState, new()
        {
            // make sure we have a this fsm object type in our state hash
            if (!_stateHash.Contains(obj.GetType()))
                _stateHash.Add(obj.GetType(), new Hashtable());

            // get the hashtable for this type of object
            Hashtable FSMTable = _stateHash[obj.GetType()] as Hashtable;

            // assert that we have a hash table for this object type by now
            Assert.Fatal(FSMTable != null, "FSM.RegisterState - FSM Manager failed to create a state hash. What the F?");

            // check if the table containse a state by that name already
            // (new state will overwrite the old one if it's different)
            if (FSMTable.Contains(stateName))
            {
                // if it's the same type, just ignore it
                if (FSMTable[stateName].GetType().Equals(typeof(T)))
                    return null;

                // wasn't the same.. overwrite!
                FSMTable.Remove(stateName);
            }

            // register the new state
            T newState = new T();
            newState.StateName = stateName;
            FSMTable.Add(stateName, newState);
            return newState;
        }

        public FSMState RegisterState(IFSMObject obj, string stateType, string stateName)
        {
            // make sure we have a this fsm object type in our state hash
            if (!_stateHash.Contains(obj.GetType()))
                _stateHash.Add(obj.GetType(), new Hashtable());

            // get the hashtable for this type of object
            Hashtable FSMTable = _stateHash[obj.GetType()] as Hashtable;

            // assert that we have a hash table for this object type by now
            Assert.Fatal(FSMTable != null, "FSM.RegisterState - FSM Manager failed to create a state hash. What the F?");

            Type t = this.GetType().Assembly.GetType(stateType);
            if (t == null)
                t = TorqueEngineComponent.Instance.ExecutableAssembly.GetType(stateType);

            if (t == null)
            {
                TorqueConsole.Error("FSM.RegisterState - State type {0} does not exist.", stateType);
                return null;
            }

            // check if the table containse a state by that name already
            // (new state will overwrite the old one if it's different)
            if (FSMTable.Contains(stateName))
            {
                // if it's the same type, just ignore it
                if (FSMTable[stateName].GetType().Equals(t))
                    return FSMTable[stateName] as FSMState;

                // wasn't the same.. overwrite!
                FSMTable.Remove(stateName);
            }

            // register the new state
            FSMState newState = Activator.CreateInstance(t) as FSMState;
            if (newState == null)
            {
                TorqueConsole.Error("FSM.RegisterState - State type {0} is not an FSMState.", stateType);
                return null;
            }

            newState.StateName = stateName;
            FSMTable.Add(stateName, newState);
            return newState;
        }

        /// <summary>
        /// Get the instance of the specified state that's registered for the specified IFSMObject.
        /// </summary>
        /// <param name="obj">The IFSMObject for which to check for a registered state.</param>
        /// <param name="stateName">The name of the state to check for.</param>
        /// <returns>The instanc of the FSMState registered under the specified name for the specified IFSMObject.</returns>
        public FSMState GetState(IFSMObject obj, string stateName)
        {
            // find the hash table for this IFSMObject
            Hashtable FSMTable = _stateHash[obj.GetType()] as Hashtable;

            // make sure there's a hash table for this IFSMObject
            if (FSMTable == null || stateName == null)
                return null;

            // return the state associated with that name
            return FSMTable[stateName] as FSMState;
        }

        #endregion


        #region Private, protected, internal fields

        private Hashtable _stateHash = new Hashtable();

        #endregion
    }
}
