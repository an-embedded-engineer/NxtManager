using System;
using System.Collections;
using System.Diagnostics;
using System.IO.Ports;
using System.Text.RegularExpressions;
namespace NxtManagerV3
{
	/// <summary>
	/// Serial Port Control クラス
	/// </summary>
	public class PortControl : SerialPort
	{
		/// <summary>
		/// Serial Port Control コンストラクタ
		/// </summary>
		public PortControl()
			: base()
		{
			// シリアルポートのパラメータ設定
			this.BaudRate = 57600;         // BaudRate = 57600 bps
			this.Parity = Parity.None;     // Parity Check = None
			this.DataBits = 8;             // Data Bit = 8 bit
			this.StopBits = StopBits.One;  // Stop Bit = 1 bit
		}

		/// <summary>
		/// ソート済みのポート名の配列を取得
		/// </summary>
		/// <returns>ポート名の配列</returns>
		public static string[] GetSortedPortNames()
		{
			// ポート名一覧
			string[] portNames = SerialPort.GetPortNames();

			// ポート名の末尾のゴミを削除
			for (int i = 0; i < portNames.Length; i++)
			{
				if (portNames[i].Length > 5)
				{
					portNames[i] = portNames[i].Substring(0, 5);
				}
				portNames[i] = Regex.Replace(portNames[i], "[^0-9]+$", "");
			}

			// ポート名をポート番号でソート
			IComparer portNoComp = new PortNoCompare();
			Array.Sort(portNames, portNoComp);

			return portNames;
		}

		/// <summary>
		/// Serial Port 接続
		/// </summary>
		/// <param name="portName">ポート名</param>
		public void Connect(string portName)
		{
			try
			{
				// シリアルポート番号設定
				this.PortName = portName;

				// ポートのオープン
				this.Open();

				// 受信バッファの破棄
				this.DiscardInBuffer();

				// ハードウェアフロー制御
				this.DtrEnable = true;
				this.RtsEnable = true;

			}
			catch (Exception ex)
			{
				Debug.WriteLine("SERIAL PORT OPEN ERROR : {0}", ex.ToString());
			}
		}

		/// <summary>
		/// Serial Port 切断
		/// </summary>
		public void Disconnect()
		{
			try
			{
				// ハードウェアフロー制御
				this.RtsEnable = false;
				this.DtrEnable = false;

				// ポートのクローズ
				this.Close();
			}
			catch (Exception ex)
			{
				Debug.WriteLine("SERIAL PORT CLOSE ERROR : {0}", ex.ToString());
			}
		}

	}
}
