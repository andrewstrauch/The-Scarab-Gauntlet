using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Xna.Framework;

using GarageGames.Torque.Core;
using GarageGames.Torque.Sim;
using GarageGames.Torque.T2D;
using GarageGames.Torque.XNA;
using GarageGames.Torque.Platform;

using GarageGames.Torque.PlatformerFramework;



namespace PlatformerStarter
{
    public class PlayerController : ActorController, ITickObject
    {
        //======================================================
        #region Constructors

        public PlayerController()
        {
            // register for a tick callback
            ProcessList.Instance.AddTickCallback(this, this);

            // add ourselves to the game's player list
            Game.Instance.Players.Add(this);

            // setup the input map for this player
            _setupInputMap();

            playerHasControl = true;
        }

        #endregion

        //======================================================
        #region Public methods

        public virtual void ProcessTick(Move move, float elapsed)
        {
            if (playerHasControl)
            {
                // check if move exists
                if (move != null)
                {
                    // set horizontal actor movement flags
                    if (move.Sticks[0].X < 0)
                        _moveLeft();
                    else if (move.Sticks[0].X > 0)
                        _moveRight();
                    else
                        _horizontalStop();

                    // keep track of whether or not the down button is pressed
                    // (so we know whether this is a normal jump, or a down-jump)
                    bool movingDown = false;

                    // set vertical actor movement flags
                    if (move.Sticks[0].Y < -0.25)
                    {
                        _moveDown();
                        movingDown = true;
                    }
                    else if (move.Sticks[0].Y > 0.25)
                        _moveUp();
                    else
                        _verticalStop();

                    // set jump only on initial button down and button release
                    if (move.Buttons[0].Pushed)
                    {
                        if (!_jumpButton)
                        {
                            _jump();

                            if (movingDown)
                                _jumpDown();

                            _jumpButton = true;
                        }
                    }
                    else if (_jumpButton)
                    {
                        _jumpButton = false;
                    }

                    // set attack only on initial button down and button release
                    if (move.Buttons[1].Pushed)
                    {
                        if (!_attackButton)
                        {
                            foreach (PlayerActorComponent actor in Movers)
                                actor.Punch();

                            _attackButton = true;
                        }
                    }

                    else if (move.Buttons[2].Pushed)
                    {
                        if (!_attackButton)
                        {
                            foreach (PlayerActorComponent actor in Movers)
                                actor.Swipe();

                            _attackButton = true;
                        }
                    }

                    else if (move.Triggers[0].Value == 1)
                    {
                        if (!_attackButton)
                        {
                            foreach (PlayerActorComponent actor in Movers)
                                actor.Shoot();

                            _attackButton = true;
                        }
                    }

                    else if (_attackButton)
                    {
                        _attackButton = false;
                    }
                }
            }
        }
        
        public virtual void InterpolateTick(float k) { }

        public void TogglePlayerControl()
        {
            playerHasControl = !playerHasControl;
        }

        #endregion

        //======================================================
        #region Private, protected, internal methods

        // setup user input
        protected void _setupInputMap()
        {
            MoveManager moveManager = new MoveManager();
            ProcessList.Instance.SetMoveManager(this, moveManager);
            InputMap inputMap = new InputMap();
            inputMap.MoveManager = moveManager;

            int gamePadNumber = Game.Instance.Players.Count - 1;
            int gamepadId = InputManager.Instance.FindDevice("gamepad" + gamePadNumber);
            inputMap.BindMove(gamepadId, (int)XGamePadDevice.GamePadObjects.LeftThumbX, MoveMapTypes.StickAnalogHorizontal, 0);
            inputMap.BindMove(gamepadId, (int)XGamePadDevice.GamePadObjects.LeftThumbY, MoveMapTypes.StickAnalogVertical, 0);
            inputMap.BindMove(gamepadId, (int)XGamePadDevice.GamePadObjects.A, MoveMapTypes.Button, 0);
            inputMap.BindMove(gamepadId, (int)XGamePadDevice.GamePadObjects.X, MoveMapTypes.Button, 1);
            inputMap.BindMove(gamepadId, (int)XGamePadDevice.GamePadObjects.Y, MoveMapTypes.Button, 2);
            inputMap.BindMove(gamepadId, (int)XGamePadDevice.GamePadObjects.LeftTrigger, MoveMapTypes.TriggerDigital, 0);
            inputMap.BindCommand(gamepadId, (int)XGamePadDevice.GamePadObjects.Back, null, Game.Instance.Exit);
            inputMap.BindCommand(gamepadId, (int)XGamePadDevice.GamePadObjects.Start, null, Game.Instance.TogglePause); 

#if !XBOX
            int keyboardId = InputManager.Instance.FindDevice("keyboard0");
            inputMap.BindMove(keyboardId, (int)Microsoft.Xna.Framework.Input.Keys.Space, MoveMapTypes.Button, 0);
            inputMap.BindMove(keyboardId, (int)Microsoft.Xna.Framework.Input.Keys.D, MoveMapTypes.StickDigitalRight, 0);
            inputMap.BindMove(keyboardId, (int)Microsoft.Xna.Framework.Input.Keys.A, MoveMapTypes.StickDigitalLeft, 0);
            inputMap.BindMove(keyboardId, (int)Microsoft.Xna.Framework.Input.Keys.S, MoveMapTypes.StickDigitalDown, 0);
            inputMap.BindMove(keyboardId, (int)Microsoft.Xna.Framework.Input.Keys.U, MoveMapTypes.Button, 1);
            inputMap.BindMove(keyboardId, (int)Microsoft.Xna.Framework.Input.Keys.I, MoveMapTypes.Button, 2);
            inputMap.BindMove(keyboardId, (int)Microsoft.Xna.Framework.Input.Keys.LeftShift, MoveMapTypes.Button, 3);
            inputMap.BindCommand(keyboardId, (int)Microsoft.Xna.Framework.Input.Keys.P, null, Game.Instance.TogglePause);
            inputMap.BindCommand(keyboardId, (int)Microsoft.Xna.Framework.Input.Keys.R, null, Game.Instance.Reset);
            inputMap.BindCommand(keyboardId, (int)Microsoft.Xna.Framework.Input.Keys.Escape, null, Game.Instance.Exit);
#endif

            InputManager.Instance.PushInputMap(inputMap.CloneInputMap());
        }

        #endregion

        //======================================================
        #region Private, protected, internal fields

        // field to store the jump button value
        // (so we can tell when new jump events should be triggered)
        private bool _jumpButton;
        private bool _attackButton;
        private bool playerHasControl;

        #endregion
    }
}