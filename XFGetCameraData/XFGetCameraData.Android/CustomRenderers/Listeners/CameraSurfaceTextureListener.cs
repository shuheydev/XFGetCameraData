using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Hardware.Camera2;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Java.Interop;
using Java.Lang;

namespace XFGetCameraData.Droid.CustomRenderers.Listeners
{
    public class CameraSurfaceTextureListener : Java.Lang.Object, TextureView.ISurfaceTextureListener
    {
        private readonly DroidCameraPreview2 _owner;

        public CameraSurfaceTextureListener(DroidCameraPreview2 owner)
        {
            if (owner == null)
                throw new System.ArgumentException(nameof(owner));
            this._owner = owner;
        }

        public void OnSurfaceTextureAvailable(SurfaceTexture surfaceTexture, int width, int height)
        {
            this._owner.SurfaceTexture = surfaceTexture;

            this._owner.StartCamera();
        }
        public bool OnSurfaceTextureDestroyed(SurfaceTexture surfaceTexture)
        {
            this._owner.StopCamera();
            return true;
        }
        public void OnSurfaceTextureSizeChanged(SurfaceTexture surface, int width, int height)
        {
        }
        public void OnSurfaceTextureUpdated(SurfaceTexture surface)
        {
            if (DroidCameraPreview2.GET_BITMAP_INTERVAL == 0)
                return;

            //Frameカウントを取得できていないとずっと動き続けてしまうので
            if (this._owner.FrameNumber == 0)
                return;

            //指定したフレーム間隔でBitmapを取得する
            if (this._owner.FrameNumber % DroidCameraPreview2.GET_BITMAP_INTERVAL != 0)
                return;

            //Frame毎に更新される
            //https://stackoverflow.com/questions/29413431/how-to-get-single-preview-frame-in-camera2-api-android-5-0
            var frame = Android.Graphics.Bitmap.CreateBitmap(this._owner.CameraTexture.Width, this._owner.CameraTexture.Height, Android.Graphics.Bitmap.Config.Argb8888);//previewSizeは大きすぎる.カメラの解像度になる
            this._owner.CameraTexture.GetBitmap(frame);

            this._owner.Frame = frame;
            //OnTextureUpdated(EventArgs.Empty);
        }
    }
}