using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Hardware.Camera2;
using Android.Media;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Xamarin.Forms;
using XFGetCameraData.Droid.Services;
using XFGetCameraData.Services;

[assembly:Xamarin.Forms.Dependency(typeof(VideoService))]
namespace XFGetCameraData.Droid.Services
{
    public class VideoService : IVideoService
    {
        #region "録画"

        private static MediaRecorder _recorder = null;
        private LinearLayout _linearLayout = null;
        private TextureView _textureView = null;
        private SurfaceTextureListener _listener = null;
        private bool _isRecording = false;

        public void PrepareRecord(string saveFilePath)
        {
            //MediaRecorderを設定します。
            this.SetUpMediaRecorder(saveFilePath);

            // MediaRecorderのプレビュー用のSurfaceViewを作成する
            var context = Forms.Context;

            //入力項目を格納するレイアウト
            _linearLayout = new LinearLayout(context);
            _linearLayout.LayoutParameters = new ViewGroup.LayoutParams(
                                                 ViewGroup.LayoutParams.MatchParent,
                                                 ViewGroup.LayoutParams.MatchParent);
            _linearLayout.SetBackgroundColor(Android.Graphics.Color.White);
            ((MainActivity)context).AddContentView(_linearLayout,
                                            new ViewGroup.LayoutParams(
                                                ViewGroup.LayoutParams.WrapContent,
                                                ViewGroup.LayoutParams.WrapContent));
            _textureView = new TextureView(context);
            _textureView.SurfaceTextureListener = new SurfaceTextureListener(_recorder);
            _linearLayout.AddView(_textureView, new ViewGroup.LayoutParams(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent));
        }

        private void SetUpMediaRecorder(string saveFilePath)
        {
            if (_recorder == null)
            {
                //MediaRecorderの設定
                _recorder = new MediaRecorder();

                // 入力ソースの設定
                _recorder.SetVideoSource(VideoSource.Surface);      // 録画の入力ソースを指定
                //_recorder.SetAudioSource(AudioSource.Mic);          // 音声の入力ソースを指定

                // ファイルフォーマットの設定
                _recorder.SetOutputFormat(OutputFormat.ThreeGpp);    // ファイルフォーマットを指定

                // エンコーダーの設定
                //_recorder.SetVideoEncoder(VideoEncoder.Mpeg4Sp);             // ビデオエンコーダを指定
                //_recorder.SetAudioEncoder(AudioEncoder.AmrNb);             // オーディオエンコーダを指定
                _recorder.SetVideoEncoder(VideoEncoder.H264);             // ビデオエンコーダを指定
                //_recorder.SetAudioEncoder(AudioEncoder.Aac);             // オーディオエンコーダを指定

                // 各種設定
                _recorder.SetOutputFile(saveFilePath);              // 動画の出力先となるファイルパスを指定
                _recorder.SetVideoEncodingBitRate(10000000);
                _recorder.SetVideoFrameRate(29);                    //信号機の点滅レートも30　 動画のフレームレートを指定
                _recorder.SetVideoSize(320, 240);                   // 動画のサイズを指定

                _recorder.Prepare();                                // 録画準備
            }
        }

        public void StartRecord()
        {
            if (_recorder != null)
            {
                _textureView.Visibility = ViewStates.Visible;

                // 録画開始
                _recorder.Start();

                _isRecording = true;
            }
        }

        public void StopRecord()
        {
            if (_isRecording)
            {
                if (_textureView != null)
                {
                    _textureView.Visibility = ViewStates.Invisible;
                }
                // 録画終了
                _recorder.Stop();
                _recorder.Reset();
                _recorder.Release();
                _recorder = null;

                if (_linearLayout != null)
                {
                    ((ViewGroup)_linearLayout.Parent).RemoveView(_linearLayout);
                    _linearLayout.Dispose();
                    _linearLayout = null;
                }

                if (_listener != null)
                {
                    _listener.StopCamera2();
                    _listener.Dispose();
                    _listener = null;
                }

                _isRecording = false;
            }
        }

        #endregion
    }

    public class SurfaceTextureListener :Java.Lang.Object, TextureView.ISurfaceTextureListener
    {
        #region "TextureView"

        private MediaRecorder _recorder = null;
        private CameraManager _manager = null;
        private CameraCallBack _callback = null;

        /// <summary>
        /// カメラ２デバイスを開始する
        /// </summary>
        /// <param name="surfaceTexture"></param>
        public SurfaceTextureListener(MediaRecorder recorder)
        {
            _recorder = recorder;
        }

        public void OpenCamera2(SurfaceTexture surfaceTexture)
        {
            //Camera2
            CameraManager manager = (CameraManager)Android.App.Application.Context.GetSystemService(Context.CameraService);
            //string cameraId = manager.GetCameraIdList().Where(r => manager.GetCameraCharacteristics(r).Get(CameraCharacteristics.LensFacing).ToString() == "").FirstOrDefault();
            string cameraId = manager.GetCameraIdList().FirstOrDefault();
            CameraCharacteristics cameraCharacteristics = manager.GetCameraCharacteristics(cameraId);
            Android.Hardware.Camera2.Params.StreamConfigurationMap scm = (Android.Hardware.Camera2.Params.StreamConfigurationMap)cameraCharacteristics.Get(CameraCharacteristics.ScalerStreamConfigurationMap);
            var previewSize = scm.GetOutputSizes((int)ImageFormatType.Jpeg)[0];
            manager.OpenCamera(cameraId, new CameraCallBack(_recorder, surfaceTexture, previewSize), null);
        }

        /// <summary>
        /// カメラ２デバイスを停止する
        /// </summary>
        public void StopCamera2()
        {
            if (_callback != null)
            {
                _callback.Disconnect();
                _callback.Dispose();
                _callback = null;
            }
            if (_manager != null)
            {
                _manager.Dispose();
                _manager = null;
            }
        }

        public void OnSurfaceTextureAvailable(SurfaceTexture surface, int width, int height)
        {
            this.OpenCamera2(surface);
        }

        public bool OnSurfaceTextureDestroyed(SurfaceTexture surface)
        {
            return true;
        }

        public void OnSurfaceTextureSizeChanged(SurfaceTexture surface, int width, int height)
        {
            //throw new NotImplementedException();
        }

        public void OnSurfaceTextureUpdated(SurfaceTexture surface)
        {
            //throw new NotImplementedException();
        }
        #endregion

    }

    public class CameraCallBack : CameraDevice.StateCallback
    {
        private MediaRecorder _recorder = null;
        private CameraDevice _cameraDevice = null;
        private SurfaceTexture _surfaceTexture = null;
        private CaptureRequest _captureRequest = null;
        private Android.Util.Size _previewSize = null;
        public CameraCallBack(MediaRecorder recorder, SurfaceTexture surfaceTexture, Android.Util.Size previewSize)
        {
            _recorder = recorder;
            _surfaceTexture = surfaceTexture;
            _previewSize = previewSize;
        }

        /// <summary>
        /// カメラを開放する
        /// </summary>
        public void Disconnect()
        {
            if (_cameraDevice != null)
            {
                _cameraDevice.Close();
                _cameraDevice = null;
            }
        }

        public override void OnDisconnected(CameraDevice camera)
        {
            //_cameraDevice.Close();
            //_cameraDevice = null;
        }

        public override void OnError(CameraDevice camera, Android.Hardware.Camera2.CameraError error)
        {
            //LogUtility.OutPutError(error.ToString());
            _cameraDevice.Close();
            _cameraDevice = null;
        }

        public override void OnOpened(CameraDevice camera)
        {
            _cameraDevice = camera;
            this.CreateCaptureSession();
        }

        private void CreateCaptureSession()
        {
            //SurfaceTexture texture = _textureView.SurfaceTexture; //エラーになる
            //バッファのサイズをプレビューサイズに設定(画面サイズ等適当な値を入れる)
            _surfaceTexture.SetDefaultBufferSize(_previewSize.Width, _previewSize.Height);
            Surface surface = new Surface(_surfaceTexture);

            List<Surface> list = new List<Surface>();
            list.Add(surface);
            list.Add(_recorder.Surface);

            CaptureRequest.Builder captureRequest = _cameraDevice.CreateCaptureRequest(CameraTemplate.Record);
            captureRequest.AddTarget(surface);
            captureRequest.AddTarget(_recorder.Surface);
            _captureRequest = captureRequest.Build();
            _cameraDevice.CreateCaptureSession(list, new CameraCaputureSessionCallBack(_captureRequest), null);
        }
    }

    //キャプチャセッションの状態取得
    public class CameraCaputureSessionCallBack : CameraCaptureSession.StateCallback
    {
        private CaptureRequest _captureRequest = null;

        public CameraCaputureSessionCallBack(CaptureRequest captureRequest)
        {
            _captureRequest = captureRequest;
        }

        public override void OnConfigured(CameraCaptureSession session)
        {
            session.SetRepeatingRequest(_captureRequest, new CameraCaptureSessionCallBack(), null);
            //session.StopRepeating();
            session.Capture(_captureRequest, new CameraCaptureSessionCallBack(), null);
        }

        public override void OnConfigureFailed(CameraCaptureSession session)
        {
            //throw new NotImplementedException();
        }
    }

    //キャプチャー開始
    public class CameraCaptureSessionCallBack : CameraCaptureSession.CaptureCallback
    {
        public override void OnCaptureStarted(CameraCaptureSession session, CaptureRequest request, long timestamp, long frameNumber)
        {
            base.OnCaptureStarted(session, request, timestamp, frameNumber);
        }

        public override void OnCaptureCompleted(CameraCaptureSession session, CaptureRequest request, TotalCaptureResult result)
        {
            base.OnCaptureCompleted(session, request, result);
        }
    }
}