using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Windows;

namespace NxtManagerV3
{
	/// <summary>
	/// NXT State Manager クラス
	/// </summary>
	public class NxtStateManager
	{
		/// <summary>
		/// Nxt State
		/// </summary>
		private NxtState state;

		/// <summary>
		/// ログリスト
		/// </summary>
		private List<NxtState> stateLogList;

		/// <summary>
		/// 現在の状態
		/// </summary>
		public NxtState CurrentState
		{
			get
			{
				return state;
			}
		}

		/// <summary>
		/// ログリスト
		/// </summary>
		public IEnumerable<NxtState> StateLogList
		{
			get
			{
				return stateLogList;
			}
		}

		/// <summary>
		/// ログカウント
		/// </summary>
		public int StateLogCount
		{
			get
			{
				return stateLogList.Count;
			}
		}

		/// <summary>
		/// ログファイル名
		/// </summary>
		public string StateLogFileName
		{
			get;
			private set;
		}

		/// <summary>
		/// ログテキスト(","区切り)
		/// </summary>
		public string StateText
		{
			get
			{
				return stateTextBuilder.ToString();
			}
		}

		private Point startPos;
		private double startDirection;
		private StringBuilder stateTextBuilder;

		/// <summary>
		/// Constructor
		/// </summary>
		public NxtStateManager(Point startPos, double startDirection)
		{
			this.startPos = startPos;
			this.startDirection = startDirection;

			// NXT Stateを初期化
			state = new NxtState(startPos, startDirection);

			// ログリストを生成
			stateLogList = new List<NxtState>();
			// 初期値をリストに追加
			stateLogList.Add((NxtState)state.Clone());

			stateTextBuilder = new StringBuilder();
			stateTextBuilder.AppendFormat("  RunTime, MoveDist, MoveSpd, Direction, TurnAngle, Posture, TailAngle, Position.X, Position.Y\r\n");
		}

		/// <summary>
		/// 状態リストにデータを追加
		/// </summary>
		/// <param name="log">ログデータ</param>
		public void AppendList(NxtLog log)
		{
			state.UpdateState(log);

			stateLogList.Add((NxtState)state.Clone());
			stateTextBuilder.AppendFormat(state.ToString());
		}

		/// <summary>
		/// ログリストのデータをクリア
		/// </summary>
		public void ClearList()
		{
			stateLogList.Clear();
			this.state = new NxtState(this.startPos, this.startDirection);
		}
	}
}
