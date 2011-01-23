//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using GarageGames.Torque.Platform;
using GarageGames.Torque.Core;
using GarageGames.Torque.Sim;
using GarageGames.Torque.MathUtil;
using GarageGames.Torque.Util;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;



namespace GarageGames.Torque.GUI
{
    /// <summary>
    /// A GUI control that allows it's contents to be scrolled through.
    /// Similar to an inline frame in HTML.
    /// </summary>
    public class GUIScroll : GUIControl, ITickObject
    {

        #region Public properties, operators, constants, and enums

        /// <summary>
        /// Gets or sets the speed at which this scroller will scroll.
        /// </summary>
        public float ScrollSpeed
        {
            get { return _scrollSpeed; }
            set { _scrollSpeed = value; }
        }



        /// <summary>
        /// The distance this scroller should scroll in the X direction each tick when awake.
        /// This value is multiplied by ScrollSpeed.
        /// </summary>
        public float ActiveScrollX
        {
            get { return _scrollAmountX; }
            set { _scrollAmountX = value; }
        }



        /// <summary>
        /// The distance this scroller should scroll in the Y direction each tick when awake.
        /// This value is multiplied by ScrollSpeed.
        /// </summary>
        public float ActiveScrollY
        {
            get { return _scrollAmountY; }
            set { _scrollAmountY = value; }
        }



        /// <summary>
        /// Specifies whether the horizontal scroll bar is visible.
        /// </summary>
        public bool HScrollVisible
        {
            get
            {
                float width = Bounds.Width;
                if (_childBounds.Height > Bounds.Height)
                    width -= _scrollBarWidth;

                return _childBounds.Width > width;
            }
        }



        /// <summary>
        /// Specifies whether the vertical scroll bar is visible.
        /// </summary>
        public bool VScrollVisible
        {
            get
            {
                float height = Bounds.Height;
                if (_childBounds.Width > Bounds.Width)
                    height -= _scrollBarWidth;

                return _childBounds.Height > height;
            }
        }

        #endregion


        #region Public methods

        /// <summary>
        /// Scrolls the panel by the specified amount in the X and Y direction.
        /// </summary>
        /// <param name="amount">The amount to scroll in both X and Y.</param>
        public void Scroll(Vector2 amount)
        {
            ScrollTo(_childOffset + amount);
        }



        /// <summary>
        /// Scrolls the panel by the specified amount in the X direction.
        /// </summary>
        /// <param name="amount">The amount to scroll in the X direction.</param>
        public void ScrollX(float amount)
        {
            ScrollToX(_childOffset.X + amount);
        }



        /// <summary>
        /// Scrolls the panel by the specified amount in the Y direction.
        /// </summary>
        /// <param name="amount">The amount to scroll in the Y direction.</param>
        public void ScrollY(float amount)
        {
            ScrollToY(_childOffset.Y + amount);
        }



        /// <summary>
        /// Scrolls the panel fully to the top.
        /// </summary>
        public void ScrollToTop()
        {
            ScrollTo(new Vector2(_childOffset.X, 0.0f));
        }



        /// <summary>
        /// Scrolls the panel fully to the bottom.
        /// </summary>
        public void ScrollToBottom()
        {
            Vector2 bottom = new Vector2(_childOffset.X, -_childBounds.Height + Bounds.Height);
            if (HScrollVisible)
                bottom.Y -= _scrollBarWidth;

            ScrollTo(bottom);
        }



        /// <summary>
        /// Scrolls the panel fully to the left.
        /// </summary>
        public void ScrollToLeft()
        {
            ScrollTo(new Vector2(0.0f, _childOffset.Y));
        }



        /// <summary>
        /// Scrolls the panel fully to the right.
        /// </summary>
        public void ScrollToRight()
        {
            Vector2 right = new Vector2(-_childBounds.Width + Bounds.Width, _childOffset.Y);
            if (VScrollVisible)
                right.X -= _scrollBarWidth;

            ScrollTo(right);
        }



        /// <summary>
        /// Scrolls the panel to the specified offset in X and Y.
        /// </summary>
        /// <param name="offset">The total offset to scroll to in X and Y.</param>
        public void ScrollTo(Vector2 offset)
        {
            ScrollToX(offset.X);
            ScrollToY(offset.Y);
        }



        /// <summary>
        /// Scrolls the panel to the specified offset in the X direction.
        /// </summary>
        /// <param name="x">The total desired X offset of the panel.</param>
        public void ScrollToX(float x)
        {
            float minX = -_childBounds.Width + Bounds.Width;
            float maxX = 0.0f;

            if (VScrollVisible)
                minX -= _scrollBarWidth;

            _childOffset.X = x;
            if (_childOffset.X < minX)
                _childOffset.X = minX;

            if (_childOffset.X > maxX)
                _childOffset.X = maxX;
        }



        /// <summary>
        /// Scrolls the panel to the specified offset on the Y axis.
        /// </summary>
        /// <param name="y">The total desired Y offset of the panel.</param>
        public void ScrollToY(float y)
        {
            float minY = -_childBounds.Height + Bounds.Height;
            float maxY = 0.0f;

            if (HScrollVisible)
                minY -= _scrollBarWidth;

            _childOffset.Y = y;

            if (_childOffset.Y < minY)
                _childOffset.Y = minY;

            if (_childOffset.Y > maxY)
                _childOffset.Y = maxY;
        }



        public override void OnRender(Vector2 offset, RectangleF updateRect)
        {
            // adltodo: render scroll bars.

#if DEBUG
            Profiler.Instance.StartBlock("GUIScroll.OnRender");
#endif

            DrawUtil.ClearBitmapModulation();

            RectangleF ctrlRect = new RectangleF(offset, _bounds.Extent);

            // fill the update rect with the fill color
            if (Style.IsOpaque)
                DrawUtil.RectFill(ctrlRect, Style.FillColor[CustomColor.ColorBase]);

            // if there's a border, draw the border
            if (Style.HasBorder)
                DrawUtil.Rect(ctrlRect, Style.BorderColor[CustomColor.ColorBase]);

            if (VScrollVisible)
            {
                RectangleF vScrollRect = ctrlRect;
                vScrollRect.X += ctrlRect.Width - _scrollBarWidth;
                vScrollRect.Width = _scrollBarWidth;
                vScrollRect.Height -= _scrollBarWidth;

                //DrawUtil.Rect(vScrollRect, new Microsoft.Xna.Framework.Graphics.Color(255, 0, 0));
            }

            if (HScrollVisible)
            {
                RectangleF hScrollRect = ctrlRect;
                hScrollRect.Y += ctrlRect.Height - _scrollBarWidth;
                hScrollRect.Width -= _scrollBarWidth;
                hScrollRect.Height = _scrollBarWidth;
                //DrawUtil.Rect(hScrollRect, new Microsoft.Xna.Framework.Graphics.Color(255, 0, 0));
            }

            // render the child controls
            RectangleF childUpdateRect = updateRect;

            if (VScrollVisible)
                childUpdateRect.Width -= _scrollBarWidth;

            if (HScrollVisible)
                childUpdateRect.Height -= _scrollBarWidth;

#if DEBUG
            Profiler.Instance.EndBlock("GUIScroll.OnRender");
#endif

            _RenderChildControls(offset + _childOffset, childUpdateRect);
        }



        public override void CopyTo(TorqueObject obj)
        {
            base.CopyTo(obj);

            GUIScroll obj2 = (GUIScroll)obj;
        }



        public override bool OnInputEvent(ref TorqueInputDevice.InputEventData data)
        {
            if (_inputMap != null)
            {
                if (_inputMap.ProcessInput(data))
                    return true;
            }

            if (data.DeviceTypeId == XGamePadDevice.GamePadId)
            {
                switch (data.ObjectId)
                {
                    case (int)XGamePadDevice.GamePadObjects.LeftThumbX:
                        _scrollAmountX = -data.Value;
                        return true;

                    case (int)XGamePadDevice.GamePadObjects.LeftThumbY:
                        _scrollAmountY = data.Value;
                        return true;

                    // trap these so they don't get used for focus changing by the canvas
                    case (int)XGamePadDevice.GamePadObjects.LeftThumbUpButton:
                    case (int)XGamePadDevice.GamePadObjects.LeftThumbDownButton:
                    case (int)XGamePadDevice.GamePadObjects.LeftThumbLeftButton:
                    case (int)XGamePadDevice.GamePadObjects.LeftThumbRightButton:
                        return true;
                }
            }
            else if (data.DeviceTypeId == XKeyboardDevice.KeyboardId)
            {
                switch (data.ObjectId)
                {
                    case (int)Keys.PageUp:
                        if (data.EventAction == TorqueInputDevice.Action.Make)
                            _scrollAmountY = 1;
                        else
                            _scrollAmountY = 0;
                        return true;

                    case (int)Keys.PageDown:
                        if (data.EventAction == TorqueInputDevice.Action.Make)
                            _scrollAmountY = -1;
                        else
                            _scrollAmountY = 0;
                        return true;

                    case (int)Keys.Left:
                        if (data.EventAction == TorqueInputDevice.Action.Make)
                            _scrollAmountX = 1;
                        else
                            _scrollAmountX = 0;
                        return true;

                    case (int)Keys.Right:
                        if (data.EventAction == TorqueInputDevice.Action.Make)
                            _scrollAmountX = -1;
                        else
                            _scrollAmountX = 0;
                        return true;
                }
            }

            return base.OnInputEvent(ref data);
        }



        public void ProcessTick(Move move, float dt)
        {
            if (Math.Abs(_scrollAmountX) > 0.1f)
                ScrollX(_scrollAmountX * _scrollSpeed);

            if (Math.Abs(_scrollAmountY) > 0.1f)
                ScrollY(_scrollAmountY * _scrollSpeed);
        }



        public void InterpolateTick(float k)
        {
            // adltodo: Scrolling could be smoother if we interpolate it
        }

        #endregion


        #region Private, protected, internal methods

        protected override bool _OnWake()
        {
            // we need to be ticked for when the control is scrolling
            ProcessList.Instance.AddTickCallback(this);
            return base._OnWake();
        }



        protected override void _OnSleep()
        {
            // no need to tick when the gui isn't active
            ProcessList.Instance.RemoveObject(this);
            base._OnSleep();
        }



        protected override void _ChildResized(GUIControl child)
        {
            base._ChildResized(child);
            _UpdateBounds();
        }



        protected override void _OnChildAdd(GUIControl child)
        {
            base._OnChildAdd(child);
            _UpdateBounds();
        }



        protected override void _OnChildRemove(GUIControl child)
        {
            base._OnChildRemove(child);
            _UpdateBounds();
        }



        private void _UpdateBounds()
        {
            Vector2 upperRight = new Vector2(0.0f, 0.0f);
            Vector2 lowerRight = new Vector2(0.0f, 0.0f);

            int numObjects = GetNumObjects();

            for (int idx = 0; idx < numObjects; idx++)
            {
                GUIControl child = (GUIControl)GetObject(idx);
                if (child == null)
                    continue;

                Vector2 childLowerRight = child.Position + child.Bounds.Extent;

                if (childLowerRight.X > lowerRight.X)
                    lowerRight.X = childLowerRight.X;

                if (childLowerRight.Y > lowerRight.Y)
                    lowerRight.Y = childLowerRight.Y;
            }

            _childBounds = new RectangleF(upperRight, lowerRight - upperRight);
        }

        #endregion


        #region Private, protected, internal fields

        Vector2 _childOffset = new Vector2(0.0f, 0.0f);
        RectangleF _childBounds = new RectangleF(0.0f, 0.0f, 0.0f, 0.0f);
        float _scrollBarWidth = 10.0f;

        // amounts to scroll the contents in each direction per tick
        float _scrollAmountX = 0.0f;
        float _scrollAmountY = 0.0f;

        // the speed at which scrolling happens
        float _scrollSpeed = 50.0f;

        #endregion
    }
}
