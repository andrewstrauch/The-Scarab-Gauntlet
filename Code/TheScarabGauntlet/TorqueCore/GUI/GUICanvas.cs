//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using GarageGames.Torque.Platform;
using GarageGames.Torque.Core;
using GarageGames.Torque.Sim;
using GarageGames.Torque.GFX;
using GarageGames.Torque.Materials;
using GarageGames.Torque.MathUtil;
using GarageGames.Torque.SceneGraph;
using GarageGames.Torque.Util;



namespace GarageGames.Torque.GUI
{
    /// <summary>
    /// The GUICanvas provides the drawing area, so to speak, for all GUIControls.
    /// It is the bottom most control in a GUI hierarchy, all GUIControls must add
    /// themselves to the GUICanvas when they want to be displayed and remove
    /// themselves when they don't. Only one GUICanvas can exist in an application,
    /// and it is created automatically by TorqueEngineComponent.
    /// </summary>
    public class GUICanvas : GUIControl
    {

        #region Static methods, fields, constructors

        /// <summary>
        /// Get the GUICanvas singleton.
        /// </summary>
        public static GUICanvas Instance
        {
            get { return _instance; }
        }

        static GUICanvas _instance;

        #endregion


        #region Constructors

        /// <summary>
        /// Creates a new GUICanvas. Only one may exist.
        /// </summary>
        public GUICanvas()
        {
            Assert.Fatal(GUICanvas._instance == null, "GUICanvas Constructor - GUICanvas already exists.");
            GUICanvas._instance = this;

            _awake = true;
            _visible = true;

            Name = "Canvas";

            // initialize the drawing utility
            DrawUtil.Setup();
        }

        #endregion


        #region Public properties, operators, constants, and enums

        /// <summary>
        /// Used by the DrawUtil class to render materials.
        /// </summary>
        public SceneRenderState RenderState
        {
            get { return _srs; }
        }



        /// <summary>
        /// Provide a GUIControl that may be used when any letter boxing is
        /// visible. Letter boxing may become visible when any GUI content is
        /// preserving it's aspect ratio. Typically a control that fills all
        /// black is used to give the letter box effect.
        /// </summary>
        public GUIControl LetterBoxControl
        {
            get { return _letterBoxControl; }
            set { _letterBoxControl = value; }
        }



        /// <summary>
        /// Defines NTSC safe zones.
        /// </summary>
        public enum SafeAreas
        {
            None,
            Action,
            ActionTitle,
            ActionOutline,
            ActionTitleOutline,
        }



        /// <summary>
        /// Enables the drawing of NTSC safe zones. Safe zones help a GUI designer
        /// avoid placing controls in bad regions of the screen. Safe zones are
        /// guaranteed to be visible on most, if not all, televisions.
        /// </summary>
        public SafeAreas SafeAreaRendering
        {
            get { return _safeAreaRendering; }
            set { _safeAreaRendering = value; }
        }



        public float SafeAreaMul
        {
            get { return _safeAreaMul; }
            set { _safeAreaMul = value; _CalculateRenderSafeBoundries(); }
        }



        public Vector2 SafeAreaSize
        {
            get { return _safeAreaSize; }
        }



        public RectangleF SafeActionBoundry
        {
            get { return _safeActionBoundry; }
        }



        public RectangleF SafeTitleBoundry
        {
            get { return _safeTitleBoundry; }
        }

        #endregion


        #region Public methods

        /// <summary>
        /// Begins the drawing of all GUIControls in the GUICanvas hierarchy.
        /// </summary>
        public void RenderFrame()
        {
            OnPreRender();
            if (width == 0 || height == 0)
                return;
            OnRender(_bounds.Point, _bounds);
        }



        /// <summary>
        /// Called before OnRender, retrieves current window resolution and
        /// recalculates the bounds if necessary.
        /// </summary>
        public override void OnPreRender()
        {
#if DEBUG
            Profiler.Instance.StartBlock("GUICanvas.OnPreRender");
#endif

            _srs.Gfx = GFXDevice.Instance;

            // get screen resolution
            height = GFX.GFXDevice.Instance.CurrentVideoMode.BackbufferHeight;
            width = GFX.GFXDevice.Instance.CurrentVideoMode.BackbufferWidth;

            if (width == 0 || height == 0)
            {
#if DEBUG
                Profiler.Instance.EndBlock("GUICanvas.OnPreRender");
#endif
                return;
            }

            // set canvas size regardless of aspect ratio
            if (_bounds.Width != width || _bounds.Height != height)
            {
                screenRect = new RectangleF(0, 0, width, height);
                _bounds = screenRect;
                _CalculateRenderSafeBoundries();
            }

#if DEBUG
            Profiler.Instance.EndBlock("GUICanvas.OnPreRender");
#endif
        }



        /// <summary>
        /// Called when the GUICanvas needs to render.
        /// </summary>
        /// <param name="offset">The location of the canvas.</param>
        /// <param name="updateRect">The size of the canvas.</param>
        public override void OnRender(Vector2 offset, RectangleF updateRect)
        {
#if DEBUG
            Profiler.Instance.StartBlock("GUICanvas.OnRender");
#endif

            numObjects = GetNumObjects();

            // pre-render phase
            renderLetterbox = false;

            for (idx = 0; idx < numObjects; idx++)
            {
                contentCtrl = (GUIControl)GetObject(idx);
                if (contentCtrl == null)
                    break;

                // pre-render the content
                contentCtrl.OnPreRender();

                if (contentCtrl.Style.PreserveAspectRatio && _letterBoxControl != null)
                {
                    if (contentCtrl.Bounds != _bounds)
                        renderLetterbox = true;
                }
            }

            // letterbox render phase
            if (renderLetterbox)
            {
                DrawUtil.ClipRect = _bounds;
                _letterBoxControl.Bounds = _bounds;
                _letterBoxControl.OnRender(_letterBoxControl.Position, _bounds);
            }

            // render phase
            for (idx = 0; idx < numObjects; idx++)
            {
                contentCtrl = (GUIControl)GetObject(idx);
                if (contentCtrl == null)
                    break;

                // clip to the control's bounds
                DrawUtil.ClipRect = contentCtrl.Bounds;

                // render the content
                contentCtrl.OnRender(contentCtrl.Position, contentCtrl.Bounds);
            }

            // safe area render phase
            if (_safeAreaRendering != SafeAreas.None)
            {
                DrawUtil.ClipRect = _bounds;
                _RenderSafeAreas();
            }

            // important, because controls can change the clip
            DrawUtil.ClipRect = _bounds;

#if DEBUG
            Profiler.Instance.EndBlock("GUICanvas.OnRender");
#endif
        }



        public override void SetBounds(Vector2 newPosition, Vector2 newSize)
        {
            base.SetBounds(newPosition, newSize);

            _CalculateRenderSafeBoundries();
        }



        /// <summary>
        /// Changes the currently rendering content control.
        /// </summary>
        /// <param name="name">The name of the content control to change to.</param>
        public void SetContentControl(string name)
        {
            GUIControl ctrl = (GUIControl)TorqueObjectDatabase.Instance.FindObject(name);
            SetContentControl(ctrl);
        }



        /// <summary>
        /// Changes the currently rendering content control.
        /// </summary>
        /// <param name="ctrl">The content control object to change to.</param>
        public void SetContentControl(GUIControl ctrl)
        {
            if (ctrl == null)
                return;

            // remove all dialogs on layer 0
            int index = 0;

            while (GetNumObjects() > index)
            {
                GUIControl child = (GUIControl)GetObject(index);
                if (child == null)
                    continue;

                if (child == ctrl || child.Layer != 0)
                {
                    index++;
                    continue;
                }

                child.Folder = TorqueObjectDatabase.Instance.RootFolder;
            }

            // lose the first responder from the old content
            ClearFocusControl();

            // add the ctrl to the front
            if (GetNumObjects() == 0 || ctrl != GetObject(0))
            {
                if (ctrl == null)
                    return;

                ctrl.Folder = this;

                if (GetNumObjects() >= 2)
                    ReOrder(ctrl, GetObject(0));
            }
        }



        /// <summary>
        /// Adds a dialog control onto the GUICanvas stack.
        /// </summary>
        /// <param name="name">The name of the dialog control.</param>
        /// <param name="layer">The layer to put the dialog control on. Usually layer 0.</param>
        public void PushDialogControl(string name, int layer)
        {
            GUIControl gui = (GUIControl)TorqueObjectDatabase.Instance.FindObject(name);
            PushDialogControl(gui, layer);
        }



        /// <summary>
        /// Adds a dialog control onto the GUICanvas stack.
        /// </summary>
        /// <param name="gui">The dialog control to add.</param>
        /// <param name="layer">The layer to put the dialog control on. Usually layer 0.</param>
        public void PushDialogControl(GUIControl gui, int layer)
        {
            // set the dialog layer
            gui.Layer = layer;
            gui.Folder = this;

            int idx;
            int numObjects = GetNumObjects();

            // reorder the controls into the correct layers
            for (idx = 0; idx < numObjects; idx++)
            {
                GUIControl ctrl = (GUIControl)GetObject(idx);
                if (ctrl == null)
                    return;

                if (ctrl.Layer > gui.Layer)
                {
                    ReOrder(gui, ctrl);
                    break;
                }
            }
        }



        /// <summary>
        /// Removes a dialog control from the GUICanvas stack.
        /// </summary>
        /// <param name="name">The name of the dialog control.</param>
        public void PopDialogControl(string name)
        {
            if (GetNumObjects() < 1)
                return;

            // find the dialog
            GUIControl ctrl = (GUIControl)FindObject(name, false);
            PopDialogControl(ctrl);
        }



        /// <summary>
        /// Removes a dialog control from the GUICanvas stack.
        /// </summary>
        /// <param name="gui">The dialog control to remove.</param>
        public void PopDialogControl(GUIControl gui)
        {
            if (gui == null)
                return;

            // remove the dialog from the canvas gui hierarchy
            gui.Folder = TorqueObjectDatabase.Instance.RootFolder;

            ClearFocusControl();
        }



        /// <summary>
        /// Called by the InputManager. First tries to pass the input event to the
        /// current focus control. If no focus control is present, then tries to 
        /// pass the input event to the current content control. If neither exists
        /// or handles the event, the input will not be handled.
        /// </summary>
        /// <param name="data">The input event data from the InputManager.</param>
        /// <returns>True if a control handled the input event.</returns>
        public bool ProcessInput(TorqueInputDevice.InputEventData data)
        {
            if (_focusControl != null)
            {
                // if the first responder or one of its parents handles the event
                if (_focusControl.OnInputEvent(ref data))
                    return true;
            }
            else
            {
                if (GetNumObjects() > 0)
                {
                    // since we don't have a focus control use the content control
                    GUIControl content = (GUIControl)GetObject(0);

                    if (content != null)
                    {
                        if (content.OnInputEvent(ref data))
                            return true;
                    }
                }
            }

            // if we are here, then no controls handled the input
            return false;
        }



        /// <summary>
        /// The general input handler for the canvas. Input events are routed here if a
        /// focus control or content control did not want to handle the input event. The
        /// canvas can use the input event to determine if the focus control needs to
        /// change.
        /// </summary>
        /// <param name="data">The input event data from the InputManager.</param>
        /// <returns>True if the canvas handled the input event.</returns>
        public override bool OnInputEvent(ref TorqueInputDevice.InputEventData data)
        {
            // handle keyboard input
            if (data.DeviceTypeId == TorqueInputDevice.KeyboardId)
            {
                // tab focus
                if (data.ObjectId == (int)Microsoft.Xna.Framework.Input.Keys.Tab)
                {
                    if (data.EventAction == TorqueInputDevice.Action.Make)
                    {
                        if ((data.Modifier & TorqueInputDevice.Action.Shift) != 0)
                        {
                            if (FocusPrev(SearchPolicy.Horizontal))
                                return true;
                            else if (FocusPrev(SearchPolicy.Vertical))
                                return true;
                        }
                        else if (data.Modifier == TorqueInputDevice.Action.None)
                        {
                            if (FocusNext(SearchPolicy.Horizontal))
                                return true;
                            else if (FocusNext(SearchPolicy.Vertical))
                                return true;
                        }
                    }
                }
            }

            // handle mouse input
            else if (data.DeviceTypeId == TorqueInputDevice.MouseId)
            {
                // rdbtodo: need to handle mouse input, pick controls, etc.
                // rdbtodo: drawing mouse cursor
            }

            // handle gamepad input
            else if (data.DeviceTypeId == TorqueInputDevice.GamePadId)
            {
                // directional focus change
                if (data.EventAction == TorqueInputDevice.Action.Make)
                {
                    switch (data.ObjectId)
                    {
                        case (int)XGamePadDevice.GamePadObjects.LeftThumbLeftButton:
                        case (int)XGamePadDevice.GamePadObjects.Left:
                            if (FocusPrev(SearchPolicy.Horizontal))
                                return true;
                            break;

                        case (int)XGamePadDevice.GamePadObjects.LeftThumbRightButton:
                        case (int)XGamePadDevice.GamePadObjects.Right:
                            if (FocusNext(SearchPolicy.Horizontal))
                                return true;
                            break;

                        case (int)XGamePadDevice.GamePadObjects.LeftThumbUpButton:
                        case (int)XGamePadDevice.GamePadObjects.Up:
                            if (FocusPrev(SearchPolicy.Vertical))
                                return true;
                            break;

                        case (int)XGamePadDevice.GamePadObjects.LeftThumbDownButton:
                        case (int)XGamePadDevice.GamePadObjects.Down:
                            if (FocusNext(SearchPolicy.Vertical))
                                return true;
                            break;
                    }
                }
            }

            return false;
        }



        public bool FocusNext(SearchPolicy search)
        {
            if (GetNumObjects() > 0)
            {
                // if our focus control is null find the first object near the upper left that can focus
                if (_focusControl == null)
                {
                    GUIControl minCtrl = null;
                    Vector2 minPos = new Vector2(1e9f, 1e9f);

                    foreach (GUIControl ctrl in Itr<GUIControl>(true))
                    {
                        if (ctrl.Style.Focusable && ctrl.Awake && ctrl.Visible && ctrl.Active)
                        {
                            Vector2 ctrlPos = ctrl.Position;
                            if (ctrlPos.X <= minPos.X && ctrlPos.Y <= minPos.Y)
                            {
                                minCtrl = ctrl;
                                minPos = ctrlPos;
                            }
                        }
                    }

                    SetFocusControl(minCtrl);

                    return minCtrl != null;
                }
                else
                {
                    GUIControl curCtrl = null;
                    Vector2 curPos = new Vector2(1e9f, 1e9f);

                    RectangleF searchRect = new RectangleF();
                    Vector2 focusPos = _focusControl.Position;

                    switch (search)
                    {
                        case SearchPolicy.Horizontal:
                            searchRect = new RectangleF(focusPos, new Vector2(_bounds.Width - focusPos.X, _focusControl.Size.Y));
                            break;

                        case SearchPolicy.Vertical:
                            Vector2 focusSize = _focusControl.Size;
                            searchRect = new RectangleF(focusPos, new Vector2(focusSize.X, _bounds.Height - focusPos.Y));
                            break;
                    }


                    foreach (GUIControl ctrl in Itr<GUIControl>(true))
                    {
                        if (ctrl == _focusControl)
                            continue;

                        if (ctrl.Style.Focusable && ctrl.Awake && ctrl.Visible && ctrl.Active)
                        {
                            if (searchRect.IntersectsWith(ctrl.Bounds))
                            {
                                Vector2 ctrlPos = ctrl.Position;

                                // check for new focus control
                                if ((ctrlPos.X >= focusPos.X && ctrlPos.Y >= focusPos.Y) &&
                                    (ctrlPos.X <= curPos.X && ctrlPos.Y <= curPos.Y))
                                {
                                    curCtrl = ctrl;
                                    curPos = ctrlPos;
                                }
                            }
                        }
                    }

                    SetFocusControl(curCtrl);

                    return curCtrl != null;
                }
            }

            // we didn't find anything
            return false;
        }



        public bool FocusPrev(SearchPolicy search)
        {
            if (GetNumObjects() > 0)
            {
                // if our focus control is null find the first object near the lower right that can focus
                if (_focusControl == null)
                {
                    GUIControl minCtrl = null;
                    Vector2 minPos = new Vector2(-1e9f, -1e9f);

                    foreach (GUIControl ctrl in Itr<GUIControl>(true))
                    {
                        if (ctrl.Style.Focusable && ctrl.Awake && ctrl.Visible && ctrl.Active)
                        {
                            Vector2 ctrlPos = ctrl.Position;
                            if (ctrlPos.X >= minPos.X && ctrlPos.Y >= minPos.Y)
                            {
                                minCtrl = ctrl;
                                minPos = ctrlPos;
                            }
                        }
                    }

                    SetFocusControl(minCtrl);

                    return minCtrl != null;
                }
                else
                {
                    GUIControl curCtrl = null;
                    Vector2 curPos = new Vector2(-1e9f, -1e9f);

                    RectangleF searchRect = new RectangleF();
                    Vector2 focusPos = _focusControl.Position;
                    Vector2 focusSize = _focusControl.Size;

                    switch (search)
                    {
                        case SearchPolicy.Horizontal:
                            searchRect = new RectangleF(new Vector2(0.0f, focusPos.Y), new Vector2(focusPos.X + focusSize.X, focusSize.Y));
                            break;
                        case SearchPolicy.Vertical:
                            searchRect = new RectangleF(new Vector2(focusPos.X, 0.0f), new Vector2(focusSize.X, focusPos.Y + focusSize.Y));
                            break;
                    }

                    foreach (GUIControl ctrl in Itr<GUIControl>(true))
                    {
                        if (ctrl == _focusControl)
                            continue;

                        if (ctrl.Style.Focusable && ctrl.Awake && ctrl.Visible && ctrl.Active)
                        {
                            if (searchRect.IntersectsWith(ctrl.Bounds))
                            {
                                Vector2 ctrlPos = ctrl.Position;

                                // check for new focus control
                                if ((ctrlPos.X <= focusPos.X && ctrlPos.Y <= focusPos.Y) &&
                                    (ctrlPos.X >= curPos.X && ctrlPos.Y >= curPos.Y))
                                {
                                    curCtrl = ctrl;
                                    curPos = ctrlPos;
                                }
                            }
                        }
                    }

                    SetFocusControl(curCtrl);

                    return curCtrl != null;
                }
            }

            // we didn't find anything
            return false;
        }



        /// <summary>
        /// Changes the current focus control.
        /// </summary>
        /// <param name="control">The GUIControl to change focus to.</param>
        public void SetFocusControl(GUIControl control)
        {
            // can't set focus to a non existing control
            // use ClearFocusControl to clear focus
            if (control == null)
                return;

            // if the control can't focus or it already has focus
            if (!control.CanFocus || ControlHasFocus(control))
                return;

            // set the new focus control
            GUIControl oldFocus = _focusControl;
            _focusControl = control;

            // let the current focus control know it's giving up its focus
            if (oldFocus != null)
                oldFocus.OnLoseFocus(_focusControl);

            // let the new focus control know it has gained focus
            _focusControl.OnGainFocus(oldFocus);
        }



        /// <summary>
        /// Returns the current focus control.
        /// </summary>
        public GUIControl GetFocusControl()
        {
            return _focusControl;
        }



        /// <summary>
        /// Determines if a given control has focus.
        /// </summary>
        /// <param name="ctrl">The GUIControl to test.</param>
        /// <returns>True if the given control is the current focus control.</returns>
        public bool ControlHasFocus(GUIControl ctrl)
        {
            return ctrl == _focusControl;
        }



        /// <summary>
        /// Clears the current focus control.
        /// </summary>
        public void ClearFocusControl()
        {
            if (_focusControl != null)
                _focusControl.OnLoseFocus(null);

            _focusControl = null;
        }

        #endregion


        #region Private, protected, internal methods

        private void _RenderSafeAreas()
        {
            Vector2 actionNW = _safeActionBoundry.Point;
            Vector2 actionSW = new Vector2(actionNW.X, _safeActionBoundry.Point.Y + _safeActionBoundry.Extent.Y);
            Vector2 actionNE = new Vector2(_safeActionBoundry.Point.X + _safeActionBoundry.Extent.X, actionNW.Y);
            Vector2 actionSE = new Vector2(actionNE.X, actionSW.Y);

            Vector2 titleNW = _safeTitleBoundry.Point;
            Vector2 titleSW = new Vector2(titleNW.X, _safeTitleBoundry.Point.Y + _safeTitleBoundry.Extent.Y);
            Vector2 titleNE = new Vector2(_safeTitleBoundry.Point.X + _safeTitleBoundry.Extent.X, titleNW.Y);
            Vector2 titleSE = new Vector2(titleNE.X, titleSW.Y);

            if (_safeAreaRendering == SafeAreas.Action || _safeAreaRendering == SafeAreas.ActionTitle)
            {
                // Draw the action safe area as four filled rectangles
                DrawUtil.RectFill(Vector2.Zero, new Vector2(_bounds.Extent.X, actionNE.Y), _actionSafeFillColor);
                DrawUtil.RectFill(new Vector2(0.0f, actionSW.Y), _bounds.Extent, _actionSafeFillColor);
                DrawUtil.RectFill(new Vector2(0.0f, actionNW.Y + 1.0f), new Vector2(actionNW.X, actionSW.Y - 1.0f), _actionSafeFillColor);
                DrawUtil.RectFill(new Vector2(actionNE.X, actionNE.Y + 1.0f), new Vector2(_bounds.Extent.X, actionSE.Y - 1.0f), _actionSafeFillColor);
            }

            if (_safeAreaRendering == SafeAreas.ActionTitle)
            {
                // Draw the title safe area as four filled rectangles
                DrawUtil.RectFill(new Vector2(actionNW.X + 1.0f, actionNW.Y + 1.0f), new Vector2(actionNE.X - 1.0f, titleNE.Y), _titleSafeFillColor);
                DrawUtil.RectFill(new Vector2(actionNW.X + 1.0f, titleSW.Y), new Vector2(actionSE.X - 1.0f, actionSE.Y - 1.0f), _titleSafeFillColor);
                DrawUtil.RectFill(new Vector2(actionNW.X + 1.0f, titleNW.Y + 1.0f), new Vector2(titleSW.X, titleSW.Y - 1.0f), _titleSafeFillColor);
                DrawUtil.RectFill(new Vector2(titleNE.X, titleNE.Y + 1.0f), new Vector2(actionSE.X - 1.0f, titleSE.Y - 1.0f), _titleSafeFillColor);
            }

            // Some helper vectors for drawing lines.  We use two pixel width lines to account for
            // interlaced displays on Xbox.
            Vector2 expandHorizontal = new Vector2(1.0f, 0.0f);
            Vector2 expandVertical = new Vector2(0.0f, 1.0f);

            // Draw the action safe area outline.
            DrawUtil.RectFill(actionNW - expandVertical, actionNE, _safeLineColor);
            DrawUtil.RectFill(actionNE, actionSE + expandHorizontal, _safeLineColor);
            DrawUtil.RectFill(actionSW, actionSE + expandVertical, _safeLineColor);
            DrawUtil.RectFill(actionNW - expandHorizontal, actionSW, _safeLineColor);

            // Draw centre crosshairs
            float midY = _bounds.Extent.Y / 2.0f;
            float midX = _bounds.Extent.X / 2.0f;
            DrawUtil.RectFill(new Vector2(midX - _safeAreaSize.X, midY - 1.0f), new Vector2(midX + _safeAreaSize.X, midY + 1.0f), _safeLineColor);
            DrawUtil.RectFill(new Vector2(midX - 1.0f, midY - _safeAreaSize.Y), new Vector2(midX + 1.0f, midY + _safeAreaSize.Y), _safeLineColor);

            if (_safeAreaRendering == SafeAreas.ActionTitle || _safeAreaRendering == SafeAreas.ActionTitleOutline)
            {
                // Draw the title safe area outline.
                DrawUtil.RectFill(titleNW - expandVertical, titleNE, _safeLineColor);
                DrawUtil.RectFill(titleNE, titleSE + expandHorizontal, _safeLineColor);
                DrawUtil.RectFill(titleSW, titleSE + expandVertical, _safeLineColor);
                DrawUtil.RectFill(titleNW - expandHorizontal, titleSW, _safeLineColor);

                // Draw title safe area edge ticks
                DrawUtil.RectFill(new Vector2(actionNW.X, midY - 1.0f), new Vector2(actionNW.X + _safeAreaSize.X * 2.0f, midY + 1.0f), _safeLineColor);
                DrawUtil.RectFill(new Vector2(actionNE.X - _safeAreaSize.X * 2.0f, midY - 1.0f), new Vector2(actionNE.X, midY + 1.0f), _safeLineColor);
                DrawUtil.RectFill(new Vector2(midX - 1.0f, actionNW.Y), new Vector2(midX + 1.0f, actionNW.Y + _safeAreaSize.Y * 2.0f), _safeLineColor);
                DrawUtil.RectFill(new Vector2(midX - 1.0f, actionSW.Y - _safeAreaSize.Y * 2.0f), new Vector2(midX + 1.0f, actionSW.Y), _safeLineColor);
            }
            else
            {
                // Draw action safe area ticks
                DrawUtil.RectFill(new Vector2(0.0f, midY - 1.0f), new Vector2(_safeAreaSize.X * 2.0f, midY + 1.0f), _safeLineColor);
                DrawUtil.RectFill(new Vector2(_bounds.Extent.X - _safeAreaSize.X * 2.0f, midY - 1.0f), new Vector2(_bounds.Extent.X, midY + 1.0f), _safeLineColor);
                DrawUtil.RectFill(new Vector2(midX - 1.0f, 0), new Vector2(midX + 1.0f, _safeAreaSize.Y * 2.0f), _safeLineColor);
                DrawUtil.RectFill(new Vector2(midX - 1.0f, _bounds.Extent.Y - _safeAreaSize.Y * 2.0f), new Vector2(midX + 1.0f, _bounds.Extent.Y), _safeLineColor);
            }
        }



        private void _CalculateRenderSafeBoundries()
        {
            _safeAreaSize = new Vector2(_bounds.Extent.X * _safeAreaMul, _bounds.Extent.Y * _safeAreaMul);
            _safeActionBoundry = new RectangleF(_safeAreaSize, _bounds.Extent - _safeAreaSize * 2.0f);
            _safeTitleBoundry = new RectangleF(_safeActionBoundry.Point + _safeAreaSize, _safeActionBoundry.Extent - _safeAreaSize * 2.0f);
        }



        public override bool OnRegister()
        {
            // Calculate initial title safe bounds
            RectangleF initialBounds = new RectangleF(Position.X, Position.Y,
                        GFX.GFXDevice.Instance.CurrentVideoMode.BackbufferWidth,
                        GFX.GFXDevice.Instance.CurrentVideoMode.BackbufferHeight);
            SetBounds(initialBounds);

            return base.OnRegister();
        }

        #endregion


        #region Private, protected, internal fields

        GUIControl _focusControl = null;
        GUIControl _letterBoxControl = null;

        SceneRenderState _srs = new SceneRenderState();

        int idx;
        int numObjects;
        bool renderLetterbox = false;
        int height;
        int width;
        RectangleF screenRect;
        SafeAreas _safeAreaRendering = SafeAreas.None;
        float _safeAreaMul = 0.05f;
        Vector2 _safeAreaSize;
        RectangleF _safeActionBoundry;
        RectangleF _safeTitleBoundry;
        Color _actionSafeFillColor = new Color(0, 0, 0, 128);
        Color _titleSafeFillColor = new Color(0, 0, 0, 64);
        Color _safeLineColor = new Color(128, 128, 128);
        GUIControl contentCtrl;

        #endregion
    }
}
