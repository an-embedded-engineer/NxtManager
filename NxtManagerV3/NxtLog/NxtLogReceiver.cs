using System;
using System.Diagnostics;
using System.IO.Ports;
using System.Windows.Threading;

namespace NxtManagerV3
{
	/// <summary>
	/// NXT Log Receiver
	/// </summary>
	public class NxtLogReceiver
	{
		/// <summary>
		/// メインスレッドへのログデータ出力デリゲート
		/// </summary>
		/// <param name="data">ログデータ</param>
		private delegate void LogOutputDelegate(Byte[] data);

		/// <summary>
		/// メッセージ受信完了デリゲート
		/// </summary>
		private AppendMessegeDelegate appendMessageDelegate;

		/// <summary>
		/// NXT Log Creator
		/// </summary>
		private NxtLogCreator logCreator;

		/// <summary>
		/// 呼び出し元スレッド
		/// </summary>
		private Dispatcher dispatcher;

		/// <summary>
		/// Serial Port Control
		/// </summary>
		public PortControl Port { get; private set; }
		
		/// <summary>
		/// Serial Port接続状態
		/// </summary>
		public bool IsConnected 
		{
			get
			{
				return this.Port.IsOpen;
			}
		}

		/// <summary>
		/// Serial PortのClear To Sendライン状態
		/// </summary>
		public bool CtsHolding
		{
			get
			{
				return this.Port.CtsHolding;
			}
		}

		/// <summary>
		/// Serial PortのData Set Readyライン状態
		/// </summary>
		public bool DsrHolding
		{
			get
			{
				return this.Port.DsrHolding;
			}
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="dispatcher">呼び出し元スレッド</param>
		/// <param name="appendMessageDelegate">メッセージ受信完了デリゲート</param>
		public NxtLogReceiver(Dispatcher dispatcher, AppendMessegeDelegate appendMessageDelegate)
		{
			this.dispatcher = dispatcher;
			this.appendMessageDelegate = appendMessageDelegate;

			// Port Controlを初期化
			this.Port = new PortControl();
			// データ受信イベントハンドラ
			this.Port.DataReceived += new SerialDataReceivedEventHandler(Port_DataReceived);

			// NXT Log Creatorを生成
			logCreator = new NxtLogCreator(appendMessageDelegate);
		}

		/// <summary>
		/// Serial Portに接続
		/// </summary>
		/// <param name="portName">ポート番号</param>
		public void Connect(string portName)
		{
			// Serial Port 接続
			this.Port.Connect(portName);
		}

		/// <summary>
		/// Serial Portから切断
		/// </summary>
		public void Disconnect()
		{
			// Serial Port 接続
			this.Port.Disconnect();
		}

		/// <summary>
		/// （メインスレッドの）ログデータ受信
		/// </summary>
		/// <param name="data">ログデータ</param>
		private void messegeReceive(Byte[] data)
		{
			// 受信したログデータをNxtLogCreatorに渡す
			for (int i = 0; i < data.Length; i++)
			{
				logCreator.Append(data[i]);
			}
		}

		/// <summary>
		/// シリアルデータ受信イベント
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void Port_DataReceived(object sender, SerialDataReceivedEventArgs e)
		{
			// ログデータ受信処理を登録
			LogOutputDelegate logOutput = new LogOutputDelegate(messegeReceive);

			// データ受信バッファ
			Byte[] buf = new byte[this.Port.BytesToRead];

			// 受信データが存在する
			if (buf.Length > 0)
			{
				try
				{
					// Serial Portより受信
					this.Port.Read(buf, 0, buf.Length);

					// 受信データをメインスレッドへ
					dispatcher.BeginInvoke(logOutput, buf);
				}
				catch (Exception ex)
				{
					Debug.WriteLine("SERIAL DATA RECEIVE EXCEPTION : {0}", ex.ToString());
				}
			}
		}
	}
}
