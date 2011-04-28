//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using GarageGames.Torque.Platform;
using GarageGames.Torque.Core;
using GarageGames.Torque.Sim;
using GarageGames.Torque.MathUtil;



namespace GarageGames.Torque.GUI
{
    /// <summary>
    /// Base class from which all GUI controls must derive. Derives from TorqueFolder
    /// to represent a GUI layout in a hierarchical manner.
    /// </summary>
    public class GUIControl : TorqueFolder
    {

        #region Constructors

        public GUIControl()
        {
            Ordered = true;
            Active = true;
        }

        #endregion


        #region Public properties, operators, constants, and enums

        /// <summary>
        /// The visibility of the control, an invisible control does not render.
        /// </summary>
        public bool Visible
        {
            get { return _visible; }
            set
            {
                _visible = value;

                if (!_visible)
                {
                    int idx;
                    int numObjects = GetNumObjects();

                    GUIControl focusCtrl = GUICanvas.Instance.GetFocusControl();

                    for (idx = 0; idx < numObjects; idx++)
                    {
                        GUIControl child = (GUIControl)GetObject(idx);
                        if (child == null)
                            break;

                        if (focusCtrl == child)
                            GUICanvas.Instance.ClearFocusControl();
                    }
                }
            }
        }



        /// <summary>
        /// The status of the control. Active controls can respond to input and are focusable.
        /// </summary>
        public bool Active
        {
            get { return _active; }
            set
            {
                _active = value;

                if (!_active && GUICanvas.Instance.GetFocusControl() == this)
                    GUICanvas.Instance.ClearFocusControl();
            }
        }



        /// <summary>
        /// A control that is awake is one that belongs to the currently rendering hierarchy,
        /// regardless if it is Visible or Active.
        /// </summary>
        public bool Awake
        {
            get { return _awake; }
        }



        [TorqueCloneIgnore]
        public int Layer
        {
            get { return _sortLayer; }
            set { _sortLayer = value; }
        }



        /// <summary>
        /// The position and size of the control.
        /// </summary>
        public RectangleF Bounds
        {
            get { return _bounds; }
            set { SetBounds(value); }
        }



        /// <summary>
        /// The position of the control.
        /// </summary>
        [TorqueCloneIgnore]
        public Vector2 Position
        {
            get { return _bounds.Point; }
            set { SetBounds(value, _bounds.Extent); }
        }



        /// <summary>
        /// The size of the control.
        /// </summary>
        [TorqueCloneIgnore]
        public Vector2 Size
        {
            get { return _bounds.Extent; }
            set { SetBounds(_bounds.Point, value); }
        }



        /// <summary>
        /// The minimum size the control is allowed to be when resizing.
        /// Any value smaller will clamp the resize to the MinExtent.
        /// </summary>
        public Vector2 MinExtent
        {
            get { return _minExtent; }
            set { _minExtent = value; }
        }



        /// <summary>
        /// Defines how this control reacts when a parent control resizes. Defines
        /// the horizontal resizing options for this control.
        /// </summary>
        public HorizSizing HorizSizing
        {
            get { return _horizSizing; }
            set { _horizSizing = value; }
        }



        /// <summary>
        /// Defines how this control reacts when a parent control resizes. Defines
        /// the vertical resizing options for this control.
        /// </summary>
        public VertSizing VertSizing
        {
            get { return _vertSizing; }
            set { _vertSizing = value; }
        }



        /// <summary>
        /// Each control must have an associated style before it can awaken.
        /// A style defines a set of properties that define the look or behavior
        /// of a control. See <see cref="GUIStyle"/>.
        /// </summary>
        public GUIStyle Style
        {
            get { return _style; }
            set { _OnNewStyle(value); }
        }



        /// <summary>
        /// The control which owns this control, in the hierarchy. The Parent
        /// defines the coordinate space for this control and can override the
        /// visibility. This control exists within the bounds of the Parent.
        /// </summary>
        public GUIControl Parent
        {
            get { return (Folder as GUIControl); }
        }



        /// <summary>
        /// This delegate is called after this control has awakened, a process in which
        /// a control or series of controls is added to the GUICanvas hierarchy. If
        /// this control and/or its parent is added to a GUI hierarchy that is not
        /// currently awake OnGUIWake will not be invoked.
        /// </summary>
        public OnWakeDelegate OnGUIWake
        {
            get { return _onWakeDelegate; }
            set { _onWakeDelegate = value; }
        }



        /// <summary>
        /// This delegate is called after this control becomes asleep, a process in which
        /// a control or series of controls is removed from the GUICanvas hierarchy. If
        /// this control and/or its parent is removed from a GUI hierarchy that is not
        /// currently awake OnGUISleep will not be invoked.
        /// </summary>
        public OnSleepDelegate OnGUISleep
        {
            get { return _onSleepDelegate; }
            set { _onSleepDelegate = value; }
        }



        /// <summary>
        /// This delegate is called after this control gains focus. A control that has focus
        /// is the first to be able to respond to input events.
        /// </summary>
        public OnGainFocusDelegate OnGUIGainFocus
        {
            get { return _onGainFocusDelegate; }
            set { _onGainFocusDelegate = value; }
        }



        /// <summary>
        /// This delegate is called after this control loses focus. A control can lose focus
        /// for a number of reasons: another control may have gained focus, this control may
        /// have become invisible, or this control may have become inactive. A control will
        /// also lose focus when it sleeps.
        /// </summary>
        public OnLoseFocusDelegate OnGUILoseFocus
        {
            get { return _onLoseFocusDelegate; }
            set { _onLoseFocusDelegate = value; }
        }



        /// <summary>
        /// This delegate is called after this control resizes.
        /// </summary>
        public OnResizeDelegate OnGUIResize
        {
            get { return _onResizeDelegate; }
            set { _onResizeDelegate = value; }
        }



        /// <summary>
        /// Whether this control should gain focus when it awakens. Typically only one
        /// control in the currently awakening GUI hierarchy should enable this as only 
        /// one control at any time may have focus. If more than one control enables
        /// this functionality, the last control to awaken will receive focus, however
        /// each control with this enabled will invoke the OnGainFocus delegate.
        /// </summary>
        public bool FocusOnWake
        {
            get { return _stealFocusOnWake; }
            set { _stealFocusOnWake = value; }
        }



        /// <summary>
        /// If this control needs to respond to input events, use the InputMap to specify how 
        /// to bind user input to specific functionality. If an InputMap isn't currently set
        /// then one will automatically be created when you get from this property. Input events
        /// are first routed to GUIControl::OnInputEvent and then to the InputMap, if it exists.
        /// </summary>
        [TorqueCloneIgnore]
        public InputMap InputMap
        {
            get
            {
                if (_inputMap == null)
                    _inputMap = new InputMap();

                return _inputMap;
            }

            set { _inputMap = value; }
        }



        /// <summary>
        /// Returns true if the control's focus can be changed.
        /// </summary>
        public bool CanFocus
        {
            get
            {
                if (_style == null)
                    return false;

                return _style.Focusable && _visible && _awake && _active;
            }
        }

        #endregion


        #region Public methods

        public override bool OnRegister()
        {
            if (!base.OnRegister())
                return false;

            return true;
        }



        public override void OnUnregister()
        {
            if (GUICanvas.Instance.ControlHasFocus(this))
                GUICanvas.Instance.ClearFocusControl();

            base.OnUnregister();

            // if we are a child control, notify our parent that we've been removed
            GUIControl parent = Parent;

            if (parent != null)
                parent._OnChildRemove(this);
        }



        /// <summary>
        /// Called when this control and its children have been added to the GUICanvas hierarchy.
        /// </summary>
        public void Awaken()
        {
            Assert.Fatal(!_awake, "GUIControl._Awaken - Control is already awake.");
            if (_awake)
                return;

            int idx;
            int numObjects = GetNumObjects();

            // awaken all child controls
            for (idx = 0; idx < numObjects; idx++)
            {
                GUIControl child = (GUIControl)GetObject(idx);
                if (child == null)
                    break;

                if (!child.Awake)
                    child.Awaken();
            }

            Assert.Fatal(!_awake, "GUIControl._Awaken - Should not be awake here.");
            if (!_awake)
            {
                if (!_OnWake())
                {
                    Assert.Fatal(false, "GUIControl._Awaken - Failed OnWake.");
                    TorqueObjectDatabase.Instance.Unregister(this);
                }
            }

            Assert.Fatal(_awake, "GUIControl._Awaken - Should be awake here.");
        }



        /// <summary>
        /// Called when this control and its children have been removed from the GUICanvas hierarchy.
        /// </summary>
        public void Sleep()
        {
            Assert.Fatal(_awake, "GUIControl._Sleep - Control is not awake");

            if (!_awake)
                return;

            int idx;
            int numObjects = GetNumObjects();

            // sleep all child controls
            for (idx = 0; idx < numObjects; idx++)
            {
                GUIControl child = (GUIControl)GetObject(idx);
                if (child == null)
                    break;

                if (child.Awake)
                    child.Sleep();
            }

            Assert.Fatal(_awake, "GUIControl._Sleep - Control shouldn't be asleep here.");

            if (_awake)
                _OnSleep();
        }



        /// <summary>
        /// Called before GUIControl::OnRender. Allows for any special processing that is
        /// needed before rendering the control.
        /// </summary>
        /*
        public virtual void OnPreRender()
        {
            // all bottom level controls should be the same dimensions as the canvas
            //  unless they wish to preserve aspect ratio

            if (_style.PreserveAspectRatio)
            {
                float width = GUICanvas.Instance.Bounds.Width;
                float height = GUICanvas.Instance.Bounds.Height;

                bool isWidescreen = GFX.GFXDevice.IsWideScreen((int)_bounds.Width, (int)_bounds.Height);
                bool needsResize = isWidescreen ? (_bounds.Width != width) : (_bounds.Height != height);

                if (needsResize)
                {
                    RectangleF newBounds = _bounds;

                    // calculate preserved aspect dimensions
                    float ratio = _bounds.Height / _bounds.Width;



                    // non-widescreen 
                    if (!GFX.GFXDevice.IsWideScreen((int)_bounds.Width, (int)_bounds.Height))
                    {
                        // calculate as fixed width, adjusting the height based on how wide the 
                        // display is, creating a horizontal letterbox on fullscreen displays
                        newBounds.Width = width;
                        newBounds.Height = width * ratio;

                        if (newBounds.Height > height)
                            newBounds.Height = height;
                    }

                    // widescreen 
                    else
                    {
                        // calculate as fixed height, adjusting the width based on how tall the
                        // display is, creating a vertical letterbox on widescreen displays
                        newBounds.Height = height;
                        newBounds.Width = height * (1.0f / ratio);

                        if (newBounds.Width > width)
                            newBounds.Width = width;
                    }

                    // calculate center position
                    newBounds.X = (width - newBounds.Width) / 2.0f;
                    newBounds.Y = (height - newBounds.Height) / 2.0f;

                    SetBounds(newBounds);
                }
            }
            else
            {
                if (_bounds != GUICanvas.Instance.Bounds)
                    SetBounds(GUICanvas.Instance.Bounds);
            }
        }
        */



        public virtual void OnPreRender()
        {
            // all bottom level controls should be the same dimensions as the canvas
            // unless they wish to preserve aspect ratio

            if (_style.PreserveAspectRatio)
            {
                float canvasWidth = GUICanvas.Instance.Bounds.Width;
                float canvasHeight = GUICanvas.Instance.Bounds.Height;
                float canvasRatio = canvasHeight / canvasWidth;

                float ctrlWidth = _bounds.Width;
                float ctrlHeight = _bounds.Height;
                float ctrlRatio = ctrlHeight / ctrlWidth;

                RectangleF newCtrlBounds = _bounds;

                if (canvasRatio == ctrlRatio)
                {
                    // stretch/shrink
                    if (_bounds != GUICanvas.Instance.Bounds)
                        SetBounds(GUICanvas.Instance.Bounds);
                }
                else if (canvasRatio > ctrlRatio)
                {
                    newCtrlBounds.Width = canvasWidth;
                    newCtrlBounds.Height = canvasWidth * ctrlRatio;

                    if (newCtrlBounds.Height > canvasHeight)
                        newCtrlBounds.Height = canvasHeight;

                    // calculate center position
                    newCtrlBounds.X = canvasWidth > newCtrlBounds.Width ? (canvasWidth - newCtrlBounds.Width) / 2.0f : 0;
                    newCtrlBounds.Y = canvasHeight > newCtrlBounds.Height ? (canvasHeight - newCtrlBounds.Height) / 2.0f : 0;

                    SetBounds(newCtrlBounds);
                }
                else // if (canvasRatio < ctrlRatio)
                {
                    newCtrlBounds.Height = canvasHeight;
                    newCtrlBounds.Width = canvasHeight * (1.0f / ctrlRatio);

                    if (newCtrlBounds.Width > canvasWidth)
                        newCtrlBounds.Width = canvasWidth;

                    // calculate center position
                    newCtrlBounds.X = canvasWidth > newCtrlBounds.Width ? (canvasWidth - newCtrlBounds.Width) / 2.0f : 0;
                    newCtrlBounds.Y = canvasHeight > newCtrlBounds.Height ? (canvasHeight - newCtrlBounds.Height) / 2.0f : 0;

                    SetBounds(newCtrlBounds);
                }
            }
            else // if (!_style.PreserveAspectRatio)
            {
                if (_bounds != GUICanvas.Instance.Bounds)
                    SetBounds(GUICanvas.Instance.Bounds);
            }
        }



        /// <summary>
        /// Called when this control is to render itself.
        /// </summary>
        /// <param name="offset">The location this control is to begin rendering.</param>
        /// <param name="updateRect">The screen area this control has drawing access to.</param>
        public virtual void OnRender(Vector2 offset, RectangleF updateRect)
        {
            ctrlRect = new RectangleF(offset, _bounds.Extent);

            // fill the update rect with the fill color
            if (_style.IsOpaque)
                DrawUtil.RectFill(ctrlRect, _style.FillColor[CustomColor.ColorBase]);

            // if there's a border, draw the border
            if (_style.HasBorder)
                DrawUtil.RectFill(ctrlRect, _style.BorderColor[CustomColor.ColorBase]);

            // render the child controls
            _RenderChildControls(offset, updateRect);
        }



        /// <summary>
        /// Changes the size and/or position of this control.
        /// </summary>
        /// <param name="newPosition">The new location of this control.</param>
        /// <param name="newSize">The new size of this control.</param>
        public virtual void SetBounds(Vector2 position, Vector2 extent)
        {
            Vector2 actualExtent = new Vector2(MathHelper.Max(_minExtent.X, extent.X),
                                           MathHelper.Max(_minExtent.Y, extent.Y));

            // only do child control resizing if we need to
            bool extentChanged = (actualExtent != _bounds.Extent);

            if (extentChanged)
            {
                // notify the children controls
                foreach (GUIControl ctrl in Itr<GUIControl>(false))
                    ctrl._ParentResized(_bounds.Extent, actualExtent);

                _bounds.Point = position;
                _bounds.Extent = actualExtent;

                // notify the parent
                GUIControl parent = Parent;

                if (parent != null)
                    parent._ChildResized(this);
            }
            else
            {
                _bounds.Point = position;
            }

            // notify any interested parties that this control has resized
            if (_onResizeDelegate != null)
                _onResizeDelegate(this, position, extent);
        }



        /// <summary>
        /// Changes the bounds of this control. A control's bounds is its position and size.
        /// </summary>
        /// <param name="newBounds">The new bounds of this control.</param>
        public void SetBounds(RectangleF newBounds)
        {
            SetBounds(newBounds.Point, newBounds.Extent);
        }



        /// <summary>
        /// The general input handler for this control. Input events are routed here first
        /// before they are given to this control's InputMap, if one exists. If a Parent
        /// control exists, input should be routed to the Parent if this control or its 
        /// InputMap did not handle the event.
        /// </summary>
        /// <param name="data">The input event data from the InputManager.</param>
        /// <returns>True if this control or its Parent handled the input event.</returns>
        public virtual bool OnInputEvent(ref TorqueInputDevice.InputEventData data)
        {
            if (_inputMap != null)
            {
                if (_inputMap.ProcessInput(data))
                    return true;
            }

            // if we got here, this control didn't handle the input,
            //  pass it up to the parent if we have one
            if (Parent != null)
                return Parent.OnInputEvent(ref data);

            return false;
        }



        /// <summary>
        /// Called when this control gains focus.
        /// </summary>
        /// <param name="oldFocusCtrl">The control that is losing focus(if any).</param>
        public virtual void OnGainFocus(GUIControl oldFocusCtrl)
        {
            // notify any interested parties that this control now has focus
            if (_onGainFocusDelegate != null)
                _onGainFocusDelegate(this);
        }



        /// <summary>
        /// Called when this control loses focus.
        /// </summary>
        /// <param name="newFocusCtrl">The control that is gaining focus(if any).</param>
        public virtual void OnLoseFocus(GUIControl newFocusCtrl)
        {
            // notify any interested parties that this control now has lost focus
            if (_onLoseFocusDelegate != null)
                _onLoseFocusDelegate(this);
        }



        /// <summary>
        /// Returns true if this control is the parent.
        /// </summary>
        public virtual bool IsParentOf(GUIControl child)
        {
            // loop through checking each child to see if it is or contains the ctrl
            foreach (GUIControl ctrl in Itr<GUIControl>(false))
            {
                if (ctrl == child || ctrl.IsParentOf(child))
                    return true;
            }

            // not found
            return false;
        }



        public override void CopyTo(TorqueObject obj)
        {
            base.CopyTo(obj);

            GUIControl obj2 = (GUIControl)obj;

            // Make sure the bounds have been properly calculated for this object.
            SetBounds(Bounds);

            if (Style != null)
                obj2.Style = Style;

            obj2.Visible = Visible;
            obj2.Active = Active;
            obj2.Bounds = Bounds;
            obj2.MinExtent = MinExtent;
            obj2.HorizSizing = HorizSizing;
            obj2.VertSizing = VertSizing;
            obj2.OnGUIGainFocus = OnGUIGainFocus;
            obj2.OnGUILoseFocus = OnGUILoseFocus;
            obj2.OnGUIResize = OnGUIResize;
            obj2.OnGUISleep = OnGUISleep;
            obj2.OnGUIWake = OnGUIWake;
            obj2.FocusOnWake = FocusOnWake;
        }



        public override void OnLoaded()
        {
            base.OnLoaded();

            if (_xmlGuiChildren != null)
            {
                foreach (GUIControl control in _xmlGuiChildren)
                    control.Folder = this;

                _xmlGuiChildren = null;
            }
        }

        #endregion


        #region Private, protected, internal methods

        internal override void _AddObject(TorqueObject obj)
        {
            GUIControl ctrl = obj as GUIControl;
            Assert.Fatal(ctrl != null, "GUIControl._AddObject - Attempted to add non-GUIControl.");

            if (!ctrl.IsRegistered)
                TorqueObjectDatabase.Instance.Register(ctrl);

            Assert.Fatal(ctrl.IsRegistered, "GUIControl._AddObject - Attempted to add an unregistered GUIControl.");

            if (!ctrl.IsRegistered)
                return;

            base._AddObject(ctrl);

            // wake new child control if we are awake
            if (_awake)
                ctrl.Awaken();

            // if we are adding a child, notify the parent that we've been added
            GUIControl parent = ctrl.Parent;

            if (parent != null)
                parent._OnChildAdd(ctrl);
        }



        internal override bool _RemoveObject(TorqueObject obj)
        {
            GUIControl ctrl = obj as GUIControl;

            Assert.Fatal(ctrl != null, "GUIControl._RemoveObject - Attempted to remove non-GUIControl.");
            Assert.Fatal(_awake == ctrl.Awake, "GUIControl:.RemoveObject - Child control wake state is bad.");

            // sleep control
            if (_awake)
                ctrl.Sleep();

            return base._RemoveObject(obj);
        }



        /// <summary>
        /// Called when this control is in the process of being awakened. Override this method if your
        /// control has requirements that must be met for it to function properly.
        /// </summary>
        /// <returns>True if the control has properly awakened.</returns>
        protected virtual bool _OnWake()
        {
            Assert.Fatal(!_awake, "GUIControl._OnWake - Control is already awake.");
            Assert.Fatal(_style != null, "GUIControl._OnWake - Control created with no style.");

            if (_awake)
                return false;

            // set the flag
            _awake = true;

            // set the layer
            GUIControl parent = Parent;

            if (parent != null && parent != GUICanvas.Instance)
                _sortLayer = parent.Layer;

            if (CanFocus && _stealFocusOnWake)
                GUICanvas.Instance.SetFocusControl(this);

            // notify any interested parties that this control is now awake
            if (_onWakeDelegate != null)
                _onWakeDelegate(this);

            return true;
        }



        /// <summary>
        /// Called when this control is in the process of becoming asleep.
        /// </summary>
        protected virtual void _OnSleep()
        {
            Assert.Fatal(_awake == true, "GUIControl._OnSleep - control is not awake.");

            if (!_awake)
                return;

            // set the flag
            _awake = false;

            // notify any interested parties that this control is now asleep
            if (_onSleepDelegate != null)
                _onSleepDelegate(this);
        }



        /// <summary>
        /// Called when a control becomes a child of this control.
        /// </summary>
        /// <param name="child">The control that was added to this control's hierarchy.</param>
        protected virtual void _OnChildAdd(GUIControl child) { }



        /// <summary>
        /// Called when one of this control's children is removed.
        /// </summary>
        /// <param name="child">The child control that was removed from this control's hierarchy.</param>
        protected virtual void _OnChildRemove(GUIControl child) { }



        /// <summary>
        /// Renders controls listed under this control's hierarchy.
        /// </summary>
        /// <param name="offset">The upper-left corner of this control in screen coordinates.</param>
        /// <param name="updateRect">The intersection rectangle, in screen coordinates, of the control hierarchy.</param>
        protected void _RenderChildControls(Vector2 offset, RectangleF updateRect)
        {
            int idx;
            int numObjects = GetNumObjects();

            // store the clip
            RectangleF oldClip = DrawUtil.ClipRect;

            // offset is the upper-left corner of this control in screen coordinates
            // updateRect is the inter
            for (idx = 0; idx < numObjects; idx++)
            {
                GUIControl child = (GUIControl)GetObject(idx);
                if (child == null)
                    break;

                if (child.Visible)
                {
                    Vector2 childPosition = new Vector2(offset.X + child.Position.X, offset.Y + child.Position.Y);
                    RectangleF childClip = new RectangleF(childPosition, child.Size);

                    if (childClip.Intersect(updateRect))
                    {
                        DrawUtil.ClipRect = childClip;
                        child.OnRender(childPosition, childClip);
                    }
                }
            }

            // restore the old clip
            DrawUtil.ClipRect = oldClip;
        }



        /// <summary>
        /// Called when a child control of this object is resized.
        /// </summary>
        /// <param name="child"></param>
        protected virtual void _ChildResized(GUIControl child) { }



        /// <summary>
        /// Called when this object's parent is resized.
        /// </summary>
        /// <param name="oldParentExtent">The previous size of the parent.</param>
        /// <param name="newParentExtent">The new size of the parent.</param>
        protected virtual void _ParentResized(Vector2 oldParentExtent, Vector2 newParentExtent)
        {
            float deltaX = newParentExtent.X - oldParentExtent.X;
            float deltaY = newParentExtent.Y - oldParentExtent.Y;

            if (deltaX == 0 && deltaY == 0)
                return;

            RectangleF newBounds = _bounds;

            // determine horizontal sizing
            switch (_horizSizing)
            {
                case HorizSizing.Center:
                    newBounds.X = (newParentExtent.X - _bounds.Width) * 0.5f;
                    break;

                case HorizSizing.Width:
                    newBounds.Width += deltaX;
                    break;

                case HorizSizing.Left:
                    newBounds.X += deltaX;
                    break;

                case HorizSizing.Relative:
                    if (oldParentExtent.X != 0)
                    {
                        float newLeft = (newBounds.X * newParentExtent.X) / oldParentExtent.X;
                        float newRight = ((newBounds.X + newBounds.Width) * newParentExtent.X) / oldParentExtent.X;

                        newBounds.X = newLeft;
                        newBounds.Width = newRight - newLeft;
                    }
                    break;
            }

            // determine vertical sizing
            switch (_vertSizing)
            {
                case VertSizing.Center:
                    newBounds.Y = (newParentExtent.Y - _bounds.Height) * 0.5f;
                    break;

                case VertSizing.Height:
                    newBounds.Height += deltaY;
                    break;

                case VertSizing.Top:
                    newBounds.Y += deltaY;
                    break;

                case VertSizing.Relative:
                    if (oldParentExtent.Y != 0)
                    {
                        float newTop = (newBounds.Y * newParentExtent.Y) / oldParentExtent.Y;
                        float newBottom = ((newBounds.Y + newBounds.Height) * newParentExtent.Y) / oldParentExtent.Y;

                        newBounds.Y = newTop;
                        newBounds.Height = newBottom - newTop;
                    }
                    break;
            }

            // only resize if MinExtent is satisfied
            if (!(newBounds.Width >= _minExtent.X && newBounds.Height >= _minExtent.Y))
                return;

            if (newBounds == _bounds)
                return;

            // finally resize the control
            SetBounds(newBounds.Point, newBounds.Extent);
        }



        /// <summary>
        /// Called when this control receives a new style, invoked from the set Style property.
        /// Override this method if your custom control uses a specialized version of GUIStyle.
        /// </summary>
        /// <param name="style"></param>
        /// <returns></returns>
        protected virtual bool _OnNewStyle(GUIStyle style)
        {
            if (style == null)
                return false;

            _style = style;

            return true;
        }

        #endregion


        #region Private, protected, internal fields

        protected bool _awake = false;
        protected bool _visible = true;    // inheriting controls should use the property instead
        protected bool _active = false;     // inheriting controls should use the property instead

        // if this is true, and our style is focuseable, set focus OnWake
        bool _stealFocusOnWake = false;

        int _sortLayer = 0;

        protected RectangleF _bounds;
        protected Vector2 _minExtent = new Vector2(8, 8);

        protected HorizSizing _horizSizing = HorizSizing.Right;
        protected VertSizing _vertSizing = VertSizing.Bottom;

        GUIStyle _style = null;

        RectangleF ctrlRect;

        OnWakeDelegate _onWakeDelegate;
        OnSleepDelegate _onSleepDelegate;
        OnGainFocusDelegate _onGainFocusDelegate;
        OnLoseFocusDelegate _onLoseFocusDelegate;
        OnResizeDelegate _onResizeDelegate;

        protected InputMap _inputMap;

        [XmlElement(ElementName = "ChildControls")]
        [TorqueXmlDeserializeInclude]
        protected List<GUIControl> _xmlGuiChildren;

        #endregion
    }
}
