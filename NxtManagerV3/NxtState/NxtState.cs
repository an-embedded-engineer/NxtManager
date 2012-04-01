using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Threading;
using System.IO;
using System.Diagnostics;

namespace NxtManagerV3
{
	/// <summary>
	/// NXT Mortor Class
	/// </summary>
	/// <typeparam name="T">データ型</typeparam>
	public class NxtMotor<T>
	{
		/// <summary>
		/// 右モータ
		/// </summary>
		public T R { get; private set; }
		/// <summary>
		/// 左モータ
		/// </summary>
		public T L { get; private set; }

		/// <summary>
		/// Constructor
		/// </summary>
		public NxtMotor()
		{
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="r">右モータ値</param>
		/// <param name="l">左モータ値</param>
		public NxtMotor(T r, T l)
		{
			this.R = r;
			this.L = l;
		}

	}


	public class NxtState : ICloneable
	{
		/// <summary>
		/// タイヤ半径
		/// </summary>
		public const double TireRadius = 42.0;
		/// <summary>
		/// 車軸長
		/// </summary>
		public const double AxleLen = 161.0;
		/// <summary>
		/// 角速度オフセット
		/// </summary>
		public const double AngularVelocityOffset = 593.0;

		/// <summary>
		/// 
		/// </summary>
		public static readonly String[] NxtStateDataMember = 
        {
            "Run Time",
            "Move Distance",
            "Move Speed",
			"Direction",
			"Turn Angle",
            "Postural Sway",
            "Tail Angle",
			"Position Coordinate"
        };

		/// <summary>
		/// 走行時間
		/// </summary>
		public uint RunTime { get; private set; }

		/// <summary>
		/// 瞬間走行時間
		/// </summary>
		public uint MomentRunTime { get; private set; }

		/// <summary>
		/// 走行距離
		/// </summary>
		public double Distance { get; private set; }

		/// <summary>
		/// 瞬間走行距離
		/// </summary>
		public double MomentDistance { get; private set; }

		/// <summary>
		/// 平均走行速度
		/// </summary>
		public double AverageSpd { get; private set; }

		/// <summary>
		/// 瞬間平均走行速度
		/// </summary>
		public double MomentAverageSpd { get; private set; }

		/// <summary>
		/// 車体傾斜
		/// </summary>
		public double Posture { get; private set; }

		/// <summary>
		/// 尻尾角度
		/// </summary>
		public double TailAngle { get; private set; }

		/// <summary>
		/// 尻尾回転角度
		/// </summary>
		public double TailTurnAngle { get; private set; }

		/// <summary>
		/// 瞬間尻尾回転角度
		/// </summary>
		public double MomentTailTurnAngle { get; private set; }

		/// <summary>
		/// 車体方向
		/// </summary>
		public double Direction { get; private set; }

		/// <summary>
		/// ターン角度
		/// </summary>
		public double TurnAngle { get; private set; }

		/// <summary>
		/// モータ速度
		/// </summary>
		public NxtMotor<int> MotorSpd { get; private set; }

		/// <summary>
		/// 位置座標
		/// </summary>
		public Point Position { get; private set; }

		/// <summary>
		/// 位置座標
		/// </summary>
		public Point StartPosition { get; private set; }

		/// <summary>
		/// 開始方向
		/// </summary>
		private double startDirection;

		// 開始時間
		private uint startTime;

		// モータエンコーダ開始値
		private NxtMotor<double> motorEncoderStart;

		// 瞬間モータ移動距離
		private NxtMotor<double> momentMotorDist;

		// モータ回転角度(2回分)
		private RingBuffer<NxtMotor<double>> motorRotateAngleLog = new RingBuffer<NxtMotor<double>>(2);

		// 移動時間ログ(2個)
		private RingBuffer<uint> RunTimeLog = new RingBuffer<uint>(2);

		// 位置座標
		private Point position = new Point();

		// ターン角度
		private double turnAngle;

		// 尻尾開始角度
		private double tailStartAngle;

		// 尻尾角度ログ(2回分)
		private RingBuffer<double> tailAngleLog = new RingBuffer<double>(2);

		// 傾斜更新状態
		enum PostureState
		{
			Init,
			Ready,
		}

		// ジャイロセンサ値(16回分)
		private RingBuffer<double> gyroBuffer = new RingBuffer<double>(16);
		
		// 角速度ログ(2回分)
		private RingBuffer<double> anglarVelocity = new RingBuffer<double>(2);

		// 角度更新値(16回分)
		private RingBuffer<double> deltaBuffer = new RingBuffer<double>(16);
		
		// ジャイロセンサ値
		private double currentGyro = 0;
		
		// オフセット
		private double offset = 0;
		
		// 傾斜
		private double posture = 0;

		// 傾斜更新状態
		private PostureState init_sate = PostureState.Init;
		
		// 不感帯閾値
		private const double DeadZoneThreshold = 0.1;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="startPos">開始位置</param>
		public NxtState(Point startPos, double startDirection)
		{
			// 開始位置をセット
			this.Position = startPos;
			this.StartPosition = startPos;
			this.startDirection = startDirection;
#if DEBUG_DUMP 
			using (StreamWriter sw = new StreamWriter(new FileStream("posture_dump.csv", FileMode.Create)))
			{
				string rec = string.Format("Gyro, Offset, AnglarVelocity, Delta, Posture\r\n");

				sw.Write(rec);
			}
#endif
		}

		/// <summary>
		/// 状態更新
		/// </summary>
		/// <param name="log">NXT Log</param>
		public void UpdateState(NxtLog log)
		{
			UpdateTime(log);
			UpdateDist(log);
			UpdateSpeed();
			UpdateDirection();
			UpdatePosition();
			UpdateTaileAngle(log);
			UpdatePosture(log);
		}

		private void UpdateTime(NxtLog log)
		{
			uint time = 0;
			uint momentTime = 0;

			// 1回目
			if (RunTimeLog.Count == 0)
			{
				// 開始時間を設定
				startTime = log.RelTick;
			}
			// 2回目以降
			else
			{
				// 走行時間T[ms] = システム時刻[ms] - 開始時間T0[ms]
				time = log.RelTick - startTime;

				// 瞬間走行時間T_m[ms] = Tn[ms] - Tn-1[ms]
				momentTime = time - RunTimeLog[0];
			}

			// 走行時間をバックアップ
			RunTimeLog.Append(time);

			// プロパティにセット
			this.RunTime = time;
			this.MomentRunTime = momentTime;
		}

		private void UpdateDist(NxtLog log)
		{
			NxtMotor<double> angle = new NxtMotor<double>();
			NxtMotor<double> momentAngle = new NxtMotor<double>();
			double dist = 0;
			double mDist = 0;

			// 1回目
			if (motorRotateAngleLog.Count == 0)
			{
				// 開始モータカウント値をセット
				motorEncoderStart = new NxtMotor<double>(log.MotorCnt1, log.MotorCnt2);
				// 瞬間モータ移動距離をリセット
				momentMotorDist = new NxtMotor<double>();
			}
			// 2回目以降
			else
			{
				// モータ回転角度A[deg] = モータカウントCn[deg] - 開始カウント値C0[deg]
				double angleR = log.MotorCnt1 - motorEncoderStart.R;
				double angleL = log.MotorCnt2 - motorEncoderStart.L;
				angle = new NxtMotor<double>(angleR, angleL);

				// モータ移動距離MD[mm] = ( 2π * R[mm] ) * (A[deg]/360)
				// R = タイヤ半径
				double distR = (2 * Math.PI * TireRadius) * (angleR / 360);
				double distL = (2 * Math.PI * TireRadius) * (angleL / 360);

				// 移動距離D[mm] = (MD_R[mm] + MD_L[mm]) / 2
				dist = (distR + distL) / 2;

				// 瞬間モータ回転角度A_m[deg] = Cn[deg] - Cn-1[deg]
				double mAngleR = angleR - motorRotateAngleLog[0].R;
				double mAngleL = angleL - motorRotateAngleLog[0].L;

				// 瞬間モータ移動距離MD_m[mm] = ( 2π * R[mm] ) * (A_m[deg]/360)
				// R = タイヤ半径
				double mDistR = (2 * Math.PI * TireRadius) * (mAngleR / 360);
				double mDistL = (2 * Math.PI * TireRadius) * (mAngleL / 360);
				this.momentMotorDist = new NxtMotor<double>(mDistR, mDistL);

				// 移動距離D_m[mm] = (MD_R_m[mm] + MD_L_m[mm]) / 2
				mDist = (mDistR + mDistL) / 2;

			}

			// モータ回転角度をバックアップ
			motorRotateAngleLog.Append(angle);

			// プロパティにセット
			this.Distance = dist;
			this.MomentDistance = mDist;
		}

		private void UpdateSpeed()
		{
			// 走行時間が0以下
			if (this.RunTime <= 0)
			{
				// 平均速度[mm/sec] = 0
				this.AverageSpd = 0;
			}
			else
			{
				// 平均速度[mm/sec] = (走行距離[mm] / 走行時間[ms]) * 1000
				this.AverageSpd = (this.Distance / this.RunTime) * 1000;
			}

			// 瞬間走行時間が0以下
			if (this.MomentRunTime <= 0)
			{
				// 瞬間平均速度[mm/sec] = 0
				this.MomentAverageSpd = 0;
			}
			else
			{
				// 瞬間平均速度[mm/sec] = (瞬間走行距離[mm] / 瞬間走行時間[ms]) * 1000
				this.MomentAverageSpd = (this.MomentDistance / this.MomentRunTime) * 1000;
			}
		}

		private void UpdateDirection()
		{
			double direction = startDirection;

			// モータ回転角度を取得
			NxtMotor<double> rotateAngle = motorRotateAngleLog[0];

			// 方向 = 開始方向 + ((タイヤ半径[mm] / 車軸長[mm]) * (右モータ回転角度[deg] - 左モータ回転角度[deg]))
			direction += (TireRadius / AxleLen) * (rotateAngle.R - rotateAngle.L);

			// 方向がマイナスの間
			while (direction < 0)
			{
				// 方向を正に補正
				direction += 360;
			}

			// 方向を0～360[deg]で正規化
			direction %= 360;

			// プロパティをセット
			this.Direction = direction;
		}

		private void UpdatePosition()
		{
			// 瞬間ターン角度[deg] = (瞬間右モータ移動距離[mm] - 瞬間左モータ移動距離[mm]) / 車軸長[mm] 
			double momentTurnAngle = Util.RadianToDegree((momentMotorDist.R - momentMotorDist.L) / AxleLen);

			// ターン角度[deg] = 前回ターン角度[deg] + (瞬間ターン角度[deg] / 2);
			double deg = this.TurnAngle + (momentTurnAngle / 2);
			double rad = Util.DegreeToRadian(deg);

			Point delta = new Point();
			// ΔX = 瞬間移動距離[mm] * cos(ターン角度[rad])
			delta.X = this.MomentDistance * Math.Cos(rad);
			// ΔY = 瞬間移動距離[mm] * sin(ターン角度[rad])
			delta.Y = this.MomentDistance * Math.Sin(rad);

			// 位置座標を更新
			this.position.X += delta.X;
			this.position.Y += delta.Y;

			// ターン角度を更新
			this.turnAngle += momentTurnAngle;

			// プロパティを更新
			this.Position = this.position;
			this.TurnAngle = this.turnAngle;
		}

		private void UpdateTaileAngle(NxtLog log)
		{
			double angleDelta = 0;
			double tailAngle = 0;
			double tailTurnAngle = 0;

			// 1回目
			if (tailAngleLog.Count == 0)
			{
				// 尻尾初期角度[deg] = モータカウント[deg]
				tailStartAngle = log.MotorCnt0;
			}
			else
			{
				// 尻尾瞬間回転角度[deg] = モータカウント[deg] - 前回尻尾角度[deg]
				angleDelta = log.MotorCnt0 - tailAngleLog[0];
				// 尻尾回転角度[deg] = モータカウント[deg] - 尻尾初期角度[deg]
				tailTurnAngle = log.MotorCnt0 - tailStartAngle;
			}

			// 尻尾角度[deg] = モータカウント[deg]
			tailAngle = log.MotorCnt0;

			// 正の値に正規化
			while (tailAngle < 0)
			{
				tailAngle += 360;
			}

			// 0～360度の範囲に正規化
			tailAngle %= 360;

			// 尻尾角度をバックアップ
			tailAngleLog.Append(tailAngle);

			// プロパティ2セット
			this.TailAngle = tailAngle;
			this.TailTurnAngle = tailTurnAngle;
			this.MomentTailTurnAngle = angleDelta;
		}

		

		
		private void UpdatePosture(NxtLog log)
		{
			double av = 0;
			double delta = 0;

			// センサ値を取得
			currentGyro = log.SensorAdc0;
			// センサ値をバッファに格納
			gyroBuffer.Append(currentGyro);

			// 初期化中
			switch(init_sate)
			{
				case PostureState.Init:
					// 初期化中は角度0
					posture = 0;
					// バッファがいっぱいになった
					if (gyroBuffer.Length == gyroBuffer.Count)
					{
						// ジャイロ平均値をオフセットに設定
						offset = gyroBuffer.Average();

						// 角速度を計算
						av = currentGyro - offset;
						anglarVelocity.Append(av);

						// Ready 状態に遷移
						init_sate = PostureState.Ready;
					}
					break;
				case PostureState.Ready:
					// 角速度を計算
					//av = currentGyro - offset;
					av = gyroBuffer.Average() - offset;
					anglarVelocity.Append(av);

					// Δt = 瞬間移動時間[msec] / 1000
					double dt = ((double)this.MomentRunTime / 1000);

					// 台形公式により、面積を計算
					// ΔP =( (AVn + AVn-1) * Δt) / 2
					delta = ((anglarVelocity[0] + anglarVelocity[1]) * dt) / 2;

					// 不感帯を設定
					if (delta <= DeadZoneThreshold && delta >= -DeadZoneThreshold)
					{
						delta = 0;
					}

					// 傾斜を更新
					posture += delta;

					deltaBuffer.Append(delta);

					// 傾斜の更新値が一定時間ほぼ0 = 直立状態
					if (-0.05 <= av && av <= 0.05)
					{
						posture = 0;
					}

					break;
			}

#if DEBUG_DUMP 
			string rec = string.Format("{0}, {1}, {2}, {3}, {4}\r\n", currentGyro, offset, anglarVelocity[0], delta, posture);


			// ログファイルを開く
			using (StreamWriter sw = new StreamWriter(new FileStream("posture_dump.csv", FileMode.Append)))
			{
				try
				{
					// ファイルへ追記
					sw.Write(rec);
				}
				catch (Exception ex)
				{
					Debug.WriteLine("FILE WRITE ERROR : {0}", ex.ToString());
				}
			}
#endif
			this.Posture = posture;

		}


		/// <summary>
		/// 時系列データを取得
		/// </summary>
		/// <param name="name">系列名</param>
		/// <returns>時系列データ</returns>
		public Point GetTimeSeriesData(string name)
		{
			Point point = new Point();

			if (name == "Position Coordinate")
			{
				point = this.Position;
			}
			else
			{
				// X軸 = システム時刻
				point.X = (double)this.RunTime;
				// Y軸 = 系列名から取得したデータ
				point.Y = GetDataByName(name);
			}

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
				case "Move Distance":
					data = (double)this.Distance;
					break;
				case "Move Speed":
					data = (double)this.AverageSpd;
					break;
				case "Postural Sway":
					data = (double)this.Posture;
					break;
				case "Direction":
					data = (double)this.Direction;
					break;
				case "Turn Angle":
					data = (double)this.TurnAngle;
					break;
			    case "Tail Angle":
					data = (double)this.TailAngle;
					break;
				default:
					break;
			}
			return data;
		}

		/// <summary>
		/// 文字列に変換(","区切り)
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			StringBuilder builder = new StringBuilder();

			string txtRunTime = string.Format("{0}", this.RunTime);
			string txtDistance = string.Format("{0:F2}", this.Distance);
			string txtSpeed = string.Format("{0:F2}", this.AverageSpd);
			string txtDirection = string.Format("{0:F2}", this.Direction);
			string txtTurnAngle = string.Format("{0:F2}", this.TurnAngle);
			string txtPosture = string.Format("{0:F2}", this.Posture);
			string txtTailAngle = string.Format("{0:F2}", this.TailAngle);
			string txtPosX = string.Format("{0:F2}", this.Position.X);
			string txtPosY = string.Format("{0:F2}", this.Position.Y);
			
			builder.Append(txtRunTime.PadLeft(9, ' ') + ",");
			builder.Append(txtDistance.PadLeft(9, ' ') + ",");
			builder.Append(txtSpeed.PadLeft(8, ' ') + ",");
			builder.Append(txtDirection.PadLeft(10, ' ') + ",");
			builder.Append(txtTurnAngle.PadLeft(10, ' ') + ",");
			builder.Append(txtPosture.PadLeft(8, ' ') + ",");
			builder.Append(txtTailAngle.PadLeft(10, ' ') + ",");
			builder.Append(txtPosX.PadLeft(11, ' ') + ",");
			builder.Append(txtPosY.PadLeft(11, ' '));

			builder.Append("\r\n");
			return builder.ToString();
		}


		#region ICloneable メンバー

		public object Clone()
		{
			NxtState clone = new NxtState(this.StartPosition, this.startDirection);

			clone.RunTime = this.RunTime;
			clone.MomentRunTime = this.MomentRunTime;
			clone.Distance = this.Distance;
			clone.MomentDistance = this.MomentDistance;
			clone.AverageSpd = this.AverageSpd;
			clone.MomentAverageSpd = this.MomentAverageSpd;

			clone.Posture = this.Posture;
			clone.TailAngle = this.TailAngle;
			clone.TailTurnAngle = this.TailTurnAngle;
			clone.MomentTailTurnAngle = this.MomentTailTurnAngle;
			clone.Direction = this.Direction;
			clone.TurnAngle = this.TurnAngle;
			clone.MotorSpd = this.MotorSpd;
			clone.Position = this.Position;

			return clone;

		}

		#endregion
	}
}
