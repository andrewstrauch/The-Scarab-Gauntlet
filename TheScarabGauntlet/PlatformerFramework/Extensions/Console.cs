//-----------------------------------------------------------------------------
// Torque X Game Engine
// Copyright © GarageGames.com, Inc.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.IO;
using GarageGames.Torque.Core;
using GarageGames.Torque.GUI;
using GarageGames.Torque.Platform;
using GarageGames.Torque.Sim;
using GarageGames.Torque.MathUtil;
using GarageGames.Torque.XNA;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;



namespace GarageGames.Torque.PlatformerFramework
{
#if TORQUE_CONSOLE
    /// <summary>
    /// MLText GUI control that can color text based on tags in the input string.
    /// </summary>
    internal class GUIConsoleText : GUIMLText
    {
    #region Public methods

        /// <summary>
        /// Overrides the standard ml text rendering to color the text based on tags in the input
        /// string.
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="updateRect"></param>
        public override void OnRender(Vector2 offset, RectangleF updateRect)
        {
            DrawUtil.ClearBitmapModulation();

            RectangleF ctrlRect = new RectangleF(offset, _bounds.Extent);

            // fill the update rect with the fill color
            if (_style.IsOpaque)
                DrawUtil.RectFill(ctrlRect, _style.FillColor[CustomColor.ColorBase]);

            // if there's a border, draw the border
            if (_style.HasBorder)
                DrawUtil.Rect(ctrlRect, _style.BorderColor[CustomColor.ColorBase]);

            Vector2 pos = offset - updateRect.Point;
            Vector2 size = new Vector2(Bounds.Width, _style.Font.Instance.LineSpacing + _style.LineSpacing);
            for (int i = 0; i < _splitText.Length; i++)
            {
                int end = 0;
                CustomColor color = CustomColor.ColorUser0;
                if (_splitText[i].StartsWith("<customcolor:"))
                {
                    int start = _splitText[i].IndexOf(":") + 1;
                    end = _splitText[i].IndexOf(">");
                    color = (CustomColor)int.Parse(_splitText[i].Substring(start, end - start));
                    end++;
                }

                DrawUtil.JustifiedText(_style.Font, pos, size, _style.Alignment, _style.TextColor[color], _splitText[i].Substring(end));
                pos.Y += size.Y;
            }

            // render the child controls
            _RenderChildControls(offset, updateRect);
        }

    #endregion
    }

    internal class GUIConsoleTextEdit : GUITextEdit
    {
    #region Public methods

        public override bool OnInputEvent(ref TorqueInputDevice.InputEventData data)
        {
            if (data.DeviceTypeId == XGamePadDevice.GamePadId)
            {
                switch (data.ObjectId)
                {
                    case (int)XGamePadDevice.GamePadObjects.LeftThumbX:
                        ConsoleGui.Instance._scroll.ActiveScrollY = -data.Value;
                        return true;

                    case (int)XGamePadDevice.GamePadObjects.LeftThumbY:
                        ConsoleGui.Instance._scroll.ActiveScrollY = data.Value;
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
                        if(data.EventAction == TorqueInputDevice.Action.Make)
                            ConsoleGui.Instance._scroll.ActiveScrollY = 1;
                        else
                            ConsoleGui.Instance._scroll.ActiveScrollY = 0;
                        return true;

                    case (int)Keys.PageDown:
                        if (data.EventAction == TorqueInputDevice.Action.Make)
                            ConsoleGui.Instance._scroll.ActiveScrollY = -1;
                        else
                            ConsoleGui.Instance._scroll.ActiveScrollY = 0;
                        return true;

                    case (int)Keys.Left:
                        if (data.EventAction == TorqueInputDevice.Action.Make)
                            ConsoleGui.Instance._scroll.ActiveScrollX = 1;
                        else
                            ConsoleGui.Instance._scroll.ActiveScrollX = 0;
                        return base.OnInputEvent(ref data);

                    case (int)Keys.Right:
                        if (data.EventAction == TorqueInputDevice.Action.Make)
                            ConsoleGui.Instance._scroll.ActiveScrollX = -1;
                        else
                            ConsoleGui.Instance._scroll.ActiveScrollX = 0;
                        return base.OnInputEvent(ref data);
                }
            }

            return base.OnInputEvent(ref data);
        }

    #endregion
    }


    /// <summary>
    /// A GUI for displaying the contents of the console.
    /// </summary>
    internal class ConsoleGui
    {

    #region Constructors

        internal ConsoleGui()
        {
#if !XBOX
            if (_dumpToFile)
            {
                // delete the console file
                if (File.Exists(_consoleDumpFile))
                    File.Delete(_consoleDumpFile);
            }
#endif
        }

    #endregion


    #region Static methods, fields, constructors

        /// <summary>
        /// Singleton instance of the console gui.
        /// </summary>
        static public ConsoleGui Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new ConsoleGui();

                _instance._BindInput();

                return _instance;
            }
        }

        static private ConsoleGui _instance;

    #endregion


    #region Public properties, operators, constants, and enums

        /// <summary>
        /// Whether or not the console has been initialized.
        /// </summary>
        public bool IsInitialized
        {
            get { return _isInitialized; }
        }

        /// <summary>
        /// The visibility status of the console.
        /// </summary>
        public bool IsVisible
        {
            get { return _root.Awake; }
            set
            {
                if (value)
                    GUICanvas.Instance.PushDialogControl(_root, 99);
                else
                    GUICanvas.Instance.PopDialogControl(_root);
            }
        }

        /// <summary>
        /// Specifies whether or not the console should dump to a file.
        /// Default value is true. Note that if you disable TORQUE_CONSOLE
        /// the console will not exist and no file access will occur.
        /// Also note that this feature only works on Windows!
        /// </summary>
        public bool DumpToFile
        {
            get { return _dumpToFile; }
            set
            {
                if (_dumpToFile == value)
                    return;

                _dumpToFile = value;
#if !XBOX
                // delete any existing log file
                if (value && !string.IsNullOrEmpty(_consoleDumpFile) && File.Exists(_consoleDumpFile))
                    File.Delete(_consoleDumpFile);
#endif

            }
        }

        /// <summary>
        /// Specifies the name of the file that the console will dump
        /// console spam to. This only works on Windows!
        /// </summary>
        public string ConsoleDumpFile
        {
            get { return _consoleDumpFile; }
            set
            {
                if (_consoleDumpFile.Equals(value))
                    return;

                _consoleDumpFile = value;
#if !XBOX
                // delete any existing log file with this name
                if (!string.IsNullOrEmpty(value) && File.Exists(_consoleDumpFile))
                    File.Delete(_consoleDumpFile);
#endif
            }
        }

    #endregion


    #region Public methods

        /// <summary>
        /// Initializes the console gui.
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized)
            {
                TorqueConsole.Warn("ConsoleGui.InitializeGui - Gui is already initialized!");
                return;
            }

            // define gui styles
            GUIStyle rootStyle = new GUIStyle();
            rootStyle.IsOpaque = true;
            rootStyle.FillColor[CustomColor.ColorBase] = new Color(0, 0, 0, 128);

            GUIStyle scrollerStyle = new GUIStyle();
            scrollerStyle.HasBorder = true;
            scrollerStyle.BorderColor[CustomColor.ColorBase] = new Color(0, 0, 0, 255);
            scrollerStyle.Focusable = false;

            GUIMLTextStyle bodyStyle = new GUIMLTextStyle();
            bodyStyle.Alignment = TextAlignment.JustifyLeft;
            bodyStyle.TextColor[CustomColor.ColorUser0] = Color.White;
            bodyStyle.TextColor[CustomColor.ColorUser1] = Color.Yellow;
            bodyStyle.TextColor[CustomColor.ColorUser2] = Color.Red;
            bodyStyle.SizeToText = true;
            bodyStyle.AutoSizeHeightOnly = false;
            bodyStyle.FontType = "Arial12";

            GUITextStyle textStyle = new GUITextStyle();
            textStyle.Alignment = TextAlignment.JustifyLeft;
            textStyle.TextColor[CustomColor.ColorBase] = Color.White;
            textStyle.SizeToText = false;
            textStyle.Focusable = true;
            textStyle.FontType = "Arial14";

            // init gui controls
            _root = new GUIControl();
            _root.Style = rootStyle;
            _root.HorizSizing = HorizSizing.Width;
            _root.VertSizing = VertSizing.Height;
            _root.Size = new Vector2(GUICanvas.Instance.Size.X, GUICanvas.Instance.Size.Y);

            _scroll = new GUIScroll();
            _scroll.Style = scrollerStyle;
            _scroll.HorizSizing = HorizSizing.Width;
            _scroll.VertSizing = VertSizing.Relative;
            _scroll.Size = new Vector2(GUICanvas.Instance.Size.X, GUICanvas.Instance.Size.Y - 30.0f);
            _scroll.Visible = true;
            _scroll.Folder = _root;

            _textEdit = new GUIConsoleTextEdit();
            _textEdit.Style = textStyle;
            _textEdit.HorizSizing = HorizSizing.Relative;
            _textEdit.VertSizing = VertSizing.Relative;
            _textEdit.Position = new Vector2(10.0f, GUICanvas.Instance.Size.Y - 30.0f);
            _textEdit.Size = new Vector2(GUICanvas.Instance.Size.X - 20.0f, 30.0f);
            _textEdit.FocusOnWake = true;
            _textEdit.Visible = true;
            _textEdit.Folder = _root;
            _textEdit.OnValidateText = _ValidateText;

            int keyboardId = InputManager.Instance.FindDevice("keyboard");
            _textEdit.InputMap.BindAction(keyboardId, (int)Keys.Up, _NextHistory);
            _textEdit.InputMap.BindAction(keyboardId, (int)Keys.Down, _PreviousHistory);

            _text = new GUIConsoleText();
            _text.Style = bodyStyle;
            _text.HorizSizing = HorizSizing.Relative;
            _text.VertSizing = VertSizing.Relative;
            _text.Position = new Vector2(10.0f, 10.0f);
            _text.Size = new Vector2(400.0f, 20.0f);
            _text.Visible = true;
            _text.Folder = _scroll;
            _text.Text = _consoleText;

            _isInitialized = true;
        }

        /// <summary>
        /// Adds an entry to the console gui.
        /// </summary>
        /// <param name="text">The text to add.</param>
        /// <param name="color">The color of the text.</param>
        public void AddEntry(string text, CustomColor color)
        {
            // add the custom color tag
            string newText = "<customcolor:" + (int)color + ">" + text + "\n";

            // draw text doesn't like '{' or '}'
            newText = newText.Replace('{', '(').Replace('}', ')');

            // store our own copy of the text so it can be dumped or added to the gui
            // when it is initialized
            _consoleText += newText;

            if (_isInitialized)
            {
                _text.Text += newText;
                _scroll.ScrollToBottom();
            }

#if !XBOX
            if(_dumpToFile && !string.IsNullOrEmpty(_consoleDumpFile))
            {
                // create a file with that name
                if (!File.Exists(_consoleDumpFile))
                    File.Create(_consoleDumpFile).Close();

                // create a text writer for the file
                TextWriter file = File.AppendText(_consoleDumpFile);

                // write the new line to the file
                file.Write(text + "\n");

                // close the file
                file.Close();
            }
#endif
        }

    #endregion


    #region Private, protected, internal methods

        protected void _ValidateText()
        {
            // print the command to the console
            string text = _textEdit.Text;
            TorqueConsole.Echo("\n>" + text);

            // parse
            string error = string.Empty;
            if (!ConsoleParser.ParseText(text, ref error))
                TorqueConsole.Error("Parse Error: " + error);

            // save the entered text
            _history.Add(text);
            _currentHistory = _history.Count;

            // clear the text field
            _textEdit.Text = string.Empty;
        }

        void _NextHistory(float val)
        {
            if (val > 0.0f)
            {
                if (_currentHistory > 0)
                {
                    _currentHistory--;
                    _textEdit.Text = _history[_currentHistory];
                }
            }
        }

        void _PreviousHistory(float val)
        {
            if (val > 0.0f)
            {
                if (_currentHistory < _history.Count - 1)
                    _textEdit.Text = _history[_currentHistory];
                else
                    _textEdit.Text = string.Empty;

                if (_currentHistory < _history.Count)
                    _currentHistory++;
            }
        }

        void _ToggleGui(float val)
        {
            if (val > 0.0f)
            {
                // can't initialize the GUI until the Canvas exists
                if (!_isInitialized && GUICanvas.Instance.Bounds.IsValid)
                    Initialize();

                IsVisible = !IsVisible;
            }
        }

        void _BindInput()
        {
            if (_isInputBound || !InputManager.Instance.HasDevices)
                return;

            // bind toggling of the console gui to ~ and the back button
            int gamepadId = InputManager.Instance.FindDevice("gamepad0");
            InputMap.Global.BindAction(gamepadId, (int)XGamePadDevice.GamePadObjects.LeftThumbButton, _ToggleGui);

            int keyboardId = InputManager.Instance.FindDevice("keyboard");
            InputMap.Global.BindAction(keyboardId, (int)Keys.OemTilde, _ToggleGui);

            _isInputBound = true;
        }

    #endregion


    #region Private, protected, internal fields

        GUIMLText _text;
        GUITextEdit _textEdit;
        internal GUIScroll _scroll;
        GUIControl _root;

        bool _isInitialized = false;
        bool _isInputBound = false;
        string _consoleText = string.Empty;

        List<string> _history = new List<string>();
        int _currentHistory = 0;

        bool _dumpToFile = true;
        string _consoleDumpFile = "console.log";
        string _fileText = string.Empty;

    #endregion
    }
#endif

    /// <summary>
    /// Simple wrapper for printing messages to a ConsoleGui.
    /// </summary>
    public class TorqueConsole
    {
        #region Static methods, fields, constructors

        /// <summary>
        /// Prints a string to the console. This should be used for general information about the
        /// status of the engine and game.
        /// </summary>
        /// <param name="format">Format string.</param>
        /// <param name="args">Parameters in the format string.</param>
        [Conditional("TORQUE_CONSOLE")]
        static public void Echo(string format, params object[] args)
        {
            Echo(String.Format(format, args));
        }

        /// <summary>
        /// Prints a string to the console. This should be used for general information about the
        /// status of the engine and game.
        /// </summary>
        /// <param name="str">The string to print.</param>
        [Conditional("TORQUE_CONSOLE")]
        static public void Echo(string str)
        {
            Console.WriteLine(str);
#if TORQUE_CONSOLE
            ConsoleGui.Instance.AddEntry(str, CustomColor.ColorUser0);
#endif
        }

        /// <summary>
        /// Prints a string to the console. This should be used for messages about things that probably
        /// shouldn't happen, even though things will still work.
        /// </summary>
        /// <param name="format">Format string.</param>
        /// <param name="args">Parameters in the format string.</param>
        [Conditional("TORQUE_CONSOLE")]
        static public void Warn(string format, params object[] args)
        {
            Warn(String.Format(format, args));
        }

        /// <summary>
        /// Prints a string to the console. This should be used for messages about things that probably
        /// shouldn't happen, even though things will still work.
        /// </summary>
        /// <param name="str">The string to print.</param>
        [Conditional("TORQUE_CONSOLE")]
        static public void Warn(string str)
        {
            Console.WriteLine(str);
#if TORQUE_CONSOLE
            ConsoleGui.Instance.AddEntry(str, CustomColor.ColorUser1);
#endif
        }

        /// <summary>
        /// Prints a string to the console. This should be used to display errors that cause the engine
        /// to break or crash.
        /// </summary>
        /// <param name="format">Format string.</param>
        /// <param name="args">Parameters in the format string.</param>
        [Conditional("TORQUE_CONSOLE")]
        static public void Error(string format, params object[] args)
        {
            Error(String.Format(format, args));
        }

        /// <summary>
        /// Prints a string to the console. This should be used to display errors that cause the engine
        /// to break or crash.
        /// </summary>
        /// <param name="str">The string to print.</param>
        [Conditional("TORQUE_CONSOLE")]
        static public void Error(string str)
        {
            Console.WriteLine(str);
#if TORQUE_CONSOLE
            ConsoleGui.Instance.AddEntry(str, CustomColor.ColorUser2);
#endif
        }

        #endregion
    }
}
