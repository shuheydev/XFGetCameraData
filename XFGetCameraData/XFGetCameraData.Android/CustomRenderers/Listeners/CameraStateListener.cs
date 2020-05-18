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
using Android.Util;
using Android.Views;
using Android.Widget;

namespace XFGetCameraData.Droid.CustomRenderers.Listeners
{
    public class CameraStateListener : CameraDevice.StateCallback
    {
        private readonly SurfaceTexture _surface;
        private readonly Size _previewSize;
        private readonly Handler _backgroundHandler;
        private CameraDevice _camera;
        private CameraCaptureStateListener _captureStateListener;

        public long FrameNumber { get; private set; }

        public CameraStateListener(SurfaceTexture surface, Android.Util.Size previewSize, Handler backgroundHandler)
        {
            this._surface = surface;
            this._previewSize = previewSize;
            this._backgroundHandler = backgroundHandler;
        }

        public override void OnOpened(CameraDevice camera)
        {
            _surface.SetDefaultBufferSize(_previewSize.Width, _previewSize.Height);
            Surface surface = new Surface(_surface);

            List<Surface> surfaces = new List<Surface>();
            surfaces.Add(surface);

            var previewBuilder = camera.CreateCaptureRequest(CameraTemplate.Preview);
            //オートフォーカスの設定
            //https://qiita.com/ohwada/items/d33cd9c90abf3ec01f9e
            previewBuilder.Set(CaptureRequest.ControlAfMode, (int)ControlAFMode.ContinuousPicture);
            //_previewBuilder.Set(CaptureRequest.ControlAfTrigger, (int)ControlAFTrigger.Start);

            previewBuilder.AddTarget(surface);
            var previewRequest = previewBuilder.Build();

            this._captureStateListener = new CameraCaptureStateListener(previewRequest, _backgroundHandler);
            this._captureStateListener.CaptureCompleted += CaptureStateListener_CaptureCompleted;
            //CameraCaptureSessionを生成
            camera.CreateCaptureSession(surfaces, this._captureStateListener, _backgroundHandler);
        }
        public override void OnDisconnected(CameraDevice camera)
        {
            _camera = camera;
            Close();
        }
        public override void OnError(CameraDevice camera, [GeneratedEnum] CameraError error)
        {
            _camera = camera;
            Close();
        }

        public void Close()
        {
            _camera.Close();
            _camera = null;
        }

        public event EventHandler CaptureCompleted;
        protected virtual void OnCaptureCompleted(EventArgs e)
        {
            CaptureCompleted?.Invoke(this, e);
        }
        private void CaptureStateListener_CaptureCompleted(object sender, EventArgs e)
        {
            var s = sender as CameraCaptureStateListener;
            if (s is null)
                return;

            this.FrameNumber = s.FrameNumber;
            OnCaptureCompleted(EventArgs.Empty);
        }

        internal void StopPreview()
        {
            this._captureStateListener?.StopPreview();
        }

        internal void RestartPreview()
        {
            this._captureStateListener?.RestartPreview();
        }
    }
}