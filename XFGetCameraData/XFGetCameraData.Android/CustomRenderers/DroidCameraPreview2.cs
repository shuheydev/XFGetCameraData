﻿using System;
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
using Java.Lang;
using XFGetCameraData.CustomRenderers;
using XFGetCameraData.Droid.CustomRenderers.Listeners;

namespace XFGetCameraData.Droid.CustomRenderers
{
    public class DroidCameraPreview2 : FrameLayout
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

        public const long UPDATE_FRAME_SPAN = 4;//例:64フレーム毎にFrameやBitmapプロパティを更新する.
        public static readonly SparseIntArray ORIENTATIONS = new SparseIntArray();

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