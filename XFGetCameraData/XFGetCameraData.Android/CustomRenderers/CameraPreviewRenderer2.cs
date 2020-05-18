using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Hardware;
using Android.Hardware.Camera2;
using Android.OS;
using Android.Runtime;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using Java.Lang;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;
using XFGetCameraData.CustomRenderers;
using XFGetCameraData.Droid.CustomRenderers;
using XFGetCameraData.Droid.CustomRenderers.Listeners;
using XFGetCameraData.Droid.Services;

[assembly: ExportRenderer(typeof(CameraPreview2), typeof(CameraPreviewRenderer2))]
namespace XFGetCameraData.Droid.CustomRenderers
{
    //これがカスタムレンダラー本体ね.
    //
    public class CameraPreviewRenderer2 : ViewRenderer<CameraPreview2, DroidCameraPreview2>
    {
        private readonly Context _context;
        private DroidCameraPreview2 _droidCameraPreview2;
        private CameraPreview2 _formsCameraPreview2;

        public long FrameNumber { get; private set; }
        public ImageSource Frame { get; private set; }

        public CameraPreviewRenderer2(Context context) : base(context)
        {
            this._context = context;
        }

        protected override void OnElementChanged(ElementChangedEventArgs<CameraPreview2> e)
        {
            base.OnElementChanged(e);

            _droidCameraPreview2 = new DroidCameraPreview2(this._context);

            _droidCameraPreview2.CaptureCompleted += CaptureCompleted;
            _droidCameraPreview2.TextureUpdated += TextureUpdated;

            this.SetNativeControl(_droidCameraPreview2);

            if (e.NewElement != null && _droidCameraPreview2 != null)
            {
                _formsCameraPreview2 = e.NewElement;

                if (this.Element == null || this.Control == null)
                    return;

                this.Control.IsPreviewing = this.Element.IsPreviewing;
            }
        }
        protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnElementPropertyChanged(sender, e);

            if (this.Element == null || this.Control == null)
                return;

            if (e.PropertyName == nameof(Element.IsPreviewing))
                this.Control.IsPreviewing = this.Element.IsPreviewing;
        }

        private async void TextureUpdated(object sender, EventArgs e)
        {
            var s = sender as DroidCameraPreview2;
            if (s is null)
                return;

            byte[] bitmapData;
            //pngのbyte[]に変換
            using (var stream = new MemoryStream())
            {
                await s.Frame.CompressAsync(Android.Graphics.Bitmap.CompressFormat.Png, 0, stream);
                bitmapData = stream.ToArray();
            }

            var imageSource = ImageSource.FromStream(() => new MemoryStream(bitmapData));
            this.Frame = imageSource;
            _formsCameraPreview2.Frame = imageSource;
        }
        private void CaptureCompleted(object sender, EventArgs e)
        {
            var s = sender as DroidCameraPreview2;
            if (s is null)
                return;

            this.FrameNumber = s.FrameNumber;
            _formsCameraPreview2.FrameNumber = s.FrameNumber;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }

    public class DroidCameraPreview2 : FrameLayout
    {
        private readonly Context _context;
        private readonly TextureView _cameraTexture;
        private readonly CameraSurfaceTextureListener _surfaceTextureListener;

        public Android.Widget.LinearLayout _linearLayout { get; }
        public bool OpeningCamera { private get; set; }
        public long FrameNumber { get; private set; }
        public Android.Graphics.Bitmap Frame { get; private set; }

        private bool _isPreviewing;
        public bool IsPreviewing
        {
            get
            {
                return _isPreviewing;
            }
            set
            {
                //Previewの停止,再開については
                //https://bellsoft.jp/blog/system/detail_538
                if (value)
                {
                        this._surfaceTextureListener?.RestartPreview();
                }
                else
                {
                    this._surfaceTextureListener?.StopPreview();
                }
                _isPreviewing = value;
            }
        }

        public DroidCameraPreview2(Context context) : base(context)
        {
            this._context = context;

            #region プレビュー用のViewを用意する.
            //予め用意しておいたレイアウトファイルを読み込む場合はこのようにする
            //この場合,Resource.LayoutにCameraLayout.xmlファイルを置いている.
            //中身はTextureViewのみ
            var inflater = LayoutInflater.FromContext(context);
            if (inflater == null)
                return;
            var view = inflater.Inflate(Resource.Layout.CameraLayout, this);
            _cameraTexture = view.FindViewById<TextureView>(Resource.Id.cameraTexture);

            #region リスナーの登録
            this._surfaceTextureListener = new CameraSurfaceTextureListener(_cameraTexture);
            this._surfaceTextureListener.CaptureCompleted += CameraSurfaceTextureListener_CaptureCompleted;
            this._surfaceTextureListener.TextureUpdated += CameraSurfaceTextureListener_TextureUpdated;
            _cameraTexture.SurfaceTextureListener = this._surfaceTextureListener;
            #endregion
            #endregion
        }

        public event EventHandler CaptureCompleted;
        protected virtual void OnCaptureCompleted(EventArgs e)
        {
            CaptureCompleted?.Invoke(this, e);
        }
        private void CameraSurfaceTextureListener_CaptureCompleted(object sender, EventArgs e)
        {
            var s = sender as CameraSurfaceTextureListener;
            if (s is null)
                return;

            this.FrameNumber = s.FrameNumber;
            OnCaptureCompleted(e);
        }

        public event EventHandler TextureUpdated;
        protected virtual void OnTextureUpdated(EventArgs e)
        {
            TextureUpdated?.Invoke(this, e);
        }
        private void CameraSurfaceTextureListener_TextureUpdated(object sender, EventArgs e)
        {
            var s = sender as CameraSurfaceTextureListener;
            if (s is null)
                return;

            this.Frame = s.Frame;
            OnTextureUpdated(EventArgs.Empty);
        }
    }
}