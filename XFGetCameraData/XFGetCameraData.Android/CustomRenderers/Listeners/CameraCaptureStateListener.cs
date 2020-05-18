using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Hardware.Camera2;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace XFGetCameraData.Droid.CustomRenderers.Listeners
{
    public class CameraCaptureStateListener : CameraCaptureSession.StateCallback
    {
        private readonly CaptureRequest _previewRequest;
        private readonly Handler _backgroundHandler;

        public long FrameNumber { get; private set; }

        public CameraCaptureStateListener(CaptureRequest previewRequest, Handler backgroundHandler)
        {
            this._previewRequest = previewRequest;
            this._backgroundHandler = backgroundHandler;
        }

        public override void OnConfigured(CameraCaptureSession session)
        {
            //_cameraCaptureListenerで1フレームごとのキャプチャに対する処理を行う
            //それをリスナーとして埋め込む
            var cameraCaptureListener = new CameraCaptureListener();
            cameraCaptureListener.CaptureCompleted += CameraCaptureListener_CaptureCompleted;
            session.SetRepeatingRequest(_previewRequest, cameraCaptureListener, _backgroundHandler);
        }
        public override void OnConfigureFailed(CameraCaptureSession session)
        {
        }

        public event EventHandler CaptureCompleted;
        protected virtual void OnCaptureCompleted(EventArgs e)
        {
            CaptureCompleted?.Invoke(this, e);
        }
        private void CameraCaptureListener_CaptureCompleted(object sender, EventArgs e)
        {
            var s = sender as CameraCaptureListener;
            if (s is null)
                return;

            this.FrameNumber = s.FrameNumber;
            OnCaptureCompleted(e);
        }
    }
}