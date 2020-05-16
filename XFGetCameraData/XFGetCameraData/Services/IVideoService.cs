using System;
using System.Collections.Generic;
using System.Text;

namespace XFGetCameraData.Services
{
    public interface IVideoService
    {
        void PrepareRecord(string saveFilePath);
        void StartRecord();
        void StopRecord();
    }
}
