# XFGetCameraData

カムさんのコードを写経した.Camera API.
https://github.com/muak/CameraPreviewSample

Camera2で実装するために色々調べた結果,2日かかった.疲れた.

Androidでのプレビューの表示と停止,再開できるようにした.

リスナーの階層が深くなってしまうのでなんとかしたい.例えばプレビューフレーム取得完了イベントのリスナーは3階層目にあるため,フレームの番号や,Bitmapデータを取得したい場合は,そこからイベントを連鎖させて一番上の階層までデータを運ぶことになる.

リスナーの階層が深くなるのは仕方がない.
以下の公式サンプルでは,各リスナーに一番上の階層のインスタンスをコンストラクタ経由で渡すことで,深い階層で取得したデータを一番上のインスタンスのプロパティに入れるようにしている.

https://github.com/xamarin/monodroid-samples/blob/master/android5.0/Camera2Basic/Camera2Basic/Camera2BasicFragment.cs

Camera2 APIを使う場合はこれがシンプルでいいと思った.



