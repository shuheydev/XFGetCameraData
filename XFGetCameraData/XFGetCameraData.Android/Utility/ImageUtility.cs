using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using static Android.Graphics.Bitmap;

namespace XFGetCameraData.Droid.Utility
{
    public static class ImageUtility
    {
        //https://blog.ch3cooh.jp/entry/20111222/1324552051
        //https://blog.shibayan.jp/entry/20140428/1398688687
        /// <summary>
        /// Jpegの向きを取得する
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
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
        /// <summary>
        /// Exif情報のbyte位置を取得する
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
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

        //Rotate and bitmap
        public static async Task<Android.Graphics.Bitmap> RotateAndBitmap(MemoryStream ms,int sensorOrientation)
        {
            //Exifからjpegのカメラの向きを取得
            var rotationType = ImageUtility.GetJpegOrientation(ms);
            //カメラの向きを元に適切に画像を回転させたいが,まだわからない

            //GetJpegOrientationメソッド内で位置が進んでいるので,先頭に戻す
            ms.Seek(0, SeekOrigin.Begin);
            //Byte[]→AndroidのBitmapを生成
            var bmp = await BitmapFactory.DecodeStreamAsync(ms);
            //Matrixを使って回転させ
            var matrix = new Matrix();
            matrix.PostRotate(180 - sensorOrientation);
            //回転したBitmapを生成し直す
            var rotated = Android.Graphics.Bitmap.CreateBitmap(bmp, 0, 0, bmp.Width, bmp.Height, matrix, true);

            return rotated;
        }

        //Bitmap to byte[]
        public static async Task<byte[]> AndroidBitmapToByteArray(Android.Graphics.Bitmap bitmap)
        {
            byte[] rotated;
            using (var ms2 = new MemoryStream())
            {
                await bitmap.CompressAsync(CompressFormat.Png, 0, ms2);
                rotated = ms2.ToArray();
            }

            return rotated;
        }
    }
}