// UnicodeToLegacy.cs
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
using System.Text;
using System.Xml;
using FontDataXML;

// Unicode to Legacy Convertor

public class UnicodeToLegacy
{
    private const int _c1 = CC_CONSONANT + CF_CONSONANT;
    private const int _c2 = CC_CONSONANT2 + CF_CONSONANT;
    private const int _c3 = CC_CONSONANT3 + CF_CONSONANT;
    private const int _co = CC_COENG + CF_COENG + CF_DOTTED_CIRCLE;
    private const int _cs = CC_CONSONANT_SHIFTER + CF_DOTTED_CIRCLE + CF_SHIFTER;
    private const int _da = CC_DEPENDENT_VOWEL + CF_POS_ABOVE + CF_DOTTED_CIRCLE + CF_ABOVE_VOWEL;
    private const int _db = CC_DEPENDENT_VOWEL + CF_POS_BELOW + CF_DOTTED_CIRCLE;
    private const int _dl = CC_DEPENDENT_VOWEL + CF_POS_BEFORE + CF_DOTTED_CIRCLE;
    private const int _dr = CC_DEPENDENT_VOWEL + CF_POS_AFTER + CF_DOTTED_CIRCLE;
    private const int _rb = CC_ROBAT + CF_POS_ABOVE + CF_DOTTED_CIRCLE;
    private const int _sa = CC_SIGN_ABOVE + CF_DOTTED_CIRCLE + CF_POS_ABOVE;
    private const int _sp = CC_SIGN_AFTER + CF_DOTTED_CIRCLE + CF_POS_AFTER;
    private const int _va = _da + CF_SPLIT_VOWEL;
    private const int _vr = _dr + CF_SPLIT_VOWEL;
    private const int _xx = 0;
    private const int CC_COENG = 7; // Subscript consonant combining character
    private const int CC_CONSONANT = 1; // Consonant of type 1 or independent vowel
    private const int CC_CONSONANT_SHIFTER = 5;
    private const int CC_CONSONANT2 = 2; // Consonant of type 2
    private const int CC_CONSONANT3 = 3; // Consonant of type 3
    private const int CC_COUNT = 12;
    private const int CC_DEPENDENT_VOWEL = 8;
    private const int CC_RESERVED = 0;
    private const int CC_ROBAT = 6; // Khmer special diacritic accent -treated differently in state table
    private const int CC_SIGN_ABOVE = 9;
    private const int CC_SIGN_AFTER = 10;
    private const int CC_ZERO_WIDTH_J_MARK = 11; // Zero width joiner character
    private const int CC_ZERO_WIDTH_NJ_MARK = 4; // Zero Width non joiner character (0x200C)
    private const int CF_ABOVE_VOWEL = 536870912; // flag to speed up comparing

    private const int CF_CLASS_MASK = 65535;
    private const int CF_COENG = 134217728; // flag to speed up comparing

    private const int CF_CONSONANT = 16777216; // flag to speed up comparing

    private const int CF_DOTTED_CIRCLE = 67108864;
    // add a dotted circle if a character with this flag is the first in a

    // syllable
    private const int CF_POS_ABOVE = 131072;
    private const int CF_POS_AFTER = 65536;
    private const int CF_POS_BEFORE = 524288;
    private const int CF_POS_BELOW = 262144;
    private const int CF_POS_MASK = 983040;
    private const int CF_SHIFTER = 268435456; // flag to speed up comparing

    private const int CF_SPLIT_VOWEL = 33554432;
    // flag for a split vowel -> the first part is added in front of the syllable

    private readonly char BA;
    private readonly char COENG;
    private readonly string CONYO;
    private readonly string CORO;

    // simple classes, they are used in the state table (in this file) to control the length of a syllable
    // they are also used to know where a character should be placed (location in reference to the base character)
    // and also to know if a character, when independently displayed, should be displayed with a dotted-circle to
    // indicate error in syllable construction

    // Character class tables
    // _xx character does not combine into syllable, such as numbers, puntuation marks, non-Khmer signs...
    // _sa Sign placed above the base
    // _sp Sign placed after the base
    // _c1 Consonant of type 1 or independent vowel (independent vowels behave as type 1 consonants)
    // _c2 Consonant of type 2 (only RO)
    // _c3 Consonant of type 3
    // _rb Khmer sign robat u17CC. combining mark for subscript consonants
    // _cd Consonant-shifter
    // _dl Dependent vowel placed before the base (left of the base)
    // _db Dependent vowel placed below the base
    // _da Dependent vowel placed above the base
    // _dr Dependent vowel placed behind the base (right of the base)
    // _co Khmer combining mark COENG u17D2, combines with the consonant or independent vowel following
    //     it to create a subscript consonant or independent vowel
    // _va Khmer split vowel in wich the first part is before the base and the second one above the base
    // _vr Khmer split vowel in wich the first part is before the base and the second one behind (right of) the base

    private readonly int[] khmerCharClasses =
        new int[]
            {
                _c1, _c1, _c1, _c3, _c1, _c1, _c1, _c1, _c3
                , _c1, _c1, _c1, _c1, _c3, _c1, _c1, _c1,
                _c1, _c1, _c1, _c3, _c1, _c1, _c1, _c1,
                _c3, _c2, _c1, _c1, _c1, _c3, _c3, _c1,
                _c3, _c1, _c1, _c1, _c1, _c1, _c1, _c1,
                _c1, _c1, _c1, _c1, _c1, _c1, _c1, _c1,
                _c1, _c1, _c1, _dr, _dr, _dr, _da, _da,
                _da, _da, _db, _db, _db, _va, _vr, _vr,
                _dl, _dl, _dl, _vr, _vr, _sa, _sp, _sp,
                _cs, _cs, _sa, _rb, _sa, _sa, _sa, _sa, _sa, _co, _sa, _xx, _xx, _xx, _xx, _xx, _xx, _xx,
                _xx, _xx, _sa, _xx, _xx
            };

    private readonly short[,] khmerStateTable = new short[,]
        {
            {1, 2, 2, 2, 1, 1, 1, 6, 1, 1, 1, 2}, {- 1, - 1, - 1, - 1, - 1, - 1, - 1, - 1, - 1, - 1, - 1, - 1},
            {- 1, - 1, - 1, - 1, 3, 4, 5, 6, 16, 17, 1, - 1}, {- 1, - 1, - 1, - 1, - 1, 4, - 1, - 1, 16, - 1, - 1, - 1},
            {- 1, - 1, - 1, - 1, 15, - 1, - 1, 6, 16, 17, 1, 14},
            {- 1, - 1, - 1, - 1, - 1, - 1, - 1, - 1, 20, - 1, 1, - 1},
            {- 1, 7, 8, 9, - 1, - 1, - 1, - 1, - 1, - 1, - 1, - 1}, {- 1, - 1, - 1, - 1, 12, 13, - 1, 10, 16, 17, 1, 14}
            ,
            {- 1, - 1, - 1, - 1, 12, 13, - 1, - 1, 16, 17, 1, 14}, {- 1, - 1, - 1, - 1, 12, 13, - 1, 10, 16, 17, 1, 14},
            {- 1, 11, 11, 11, - 1, - 1, - 1, - 1, - 1, - 1, - 1, - 1},
            {- 1, - 1, - 1, - 1, 15, - 1, - 1, - 1, 16, 17, 1, 14},
            {- 1, - 1, - 1, - 1, -1, 13, - 1, - 1, 16, - 1, - 1, - 1},
            {- 1, - 1, - 1, - 1, 15, - 1, - 1, - 1, 16, 17, 1, 14},
            {- 1, - 1, - 1, - 1, - 1, - 1, - 1, - 1, 16, - 1, - 1, - 1},
            {- 1, - 1, - 1, - 1, - 1, - 1, - 1, - 1, 16, - 1, - 1, - 1},
            {- 1, - 1, - 1, - 1, - 1, - 1, - 1, - 1, - 1, 17, 1, 18},
            {- 1, - 1, - 1, - 1, - 1, - 1, - 1, - 1, - 1, - 1, 1, 18},
            {- 1, - 1, - 1, - 1, - 1, - 1, - 1, 19, - 1, - 1, - 1, - 1},
            {- 1, 1, - 1, 1, - 1, - 1, - 1, - 1, - 1, - 1, - 1, - 1},
            {- 1, - 1, - 1, - 1, - 1, - 1, - 1, - 1, - 1, - 1, 1, - 1}
        };

    private readonly char LA;
    private readonly char MARK;
    private readonly char MUUSIKATOAN;
    private readonly char NYO;
    private readonly char SA;
    private readonly char SAMYOKSANNYA;
    private readonly char SRAAA;
    private readonly char SRAAU;
    private readonly char SRAE;
    private readonly char SRAIE;
    private readonly char SRAII;
    private readonly char SRAOE;
    private readonly char SRAOM;
    private readonly char SRAOO;
    private readonly char SRAU;
    private readonly char SRAYA;
    private readonly char TRIISAP;
    private readonly char YO;


    public UnicodeToLegacy()
    {
        SRAAA = unichr("0x17B6");
        SRAE = unichr("0x17C1");
        SRAOE = unichr("0x17BE");
        SRAOO = unichr("0x17C4");
        SRAYA = unichr("0x17BF");
        SRAIE = unichr("0x17C0");
        SRAAU = unichr("0x17C5");
        SRAII = unichr("0x17B8");
        SRAU = unichr("0x17BB");
        TRIISAP = unichr("0x17CA");
        MUUSIKATOAN = unichr("0x17C9");
        SAMYOKSANNYA = unichr("0x17D0");
        LA = unichr("0x17A1");
        NYO = unichr("0x1789");
        BA = unichr("0x1794");
        YO = unichr("0x1799");
        SA = unichr("0x179F");
        COENG = unichr("0x17D2");
        CORO = string.Concat(unichr("0x17D2"), unichr("0x179A"));
        CONYO = string.Concat(unichr("0x17D2"), unichr("0x1789"));
        SRAOM = unichr("0x17C6");
        MARK = unichr("0x17EA");
    }


    private char strEcombining(char chrInput)
    {
        char retChar = new char();
        if (chrInput == SRAOE)
            retChar = SRAII;
        else if (chrInput == SRAYA)
            retChar = SRAYA;
        else if (chrInput == SRAIE)
            retChar = SRAIE;
        else if (chrInput == SRAOO)
            retChar = SRAAA;
        else if (chrInput == SRAAU)
            retChar = SRAAU;

        return retChar;
    }


    /// <summary>
    /// Gets the charactor class.
    /// </summary>
    /// <param name="uniChar">input charactor</param>
    /// <returns></returns>
    private Int32 getCharClass(char uniChar)
    {
        Int32 retValue = 0;
        Int32 ch;
        ch = uniChar;
        if (ch > 255)
        {
            if (ch >= Convert.ToInt32("0x1780", 16))
            {
                ch -= Convert.ToInt32("0x1780", 16);
                if (ch < khmerCharClasses.Length)
                    retValue = khmerCharClasses[ch];
            }
        }
        return retValue;
    }

    /// <summary>
    /// Re-order unicode string.
    /// </summary>
    /// <param name="sin">The unicode string.</param>
    /// <returns></returns>
    public string ReOrder(string sin)
    {
        //Given an input string of unicode cluster to reorder.
        //The return is the visual based cluster (legacy style) string.

        int cursor = 0;
        short state = 0;
        int charCount = sin.Length;
        StringBuilder result = new StringBuilder();

        while (cursor < charCount)
        {
            string _reserved = string.Empty;
            string _signAbove = string.Empty;
            string _signAfter = string.Empty;
            string _base = string.Empty;
            string _robat = string.Empty;
            string _shifter = string.Empty;
            string _vowelBefore = string.Empty;
            string _vowelBelow = string.Empty;
            string _vowelAbove = string.Empty;
            string _vowelAfter = string.Empty;
            bool _coeng = false;
            string _cluster;

            string _coeng1 = string.Empty;
            string _coeng2 = string.Empty;

            bool _shifterAfterCoeng = false;

            while (cursor < charCount)
            {
                char curChar = sin[cursor];
                int kChar = getCharClass(curChar);
                int charClass = kChar & CF_CLASS_MASK;
                try
                {
                    state = khmerStateTable[state, charClass];
                }
                catch (Exception)
                {
                    state = -1;
                }


                if (state < 0)
                    break;


                //collect variable for cluster here

                if (kChar == _xx)
                    _reserved = curChar.ToString();
                else if (kChar == _sa) //Sign placed above the base                
                    _signAbove = curChar.ToString();
                else if (kChar == _sp) //Sign placed after the base                
                    _signAfter = curChar.ToString();
                else if ((kChar == _c1) || (kChar == _c2) || (kChar == _c3)) //Consonant                
                    if (_coeng)
                    {
                        if (_coeng1 == string.Empty)
                            _coeng1 = string.Concat(COENG, curChar);
                        else
                            _coeng2 = string.Concat(COENG, curChar);
                        _coeng = false;
                    }
                    else
                        _base = curChar.ToString();
                else if (kChar == _rb) //Khmer sign robat u17CC                
                    _robat = curChar.ToString();
                else if (kChar == _cs) //Consonant-shifter
                {
                    if (_coeng1 != string.Empty)
                        _shifterAfterCoeng = true;

                    _shifter = curChar.ToString();
                }
                else if (kChar == _dl) //Dependent vowel placed before the base                
                    _vowelBefore = curChar.ToString();
                else if (kChar == _db) //Dependent vowel placed below the base                
                    _vowelBelow = curChar.ToString();
                else if (kChar == _da) //Dependent vowel placed above the base                
                    _vowelAbove = curChar.ToString();
                else if (kChar == _dr) //Dependent vowel placed behind the base                
                    _vowelAfter = curChar.ToString();
                else if (kChar == _co) //Khmer combining mark COENG                
                    _coeng = true;
                else if (kChar == _va) //Khmer split vowel, see _da
                {
                    _vowelBefore = SRAE.ToString();
                    _vowelAbove = strEcombining(curChar).ToString();
                }
                else if (kChar == _vr) //Khmer split vowel, see _dr
                {
                    _vowelBefore = SRAE.ToString();
                    _vowelAfter = strEcombining(curChar).ToString();
                }

                cursor += 1;
            }
            // end of while (a cluster has found)

            // logic of vowel
            // determine if right side vowel should be marked

            if ((_coeng1 != string.Empty) && (_vowelBelow != string.Empty))
                _vowelBelow = MARK + _vowelBelow;
            else if ((_base == LA.ToString() || _base == NYO.ToString()) && (_vowelBelow != string.Empty))
                _vowelBelow = MARK + _vowelBelow;
            else if ((_coeng1 != string.Empty) && (_vowelBefore != string.Empty) && (_vowelAfter != string.Empty))
                _vowelAfter = MARK + _vowelAfter;


            // logic when cluster has coeng
            // should coeng be located on left side
            string _coengBefore = string.Empty;
            if (_coeng1 == CORO)
            {
                _coengBefore = _coeng1;
                _coeng1 = string.Empty;
            }
            else if (_coeng2 == CORO)
            {
                _coengBefore = MARK + _coeng2;
                _coeng2 = string.Empty;
            }

            if ((_coeng1 != string.Empty) || (_coeng2 != string.Empty))
            {
                // NYO must change to other form when there is coeng
                if (_base == NYO.ToString())
                {
                    _base = MARK + _base;
                    // coeng NYO must be marked
                    if (_coeng1 == CONYO)
                        _coeng1 = MARK + _coeng1;
                }

                //Move for testing otherwise move it back to end of endif
                if ((_coeng1 != string.Empty) && (_coeng2 != string.Empty))
                    _coeng2 = MARK + _coeng2;
            }

            //logic of shifter with base character
            if ((_base != string.Empty) && (_shifter != string.Empty))
            {
                //special case apply to BA only
                if ((_vowelAbove != string.Empty) && (_base == BA.ToString()) && (_shifter == TRIISAP.ToString()))
                    _vowelAbove = MARK + _vowelAbove;
                else if (_vowelAbove != string.Empty)
                    _shifter = MARK + _shifter;
                else if ((_signAbove == SAMYOKSANNYA.ToString()) && (_shifter == MUUSIKATOAN.ToString()))
                    _shifter = MARK + _shifter;
                else if ((_signAbove != string.Empty) && (_vowelAfter != string.Empty))
                    _shifter = MARK + _shifter;
                else if (_signAbove != string.Empty)
                    _signAbove = MARK + _signAbove;

                //add another mark to shifter
                if ((_coeng1 != string.Empty) && ((_vowelAbove != string.Empty) || (_signAbove != string.Empty)))
                    _shifter = MARK + _shifter;


                if ((_base == LA.ToString()) || (_base == NYO.ToString()))
                    _shifter = MARK + _shifter;
            }

            // uncomplete coeng
            if (_coeng && (_coeng1 == string.Empty))
                _coeng1 = COENG.ToString();
            else if (_coeng && (_coeng2 == string.Empty))
                _coeng2 = string.Concat(MARK, COENG);


            //render DOTCIRCLE for standalone sign or vowel
            if ((_base == string.Empty) &&
                ((_vowelBefore != string.Empty) || (_coengBefore != string.Empty) || (_robat != string.Empty) ||
                 (_shifter != string.Empty) || (_coeng1 != string.Empty) ||
                 (_coeng2 != string.Empty) || (_vowelAfter != string.Empty) || (_vowelBelow != string.Empty) ||
                 (_vowelAbove != string.Empty) ||
                 (_signAbove != string.Empty) || (_signAfter != string.Empty)))
            {
                //_base = string.Empty; //DOTCIRCLE
            }

            //place of shifter
            string _shifter1 = string.Empty;
            string _shifter2 = string.Empty;

            if (_shifterAfterCoeng)
                _shifter2 = _shifter;
            else
                _shifter1 = _shifter;


            bool _specialCaseBA = false;

            if ((_base == BA.ToString()) &&
                ((_vowelAfter == SRAAA.ToString()) || (_vowelAfter == SRAAU.ToString()) ||
                 (_vowelAfter == string.Concat(MARK, SRAAA)) || (_vowelAfter == string.Concat(MARK, SRAAU))))
            {
                // SRAAA or SRAAU will get a MARK if there is coeng, redefine to last char
                _vowelAfter = _vowelAfter.Substring(_vowelAfter.Length - 1);
                _specialCaseBA = true;


                if ((_coeng1 != string.Empty) &&
                    ((_coeng1.Substring(_coeng1.Length - 1) == BA.ToString()) ||
                     (_coeng1.Substring(_coeng1.Length - 1) == YO.ToString()) ||
                     (_coeng1.Substring(_coeng1.Length - 1) == SA.ToString())))
                {
                    _specialCaseBA = false;
                }
            }

            // cluster formation
            if (_specialCaseBA)
            {
                _cluster = _vowelBefore + _coengBefore + _base + _vowelAfter + _robat + _shifter1 + _coeng1 + _coeng2 +
                           _shifter2 + _vowelBelow + _vowelAbove + _signAbove + _signAfter;
            }
            else
            {
                _cluster = _vowelBefore + _coengBefore + _base + _robat + _shifter1 + _coeng1 + _coeng2 + _shifter2 +
                           _vowelBelow + _vowelAbove + _vowelAfter + _signAbove + _signAfter;
            }

            result.Append(_cluster + _reserved);
            state = 0;
            //end of while
        }

        return result.ToString();
    }

    /// <summary>
    /// Convert Hexadecimal string to unicode charactor.
    /// </summary>
    /// <param name="strInput">The input hexadecimal string.</param>
    /// <returns></returns>
    private static char unichr(string strInput)
    {
        return (char) (Convert.ToInt16(strInput, 16));
    }

    private static void addToGeneric(string strKey, string strValue, IDictionary<int, Hashtable> unicodeDicts)
    {
        if (!unicodeDicts.ContainsKey(strKey.Length - 1))
        {
            Hashtable tmpNew = new Hashtable();
            tmpNew.Add(strKey.ToLower(), strValue);
            unicodeDicts.Add(strKey.Length - 1, tmpNew);
        }
        else
        {
            Hashtable tmp = unicodeDicts[strKey.Length - 1];
            if (!tmp.ContainsKey(strKey.ToLower()))
            {
                tmp.Add(strKey.ToLower(), strValue);
                unicodeDicts[strKey.Length - 1] = tmp;
            }
        }
    }

    /// <summary>
    /// Read the unicode font data from fontdata.xml.
    /// </summary>
    /// <param name="ft">The fonttype of the output string.</param>
    /// <param name="unicodeDicts">The unicode dicts.</param>
    /// <param name="unicodeTable">The unicode table.</param>
    private static void readUnicodeData(string ft, Dictionary<int, Hashtable> unicodeDicts, Hashtable unicodeTable)
    {
        FontData FontData = new FontData();
        if (FontData.parents[ft] != null)
            readUnicodeData(FontData.parents[ft].ToString(), unicodeDicts, unicodeTable);


        XmlDocument DataSource = new XmlDocument();
        DataSource.Load(@"XML\fontdata.xml");
        XmlNodeList fonts;
        fonts = DataSource.GetElementsByTagName("font");

        if (unicodeTable.Count < 126)
        {
            for (int i = 0; i < 127; i++)
                unicodeTable.Add(i, string.Empty);
        }

        if (fonts.Count > 0)
        {
            foreach (XmlNode font in fonts)
            {
                if (font.Attributes["type"].Value.ToLower() == ft)
                {
                    foreach (XmlNode map in font.SelectSingleNode("maps").SelectSingleNode("global").SelectNodes("map"))
                    {
                        string uni = map.Attributes["unicode"].Value;
                        string leg = FontData.Covert2String(map.Attributes["legacy"].Value);
                        int l = uni.Length;
                        if (l == 1)
                        {
                            int i = (Int16) uni[0] - Convert.ToInt16("0x1780", 16);
                            if ((i >= 0 & i < 127))
                            {
                                if (unicodeTable[i].ToString() == string.Empty)
                                    unicodeTable[i] = leg;
                            }
                            else
                            {
                                addToGeneric(uni, leg, unicodeDicts);
                            }
                        }
                        else
                        {
                            addToGeneric(uni, leg, unicodeDicts);
                        }
                    }

                    if ((font.SelectSingleNode("maps").ChildNodes.Count > 0))
                    {
                        for (int i = 0; i <= font.SelectSingleNode("maps").ChildNodes.Count - 1; i++)
                        {
                            string ab = font.SelectSingleNode("maps").ChildNodes[i].Name.ToLower();
                            if ((ab == "fromunicode"))
                            {
                                foreach (
                                    XmlNode map in
                                        font.SelectSingleNode("maps").SelectSingleNode("fromunicode").SelectNodes("map")
                                    )
                                {
                                    string uni = map.Attributes["unicode"].Value;
                                    string leg = FontData.Covert2String(map.Attributes["legacy"].Value);
                                    int l = uni.Length;
                                    if ((l > 0 & l < 256))
                                    {
                                        addToGeneric(uni, leg, unicodeDicts);
                                    }
                                }
                                break;
                            }
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Processes the convertor.
    /// </summary>
    /// <param name="strInput">Khmer Unicode string.</param>
    /// <param name="fontname">Target legacy fontname.</param>
    /// <returns></returns>
    public string process(string strInput, string fontname)
    {
        StringBuilder sout = new StringBuilder();
        FontData fd = new FontData();

        if (fd.isConvertable(fontname))
        {
            string fonttype = fd.typeForFontname(fontname);
            Dictionary<int, Hashtable> dicts = new Dictionary<int, Hashtable>();
            Hashtable replaceData = new Hashtable();
            readUnicodeData(fonttype, dicts, replaceData);
            int listLength = replaceData.Count;
            int i = 0;
            strInput = ReOrder(strInput);
            int end = strInput.Length;
            while (i < end)
            {
                //for (int j = dicts.Count - 1; j > -1; j--)
                int j = dicts.Count - 1;
                bool valide = true;
                while ((j > -1) && (valide))
                {
                    if (j == 0)
                    {
                        if (dicts.ContainsKey(0) && dicts[0].Count > 0)
                        {
                            if (i + 1 <= strInput.Length)
                            {
                                if (dicts[j].ContainsKey(strInput.Substring(i, 1)))
                                {
                                    sout.Append(dicts[0][strInput.Substring(i, 1)].ToString());
                                    i += 1;
                                    valide = false;
                                }
                                else
                                {
                                    char c = strInput.Substring(i, 1)[0];
                                    int n = (int) c - 6016;
                                    if ((n >= 0) && (n < listLength))
                                        sout.Append(replaceData[n].ToString());
                                    else if ((int) c < 127)
                                        sout.Append(c);

                                    i += 1;
                                    valide = false;
                                }
                            }
                        }
                    }
                    else
                    {
                        if (dicts.ContainsKey(j) && dicts[j].Count > 0 &&
                            (i + j + 1 <= strInput.Length && dicts[j].ContainsKey(strInput.Substring(i, j + 1))))
                        {
                            sout.Append(dicts[j][strInput.Substring(i, j + 1)].ToString());
                            i += j + 1;
                            valide = false;
                        }
                    }
                    j--;
                }
            }
        }
        return sout.ToString();
    }
}