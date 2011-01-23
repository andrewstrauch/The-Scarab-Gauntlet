//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using GarageGames.Torque.Core;
using GarageGames.Torque.GUI;
using GarageGames.Torque.Platform;



namespace GarageGames.Torque.Sim
{
    /// <summary>
    /// Base class for input devices in Torque X.  See XGamePadDevice, XMouseDevice, and XKeyboardDevice for examples.
    /// </summary>
    public class TorqueInputDevice
    {
        /// <summary>
        /// Data generated whenever an input event occurs for a TorqueInputDevice.
        /// </summary>
        public struct InputEventData : IEventReadWrite
        {

            #region Public properties, operators, constants, and enums

            /// <summary>
            /// InputManager device number.
            /// </summary>
            public int DeviceNumber;
            /// <summary>
            /// GetDeviceTypeId("Mouse"), GetDeviceTypeId("Keyboard"), etc. or use TorqueInputObject properties MouseId, etc.
            /// </summary>
            public int DeviceTypeId;
            /// <summary>
            /// Which of the devices of this type is the event for.
            /// </summary>
            public int DeviceInstance;
            /// <summary>
            /// GetDeviceObjectId("Up"),GetDeviceObjectId("LeftThumbX"), GetDeviceObjectId("key"), etc. or use TorqueInputObject properties UpButton, etc.
            /// </summary>
            public int ObjectId;
            /// <summary>
            /// Trigger value, axis value, button value, etc.
            /// </summary>
            public float Value;
            /// <summary>
            /// ASCII character code if this is a keyboard event.
            /// </summary>
            public char Ascii;
            /// <summary>
            /// What was the action? (MAKE/BREAK/MOVE)
            /// </summary>
            public Action EventAction;
            /// <summary>
            /// Modifier to action: LeftShift, RightCtrl, etc.
            /// </summary>
            public Action Modifier;

            #endregion


            #region Public methods

            public void WriteEventData(BinaryWriter writer)
            {
                writer.Write(DeviceNumber);
                writer.Write(DeviceTypeId);
                writer.Write(DeviceInstance);
                writer.Write(ObjectId);
                writer.Write(Value);
                writer.Write(Ascii);
                writer.Write((int)EventAction);
                writer.Write((int)Modifier);
            }

            public object ReadEventData(BinaryReader reader)
            {
                DeviceNumber = reader.ReadInt32();
                DeviceTypeId = reader.ReadInt32();
                DeviceInstance = reader.ReadInt32();
                ObjectId = reader.ReadInt32();
                Value = reader.ReadSingle();
                Ascii = reader.ReadChar();
                EventAction = (Action)reader.ReadInt32();
                Modifier = (Action)reader.ReadInt32();
                return this;
            }

            #endregion
        }


        #region Static methods, fields, constructors

        #region Static Constructor

        static TorqueInputDevice()
        {
            _mouseId = GetDeviceTypeId("mouse");
            _gamepadId = GetDeviceTypeId("gamepad");
            _keyboardId = GetDeviceTypeId("keyboard");

            // sign up input manager to listen to our input events
            TorqueEventManager.ListenEvents<InputEventData>(MouseEvent, InputManager.Instance.ProcessInputEvent);
            TorqueEventManager.ListenEvents<InputEventData>(KeyboardEvent, InputManager.Instance.ProcessInputEvent);
            TorqueEventManager.ListenEvents<InputEventData>(GamepadEvent, InputManager.Instance.ProcessInputEvent);
        }

        #endregion

        #region Pre-defined input events

        /// <summary>
        /// TorqueEvent which is triggered whenver mouse input occurs.
        /// </summary>
        public static TorqueEvent<InputEventData> MouseEvent
        {
            get { return _mouseEvent; }
        }

        /// <summary>
        /// TorqueEvent which is triggered whenever keyboard input occurs.
        /// </summary>
        public static TorqueEvent<InputEventData> KeyboardEvent
        {
            get { return _keyboardEvent; }
        }

        /// <summary>
        /// TorqueEvent which is triggered whenever gamepad input occurs.
        /// </summary>
        public static TorqueEvent<InputEventData> GamepadEvent
        {
            get { return _gamepadEvent; }
        }

        #endregion

        #region Pre-defined device id's

        /// <summary>
        /// Device ID for a game pad device.
        /// </summary>
        public static int GamePadId
        {
            get { return _gamepadId; }
        }

        /// <summary>
        /// Device ID for a mouse device.
        /// </summary>
        public static int MouseId
        {
            get { return _mouseId; }
        }

        /// <summary>
        /// Device id for a keyboard device.
        /// </summary>
        public static int KeyboardId
        {
            get { return _keyboardId; }
        }

        #endregion

        #region Public static interface for mapping device type names to id's

        /// <summary>
        /// Lookup the device id for the given device type.  If device type not
        /// found then new id is generated.
        /// Note:  GetDeviceTypeId and GetDeviceTypeName are inversese of each other.
        /// </summary>
        /// <param name="deviceType">Name of device type.</param>
        /// <returns>Device id.</returns>
        public static int GetDeviceTypeId(String deviceType)
        {
            int id;
            if (_deviceTypeToId.TryGetValue(deviceType, out id))
                return id;
            else
            {
                String name;
                while (_deviceIdToType.TryGetValue(_nextDeviceId, out name))
                    ++_nextDeviceId;
                _deviceIdToType[_nextDeviceId] = deviceType;
                _deviceTypeToId[deviceType] = _nextDeviceId;
                return _nextDeviceId++;
            }
        }

        /// <summary>
        /// Given device id, return the name of the device type.
        /// Note:  GetDeviceTypeId and GetDeviceTypeName are inversese of each other.
        /// </summary>
        /// <param name="id">Id of type.</param>
        /// <returns>Device type name.</returns>
        public static String GetDeviceTypeName(int id)
        {
            String name;
            if (_deviceIdToType.TryGetValue(id, out name))
                return name;
            return "none";
        }

        /// <summary>
        /// Test whether given device type name is an already defined device type.
        /// </summary>
        /// <param name="deviceType">Device type name.</param>
        /// <param name="id">Returned id.</param>
        /// <returns>True if device type name is valid, false if not.</returns>
        public static bool IsDeviceType(String deviceType, out int id)
        {
            return _deviceTypeToId.TryGetValue(deviceType, out id);
        }

        /// <summary>
        /// Test whether given device type name is an already defined device type.
        /// </summary>
        /// <param name="deviceType">Device type name.</param>
        /// <returns>True if device type name is valid, false if not.</returns>
        public static bool IsDeviceType(String deviceType)
        {
            int id;
            return _deviceTypeToId.TryGetValue(deviceType, out id);
        }

        #endregion

        #endregion


        #region Public properties, operators, constants, and enums

        /// <summary>
        /// Device number of this device.  Device number is used to differentiate devices of the same type.  E.g., gamepad0, gamepad1, etc.
        /// </summary>
        public int DeviceNumber
        {
            get
            {
                return _deviceNumber;
            }
        }

        /// <summary>
        /// Id assigned by the InputManager to devices of this type.
        /// </summary>
        public int DeviceTypeId
        {
            get
            {
                return _deviceTypeId;
            }
        }

        /// <summary>
        /// Differntiates types of input events.
        /// </summary>
        public enum Action : int
        {
            None = 0,

            // pure actions

            /// <summary>
            /// Make events occur when a button or key is pushed.
            /// </summary>
            Make,
            /// <summary>
            /// Break events occur when a button or key is released.
            /// </summary>
            Break,
            /// <summary>
            /// Move events occur when a thumbstick, trigger, or mouse is moved.
            /// </summary>
            Move,

            // modifiers

            /// <summary>
            /// A modifier which signifies that an event occured with the shift key held down.
            /// </summary>
            Shift,
            /// <summary>
            /// A modifier which signifies that an event occured with the control key held down.
            /// </summary>
            Ctrl,
            /// <summary>
            /// A modifier which signifies that an event occured with the left mouse button held down.
            /// </summary>
            LeftClick,
            /// <summary>
            /// A modifier which signifies that an event occured with the middle mouse button held down.
            /// </summary>
            MiddleClick,
            /// <summary>
            /// A modifier which signifies that an event occured with the right mouse button held down.
            /// </summary>
            RightClick
        }

        #endregion


        #region Public methods

        virtual public void PumpDevice()
        {
        }

        public int GetObjectId(String objectType)
        {
            if (_objectIdList != null)
            {
                for (int i = 0; i < _objectIdList.Count; i++)
                {
                    if (_objectIdList[i].Key == objectType)
                        return _objectIdList[i].Value;
                }
            }
            return -1;
        }

        public String GetObjectType(int id)
        {
            if (_objectIdList != null)
            {
                for (int i = 0; i < _objectIdList.Count; i++)
                {
                    if (_objectIdList[i].Value == id)
                        return _objectIdList[i].Key;
                }
            }
            return "none";
        }

        public int GetNumObjects()
        {
            return _objectIdList == null ? 0 : _objectIdList.Count;
        }

        public int GetObjectByIndex(int index)
        {
            if (_objectIdList == null || index < 0 || index >= _objectIdList.Count - 1)
                return -1;
            return _objectIdList[index].Value;
        }

        public bool IsValidObject(String objectType)
        {
            return GetObjectId(objectType) != -1;
        }

        public bool IsValidObject(int id)
        {
            if (_objectIdList != null)
            {
                for (int i = 0; i < _objectIdList.Count; i++)
                {
                    if (_objectIdList[i].Value == id)
                        return true;
                }
            }
            return false;
        }

        #endregion


        #region Private, protected, internal methods

        internal void _setDeviceIdentifiers(int deviceNumber, int deviceInstance)
        {
            _deviceNumber = deviceNumber;
            _deviceInstance = deviceInstance;
        }

        protected InputEventData _getEventData(int objectId)
        {
            InputEventData ret = new TorqueInputDevice.InputEventData();
            ret.DeviceNumber = _deviceNumber;
            ret.DeviceTypeId = _deviceTypeId;
            ret.DeviceInstance = _deviceNumber;
            ret.ObjectId = objectId;
            return ret;
        }

        protected void _initDevice(String eventName, int unused)
        {
        }

        #endregion


        #region Private, protected, internal fields

        static int _mouseId;
        static int _gamepadId;
        static int _keyboardId;

        static int _nextDeviceId = 1;
        static Dictionary<String, int> _deviceTypeToId = new Dictionary<string, int>();
        static Dictionary<int, String> _deviceIdToType = new Dictionary<int, string>();

        static TorqueEvent<InputEventData> _mouseEvent = new TorqueEvent<InputEventData>("MouseEvent");
        static TorqueEvent<InputEventData> _keyboardEvent = new TorqueEvent<InputEventData>("KeyboardEvent");
        static TorqueEvent<InputEventData> _gamepadEvent = new TorqueEvent<InputEventData>("GamepadEvent");

        protected int _deviceNumber;
        protected int _deviceTypeId;
        internal int _deviceInstance;
        protected List<KeyValuePair<String, int>> _objectIdList;

        #endregion
    }

    /// <summary>
    /// The InputManager has 4 basic purposes.  1) It is the central place for registering and looking up input devices.
    /// 2) All input is received and then routed by InputManager.  The global InputMap gets the first opportunity to
    /// process the input, followed by the GUI system, and finally each pushed InputMap has an opportunity to process the
    /// input.  3) The InputMap stack is stored on the InputManager.  Use PushInputMap and PopInputMap to manipulate the
    /// stack.  4) The InputManager provides a shortcut to activate vibrations on gamepad devices.  See SetVibration, 
    /// SetLowFrequencyVibration, SetHighFrequencyVibration, StopVibration, and StopAllVibration.
    /// </summary>
    public class InputManager
    {

        #region Static methods, fields, constructors

        static public InputManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new InputManager();

                return _instance;
            }
        }

        static InputManager _instance;

        #endregion


        #region Constructors

        private InputManager() { }

        #endregion


        #region Public properties, operators, constants, and enums

        /// <summary>
        /// Returns whether or not a device has been added to the input manager. This is a good way
        /// to determine if input has been initialized yet.
        /// </summary>
        public bool HasDevices
        {
            get { return _devices != null && _devices.Count > 0; }
        }

        #endregion


        #region Public methods

        public void Pump(String eventName, float elapsed)
        {
            for (int i = 0; i < _devices.Count; i++)
                _devices[i].PumpDevice();
        }

        /// <summary>
        /// Register an input device with the InputManager.
        /// </summary>
        /// <param name="device">Input device to register.</param>
        /// <returns>Index of device in input manager.</returns>
        public int AddDevice(TorqueInputDevice device)
        {
            // for sanity sake, make sure we aren't already added
            // while we're at it, get highest count of similar devices
            int count = -1;
            for (int i = 0; i < _devices.Count; i++)
            {
                if (_devices[i] == device)
                    return i;
                if (_devices[i].DeviceTypeId == device.DeviceTypeId)
                    count = _devices[i]._deviceInstance;
            }

            _devices.Add(device);
            // Note: we won't worry too much about repeating old device instance
            // numbers since we aren't geared toward adding/removing devices
            // and re-using number should work fine anyway.  But take the trivial
            // precaution of choosing max prior instance +1 rather than simply
            // counting current instances +1 (otherwise could get a duplication).
            device._setDeviceIdentifiers(_devices.Count - 1, count + 1);
            return _devices.Count - 1;
        }

        /// <summary>
        /// Unregister a device from the input manager.
        /// </summary>
        /// <param name="device"></param>
        /// <returns></returns>
        public bool RemoveDevice(TorqueInputDevice device)
        {
            for (int i = 0; i < _devices.Count; i++)
                if (_devices[i] == device)
                {
                    _devices.RemoveAt(i);
                    return true;
                }
            return false;
        }

        /// <summary>
        /// Return the indexed device.  Indices go from 0 to GetNumDevices().
        /// </summary>
        /// <param name="index">Index of device.</param>
        /// <returns>Indexed device or null if out of range.</returns>
        public TorqueInputDevice GetDevice(int index)
        {
            if (index < 0 || index >= _devices.Count)
                return null;
            return _devices[index];
        }

        /// <summary>
        /// Total number of input devices currently registered.
        /// </summary>
        /// <returns>Number of devices.</returns>
        public int GetNumDevices()
        {
            return _devices.Count;
        }

        /// <summary>
        /// Find device using fully specified device string, including device instance specifier.  E.g.,
        /// the string "gamepad3" will find the device of type "gamepad" with instance number of 3.
        /// </summary>
        /// <param name="deviceName">Full name of device to find.</param>
        /// <returns>Found device index or -1 if none found.</returns>
        public int FindDevice(String deviceName)
        {
            for (int i = 0; i < GetNumDevices(); i++)
            {
                TorqueInputDevice device = GetDevice(i);
                String name = TorqueInputDevice.GetDeviceTypeName(device.DeviceTypeId);
                if (!deviceName.StartsWith(name))
                    continue;
                String numStr = deviceName.Substring(name.Length);
                int num = 0;
                if (numStr.Length > 0)
                {
                    // convert number, but ignore exception
                    try
                    {
                        num = Convert.ToInt32(numStr);
                    }
                    catch (FormatException)
                    {
                        num = -1;
                    }
                }
                if (device._deviceInstance == num)
                    return i;
            }
            return -1;
        }

        /// <summary>
        /// Push InputMap onto the InputMap stack.  Bindings in this map will be processed after all
        /// currently pushed maps.  If already pushed, this map will first be popped.
        /// </summary>
        /// <param name="map">Input map to push.</param>
        public void PushInputMap(InputMap map)
        {
            _inputMaps.Remove(map);
            _inputMaps.Add(map);
        }

        /// <summary>
        /// Pop InputMap off the InputMap stack.  Bindings in this map will no longer be processed by
        /// InputManager.
        /// </summary>
        /// <param name="map">Input map to pop.</param>
        public void PopInputMap(InputMap map)
        {
            _inputMaps.Remove(map);
        }

        /// <summary>
        /// Output to TextWriter all the registered input devices.
        /// </summary>
        /// <param name="writer">TextWriter to dump device to.</param>
        public void Dump(TextWriter writer)
        {
            // Test code for dumping all input device info using publicly accessible methods only.
            for (int i = 0; i < GetNumDevices(); i++)
            {
                TorqueInputDevice device = GetDevice(i);
                String typename = TorqueInputDevice.GetDeviceTypeName(device.DeviceTypeId);
                writer.WriteLine("Input device #{0}, Type id {1}, Type name {2}, Type instance {3}",
                    device.DeviceNumber, device.DeviceTypeId, typename, device._deviceInstance);
                for (int j = 0; j < device.GetNumObjects(); j++)
                {
                    int objId = device.GetObjectByIndex(j);
                    writer.WriteLine("   Object #{0}, id = {1}, type name = {2}", j, objId, device.GetObjectType(objId));
                }
            }
        }

        /// <summary>
        /// Callback to TorqueInputEvent -- all input in Torque X is passed via this method.
        /// </summary>
        /// <param name="eventName">Name of event for which this callback.</param>
        /// <param name="inputData">Input data.</param>
        public void ProcessInputEvent(String eventName, TorqueInputDevice.InputEventData inputData)
        {
            // send input to global input map
            if (InputMap.Global.ProcessInput(inputData))
                return;

            // if not handled, send to gui
            if (GUICanvas.Instance.ProcessInput(inputData))
                return;

            // if not handled, send to rest of the input maps (go in reverse order so most recent push first)
            for (int i = _inputMaps.Count - 1; i >= 0; i--)
            {
                InputMap map = _inputMaps[i];
                if (map.ProcessInput(inputData))
                    return;
            }
        }


        #region Gamepad vibration public methods

        /// <summary>
        /// Sets the vibration motor speeds for the given gamepad device.
        /// </summary>
        /// <param name="gamepad">Index of gamepad device</param>
        /// <param name="lowSpeed">The speed of the low-frequency motor, between 0.0 and 1.0</param>
        /// <param name="highSpeed">The speed of the high-frequency motor, between 0.0 and 1.0</param>
        public void SetVibration(int gamepad, float lowSpeed, float highSpeed)
        {
            TorqueInputDevice device = GetDevice(gamepad);
            if (device is XGamePadDevice)
            {
                XGamePadDevice gp = (XGamePadDevice)device;
                gp.SetVibration(lowSpeed, highSpeed);
            }
        }

        /// <summary>
        /// Sets the low frequency motor vibration speed for the given gamepad device.
        /// </summary>
        /// <param name="gamepad">Index of gamepad device</param>
        /// <param name="lowSpeed">The speed of the low-frequency motor, between 0.0 and 1.0</param>
        public void SetLowFrequencyVibration(int gamepad, float lowSpeed)
        {
            TorqueInputDevice device = GetDevice(gamepad);
            if (device is XGamePadDevice)
            {
                XGamePadDevice gp = (XGamePadDevice)device;
                gp.SetLowFrequencyVibration(lowSpeed);
            }
        }

        /// <summary>
        /// Sets the high frequency motor vibration speed for the given gamepad device.
        /// </summary>
        /// <param name="gamepad">Index of gamepad device</param>
        /// <param name="highSpeed">The speed of the high-frequency motor, between 0.0 and 1.0</param>
        public void SetHighFrequencyVibration(int gamepad, float highSpeed)
        {
            TorqueInputDevice device = GetDevice(gamepad);
            if (device is XGamePadDevice)
            {
                XGamePadDevice gp = (XGamePadDevice)device;
                gp.SetHighFrequencyVibration(highSpeed);
            }
        }

        /// <summary>
        /// Stop vibration of both motors for the given gamepad device.
        /// </summary>
        /// <param name="gamepad">Index of gamepad device</param>
        public void StopVibration(int gamepad)
        {
            TorqueInputDevice device = GetDevice(gamepad);
            if (device is XGamePadDevice)
            {
                XGamePadDevice gp = (XGamePadDevice)device;
                gp.StopVibration();
            }
        }

        /// <summary>
        /// Stops vibration of both motors for all gamepad devices.
        /// </summary>
        public void StopAllVibration()
        {
            for (int i = 0; i < GetNumDevices(); i++)
            {
                TorqueInputDevice device = GetDevice(i);
                if (device is XGamePadDevice)
                {
                    XGamePadDevice gp = (XGamePadDevice)device;
                    gp.StopVibration();
                }
            }
        }

        #endregion

        #endregion


        #region Private, protected, internal fields

        List<TorqueInputDevice> _devices = new List<TorqueInputDevice>();
        List<InputMap> _inputMaps = new List<InputMap>();

        #endregion
    }
}
