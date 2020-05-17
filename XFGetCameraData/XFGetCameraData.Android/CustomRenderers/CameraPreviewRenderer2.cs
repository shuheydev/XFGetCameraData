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

        public CameraPreviewRenderer2(Context context) : base(context)
        {
            this._context = context;
        }

        protected override void OnElementChanged(ElementChangedEventArgs<CameraPreview2> e)
        {
            base.OnElementChanged(e);

            _camera = new DroidCameraPreview2(this.Context);
            this.SetNativeControl(_camera);

            if (e.NewElement != null && _camera != null)
            {
                _currentElement = e.NewElement;
                
            }
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
        private CaptureRequest.Builder _previewBuilder;
        private CameraCaptureSession _previewSession;
        private CaptureRequest _previewRequest;

        public bool OpeningCamera { private get; set; }

        public DroidCameraPreview2(Context context) : base(context)
        {
            this._context = context;

            #region プレビュー用のViewを用意する.
            //予め用意しておいたレイアウトファイルを読み込む場合はこのようにする
            //この場合,Resource.LayoutにCameraLayout.xmlファイルを置いている.
            //中身はTextureViewのみ
            //var inflater = LayoutInflater.FromContext(context);
            //if (inflater == null)
            //    return;
            //var view = inflater.Inflate(Resource.Layout.CameraLayout, this);
            //_cameraTexture = view.FindViewById<TextureView>(Resource.Id.cameraTexture);

            //コードで作成する場合は以下のようにする
            _linearLayout = new Android.Widget.LinearLayout(context);
            _linearLayout.LayoutParameters = new ViewGroup.LayoutParams(
                ViewGroup.LayoutParams.MatchParent,
                ViewGroup.LayoutParams.MatchParent);
            ((MainActivity)context).AddContentView(_linearLayout,
                new ViewGroup.LayoutParams(
                    ViewGroup.LayoutParams.WrapContent,
                    ViewGroup.LayoutParams.WrapContent));
            _cameraTexture = new TextureView(context);
            _linearLayout.AddView(_cameraTexture, new ViewGroup.LayoutParams(
                ViewGroup.LayoutParams.WrapContent,
                ViewGroup.LayoutParams.WrapContent));
            #endregion


            _cameraTexture.SurfaceTextureListener = this;

            //OpenCameraするときに必要となる
            //カメラの状態に応じて行われる処理が記述されている.
            //これをCameraManagerに渡す
            _cameraStateListener = new CameraStateListener { Camera = this };

            //_cameraCaptureListener = new CameraCaptureStateListener(this);
        }


        public void OnSurfaceTextureAvailable(SurfaceTexture surface, int width, int height)
        {
            _viewsurface = surface;

            OpenCamera();
        }

        private void OpenCamera()
        {
            _cameraManager = (CameraManager)_context.GetSystemService(Context.CameraService);

            _cameraManager.OpenCamera("0", _cameraStateListener, null);

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
            return true;
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

            _previewBuilder.Set(CaptureRequest.ControlAfMode, (int)ControlAFMode.ContinuousPicture);

            _previewRequest = _previewBuilder.Build();
            _previewSession.SetRepeatingRequest(_previewRequest, null, null);
        }
    }
}