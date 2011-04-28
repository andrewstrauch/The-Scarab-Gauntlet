//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using GarageGames.Torque.Core;
using GarageGames.Torque.T2D;
using GarageGames.Torque.Sim;
using GarageGames.Torque.Platform;



namespace GarageGames.Torque.T2D
{
    /// <summary>
    /// This component automatically sets up an input map with all buttons, sticks,
    /// and triggers bound. Keyboard bindings are also setup to somewhat mirror
    /// those on the controller.
    /// </summary>

    [TorqueXmlSchemaType]
    public class T2DControlComponent : TorqueComponent
    {
        #region Public properties, operators, constants, and enums

        /// <summary>
        /// This number defines which game pad the input map will be set up for.
        /// </summary>
        public int PlayerNumber
        {
            get { return _playerNumber; }
            set { _playerNumber = value; }
        }

        #endregion



        #region Private, protected, internal methods

        protected override bool _OnRegister(TorqueObject owner)
        {
            if (!base._OnRegister(owner) || !(Owner is T2DSceneObject))
                return false;

            T2DSceneObject sceneObject = owner as T2DSceneObject;
            _SetupInputMap(sceneObject, _playerNumber, "gamepad" + _playerNumber, "keyboard");

            return true;
        }



        private void _SetupInputMap(TorqueObject player, int playerIndex, String gamePad, String keyboard)
        {
            // Set player as the controllable object
            PlayerManager.Instance.GetPlayer(playerIndex).ControlObject = player;

            // Get input map for this player and configure it
            InputMap inputMap = PlayerManager.Instance.GetPlayer(playerIndex).InputMap;

            int gamepadId = InputManager.Instance.FindDevice(gamePad);
            if (gamepadId >= 0)
            {
                // left stick
                inputMap.BindMove(gamepadId, (int)XGamePadDevice.GamePadObjects.LeftThumbX, MoveMapTypes.StickAnalogHorizontal, 0);
                inputMap.BindMove(gamepadId, (int)XGamePadDevice.GamePadObjects.LeftThumbY, MoveMapTypes.StickAnalogVertical, 0);

                // right stick
                inputMap.BindMove(gamepadId, (int)XGamePadDevice.GamePadObjects.RightThumbX, MoveMapTypes.StickAnalogHorizontal, 1);
                inputMap.BindMove(gamepadId, (int)XGamePadDevice.GamePadObjects.RightThumbY, MoveMapTypes.StickAnalogVertical, 1);

                // d pad
                inputMap.BindMove(gamepadId, (int)XGamePadDevice.GamePadObjects.Up, MoveMapTypes.StickDigitalUp, 0);
                inputMap.BindMove(gamepadId, (int)XGamePadDevice.GamePadObjects.Down, MoveMapTypes.StickDigitalDown, 0);
                inputMap.BindMove(gamepadId, (int)XGamePadDevice.GamePadObjects.Left, MoveMapTypes.StickDigitalLeft, 0);
                inputMap.BindMove(gamepadId, (int)XGamePadDevice.GamePadObjects.Right, MoveMapTypes.StickDigitalRight, 0);

                // face buttons
                inputMap.BindMove(gamepadId, (int)XGamePadDevice.GamePadObjects.A, MoveMapTypes.Button, 0);
                inputMap.BindMove(gamepadId, (int)XGamePadDevice.GamePadObjects.B, MoveMapTypes.Button, 1);
                inputMap.BindMove(gamepadId, (int)XGamePadDevice.GamePadObjects.X, MoveMapTypes.Button, 2);
                inputMap.BindMove(gamepadId, (int)XGamePadDevice.GamePadObjects.Y, MoveMapTypes.Button, 3);

                // bumpers
                inputMap.BindMove(gamepadId, (int)XGamePadDevice.GamePadObjects.LeftShoulder, MoveMapTypes.Button, 4);
                inputMap.BindMove(gamepadId, (int)XGamePadDevice.GamePadObjects.RightShoulder, MoveMapTypes.Button, 5);

                // start/back
                inputMap.BindMove(gamepadId, (int)XGamePadDevice.GamePadObjects.Start, MoveMapTypes.Button, 6);
                inputMap.BindMove(gamepadId, (int)XGamePadDevice.GamePadObjects.Back, MoveMapTypes.Button, 7);

                // stick buttons
                inputMap.BindMove(gamepadId, (int)XGamePadDevice.GamePadObjects.LeftThumbButton, MoveMapTypes.Button, 8);
                inputMap.BindMove(gamepadId, (int)XGamePadDevice.GamePadObjects.RightThumbButton, MoveMapTypes.Button, 9);

                // triggers
                inputMap.BindMove(gamepadId, (int)XGamePadDevice.GamePadObjects.LeftTrigger, MoveMapTypes.TriggerAnalog, 0);
                inputMap.BindMove(gamepadId, (int)XGamePadDevice.GamePadObjects.RightTrigger, MoveMapTypes.TriggerAnalog, 1);
            }

            // Keyboard controls. These are really somewhat arbitrary. This could probably be expanded
            // to allow editor selection of the keys.
            int keyboardId = InputManager.Instance.FindDevice(keyboard);

            if (keyboardId >= 0)
            {
                // arrows
                inputMap.BindMove(keyboardId, (int)Keys.Right, MoveMapTypes.StickDigitalRight, 0);
                inputMap.BindMove(keyboardId, (int)Keys.Left, MoveMapTypes.StickDigitalLeft, 0);
                inputMap.BindMove(keyboardId, (int)Keys.Up, MoveMapTypes.StickDigitalUp, 0);
                inputMap.BindMove(keyboardId, (int)Keys.Down, MoveMapTypes.StickDigitalDown, 0);

                // wasd
                inputMap.BindMove(keyboardId, (int)Keys.D, MoveMapTypes.StickDigitalRight, 1);
                inputMap.BindMove(keyboardId, (int)Keys.A, MoveMapTypes.StickDigitalLeft, 1);
                inputMap.BindMove(keyboardId, (int)Keys.W, MoveMapTypes.StickDigitalUp, 1);
                inputMap.BindMove(keyboardId, (int)Keys.S, MoveMapTypes.StickDigitalDown, 1);

                // face buttons
                inputMap.BindMove(keyboardId, (int)Keys.J, MoveMapTypes.Button, 0);
                inputMap.BindMove(keyboardId, (int)Keys.K, MoveMapTypes.Button, 1);
                inputMap.BindMove(keyboardId, (int)Keys.I, MoveMapTypes.Button, 2);
                inputMap.BindMove(keyboardId, (int)Keys.L, MoveMapTypes.Button, 3);

                // bumpers
                inputMap.BindMove(keyboardId, (int)Keys.Q, MoveMapTypes.Button, 4);
                inputMap.BindMove(keyboardId, (int)Keys.O, MoveMapTypes.Button, 5);

                // start/back
                inputMap.BindMove(keyboardId, (int)Keys.R, MoveMapTypes.Button, 6);
                inputMap.BindMove(keyboardId, (int)Keys.F, MoveMapTypes.Button, 7);

                // stick buttons
                inputMap.BindMove(keyboardId, (int)Keys.Y, MoveMapTypes.Button, 8);
                inputMap.BindMove(keyboardId, (int)Keys.H, MoveMapTypes.Button, 9);

                // triggers
                inputMap.BindMove(keyboardId, (int)Keys.E, MoveMapTypes.TriggerAnalog, 0);
                inputMap.BindMove(keyboardId, (int)Keys.U, MoveMapTypes.TriggerAnalog, 1);
            }
        }

        #endregion


        #region Private, protected, internal fields

        int _playerNumber = 0;

        #endregion
    }
}
