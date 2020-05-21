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
using Xamarin.Forms.Internals;

namespace XFGetCameraData.Droid.CustomRenderers.Listeners
{
    public class CameraStateListener : CameraDevice.StateCallback
    {
        private readonly DroidCameraPreview2 _owner;

        public long FrameNumber { get; private set; }

        public CameraStateListener(DroidCameraPreview2 owner)
        {
            this._owner = owner;
        }

        public override void OnOpened(CameraDevice cameraDevice)
        {
            this._owner.CameraDevice = cameraDevice;
            this._owner.CreateCameraPreviewSession();
        }
        public override void OnDisconnected(CameraDevice cameraDevice)
        {
            this._owner.CameraDevice.Close();
            this._owner.CameraDevice = null;
        }
        public override void OnError(CameraDevice cameraDevice, [GeneratedEnum] CameraError error)
        {
            cameraDevice.Close();
            this._owner.CameraDevice = null;
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


    }
}