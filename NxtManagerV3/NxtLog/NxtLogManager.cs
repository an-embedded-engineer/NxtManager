using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.ComponentModel;
using Microsoft.VisualBasic.FileIO;
using System.Text;

namespace NxtManagerV3
{
	/// <summary>
	/// NXT Log Manager クラス
	/// </summary>
	public class NxtLogManager
	{
		/// <summary>
		/// ログリスト
		/// </summary>
		private List<NxtLog> logList;

		/// <summary>
		/// ログリスト
		/// </summary>
		public IEnumerable<NxtLog> LogList
		{
			get
			{
				return logList;
			}
		}

		/// <summary>
		/// ログカウント
		/// </summary>
		public NxtLog CuurentLog
		{
			get
			{
				return logList[LogCount - 1];
			}
		}

		/// <summary>
		/// ログカウント
		/// </summary>
		public int LogCount
		{
			get
			{
				return logList.Count;
			}
		}

		/// <summary>
		/// ログファイル名
		/// </summary>
		public string LogFileName
		{
			get;
			private set;
		}

		/// <summary>
		/// ログテキスト(","区切り)
		/// </summary>
		public string LogText
		{
			get
			{
				return logTextBuilder.ToString();
			}
		}

		private StringBuilder logTextBuilder;

		/// <summary>
		/// Constructor
		/// </summary>
		public NxtLogManager()
		{
			// ログリストを生成
			logList = new List<NxtLog>();
			logTextBuilder = new StringBuilder();

			logTextBuilder.AppendFormat("  Time, Data1, Data2, Battery, MotorA, MotorB, MotorC, ADC1, ADC2, ADC3, ADC4, I2C\r\n");
		}

		/// <summary>
		/// ログリストにデータを追加
		/// </summary>
		/// <param name="log">ログデータ</param>
		public void AppendList(NxtLog log)
		{
			logList.Add(log);
			logTextBuilder.AppendFormat(log.ToString());
		}

		/// <summary>
		/// ログリストのデータをクリア
		/// </summary>
		public void ClearList()
		{
			logList.Clear();
		}

		/// <summary>
		/// ログファイルを生成
		/// </summary>
		/// <param name="filename">ファイル名</param>
		public void CreateLogFile(string filename)
		{
			// ファイル名を保存
			this.LogFileName = filename;

			// ファイル生成
			using (StreamWriter sw = new StreamWriter(new FileStream(this.LogFileName, FileMode.Append)))
			{
				try
				{
					// ログファイル(*.csv)の一行目にタイトル挿入
					sw.WriteLine("Time,Data1,Data2,Battery,Motor Rev A,Motor Rev B,Motor Rev C,ADC S1,ADC S2,ADC S3,ADC S4,I2C");
				}
				catch (Exception ex)
				{
					Debug.WriteLine("FILE WRITE ERROR : {0}", ex.ToString());
				}
			}
		}

		/// <summary>
		/// ログファイルにデータを追加
		/// </summary>
		/// <param name="log">ログデータ</param>
		public void AppendLogFile(NxtLog log)
		{
			// CSV形式でログデータを取得
			string rec = log.ToCsvString();

			// ログファイルを開く
			using (StreamWriter sw = new StreamWriter(new FileStream(this.LogFileName, FileMode.Append)))
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
		}

		/// <summary>
		/// ログファイルを開く
		/// </summary>
		/// <param name="filename">ファイル名</param>
		/// <param name="bgWorker">Background Worker</param>
		/// <param name="stateManager">Nxt State Manager</param>
		public void OpenLogFile(string filename, BackgroundWorker bgWorker, NxtStateManager stateManager = null)
		{
			// ファイル名を保存
			this.LogFileName = filename;

			// CSV ファイルから行単位で読み込み
			string[] file_data = File.ReadAllLines(filename);
			// 行数を取得
			int length = file_data.Length;

			// テキストボックス出力用文字列
			logTextBuilder = new StringBuilder();
			logTextBuilder.AppendFormat("  Time, Data1, Data2, Battery, MotorA, MotorB, MotorC, ADC1, ADC2, ADC3, ADC4, I2C\r\n");

			// CSVパーサーを生成
			using (TextFieldParser parser = new TextFieldParser(filename, System.Text.Encoding.GetEncoding("Shift_JIS")))
			{
				// CSVは区切り形式
				parser.TextFieldType = FieldType.Delimited;
				// 区切り文字はコンマ
				parser.SetDelimiters(",");

				// 最初の1行(系列名)は捨てる
				string[] comment = parser.ReadFields();
				int cnt = 0;

				// 最終行まで走査
				while (parser.EndOfData != true)
				{
					// 1行分のデータを取得
					string[] row = parser.ReadFields();

					// CSV形式をByte配列に変換
					using (MemoryStream ms = new MemoryStream())
					{
						// Time
						ms.Write(BitConverter.GetBytes(uint.Parse(row[0])), 0, sizeof(uint));
						// Data1
						ms.Write(BitConverter.GetBytes(sbyte.Parse(row[1])), 0, sizeof(sbyte));
						// Data2
						ms.Write(BitConverter.GetBytes(sbyte.Parse(row[2])), 0, sizeof(sbyte));
						// Battery
						ms.Write(BitConverter.GetBytes(ushort.Parse(row[3])), 0, sizeof(ushort));
						// MotorA
						ms.Write(BitConverter.GetBytes(int.Parse(row[4])), 0, sizeof(int));
						// MotorB
						ms.Write(BitConverter.GetBytes(int.Parse(row[5])), 0, sizeof(int));
						// MotorC
						ms.Write(BitConverter.GetBytes(int.Parse(row[6])), 0, sizeof(int));
						// ADC1
						ms.Write(BitConverter.GetBytes(short.Parse(row[7])), 0, sizeof(short));
						// ADC2
						ms.Write(BitConverter.GetBytes(short.Parse(row[8])), 0, sizeof(short));
						// ADC3
						ms.Write(BitConverter.GetBytes(short.Parse(row[9])), 0, sizeof(short));
						// ADC4
						ms.Write(BitConverter.GetBytes(short.Parse(row[10])), 0, sizeof(short));
						// I2C
						ms.Write(BitConverter.GetBytes(int.Parse(row[11])), 0, sizeof(int));

						// ログを生成
						NxtLog log = new NxtLog(ms.ToArray());

						// リストに追加
						logList.Add(log);

						// テキストボックス出力用文字列にログを追加
						logTextBuilder.AppendFormat(log.ToString());

						// Nxt State Managerが初期化済みかつ、NXT State読み込み設定がON
						if (stateManager != null && Properties.Settings.Default.LoadNxtStateFromLogFile == true)
						{
							// 状態を更新
							stateManager.AppendList(log);
						}
					}

					cnt++;
					// 進捗率を計算
					double parcent = ((double)cnt / (double)length) * 100;

					// 進捗率を通知
					bgWorker.ReportProgress((int)parcent);
				}
			}
		}
	}
}
