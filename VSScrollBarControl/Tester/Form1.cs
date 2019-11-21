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
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using static ThemeLoader;

namespace Tester
{
    public partial class Form1 : Form
    {
        private Point _StartPoint;

        public Form1() => InitializeComponent();

        private void pictureBox1_MouseDown(object sender, MouseEventArgs e) { if (e.Button == MouseButtons.Left) { _StartPoint = e.Location; } }

        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {            
            if (e.Button == MouseButtons.Left)
            {
                Point changePoint = new Point(e.Location.X - _StartPoint.X,
                                              e.Location.Y - _StartPoint.Y);

                panel1.AutoScrollPosition = new Point(-panel1.AutoScrollPosition.X - changePoint.X,
                                                      -panel1.AutoScrollPosition.Y - changePoint.Y);

                vsScrollBarControl1.AdjustValue(changePoint.X);
                vsScrollBarControl2.AdjustValue(inVert: changePoint.Y);
            }
        }

        private void button2_Click(object sender, EventArgs e) => vsScrollBarControl2.Focus();
        private void button3_Click(object sender, EventArgs e) => vsScrollBarControl1.RemoteScroll(-10);
        private void button4_Click(object sender, EventArgs e) => vsScrollBarControl1.RemoteScroll(10);
        private void button5_Click(object sender, EventArgs e) { propertyGrid1.Refresh(); }
        private void button6_Click(object sender, EventArgs e) => vsScrollBarControl1.Focus();

        private void button7_Click(object sender, EventArgs e) => MessageBox.Show($"HMin : {panel1.HorizontalScroll.Minimum} HMax : {panel1.HorizontalScroll.Maximum} " +
            $"HLarge : {panel1.HorizontalScroll.LargeChange} VMin : {panel1.VerticalScroll.Minimum} VMax : {panel1.VerticalScroll.Maximum} VLarge : {panel1.VerticalScroll.LargeChange}");

        private void button8_Click(object sender, EventArgs e) => vsScrollBarControl2.RemoteScroll(-10);
        private void button9_Click(object sender, EventArgs e) => vsScrollBarControl2.RemoteScroll(10);
        private void button12_Click(object sender, EventArgs e) { panel1.Dispose(); button5.PerformClick(); }

        private void vsScrollBarControl1_ValueChanged(object sender, EventArgs e) => label1.Text = $"{vsScrollBarControl1.Value}";
        private void vsScrollBarControl2_ValueChanged(object sender, EventArgs e) => label4.Text = $"{vsScrollBarControl2.Value}";
        private void hScrollBar1_ValueChanged(object sender, EventArgs e) => label2.Text = $"{hScrollBar1.Value}";
        private void vScrollBar1_ValueChanged(object sender, EventArgs e) => label10.Text = $"{vScrollBar1.Value}";

        private void button1_Click(object sender, EventArgs e)
        {
            vsScrollBarControl1.dumb();

            //Debug.Print($"{vsScrollBarControl1.RemoteObject.ClientRectangle.Width} {vsScrollBarControl1.RemoteObject.DisplayRectangle.Width}");
        }
    }
}