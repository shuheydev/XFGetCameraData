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
    public class CameraStateListener:CameraDevice.StateCallback
    {
        public DroidCameraPreview2 Camera;

        public override void OnOpened(CameraDevice camera)
        {
            if (this.Camera == null)
                return;

            this.Camera.CameraDevice = camera;
            this.Camera.StartPreview();
            this.Camera.OpeningCamera = false;
        }

        public override void OnDisconnected(CameraDevice camera)
        {
            if (this.Camera == null)
                return;

            camera.Close();
            this.Camera.CameraDevice = null;
            this.Camera.OpeningCamera = false;
        }

        public override void OnError(CameraDevice camera, [GeneratedEnum] CameraError error)
        {
            camera.Close();

            if (this.Camera == null)
                return;

            this.Camera.CameraDevice = null;
            this.Camera.OpeningCamera = false;
        }
    }
}