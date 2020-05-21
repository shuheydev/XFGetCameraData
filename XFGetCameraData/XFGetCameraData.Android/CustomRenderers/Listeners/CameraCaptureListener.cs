using Android.Hardware.Camera2;
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

            Process(result);
            //OnCaptureCompleted(EventArgs.Empty);

            ////Still撮影をする場合はこれを追加する.
            ////Captureメソッド実行でキャプチャされる
            //this._owner.StillCaptureRequest = this._owner.StillCaptureBuilder.Build();
            //this._owner.CaptureSession.Capture(this._owner.StillCaptureRequest,
            //                                   this._owner.CameraCaptureStillPictureSessionListener,
            //                                   this._owner.BackgroundHandler);
        }
        public override void OnCaptureProgressed(CameraCaptureSession session, CaptureRequest request, CaptureResult partialResult)
        {
            base.OnCaptureProgressed(session, request, partialResult);
            Process(partialResult);
        }
       
        private void Process(CaptureResult result)
        {
            this._owner.FrameCount = result.FrameNumber;

            switch (this._owner.CameraState)
            {
                case DroidCameraPreview2.STATE_WAITING_LOCK:
                    {
                        Integer afState = (Integer)result.Get(CaptureResult.ControlAfState);
                        if (afState == null)
                        {
                            this._owner.CameraState = DroidCameraPreview2.STATE_PICTURE_TAKEN; // avoids multiple picture callbacks
                            //this._owner.CaptureStillPicture();
                        }

                        else if ((((int)ControlAFState.FocusedLocked) == afState.IntValue()) ||
                                   (((int)ControlAFState.NotFocusedLocked) == afState.IntValue()))
                        {
                            // ControlAeState can be null on some devices
                            Integer aeState = (Integer)result.Get(CaptureResult.ControlAeState);
                            if (aeState == null ||
                                    aeState.IntValue() == ((int)ControlAEState.Converged))
                            {
                                this._owner.CameraState = DroidCameraPreview2.STATE_PICTURE_TAKEN;
                                //this._owner.CaptureStillPicture();
                            }
                            else
                            {
                                //this._owner.RunPrecaptureSequence();
                            }
                        }
                        break;
                    }
                case DroidCameraPreview2.STATE_WAITING_PRECAPTURE:
                    {
                        // ControlAeState can be null on some devices
                        Integer aeState = (Integer)result.Get(CaptureResult.ControlAeState);
                        if (aeState == null ||
                                aeState.IntValue() == ((int)ControlAEState.Precapture) ||
                                aeState.IntValue() == ((int)ControlAEState.FlashRequired))
                        {
                            this._owner.CameraState = DroidCameraPreview2.STATE_WAITING_NON_PRECAPTURE;
                        }
                        break;
                    }
                case DroidCameraPreview2.STATE_WAITING_NON_PRECAPTURE:
                    {
                        // ControlAeState can be null on some devices
                        Integer aeState = (Integer)result.Get(CaptureResult.ControlAeState);
                        if (aeState == null || aeState.IntValue() != ((int)ControlAEState.Precapture))
                        {
                            this._owner.CameraState = DroidCameraPreview2.STATE_PICTURE_TAKEN;
                            //this._owner.CaptureStillPicture();
                        }
                        break;
                    }
            }
        }

        public event EventHandler CaptureCompleted;
        protected virtual void OnCaptureCompleted(EventArgs e)
        {
            CaptureCompleted?.Invoke(this, e);
        }
    }
}