using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;

namespace XFGetCameraData.Droid.CustomRenderers
{
    //http://spiratesta.hatenablog.com/entry/20111020/1319094221
    public class MyView : View
    {
        public MyView(Context context, IAttributeSet attrs) :
            base(context, attrs)
        {
            Initialize();
        }

        public MyView(Context context, IAttributeSet attrs, int defStyle) :
            base(context, attrs, defStyle)
        {
            Initialize();
        }

        private Paint _paint;
        private void Initialize()
        {
            _paint = new Paint();
        }

        private bool rotated=false;
        protected override void OnDraw(Canvas canvas)
        {
            base.OnDraw(canvas);
            //canvas.DrawColor(Color.Transparent, PorterDuff.Mode.Clear);
            //// 円
            //_paint.Color = Color.Argb(255, 255, 0, 255);
            //_paint.StrokeWidth = 30;
            //_paint.AntiAlias = true;
            //_paint.SetStyle(Paint.Style.Stroke);
            //// (x1,y1,r,paint) 中心x1座標, 中心y1座標, r半径
            //canvas.DrawCircle(130, 150, 100, _paint);


            //canvas.Translate(_x/2, _y/2);
            //canvas.Rotate(90);
            if (this._faces != null)
            {
                _paint.Color = Color.Argb(255, 255, 0, 255);
                _paint.StrokeWidth = 3;
                _paint.AntiAlias = true;
                _paint.SetStyle(Paint.Style.Stroke);



                foreach (var face in this._faces)
                {
                    //var rect = new Rect(face.Bounds.Top, (int)((_y - face.Bounds.Left) / _wRatio), face.Bounds.Bottom, _y - face.Bounds.Right);
                    //Back上下逆転･画像が90°
                    var rect = new Rect((int)((_y - face.Bounds.Top) * _wRatio), (int)((face.Bounds.Left) * _hRatio), (int)((_y - face.Bounds.Bottom) * _wRatio), (int)((face.Bounds.Right) * _hRatio));

                    //var rect = new Rect((int)((face.Bounds.Left) * _wRatio), (int)(face.Bounds.Top * _hRatio), (int)(face.Bounds.Right * _wRatio), (int)(face.Bounds.Bottom * _hRatio));
                    canvas.DrawRect(rect, _paint);
                }

            }
        }

        private Android.Hardware.Camera2.Params.Face[] _faces;
        private double _wRatio;
        private double _hRatio;
        private int _y;
        private int _x;
        public void ShowBoundsOnFace(Android.Hardware.Camera2.Params.Face[] faces, double wRatio, double hRatio, int x, int y)
        {
            this._faces = faces;
            this._wRatio = wRatio;
            this._hRatio = hRatio;
            this._y = y;
            this._x = x;
            Invalidate();//←再描画?
        }
    }
}