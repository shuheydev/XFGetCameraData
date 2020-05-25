using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Media;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Firebase.ML.Vision;
using Firebase.ML.Vision.Common;
using Firebase.ML.Vision.Face;
using Java.Lang;
using Java.Nio;
using Java.Security;
using XFGetCameraData.Droid.FirebaseML.Listeners;
using XFGetCameraData.Droid.Utility;

namespace XFGetCameraData.Droid.CustomRenderers.Listeners
{
    public class ImageAvailableListener : Java.Lang.Object, ImageReader.IOnImageAvailableListener
    {
        private readonly DroidCameraPreview2 _owner;

        public ImageAvailableListener(DroidCameraPreview2 owner)
        {
            this._owner = owner;
        }
        public void OnImageAvailable(ImageReader reader)
        {
            this._owner.ImageReader = reader;

            this.ProcessOnImageAvailable();
        }

        /// <summary>
        /// OnImageAvailableから呼び出される
        /// </summary>
        private async void ProcessOnImageAvailable()
        {
            //this._owner.BackgroundHandler.Post(new ImageSaver(reader.AcquireNextImage()));
            var image = this._owner.ImageReader.AcquireNextImage();
            ByteBuffer buffer = image.GetPlanes()[0].Buffer;
            byte[] jpegBytes = new byte[buffer.Remaining()];
            buffer.Get(jpegBytes);
            image.Close();

            //acquireNextImageのあとでないと,次を取得せずに止まる.
            if (DroidCameraPreview2.UPDATE_FRAME_SPAN == 0)
                return;

            //Frameカウントを取得できていないとずっと動き続けてしまうので
            if (this._owner.FrameCount == 0)
                return;

            //指定したフレーム間隔でBitmapを取得する
            if (this._owner.FrameCount % DroidCameraPreview2.UPDATE_FRAME_SPAN != 0)
                return;

            //bytes[]→回転+bitmap→byte[]
            using (var ms = new MemoryStream(jpegBytes))
            {
                #region 画像回転
                this._owner.AndroidBitmap_Rotated = await ImageUtility.RotateAndBitmap(ms, this._owner.SensorOrientation);
                #endregion

                #region byte[]へ
                //AndroidBitmap→byte[]
                this._owner.JpegBytes = await ImageUtility.AndroidBitmapToByteArray(this._owner.AndroidBitmap_Rotated);
                #endregion
            }

            #region Firebase face detectを使った顔検出
            var imgForFirebase = FirebaseVisionImage.FromBitmap(this._owner.AndroidBitmap_Rotated);
            var options = new FirebaseVisionFaceDetectorOptions.Builder().Build();
            var detector = FirebaseVision.GetInstance(this._owner.FirebaseApp)
                .GetVisionFaceDetector(options);

            detector.DetectInImage(imgForFirebase)
                .AddOnSuccessListener(this._owner.DetectSuccessListener);
            #endregion         
        }
    }

    class ImageSaver : Java.Lang.Object, IRunnable
    {
        private Image _image;//jpeg

        public ImageSaver(Image image)
        {
            this._image = image;
        }

        public void Run()
        {
            ByteBuffer buffer = _image.GetPlanes()[0].Buffer;
            byte[] bytes = new byte[buffer.Remaining()];
            buffer.Get(bytes);
            this._image.Close();
        }
    }
}