using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Hardware.Camera2;
using Android.Media;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Firebase;
using Firebase.ML.Vision;
using Firebase.ML.Vision.Common;
using Firebase.ML.Vision.Face;
using Java.Interop;
using Java.Lang;
using XFGetCameraData.Droid.FirebaseML.Listeners;

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

            if (this._owner.IsPreviewing == true)
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
            this._owner.SurfaceTexture = surface;

            this.ProcessOnSurfaceTextureUpdate();
        }

        /// <summary>
        /// CameraSurfaceTextureListenerのOnSurfaceTextureUpdatedから呼び出される
        /// </summary>
        private void ProcessOnSurfaceTextureUpdate()
        {
            if (DroidCameraPreview2.UPDATE_FRAME_SPAN == 0)
                return;

            //Frameカウントを取得できていないとずっと動き続けてしまうので
            if (this._owner.FrameCount == 0)
                return;

            //指定したフレーム間隔でBitmapを取得する
            if (this._owner.FrameCount % DroidCameraPreview2.UPDATE_FRAME_SPAN != 0)
                return;

            #region TextureViewからBitmapを取得したい場合は有効にする
            ////Frame毎に更新される
            ////https://stackoverflow.com/questions/29413431/how-to-get-single-preview-frame-in-camera2-api-android-5-0
            //var bitmap = Android.Graphics.Bitmap.CreateBitmap(this._owner.CameraTexture.Width, this._owner.CameraTexture.Height, Android.Graphics.Bitmap.Config.Argb8888);//previewSizeは大きすぎる.カメラの解像度になる
            //this._owner.CameraTexture.GetBitmap(bitmap);

            //this._owner.AndroidBitmap = bitmap;
            #endregion
        }
    }
}