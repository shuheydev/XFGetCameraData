using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Hardware;
using Android.Hardware.Camera2;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Java.Lang;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;
using XFGetCameraData.CustomRenderers;
using XFGetCameraData.Droid.CustomRenderers;
using XFGetCameraData.Droid.CustomRenderers.Listeners;

[assembly: ExportRenderer(typeof(CameraPreview2), typeof(CameraPreviewRenderer2))]
namespace XFGetCameraData.Droid.CustomRenderers
{
    //これがカスタムレンダラー本体ね.
    //
    public class CameraPreviewRenderer2 : ViewRenderer<CameraPreview2, DroidCameraPreview2>
    {
        private readonly Context _context;
        private DroidCameraPreview2 _camera;
        private CameraPreview2 _currentElement;

        public long FrameNumber { get; private set; }

        public CameraPreviewRenderer2(Context context) : base(context)
        {
            this._context = context;
        }

        protected override void OnElementChanged(ElementChangedEventArgs<CameraPreview2> e)
        {
            base.OnElementChanged(e);

            _camera = new DroidCameraPreview2(this.Context);
            _camera.CaptureCompleted += _camera_CaptureCompleted;

            this.SetNativeControl(_camera);

            if (e.NewElement != null && _camera != null)
            {
                _currentElement = e.NewElement;
            }
        }

        private void _camera_CaptureCompleted(object sender, EventArgs e)
        {
            var s = sender as DroidCameraPreview2;
            this.FrameNumber = s.FrameNumber;
            _currentElement.FrameNumber = s.FrameNumber;
        }

        //アプリの非アクティブ化,復帰ができるようになった
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }

    public class DroidCameraPreview2 : FrameLayout, TextureView.ISurfaceTextureListener
    {
        private readonly Context _context;

        public Android.Widget.LinearLayout _linearLayout { get; }
        public CameraDevice CameraDevice { get; internal set; }

        private readonly TextureView _cameraTexture;
        private SurfaceTexture _viewsurface;
        private CameraManager _cameraManager;
        private string _cameraId;
        private CameraStateListener _cameraStateListener;
        private readonly CameraCaptureListener _cameraCaptureListener;
        private CaptureRequest.Builder _previewBuilder;
        private CameraCaptureSession _previewSession;
        private CaptureRequest _previewRequest;
        private HandlerThread _backgroundThread;
        private Handler _backgroundHandler;

        public bool OpeningCamera { private get; set; }
        public long FrameNumber { get; private set; }

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
            _cameraTexture.SurfaceTextureListener = this;

            //OpenCameraするときに必要となる
            //カメラの状態に応じて行われる処理が記述されている.
            //これをCameraManagerに渡す
            _cameraStateListener = new CameraStateListener { Camera = this };

            _cameraCaptureListener = new CameraCaptureListener(this);
            _cameraCaptureListener.CaptureCompleted += _cameraCaptureListener_CaptureCompleted;
            #endregion

            ////コードで作成する場合は以下のようにする
            //_linearLayout = new LinearLayout(context);
            //_linearLayout.LayoutParameters = new ViewGroup.LayoutParams(
            //                                     ViewGroup.LayoutParams.MatchParent,
            //                                     ViewGroup.LayoutParams.MatchParent);
            //_linearLayout.SetBackgroundColor(Android.Graphics.Color.White);
            //((MainActivity)context).AddContentView(_linearLayout,
            //                                new ViewGroup.LayoutParams(
            //                                    ViewGroup.LayoutParams.WrapContent,
            //                                    ViewGroup.LayoutParams.WrapContent));
            //_cameraTexture = new TextureView(context);
            //#region リスナーの登録
            //_cameraTexture.SurfaceTextureListener = this;

            ////OpenCameraするときに必要となる
            ////カメラの状態に応じて行われる処理が記述されている.
            ////これをCameraManagerに渡す
            //_cameraStateListener = new CameraStateListener { Camera = this };

            ////_cameraCaptureListener = new CameraCaptureStateListener(this);
            //#endregion

            #endregion


        }

        public event EventHandler CaptureCompleted;
        protected virtual void OnCaptureCompleted(EventArgs e)
        {
            CaptureCompleted?.Invoke(this, e);
        }

        private void _cameraCaptureListener_CaptureCompleted(object sender, EventArgs e)
        {
            var s = sender as CameraCaptureListener;
            this.FrameNumber = s.FrameNumber;
            OnCaptureCompleted(e);
        }

        public void OnSurfaceTextureAvailable(SurfaceTexture surface, int width, int height)
        {
            _viewsurface = surface;

            StartBackgroundThread();

            OpenCamera();
        }

        private void StartBackgroundThread()
        {
            _backgroundThread = new HandlerThread("CameraBackground");//名前付きでスレッドを作成
            _backgroundThread.Start();
            _backgroundHandler = new Handler(_backgroundThread.Looper);
        }

        private void OpenCamera()
        {
            _cameraManager = (CameraManager)_context.GetSystemService(Context.CameraService);

            _cameraManager.OpenCamera("0", _cameraStateListener, _backgroundHandler);

            // string[] cameraIds = _cameraManager.GetCameraIdList();

            // //0番目は殆どの場合リアカメラ
            // _cameraId = cameraIds[0];
            //for(int i=0;i<cameraIds.Length;i++)
            // {
            //     CameraCharacteristics chararc = _cameraManager.GetCameraCharacteristics(cameraIds[i]);

            //     var facing=(Integer)chararc.Get(CameraCharacteristics.LensFacing)
            // }
        }

        public bool OnSurfaceTextureDestroyed(SurfaceTexture surface)
        {
            StopBackgroundThread();

            return true;
        }

        private void StopBackgroundThread()
        {
            _backgroundThread.QuitSafely();
            try
            {
                _backgroundThread.Join();
                _backgroundThread = null;
                _backgroundHandler = null;
            }
            catch (InterruptedException ex)
            {
                ex.PrintStackTrace();
            }
        }

        public void OnSurfaceTextureSizeChanged(SurfaceTexture surface, int width, int height)
        {
        }

        public void OnSurfaceTextureUpdated(SurfaceTexture surface)
        {
        }

        internal void StartPreview()
        {
            if (CameraDevice == null || !_cameraTexture.IsAvailable)
                return;

            var texture = _cameraTexture.SurfaceTexture;
            //画像サイズを指定する.
            texture.SetDefaultBufferSize(640, 480);

            var surface = new Surface(texture);

            _previewBuilder = CameraDevice.CreateCaptureRequest(CameraTemplate.Preview);
            _previewBuilder.AddTarget(surface);

            List<Surface> surfaces = new List<Surface>();
            surfaces.Add(surface);

            CameraDevice.CreateCaptureSession(surfaces, new CameraCaptureStateListener
            {
                OnConfigureFailedAction = session => { },
                OnConfiguredAction = session =>
                {
                    _previewSession = session;
                    UpdatePreview();
                }
            },
            null);
        }

        private void UpdatePreview()
        {
            if (CameraDevice == null)
                return;

            //オートフォーカスの設定
            //https://qiita.com/ohwada/items/d33cd9c90abf3ec01f9e
            _previewBuilder.Set(CaptureRequest.ControlAfMode, (int)ControlAFMode.ContinuousPicture);
            //_previewBuilder.Set(CaptureRequest.ControlAfTrigger, (int)ControlAFTrigger.Start);

            _previewRequest = _previewBuilder.Build();
            //_cameraCaptureListenerで1フレームごとのキャプチャに対する処理を行う
            _previewSession.SetRepeatingRequest(_previewRequest, _cameraCaptureListener, _backgroundHandler);

        }
    }
}