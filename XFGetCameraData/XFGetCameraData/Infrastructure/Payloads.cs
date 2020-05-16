using System;
using System.Collections.Generic;
using System.Text;

namespace XFGetCameraData.Infrastructure
{
	//CustomRendererと通信するためのコンテナ
	public class LifeCyclePayload
	{
		public LifeCycle Status { get; set; }
	}
	public enum LifeCycle
	{
		OnStart,
		OnSleep,
		OnResume
	}
}
