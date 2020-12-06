using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Hardware.Camera2;
using Android.Media;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using Firebase;
using Java.Lang;
using XFGetCameraData.CustomRenderers;
using XFGetCameraData.Droid.CustomRenderers;
using XFGetCameraData.Droid.CustomRenderers.Listeners;
using XFGetCameraData.Droid.FirebaseML.Listeners;
using XFGetCameraData.Services;

[assembly: Xamarin.Forms.Dependency(typeof(DroidCameraPreview2))]
namespace XFGetCameraData.Droid.CustomRenderers
{
    public class DroidCameraPreview2 : FrameLayout, IDroidCameraPreview2
    {
        //public Android.Widget.LinearLayout _linearLayout { get; }
        //public bool OpeningCamera { private get; set; }

        //public static readonly int REQUEST_CAMERA_PERMISSION = 1;
        //private static readonly string FRAGMENT_DIALOG = "dialog";

        //// Tag for the {@link Log}.
        //private static readonly string TAG = "Camera2BasicFragment";

        //// Camera state: Showing camera preview.
        //public const int STATE_PREVIEW = 0;

        //// Camera state: Waiting for the focus to be locked.
        //public const int STATE_WAITING_LOCK = 1;

        //// Camera state: Waiting for the exposure to be precapture state.
        //public const int STATE_WAITING_PRECAPTURE = 2;

        ////Camera state: Waiting for the exposure state to be something other than precapture.
        //public const int STATE_WAITING_NON_PRECAPTURE = 3;

        //// Camera state: Picture was taken.
        //public const int STATE_PICTURE_TAKEN = 4;



        //// Max preview width that is guaranteed by Camera2 API
        //private static readonly int MAX_PREVIEW_WIDTH = 1920;

        //// Max preview height that is guaranteed by Camera2 API
        //private static readonly int MAX_PREVIEW_HEIGHT = 1080;

        //public int CameraState = STATE_PREVIEW;

        internal const string BACKGROUND_THREAD_TAG = "CameraBackground";

        internal const long UPDATE_FRAME_SPAN = 32;//例:64フレーム毎にFrameやBitmapプロパティを更新する.
        internal static readonly SparseIntArray ORIENTATIONS = new SparseIntArray();

        #region Important
        private readonly Context _context;
        internal TextureView CameraTexture;

        public FaceBoundsView FaceDetectBoundsView { get; private set; }

        private HandlerThread _backgroundThread;
        internal Handler BackgroundHandler { get; set; }

        private string _cameraId;
        private CameraManager _cameraManager;

        public Android.Util.Size PreviewSize { get; internal set; }
        #endregion

        #region Listener
        internal CameraCaptureSessionListener CameraCaptureSessionListener { get; private set; }
        private CameraCaptureStillPictureSessionListener CameraCaptureStillPictureSessionListener { get; set; }
        private ImageAvailableListener ImageAvailableListener { get; set; }
        public DetectSuccessListener DetectSuccessListener { get; private set; }
        private CameraSurfaceTextureListener _surfaceTextureListener { get; set; }
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

                StopCamera();
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
            set
            {
                _jpegBytes = value;
                OnJpegBytesUpdated(EventArgs.Empty);
            }
        }
        private Android.Graphics.Bitmap _androidBitmap;
        internal Android.Graphics.Bitmap AndroidBitmap
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
        internal int SensorOrientation
        {
            get
            {
                return _sensorOrientation;
            }
            set
            {
                _sensorOrientation = value;
                OnSensorOrientationUpdated(EventArgs.Empty);
            }
        }
        #endregion

        #region Data which be set from Listener
        internal CameraDevice CameraDevice { get; set; }
        internal SurfaceTexture SurfaceTexture { get; set; }
        internal CameraCaptureSession CaptureSession { get; set; }
        internal ImageReader ImageReader { get; set; }
        internal CaptureResult CaptureResult { get; set; }
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
        internal CaptureRequest.Builder PreviewRequestBuilder { get; set; }
        internal CaptureRequest PreviewRequest { get; set; }
        internal CaptureRequest.Builder StillCaptureBuilder { get; set; }
        internal CaptureRequest StillCaptureRequest { get; set; }
        public FirebaseApp FirebaseApp { get; set; }
        public Bitmap AndroidBitmap_Rotated { get; internal set; }
        #endregion


        public DroidCameraPreview2(Context context) : base(context)
        {
            this._context = context;

            ORIENTATIONS.Append((int)SurfaceOrientation.Rotation0, 90);
            ORIENTATIONS.Append((int)SurfaceOrientation.Rotation90, 0);
            ORIENTATIONS.Append((int)SurfaceOrientation.Rotation180, 270);
            ORIENTATIONS.Append((int)SurfaceOrientation.Rotation270, 180);

            Initialize();
        }

        private void Initialize()
        {
            #region プレビュー用のViewを用意する.
            //予め用意しておいたレイアウトファイルを読み込む場合はこのようにする
            //この場合,Resource.LayoutにCameraLayout.xmlファイルを置いている.
            //中身はTextureViewのみ
            var inflater = LayoutInflater.FromContext(this._context);
            if (inflater == null)
                return;
            var view = inflater.Inflate(Resource.Layout.CameraLayout, this);
            CameraTexture = view.FindViewById<TextureView>(Resource.Id.cameraTexture);

            this.FaceDetectBoundsView = view.FindViewById<FaceBoundsView>(Resource.Id.faceDetectBounds);

            //リスナーの作成
            this.CameraCaptureSessionListener = new CameraCaptureSessionListener(this);
            this.CameraCaptureStillPictureSessionListener = new CameraCaptureStillPictureSessionListener(this);
            this.ImageAvailableListener = new ImageAvailableListener(this);
            this.DetectSuccessListener = new DetectSuccessListener(this);

            #region リスナーの登録
            this._surfaceTextureListener = new CameraSurfaceTextureListener(this);
            CameraTexture.SurfaceTextureListener = this._surfaceTextureListener;
            #endregion
            #endregion

            //MLKit用の初期化
            //FirebaseAppを生成するには以下のようにする.
            //クラウドの機能を使わず,ローカルだけでの顔検出なので,
            //Firebaseでプロジェクトを作成する必要はない.
            //ApplicationIdに適当な文字列を設定し,opsionを生成し,
            //FirebaseApp.InitializeAppメソッドにわたす.
            var options = new FirebaseOptions.Builder()
                .SetApplicationId("testApp")
                .Build();
            this.FirebaseApp = FirebaseApp.InitializeApp(this._context, options);
        }

        private void StartBackgroundThread()
        {
            _backgroundThread = new HandlerThread(BACKGROUND_THREAD_TAG);//名前付きでスレッドを作成
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

        public void StartCamera()
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
            this.PreviewSize = scm.GetOutputSizes((int)ImageFormatType.Jpeg)[0];

            this.SensorOrientation = (int)cameraCharacteristics.Get(CameraCharacteristics.SensorOrientation);//Back:4032*3024,Front:3264*2448

            //ImageReaderの設定
            this.ImageReader = ImageReader.NewInstance(480, 640, ImageFormatType.Jpeg, 1);
            this.ImageReader.SetOnImageAvailableListener(this.ImageAvailableListener, this.BackgroundHandler);

            this._cameraStateListener = new CameraStateListener(this);

            _cameraManager.OpenCamera(_cameraId, this._cameraStateListener, null);
        }
        public void StopCamera()
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

                this.SurfaceTexture.SetDefaultBufferSize(PreviewSize.Width, PreviewSize.Height);

                //プレビュー用のRequestBuilderを作成
                this.PreviewRequestBuilder = this.CameraDevice.CreateCaptureRequest(CameraTemplate.Preview);
                Surface previewSurface = new Surface(this.SurfaceTexture);
                PreviewRequestBuilder.AddTarget(previewSurface);

                //キャプチャ用
                //プレビューキャプチャと同じタイミングで動作させるのではなく,独立させたい場合は,RequestBuilderを別に作成する.
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