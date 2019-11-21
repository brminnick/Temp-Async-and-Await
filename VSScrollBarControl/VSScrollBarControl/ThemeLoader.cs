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
using System.Diagnostics;
using System.Drawing;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Linq;
using static misc;

public static class ThemeLoader
{
    public interface ITheme { public Task<bool> ApplyTheme(); }

    public class Theme
    {
        /// <summary> Class definition for Custom Color Themes.  This class can be easily altered, to meet your needs. </summary>
        public string Name { get; set; }
        public Color Primary_Background { get; set; }
        public Color Secondary_Background { get; set; }
        public Color Tertiary_Background { get; set; }
        public Color Arrow_Normal { get; set; }
        public Color Arrow_Down { get; set; }
        public Color Arrow_Hover { get; set; }
        public Color Border_Normal { get; set; }
        public Color Edge_Normal { get; set; }
        public Color Edge_Light { get; set; }
        public Color Edge_Dark { get; set; }
        public Color Gutter_Normal { get; set; }
        public Color Gutter_Hover { get; set; }
        public Color Thumb_Normal { get; set; }
        public Color Thumb_Down { get; set; }
        public Color Thumb_Hover { get; set; }
    }

    private const string DefaultThemeFIle = "Themes.json";

    private static async Task<List<Theme>> GetThemes(string inFile = DefaultThemeFIle) =>
                        (FileExists(inFile) ? new JavaScriptSerializer().Deserialize<List<Theme>>(await ReadTextAsync(inFile).ConfigureAwait(false)) : null);

    /// <summary> Get the Color values of a named Theme. </summary>
    public static async Task<Theme> GetValuesForTheme(string inTheme, string inFile = DefaultThemeFIle)
    {
        List<Theme> buffer = await GetThemes(inFile).ConfigureAwait(false);

        return ((buffer != null) ? (Theme)(from i in buffer where i.Name == inTheme select i.Name) : null);
    }

    public static async Task<string[]> GetThemeNames(string inFile = DefaultThemeFIle)
    {
        List<Theme> buffer = await GetThemes(inFile).ConfigureAwait(false);

        return ((buffer != null) ? (from i in buffer select i.Name).ToArray() : new string[0]);
    }
}