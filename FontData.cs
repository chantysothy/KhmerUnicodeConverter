// FontDataXML.cs
//
// Copyright 2008 Chanty Sothy
//
// Copyright (C) 2001 Free Software Foundation, Inc.
//
// This program is free software; you can redistribute it and/or
// modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; either version 2
// of the License, or (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
//
// Linking this library statically or dynamically with other modules is
// making a combined work based on this library.  Thus, the terms and
// conditions of the GNU General Public License cover the whole
// combination.
// 
// As a special exception, the copyright holders of this library give you
// permission to link this library with independent modules to produce an
// executable, regardless of the license terms of these independent
// modules, and to copy and distribute the resulting executable under
// terms of your choice, provided that you also meet, for each linked
// independent module, the terms and conditions of the license of that
// module.  An independent module is a module which is not derived from
// or based on this library.  If you modify this library, you may extend
// this exception to your version of the library, but you are not
// obligated to do so.  If you do not wish to do so, delete this
// exception statement from your version.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;

namespace FontDataXML
{
    public class FontData
    {
        #region " Class Member Declarations "

        // cache for the font data
        // maps fonttypes to DOM tree elements for reading on demand
        private readonly Hashtable _fontElements = new Hashtable();
        private readonly Hashtable _fontNames = new Hashtable();
        private readonly Hashtable _legacyFontData = new Hashtable();
        // maps fonttypes to its parents
        private readonly Hashtable _parents = new Hashtable();
        private readonly Dictionary<string, Hashtable> _unicodeFontData = new Dictionary<string, Hashtable>();

        #endregion

        private readonly XmlDocument DataSource = new XmlDocument();
        private string[] st = new string[] {};

        public FontData()
        {
            InitClass();
        }

        private void InitClass()
        {
            if (_fontNames.Count == 0)
            {
                readXML();
            }
        }

        private void readXML()
        {
            string inherit = "";

            DataSource.Load(@"XML\fontdata.xml");
            XmlNodeList fonts;
            fonts = DataSource.GetElementsByTagName("font");
            if (fonts.Count > 0)
            {
                foreach (XmlNode font in fonts)
                {
                    string fonttype = font.Attributes["type"].Value.ToLower();
                    if (!_fontElements.ContainsKey(fonttype))
                    {
                        if (font.Attributes["inherit"] != null)
                        {
                            inherit = font.Attributes["inherit"].Value.ToLower();
                        }

                        if (inherit != string.Empty)
                        {
                            if (_fontElements.ContainsKey(inherit))
                            {
                                //map font to parent
                                _parents[fonttype] = inherit;
                            }
                        }

                        //map name to element
                        _fontElements[fonttype] = font;
                        bool hidden = font.Attributes["hidden"].Value == "true";
                        if (!hidden)
                        {
                            //add default fonttype to known fontnames
                            _fontNames[fonttype] = fonttype;
                            //add alias names                             
                            XmlNodeList aliases = font.SelectSingleNode("aliases").SelectNodes("alias");
                            foreach (XmlNode aliase in aliases)
                            {
                                _fontNames[aliase.Attributes["name"].Value.ToLower()] = fonttype;
                            }
                        }
                    }
                }
            }
        }

        public ArrayList listFontTypes()
        {
            ArrayList retArray = new ArrayList();

            foreach (DictionaryEntry ret in _fontNames)
            {
                if (!retArray.Contains(ret.Value))
                {
                    retArray.Add(ret.Value);
                }
            }
            retArray.Sort();
            return retArray;
        }

        //return sorted list of all known font names ("Limon S1", "Baidok3c", ...)
        public ArrayList listFontNames()
        {
            ArrayList retArray = new ArrayList();
            foreach (DictionaryEntry ret in _fontNames)
            {
                if (ret.Key != ret.Value)
                    retArray.Add(ret.Key);
            }
            retArray.Sort();
            return retArray;
        }

        //return sorted list of all known font names for a font type
        public ArrayList listFontNamesForType(string fonttype)
        {
            ArrayList retArray = new ArrayList();
            foreach (DictionaryEntry ret in _fontNames.Values)
            {
                if ((ret.Value.ToString() == fonttype) & (ret.Value != ret.Key))
                {
                    retArray.Add(ret.Key);
                }
            }
            retArray.Sort();
            return retArray;
        }

        //return fonttype for fontname
        public string typeForFontname(string fontname)
        {
            string name = fontname.ToLower();
            if (!_fontNames.ContainsKey(name))
            {
                throw new Exception("FontData:typeForFontname:Font: " + name + " is unknown.");
            }
            else
            {
                return _fontNames[name].ToString();
            }
        }

        //return True if fontname is known, else return False
        public bool isConvertable(string fontname)
        {
            try
            {
                typeForFontname(fontname);
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }

        //return default font name according to fontname
        public string defaultFont(string fonttype)
        {
            if (!_fontElements.ContainsKey(fonttype))
            {
                return fonttype;
            }
            else
            {
                XmlElement element = (XmlElement) _fontElements[fonttype];
                string fontname = element.GetAttribute("default");
                if (fontname != "")
                {
                    return fontname;
                }
                else
                {
                    return fonttype;
                }
            }
        }

        public static string Covert2String(string strInput)
        {
            string strRet = string.Empty;
            if (strInput != "")
            {
                if (strInput.IndexOf(";") > 0)
                {
                    string[] strOutput = strInput.Split(';');
                    foreach (string hexStr in strOutput)
                    {
                        strRet += (char) (Convert.ToInt16(hexStr, 16));
                    }
                }
                else
                {
                    if (strInput.Length > 1)
                    {
                        strRet = ((char) (Convert.ToInt16(strInput, 16))).ToString();
                    }
                    else
                    {
                        strRet = strInput;
                    }
                }
            }
            return strRet;
        }

        #region " Class Property Declarations "

        public Hashtable legacyFontData
        {
            get { return _legacyFontData; }
        }

        public Dictionary<string, Hashtable> unicodeFontData
        {
            get { return _unicodeFontData; }
        }

        public Hashtable fontNames
        {
            get { return _fontNames; }
        }

        public Hashtable fontElements
        {
            get { return _fontElements; }
        }

        public Hashtable parents
        {
            get { return _parents; }
        }

        #endregion

        //private static void readUnicodeData(string ft, Hashtable unicodeDicts, Hashtable unicodeTable)
        //{
        //    FontData FontData = new FontDataXML.FontData();
        //    if (FontData.parents[ft] != null)
        //    {
        //        readUnicodeData(FontData.parents[ft].ToString(), unicodeDicts, unicodeTable);
        //    }

        //    XmlDocument DataSource = new XmlDocument();
        //    DataSource.Load("E:\\KhmerConvertor\\WindowsApplication1\\XML\\fontdata.xml");
        //    XmlNodeList fonts;
        //    fonts = DataSource.GetElementsByTagName("font");
        //    if (fonts.Count > 0)
        //    {
        //        foreach (XmlNode font in fonts)
        //        {
        //            if (font.Attributes["type"].Value.ToLower() == ft)
        //            {
        //                //read global uni
        //                foreach (XmlNode map in font.SelectSingleNode("maps").SelectSingleNode("global").SelectNodes("map"))
        //                {
        //                    string uni = map.Attributes["unicode"].Value;
        //                    string leg = Covert2String(map.Attributes["legacy"].Value);
        //                    int l = uni.Length;
        //                    if (l == 1)
        //                    {
        //                        int i = (Int16)uni[0] - Convert.ToInt16("0x1780", 16);
        //                        if ((i >= 0 & i < 127))
        //                        {
        //                            if (!unicodeTable.ContainsKey(i))
        //                            {
        //                                unicodeTable.Add(i, leg);
        //                            }
        //                            else if (unicodeTable[i].ToString() == "")
        //                            {
        //                                unicodeTable[i] = leg;
        //                            }
        //                        }
        //                        else
        //                        {
        //                            unicodeDicts.Add(uni, leg);
        //                        }
        //                    }
        //                    else
        //                    {
        //                        unicodeDicts.Add(uni, leg);
        //                    }
        //                }

        //                if ((font.SelectSingleNode("maps").ChildNodes.Count > 0))
        //                {
        //                    for (int i = 0; i <= font.SelectSingleNode("maps").ChildNodes.Count - 1; i++)
        //                    {
        //                        string ab = font.SelectSingleNode("maps").ChildNodes[i].Name.ToLower();
        //                        if ((ab == "fromunicode"))
        //                        {
        //                            foreach (XmlNode map in font.SelectSingleNode("maps").SelectSingleNode("fromunicode").SelectNodes("map"))
        //                            {
        //                                string uni = map.Attributes["unicode"].Value;
        //                                string leg = Covert2String(map.Attributes["legacy"].Value);
        //                                int l = uni.Length;
        //                                if ((l > 0 & l < 256))
        //                                {
        //                                    unicodeDicts.Add(uni, leg);
        //                                }
        //                            }
        //                            break;
        //                        }
        //                    }
        //                }


        //                //                //If (ft <> "digit") And (ft <> "abc-zwsp") Then
        //                //                //    For Each map As XmlNode In font.SelectSingleNode("maps").SelectSingleNode("fromunicode").SelectNodes("map")
        //                //                //        Dim uni As String = map.Attributes("unicode").Value
        //                //                //        Dim leg As String = Covert2String(map.Attributes("legacy").Value)
        //                //                //        Dim l As Int16 = uni.Length
        //                //                //        If (l > 0 And l < 256) Then
        //                //                //            unicodeDicts.Add(uni, leg)
        //                //                //        End If
        //                //                //    Next
        //                //                //End If
        //            }
        //        }
        //    }
        //}        
    }
}