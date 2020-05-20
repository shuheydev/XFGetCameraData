using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
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
using Android.Util;
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

            _droidCameraPreview2.FrameNumberUpdated += _droidCameraPreview2_FrameNumberUpdated;
            _droidCameraPreview2.FrameUpdated += _droidCameraPreview2_FrameUpdated;

            this.SetNativeControl(_droidCameraPreview2);

            if (e.NewElement != null && _droidCameraPreview2 != null)
            {
                _formsCameraPreview2 = e.NewElement;

                if (this.Element == null || this.Control == null)
                    return;
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

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }

        private async void _droidCameraPreview2_FrameUpdated(object sender, EventArgs e)
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

        private void _droidCameraPreview2_FrameNumberUpdated(object sender, EventArgs e)
        {
            var s = sender as DroidCameraPreview2;

            _formsCameraPreview2.FrameNumber = s.FrameNumber;
        }
    }

    public class DroidCameraPreview2 : FrameLayout
    {
        private static readonly SparseIntArray ORIENTATIONS = new SparseIntArray();
        public static readonly int REQUEST_CAMERA_PERMISSION = 1;
        private static readonly string FRAGMENT_DIALOG = "dialog";

        // Tag for the {@link Log}.
        private static readonly string TAG = "Camera2BasicFragment";

        // Camera state: Showing camera preview.
        public const int STATE_PREVIEW = 0;

        // Camera state: Waiting for the focus to be locked.
        public const int STATE_WAITING_LOCK = 1;

        // Camera state: Waiting for the exposure to be precapture state.
        public const int STATE_WAITING_PRECAPTURE = 2;

        //Camera state: Waiting for the exposure state to be something other than precapture.
        public const int STATE_WAITING_NON_PRECAPTURE = 3;

        // Camera state: Picture was taken.
        public const int STATE_PICTURE_TAKEN = 4;

        // Max preview width that is guaranteed by Camera2 API
        private static readonly int MAX_PREVIEW_WIDTH = 1920;

        // Max preview height that is guaranteed by Camera2 API
        private static readonly int MAX_PREVIEW_HEIGHT = 1080;

        private readonly Context _context;
        public TextureView CameraTexture;

        public CameraCaptureSessionListener CameraCaptureSessionListener { get; internal set; }

        private readonly CameraSurfaceTextureListener _surfaceTextureListener;

        public Android.Widget.LinearLayout _linearLayout { get; }
        public bool OpeningCamera { private get; set; }

        private long _frameNumber;
        public long FrameNumber
        {
            get
            {
                return _frameNumber;
            }
            set
            {
                _frameNumber = value;
                OnFrameNumberUpdated(EventArgs.Empty);
            }
        }


        public event EventHandler FrameUpdated;
        protected virtual void OnFrameUpdated(EventArgs e)
        {
            FrameUpdated?.Invoke(this, e);
        }

        public event EventHandler FrameNumberUpdated;
        protected virtual void OnFrameNumberUpdated(EventArgs e)
        {
            FrameNumberUpdated?.Invoke(this, e);
        }

        private Android.Graphics.Bitmap _frame;
        public Android.Graphics.Bitmap Frame {
            get {
                return _frame;
            }
            set
            {
                _frame = value;
                OnFrameUpdated(EventArgs.Empty);
            }
        }

        public int CameraState = STATE_PREVIEW;

        private bool _isPreviewing;
        public bool IsPreviewing
        {
            get
            {
                return _isPreviewing;
            }
            set
            {
                _isPreviewing = value;

                //Previewの停止,再開については
                //https://bellsoft.jp/blog/system/detail_538
                if (value)
                {
                    //RestartPreview();
                    StartCamera();
                }
                else
                {
                    StopPreview();
                }
            }
        }

        public CameraDevice CameraDevice { get; internal set; }
        public SurfaceTexture SurfaceTexture { get; internal set; }
        public CameraCaptureSession CaptureSession { get; internal set; }
        public CaptureRequest PreviewRequest { get; internal set; }
        public Handler BackgroundHandler { get; internal set; }

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
            CameraTexture = view.FindViewById<TextureView>(Resource.Id.cameraTexture);

            //リスナーの作成
            this.CameraCaptureSessionListener = new CameraCaptureSessionListener(this);

            #region リスナーの登録
            this._surfaceTextureListener = new CameraSurfaceTextureListener(this);
            //this._surfaceTextureListener.CaptureCompleted += CameraSurfaceTextureListener_CaptureCompleted;
            //this._surfaceTextureListener.TextureUpdated += CameraSurfaceTextureListener_TextureUpdated;
            CameraTexture.SurfaceTextureListener = this._surfaceTextureListener;
            #endregion
            #endregion
        }

        private HandlerThread _backgroundThread;

        public Android.Util.Size PreviewSize;
        private CameraDevice.StateCallback _cameraStateListener;
        private string _cameraId;
        public CaptureRequest.Builder PreviewRequestBuilder;
        public const long GET_BITMAP_INTERVAL = 32;





        internal void StartCamera()
        {
            if (!this.IsPreviewing)
                return;

            StartBackgroundThread();

            var _cameraManager = (CameraManager)Android.App.Application.Context.GetSystemService(Context.CameraService);
            this._cameraId = _cameraManager.GetCameraIdList().FirstOrDefault();
            CameraCharacteristics cameraCharacteristics = _cameraManager.GetCameraCharacteristics(_cameraId);
            Android.Hardware.Camera2.Params.StreamConfigurationMap scm = (Android.Hardware.Camera2.Params.StreamConfigurationMap)cameraCharacteristics.Get(CameraCharacteristics.ScalerStreamConfigurationMap);
            this.PreviewSize = scm.GetOutputSizes((int)ImageFormatType.Jpeg)[0];

            this._cameraStateListener = new CameraStateListener(this);
            _cameraManager.OpenCamera(_cameraId, this._cameraStateListener, null);
        }
        internal void StopCamera()
        {
            StopBackgroundThread();
            StopPreview();
        }

        private void StopPreview()
        {
            if (this.CaptureSession == null)
                return;

            this.CaptureSession.StopRepeating();
        }

        private void RestartPreview()
        {
            if (this.CaptureSession == null)
                return;

            this.CaptureSession.SetRepeatingRequest(this.PreviewRequest, this.CameraCaptureSessionListener, this.BackgroundHandler);
        }

        private void StartBackgroundThread()
        {
            _backgroundThread = new HandlerThread("CameraBackground");//名前付きでスレッドを作成
            _backgroundThread.Start();
            this.BackgroundHandler = new Handler(_backgroundThread.Looper);
        }
        private void StopBackgroundThread()
        {
            _backgroundThread.QuitSafely();
            try
            {
                _backgroundThread.Join();
                _backgroundThread = null;
                this.BackgroundHandler = null;
            }
            catch (InterruptedException ex)
            {
                ex.PrintStackTrace();
            }
        }

        internal void CreateCameraPreviewSession()
        {
            try
            {
                if (this.SurfaceTexture == null)
                {
                    throw new IllegalStateException("SurfaceTexture is null");
                }

                this.SurfaceTexture.SetDefaultBufferSize(PreviewSize.Width, PreviewSize.Height);

                Surface surface = new Surface(this.SurfaceTexture);

                this.PreviewRequestBuilder = this.CameraDevice.CreateCaptureRequest(CameraTemplate.Preview);
                PreviewRequestBuilder.AddTarget(surface);

                //CameraCaptureSessionを生成
                List<Surface> surfaces = new List<Surface>();
                surfaces.Add(surface);
                this.CameraDevice.CreateCaptureSession(surfaces, new CameraCaptureStateListener(this), BackgroundHandler);
            }
            catch (CameraAccessException ex)
            {
                ex.PrintStackTrace();
            }
        }
    }
}