using System;
using System.Windows;

namespace NxtManagerV3
{
	/// <summary>
	/// Nxt Log クラス
	/// </summary>
	public class NxtLog
	{
		/// <summary>
		/// Log Data Member 文字列
		/// </summary>
		public static readonly String[] LogDataMember = 
		{
			"Time",
			"Data1",
			"Data2",
			"Battery",
			"Motor Rev A",
			"Motor Rev B",
			"Motor Rev C",
			"ADC S1",
			"ADC S2",
			"ADC S3",
			"ADC S4",
			"I2C"
		};

		// SPP(Bluetooth)のパケット長定義
		/// <summary>
		/// // Bluetooth Log Message のパケットヘッダ長
		/// </summary>
		public const UInt16 PacketHeaderLen = 2;
		/// <summary>
		/// // Bluetooth Log Message のパケットデータ長
		/// </summary>
		public const UInt16 PacketDataLen = 32;
		/// <summary>
		/// // Bluetooth Log Message のパケット長
		/// </summary>
		public const UInt16 PacketLen = PacketHeaderLen + PacketDataLen;

		// ログデータ
		/// <summary>
		/// システム時刻
		/// </summary>
		public UInt32 SysTick { get; private set; }
		/// <summary>
		/// データ左
		/// </summary>
		public SByte DataLeft { get; private set; }
		/// <summary>
		/// データ右
		/// </summary>
		public SByte DataRight { get; private set; }
		/// <summary>
		/// バッテリーレベル
		/// </summary>
		public UInt16 Batt { get; private set; }
		/// <summary>
		/// モータカウンタ0
		/// </summary>
		public Int32 MotorCnt0 { get; private set; }
		/// <summary>
		/// モータカウンタ1
		/// </summary>
		public Int32 MotorCnt1 { get; private set; }
		/// <summary>
		/// モータカウンタ2
		/// </summary>
		public Int32 MotorCnt2 { get; private set; }
		/// <summary>
		/// A/D センサ0
		/// </summary>
		public Int16 SensorAdc0 { get; private set; }
		/// <summary>
		/// A/D センサ1
		/// </summary>
		public Int16 SensorAdc1 { get; private set; }
		/// <summary>
		/// A/D センサ2
		/// </summary>
		public Int16 SensorAdc2 { get; private set; }
		/// <summary>
		/// A/D センサ3
		/// </summary>
		public Int16 SensorAdc3 { get; private set; }
		/// <summary>
		/// I2Cセンサ
		/// </summary>
		public Int32 I2c { get; private set; }

		/// <summary>
		/// 相対時刻(ログ取得開始時刻からの相対時刻)
		/// </summary>
		public UInt32 RelTick { get; private set; }

		/// <summary>
		/// 時刻オフセット(Nullable型)
		/// </summary>
		private static UInt32? TickOffset;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="packet">Packet Data</param>
		public NxtLog(byte[] packet)
		{
			// データ長が異なる
			if (packet.Length != PacketDataLen)
			{
				// 無効パケット長例外
				throw new InvalidPacketLengthException(string.Format("Packet Length : {0}", packet.Length));
			}

			// パケットをフィールドに変換
			this.SysTick = BitConverter.ToUInt32(packet, 0);
			this.DataLeft = (SByte)packet[4];
			this.DataRight = (SByte)packet[5];
			this.Batt = BitConverter.ToUInt16(packet, 6);
			this.MotorCnt0 = BitConverter.ToInt32(packet, 8);
			this.MotorCnt1 = BitConverter.ToInt32(packet, 12);
			this.MotorCnt2 = BitConverter.ToInt32(packet, 16);
			this.SensorAdc0 = BitConverter.ToInt16(packet, 20);
			this.SensorAdc1 = BitConverter.ToInt16(packet, 22);
			this.SensorAdc2 = BitConverter.ToInt16(packet, 24);
			this.SensorAdc3 = BitConverter.ToInt16(packet, 26);
			this.I2c = BitConverter.ToInt32(packet, 28);

			// オフセット時間が未初期化(null)ならば
			if (TickOffset == null)
			{
				// オフセット時間（ログ開始時のシステム時刻）セット
				TickOffset = this.SysTick;
			}

			// 相対時間（ログ開始時からの時刻）計算
			if (this.SysTick >= TickOffset)
			{
				// 通常の場合
				this.RelTick = this.SysTick - (UInt32)TickOffset;
			}
			else
			{
				// システム時刻が最大値を越えて一周した場合
				this.RelTick = this.SysTick + UInt32.MaxValue - (UInt32)TickOffset;
			}
		}

		/// <summary>
		/// CSV形式文字列に変換
		/// </summary>
		/// <returns>データ文字列(CSV形式)</returns>
		public string ToCsvString()
		{
			String str
				= Convert.ToString(this.RelTick) + ","
				+ Convert.ToString(this.DataLeft) + ","
				+ Convert.ToString(this.DataRight) + ","
				+ Convert.ToString(this.Batt) + ","
				+ Convert.ToString(this.MotorCnt0) + ","
				+ Convert.ToString(this.MotorCnt1) + ","
				+ Convert.ToString(this.MotorCnt2) + ","
				+ Convert.ToString(this.SensorAdc0) + ","
				+ Convert.ToString(this.SensorAdc1) + ","
				+ Convert.ToString(this.SensorAdc2) + ","
				+ Convert.ToString(this.SensorAdc3) + ","
				+ Convert.ToString(this.I2c) + "\r\n";

			return str;
		}

		/// <summary>
		/// 文字列に変換
		/// </summary>
		/// <returns>データ文字列</returns>
		public override string ToString()
		{
			string str
				= Convert.ToString(this.RelTick).PadLeft(6, ' ') + ","
				+ Convert.ToString(this.DataLeft).PadLeft(6, ' ') + ","
				+ Convert.ToString(this.DataRight).PadLeft(6, ' ') + ","
				+ Convert.ToString(this.Batt).PadLeft(8, ' ') + ","
				+ Convert.ToString(this.MotorCnt0).PadLeft(7, ' ') + ","
				+ Convert.ToString(this.MotorCnt1).PadLeft(7, ' ') + ","
				+ Convert.ToString(this.MotorCnt2).PadLeft(7, ' ') + ","
				+ Convert.ToString(this.SensorAdc0).PadLeft(5, ' ') + ","
				+ Convert.ToString(this.SensorAdc1).PadLeft(5, ' ') + ","
				+ Convert.ToString(this.SensorAdc2).PadLeft(5, ' ') + ","
				+ Convert.ToString(this.SensorAdc3).PadLeft(5, ' ') + ","
				+ Convert.ToString(this.I2c).PadLeft(4, ' ')
				+ "\r\n";

			return str;
		}

		/// <summary>
		/// 時系列データを取得
		/// </summary>
		/// <param name="name">系列名</param>
		/// <returns>時系列データ</returns>
		public Point GetTimeSeriesData(string name)
		{
			Point point = new Point();

			// X軸 = システム時刻
			point.X = (double)this.RelTick;

			// Y軸 = 系列名から取得したデータ
			point.Y = GetDataByName(name);

			return point;
		}

		/// <summary>
		/// 系列名からデータを取得
		/// </summary>
		/// <param name="name">系列名</param>
		/// <returns>ログデータ</returns>
		public double GetDataByName(string name)
		{
			double data = 0;
			switch (name)
			{
				case "Time":
					data = (double)this.RelTick;
					break;
				case "Data1":
					data = (double)this.DataLeft;
					break;
				case "Data2":
					data = (double)this.DataRight;
					break;
				case "Battery":
					data = (double)this.Batt;
					break;
				case "Motor Rev A":
					data = (double)this.MotorCnt0;
					break;
				case "Motor Rev B":
					data = (double)this.MotorCnt1;
					break;
				case "Motor Rev C":
					data = (double)this.MotorCnt2;
					break;
				case "ADC S1":
					data = (double)this.SensorAdc0;
					break;
				case "ADC S2":
					data = (double)this.SensorAdc1;
					break;
				case "ADC S3":
					data = (double)this.SensorAdc2;
					break;
				case "ADC S4":
					data = (double)this.SensorAdc3;
					break;
				case "I2C":
					data = (double)this.I2c;
					break;
				default:
					break;
			}
			return data;
		}

	}

	/// <summary>
	/// Invalid Packet Length Exception クラス
	/// </summary>
	[Serializable]
	public class InvalidPacketLengthException : Exception
	{
		/// <summary>
		/// Constructor
		/// </summary>
		public InvalidPacketLengthException() { }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="message">Message</param>
		public InvalidPacketLengthException(string message) : base(message) { }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="message">Message</param>
		/// <param name="inner">Inner Exception</param>
		public InvalidPacketLengthException(string message, Exception inner)
			: base(message)
		{
		}
	}
}
