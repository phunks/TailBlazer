
// 文字コードを判別する
// https://dobon.net/vb/dotnet/string/detectcode.html

namespace Multibyte.Text.Encoding
{
    /// <summary>
    /// 文字コードに関するクラス
    /// </summary>
    public static class MultibyteCharCode
    {
        /// <summary>
        /// 文字コードを判別する
        /// </summary>
        /// <remarks>
        /// Jcode.pmのgetcodeメソッドを移植したものです。
        /// Jcode.pm(http://openlab.ring.gr.jp/Jcode/index-j.html)
        /// Jcode.pmの著作権情報
        /// Copyright 1999-2005 Dan Kogai <dankogai@dan.co.jp>
        /// This library is free software; you can redistribute it and/or modify it
        ///  under the same terms as Perl itself.
        /// </remarks>
        /// <param name="bytes">文字コードを調べるデータ</param>
        /// <returns>適当と思われるEncodingオブジェクト。
        /// 判断できなかった時はnull。</returns>
        public static System.Text.Encoding DetectMultibyte(byte[] bytes)
        {
            const byte bEscape = 0x1B;
            const byte bAt = 0x40;
            const byte bDollar = 0x24;
            const byte bAnd = 0x26;
            const byte bOpen = 0x28;    //'('
            const byte bB = 0x42;
            const byte bD = 0x44;
            const byte bJ = 0x4A;
            const byte bI = 0x49;

            int len = bytes.Length;
            byte b1, b2, b3, b4;

            //Encode::is_utf8 は無視

            bool isBinary = false;
            //for (int i = 0; i < len; i++)
            //{
            //    b1 = bytes[i];
            //    if (b1 <= 0x06 || b1 == 0x7F || b1 == 0xFF)
            //    {
            //        //'binary'
            //        isBinary = true;
            //        if (b1 == 0x00 && i < len - 1 && bytes[i + 1] <= 0x7F)
            //        {
            //            //smells like raw unicode
            //            //unicodeは
            //            // UTF-16,unicodeFFFE,UTF-16LE,UTF-16BE
            //            return System.Text.Encoding.Unicode;
            //        }
            //    }
            //}
            if (DetectBOM(bytes, 0xff, 0xfe) && DetectEncoding(bytes, System.Text.Encoding.GetEncoding("UTF-16")))
            {
                return System.Text.Encoding.GetEncoding("UTF-16");
            }
            else if (DetectBOM(bytes, 0xfe, 0xff) && DetectEncoding(bytes, System.Text.Encoding.GetEncoding("unicodeFFFE")))
            {
                return System.Text.Encoding.GetEncoding("unicodeFFFE");
            }
            else if (bytes.Length >= 2 && bytes.Length % 2 == 0)
            {
                if (bytes[0] == 0x00)
                {
                    return System.Text.Encoding.GetEncoding("UTF-16BE");
                }
                else if (bytes[1] == 0x00)
                {
                    return System.Text.Encoding.GetEncoding("UTF-16LE");
                }
            }
            if (isBinary)
            {
                return null;
            }

            //not Japanese
            bool notJapanese = true;
            for (int i = 0; i < len; i++)
            {
                b1 = bytes[i];
                if (b1 == bEscape || 0x80 <= b1)
                {
                    notJapanese = false;
                    break;
                }
            }
            if (notJapanese)
            {
                return System.Text.Encoding.ASCII;
            }

            for (int i = 0; i < len - 2; i++)
            {
                b1 = bytes[i];
                b2 = bytes[i + 1];
                b3 = bytes[i + 2];

                if (b1 == bEscape)
                {
                    if (b2 == bDollar && b3 == bAt)
                    {
                        //JIS_0208 1978
                        //JIS
                        return System.Text.Encoding.GetEncoding(50220);
                    }
                    else if (b2 == bDollar && b3 == bB)
                    {
                        //JIS_0208 1983
                        //JIS
                        return System.Text.Encoding.GetEncoding(50220);
                    }
                    else if (b2 == bOpen && (b3 == bB || b3 == bJ))
                    {
                        //JIS_ASC
                        //JIS
                        return System.Text.Encoding.GetEncoding(50220);
                    }
                    else if (b2 == bOpen && b3 == bI)
                    {
                        //JIS_KANA
                        //JIS
                        return System.Text.Encoding.GetEncoding(50220);
                    }
                    if (i < len - 3)
                    {
                        b4 = bytes[i + 3];
                        if (b2 == bDollar && b3 == bOpen && b4 == bD)
                        {
                            //JIS_0212
                            //JIS
                            return System.Text.Encoding.GetEncoding(50220);
                        }
                        if (i < len - 5 &&
                            b2 == bAnd && b3 == bAt && b4 == bEscape &&
                            bytes[i + 4] == bDollar && bytes[i + 5] == bB)
                        {
                            //JIS_0208 1990
                            //JIS
                            return System.Text.Encoding.GetEncoding(50220);
                        }
                    }
                }
            }

            //should be euc|sjis|utf8
            //use of (?:) by Hiroki Ohzaki <ohzaki@iod.ricoh.co.jp>
            int sjis = 0;
            int euc = 0;
            int utf8 = 0;
            for (int i = 0; i < len - 1; i++)
            {
                b1 = bytes[i];
                b2 = bytes[i + 1];
                if (((0x81 <= b1 && b1 <= 0x9F) || (0xE0 <= b1 && b1 <= 0xFC)) &&
                    ((0x40 <= b2 && b2 <= 0x7E) || (0x80 <= b2 && b2 <= 0xFC)))
                {
                    //SJIS_C
                    sjis += 2;
                    i++;
                }
            }
            for (int i = 0; i < len - 1; i++)
            {
                b1 = bytes[i];
                b2 = bytes[i + 1];
                if (((0xA1 <= b1 && b1 <= 0xFE) && (0xA1 <= b2 && b2 <= 0xFE)) ||
                    (b1 == 0x8E && (0xA1 <= b2 && b2 <= 0xDF)))
                {
                    //EUC_C
                    //EUC_KANA
                    euc += 2;
                    i++;
                }
                else if (i < len - 2)
                {
                    b3 = bytes[i + 2];
                    if (b1 == 0x8F && (0xA1 <= b2 && b2 <= 0xFE) &&
                        (0xA1 <= b3 && b3 <= 0xFE))
                    {
                        //EUC_0212
                        euc += 3;
                        i += 2;
                    }
                }
            }
            for (int i = 0; i < len - 1; i++)
            {
                b1 = bytes[i];
                b2 = bytes[i + 1];
                if ((0xC0 <= b1 && b1 <= 0xDF) && (0x80 <= b2 && b2 <= 0xBF))
                {
                    //UTF8
                    utf8 += 2;
                    i++;
                }
                else if (i < len - 2)
                {
                    b3 = bytes[i + 2];
                    if ((0xE0 <= b1 && b1 <= 0xEF) && (0x80 <= b2 && b2 <= 0xBF) &&
                        (0x80 <= b3 && b3 <= 0xBF))
                    {
                        //UTF8
                        utf8 += 3;
                        i += 2;
                    }
                }
            }
            //M. Takahashi's suggestion
            //utf8 += utf8 / 2;

            System.Diagnostics.Debug.WriteLine(
                string.Format("sjis = {0}, euc = {1}, utf8 = {2}", sjis, euc, utf8));
            if (euc > sjis && euc > utf8)
            {
                //EUC
                return System.Text.Encoding.GetEncoding(51932);
            }
            else if (sjis > euc && sjis > utf8)
            {
                //SJIS
                return System.Text.Encoding.GetEncoding(932);
            }
            else if (utf8 > euc && utf8 > sjis)
            {
                //UTF8
                return System.Text.Encoding.UTF8;
            }

            return null;
        }

        /// <summary>
        /// BOM から文字コードを判定します。
        /// </summary>
        /// <param name="srcBytes">文字コードを判別するバイト配列</param>
        /// <param name="bom">バイトオーダーマーク</param>
        /// <returns><paramref name="srcBytes"/> から文字コードが判定できた場合は true</returns>
        private static bool DetectBOM(byte[] srcBytes, params byte[] bom)
        {
            if (srcBytes.Length < bom.Length)
            {
                return false;
            }

            for (int i = 0; i < bom.Length; i++)
            {
                if (srcBytes[i] != bom[i])
                {
                    return false;
                }
            }

            return true;
        }
        /// <summary>
        /// バイト配列から文字列、文字列からバイト配列に変換し、文字コードを判別します。
        /// </summary>
        /// <param name="srcBytes">文字コードを判別するバイト配列</param>
        /// <param name="encoding">変換する文字コード</param>
        /// <returns><paramref name="srcBytes"/> から文字コードが判定できた場合は true</returns>
        private static bool DetectEncoding(byte[] srcBytes, System.Text.Encoding encoding)
        {
            string encodedStr = encoding.GetString(srcBytes);
            byte[] encodedByte = encoding.GetBytes(encodedStr);

            if (srcBytes.Length != encodedByte.Length)
            {
                return false;
            }

            for (int i = 0; i < srcBytes.Length; i++)
            {
                if (!srcBytes[i].Equals(encodedByte[i]))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
