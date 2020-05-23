using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace XFGetCameraData.Droid.Utility
{
    public static class ImageUtility
    {
        //https://blog.ch3cooh.jp/entry/20111222/1324552051
        //https://blog.shibayan.jp/entry/20140428/1398688687
        public static int GetJpegOrientation(System.IO.Stream stream)
        {
            var exifIdx = FindExifMaker(stream);
            if (exifIdx < 0)
            {
                return -1;
            }

            stream.Seek(exifIdx, SeekOrigin.Begin);

            int n = 0;
            byte[] buf = new byte[2];
            while (true)
            {
                if (n + 2 > stream.Length)
                    break;
                stream.Seek(n, SeekOrigin.Begin);
                stream.Read(buf, 0, 2);
                if (buf[0] == 0x01 && buf[1] == 0x12)
                {
                    n += 2;
                    stream.Seek(n, SeekOrigin.Begin);
                    stream.Read(buf, 0, 2);
                    return buf[0] * 256 + buf[1];
                }
                n++;
                if (n > 2048)
                    break;
            }
            return -1;
        }
        public static int FindExifMaker(System.IO.Stream stream)
        {
            int n = 0;
            byte[] buf = new byte[2];

            while (true)
            {
                if (n + 2 > stream.Length)
                    break;
                stream.Seek(n, SeekOrigin.Begin);
                stream.Read(buf, 0, 2);
                if (buf[0] == 0xFF && buf[1] == 0xE1)
                {
                    return n;
                }
                n++;
                if (n > 2048)
                    break;
            }
            return -1;
        }
    }
}