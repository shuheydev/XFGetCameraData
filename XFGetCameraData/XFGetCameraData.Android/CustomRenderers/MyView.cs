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

        private bool rotated = false;
        protected override void OnDraw(Canvas canvas)
        {
            base.OnDraw(canvas);
            this._canvas = canvas;
            if (this._faces != null)
            {
                _paint.Color = Color.Argb(255, 255, 0, 255);
                _paint.StrokeWidth = 3;
                _paint.AntiAlias = true;
                _paint.SetStyle(Paint.Style.Stroke);

                canvas.Translate((float)(_y * _wRatio), 0);
                canvas.Rotate(90);

                foreach (var face in this._faces)
                {
                    Rect rect = null;
                    if (_sensorOrientation == 90)
                        rect = new Rect((int)((face.Bounds.Left) * _hRatio), (int)(face.Bounds.Top * _wRatio), (int)(face.Bounds.Right * _hRatio), (int)(face.Bounds.Bottom * _wRatio));
                    else if(_sensorOrientation==270)
                        rect = new Rect((int)((_x-face.Bounds.Left) * _hRatio), (int)((face.Bounds.Top) * _wRatio), (int)((_x-face.Bounds.Right) * _hRatio), (int)((face.Bounds.Bottom) * _wRatio));

                    canvas.DrawRect(rect, _paint);
                }
            }
            else
            {
                canvas.DrawColor(Color.Transparent, PorterDuff.Mode.Clear);
            }
        }

        private Android.Hardware.Camera2.Params.Face[] _faces;

        internal void ClearBounds()
        {
            //_canvas.DrawColor(Color.Transparent, PorterDuff.Mode.Clear);
            Invalidate();
        }

        private double _wRatio;
        private double _hRatio;
        private int _y;


        private int _x;
        private int _sensorOrientation;
        private Canvas _canvas;

        public void ShowBoundsOnFace(Android.Hardware.Camera2.Params.Face[] faces,
                                     double wRatio,
                                     double hRatio,
                                     int x,
                                     int y,
                                     int sensorOrientation)
        {
            this._faces = faces;
            this._wRatio = wRatio;
            this._hRatio = hRatio;
            this._y = y;
            this._x = x;
            this._sensorOrientation = sensorOrientation;
            Invalidate();//←再描画?
        }
    }
}