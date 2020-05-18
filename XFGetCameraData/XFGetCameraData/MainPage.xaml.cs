using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;
using XFGetCameraData.Services;

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

            CameraPreview2.PictureFinished += OnPictureFinished;

            this.Disappearing += (sender, e) =>
            {
                //画面が非表示の時はプレビューを止める
                this.CameraPreview2.IsPreviewing = false;
            };

            this.Appearing += async (sender, e) =>
            {

                if (await GetCameraPermission() != PermissionStatus.Granted)
                    return;

                //画面が表示されたらプレビューを開始する
                this.CameraPreview2.IsPreviewing = true;
            };
        }

        private void OnPictureFinished()
        {
            throw new NotImplementedException();
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

        private void Button_Clicked_1(object sender, EventArgs e)
        {
            CameraPreview2.IsPreviewing = !CameraPreview2.IsPreviewing;
        }
    }
}
