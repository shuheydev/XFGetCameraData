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

    //カメラのフレーム毎に発生するイベントのリスナー
    public class CameraCaptureListener : CameraCaptureSession.CaptureCallback
    {
        public long FrameNumber { get; private set; }

        public event EventHandler CaptureCompleted;
        protected virtual void OnCaptureCompleted(EventArgs e)
        {
            CaptureCompleted?.Invoke(this, e);
        }

        private readonly DroidCameraPreview2 _owner;

        public CameraCaptureListener(DroidCameraPreview2 owner)
        {
            this._owner = owner ?? throw new ArgumentNullException("owner");
        }

        public override void OnCaptureCompleted(CameraCaptureSession session, CaptureRequest request, TotalCaptureResult result)
        {
            base.OnCaptureCompleted(session, request, result);
            this.FrameNumber = result.FrameNumber;

            OnCaptureCompleted(EventArgs.Empty);
        }

        public override void OnCaptureProgressed(CameraCaptureSession session, CaptureRequest request, CaptureResult partialResult)
        {
            base.OnCaptureProgressed(session, request, partialResult);
        }
    }
}