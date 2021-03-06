﻿using System;
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
    public class FaceBoundsView : View
    {
        public FaceBoundsView(Context context, IAttributeSet attrs) :
            base(context, attrs)
        {
            Initialize();
        }

        public FaceBoundsView(Context context, IAttributeSet attrs, int defStyle) :
            base(context, attrs, defStyle)
        {
            Initialize();
        }

        private Paint _paintRect;
        private Paint _paintText;

        private void Initialize()
        {
            _paintRect = new Paint();
            _paintText = new Paint();
        }

        /// <summary>
        /// ShowBoundsOnFace内のInvalidateで呼び出される.
        /// </summary>
        /// <param name="canvas"></param>
        protected override void OnDraw(Canvas canvas)
        {
            base.OnDraw(canvas);


            if (this._faces?.Length > 0)
            {
                //顔を囲む枠線の設定
                _paintRect.Color = Color.Argb(255, 255, 0, 255);
                _paintRect.StrokeWidth = 3;
                _paintRect.AntiAlias = true;
                _paintRect.SetStyle(Paint.Style.Stroke);

                //テキスト
                _paintText.Color = Color.Argb(255, 255, 0, 255);
                _paintText.StrokeWidth = 3;
                _paintText.AntiAlias = true;
                _paintText.TextSize = 50;
                _paintText.SetStyle(Paint.Style.FillAndStroke);

                foreach (var face in _faces)
                {
                    Rect rect = null;
                    //FrontとBackのカメラでは画像の向きが異なるので,
                    //それに合わせて調整している
                    //難しい...
                    //参考:https://qiita.com/ohwada/items/94105a7ea134ab4d2734
                    if (_sensorOrientation == 90)//Backカメラ
                        rect = new Rect((int)((_previewImageHeight - face.Bounds.Top) * _heightRatio),
                                        (int)(face.Bounds.Left * _widthRatio),
                                        (int)((_previewImageHeight - face.Bounds.Bottom) * _heightRatio),
                                        (int)(face.Bounds.Right * _widthRatio));
                    else if (_sensorOrientation == 270)//フロントカメラ
                        rect = new Rect((int)((_previewImageHeight - face.Bounds.Top) * _heightRatio),
                                        (int)((_previewImageWidth - face.Bounds.Left) * _widthRatio),
                                        (int)((_previewImageHeight - face.Bounds.Bottom) * _heightRatio),
                                        (int)((_previewImageWidth - face.Bounds.Right) * _widthRatio));

                    canvas.DrawRect(rect, _paintRect);

                    //テキストも描画したいな.
                    canvas.DrawText("hello", rect.Right, rect.Bottom, _paintText);
                }
            }
            else
            {
                canvas.DrawColor(Color.Transparent, PorterDuff.Mode.Clear);
            }
        }

        private Android.Hardware.Camera2.Params.Face[] _faces;

        private double _heightRatio;
        private double _widthRatio;
        private int _previewImageHeight;
        private int _previewImageWidth;
        private int _sensorOrientation;

        public void ShowBoundsOnFace(Android.Hardware.Camera2.Params.Face[] faces,
                                     int textureWidth,
                                     int textureHeight,
                                     int previewImageWidth,
                                     int previewImageHeight,
                                     int sensorOrientation)
        {
            _faces = faces;
            _previewImageHeight = previewImageHeight;
            _previewImageWidth = previewImageWidth;
            _sensorOrientation = sensorOrientation;

            //プレビューの画像は横になっているのでWidthとHeightを入れ替えて計算している
            _widthRatio = (double)textureHeight / previewImageWidth;
            _heightRatio = (double)textureWidth / previewImageHeight;

            Invalidate();//←再描画?
        }
    }
}