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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Math;

static class misc
{
    #region BytePacker
        /// <summary> Allows you to pack 4 Bytes, into a single Int32 value and preserving each of their values. </summary>
        [StructLayout(LayoutKind.Explicit)]
        struct ByteArray
        {
            [FieldOffset(0)]
            public byte Byte01;
            [FieldOffset(1)]
            public byte Byte02;
            [FieldOffset(2)]
            public byte Byte03;
            [FieldOffset(3)]
            public byte Byte04;

            [FieldOffset(0)]
            public Int32 bytearray;

            public ByteArray(Int32 bytearray) : this() => this.bytearray = bytearray;
        }

        public enum WhichByte
        {
            Byte0,
            Byte1,
            Byte2,
            Byte3
        }

        /// <summary> Get a specific Byte from the packed Int32. </summary>
        public static byte BytePackGet(Int32 inValue, WhichByte inByte = WhichByte.Byte0)
        {
            ByteArray gb = new ByteArray(inValue);

            switch (inByte)
            {
                case WhichByte.Byte0:
                    return gb.Byte01;
                case WhichByte.Byte1:
                    return gb.Byte02;
                case WhichByte.Byte2:
                    return gb.Byte03;
                case WhichByte.Byte3:
                    return gb.Byte04;
                default:
                    return default(byte);
            }
        }

        /// <summary> Set a specific Byte of the specified packed Int32. </summary>
        public static int BytePackSet(Int32 inValue, WhichByte inByte, byte inSetByteTo)
        {
            ByteArray gb = new ByteArray(inValue);

            switch (inByte)
            {
                case WhichByte.Byte0:
                    gb.Byte01 = inSetByteTo;
                    break;
                case WhichByte.Byte1:
                    gb.Byte02 = inSetByteTo;
                    break;
                case WhichByte.Byte2:
                    gb.Byte03 = inSetByteTo;
                    break;
                case WhichByte.Byte3:
                    gb.Byte04 = inSetByteTo;
                    break;
            }

            return gb.bytearray;
        }
    #endregion

    #region Directory & File stuff : Start
        ////Temp : Get a list of image files from a directory.  Real editor will load from single image files.
        //imageList1.Images.Clear(); listView2.Clear();

        //Parallel.ForEach(GetFileList(Application.StartupPath), (fn) =>
        //{
        //    listView2.Invoke((MethodInvoker)delegate
        //    {
        //        imageList1.Images.Add(Icon.ExtractAssociatedIcon(fn));
        //        listView2.Items.Add(fn, imageList1.Images.Count - 1);
        //    });
        //});

        /// <summary> Specifies that the operating system should create a new file. This requires <see cref="System.Security.Permissions.FileIOPermissionAccess.Write"/>
        /// If the file already exists, <see cref="IOException"/> exception is thrown
        /// </summary>
        public static void CreateFile(string inFile) { if (!FileExists(inFile)) { using (File.Open(inFile, FileMode.CreateNew)) { } } }
        /// <summary> Create all directories and subdirectories in the specified path unless they already exist. </summary>
        public static void CreateDirectory(string inPath) { Directory.CreateDirectory(Path.GetDirectoryName(inPath)); }
        /// <summary> Determines whether the specified file exists. </summary>
        public static bool FileExists(string inFile) => File.Exists(inFile);
        /// <summary> Determines whether the specified path refers to an existing directory on disk. </summary>
        public static bool DirectoryExists(string inDirectory) => Directory.Exists(inDirectory);
        /// <summary> Returns the file name and extension of the specified path string. </summary>
        public static string ExtractFileName(string inFile) => Path.GetFileName(inFile);
        /// <summary> Asynchronously writes the bytes of given string to a specified file. </summary>
        public static async void WriteToFileAsync(string inPath, string inText)
        {        
            async Task WriteTextAsync(string filePath, string text)
            {
                byte[] encodedText = Encoding.Unicode.GetBytes(text);

                using (FileStream sourceStream = new FileStream(filePath, FileMode.Append, FileAccess.Write, FileShare.None,
                       bufferSize: 4096, useAsync: true))
                {
                    await sourceStream.WriteAsync(encodedText, 0, encodedText.Length);
                };
            }

            await WriteTextAsync(inPath, inText);
        }

        /// <summary> Asynchronously read text from a specified file. </summary>
        public static async Task<string> ReadTextAsync(string file)
        {        
            using (FileStream sourceStream = new FileStream(file, FileMode.Open, FileAccess.Read,
                                                            FileShare.Read, bufferSize: 4096, useAsync: true))
            {
                StringBuilder sb = new StringBuilder();

                byte[] buffer = new byte[0x1000];
                int numRead;

                while ((numRead = await sourceStream.ReadAsync(buffer, 0, buffer.Length)) != 0)
                {
                    sb.Append(Encoding.UTF8.GetString(buffer, 0, numRead));
                }

                return sb.ToString();
            }
        }

        /// <summary> Synchronously read text from a specified file. </summary>
        public static string ReadTextSync(string file)
        {
            using (FileStream sourceStream = new FileStream(file, FileMode.Open, FileAccess.Read,
                                                            FileShare.Read, bufferSize: 4096, useAsync: false))
            {
                StringBuilder sb = new StringBuilder();

                byte[] buffer = new byte[0x1000];
                int numRead;

                while ((numRead = sourceStream.Read(buffer, 0, buffer.Length)) != 0)
                {
                    sb.Append(Encoding.UTF8.GetString(buffer, 0, numRead));
                }

                return sb.ToString();
            }
        }

    /// <summary> Scintilla - Read Text from a specified file. </summary>
    //public static async Task<Document> LoadFileAsync(ILoader loader, string path, CancellationToken cancellationToken)
    //{
    //    try
    //    {
    //        using (var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true))
    //        using (var reader = new StreamReader(file))
    //        {
    //            char[] buffer = new char[4096];
    //            int count = 0;

    //            while ((count = await reader.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false)) > 0)
    //            {                        
    //                cancellationToken.ThrowIfCancellationRequested(); //Check for cancellation

    //                if (!loader.AddData(buffer, count)) { throw new IOException("The data could not be added to the loader."); } //Add the data to the document
    //            }

    //            return loader.ConvertToDocument();
    //        }
    //    }
    //    catch
    //    {
    //        loader.Release();
    //        throw;
    //    }
    //}
    #endregion

    #region Image stuff
    /// <summary> Convert a specified byte[] to an image. </summary>
    public static Image byteArrayToImage(byte[] byteArrayIn) => Image.FromStream(new MemoryStream(byteArrayIn));

        /// <summary> Convert a specified image to a byte[]. </summary>
        public static byte[] imageToByteArray(Image imageIn)
        {
            MemoryStream ms = new MemoryStream();
            imageIn.Save(ms, imageIn.RawFormat);

            return ms.ToArray();
        }

        public static Bitmap RotateImage(Image inputImage, float angleDegrees, bool upsizeOk, bool clipOk, Color backgroundColor)
        {
            if (inputImage == null) { return default(Bitmap); }

            // Test for zero rotation and return a clone of the input image
            if (angleDegrees == 0f) { return (Bitmap)inputImage.Clone(); }

            int oldWidth = inputImage.Width,
                oldHeight = inputImage.Height,
                newWidth = oldWidth,
                newHeight = oldHeight;

            float scaleFactor = 1f;

            // If upsizing wanted or clipping not OK calculate the size of the resulting bitmap
            if (upsizeOk || !clipOk)
            {
                double angleRadians = angleDegrees * PI / 180d,
                       cos = Abs(Cos(angleRadians)),
                       sin = Abs(Sin(angleRadians));

                newWidth = (int)Round(oldWidth * cos + oldHeight * sin);
                newHeight = (int)Round(oldWidth * sin + oldHeight * cos);
            }

            // If upsizing not wanted and clipping not OK need a scaling factor
            if (!upsizeOk && !clipOk)
            {
                scaleFactor = Min((float)oldWidth / newWidth, (float)oldHeight / newHeight);
                newWidth = oldWidth;
                newHeight = oldHeight;
            }

            Bitmap newBitmap = new Bitmap(newWidth, newHeight, PixelFormat.Format32bppArgb);

            newBitmap.SetResolution(inputImage.HorizontalResolution, inputImage.VerticalResolution);

            using (Graphics graphicsObject = Graphics.FromImage(newBitmap))
            {
                graphicsObject.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphicsObject.PixelOffsetMode = PixelOffsetMode.HighQuality;
                graphicsObject.SmoothingMode = SmoothingMode.HighQuality;

                if (backgroundColor != Color.Transparent) { graphicsObject.Clear(backgroundColor); } //Fill in the specified background color

                //Tranform and Rotate image
                {
                    graphicsObject.TranslateTransform(newWidth / 2f, newHeight / 2f); //Update the transformation matrix

                    if (scaleFactor != 1f) { graphicsObject.ScaleTransform(scaleFactor, scaleFactor); }

                    graphicsObject.RotateTransform(angleDegrees);

                    graphicsObject.TranslateTransform(-oldWidth / 2f, -oldHeight / 2f);
                }

                graphicsObject.DrawImage(inputImage, 0, 0); //Draw the result
            }

            return newBitmap;
        }

        /// <summary> Draw Border and Drag Handles around a specified control </summary>
        private static void DrawControlBorder(object sender)
        {
            Control control = (Control)sender;

            int HANDLE_SIZE = 5,
                HANDLE_HALF_SIZE = (HANDLE_SIZE / 2);

            using (Graphics g = control.CreateGraphics())
            {
                Rectangle Border = new Rectangle(
                    new Point(control.Location.X - HANDLE_HALF_SIZE, control.Location.Y - HANDLE_HALF_SIZE),
                    new Size(control.Size.Width + HANDLE_SIZE, control.Size.Height + HANDLE_SIZE));

                //define the 8 drag handles, that has the size of DRAG_HANDLE_SIZE
                Size gripSize = new Size(HANDLE_SIZE, HANDLE_SIZE);

                int x, y = control.Location.Y;

                x = control.Location.X + (control.Width / 2) - HANDLE_HALF_SIZE;
                Rectangle N = new Rectangle(new Point(x, y - HANDLE_SIZE), gripSize);
                Rectangle S = new Rectangle(new Point(x, y + control.Height), gripSize);

                x = control.Location.X - HANDLE_SIZE;
                Rectangle W = new Rectangle(new Point(x, y + (control.Height / 2) - HANDLE_HALF_SIZE), gripSize);
                Rectangle SW = new Rectangle(new Point(x, y + control.Height), gripSize);
                Rectangle NW = new Rectangle(new Point(x, y - HANDLE_SIZE), gripSize);

                x = control.Location.X + control.Width;
                Rectangle NE = new Rectangle(new Point(x, y - HANDLE_SIZE), gripSize);
                Rectangle E = new Rectangle(new Point(x, y + (control.Height / 2) - HANDLE_HALF_SIZE), gripSize);
                Rectangle SE = new Rectangle(new Point(x, y + control.Height), gripSize);

                //draw the border and drag handles
                {
                    ControlPaint.DrawBorder(g, Border, Color.Gray, ButtonBorderStyle.Dotted);
                    ControlPaint.DrawGrabHandle(g, NW, true, true);
                    ControlPaint.DrawGrabHandle(g, N, true, true);
                    ControlPaint.DrawGrabHandle(g, NE, true, true);
                    ControlPaint.DrawGrabHandle(g, W, true, true);
                    ControlPaint.DrawGrabHandle(g, E, true, true);
                    ControlPaint.DrawGrabHandle(g, SW, true, true);
                    ControlPaint.DrawGrabHandle(g, S, true, true);
                    ControlPaint.DrawGrabHandle(g, SE, true, true);
                }
            }
        }
    #endregion

    #region String utilities
        /// <summary> String extension method to remove all white spaces from a specified string. </summary>
        public static string RemoveWhitespace(this string str) => string.Join("", str.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));
        /// <summary> String extension method to format a specified string to TitleCase. </summary>
        public static string ToTitleCase(this string inString) => new CultureInfo("en-US", false).TextInfo.ToTitleCase(inString);

        /// <summary> Build a String from an inputted T[] or List<T>.  StringBuilder is more effecient than just concatenating strings together. </summary>
        public static string BuildString<T>(IEnumerable<T> inValues)
        {
            StringBuilder build = new StringBuilder();

            using (IEnumerator<T> iterator = inValues.GetEnumerator())
            {
                if (!iterator.MoveNext()) { return default(string); }

                build.Append(iterator.Current.ToString());

                while (iterator.MoveNext())
                {
                    build.Append(iterator.Current.ToString());
                }
            }

            return build.ToString();
        }
    #endregion

    /// <summary> Maps a number range to another specified range. </summary>
    public static float Map(float value, float istart, float istop, float ostart, float ostop) => (ostart + (ostop - ostart) * ((value - istart) / (istop - istart)));
        
    /// <summary> Essentially ... clamp </summary>
    public static int LimitToRange(this int value, int inclusiveMinimum, int inclusiveMaximum)
    {
        if (value < inclusiveMinimum) { return inclusiveMinimum; }
        else if (value > inclusiveMaximum) { return inclusiveMaximum; }
        return value;
    }

    /// <summary> Swap function that uses Generics. </summary>
    public static void Swap<T>(ref T a, ref T b)
    {
        T temp = a;
        a = b; b = temp;
    }

    /// <summary> Iterate over the items & sub-items of a MenuStrip and executed an Action on the item if it's found. </summary>
    public static void ExecuteActionOnMenuItem(MenuStrip inMainMenu, string inFind, Action<dynamic> inAction)
    {
        void GetMenuItems(ToolStripMenuItem item, List<ToolStripMenuItem> items)
        {
            item.DropDownItems.OfType<ToolStripMenuItem>().ToList().ForEach(dropItem =>
            {
                if (dropItem.Text == inFind) { inAction.Invoke(dropItem); return; }

                GetMenuItems(dropItem, items);
            });
        }

        List<ToolStripMenuItem> MainItems;

        (MainItems = inMainMenu.Items.OfType<ToolStripMenuItem>().ToList()).ForEach(Item =>
        {
            if (Item.Text == inFind) { inAction.Invoke(Item); return; }

            Item.DropDownItems.OfType<ToolStripMenuItem>().ToList().ForEach(dropItem =>
            {
                if (dropItem.Text == inFind) { inAction.Invoke(dropItem); return; }

                GetMenuItems(dropItem, MainItems);
            });
        });
    }

    /// <summary> Clones a specified object. </summary>
    public static T CloneObject<T>(T inClone)
    {
        //Can't clone read-only values. Which makes sense. But, a control like Scintilla has a Styles collection
        //they ARE values you set in code Styles[Style.Default].ForeColor = IntToColor(0xEFEAEF); Things like that,
        //I'd like to transfer ... :< ... Think, this Cloner mostly does work like it does in the VS Designer and 
        //most situations will work perfectly.
        Type controlType = Assembly.Load(inClone.GetType().Namespace).GetType(inClone.GetType().Namespace + "." + inClone.GetType().Name);

        T outClone = (T)Activator.CreateInstance(controlType);

        Hashtable PropertyList = new Hashtable();

        FieldInfo[] fields = controlType.GetFields();
        PropertyInfo[] properties = controlType.GetProperties();

        foreach (FieldInfo field in fields)
        {
            try { field.SetValue(outClone, field.GetValue(inClone)); }
            catch (Exception) { }
        }

        foreach (PropertyInfo property in properties)
        {
            try { property.SetValue(outClone, property.GetValue(inClone)); }
            catch (Exception) { }
        }

        return outClone;
    }

    /// <summary> Converts the <Tag> property of a control to an integer. </summary>
    public static int TagToInt(object inObject) => Convert.ToInt32((inObject as Control).Tag);

    /// <summary> Converts a Hex Color code to RGB - IntToColor(0xD0DAE2); </summary>
    public static Color IntToColor(int rgb) => Color.FromArgb(255, (byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb);

    public static bool insideGivenControl(Control control) => control.RectangleToScreen(control.ClientRectangle).Contains(Cursor.Position);

    /// <summary> Displays a Confirmation Dialog Box. </summary>
    public static bool DialogConfirmation(string szTitle, string szMessage) => (MessageBox.Show(szMessage, szTitle, MessageBoxButtons.OKCancel, MessageBoxIcon.Information) == DialogResult.OK);

    /// <summary> Open the default HTTP browser with a specified URL. </summary>
    public static void OpenUrl(string url="https://google.com")
    {
        try { Process.Start(url); }
        catch
        {
            if (Environment.OSVersion.ToString().ToUpper().Contains("MICROSOFT"))
            { 
                url = url.Replace("&", "^&");
                Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
            }
            else
            {
                Process.Start("open", url);
            }
        }
    }

    public static bool IsNull(Object T) => T == null;
    public static bool NotNull(Object T) => T != null;

    public static PointF RandomVector(float speed, Random inRandom)
    {
        float rangle = (float)((inRandom.NextDouble() * (Math.PI * 2)));

        speed *= (float)inRandom.NextDouble();

        return new PointF((float)Math.Cos(rangle) * speed, (float)Math.Sin(rangle) * speed);
    }

    /// <summary> Range class. </summary>
    public class Range<T> where T : IComparable<T>
    {
        /// <summary>Minimum value of the range.</summary>
        public T Minimum { get; set; }

        /// <summary>Maximum value of the range.</summary>
        public T Maximum { get; set; }

        /// <summary>Presents the Range in readable format.</summary>
        /// <returns>String representation of the Range</returns>
        public override string ToString()
        {
            return string.Format("[{0} - {1}]", this.Minimum, this.Maximum);
        }

        /// <summary>Determines if the range is valid.</summary>
        /// <returns>True if range is valid, else false</returns>
        public bool IsValid()
        {
            return this.Minimum.CompareTo(this.Maximum) <= 0;
        }

        /// <summary>Determines if the provided value is inside the range.</summary>
        /// <param name="value">The value to test</param>
        /// <returns>True if the value is inside Range, else false</returns>
        public bool ContainsValue(T value)
        {
            return (this.Minimum.CompareTo(value) <= 0) && (value.CompareTo(this.Maximum) <= 0);
        }

        /// <summary>Determines if this Range is inside the bounds of another range.</summary>
        /// <param name="Range">The parent range to test on</param>
        /// <returns>True if range is inclusive, else false</returns>
        public bool IsInsideRange(Range<T> range)
        {
            return this.IsValid() && range.IsValid() && range.ContainsValue(this.Minimum) && range.ContainsValue(this.Maximum);
        }

        /// <summary>Determines if another range is inside the bounds of this range.</summary>
        /// <param name="Range">The child range to test</param>
        /// <returns>True if range is inside, else false</returns>
        public bool ContainsRange(Range<T> range)
        {
            return this.IsValid() && range.IsValid() && this.ContainsValue(range.Minimum) && this.ContainsValue(range.Maximum);
        }
    }
}