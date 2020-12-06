using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using Xamarin.Forms;
using Xamarin.Forms.Internals;

//各プラットフォームのプロジェクトからinternalなメンバーが見えるように
[assembly: InternalsVisibleTo("XFGetCameraData.Android")]
[assembly: InternalsVisibleTo("XFGetCameraData.iOS")]
namespace XFGetCameraData.CustomRenderers
{
    public class CameraPreview2 : View
    {
        #region Property
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
            defaultValue: CameraOption.Front);
        public CameraOption Camera
        {
            get { return (CameraOption)GetValue(CameraProperty); }
            set { SetValue(CameraProperty, value); }
        }

        public static readonly BindableProperty FrameCountProperty = BindableProperty.Create(
            propertyName: "FrameCount",
            returnType: typeof(long),
            declaringType: typeof(CameraPreview2),
            propertyChanged: FrameCountPropertyChanged,
            defaultValue: 0L);
        public long FrameCount
        {
            get { return (long)GetValue(FrameCountProperty); }
            set { SetValue(FrameCountProperty, value); }
        }
        private static void FrameCountPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var cameraView = bindable as CameraPreview2;
            if (cameraView == null)
                return;
            cameraView.OnFrameCountUpdated(EventArgs.Empty);
        }

        public static readonly BindableProperty ImageSourceProperty = BindableProperty.Create(
            propertyName: "ImageSource",
            returnType: typeof(ImageSource),
            declaringType: typeof(CameraPreview2),
            propertyChanged: ImageSourcePropertyChanged,
            defaultValue: null);
        private static void ImageSourcePropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (newValue == null)
                return;
            var cameraView = bindable as CameraPreview2;
            cameraView?.OnImageSourceUpdated(EventArgs.Empty);
        }
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
            propertyChanged: JpegBytesPropertyChanged,
            defaultValue: null);
        private static void JpegBytesPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (newValue == null)
                return;
            var cameraView = bindable as CameraPreview2;
            cameraView?.OnJpegBytesUpdated(EventArgs.Empty);
        }

        public byte[] JpegBytes
        {
            get { return (byte[])GetValue(JpegBytesProperty); }
            set { SetValue(JpegBytesProperty, value); }
        }

        public static readonly BindableProperty SensorOrientationProperty = BindableProperty.Create(
            propertyName: "SensorOrientation",
            returnType: typeof(int),
            declaringType: typeof(CameraPreview2),
            propertyChanged: SensorOrientationPropertyChanged,
            defaultValue: 0);
        public int SensorOrientation
        {
            get { return (int)GetValue(SensorOrientationProperty); }
            set { SetValue(SensorOrientationProperty, value); }
        }
        private static void SensorOrientationPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var cameraView = bindable as CameraPreview2;
            cameraView?.OnSensorOrientationUpdated(EventArgs.Empty);
        }


        #endregion

        #region Event
        public event EventHandler FrameCountupdated;
        private void OnFrameCountUpdated(EventArgs e)
        {
            FrameCountupdated?.Invoke(this, e);
        }

        public event EventHandler ImageSourceUpdated;
        private void OnImageSourceUpdated(EventArgs e)
        {
            ImageSourceUpdated?.Invoke(this, e);
        }

        public event EventHandler JpegBytesUpdated;
        private void OnJpegBytesUpdated(EventArgs e)
        {
            JpegBytesUpdated?.Invoke(this, e);
        }

        public event EventHandler SensorOrientationUpdated;
        private void OnSensorOrientationUpdated(EventArgs e)
        {
            SensorOrientationUpdated?.Invoke(this, e);
        }
        #endregion
    }

    public enum CameraOption
    {
        Front = 0,
        Back = 1,
    }
}
