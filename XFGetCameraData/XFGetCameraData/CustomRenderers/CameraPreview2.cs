using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using Xamarin.Forms;
using Xamarin.Forms.Internals;

[assembly: InternalsVisibleTo("XFGetCameraData.Droid")]
namespace XFGetCameraData.CustomRenderers
{
    public class CameraPreview2 : View
    {
        Command cameraClick;
        public static readonly BindableProperty IsPreviewingProperty = BindableProperty.Create(
            propertyName: "IsPreviewing",
            returnType: typeof(bool),
            declaringType: typeof(CameraPreview2),
            defaultValue: false);
        public bool IsPreviewing
        {
            get { return (bool)GetValue(IsPreviewingProperty); }
            set { SetValue(IsPreviewingProperty, value); }
        }

        public static readonly BindableProperty CameraProperty = BindableProperty.Create(
            propertyName: "Camera",
            returnType: typeof(CameraOption),
            declaringType: typeof(CameraPreview2),
            defaultValue: CameraOption.Back);
        public CameraOption Camera
        {
            get { return (CameraOption)GetValue(CameraProperty); }
            set { SetValue(CameraProperty, value); }
        }

        public static readonly BindableProperty FrameCountProperty = BindableProperty.Create(
            propertyName: "FrameCount",
            returnType: typeof(long),
            declaringType: typeof(CameraPreview2),
            defaultValue: 0L);
        public long FrameCount
        {
            get { return (long)GetValue(FrameCountProperty); }
            set { SetValue(FrameCountProperty, value); }
        }

        public static readonly BindableProperty ImageSourceProperty = BindableProperty.Create(
            propertyName: "ImageSource",
            returnType: typeof(ImageSource),
            declaringType: typeof(CameraPreview2),
            defaultValue: null);
        public ImageSource ImageSource
        {
            get { return (ImageSource)GetValue(ImageSourceProperty); }
            set { SetValue(ImageSourceProperty, value); }
        }

        public static readonly BindableProperty BitmapProperty = BindableProperty.Create(
            propertyName: "Bitmap",
            returnType: typeof(Bitmap),
            declaringType: typeof(CameraPreview2),
            defaultValue: null);
        public Bitmap Bitmap
        {
            get { return (Bitmap)GetValue(BitmapProperty); }
            set { SetValue(BitmapProperty, value); }
        }

        public static readonly BindableProperty JpegBytesProperty = BindableProperty.Create(
            propertyName: "JpegBytes",
            returnType: typeof(byte[]),
            declaringType: typeof(CameraPreview2),
            defaultValue: null);
        public byte[] JpegBytes
        {
            get { return (byte[])GetValue(JpegBytesProperty); }
            set { SetValue(JpegBytesProperty, value); }
        }

        public static readonly BindableProperty SensorOrientationProperty = BindableProperty.Create(
            propertyName: "SensorOrientation",
            returnType: typeof(int),
            declaringType: typeof(CameraPreview2),
            defaultValue: 0);
        public int SensorOrientation
        {
            get { return (int)GetValue(SensorOrientationProperty); }
            set { SetValue(SensorOrientationProperty, value); }
        }

        public event EventHandler ImageSourceUpdated;
        public void OnImageSourceUpdated(EventArgs e)
        {
            ImageSourceUpdated?.Invoke(this, e);
        }

        public event EventHandler JpegBytesUpdated;
        public void OnJpegBytesUpdated(EventArgs e)
        {
            JpegBytesUpdated?.Invoke(this, e);
        }

        public event EventHandler SensorOrientationUpdated;
        public void OnSensorOrientationUpdated(EventArgs e)
        {
            SensorOrientationUpdated?.Invoke(this,e);
        }
    }

    public enum CameraOption
    {
        Front = 0,
        Back = 1,
    }
}
