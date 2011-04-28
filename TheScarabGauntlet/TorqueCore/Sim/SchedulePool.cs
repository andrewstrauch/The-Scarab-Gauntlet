//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using GarageGames.Torque.Core;



namespace GarageGames.Torque.Sim
{
    public class ScheduledEventPool
    {
        /// <summary>
        /// Storage for schedule callback information.
        /// </summary>
        private struct CallbackInformation
        {

            #region Public properties, operators, constants, and enums

            /// <summary>
            /// The delegate to trigger when the schedule is triggered.
            /// </summary>
            public ScheduledEventDelegate MethodToCall;

            /// <summary>
            /// The data to send with the schedule.
            /// </summary>
            public object DataToSend;

            /// <summary>
            /// The event id for tracking the schedule.
            /// </summary>
            public int EventId;

            #endregion


            #region Public methods

            /// <summary>
            /// Sets the callback information for this schedule.
            /// </summary>
            /// <param name="methodToCall">The delegate to trigger.</param>
            /// <param name="dataToSend">Data to pass.</param>
            public CallbackInformation(ScheduledEventDelegate methodToCall, object dataToSend)
            {
                MethodToCall = methodToCall;
                DataToSend = dataToSend;
                EventId = 0;
            }

            #endregion
        }


        #region Public methods

        /// <summary>
        /// Schedule a method to execute at a specific time.
        /// </summary>
        /// <param name="deltaTime">The amount of time in milliseconds into the future you wish this method to be invoked.
        /// The actual game-time may be slightly later than when you specify (but never earlier).
        /// <para>Value must be greater than zero.</para></param>
        /// <param name="methodToCall">The method you wish to invoke.</param>
        /// <param name="dataToSend">Any object, this will be sent to the specified method in the SecheduleEventArguments.</param>
        public int Schedule(int deltaTime, ScheduledEventDelegate methodToCall, object dataToSend)
        {
            Assert.Fatal(deltaTime >= 0, "Time can't go backwards");
            int targetTime = _currentTime + deltaTime;
            CallbackInformation toSchedule = new CallbackInformation(methodToCall, dataToSend);
            toSchedule.EventId = ++_nextEventId;

            LinkedListNode<KeyValuePair<int, CallbackInformation>> currentNode = this._scheduledEvents.First;

            while (currentNode != null)
            {
                if (currentNode.Value.Key == targetTime)
                {
                    currentNode.List.AddAfter(currentNode, new KeyValuePair<int, CallbackInformation>(targetTime, toSchedule));

                    return toSchedule.EventId;
                }
                if (currentNode.Value.Key > targetTime)
                {
                    currentNode.List.AddBefore(currentNode, new KeyValuePair<int, CallbackInformation>(targetTime, toSchedule));
                    return toSchedule.EventId;
                }
                currentNode = currentNode.Next;
            }
            this._scheduledEvents.AddLast(new KeyValuePair<int, CallbackInformation>(targetTime, toSchedule));

            return toSchedule.EventId;
        }

        /// <summary>
        /// Schedule a method to execute at a specific time.
        /// </summary>
        /// <param name="deltaTime">The amount of time in milliseconds into the future you wish this method to be invoked.
        /// The actual game-time may be slightly later than when you specify (but never earlier).
        /// <para>Value must be greater than zero.</para></param>
        /// <param name="methodToCall">The method you wish to invoke.</param>
        public int Schedule(int deltaTime, ScheduledEventDelegate methodToCall)
        {
            return this.Schedule(deltaTime, methodToCall, null);
        }

        /// <summary>
        /// Remove a scheduled method at a specific time.
        /// <para>If the same method has multiple schedules for the same time, the first occurance is removed. (call this multiple times to remove multiple occurances)</para>
        /// </summary>
        /// <returns>false if unable to remove a scheduled method (due to it not existing at the time given)</returns>
        public bool Remove(int time, ScheduledEventDelegate methodToCall)
        {
            LinkedListNode<KeyValuePair<int, CallbackInformation>> currentNode = this._scheduledEvents.First;

            while (currentNode != null)
            {
                if (currentNode.Value.Key < time)
                {
                    currentNode = currentNode.Next;
                    continue;
                }
                if (currentNode.Value.Key > time)
                    return false;

                CallbackInformation methodInfo = currentNode.Value.Value;

                if (methodToCall.Equals(methodInfo.MethodToCall))
                {
                    LinkedListNode<KeyValuePair<int, CallbackInformation>> toRemove = currentNode;
                    currentNode = currentNode.Next;
                    this._scheduledEvents.Remove(toRemove);
                    return true;
                }
                else
                    currentNode = currentNode.Next;
            }

            return false;
        }

        /// <summary>
        /// Remove the event with the specified event Id.
        /// </summary>
        /// <param name="methodToCall"></param>
        /// <returns></returns>
        public void Remove(int eventId)
        {
            // ignore if they are trying to remove the current event
            if (_currentEventId == eventId)
                return;

            LinkedListNode<KeyValuePair<int, CallbackInformation>> currentNode = this._scheduledEvents.First;

            while (currentNode != null)
            {
                CallbackInformation methodInfo = currentNode.Value.Value;

                if (methodInfo.EventId == eventId)
                {
                    LinkedListNode<KeyValuePair<int, CallbackInformation>> toRemove = currentNode;
                    currentNode = currentNode.Next;
                    this._scheduledEvents.Remove(toRemove);
                    break;
                }
                else
                {
                    currentNode = currentNode.Next;
                }
            }
        }

        /// <summary>
        /// Remove all scheduled events for the specified object.
        /// </summary>
        /// <param name="targetObj"></param>
        /// <returns></returns>
        public bool Remove(object targetObj)
        {
#if XBOX
            Assert.Fatal(false, "doh, remove not supported on Xbox"); // JMQtodo: we need to suppot SchedulePool.Remove on thie xbox (problem is that .Target does not exist on delegate in compact framework)
#else
            LinkedListNode<KeyValuePair<int, CallbackInformation>> currentNode = this._scheduledEvents.First;

            while (currentNode != null)
            {
                CallbackInformation methodInfo = currentNode.Value.Value;

                if (methodInfo.MethodToCall.Target == targetObj)
                {
                    LinkedListNode<KeyValuePair<int, CallbackInformation>> toRemove = currentNode;
                    currentNode = currentNode.Next;
                    this._scheduledEvents.Remove(toRemove);
                }
                else
                {
                    currentNode = currentNode.Next;
                }
            }
#endif
            return false;
        }

        /// <summary>
        /// Invoke the events at the given time (and any time earlier) (and then removing that time/events list from the pool)
        /// </summary>
        /// <param name="time">the actual game-time.  all events scheduled at this time or earlier will be triggered and removed from the pool.</param>
        public void AdvanceTime(float elapsedMS)
        {
            elapsedMS += _remainderMS;
            int ms = (int)elapsedMS;
            _remainderMS = elapsedMS - (float)ms;
            _currentTime += ms;

            CallbackInformation toExecute;

            LinkedListNode<KeyValuePair<int, CallbackInformation>> currentNode = _scheduledEvents.First;

            while (currentNode != null)
            {
                if (currentNode.Value.Key > _currentTime)
                    return;
                toExecute = currentNode.Value.Value;

                if (_tempToSend == null)
                    _tempToSend = new ScheduledEventArguments();
                _tempToSend.DataToSend = toExecute.DataToSend;
                _tempToSend.TargetTime = currentNode.Value.Key;
                _currentEventId = toExecute.EventId;
                toExecute.MethodToCall(this, _tempToSend);
                _currentEventId = 0; // no event being processed now
                currentNode = currentNode.Next;
                _scheduledEvents.RemoveFirst();
            }

        }

        #endregion


        #region Private, protected, internal fields

        int _nextEventId;
        int _currentEventId;

        int _currentTime;
        float _remainderMS;

        LinkedList<KeyValuePair<int, CallbackInformation>> _scheduledEvents = new LinkedList<KeyValuePair<int, CallbackInformation>>();
        ScheduledEventArguments _tempToSend;

        #endregion
    }



    /// <summary>
    /// Delegate that each callback event uses to interface with.
    /// </summary>
    /// <param name="sender">The object that triggered the event.</param>
    /// <param name="scheduleEventArguments">Arguments to send with the event.</param>
    public delegate void ScheduledEventDelegate(object sender, ScheduledEventArguments scheduleEventArguments);



    /// <summary>
    /// Data that will be sent to the Scheduled callback method.  
    /// </summary>
    public class ScheduledEventArguments : EventArgs
    {

        #region Public properties, operators, constants, and enums

        /// <summary>
        /// Data the requestor wants to send to the scheduled event.
        /// </summary>
        public object DataToSend
        {
            get { return _dataToSend; }
            set { _dataToSend = value; }
        }

        /// <summary>
        /// The current simulation time.
        /// </summary>
        public int TargetTime
        {
            get { return _time; }
            set { _time = value; }
        }

        #endregion


        #region Private, protected, internal fields

        private object _dataToSend;
        private int _time;

        #endregion
    }
}
