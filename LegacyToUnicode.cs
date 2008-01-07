// LegacyToUnicode.cs
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
using System.Text;
using System.Xml;
using FontDataXML;

// Legacy to Unicode Convertor

///<summary>
///</summary>
public class LegacyToUnicode
{
    private const int BASE = 1;
    private const int COENG = 8;
    private const int LEFT = 32; // vowel appear on left side of base
    private const int MUUS = 512; // shifter place on specific character
    private const int POSRAA = 256; // can be under PO SraA
    private const int ROBAT = 2048; // is robat character
    private const int SHIFTER = 4; // is shifter (muusekatoan or triisap) characer
    private const int SIGN = 16;
    private const int TRII = 1024; // shifter place on specific character
    private const int VOWEL = 2;
    private const int WITHE = 64; // vowel can be combined with SRA-E
    private const int WITHU = 128; // vowel can be combined with SRA-U

    private readonly int[] KHMERCHAR =
        new int[]
            {
                BASE, BASE, BASE, BASE, BASE, BASE, BASE, BASE, BASE, BASE + MUUS, BASE, BASE, BASE, BASE, BASE,
                BASE + POSRAA, BASE, BASE, BASE, BASE + POSRAA, BASE + MUUS, BASE, BASE, BASE + POSRAA, BASE,
                BASE + POSRAA
                , BASE + POSRAA, BASE + POSRAA, BASE + POSRAA, BASE, BASE, BASE + TRII, BASE, BASE, BASE + TRII, BASE,
                BASE
                , BASE, BASE, BASE, BASE, BASE, BASE, BASE, BASE, BASE, BASE, BASE, BASE, BASE, BASE, BASE, 0, 0,
                VOWEL + WITHE + WITHU, VOWEL + WITHU, VOWEL + WITHE + WITHU, VOWEL + WITHU, VOWEL + WITHU, VOWEL, VOWEL,
                VOWEL, VOWEL + WITHU, VOWEL + WITHE, VOWEL + WITHE, VOWEL + LEFT, VOWEL + LEFT, VOWEL + LEFT, VOWEL,
                VOWEL + WITHE, SIGN + WITHU, SIGN, SIGN, SHIFTER, SHIFTER, SIGN, ROBAT, SIGN, SIGN, SIGN, SIGN + WITHU,
                SIGN, COENG, SIGN
            };

    private readonly char MUUSIKATOAN;
    private readonly char NYO;

    // important character to test in order to form a cluster
    private readonly char PO;
    private readonly char RO;
    private readonly char SAMYOKSANNYA;
    private readonly char SRAAA;
    private readonly char SRAAU;
    private readonly char SRAE;
    private readonly char SRAIE;
    private readonly char SRAII;
    private readonly char SRAOE;
    private readonly char SRAOO;
    private readonly char SRAU;
    private readonly char SRAYA;
    private readonly char TRIISAP;
    private readonly char ZWSP;

    public LegacyToUnicode()
    {
        RO = unichr("0x179A");
        PO = unichr("0x1796");
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
        //S_SA = unichr("0x179F");
        SAMYOKSANNYA = unichr("0x17D0");
        NYO = unichr("0x1789");
        ZWSP = unichr("0x200B");
    }

   
    /// <summary>
    /// possible combination for sra E
    /// </summary>
    /// <param name="chrInput">The input charactor.</param>
    /// <returns></returns>
    private char sraEcombining(char chrInput)
    {
        char retChar = new char();
        if (chrInput == SRAII)
            retChar = SRAOE;
        else if (chrInput == SRAYA)
            retChar = SRAYA;
        else if (chrInput == SRAIE)
            retChar = SRAIE;
        else if (chrInput == SRAAA)
            retChar = SRAOO;
        else if (chrInput == SRAAU)
            retChar = SRAAU;

        return retChar;
    }

    /// <summary>
    /// Determines whether Khmer unicode specified in string.
    /// </summary>
    /// <param name="inString">The input string.</param>
    /// <returns>
    /// 	<c>true</c> if Khmer unicode specified in string; otherwise, <c>false</c>.
    /// </returns>
    private static bool isKhmerUni(string inString)
    {
        bool retBool = false;
        if (!string.IsNullOrEmpty(inString))
        {
            foreach (char c in inString)
            {
                if ((c > 6015 && c < 6138) || (c > 6623 && c < 6656))
                {
                    retBool = true;
                    break;
                }
            }
        }
        return retBool;
    }

    /// <summary>
    /// Check the type of current Khmer charactor.
    /// </summary>
    /// <param name="uniChar">The input charactor.</param>
    /// <returns></returns>
    private int khmerType(char uniChar)
    {
        //todo: check if not unicode charactor

        int ch = (int) uniChar;
        if (ch >= 6016)
        {
            ch -= 6016;
            if (ch < KHMERCHAR.Length)
                return KHMERCHAR[ch];
        }
        return 0;
    }

    /// <summary>
    /// Re-order unicode charactor.
    /// </summary>
    /// <param name="sin">Khmer Unicode string</param>
    /// <returns></returns>
    public string ReOrder(string sin)
    {
        //todo: check if not unicode charactor;

        StringBuilder result = new StringBuilder();
        int sinLimit = sin.Length - 1;
        int i = -1;
        while (i < sinLimit)
        {
            string baseChar = string.Empty;
            string robat = string.Empty;
            string shifter1 = string.Empty;
            string shifter2 = string.Empty;
            string coeng1 = string.Empty;
            string coeng2 = string.Empty;
            string vowel = string.Empty;
            bool poSraA = false;
            string sign = string.Empty;
            string keep = string.Empty;
            string cluster;
            while (i < sinLimit)
            {
                i += 1;
                int sinType = khmerType(sin[i]);

                if ((sinType & BASE) > 0)
                {
                    if (baseChar != string.Empty)
                    {
                        // second baseChar -> end of cluster
                        i -= 1; // continue with the found character
                        break;
                    }
                    baseChar = sin[i].ToString();
                    keep = string.Empty;
                    continue;
                }
                else if ((sinType & ROBAT) > 0)
                {
                    if (robat != string.Empty)
                    {
                        // second robat -> end of cluster
                        i -= 1; // continue with the found character
                        break;
                    }
                    robat = sin[i].ToString();
                    keep = string.Empty;
                    continue;
                }
                else if ((sinType & SHIFTER) > 0)
                {
                    if (shifter1 != string.Empty)
                    {
                        // second shifter -> end of cluster
                        i -= 1; // continue with the found character
                        break;
                    }
                    shifter1 = sin[i].ToString();
                    keep = string.Empty;
                    continue;
                }
                else if ((sinType & SIGN) > 0)
                {
                    if (sign != string.Empty)
                    {
                        // second sign -> end of cluster
                        i -= 1; // continue with the found character
                        break;
                    }
                    sign = sin[i].ToString();
                    keep = string.Empty;
                    continue;
                }
                else if ((sinType & COENG) > 0)
                {
                    if (i == sinLimit)
                    {
                        coeng1 = sin[i].ToString();
                        break;
                    }
                    // if it is coeng RO (and consonent is not blank), it must belong to next cluster
                    // so finish this cluster
                    if ((sin[i + 1] == RO) && (baseChar != string.Empty))
                    {
                        i -= 1;
                        break;
                    }
                    // no coeng yet so dump coeng to coeng1
                    if (coeng1 == string.Empty)
                    {
                        coeng1 = sin.Substring(i, 2);
                        i += 1;
                        keep = string.Empty;
                    } //# coeng1 is coeng RO, the cluster can have two coeng, dump coeng to coeng2
                    else if (coeng1[1] == RO)
                    {
                        coeng2 = sin.Substring(i, 2);
                        i += 1;
                        keep = string.Empty;
                    }
                    else
                    {
                        i -= 1;
                        break;
                    }
                }
                else if ((sinType & VOWEL) > 0)
                {
                    if (vowel == string.Empty)
                    {
                        // if it is sra E ES AI (and consonent is not blank), it must belong to next cluster,
                        // so finish this cluster
                        if ((sinType & LEFT) > 0)
                        {
                            if (baseChar != string.Empty)
                            {
                                i -= 1;
                                break;
                            }
                        }
                        // give vowel a value found in the unorganized cluster
                        vowel = sin[i].ToString();
                        keep = string.Empty;
                    }
                    else if ((baseChar == PO.ToString()) && (!poSraA) &&
                             ((sin[i] == SRAAA) || (vowel == SRAAA.ToString())))
                    {
                        poSraA = true;
                        if (vowel == SRAAA.ToString())
                        {
                            vowel = sin[i].ToString();
                            keep = string.Empty;
                        }
                    }
                    else
                    {
                        // test if sra E is follow by sin which could combine with the following
                        if ((vowel == SRAE.ToString()) && ((sinType & WITHE) > 0))
                        {
                            vowel = sraEcombining(sin[i]).ToString();
                            keep = string.Empty;
                        } // test if vowel can be combine with sin[i] (e.g. sra U and sra I or vice versa)
                        else if ((vowel == SRAU.ToString() && ((sinType & WITHU) > 0)) ||
                                 (((khmerType(vowel[0]) & WITHU) > 0) && sin[i] == SRAU))
                        {
                            // vowel is not Sra I, II, Y, YY, transfer value from sin[i] to vowel
                            if ((khmerType(vowel[0]) & WITHU) == 0)
                            {
                                vowel = sin[i].ToString();
                            }
                            // select shifter1 base on specific consonants
                            if ((baseChar != string.Empty) && ((khmerType(baseChar[0]) & TRII) > 0))
                            {
                                shifter1 = TRIISAP.ToString();
                            }
                            else
                            {
                                shifter1 = MUUSIKATOAN.ToString();
                            }
                            // examine if shifter1 should move shifter2 (base on coeng SA_)                       
                        }
                        else if ((vowel == SRAE.ToString()) && (sin[i] == SRAU))
                        {
                            if ((baseChar != string.Empty) && ((khmerType(baseChar[0]) & TRII) > 0))
                            {
                                shifter1 = TRIISAP.ToString();
                            }
                            else
                            {
                                shifter1 = MUUSIKATOAN.ToString();
                            }
                        }
                        else
                        {
                            // sign can't be combine -> end of cluster
                            i -= 1; // continue with the found character
                            break;
                        }
                    }
                }
                else
                {
                    // other than khmer -> end of cluster
                    // continue with the next character
                    if (sin[i] == ZWSP)
                    {
                        // avoid breaking of cluster if meet zwsp
                        // and move zwsp to end of cluster
                        keep = ZWSP.ToString();
                    }
                    else
                    {
                        keep = sin[i].ToString();
                        break;
                    }
                }
            } // end of while loop


            // Organization of a cluster:

            if ((vowel == SRAU.ToString()) && (sign != string.Empty) && ((khmerType(sign[0]) & WITHU) > 0))
            {
                // samyoksanha + sraU --> MUUS + samyoksanha
                if (sign == SAMYOKSANNYA.ToString())
                {
                    vowel = string.Empty;
                    shifter1 = MUUSIKATOAN.ToString();
                }
            }

            // examine if shifter1 should move shifter2 (base on coeng)
            if (shifter1 != string.Empty && coeng1 != string.Empty)
            {
                if ((khmerType(coeng1[1]) & TRII) > 0)
                {
                    shifter2 = TRIISAP.ToString();
                    shifter1 = string.Empty;
                }
                else if ((khmerType(coeng1[1]) & MUUS) > 0)
                {
                    shifter2 = MUUSIKATOAN.ToString();
                    shifter1 = string.Empty;
                }
            }
            // examine if PO + sraA > NYO, this case can only determin 
            // here since it need all element
            // coeng2 is priority (if coeng2 exist, coeng1 is always coRO)

            string underPoSraA;

            if (coeng2 != string.Empty)
                underPoSraA = coeng2;
            else
                underPoSraA = coeng1;

            if (underPoSraA.Length == 2)
            {
                underPoSraA = (khmerType(underPoSraA[1]) & POSRAA).ToString();
                // test if coeng is allow under PO + SRAA
                if ((poSraA && (underPoSraA == "0") && (vowel != string.Empty)) ||
                    ((baseChar == PO.ToString()) && (vowel == SRAAA.ToString()) && (underPoSraA == "0")))
                {
                    // change baseChar to letter NYO
                    baseChar = NYO.ToString();
                    if ((vowel == SRAAA.ToString()) && (!poSraA))
                    {
                        vowel = string.Empty;
                    }
                }
            }

            // PO + SraA + SraE
            if ((poSraA) && (vowel == SRAE.ToString()))
            {
                // PO + sraA is not NYO and there is leading sraE they should be recombined
                vowel = SRAOO.ToString();
            }
            // Rule of cluster
            // if there are two coeng, ceong1 is always coRO so put it after coeng2

            cluster = baseChar + robat + shifter1 + coeng2 + coeng1 + shifter2 + vowel + sign;
            result = result.Append(cluster + keep); //result + cluster + keep;
        }

        return result.ToString();
    }

    /// <summary>
    /// Read the legacy font data from fontdata.xml.
    /// </summary>
    /// <param name="ft">The font type of legacy font.</param>
    /// <param name="unicodeDicts">The unicode dicts.</param>
    /// <param name="legacyTable">The legacy table.</param>
    private static void readLegacyData(string ft, Hashtable unicodeDicts, ArrayList legacyTable)
    {
        FontData FontData = new FontData();
        if (FontData.parents[ft] != null)
            readLegacyData(FontData.parents[ft].ToString(), unicodeDicts, legacyTable);


        XmlDocument DataSource = new XmlDocument();
        DataSource.Load(@"XML\fontdata.xml");
        XmlNodeList fonts;
        fonts = DataSource.GetElementsByTagName("font");


        for (int i = 0; i < 256; i++)
            legacyTable.Add(((char) i).ToString());

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
                        int l = leg.Length;
                        if (l == 1)
                        {
                            int i = (Int16) leg[0];
                            if ((i >= 0 & i < 256))
                            {
                                if (legacyTable[i].ToString() == ((char) i).ToString())
                                    legacyTable[i] = uni;
                            }
                            else
                            {
                                addToHashtable(leg, uni, unicodeDicts);
                            }
                        }
                        else
                        {
                            addToHashtable(leg, uni, unicodeDicts);
                        }
                    }

                    if ((font.SelectSingleNode("maps").ChildNodes.Count > 0))
                    {
                        for (int i = 0; i <= font.SelectSingleNode("maps").ChildNodes.Count - 1; i++)
                        {
                            string ab = font.SelectSingleNode("maps").ChildNodes[i].Name.ToLower();
                            if ((ab == "tounicode"))
                            {
                                foreach (
                                    XmlNode map in
                                        font.SelectSingleNode("maps").SelectSingleNode("tounicode").SelectNodes("map")
                                    )
                                {
                                    string uni = map.Attributes["unicode"].Value;
                                    string leg = FontData.Covert2String(map.Attributes["legacy"].Value);
                                    int l = leg.Length;
                                    if ((l > 0 & l < 10))
                                    {
                                        addToHashtable(leg, uni, unicodeDicts);
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
    /// Add Key and it Value to hashtable.
    /// </summary>
    /// <param name="strKey">The string key.</param>
    /// <param name="strValue">The string value.</param>
    /// <param name="hashDicts">The hashtable dictionary.</param>
    private static void addToHashtable(string strKey, string strValue, Hashtable hashDicts)
    {
        if (!hashDicts.ContainsKey(strKey))
            hashDicts.Add(strKey, strValue);
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

    /// <summary>
    /// Processes Legacy to unicode convertor.
    /// </summary>
    /// <param name="strInput">The Khmer unicode string</param>
    /// <param name="fontname">The name of source font</param>
    /// <returns></returns>
    public string process(string strInput, string fontname)
    {
        StringBuilder sout = new StringBuilder();

        FontData fd = new FontData();
        if (fd.isConvertable(fontname))
        {
            string fonttype = fd.typeForFontname(fontname);
            Hashtable condenseData;
            string[] replaceData;

            ArrayList tmpReplaceData = new ArrayList();
            Hashtable tmpCondenseData = new Hashtable();
            readLegacyData(fonttype, tmpCondenseData, tmpReplaceData);

            condenseData = tmpCondenseData;
            replaceData = new string[tmpReplaceData.Count];
            tmpReplaceData.CopyTo(replaceData);


            int listLength = replaceData.Length;
            int i = 0;
            int end = strInput.Length;

            while (i < end)
            {
                foreach (string key in condenseData.Keys)
                {
                    string testString;
                    if (i + key.Length > strInput.Length)
                        testString = strInput;
                    else
                        testString = strInput.Substring(i, key.Length);

                    if (key == testString)
                    {
                        sout.Append(condenseData[key].ToString());
                        i += key.Length;
                        break;
                    }
                }
                if (i < end)
                {
                    int n = (strInput[i]);
                    if (n < listLength)
                        sout.Append(replaceData[n]);
                    else
                        sout.Append(((char) (n)).ToString());

                    i += 1;
                }
            }
        }
        return ReOrder(sout.ToString());
    }
}