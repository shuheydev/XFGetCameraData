using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Gms.Tasks;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Firebase;
using Firebase.ML.Vision;
using Firebase.ML.Vision.Face;
using Xamarin.Forms.Internals;
using XFGetCameraData.Droid.CustomRenderers;

namespace XFGetCameraData.Droid.FirebaseML.Listeners
{
    public class DetectSuccessListener : Java.Lang.Object, IOnSuccessListener
    {
        private readonly DroidCameraPreview2 _owner;

        public DetectSuccessListener(DroidCameraPreview2 owner)
        {
            this._owner = owner;
        }

        public void OnSuccess(Java.Lang.Object result)
        {
            var faces = result as JavaList;
            if (faces.Count == 0)
            {
                this._owner.AndroidBitmap = this._owner.AndroidBitmap_Rotated;
                return;
            }

            #region Bitmapの顔領域に枠線を描画
            //枠線
            var paint = new Paint();
            paint.StrokeWidth = 3;
            paint.Color = Color.Red;
            paint.SetStyle(Paint.Style.Stroke);

            Canvas canvas = new Canvas(this._owner.AndroidBitmap_Rotated);
            //認識された顔の領域に枠線を描画
            foreach (FirebaseVisionFace face in faces)
            {
                var box = face.BoundingBox;
                canvas.DrawRect(box, paint);
            }

            this._owner.AndroidBitmap = this._owner.AndroidBitmap_Rotated;
            #endregion
        }
    }
}