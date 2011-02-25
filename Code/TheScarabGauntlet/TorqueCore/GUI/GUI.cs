//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using GarageGames.Torque.Core;
using GarageGames.Torque.Sim;
using GarageGames.Torque.MathUtil;



namespace GarageGames.Torque.GUI
{
    /// <summary>
    /// Game UI specific classes wrapping a variety of GUIControls, designed to expose
    /// a user interface a player may interact with, should implement this interface
    /// to provide a means to, using Reflection, initialize the Game UI.
    /// </summary>
    public interface IGUIScreen
    {
    }



    /// <summary>
    /// The GUICanvas searches for focus capable controls. Use this type to specify
    /// in which direction to search.
    /// </summary>
    public enum SearchPolicy
    {
        /// <summary>
        /// Direct focus searching up and down.
        /// </summary>
        Vertical,

        /// <summary>
        /// Direct focus searching left and right.
        /// </summary>
        Horizontal
    }



    public enum HorizSizing
    {
        /// <summary>
        /// The control will be fixed on the left side and in width.
        /// </summary>
        Right = 0,

        /// <summary>
        /// The control will be fixed on the left side and right side.
        /// </summary>
        Width,

        /// <summary>
        /// The control will be fixed on the right side and in width.
        /// </summary>
        Left,

        /// <summary>
        /// The control will stay centered horizontally to parent.
        /// </summary>
        Center,

        /// <summary>
        /// The control will resize relative to parent.
        /// </summary>
        Relative,
    }



    public enum VertSizing
    {
        /// <summary>
        /// The control will be fixed on the top side and in height.
        /// </summary>
        Bottom = 0,

        /// <summary>
        /// The control will be fixed on the top side and bottom side.
        /// </summary>
        Height,

        /// <summary>
        /// The control will be fixed on the bottom side and in height.
        /// </summary>
        Top,

        /// <summary>
        /// The control will stay centered vertically to parent.
        /// </summary>
        Center,

        /// <summary>
        /// The control will resize relative to parent.
        /// </summary>
        Relative,
    }



    /// <summary>
    /// Represents any flipping to be done about the x and/or y axis
    /// of a bitmap in texture space.
    /// </summary>
    [Flags] // rdbtodo: move this someplace else ?
    public enum BitmapFlip
    {
        None = 0,
        FlipX = 1,
        FlipY = 2,
        FlipXY = FlipX | FlipY,
    }



    /// <summary>
    /// The alignment of text evenly between left and right margins.
    /// </summary>
    public enum TextAlignment
    {
        /// <summary>
        /// Adjust the text position so that the left margin is lined up.
        /// </summary>
        JustifyLeft,

        /// <summary>
        /// Adjust the text position so that the right margin is lined up.
        /// </summary>
        JustifyRight,

        /// <summary>
        /// Adjust the text position so the left and right margin are evenly spaced.
        /// </summary>
        JustifyCenter,
    }



    /// <summary>
    /// Defines a set of user customizeable color values.
    /// </summary>
    public enum CustomColor
    {
        ColorBase = 0,  // base
        ColorHL,        // hilight
        ColorNA,        // disabled
        ColorSEL,       // selected
        ColorUser0,     // custom ...
        ColorUser1,
        ColorUser2,
        ColorUser3,
        ColorUser4,
        ColorUser5,

        NumColors
    }



    /// <summary>
    /// Collection of Color objects.
    /// </summary>
    public sealed class ColorCollection
    {
        /// <summary>
        /// Returns the Color at the specified index.
        /// </summary>
        /// <param name="index">Index of the Color to return.</param>
        /// <returns>The color at the requested index.</returns>
        public Color this[CustomColor index]
        {
            get { return _color[(int)index]; }
            set { _color[(int)index] = value; }
        }

        Color[] _color = new Color[(int)CustomColor.NumColors];
    }



    /// <summary>
    /// Called when a GUIControl is added to the visible scene by the GUICanvas.
    /// </summary>
    /// <param name="ctrl">The GUIControl that was told to awaken.</param>
    public delegate void OnWakeDelegate(GUIControl ctrl);



    /// <summary>
    /// Called when a GUIControl is removed from the visible scene by the GUICanvas.
    /// </summary>
    /// <param name="ctrl">The GUIControl that was told to sleep.</param>
    public delegate void OnSleepDelegate(GUIControl ctrl);



    /// <summary>
    /// Called when a GUIControl gains priority input focus.
    /// </summary>
    /// <param name="ctrl">The GUIControl that gained focus.</param>
    public delegate void OnGainFocusDelegate(GUIControl ctrl);



    /// <summary>
    /// Called when a GUIControl loses priority input focus.
    /// </summary>
    /// <param name="ctrl">The GUIControl that losted focus.</param>
    public delegate void OnLoseFocusDelegate(GUIControl ctrl);



    /// <summary>
    /// Called when a GUIControl is resized.
    /// </summary>
    /// <param name="ctrl">The GUIControl that was resized.</param>
    /// <param name="newPosition">New position of the resized control.</param>
    /// <param name="newSize">New size of the resized control.</param>
    public delegate void OnResizeDelegate(GUIControl ctrl, Vector2 newPosition, Vector2 newSize);
}
