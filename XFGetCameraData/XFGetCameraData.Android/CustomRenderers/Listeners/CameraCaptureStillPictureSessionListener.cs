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
    public class CameraCaptureStillPictureSessionListener:CameraCaptureSession.CaptureCallback
    {
        private readonly DroidCameraPreview2 _owner;

        public CameraCaptureStillPictureSessionListener(DroidCameraPreview2 owner)
        {
            if (owner == null)
                throw new ArgumentNullException("owner");
            this._owner = owner;
        }

        public override void OnCaptureCompleted(CameraCaptureSession session, CaptureRequest request, TotalCaptureResult result)
        {
            base.OnCaptureCompleted(session, request, result);
        }
    }
}