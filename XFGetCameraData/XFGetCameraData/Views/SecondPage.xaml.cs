﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using XFGetCameraData.CustomRenderers;
using XFGetCameraData.Services;

namespace XFGetCameraData.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class SecondPage : ContentPage
    {
        public SecondPage()
        {
            InitializeComponent();

            this.BindingContext = this;
        }

        private void Button_Clicked(object sender, EventArgs e)
        {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), $"Record_{DateTimeOffset.Now.LocalDateTime.ToString("yyyyMMddHHmmss")}.mp4");
            DependencyService.Get<IVideoService>().PrepareRecord(dir);
            DependencyService.Get<IVideoService>().StartRecord();

            var task = new Task(() =>
            {
                //20秒撮影している間待機
                Task.Delay(20000).Wait();

                //停止時にUIを操作する為、Device.BeginInvokeOnMainThreadで囲みます
                Device.BeginInvokeOnMainThread(() =>
                {
                    //撮影を停止します。
                    DependencyService.Get<IVideoService>().StopRecord();
                });
            });

            task.Start();
        }
    }
}