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
        private readonly DroidCameraPreview2 _owner;

        public long FrameNumber { get; private set; }

        public CameraCaptureStateListener(DroidCameraPreview2 owner)
        {
            if (owner == null)
                throw new ArgumentNullException(nameof(owner));
            this._owner = owner;
        }

        //Sessionの設定完了(準備完了).プレビュー表示を開始
        public override void OnConfigured(CameraCaptureSession session)
        {
            if (this._owner.CameraDevice == null)
                return;

            this._owner.CaptureSession = session;

            try
            {
                //オートフォーカスの設定
                //https://qiita.com/ohwada/items/d33cd9c90abf3ec01f9e
                this._owner.PreviewRequestBuilder.Set(CaptureRequest.ControlAfMode,
                                                      (int)ControlAFMode.ContinuousPicture);
                //_previewBuilder.Set(CaptureRequest.ControlAfTrigger, (int)ControlAFTrigger.Start);

                //ここでやっとプレビューが表示される.
                this._owner.PreviewRequest = this._owner.PreviewRequestBuilder.Build();
                this._owner.CaptureSession.SetRepeatingRequest(this._owner.PreviewRequest,
                                                               this._owner.CameraCaptureSessionListener,
                                                               this._owner.BackgroundHandler);
            }
            catch (CameraAccessException ex)
            {
                ex.PrintStackTrace();
            }
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
            var s = sender as CameraCaptureSessionListener;
            if (s is null)
                return;

            this.FrameNumber = s.FrameNumber;
            OnCaptureCompleted(e);
        }
    }
}