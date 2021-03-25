using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Hardware;
using Android.Hardware.Camera2;
using Android.Media;
using Android.OS;
using Android.Runtime;
//using Android.Support.V7.Widget;
using Android.Util;
using Android.Views;
using Android.Widget;
using Java.Lang;
using Java.Nio;
using Xamarin.Forms;
using Xamarin.Forms.Internals;
using Xamarin.Forms.Platform.Android;
using XFGetCameraData.CustomRenderers;
using XFGetCameraData.Droid.CustomRenderers;
using XFGetCameraData.Droid.CustomRenderers.Listeners;
using XFGetCameraData.Droid.Services;
using XFGetCameraData.Droid.Utility;
using static Android.Graphics.Bitmap;

[assembly: ExportRenderer(typeof(CameraPreview2), typeof(CameraPreviewRenderer2))]
namespace XFGetCameraData.Droid.CustomRenderers
{
    /// <summary>
    /// 
    /// </summary>
    public class CameraPreviewRenderer2 : ViewRenderer<CameraPreview2, DroidCameraPreview2>
    {
        private readonly Context _context;
        private DroidCameraPreview2 _droidCameraPreview2;
        private CameraPreview2 _formsCameraPreview2;

        public long FrameCount { get; private set; }
        public ImageSource ImageSource { get; private set; }
        public byte[] JpegBytes { get; private set; }
        public int SensorOrientation { get; private set; }

        public CameraPreviewRenderer2(Context context) : base(context)
        {
            this._context = context;
        }

        protected override void OnElementChanged(ElementChangedEventArgs<CameraPreview2> e)
        {
            base.OnElementChanged(e);

            _droidCameraPreview2 = new DroidCameraPreview2(this._context);

            _droidCameraPreview2.FrameCountUpdated += _droidCameraPreview2_FrameCountUpdated;
            _droidCameraPreview2.AndroidBitmapUpdated += _droidCameraPreview2_AndroidBitmapUpdated;
            //_droidCameraPreview2.JpegBytesUpdated += _droidCameraPreview2_JpegBytesUpdated;
            _droidCameraPreview2.SensorOrientationUpdated += _droidCameraPreview2_SensorOrientationUpdated;

            this.SetNativeControl(_droidCameraPreview2);

            if (e.NewElement != null && _droidCameraPreview2 != null)
            {
                _formsCameraPreview2 = e.NewElement;

                if (this.Element == null || this.Control == null)
                    return;

                //プロパティの初期化はここで
                this.Control.IsPreviewing = this.Element.IsPreviewing;
                this.Control.CameraOption = this.Element.Camera;
            }
        }
        protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnElementPropertyChanged(sender, e);

            if (this.Element == null || this.Control == null)
                return;

            if (e.PropertyName == nameof(Element.IsPreviewing))
                this.Control.IsPreviewing = this.Element.IsPreviewing;

            if (e.PropertyName == nameof(Element.Camera))
                this.Control.CameraOption = this.Element.Camera;
        }
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }

        #region Event handler for set value to Xamarin.Forms control.
        private void _droidCameraPreview2_SensorOrientationUpdated(object sender, EventArgs e)
        {
            var s = sender as DroidCameraPreview2;
            if (s is null)
                return;

            this.SensorOrientation = s.SensorOrientation;
            _formsCameraPreview2.SensorOrientation = s.SensorOrientation;
        }
        private void _droidCameraPreview2_JpegBytesUpdated(object sender, EventArgs e)
        {
            var s = sender as DroidCameraPreview2;
            if (s is null)
                return;

            this.JpegBytes = s.JpegBytes;

            #region byte[] → ImageSource
            var imgSource = ImageSource.FromStream(() => new MemoryStream(this.JpegBytes));
            this.ImageSource = ImageSource;
            _formsCameraPreview2.ImageSource = imgSource;
            #endregion
        }

        private async void _droidCameraPreview2_AndroidBitmapUpdated(object sender, EventArgs e)
        {
            var s = sender as DroidCameraPreview2;
            if (s is null)
                return;

            #region Bitmap → ImageSource

            byte[] bitmapData;
            //pngのbyte[]に変換
            using (var stream = new MemoryStream())
            {
                await s.AndroidBitmap.CompressAsync(Android.Graphics.Bitmap.CompressFormat.Png, 0, stream);
                bitmapData = stream.ToArray();
            }

            var imageSource = ImageSource.FromStream(() => new MemoryStream(bitmapData));
            this.ImageSource = imageSource;
            _formsCameraPreview2.ImageSource = imageSource;
            #endregion
        }
        private void _droidCameraPreview2_FrameCountUpdated(object sender, EventArgs e)
        {
            var s = sender as DroidCameraPreview2;

            this.FrameCount = s.FrameCount;
            _formsCameraPreview2.FrameCount = s.FrameCount;
        }
        #endregion
    }

}