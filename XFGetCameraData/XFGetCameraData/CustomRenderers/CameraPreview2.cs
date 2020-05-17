﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using Xamarin.Forms;
using Xamarin.Forms.Internals;

namespace XFGetCameraData.CustomRenderers
{
    public class CameraPreview2 : View
    {
        Command cameraClick;

        public static readonly BindableProperty CameraProperty = BindableProperty.Create(
            propertyName: "Camera",
            returnType: typeof(CameraOptions),
            declaringType: typeof(CameraPreview2),
            defaultValue: CameraOptions.Rear);
        public CameraOptions Camera
        {
            get { return (CameraOptions)GetValue(CameraProperty); }
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

        public Command CameraClick
        {
            get { return cameraClick; }
            set { cameraClick = value; }
        }

        public void PictureTaken()
        {
            PictureFinished?.Invoke();
        }

        public event Action PictureFinished;
    }
}
