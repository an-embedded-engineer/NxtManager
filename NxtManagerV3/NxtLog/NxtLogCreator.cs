using System;

namespace NxtManagerV3
{
	/// <summary>
	/// Message取得完了デリゲート
	/// </summary>
	public delegate void AppendMessegeDelegate(NxtLog  log);

	/// <summary>
	/// NXT Log Creator
	/// </summary>
	public class NxtLogCreator
	{
		/// <summary>
		///  Message取得完了デリゲート
		/// </summary>
		private AppendMessegeDelegate appendMessageDelegate;

		/// <summary>
		/// パケット先頭からのバイト数
		/// </summary>
		private UInt32 byteNo = 0;

		/// <summary>
		/// パケット ヘッダ部
		/// </summary>
		private Byte[] packetHeader = new Byte[NxtLog.PacketHeaderLen];

		/// <summary>
		/// パケット データ部
		/// </summary>
		private Byte[] packetData = new Byte[NxtLog.PacketDataLen];

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="appendMessageDelegate">Message取得完了デリゲート</param>
		public NxtLogCreator(AppendMessegeDelegate appendMessageDelegate)
		{
			this.appendMessageDelegate = appendMessageDelegate;
		}

		/// <summary>
        /// Log Message Data 生成
        /// </summary>
        /// <param name="data"></param>
		public void Append(Byte data)
		{
			// パケットヘッダ部取得
			if (byteNo < NxtLog.PacketHeaderLen)
			{
				// 受信したデータをヘッダ配列へ格納
				packetHeader[byteNo++] = data;

				if (byteNo == NxtLog.PacketHeaderLen)
				{
					// パケットヘッダ解析
					// パケットサイズのチェック
					UInt16 len = BitConverter.ToUInt16(packetHeader, 0);

					// ヘッダに格納された値がパケットデータ長と異なる
					if (len != NxtLog.PacketDataLen)
					{
						// １byte分を読み捨てる
						packetHeader[0] = packetHeader[1];
						byteNo = 1;
					}

				}
			}
			// パケットデータ
			else if (byteNo < NxtLog.PacketLen)
			{
				// 受信したデータをデータ配列へ格納
				packetData[byteNo++ - NxtLog.PacketHeaderLen] = data;

				if (byteNo == NxtLog.PacketLen)
				{
					// パケットをフィールドに変換
					NxtLog log = new NxtLog(packetData);

					// デリゲートを介してログデータ追加メソッドを呼び出し
					this.appendMessageDelegate.Invoke(log);

					// バイト番号を先頭に戻す
					byteNo = 0;
				}
			}
			else
			{
				// バイト番号を先頭に戻す
				byteNo = 0;
			}
		}

	}
}
