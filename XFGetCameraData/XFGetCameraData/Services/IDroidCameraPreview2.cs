using System;
using System.Collections.Generic;
using System.Text;
using XFGetCameraData.CustomRenderers;

namespace XFGetCameraData.Services
{
    public interface IDroidCameraPreview2
    {
        event EventHandler JpegBytesUpdated;
        event EventHandler AndroidBitmapUpdated;
        event EventHandler FrameCountUpdated;
        event EventHandler SensorOrientationUpdated;

        bool IsPreviewing { get; set; }
        CameraOption CameraOption { get; set; }
        byte[] JpegBytes { get; set; }
        long FrameCount { get; set; }

        void StartCamera();
        void StopCamera();
    }
}
