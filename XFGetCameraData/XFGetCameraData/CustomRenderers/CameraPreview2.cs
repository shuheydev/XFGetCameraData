using System;
using System.Collections.Generic;
using System.Drawing;
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

        public static readonly BindableProperty FrameNumberProperty = BindableProperty.Create(
            propertyName: "FrameNumber",
            returnType: typeof(long),
            declaringType: typeof(CameraPreview2),
            defaultValue: 0L);
        public long FrameNumber
        {
            get { return (long)GetValue(FrameNumberProperty); }
            set { SetValue(FrameNumberProperty, value); }
        }

        public static readonly BindableProperty FrameProperty = BindableProperty.Create(
            propertyName: "Frame",
            returnType: typeof(ImageSource),
            declaringType: typeof(CameraPreview2),
            defaultValue: null);
        public ImageSource Frame
        {
            get { return (ImageSource)GetValue(FrameProperty); }
            set { SetValue(FrameProperty, value); }
        }


        public event EventHandler FrameUpdated;
        public void OnFrameUpdated(EventArgs e)
        {
            FrameUpdated?.Invoke(this, e);
        }

        //public Command CameraClick
        //{
        //    get { return cameraClick; }
        //    set { cameraClick = value; }
        //}

        //public void PictureTaken()
        //{
        //    PictureFinished?.Invoke();
        //}

        //public event Action PictureFinished;
    }

    public enum CameraOption
    {
        Back = 1,
        Front = 0,
    }
}
