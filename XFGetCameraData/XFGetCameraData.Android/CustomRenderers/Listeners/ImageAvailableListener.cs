using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Media;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Java.Lang;
using Java.Nio;
using Java.Security;

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
        private void ProcessOnImageAvailable()
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

            this._owner.JpegBytes = jpegBytes;
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