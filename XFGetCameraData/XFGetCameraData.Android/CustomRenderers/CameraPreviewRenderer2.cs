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
using Android.Support.V7.Widget;
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
    //これがカスタムレンダラー本体ね.
    //
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
            //_droidCameraPreview2.AndroidBitmapUpdated += _droidCameraPreview2_AndroidBitmapUpdated;
            _droidCameraPreview2.JpegBytesUpdated += _droidCameraPreview2_JpegBytesUpdated;
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
            _formsCameraPreview2.OnSensorOrientationUpdated(EventArgs.Empty);
        }
        private async void _droidCameraPreview2_JpegBytesUpdated(object sender, EventArgs e)
        {
            var s = sender as DroidCameraPreview2;
            if (s is null)
                return;

            //s.Imageは画像が横のままなので,困る
            //var imageSource = ImageSource.FromStream(() => new MemoryStream(s.Image));
            //_formsCameraPreview2.Frame = imageSource;
            //_formsCameraPreview2.OnFrameUpdated(EventArgs.Empty);



            //bytes[]→bitmap→
            using (var ms = new MemoryStream(s.JpegBytes))
            {
                #region 画像回転
                //Exifからjpegのカメラの向きを取得
                var rotationType = ImageUtility.GetJpegOrientation(ms);

                //GetJpegOrientationメソッド内で位置が進んでいるので,先頭に戻す
                ms.Seek(0, SeekOrigin.Begin);
                //Byte[]→AndroidのBitmapを生成
                var bmp = await BitmapFactory.DecodeStreamAsync(ms);
                //Matrixを使って回転させ
                var matrix = new Matrix();
                matrix.PostRotate(180 - this.SensorOrientation);
                //回転したBitmapを生成し直す
                var rotated = Android.Graphics.Bitmap.CreateBitmap(bmp, 0, 0, bmp.Width, bmp.Height, matrix, true);

                #endregion

                //AndroidBitmap→byte[]
                byte[] rotatedBytes;
                using (var ms2 = new MemoryStream())
                {
                    await rotated.CompressAsync(CompressFormat.Png, 0, ms2);
                    rotatedBytes = ms2.ToArray();
                }

                this.JpegBytes = rotatedBytes;
                _formsCameraPreview2.JpegBytes = s.JpegBytes;
                _formsCameraPreview2.OnJpegBytesUpdated(EventArgs.Empty);

                //byte[] → ImageSource
                var imgSource = ImageSource.FromStream(() => new MemoryStream(rotatedBytes));
                this.ImageSource = ImageSource;
                _formsCameraPreview2.ImageSource = imgSource;
                _formsCameraPreview2.OnImageSourceUpdated(EventArgs.Empty);
            }
        }
        private async void _droidCameraPreview2_AndroidBitmapUpdated(object sender, EventArgs e)
        {
            var s = sender as DroidCameraPreview2;
            if (s is null)
                return;

            //Bitmap → ImageSource

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
            _formsCameraPreview2.OnImageSourceUpdated(EventArgs.Empty);
        }
        private void _droidCameraPreview2_FrameCountUpdated(object sender, EventArgs e)
        {
            var s = sender as DroidCameraPreview2;

            this.FrameCount = s.FrameCount;
            _formsCameraPreview2.FrameCount = s.FrameCount;
        }
        #endregion
    }

    public class DroidCameraPreview2 : FrameLayout
    {
        public Android.Widget.LinearLayout _linearLayout { get; }
        public bool OpeningCamera { private get; set; }

        public static readonly SparseIntArray ORIENTATIONS = new SparseIntArray();
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

        public const long UPDATE_FRAME_SPAN = 4;//例:64フレーム毎にFrameやBitmapプロパティを更新する.


        // Max preview width that is guaranteed by Camera2 API
        private static readonly int MAX_PREVIEW_WIDTH = 1920;

        // Max preview height that is guaranteed by Camera2 API
        private static readonly int MAX_PREVIEW_HEIGHT = 1080;

        public int CameraState = STATE_PREVIEW;

        #region Important
        private readonly Context _context;
        public TextureView CameraTexture;

        private HandlerThread _backgroundThread;
        public Handler BackgroundHandler { get; internal set; }

        private string _cameraId;
        private CameraManager _cameraManager;

        private Android.Util.Size _previewSize;
        #endregion

        #region Listener
        public CameraCaptureSessionListener CameraCaptureSessionListener { get; internal set; }
        public CameraCaptureStillPictureSessionListener CameraCaptureStillPictureSessionListener { get; }
        public ImageAvailableListener ImageAvailableListener { get; }
        private readonly CameraSurfaceTextureListener _surfaceTextureListener;
        private CameraDevice.StateCallback _cameraStateListener;
        #endregion

        #region Data which be set to custome renderer class.
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
                    StartCamera();
                }
                else
                {
                    StopCamera();
                }
            }
        }
        private CameraOption _cameraOption;
        public CameraOption CameraOption
        {
            get
            {
                return _cameraOption;
            }
            set
            {
                _cameraOption = value;

                StartCamera();
            }
        }
        private byte[] _jpegBytes;
        public byte[] JpegBytes
        {
            get
            {
                return _jpegBytes;
            }
            internal set
            {
                _jpegBytes = value;
                OnJpegBytesUpdated(EventArgs.Empty);
            }
        }
        private Android.Graphics.Bitmap _androidBitmap;
        public Android.Graphics.Bitmap AndroidBitmap
        {
            get
            {
                return _androidBitmap;
            }
            set
            {
                _androidBitmap = value;
                OnAndroidBitmapUpdated(EventArgs.Empty);
            }
        }
        private long _frameCount;
        public long FrameCount
        {
            get
            {
                return _frameCount;
            }
            set
            {
                _frameCount = value;
                OnFrameCountUpdated(EventArgs.Empty);
            }
        }
        private int _sensorOrientation;
        public int SensorOrientation
        {
            get
            {
                return _sensorOrientation;
            }
            internal set
            {
                _sensorOrientation = value;
                OnSensorOrientationUpdated(EventArgs.Empty);
            }
        }
        #endregion

        #region Data which be set from Listener
        public CameraDevice CameraDevice { get; internal set; }
        public SurfaceTexture SurfaceTexture { get; internal set; }
        public CameraCaptureSession CaptureSession { get; internal set; }
        public ImageReader ImageReader { get; internal set; }
        public CaptureResult CaptureResult { get; internal set; }
        #endregion

        #region Events called when property value changed.
        public event EventHandler JpegBytesUpdated;
        protected virtual void OnJpegBytesUpdated(EventArgs e)
        {
            JpegBytesUpdated?.Invoke(this, e);
        }
        public event EventHandler AndroidBitmapUpdated;
        protected virtual void OnAndroidBitmapUpdated(EventArgs e)
        {
            AndroidBitmapUpdated?.Invoke(this, e);
        }
        public event EventHandler FrameCountUpdated;
        protected virtual void OnFrameCountUpdated(EventArgs e)
        {
            FrameCountUpdated?.Invoke(this, e);
        }
        public event EventHandler SensorOrientationUpdated;
        protected virtual void OnSensorOrientationUpdated(EventArgs e)
        {
            SensorOrientationUpdated?.Invoke(this, e);
        }
        #endregion

        #region Request Builder
        public CaptureRequest.Builder PreviewRequestBuilder;
        public CaptureRequest PreviewRequest { get; internal set; }
        public CaptureRequest.Builder StillCaptureBuilder { get; private set; }
        public CaptureRequest StillCaptureRequest { get; internal set; }
        #endregion


        public DroidCameraPreview2(Context context) : base(context)
        {
            ORIENTATIONS.Append((int)SurfaceOrientation.Rotation0, 90);
            ORIENTATIONS.Append((int)SurfaceOrientation.Rotation90, 0);
            ORIENTATIONS.Append((int)SurfaceOrientation.Rotation180, 270);
            ORIENTATIONS.Append((int)SurfaceOrientation.Rotation270, 180);

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
            this.CameraCaptureStillPictureSessionListener = new CameraCaptureStillPictureSessionListener(this);
            this.ImageAvailableListener = new ImageAvailableListener(this);

            #region リスナーの登録
            this._surfaceTextureListener = new CameraSurfaceTextureListener(this);
            CameraTexture.SurfaceTextureListener = this._surfaceTextureListener;
            #endregion
            #endregion
        }

        private void StartBackgroundThread()
        {
            _backgroundThread = new HandlerThread("CameraBackground");//名前付きでスレッドを作成
            _backgroundThread.Start();
            this.BackgroundHandler = new Handler(_backgroundThread.Looper);
        }
        private void StopBackgroundThread()
        {
            if (_backgroundThread == null)
                return;

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

        internal void StartCamera()
        {
            //アプリ起動時に,表示領域が未作成の前にStartCameraが実行されることを防ぐ
            if (this.SurfaceTexture == null)
                return;

            StartBackgroundThread();

            this._cameraManager = (CameraManager)Android.App.Application.Context.GetSystemService(Context.CameraService);

            var cameraIdList = this._cameraManager.GetCameraIdList();
            CameraCharacteristics cameraCharacteristics = null;
            //指定のカメラのidを取得する
            //フロント,バックのカメラidの取得についてはこちらを参考
            //https://bellsoft.jp/blog/system/detail_538
            this._cameraId = cameraIdList.FirstOrDefault(cId =>
            {
                cameraCharacteristics = _cameraManager.GetCameraCharacteristics(cId);
                var lensFacing = (int)cameraCharacteristics.Get(CameraCharacteristics.LensFacing);
                if (lensFacing == (int)this.CameraOption)
                    return true;
                return false;
            });
            Android.Hardware.Camera2.Params.StreamConfigurationMap scm = (Android.Hardware.Camera2.Params.StreamConfigurationMap)cameraCharacteristics.Get(CameraCharacteristics.ScalerStreamConfigurationMap);
            this._previewSize = scm.GetOutputSizes((int)ImageFormatType.Jpeg)[0];

            this.SensorOrientation = (int)cameraCharacteristics.Get(CameraCharacteristics.SensorOrientation);

            //ImageReaderの設定
            this.ImageReader = ImageReader.NewInstance(480, 640, ImageFormatType.Jpeg, 1);
            this.ImageReader.SetOnImageAvailableListener(this.ImageAvailableListener, this.BackgroundHandler);

            this._cameraStateListener = new CameraStateListener(this);
            _cameraManager.OpenCamera(_cameraId, this._cameraStateListener, null);
        }
        internal void StopCamera()
        {
            this.CaptureSession?.Close();
            this.CaptureSession = null;

            this.CameraDevice?.Close();
            this.CameraDevice = null;

            StopBackgroundThread();
        }

        /// <summary>
        /// CameraStateListenerのOnOpenedから呼び出される
        /// </summary>
        internal void CreateCameraPreviewSession()
        {
            try
            {
                if (this.SurfaceTexture == null)
                {
                    throw new IllegalStateException("SurfaceTexture is null");
                }

                this.SurfaceTexture.SetDefaultBufferSize(_previewSize.Width, _previewSize.Height);

                //プレビュー用
                this.PreviewRequestBuilder = this.CameraDevice.CreateCaptureRequest(CameraTemplate.Preview);
                Surface previewSurface = new Surface(this.SurfaceTexture);
                PreviewRequestBuilder.AddTarget(previewSurface);

                //キャプチャ用
                //this.StillCaptureBuilder = this.CameraDevice.CreateCaptureRequest(CameraTemplate.StillCapture);
                Surface readerSurface = this.ImageReader.Surface;
                PreviewRequestBuilder.AddTarget(readerSurface);

                List<Surface> surfaces = new List<Surface>();
                surfaces.Add(previewSurface);
                surfaces.Add(readerSurface);

                //CameraCaptureSessionを生成
                this.CameraDevice.CreateCaptureSession(surfaces, new CameraCaptureStateListener(this), BackgroundHandler);
            }
            catch (CameraAccessException ex)
            {
                ex.PrintStackTrace();
            }
        }
    }
}