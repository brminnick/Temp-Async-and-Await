#region Copyright notice and license
/*The MIT License(MIT)
Copyright(c), Tobey Peters, https://github.com/tobeypeters

Permission is hereby granted, free of charge, to any person obtaining a copy of this software
and associated documentation files (the "Software"), to deal in the Software without restriction,
including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense,
and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so,
subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
#endregion
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Threading.Tasks;
using System.Windows.Forms;
using AsyncAwaitBestPractices;
using static ThemeLoader;

namespace VSScrollBarControl
{
    #region Reference Controller
        enum ReferenceMode { Increment = 1, Decrement = -1 }

        /// <summary> Stores a Reference Count "How many ScrollBars are controlling a given Control", and the ScrollBar state. The state is saved,
        /// if you want to use a single ScrollBar to Remote multiple Controls at once. You can't remote multiple controls at the sametime.  You have
        /// to switch back & forth between the desired Controls. </summary>
        class ControllerNode
        {
            public int Count,
                       Min,
                       Max,
                       SmallChange,
                       LargeChange,
                       Value;
        }

        static class ReferenceController
        {
            /// <summary> Dictionary used to store a list of Controls being remote controlled. </summary>
            public static Dictionary<int, ControllerNode> Controller = new Dictionary<int, ControllerNode>();

            /// <summary> Get the saved State of a specific Control. </summary>
            /// GetControllerNode Starts
            public static bool GetControllerNode(int inHash, bool bDoesntMatter) => Controller.ContainsKey(inHash);  //Do I leave this?  Waste of time?  Technically, faster.  //Don't have to create & return a ControllerNode.
            public static ControllerNode GetControllerNode(int inHash) => (Controller.ContainsKey(inHash) ? Controller[inHash] : null);
            /// GetControllerNode Ends

            /// <summary> Saves the State of a specific Control. </summary>
            public static bool SetControllerNode(int inHash, ControllerNode inValues)
            {
                if (GetControllerNode(inHash, true))
                {
                    Controller[inHash].Min = inValues.Min;
                    Controller[inHash].Max = inValues.Max;
                    Controller[inHash].LargeChange = inValues.LargeChange;
                    Controller[inHash].SmallChange = inValues.SmallChange;
                    Controller[inHash].Value = inValues.Value;

                    return true;
                }

                return false;
            }

            /// <summary> Increment or Decrement a given Keys Reference count. </summary>
            public static void AdjustReferenceCountFor(ScrollableControl inControl, ReferenceMode inMode = ReferenceMode.Increment, bool inOrientHorz = true)
            {
                int UpdateReferenceCountFor(int key, ReferenceMode inRefMode = ReferenceMode.Increment) => 
                                            Controller[key].Count += (int)inRefMode; //ContainsKey() is called before this.

                int hash = inControl.GetHashCode();

                if ((inMode == ReferenceMode.Increment) && !Controller.ContainsKey(hash))
                {
                    Controller.Add(hash, new ControllerNode() { Count = 1 });
                }
                else { if (UpdateReferenceCountFor(hash, inMode) < 1) { Controller.Remove(hash); } }            
            }
        }
    #endregion Reference Controller

    [ComVisible(true), ClassInterface(ClassInterfaceType.AutoDispatch)]
    [DefaultProperty("Value"), DefaultEvent("Scroll")]
    public partial class VSScrollBarControl : UserControl, ITheme
    {
    #region Init & De-Init
        protected override CreateParams CreateParams
        {
            [SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.UnmanagedCode)]
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x02000000;  // Turn on WS_EX_COMPOSITED
                return cp;
            }
        }

        public VSScrollBarControl()
        {
            InitializeComponent();

            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.CacheText, true);

            if (UseTheme)
            {
                ApplyTheme().SafeFireAndForget();
                PrintThemes().SafeFireAndForget();
            }
        }

        public async Task<bool> ApplyTheme()
        {
            Theme buffer = await GetValuesForTheme("Visual Studio");

            if (buffer != null)
            {
                ArrowColor = buffer.Arrow_Normal;
                ArrowDownColor = buffer.Arrow_Down;
                ArrowHoverColor = buffer.Arrow_Hover;
                BackgroundColor = buffer.Primary_Background;
                BorderColor = buffer.Primary_Background;
                GutterTrackColor = buffer.Gutter_Normal;
                ThumbColor = buffer.Thumb_Normal;
                ThumbDownColor = buffer.Thumb_Down;
                ThumbHoverColor = buffer.Thumb_Hover;

                return true;
            }

            return false;
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);

            if (RemoteControl != NONE)
            {
                string buffer = RemoteControl;
                RemoteControl = NONE; RemoteControl = buffer;
            }
            else
            { SetThumbPos(); }
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            RemoteControl = NONE;

            base.OnHandleDestroyed(e);
        }

        protected override void SetBoundsCore(int x, int y, int width, int height, BoundsSpecified specified)
        {
            Size MIN_SIZE = (bOrientHorz ? new Size((ti.ArrowSize * 2), ti.ArrowSize + 1) : new Size(ti.ArrowSize + 1, (ti.ArrowSize * 2)));

            base.SetBoundsCore(x, y,
                               (bOrientHorz ? ((width < MIN_SIZE.Width) ? MIN_SIZE.Width : width) : MIN_SIZE.Width),
                               (bOrientHorz ? MIN_SIZE.Height : ((height < MIN_SIZE.Height) ? MIN_SIZE.Height : height)),
                               specified);
        }
    #endregion Init & De-Init

    #region TrackInfo Class
        private class TrackInfo
        {
            public int ArrowSize { get; } = 18;

            public ScrollOrientation Orientation { get; set; }

            private int _GutterWidth;
            public int GutterWidth
            {
                get => _GutterWidth;
                set { _GutterWidth = (value - ((ArrowSize - 1) * 2)); }
            }

            public int GutterHeight { get; set; }

            public int ThumbStart { get => (ArrowSize - 1); }
            public int ThumbEnd { get => (ThumbStart + (GutterWidth - ThumbWidth) + 1); }

            private int _ThumbWidth;
            public int ThumbWidth
            {
                get => _ThumbWidth;
                set { _ThumbWidth = value.LimitToRange(SCROLLBAR_SIZE, value); }
            }

            private int _ThumbHeight;
            public int ThumbHeight
            {
                get => ((Orientation == ScrollOrientation.HorizontalScroll) ? (GutterHeight - 2) : _ThumbHeight);
                set => _ThumbHeight = value.LimitToRange(SCROLLBAR_SIZE, value);
            }

            public int ThumbPos { get; set; } = -1;
        }
    #endregion TrackInfo Class

    #region Standard Properties
        private int _Minimum = MINIMUM;
        [Category("Standard"), Description("The lower limit value of the scrollable range."), RefreshProperties(RefreshProperties.Repaint), DefaultValue(MINIMUM)]
        public int Minimum
        {
            get => _Minimum;
            set
            {
                if (_Minimum != value)
                {
                    if (Maximum < value) { Maximum = value; }
                    if (value > Value) { Value = value; }

                    _Minimum = value;

                    OnSizeChanged(EventArgs.Empty);
                }
            }
        }

        private int _Maximum = MAXIMUM;
        [Category("Standard"), Description("The upper limit value of the scrollable range."), RefreshProperties(RefreshProperties.Repaint), DefaultValue(MAXIMUM)]
        public int Maximum
        {
            get => _Maximum;
            set
            {
                if (_Maximum != value)
                {
                    if (Minimum > value) { Minimum = value; }
                    if (value < Value) { Value = value; }

                    _Maximum = value;

                    OnSizeChanged(EventArgs.Empty);
                }
            }
        }

        private int _SmallChange = SMALLCHANGE;
        [Category("Standard"), Description("The amount by which the scroll box position changes when the user clicks a scroll arrow or presses an arrow key."), RefreshProperties(RefreshProperties.Repaint), DefaultValue(SMALLCHANGE)]
        public int SmallChange
        {
            get => Math.Min(_SmallChange, LargeChange);
            set
            {
                if (_SmallChange != value)
                {
                    _SmallChange = value.LimitToRange(Minimum, value);

                    OnSizeChanged(EventArgs.Empty);
                }
            }
        }

        private int _LargeChange = LARGECHANGE;
        [Category("Standard"), Description("The amount by which the scroll box position changes when the user clicks in the scroll bar or presses the PAGE UP or PAGE DOWN keys."), RefreshProperties(RefreshProperties.Repaint), DefaultValue(LARGECHANGE)]
        public int LargeChange
        {
            get => Math.Min(_LargeChange, Maximum - Minimum + 1);
            set
            {
                if (_LargeChange != value)
                {
                    _LargeChange = value.LimitToRange(0, int.MaxValue);

                    OnSizeChanged(EventArgs.Empty);
                }
            }
        }

        private int _Value = VALUE;
        [Category("Standard"), Description("The value the scroll box position represents."), DefaultValue(VALUE)]
        public int Value
        {
            get => _Value;
            set
            {
                if (_Value != value)
                {
                    _Value = value.LimitToRange(Minimum, MaxValue);

                    if (!bMouseDown) { SetThumbPos(); }  //Do this in OnValuechanged?  Guess, it doesn't matter.

                    if (_Value == Minimum) { OnScroll(new ScrollEventArgs(ScrollEventType.First, -1, Value, Orientation)); }
                    if (_Value == MaxValue) { OnScroll(new ScrollEventArgs(ScrollEventType.Last, -1, Value, Orientation)); }

                    OnValueChanged(this, EventArgs.Empty);

                    Invalidate();
                }
            }
        }
    #endregion Standard Properties

    #region Custom Properties
        [Category("Custom"), Description("The color used for the components Arrows normal state.")]
        public Color ArrowColor { get; set; } = SystemColors.ControlDark;

        [Category("Custom"), Description("The color used for the components Arrows down state.")]
        public Color ArrowDownColor { get; set; } = SystemColors.ControlLight;

        [Category("Custom"), Description("The color used for the components Arrows hover state.")]
        public Color ArrowHoverColor { get; set; } = SystemColors.Highlight;

        [Category("Custom"), Description("The color used for the components Background.")]
        public Color BackgroundColor { get; set; } = SystemColors.Control;

        [Category("Custom"), Description("The color used for the components Border."), DefaultValue(typeof(Color), "67, 67, 70")]
        public Color BorderColor { get; set; } = SystemColors.Control;

        [Category("Custom"), Description("Indicates whether or not the components ScrollBar Thumb has border edges."), DefaultValue(true)]
        public bool BorderEdges { get; set; } = true;

        [Category("Custom"), Description("The color used for the components Gutter Track."), DefaultValue(typeof(Color), "72, 72, 75")]
        public Color GutterTrackColor { get; set; } = SystemColors.ControlLight;

        [Category("Custom"), Description("Indicates whether or not the components Gutter is drawn."), DefaultValue(false)]
        public bool GutterTrackShow { get; set; } = false;

        [Category("Custom"), Description("Specifies if the Control hides both scrollbars during \"Scroll\" mode."), DefaultValue(false)]
        public bool HideBothScrollbars { get; set; } = false;

        [Category("Custom"), Description("Specifies if the Control is allowed to \"Scroll\" multiple controls. Can only set this property, if RemoteControl equals \"(none)\"."), DefaultValue(false)]
        private bool _MultiScrollMode = false;
        public bool MultiScrollMode
        {
            get => _MultiScrollMode;
            set { _MultiScrollMode = ((value && (RemoteControl == NONE)) ? value : _MultiScrollMode); }
        }

        private ScrollOrientation _Orientation = ScrollOrientation.HorizontalScroll;
        [Category("Custom"), Description("The components orientation relative to the containers orientation.")]
        public ScrollOrientation Orientation
        {
            get => _Orientation;
            set
            {
                if (_Orientation != value)
                {
                    _Orientation = value;

                    bOrientHorz = (_Orientation == ScrollOrientation.HorizontalScroll);

                    Size = (bOrientHorz ? new Size(Size.Height, 19) : new Size(19, Size.Width));
                }
            }
        }

        [Category("Custom"), Description("The color used for the components Thumbs normal state.")]
        public Color ThumbColor { get; set; } = SystemColors.ControlDark;

        [Category("Custom"), Description("The color used for the components Thumbs down State.")]
        public Color ThumbDownColor { get; set; } = SystemColors.Highlight;
        
        [Category("Custom"), Description("The color used for the components Thumbs hover State.")]
        public Color ThumbHoverColor { get; set; } = SystemColors.Highlight;

        [Category("Custom"), Description("Indicates whether or not the components Thumb is drawn like a standard ScrollBar thumb."), DefaultValue(true)]
        public bool UseStandardThumb { get; set; } = true;

        [Category("Custom"), Description("Indicates whether or not the component uses a specified Named Theme to use for its color palette."), DefaultValue(true)]
        public bool UseTheme { get; set; } = true;
    #endregion Custom Properties

    #region Hidden Properties
        [Browsable(false)] public override Color BackColor { get; set; }
        [Browsable(false)] public override Image BackgroundImage { get; set; }
        [Browsable(false)] public override ImageLayout BackgroundImageLayout { get; set; }
    #endregion Hidden Properties

    #region StringConverter Class
        class SelectedObjectConverter : StringConverter
        {
            public static StandardValuesCollection StringObjects = new StandardValuesCollection(new string[0]);

            public override bool GetStandardValuesSupported(ITypeDescriptorContext context) => true;

            public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context) => StringObjects;
        }
    #endregion


        }
        #endregion

        public async Task PrintThemes()
        {
            var buffer = await GetThemeNames("testjson.txt").ConfigureAwait(false);

            foreach (string s in buffer)
            {
                Debug.Print($"Theme: {s}");
            }

            SelectedObjectConverter.StringObjects = new SelectedObjectConverter.StandardValuesCollection(buffer);
            //SelectedObjectConverter.StringObjects = new SelectedObjectConverter.StandardValuesCollection(Task.Run(async () => await GetThemeNames("testjson.txt").ConfigureAwait(false)).Result);
        }


        #region RemoteControl Object
        private string _RemoteControl = NONE;
        public ScrollableControl RemoteObject;
        [Category("Custom"), Description("Specifies which Control you want to remotely scroll."), DefaultValue("(none)"), TypeConverter(typeof(SelectedObjectConverter))]
        public string RemoteControl
        {
            //Build a String[] of Scrollable Controls and display those in the property grid
            get
            {
                void FindScrollControls(ref List<string> inBuffer, Control inControl)
                {
                    if (inControl == null) return;

                    for (int i = 0; i < inControl.Controls.Count; i++)
                    {
                        if (typeof(ScrollableControl).IsAssignableFrom(inControl.Controls[i]?.GetType())) { if (inControl.Controls[i].Name != "") { inBuffer.Add(inControl.Controls[i].Name); } }

                        if (inControl.Controls[i].Controls.Count > 0) { FindScrollControls(ref inBuffer, inControl.Controls[i]); }
                    }
                }

                List<string> objectStringBuffer = new List<string>() { NONE };

                FindScrollControls(ref objectStringBuffer, Parent);
                objectStringBuffer.Sort();

                SelectedObjectConverter.StringObjects = new SelectedObjectConverter.StandardValuesCollection(objectStringBuffer);

                return _RemoteControl;
            }
            set
            {
                Control FindControl(Control inControl)
                {
                    if (inControl == null) { return null; }

                    Control FindResult = null;

                    for (int i = 0; i < inControl.Controls.Count; i++)
                    {
                        if (inControl.Controls[i].Name == _RemoteControl)
                        {
                            FindResult = inControl.Controls[i]; break;
                        }
                        else
                        {
                            FindResult = FindControl(inControl.Controls[i]);
                            if (FindResult != null) { break; }
                        }
                    }

                    return FindResult;
                }

                void Pair(ScrollableControl inControl)
                {
                    if (!DesignMode && inControl != null)
                    {
                        ReferenceController.AdjustReferenceCountFor(inControl); //Add a reference

                        inControl.Disposed += RemoteObjectDisposed;
                        inControl.Layout += RemoteObjectLayoutChanged;

                        SetLayoutScrollValuesFor(inControl, inInitMode: true);
                    }
                }

                void UnPair(ScrollableControl inControl)
                {
                    if (!DesignMode && inControl != null)
                    {
                        if (!MultiScrollMode)
                        {
                            inControl.Layout -= RemoteObjectLayoutChanged;
                            inControl.Disposed -= RemoteObjectDisposed;

                            ReferenceController.AdjustReferenceCountFor(inControl, ReferenceMode.Decrement);  //Remove the reference to the RemoteObject
                        }                        

                        SetLayoutScrollValuesFor(inControl, inResetMode: true);
                    }
                }

                void ResetDefaults() { Maximum = MAXIMUM; Minimum = MINIMUM; Value = VALUE; SmallChange = SMALLCHANGE; LargeChange = LARGECHANGE; }

                _RemoteControl = value;

                if (DesignMode) { return; }

                if (MultiScrollMode && (RemoteObject != null))
                {
                    ReferenceController.SetControllerNode(RemoteObject.GetHashCode(), new ControllerNode()
                    {
                        Min = Minimum,
                        Max = Maximum,
                        LargeChange = LargeChange,
                        SmallChange = SmallChange,
                        Value = Value
                    });

                    ReferenceController.AdjustReferenceCountFor(RemoteObject, ReferenceMode.Decrement); //Counter would just keep incrementing everytime, I switched to a different Control.                    
                }

                UnPair(RemoteObject);

                RemoteObject = null;

                Control Result = FindControl(Parent);

                if (Result != null)
                {
                    ControllerNode cnValues = ReferenceController.GetControllerNode(Result.GetHashCode());

                    if (cnValues != null)
                    {
                        Minimum = cnValues.Min;
                        Maximum = cnValues.Max;
                        LargeChange = cnValues.LargeChange;
                        SmallChange = cnValues.SmallChange;
                        Value = cnValues.Value;
                    }

                    RemoteObject = (ScrollableControl)Result;
                    Pair(RemoteObject);
                }
                else
                {
                    ResetDefaults();
                }

                OnSizeChanged(EventArgs.Empty);
                SetThumbPos();
            }
        }
    #endregion RemoteControl Object

        public async void dumb()
        {
            string[] test = await GetThemeNames("testjson.txt");

            foreach (string item in test)
            {
                Debug.Print($"item : {item}");
            }
        }

        #region RemoteControl Object Events
        private void RemoteObjectLayoutChanged(object sender, LayoutEventArgs e) => SetLayoutScrollValuesFor(RemoteObject);

        private void RemoteObjectDisposed(object sender, EventArgs e) => RemoteControl = NONE;
    #endregion RemoteControl Object Events

    #region Track Info
        private TrackInfo ti { get; set; } = new TrackInfo();

        private float GutterIncrement { get => (((float)(MaxValue + ((Minimum < 0) ? Math.Abs(Minimum) : 0))) / ((ti.GutterWidth) - ti.ThumbWidth)); }
        private int MaxValue { get => (Maximum - ((RemoteObject != null) ? LargeChange : ((Minimum < 0) ? 0 : Minimum))); }
    #endregion

    #region Event Handlers
        private EventHandler<ScrollEventArgs> onScroll;
        [Category("Custom"), Description("Occurs when the user moves the scroll box.")]
        public new event EventHandler<ScrollEventArgs> Scroll
        {
            add => onScroll += value;
            remove => onScroll -= value;
        }

        protected virtual void OnScroll(object sender, ScrollEventArgs e) => onScroll?.Invoke(this, e);

        private EventHandler onValueChanged;
        public event EventHandler ValueChanged
        {
            add => onValueChanged += value;
            remove => onValueChanged -= value;
        }

        [Category("Custom"), Description("Event raised when the value of the Size property is changed on the control.")]
        protected override void OnSizeChanged(EventArgs e)
        {
            ti.Orientation = Orientation;

            ti.GutterWidth = (bOrientHorz ? Width : Height);
            ti.GutterHeight = (bOrientHorz ? Height : Width);

            ti.ThumbHeight = ti.ThumbWidth = Convert.ToInt32(Math.Min(ti.GutterWidth,
                                            Math.Max((Maximum == 0 || LargeChange == 0) ? ti.GutterWidth : (LargeChange * (float)ti.GutterWidth) / Maximum, 10f)));

            if (ti.ThumbPos == -1) { ti.ThumbPos = ti.ThumbStart; }

            SetClickOverlayRects();

            Invalidate();
        }

        [Category("Custom"), Description("Occurs when value of the control changes.")]
        protected virtual void OnValueChanged(object sender, EventArgs e) => onValueChanged?.Invoke(this, e);
    #endregion Event Handler

    #region Mouse Events
        private void WhatAmIOver(MouseEventArgs inArgs)
        {
            //Was getting random false trues.  So, I added the Clicks test and logic in the MouseLeave().
            bOverDecArrow = ((inArgs.Clicks != -1) && DecArrow.Contains(inArgs.Location));
            bOverGutter = ((inArgs.Clicks != -1) && Gutter.Contains(inArgs.Location) && (inArgs != null));
            bOverThumb = ((inArgs.Clicks != -1) && Thumb.Contains(inArgs.Location) && (inArgs != null));
            bOverIncArrow = ((inArgs.Clicks != -1) && IncArrow.Contains(inArgs.Location) && (inArgs != null));

            if (bOverThumb) { bOverGutter = false; }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                bMouseDown = true;

                WhatAmIOver(e);

                if (bOverThumb) { uh = ti.ThumbPos; OnScroll(this, new ScrollEventArgs(ScrollEventType.ThumbTrack, 0, Value, Orientation)); }

                Invalidate();
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left || bMouseWheel)
            {
                bMouseDown = false;

                if (bOverDecArrow || bOverIncArrow)
                {
                    int smallamt = (int)((bOverDecArrow ? -SmallChange : SmallChange) * GutterIncrement);

                    Value += smallamt;

                    SetThumbPos();

                    RemoteScroll((bOrientHorz ? smallamt : 0), (bOrientHorz ? 0 : smallamt));

                    OnScroll(this, new ScrollEventArgs(((bOverDecArrow) ? ScrollEventType.SmallDecrement : ScrollEventType.SmallIncrement), -1, Value, Orientation));
                }

                if (bOverThumb) { OnScroll(this, new ScrollEventArgs(ScrollEventType.ThumbPosition, -1, Value, Orientation)); Invalidate(); }
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.None) { WhatAmIOver(e); Invalidate(); }

            if (e.Button == MouseButtons.Left)
            {
                if (bMouseDown)
                {
                    if (bOverThumb)
                    {
                        if (bOrientHorz) { ti.ThumbPos = e.X.LimitToRange(ti.ThumbStart, ti.ThumbEnd); }
                        if (!bOrientHorz) { ti.ThumbPos = e.Y.LimitToRange(ti.ThumbStart, ti.ThumbEnd); }

                        Value = (Minimum + ((int)((float)(((float)((ti.ThumbPos - ti.ThumbStart + ((ti.ThumbPos > ti.ThumbStart) ? 1 : 0))) * GutterIncrement)))));

                        int scrollamt = (int)((ti.ThumbPos - uh) * GutterIncrement) + ((ti.ThumbPos == ti.ThumbStart) ? -1 : ((ti.ThumbPos == ti.ThumbEnd) ? 1 : 0));

                        RemoteScroll((bOrientHorz ? scrollamt : 0), (bOrientHorz ? 0 : scrollamt));

                        uh = ti.ThumbPos;

                        OnScroll(new ScrollEventArgs(ScrollEventType.ThumbTrack, -1, Value, Orientation));

                        SetClickOverlayRects(true);

                        Invalidate();
                    }
                }
            }
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            int WHEEL_DELTA = 120,
                wheelDelta = e.Delta;

            bool scrolled = false;

            bMouseWheel = true;
            {
                while (Math.Abs(wheelDelta) >= WHEEL_DELTA)
                {
                    bMouseDown = true;

                    scrolled = false;

                    if (wheelDelta > 0) { wheelDelta -= WHEEL_DELTA; bOverIncArrow = true; }
                    else { wheelDelta += WHEEL_DELTA; bOverDecArrow = true; }

                    if ((bOverDecArrow && (Value > Minimum)) || (bOverIncArrow && (Value < MaxValue))) //Just trying to cut down on OnScroll() triggering. 
                    {
                        OnMouseUp(e);

                        SetClickOverlayRects(true);

                        Invalidate();

                        scrolled = true;
                    }

                    bOverDecArrow = bOverIncArrow = false;
                }
            }
            bMouseWheel = false;

            if (scrolled) { OnScroll(this, new ScrollEventArgs(ScrollEventType.EndScroll, -1, Value, Orientation)); }

            if (e is HandledMouseEventArgs) { ((HandledMouseEventArgs)e).Handled = true; }

            base.OnMouseWheel(e);
        }

        protected override void OnMouseLeave(EventArgs e) { WhatAmIOver(new MouseEventArgs(MouseButtons.None, -1, 0, 0, 0)); Invalidate(); }
    #endregion

    #region KeyBoard
        protected override bool ProcessDialogKey(Keys keyData)
        {
            //Keys recognized : up&down or left&right, pageup, pagedown, home, end
            if (new List<Keys>() { Keys.Up, Keys.Down, Keys.Left, Keys.Right,
                                   Keys.PageUp, Keys.PageDown, Keys.Home, Keys.End }.Contains(keyData))
            {
                switch (keyData)
                {
                    case Keys.Up:
                    case Keys.Left:
                        Value -= SmallChange;
                        break;
                    case Keys.Right:
                    case Keys.Down:
                        Value += SmallChange;
                        break;
                    case Keys.PageUp:
                        Value -= LargeChange;
                        break;
                    case Keys.PageDown:
                        Value += LargeChange;
                        break;
                    case Keys.Home:
                        Value = Minimum;
                        break;
                    case Keys.End:
                        Value = Maximum;
                        break;
                }

                SetThumbPos(inRedraw: true);

                return true;
            }

            return base.ProcessDialogKey(keyData);
        }
    #endregion KeyBoard

    #region Procedures
        public void AdjustValue(int inHorz = 0, int inVert = 0)
        {
            if (RemoteObject == null) return;

            Value -= (bOrientHorz ? inHorz : inVert);

            SetThumbPos(inRedraw: true);
        }

        public void RemoteScroll(int inScrollAmountX = 0, int inScrollAmountY = 0)
        {
            if (RemoteObject == null) return;

            RemoteObject.AutoScrollPosition = new Point((bOrientHorz ? (-RemoteObject.AutoScrollPosition.X + inScrollAmountX) : 0),
                                                    (bOrientHorz ? 0 : (-RemoteObject.AutoScrollPosition.Y + inScrollAmountY)));

            Value += (bOrientHorz ? inScrollAmountX : inScrollAmountY);
        }

        private void SetClickOverlayRects(bool bThumbOnly = false)
        {
            if (!bThumbOnly)
            {
                DecArrow = new Rectangle(0, 0, ti.ArrowSize - (bOrientHorz ? 3 : 0), ti.ArrowSize - (bOrientHorz ? 0 : 3));

                IncArrow = new Rectangle((bOrientHorz ? (Width - ti.ArrowSize + 2) : 0), (bOrientHorz ? 0 : (Height - ti.ArrowSize + 2)), DecArrow.Width, DecArrow.Height);

                Gutter = new Rectangle((bOrientHorz ? (DecArrow.Right + 1) : DecArrow.Left), (bOrientHorz ? DecArrow.Top : DecArrow.Bottom + 1),
                                       (bOrientHorz ? (IncArrow.Left - DecArrow.Right - 2) : DecArrow.Width), (bOrientHorz ? DecArrow.Height : (IncArrow.Top - DecArrow.Bottom - 2)));
            }

            GutterTrack = new Rectangle((Gutter.X + (bOrientHorz ? 1 : 6)), ((Gutter.Y + (bOrientHorz ? 6 : 1))),
                                        (bOrientHorz ? (Gutter.Width - 1) : 6), (bOrientHorz ? 6 : (Gutter.Height - 1)));

            Thumb = new Rectangle((bOrientHorz ? ti.ThumbPos : 1), (bOrientHorz ? 1 : ti.ThumbPos), ti.ThumbWidth, ti.ThumbHeight);
            Thumber = (UseStandardThumb ? Thumb : new Rectangle((Thumb.X + (bOrientHorz ? 0 : 5)), ((Thumb.Y + (bOrientHorz ? 5 : 0))),
                                                                (bOrientHorz ? Thumb.Width : 7), (bOrientHorz ? 7 : Thumb.Height)));
        }

        private void SetLayoutScrollValuesFor(ScrollableControl inControl, bool inResetMode = false, bool inInitMode = false)
        {
            int PercentageOfMaxValue(int inValue) => (inValue < 1) ? 0 : (int)(((float)inValue / (float)(MaxValue + 1)) * 100);

            if (inControl == null) { return; }

            int hMax = (inControl.DisplayRectangle.Width - 1),
                vMax = (inControl.DisplayRectangle.Height - 1),
                hLarge = inControl.ClientRectangle.Width,
                vLarge = inControl.ClientRectangle.Height,
                CurrentValuePercentage = PercentageOfMaxValue(Value);

            if (!inResetMode)
            {
                if (inControl.DisplayRectangle.Width == inControl.ClientRectangle.Width) { hLarge += SCROLLBAR_SIZE; }
                if (inControl.DisplayRectangle.Height == inControl.ClientRectangle.Height) { vLarge += SCROLLBAR_SIZE; }
            }

            hLarge = hLarge.LimitToRange(0, hLarge);
            vLarge = vLarge.LimitToRange(0, vLarge);

            if (!inResetMode)
            {
                Maximum = (bOrientHorz ? hMax : vMax);
                LargeChange = (bOrientHorz ? hLarge : vLarge);

                if (inInitMode && ReferenceController.GetControllerNode(inControl.GetHashCode(), true))
                {
                    inControl.AutoScroll = false;
                    {
                        if (bOrientHorz || HideBothScrollbars) { inControl.HorizontalScroll.Minimum = inControl.HorizontalScroll.Maximum = 0; }
                        if (!bOrientHorz || HideBothScrollbars) { inControl.VerticalScroll.Minimum = inControl.VerticalScroll.Maximum = 0; }
                    }
                    inControl.AutoScroll = true;
                }
            }
            else
            {
                inControl.AutoScroll = false;
                {
                    ControllerNode buffer = ReferenceController.GetControllerNode(inControl.GetHashCode());

                    if (bOrientHorz || HideBothScrollbars)
                    {
                        inControl.HorizontalScroll.Maximum = hMax;
                        inControl.HorizontalScroll.LargeChange = hLarge;
                    }
                    if (!bOrientHorz || HideBothScrollbars)
                    {
                        inControl.VerticalScroll.Maximum = vMax;
                        inControl.VerticalScroll.LargeChange = vLarge;
                    }
                }
                inControl.AutoScroll = true;
            }

            Value = ((Value > 0) ? ((CurrentValuePercentage / 100) * MaxValue) : 0);
        }

        private void SetThumbPos(float inPercent = 0f, bool inRedraw = false)
        {
            ti.ThumbPos = ti.ThumbStart + ((int)((float)(Math.Abs(Value - Minimum)) / GutterIncrement));
            ti.ThumbPos = ti.ThumbPos.LimitToRange(0, ti.ThumbPos);

            SetClickOverlayRects(true);

            if (inRedraw) { Invalidate(); }
        }
    #endregion Procedures

        private int uh { get; set; }

        private const string NONE = "(none)";

        private const int MINIMUM = 0,
                          MAXIMUM = 100,
                          LARGECHANGE = 10,
                          SMALLCHANGE = 1,
                          VALUE = 0,
                          SCROLLBAR_SIZE = 17;

        private bool bOverDecArrow, bOverGutter, bOverThumb, bOverIncArrow, 
                     bMouseDown, bMouseWheel, bOrientHorz = true;

        private Rectangle DecArrow, Gutter, GutterTrack, IncArrow, Thumb, Thumber;

        private void VSScrollBarControl_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.Clear(BackgroundColor);

            using (SolidBrush brushBorder = new SolidBrush(BorderColor))
            using (SolidBrush brushDecArrow = new SolidBrush(((bMouseDown && bOverDecArrow) ? ArrowDownColor : ((!bMouseDown && bOverDecArrow) ? ArrowHoverColor : ArrowColor))))
            using (SolidBrush brushIncArrow = new SolidBrush(((bMouseDown && bOverIncArrow) ? ArrowDownColor : ((!bMouseDown && bOverIncArrow) ? ArrowHoverColor : ArrowColor))))
            using (SolidBrush brushThumb = new SolidBrush(((bMouseDown && bOverThumb) ? ThumbDownColor : ((!bMouseDown && bOverThumb) ? ThumbHoverColor : ThumbColor))))
            using (SolidBrush brushGutterTrack = new SolidBrush(GutterTrackColor))
            using (Pen penBorder = new Pen(brushBorder))
            {
                e.Graphics.DrawRectangle(penBorder, new Rectangle(0, 0, (Width - 1), (Height - 1))); //Border

                //arrows
                {
                    Point[] points = default(Point[]);
                    {
                        if (bOrientHorz) { points = new Point[] { new Point(7, 9), new Point(11, 5), new Point(11, 13) }; }
                        if (!bOrientHorz) { points = new Point[] { new Point(9, 6), new Point(5, 11), new Point(13, 11) }; }

                        e.Graphics.FillPolygon(brushDecArrow, points);

                        if (bOrientHorz) { points = new Point[] { new Point(Width - 7, 9), new Point(Width - 11, 5), new Point(Width - 11, 13) }; }
                        if (!bOrientHorz) { points = new Point[] { new Point(6, Height - 11), new Point(13, Height - 11), new Point(9, Height - 7) }; }
                    }

                    e.Graphics.FillPolygon(brushIncArrow, points);
                }

                //Overlay Click Rectangles
                //{
                //    e.Graphics.DrawRectangle(new Pen(Color.Red), IncArrow);
                //    e.Graphics.DrawRectangle(new Pen(Color.Green), DecArrow);
                //    e.Graphics.DrawRectangle(new Pen(Color.Blue), Gutter);

                if (GutterTrackShow) { e.Graphics.FillRectangle(brushGutterTrack, GutterTrack); }
                
                //}

                if ((ti.GutterWidth > SCROLLBAR_SIZE) && (ti.ThumbWidth < ti.GutterWidth)) { e.Graphics.FillRectangle(brushThumb, Thumber); }

                if (BorderEdges)
                {
                    e.Graphics.DrawRectangle(new Pen(ControlPaint.Light(brushThumb.Color)), new Rectangle(Thumb.X, Thumb.Y, Thumb.Width - 1, Thumb.Height - 1));

                    Point[] points = new Point[] { new Point(Thumb.X + 1, Thumb.Y + Thumb.Height - 1), new Point(Thumb.X + Thumb.Width - 1, Thumb.Y + Thumb.Height - 1), new Point(Thumb.X + Thumb.Width - 1, Thumb.Y) };

                    e.Graphics.DrawLine(new Pen(ControlPaint.Dark(brushThumb.Color)), points[0], points[1]);
                    e.Graphics.DrawLine(new Pen(ControlPaint.Dark(brushThumb.Color)), points[1], points[2]);
                }
            }
        }
    }
}