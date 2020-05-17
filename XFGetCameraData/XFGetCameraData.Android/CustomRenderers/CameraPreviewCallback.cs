using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using XFGetCameraData.CustomRenderers;
using static Android.Hardware.Camera;

namespace XFGetCameraData.Droid.CustomRenderers
{
    public class CameraPreviewCallback : Java.Lang.Object, IPreviewCallback
    {
        private long FrameCount = 1;
        public CameraPreview CameraPreview { get; set; }
        public byte[] Buff { get; set; }

        public void OnPreviewFrame(byte[] data, Android.Hardware.Camera camera)
        {

            //ここでフレーム画像データを加工したり情報を取得したり

            //PCLプロジェクトとのやりとりやら
            CameraPreview.Hoge = (object)(this.FrameCount++.ToString());

            //変更した画像をプレビューに反映させたりする

            //次のバッファをセット
            camera.AddCallbackBuffer(Buff);
        }
    }
}