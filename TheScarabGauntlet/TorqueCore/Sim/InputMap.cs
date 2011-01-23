//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using GarageGames.Torque.Core;
using GarageGames.Torque.Platform;
using Microsoft.Xna.Framework.Input;



namespace GarageGames.Torque.Sim
{
    /// <summary>
    /// An InputMap provides a way to bind delegates to input
    /// events.  There are three types of bindings: 1) Make/Break
    /// events can be bound to MakeDelegate and BreakDelegate via
    /// the BindCommand method.  2) An ActionDelegate can be bound to
    /// Make/Break/Move events by using the BindAction method.
    /// 3) Input can be bound to a MoveManager using the BindMove method.
    /// </summary>
    public class InputMap : TorqueBase, ICloneable
    {
        public delegate void MakeDelegate();
        public delegate void BreakDelegate();
        public delegate void ActionDelegate(float val);

        struct InputNode
        {

            #region Public properties, operators, constants, and enums

            [Flags]
            public enum ActionFlags
            {
                None = 0,
                Ranged = 1,
                HasScale = 2,
                HasDeadZone = 4,
                Inverted = 8,
                BindCmd = 16
            }

            public int DeviceNumber;
            public int ObjectId;

            public TorqueInputDevice.Action Modifier;
            public ActionFlags Flags;
            public float DeadZoneBegin;
            public float DeadZoneEnd;
            public float ScaleFactor;
            public int MoveIndex;
            public MoveMapTypes MoveMapType;

            public MakeDelegate MakeDelegate;
            public BreakDelegate BreakDelegate;
            public ActionDelegate ActionDelegate;

            #endregion


            #region Private, protected, internal fields

            // This is here for the break table only, so that break
            // event is processed on the right input map.
            internal InputMap _map;

            #endregion
        }


        #region Static methods, fields, constructors

        static public InputMap Global
        {
            get
            {
                if (_globalInputMap == null)
                    _globalInputMap = new InputMap();

                return _globalInputMap;
            }
            set { _globalInputMap = value; }
        }

        #endregion


        #region Public properties, operators, constants, and enums

        static public int DeviceNumber
        {
            get { return _deviceNumber; }
        }

        /// <summary>
        /// Current binding.  This index is used when applying auxillary properties to a binding (used
        /// by SetBindScale, SetBindInverted, SetBindDeadZone, and SetBindRanged).  Every time a new action
        /// or move is bound CurrentBindIndex is updated.  To modify a binding other than the last one
        /// the CurrentBindIndex needs to be set to that bind index.  Note that BindCommand does not change
        /// the current bind index since command bindings do not have maleable properties.
        /// </summary>
        public int CurrentBindIndex
        {
            get { return _currentBindIndex; }
            set { if (value >= 0 && value < _inputNodes.Count) _currentBindIndex = value; else _currentBindIndex = -1; }
        }

        /// <summary>
        /// Move manager which move bindings are referred to.
        /// </summary>
        public MoveManager MoveManager
        {
            get { return _moveManager; }
            set
            {
                if (_moveManager == value)
                    return;
                _moveManager = value;
                if (_moveManager != null)
                    _ResetMoveManager();
            }
        }

        #endregion


        #region Public methods

        /// <summary>
        /// Bind delegates to particular make/break input event.
        /// </summary>
        /// <param name="deviceNumber">Device number to bind.</param>
        /// <param name="objectId">Device object to bind to.  E.g., GamePads have thumb sticks, buttons, and triggers, which 
        /// have id's corresponding to XGamePadDevice.GamePadObjects enum values.</param>
        /// <param name="modifier">Restrict binding to be in effect only when given modifier is present.  Valid modifiers are 
        /// Shift, Ctrl, LeftClick, MiddleClick, RightClick</param>
        /// <param name="makeDelegate">Delegate to invoke on make events.</param>
        /// <param name="breakDelegate">Delegate to invoke on break events.</param>
        /// <returns>True if binding was succesful.</returns>
        public bool BindCommand(int deviceNumber, int objectId, TorqueInputDevice.Action modifier, MakeDelegate makeDelegate, BreakDelegate breakDelegate)
        {
            if (deviceNumber < 0 || deviceNumber >= InputManager.Instance.GetNumDevices())
                return false;
            TorqueInputDevice device = InputManager.Instance.GetDevice(deviceNumber);
            if (!device.IsValidObject(objectId))
                return false;

            InputNode node = new InputNode();
            node.DeviceNumber = deviceNumber;
            node.ObjectId = objectId;
            node.Modifier = modifier;
            node.Flags = InputNode.ActionFlags.BindCmd;
            node.DeadZoneBegin = 0.0f;
            node.DeadZoneEnd = 0.0f;
            node.ScaleFactor = 1.0f;
            node.MoveIndex = -1;
            node.MakeDelegate = makeDelegate;
            node.BreakDelegate = breakDelegate;
            _SetInputNode(deviceNumber, objectId, modifier, ref node);
            _currentBindIndex = -1;

            return true;
        }



        /// <summary>
        /// Bind action delegate to particular make/break/move event.  Action delegates take as input the input event value.
        /// </summary>
        /// <param name="deviceNumber">Device number to bind.</param>
        /// <param name="objectId">Device object to bind to.  E.g., GamePads have thumb sticks, buttons, and triggers, which 
        /// have id's corresponding to XGamePadDevice.GamePadObjects enum values.</param>
        /// <param name="modifier">Restrict binding to be in effect only when given modifier is present.  Valid modifiers are 
        /// Shift, Ctrl, LeftClick, MiddleClick, RightClick</param>
        /// <param name="commandDelegate">Command delegate to bind.</param>
        /// <returns>True if binding is successful.</returns>
        public bool BindAction(int deviceNumber, int objectId, TorqueInputDevice.Action modifier, ActionDelegate actionDelegate)
        {
            if (deviceNumber < 0 || deviceNumber >= InputManager.Instance.GetNumDevices())
                return false;
            TorqueInputDevice device = InputManager.Instance.GetDevice(deviceNumber);
            if (!device.IsValidObject(objectId))
                return false;

            InputNode node = new InputNode();
            node.DeviceNumber = deviceNumber;
            node.ObjectId = objectId;
            node.Modifier = modifier;
            node.Flags = InputNode.ActionFlags.None;
            node.DeadZoneBegin = 0.0f;
            node.DeadZoneEnd = 0.0f;
            node.ScaleFactor = 1.0f;
            node.MoveIndex = -1;
            node.ActionDelegate = actionDelegate;

            _currentBindIndex = _SetInputNode(deviceNumber, objectId, modifier, ref node);
            return true;
        }



        /// <summary>
        /// Bind move manager move to particular make/break/move event.
        /// </summary>
        /// <param name="deviceNumber">Device number to bind.</param>
        /// <param name="objectId">Device object to bind to.  E.g., GamePads have thumb sticks, buttons, and triggers, which 
        /// have id's corresponding to XGamePadDevice.GamePadObjects enum values.</param>
        /// <param name="modifier">Restrict binding to be in effect only when given modifier is present.  Valid modifiers are 
        /// Shift, Ctrl, LeftClick, MiddleClick, RightClick</param>
        /// <param name="mapType">Type of move map binding.  This parameter controls whether we bind to a move stick, lever, trigger,
        /// or button, and whether we assume a digital input (e.g., keypress, button press) or an analog input (e.g., thumbstick).</param>
        /// <param name="moveIdx">Index of stick, lever, trigger, or button on the move manager to bind to.</param>
        /// <returns>True if binding is successful.</returns>
        public bool BindMove(int deviceNumber, int objectId, TorqueInputDevice.Action modifier, MoveMapTypes mapType, int moveIdx)
        {
            if (deviceNumber < 0 || deviceNumber >= InputManager.Instance.GetNumDevices())
                return false;
            TorqueInputDevice device = InputManager.Instance.GetDevice(deviceNumber);
            if (!device.IsValidObject(objectId))
                return false;

            InputNode node = new InputNode();
            node.DeviceNumber = deviceNumber;
            node.ObjectId = objectId;
            node.Modifier = modifier;
            node.Flags = InputNode.ActionFlags.None;
            node.DeadZoneBegin = 0.0f;
            node.DeadZoneEnd = 0.0f;
            node.ScaleFactor = 1.0f;
            node.MoveIndex = moveIdx;
            node.MoveMapType = mapType;

            _currentBindIndex = _SetInputNode(deviceNumber, objectId, modifier, ref node);
            _UpdateMoveManager(node);

            return true;
        }



        #region BindCommand, BindAction, BindMove variants for convenience

        /// <summary>
        /// Bind delegates to particular make/break input event.
        /// </summary>
        /// <param name="deviceName">Name of device to bind to, e.g. "gamepad1", "mouse0", "keyboard0".</param>
        /// <param name="inputObject">Name of input object to bind to, e.g. "ThumbStickX", "LeftTriggerButton".</param>
        /// <param name="modifier">Restrict binding to be in effect only when given modifier is present.  Valid modifiers are 
        /// Shift, Ctrl, LeftClick, MiddleClick, RightClick</param>
        /// <param name="makeDelegate">Delegate to invoke on make events.</param>
        /// <param name="breakDelegate">Delegate to invoke on break events.</param>
        /// <returns>True if binding is successful.</returns>
        public bool BindCommand(String deviceName, String inputObject, TorqueInputDevice.Action modifier, MakeDelegate makeDelegate, BreakDelegate breakDelegate)
        {
            int deviceNumber = InputManager.Instance.FindDevice(deviceName);
            if (deviceNumber == -1)
                return false;

            TorqueInputDevice device = InputManager.Instance.GetDevice(deviceNumber);
            int objectId = device.GetObjectId(inputObject);
            if (objectId == -1)
                return false;

            return BindCommand(deviceNumber, objectId, modifier, makeDelegate, breakDelegate);
        }

        /// <summary>
        /// Bind action delegate to particular make/break/move event.  Action delegates take as input the input event value.
        /// </summary>
        /// <param name="deviceName">Name of device to bind to, e.g. "gamepad1", "mouse0", "keyboard0".</param>
        /// <param name="inputObject">Name of input object to bind to, e.g. "ThumbStickX", "LeftTriggerButton".</param>
        /// <param name="modifier">Restrict binding to be in effect only when given modifier is present.  Valid modifiers are 
        /// Shift, Ctrl, LeftClick, MiddleClick, RightClick</param>
        /// <param name="commandDelegate">Command delegate to bind.</param>
        /// <returns>True if binding is successful.</returns>
        public bool BindAction(String deviceName, String inputObject, TorqueInputDevice.Action modifier, ActionDelegate commandDelegate)
        {
            int deviceNumber = InputManager.Instance.FindDevice(deviceName);
            if (deviceNumber == -1)
                return false;

            TorqueInputDevice device = InputManager.Instance.GetDevice(deviceNumber);
            int objectId = device.GetObjectId(inputObject);
            if (objectId == -1)
                return false;

            return BindAction(deviceNumber, objectId, modifier, commandDelegate);
        }

        /// <summary>
        /// Bind move manager move to particular make/break/move event.
        /// </summary>
        /// <param name="deviceName">Name of device to bind to, e.g. "gamepad1", "mouse0", "keyboard0".</param>
        /// <param name="inputObject">Name of input object to bind to, e.g. "ThumbStickX", "LeftTriggerButton".</param>
        /// <param name="modifier">Restrict binding to be in effect only when given modifier is present.  Valid modifiers are 
        /// Shift, Ctrl, LeftClick, MiddleClick, RightClick</param>
        /// <param name="mapType">Type of move map binding.  This parameter controls whether we bind to a move stick, lever, trigger,
        /// or button, and whether we assume a digital input (e.g., keypress, button press) or an analog input (e.g., thumbstick).</param>
        /// <param name="moveIdx">Index of stick, lever, trigger, or button on the move manager to bind to.</param>
        /// <returns>True if binding is successful.</returns>
        public bool BindMove(String deviceName, String inputObject, TorqueInputDevice.Action modifier, MoveMapTypes mapType, int moveIdx)
        {
            int deviceNumber = InputManager.Instance.FindDevice(deviceName);
            if (deviceNumber == -1)
                return false;

            TorqueInputDevice device = InputManager.Instance.GetDevice(deviceNumber);
            int objectId = device.GetObjectId(inputObject);
            if (objectId == -1)
                return false;

            return BindMove(deviceNumber, objectId, modifier, mapType, moveIdx);
        }

        /// <summary>
        /// Undoes any previous call to BindCommand, BindAction, and/or BindMove for
        /// the passed combination of deviceNumber, objectId, and modifier.
        /// </summary>
        /// <param name="deviceNumber">e.g. InputManager.Instance.FindDevice("keyboard")</param>
        /// <param name="objectId">e.g. (int)Keys.S</param>
        /// <param name="modifier">e.g. TorqueInputDevice.Action.Shift</param>
        /// <returns>Whether deviceNumber and objectId are valid.</returns>
        public bool UnbindInput(int deviceNumber, int objectId, TorqueInputDevice.Action modifier)
        {
            TorqueInputDevice device = InputManager.Instance.GetDevice(deviceNumber);
            if (device == null || !device.IsValidObject(objectId))
                return false;

            int index = _FindInputNode(deviceNumber, objectId, modifier);
            if (index < 0)
                return true; //no binding, yet, for that combination

            InputNode node = _inputNodes[index];

            node.Flags = InputNode.ActionFlags.None;
            node.MakeDelegate = null;
            node.BreakDelegate = null;
            node.ActionDelegate = null;
            node.Modifier = TorqueInputDevice.Action.None;
            node.MoveIndex = -1;

            _inputNodes[index] = node;

            return true;
        }

        #region UnbindInput variants for convenience

        /// <summary>
        /// Undoes any previous call to BindCommand, BindAction, and/or BindMove for
        /// the passed combination of deviceName, objectName, and modifier.
        /// </summary>
        /// <param name="deviceName">e.g. "keyboard"</param>
        /// <param name="objectName">e.g. "S"</param>
        /// <param name="modifier">e.g. TorqueInputDevice.Action.Shift</param>
        /// <returns>Whether deviceNumber and objectId are valid.</returns>
        public bool UnbindInput(String deviceName, String objectName, TorqueInputDevice.Action modifier)
        {
            int deviceNumber = InputManager.Instance.FindDevice(deviceName);
            if (deviceNumber == -1)
                return false;

            TorqueInputDevice device = InputManager.Instance.GetDevice(deviceNumber);
            int objectId = device.GetObjectId(objectName);
            if (objectId == -1)
                return false;

            return UnbindInput(deviceNumber, objectId, modifier);
        }

        /// <summary>
        /// Undoes any previous call to BindCommand, BindAction, and/or BindMove for
        /// the passed combination of deviceName and objectName.
        /// </summary>
        /// <param name="deviceName">e.g. "keyboard"</param>
        /// <param name="objectName">e.g. "S"</param>
        /// <returns>Whether deviceNumber and objectId are valid.</returns>
        public bool UnbindInput(String deviceName, String objectName)
        {
            return UnbindInput(deviceName, objectName, TorqueInputDevice.Action.None);
        }

        /// <summary>
        /// Undoes any previous call to BindCommand, BindAction, and/or BindMove for
        /// the passed combination of deviceNumber and objectId.
        /// </summary>
        /// <param name="deviceNumber">e.g. InputManager.Instance.FindDevice("keyboard")</param>
        /// <param name="objectId">e.g. (int)Keys.S</param>
        /// <returns>Whether deviceNumber and objectId are valid.</returns>
        public bool UnbindInput(int deviceNumber, int objectId)
        {
            return UnbindInput(deviceNumber, objectId, TorqueInputDevice.Action.None);
        }

        #endregion

        /// <summary>
        /// Bind delegates to particular make/break input event.
        /// </summary>
        /// <param name="deviceName">Name of device to bind to, e.g. "gamepad1", "mouse0", "keyboard0".</param>
        /// <param name="inputObject">Name of input object to bind to, e.g. "ThumbStickX", "LeftTriggerButton".</param>
        /// <param name="makeDelegate">Delegate to invoke on make events.</param>
        /// <param name="breakDelegate">Delegate to invoke on break events.</param>
        /// <returns>True if binding is successful.</returns>
        public bool BindCommand(String deviceName, String inputObject, MakeDelegate makeDelegate, BreakDelegate breakDelegate)
        {
            return BindCommand(deviceName, inputObject, TorqueInputDevice.Action.None, makeDelegate, breakDelegate);
        }

        /// <summary>
        /// Bind delegates to particular make/break input event.
        /// </summary>
        /// <param name="deviceNumber">Device number to bind.</param>
        /// <param name="objectId">Device object to bind to.  E.g., GamePads have thumb sticks, buttons, and triggers, which 
        /// have id's corresponding to XGamePadDevice.GamePadObjects enum values.</param>
        /// <param name="makeDelegate">Delegate to invoke on make events.</param>
        /// <param name="breakDelegate">Delegate to invoke on break events.</param>
        /// <returns>True if binding was succesful.</returns>
        public bool BindCommand(int deviceNumber, int objectId, MakeDelegate makeDelegate, BreakDelegate breakDelegate)
        {
            return BindCommand(deviceNumber, objectId, TorqueInputDevice.Action.None, makeDelegate, breakDelegate);
        }

        /// <summary>
        /// Bind action delegate to particular make/break/move event.  Action delegates take as input the input event value.
        /// </summary>
        /// <param name="deviceName">Name of device to bind to, e.g. "gamepad1", "mouse0", "keyboard0".</param>
        /// <param name="inputObject">Name of input object to bind to, e.g. "ThumbStickX", "LeftTriggerButton".</param>
        /// <param name="commandDelegate">Command delegate to bind.</param>
        /// <returns>True if binding is successful</returns>
        public bool BindAction(String deviceName, String inputObject, ActionDelegate commandDelegate)
        {
            return BindAction(deviceName, inputObject, TorqueInputDevice.Action.None, commandDelegate);
        }

        /// <summary>
        /// Bind action delegate to particular make/break/move event.  Action delegates take as input the input event value.
        /// </summary>
        /// <param name="deviceNumber">Device number to bind.</param>
        /// <param name="objectId">Device object to bind to.  E.g., GamePads have thumb sticks, buttons, and triggers, which 
        /// have id's corresponding to XGamePadDevice.GamePadObjects enum values.</param>
        /// <param name="commandDelegate">Command delegate to bind.</param>
        /// <returns>True if binding is successful.</returns>
        public bool BindAction(int deviceNumber, int objectId, ActionDelegate commandDelegate)
        {
            return BindAction(deviceNumber, objectId, TorqueInputDevice.Action.None, commandDelegate);
        }

        /// <summary>
        /// Bind move manager move to particular make/break/move event.
        /// </summary>
        /// <param name="deviceNumber">Device number to bind.</param>
        /// <param name="objectId">Device object to bind to.  E.g., GamePads have thumb sticks, buttons, and triggers, which 
        /// have id's corresponding to XGamePadDevice.GamePadObjects enum values.</param>
        /// <param name="mapType">Type of move map binding.  This parameter controls whether we bind to a move stick, lever, trigger,
        /// or button, and whether we assume a digital input (e.g., keypress, button press) or an analog input (e.g., thumbstick).</param>
        /// <param name="moveIdx">Index of stick, lever, trigger, or button on the move manager to bind to.</param>
        /// <returns>True if binding is successful.</returns>
        public bool BindMove(int deviceNumber, int objectId, MoveMapTypes mapType, int moveIdx)
        {
            return BindMove(deviceNumber, objectId, TorqueInputDevice.Action.None, mapType, moveIdx);
        }

        /// <summary>
        /// Bind move manager move to particular make/break/move event.
        /// </summary>
        /// <param name="deviceName">Name of device to bind to, e.g. "gamepad1", "mouse0", "keyboard0".</param>
        /// <param name="inputObject">Name of input object to bind to, e.g. "ThumbStickX", "LeftTriggerButton".</param>
        /// <param name="mapType">Type of move map binding.  This parameter controls whether we bind to a move stick, lever, trigger,
        /// or button, and whether we assume a digital input (e.g., keypress, button press) or an analog input (e.g., thumbstick).</param>
        /// <param name="moveIdx">Index of stick, lever, trigger, or button on the move manager to bind to.</param>
        /// <returns>True if binding is successful.</returns>
        public bool BindMove(String deviceName, String inputObject, MoveMapTypes mapType, int moveIdx)
        {
            return BindMove(deviceName, inputObject, TorqueInputDevice.Action.None, mapType, moveIdx);
        }

        #endregion



        /// <summary>
        /// Set scale on current binding.
        /// </summary>
        /// <param name="scale"></param>
        public void SetBindScale(float scale)
        {
            if (_currentBindIndex < 0 || _currentBindIndex >= _inputNodes.Count)
                // fail silently
                return;

            // have to go to a bit of trouble because _inputNodes is a list not an array
            InputNode node = _inputNodes[_currentBindIndex];
            node.Flags |= InputNode.ActionFlags.HasScale;
            node.ScaleFactor = scale;
            _inputNodes[_currentBindIndex] = node;
        }



        /// <summary>
        /// Set current binding to be inverted.  If binding is ranged then -1 to 1
        /// is mapped into 1 to -1, otherwise 0 to 1 is mapped into 1 to 0.
        /// </summary>
        public void SetBindInverted()
        {
            if (_currentBindIndex < 0 || _currentBindIndex >= _inputNodes.Count)
                // fail silently
                return;

            // have to go to a bit of trouble because _inputNodes is a list not an array
            InputNode node = _inputNodes[_currentBindIndex];
            node.Flags |= InputNode.ActionFlags.Inverted;
            _inputNodes[_currentBindIndex] = node;
        }



        /// <summary>
        /// Add a deadzone to current binding.  Note that XNA adds a deadzone to gamepad devices
        /// automatically (and there is no way to override that behavior).
        /// </summary>
        /// <param name="deadZoneBegin"></param>
        /// <param name="deadZoneEnd"></param>
        public void SetBindDeadZone(float deadZoneBegin, float deadZoneEnd)
        {
            if (_currentBindIndex < 0 || _currentBindIndex >= _inputNodes.Count)
                // fail silently
                return;

            // have to go to a bit of trouble because _inputNodes is a list not an array
            InputNode node = _inputNodes[_currentBindIndex];
            node.Flags |= InputNode.ActionFlags.HasDeadZone;
            node.DeadZoneBegin = deadZoneBegin;
            node.DeadZoneEnd = deadZoneEnd;
            _inputNodes[_currentBindIndex] = node;
        }



        /// <summary>
        /// Make current binding ranged.  A ranged binding maps input range (0 to 1)
        /// into -1 to 1.
        /// </summary>
        public void SetBindRanged()
        {
            if (_currentBindIndex < 0 || _currentBindIndex >= _inputNodes.Count)
                // fail silently
                return;

            // have to go to a bit of trouble because _inputNodes is a list not an array
            InputNode node = _inputNodes[_currentBindIndex];
            node.Flags |= InputNode.ActionFlags.Ranged;
            _inputNodes[_currentBindIndex] = node;
        }



        /// <summary>
        /// Switch all mappings which share the device type with passed device over to using passed device.
        /// Ex. inputMap.ChangeDevice("gamePad3") would change all game pad bindings to the third gamepad.
        /// </summary>
        /// <param name="deviceName">Name of device to switch to.</param>
        /// <returns>True if deviceName is a valid device.</returns>
        public bool ChangeDevice(String deviceName)
        {
            int deviceNumber = InputManager.Instance.FindDevice(deviceName);
            if (deviceNumber == -1)
                return false;
            TorqueInputDevice device = InputManager.Instance.GetDevice(deviceNumber);

            for (int i = 0; i < _inputNodes.Count; i++)
            {
                int devNum = _inputNodes[i].DeviceNumber;
                if (devNum < 0)
                    // this should never really happen since we make sure device is valid
                    // when we add it...
                    continue;
                TorqueInputDevice dev = InputManager.Instance.GetDevice(devNum);
                if (dev.DeviceTypeId == device.DeviceTypeId)
                {
                    // same device type, replace
                    InputNode inputNode = _inputNodes[i];
                    inputNode.DeviceNumber = deviceNumber;
                    _inputNodes[i] = inputNode;
                }
            }
            return true;
        }



        /// <summary>
        /// Fires any actions, commands, or moves bound to passed input.  This method is
        /// generally only called by the input manager.
        /// </summary>
        /// <param name="data">Input event data.</param>
        /// <returns>True if input map processes the input event.</returns>
        public bool ProcessInput(TorqueInputDevice.InputEventData data)
        {
            int idx = _FindInputNode(data.DeviceNumber, data.ObjectId, data.Modifier);
            if (idx < 0)
                return false;

            if (data.DeviceNumber < 5 && data.DeviceNumber >= 0)
                _deviceNumber = data.DeviceNumber;

            if (data.EventAction == TorqueInputDevice.Action.Break)
                return _CheckBreakTable(data);

            float value = data.Value;
            InputNode.ActionFlags flags = _inputNodes[idx].Flags;
            if ((flags & InputNode.ActionFlags.BindCmd) != 0)
            {
                if (data.EventAction == TorqueInputDevice.Action.Make)
                {
                    if (_inputNodes[idx].MakeDelegate != null)
                        _inputNodes[idx].MakeDelegate();

                    _EnterBreakEvent(data, idx);
                    return true;
                }
            }
            else
            {
                if (data.EventAction == TorqueInputDevice.Action.Make || data.EventAction == TorqueInputDevice.Action.Move)
                {
                    _MakeBreakModifyEventValue(ref value, _inputNodes[idx]);
                    if (_inputNodes[idx].ActionDelegate != null)
                        _inputNodes[idx].ActionDelegate(value);
                    else if (_inputNodes[idx].MoveIndex >= 0)
                        _SetMoveValue(_inputNodes[idx].MoveMapType, _inputNodes[idx].MoveIndex, value);
                }
                if (data.EventAction == TorqueInputDevice.Action.Make)
                    _EnterBreakEvent(data, idx);
                return true;
            }

            return false;
        }



        /// <summary>
        /// Clone input map and return as InputMap typed object.
        /// </summary>
        /// <returns>Cloned input map.</returns>
        public InputMap CloneInputMap()
        {
            return Clone() as InputMap;
        }



        /// <summary>
        /// Implements ICloneable interface.
        /// </summary>
        /// <returns>Cloned input map.</returns>
        public object Clone()
        {
            InputMap map = new InputMap();
            map._moveManager = _moveManager;
            map._currentBindIndex = _currentBindIndex;
            for (int i = 0; i < _inputNodes.Count; i++)
                map._inputNodes.Add(_inputNodes[i]);
            return map;
        }


        public void ClearControls()
        {
            _ResetMoveManager();
        }

        #endregion


        #region Private, protected, internal methods

        /// <summary>
        /// Input bound to a move is process via this routine.
        /// </summary>
        /// <param name="mapType">Type of move mapping.</param>
        /// <param name="idx">Move index (stick number, button number, etc.)</param>
        /// <param name="value">Value of input event.</param>
        void _SetMoveValue(MoveMapTypes mapType, int idx, float value)
        {
            if (_moveManager == null)
                return;

            switch (mapType)
            {
                case MoveMapTypes.Button:
                    _moveManager.SetButton(idx, value > 0.5f);
                    break;
                case MoveMapTypes.TriggerAnalog:
                    _moveManager.SetTriggerDirect(idx, value);
                    break;
                case MoveMapTypes.TriggerDigital:
                    _moveManager.SetTriggerTarget(idx, value > 0.5f);
                    break;
                case MoveMapTypes.LeverAnalog:
                    _moveManager.SetLeverDirect(idx, value);
                    break;
                case MoveMapTypes.LeverDigitalMinus:
                    _moveManager.SetLeverNegative(idx, value > 0.5f);
                    break;
                case MoveMapTypes.LeverDigitalPlus:
                    _moveManager.SetLeverPositive(idx, value > 0.5f);
                    break;
                case MoveMapTypes.StickAnalogHorizontal:
                    _moveManager.SetStickHorizontalDirect(idx, value);
                    break;
                case MoveMapTypes.StickDigitalLeft:
                    _moveManager.SetStickLeft(idx, value > 0.5f);
                    break;
                case MoveMapTypes.StickDigitalRight:
                    _moveManager.SetStickRight(idx, value > 0.5f);
                    break;
                case MoveMapTypes.StickAnalogVertical:
                    _moveManager.SetStickVerticalDirect(idx, value);
                    break;
                case MoveMapTypes.StickDigitalDown:
                    _moveManager.SetStickDown(idx, value > 0.5f);
                    break;
                case MoveMapTypes.StickDigitalUp:
                    _moveManager.SetStickUp(idx, value > 0.5f);
                    break;
            }
        }

        /// <summary>
        /// Update move counts on current move manager to account for passed input node.
        /// </summary>
        /// <param name="inputNode">InputNode to check move manager against.</param>
        void _UpdateMoveManager(InputNode inputNode)
        {
            if (_moveManager == null || _moveManager.LockMoveCounts)
                return;

            if (inputNode.ActionDelegate == null && inputNode.BreakDelegate == null && inputNode.MakeDelegate == null && inputNode.MoveIndex >= 0)
            {
                // by process of elimination we are a move node
                switch (inputNode.MoveMapType)
                {
                    case MoveMapTypes.Button:
                        _moveManager.NumButtons = Math.Max(_moveManager.NumButtons, inputNode.MoveIndex + 1);
                        break;
                    case MoveMapTypes.TriggerAnalog:
                    case MoveMapTypes.TriggerDigital:
                        _moveManager.NumTriggers = Math.Max(_moveManager.NumTriggers, inputNode.MoveIndex + 1);
                        break;
                    case MoveMapTypes.LeverAnalog:
                    case MoveMapTypes.LeverDigitalMinus:
                    case MoveMapTypes.LeverDigitalPlus:
                        _moveManager.NumLevers = Math.Max(_moveManager.NumLevers, inputNode.MoveIndex + 1);
                        break;
                    case MoveMapTypes.StickAnalogHorizontal:
                    case MoveMapTypes.StickDigitalLeft:
                    case MoveMapTypes.StickDigitalRight:
                    case MoveMapTypes.StickAnalogVertical:
                    case MoveMapTypes.StickDigitalDown:
                    case MoveMapTypes.StickDigitalUp:
                        _moveManager.NumSticks = Math.Max(_moveManager.NumSticks, inputNode.MoveIndex + 1);
                        break;
                }
            }
        }

        /// <summary>
        /// When move manager changes for this input map, go into the move manager and reset the move counts.  If move manager
        /// can set LockMoveCounts property to over-ride this behavoir.
        /// </summary>
        protected void _ResetMoveManager()
        {
            if (_moveManager == null || _moveManager.LockMoveCounts)
                return;

            _moveManager.NumButtons = 0;
            _moveManager.NumLevers = 0;
            _moveManager.NumSticks = 0;
            _moveManager.NumTriggers = 0;
            for (int i = 0; i < _inputNodes.Count; i++)
                _UpdateMoveManager(_inputNodes[i]);
        }

        void _MakeBreakModifyEventValue(ref float value, InputNode node)
        {
            InputNode.ActionFlags flags = node.Flags;
            if ((flags & InputNode.ActionFlags.Ranged) != 0)
            {
                value = value * 2.0f - 1.0f;
                if ((flags & InputNode.ActionFlags.Inverted) != 0)
                    value *= -1.0f;
            }
            else if ((flags & InputNode.ActionFlags.Inverted) != 0)
                value = 1.0f - value;

            if ((flags & InputNode.ActionFlags.HasScale) != 0)
                value *= node.ScaleFactor;

            if ((flags & InputNode.ActionFlags.HasDeadZone) != 0)
            {
                if (value >= node.DeadZoneBegin && value <= node.DeadZoneEnd)
                    value = 0.0f;
            }
        }

        void _MoveModifyEventValue(ref float value, InputNode node)
        {
            InputNode.ActionFlags flags = node.Flags;
            if ((flags & InputNode.ActionFlags.Inverted) != 0)
                value *= -1.0f;

            if ((flags & InputNode.ActionFlags.HasScale) != 0)
                value *= node.ScaleFactor;

            if ((flags & InputNode.ActionFlags.HasDeadZone) != 0)
            {
                if (value >= node.DeadZoneBegin && value <= node.DeadZoneEnd)
                    value = 0.0f;
            }
        }

        bool _CheckBreakTable(TorqueInputDevice.InputEventData data)
        {
            for (int i = 0; i < _breakTable.Count; i++)
            {
                if (_breakTable[i].DeviceNumber == data.DeviceNumber && _breakTable[i].ObjectId == data.ObjectId)
                {
                    if ((_breakTable[i].Flags & InputNode.ActionFlags.BindCmd) != 0)
                    {
                        if (_breakTable[i].BreakDelegate != null)
                            _breakTable[i].BreakDelegate();
                    }
                    else
                    {
                        if (_breakTable[i].ActionDelegate != null)
                        {
                            float value = data.Value;
                            _MakeBreakModifyEventValue(ref value, _breakTable[i]);
                            _breakTable[i].ActionDelegate(value);
                        }
                        else if (_breakTable[i].MoveIndex >= 0)
                        {
                            Assert.Fatal(_breakTable[i]._map != null, "Input map should never be null in break table");
                            float value = data.Value;
                            _breakTable[i]._map._MakeBreakModifyEventValue(ref value, _breakTable[i]);
                            _breakTable[i]._map._SetMoveValue(_breakTable[i].MoveMapType, _breakTable[i].MoveIndex, value);
                        }
                    }
                    _breakTable.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }

        void _EnterBreakEvent(TorqueInputDevice.InputEventData data, int inputNodeIdx)
        {
            int deviceNumber = _inputNodes[inputNodeIdx].DeviceNumber;
            int objectId = _inputNodes[inputNodeIdx].ObjectId;
            InputNode node;
            for (int i = 0; i < _breakTable.Count; i++)
            {
                if (_breakTable[i].DeviceNumber == deviceNumber && _breakTable[i].ObjectId == objectId)
                {
                    node = _inputNodes[inputNodeIdx];
                    node._map = this;
                    _breakTable[i] = node;
                    return;
                }
            }
            node = _inputNodes[inputNodeIdx];
            node._map = this;
            _breakTable.Add(node);
        }

        int _FindInputNode(int deviceNumber, int objectId, TorqueInputDevice.Action modifier)
        {
            for (int i = 0; i < _inputNodes.Count; i++)
            {
                if (_inputNodes[i].DeviceNumber == deviceNumber && _inputNodes[i].ObjectId == objectId && _inputNodes[i].Modifier == modifier)
                    return i;
            }
            return -1;
        }

        int _SetInputNode(int deviceNumber, int objectId, TorqueInputDevice.Action modifier, ref InputNode node)
        {
            int ret = _FindInputNode(deviceNumber, objectId, modifier);

            if (ret >= 0)
            {
                //_inputNodes[ret] = node;
                //    return ret;


                if (null == node.ActionDelegate &&
                    null == node.MakeDelegate &&
                    null == node.BreakDelegate &&
                    node.MoveIndex < 0)
                {
                    _inputNodes.RemoveAt(ret);
                    return _inputNodes.Count - 1;
                }
                else
                {
                    _inputNodes[ret] = node;
                    return ret;
                }

            }

            _inputNodes.Add(node);
            return _inputNodes.Count - 1;
        }

        #endregion


        #region Private, protected, internal fields

        List<InputNode> _inputNodes = new List<InputNode>();
        int _currentBindIndex = -1;
        MoveManager _moveManager;
        static List<InputNode> _breakTable = new List<InputNode>();
        static InputMap _globalInputMap;
        static int _deviceNumber = 5;

        #endregion
    }
}
