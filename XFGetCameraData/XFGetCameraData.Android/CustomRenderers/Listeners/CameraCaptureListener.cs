using Android.Gms.Vision.Faces;
using Android.Hardware.Camera2;
using Android.Hardware.Camera2.Params;
using Android.Runtime;
using Java.Lang;
using System;

namespace XFGetCameraData.Droid.CustomRenderers.Listeners
{
    //カメラのフレーム毎に発生するイベントのリスナー
    public class CameraCaptureSessionListener : CameraCaptureSession.CaptureCallback
    {
        private readonly DroidCameraPreview2 _owner;

        public long FrameNumber { get; private set; }

        public CameraCaptureSessionListener(DroidCameraPreview2 owner)
        {
            this._owner = owner;
        }

        public override void OnCaptureStarted(CameraCaptureSession session, CaptureRequest request, long timestamp, long frameNumber)
        {
            base.OnCaptureStarted(session, request, timestamp, frameNumber);
        }
        //毎フレームの処理
        public override void OnCaptureCompleted(CameraCaptureSession session, CaptureRequest request, TotalCaptureResult result)
        {
            base.OnCaptureCompleted(session, request, result);

            this._owner.CaptureResult = result;

            this.ProcessOnCapture();
        }
        public override void OnCaptureProgressed(CameraCaptureSession session, CaptureRequest request, CaptureResult partialResult)
        {
            base.OnCaptureProgressed(session, request, partialResult);

            this._owner.CaptureResult = partialResult;

            ProcessOnCapture();
            //Process(partialResult);
        }


        /// <summary>
        /// CameraCaptureStateListenerのOnCaptureCompleted,Progressedから呼び出される
        /// </summary>
        private void ProcessOnCapture()
        {
            this._owner.FrameCount = this._owner.CaptureResult.FrameNumber;

            //Face[]の取得方法
            //https://forums.xamarin.com/discussion/95912/xamarin-studio-android-face-detection-with-camera2
            var f = this._owner.CaptureResult?.Get(CaptureResult.StatisticsFaces);//Java.Lang.Objectが返ってくるので↓で変換する
            Android.Hardware.Camera2.Params.Face[] faces = f.ToArray<Android.Hardware.Camera2.Params.Face>();

            if (faces.Length <= 0)
                return;

            this._owner.FaceDetectBoundsView.ShowBoundsOnFace(faces,
                                                              this._owner.CameraTexture.Width,
                                                              this._owner.CameraTexture.Height,
                                                              this._owner.PreviewSize.Width,
                                                              this._owner.PreviewSize.Height,
                                                              this._owner.SensorOrientation);
        }

        //private void Process(CaptureResult result)
        //{
        //    switch (this._owner.CameraState)
        //    {
        //        case DroidCameraPreview2.STATE_WAITING_LOCK:
        //            {
        //                Integer afState = (Integer)result.Get(CaptureResult.ControlAfState);
        //                if (afState == null)
        //                {
        //                    this._owner.CameraState = DroidCameraPreview2.STATE_PICTURE_TAKEN; // avoids multiple picture callbacks
        //                    //this._owner.CaptureStillPicture();
        //                }

        //                else if ((((int)ControlAFState.FocusedLocked) == afState.IntValue()) ||
        //                           (((int)ControlAFState.NotFocusedLocked) == afState.IntValue()))
        //                {
        //                    // ControlAeState can be null on some devices
        //                    Integer aeState = (Integer)result.Get(CaptureResult.ControlAeState);
        //                    if (aeState == null ||
        //                            aeState.IntValue() == ((int)ControlAEState.Converged))
        //                    {
        //                        this._owner.CameraState = DroidCameraPreview2.STATE_PICTURE_TAKEN;
        //                        //this._owner.CaptureStillPicture();
        //                    }
        //                    else
        //                    {
        //                        //this._owner.RunPrecaptureSequence();
        //                    }
        //                }
        //                break;
        //            }
        //        case DroidCameraPreview2.STATE_WAITING_PRECAPTURE:
        //            {
        //                // ControlAeState can be null on some devices
        //                Integer aeState = (Integer)result.Get(CaptureResult.ControlAeState);
        //                if (aeState == null ||
        //                        aeState.IntValue() == ((int)ControlAEState.Precapture) ||
        //                        aeState.IntValue() == ((int)ControlAEState.FlashRequired))
        //                {
        //                    this._owner.CameraState = DroidCameraPreview2.STATE_WAITING_NON_PRECAPTURE;
        //                }
        //                break;
        //            }
        //        case DroidCameraPreview2.STATE_WAITING_NON_PRECAPTURE:
        //            {
        //                // ControlAeState can be null on some devices
        //                Integer aeState = (Integer)result.Get(CaptureResult.ControlAeState);
        //                if (aeState == null || aeState.IntValue() != ((int)ControlAEState.Precapture))
        //                {
        //                    this._owner.CameraState = DroidCameraPreview2.STATE_PICTURE_TAKEN;
        //                    //this._owner.CaptureStillPicture();
        //                }
        //                break;
        //            }
        //    }
        //}
    }
}