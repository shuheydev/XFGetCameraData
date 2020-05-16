﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace XFGetCameraData
{
    // Learn more about making custom code visible in the Xamarin.Forms previewer
    // by visiting https://aka.ms/xamarinforms-previewer
    [DesignTimeVisible(false)]
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();

            this.Disappearing += (sender, e) => {
                //画面が非表示の時はプレビューを止める
                this.CameraPreview.IsPreviewing = false;
            };

            this.Appearing += async (sender, e) => {

                if (await GetCameraPermission() != PermissionStatus.Granted)
                    return;

                //画面が表示されたらプレビューを開始する
                this.CameraPreview.IsPreviewing = true;
            };
        }

        async void Handle_Clicked(object sender, System.EventArgs e)
        {
            await Navigation.PushAsync(new ContentPage { Title = "空のページ" });
        }

        async Task<PermissionStatus> GetCameraPermission()
        {
            var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.Camera>();
            }

            return status;
        }
    }
}
