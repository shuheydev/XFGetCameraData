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
        public Bitmap Frame { get; private set; }
        public long FrameNumber { get; private set; }

        private CameraManager _cameraManager = null;
        private HandlerThread _backgroundThread;
        private Handler _backgroundHandler;
        private TextureView _cameraTexture;

        public CameraSurfaceTextureListener(TextureView cameraTexture)
        {
            this._cameraTexture = cameraTexture;
        }

        public void OnSurfaceTextureAvailable(SurfaceTexture surface, int width, int height)
        {
            this.StartCamera2(surface);
        }
        public bool OnSurfaceTextureDestroyed(SurfaceTexture surface)
        {
            this.StopCamera2();

            return true;
        }
        public void OnSurfaceTextureSizeChanged(SurfaceTexture surface, int width, int height)
        {
        }
        public void OnSurfaceTextureUpdated(SurfaceTexture surface)
        {
            //32フレーム毎にBitmap画像を取得する.
            if (this.FrameNumber % 32 != 0)
                return;

            //Frame毎に更新される
            //https://stackoverflow.com/questions/29413431/how-to-get-single-preview-frame-in-camera2-api-android-5-0
            var frame = Android.Graphics.Bitmap.CreateBitmap(_cameraTexture.Width, _cameraTexture.Height, Android.Graphics.Bitmap.Config.Argb8888);
            _cameraTexture.GetBitmap(frame);

            this.Frame = frame;
            OnTextureUpdated(EventArgs.Empty);
        }

        private void StartBackgroundThread()
        {
            _backgroundThread = new HandlerThread("CameraBackground");//名前付きでスレッドを作成
            _backgroundThread.Start();
            _backgroundHandler = new Handler(_backgroundThread.Looper);
        }
        private void StopBackgroundThread()
        {
            _backgroundThread.QuitSafely();
            try
            {
                _backgroundThread.Join();
                _backgroundThread = null;
                _backgroundHandler = null;
            }
            catch (InterruptedException ex)
            {
                ex.PrintStackTrace();
            }
        }

        private void StartCamera2(SurfaceTexture surface)
        {
            StartBackgroundThread();

            _cameraManager = (CameraManager)Android.App.Application.Context.GetSystemService(Context.CameraService);
            string cameraId = _cameraManager.GetCameraIdList().FirstOrDefault();
            CameraCharacteristics cameraCharacteristics = _cameraManager.GetCameraCharacteristics(cameraId);
            Android.Hardware.Camera2.Params.StreamConfigurationMap scm = (Android.Hardware.Camera2.Params.StreamConfigurationMap)cameraCharacteristics.Get(CameraCharacteristics.ScalerStreamConfigurationMap);
            var previewSize = scm.GetOutputSizes((int)ImageFormatType.Jpeg)[0];

            var cameraStateListener = new CameraStateListener(surface, previewSize, _backgroundHandler);
            cameraStateListener.CaptureCompleted += CameraStateListener_CaptureCompleted;

            _cameraManager.OpenCamera(cameraId, cameraStateListener, _backgroundHandler);
        }
        private void StopCamera2()
        {
            StopBackgroundThread();           
        }

        public event EventHandler CaptureCompleted;
        protected virtual void OnCaptureCompleted(EventArgs e)
        {
            CaptureCompleted?.Invoke(this, e);
        }

        public event EventHandler TextureUpdated;
        protected virtual void OnTextureUpdated(EventArgs e)
        {
            TextureUpdated?.Invoke(this, e);
        }

        private void CameraStateListener_CaptureCompleted(object sender, EventArgs e)
        {
            var s = sender as CameraStateListener;
            if (s is null)
                return;

            this.FrameNumber = s.FrameNumber;
            OnCaptureCompleted(EventArgs.Empty);
        }
    }
}