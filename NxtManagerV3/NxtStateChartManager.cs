using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms.DataVisualization.Charting;
using System.Windows;
using System.Collections;

namespace NxtManagerV3
{
	/// <summary>
	/// NXT State Chart Manager Class
	/// </summary>
	public class NxtStateChartManager
	{
		/// <summary>
		/// State Series リスト
		/// </summary>
		List<Series> stateSeriesList;

		/// <summary>
		/// Nxt Chart リスト
		/// </summary>
		List<NxtChart> stateChartList;

		/// <summary>
		/// グラフ数
		/// </summary>
		public int ChartCount
		{
			get
			{
				return stateChartList.Count;
			}
		}

		/// <summary>
		/// Constructor
		/// </summary>
		public NxtStateChartManager()
		{
			// 状態グラフリストを作成
			stateChartList = new List<NxtChart>();

			// 状態データメンバー数だけ系列を生成
			stateSeriesList = new List<Series>(NxtState.NxtStateDataMember.Length);

			// 系列名とグラフの種類を設定してリストに追加
			foreach (string name in NxtState.NxtStateDataMember)
			{
				Series series = new Series(name);
				series.ChartType = SeriesChartType.FastLine;
				stateSeriesList.Add(series);
			}
		}

		/// <summary>
		/// 新規グラフを生成
		/// </summary>
		/// <param name="seriesNames">系列名リスト</param>
		/// <returns>新規グラフ</returns>
		public NxtChart CreateChart(IEnumerable seriesNames)
		{
			// 新規グラフを生成
			NxtChart chart = new NxtChart();

			// チェックされた項目の系列を追加
			foreach (string name in seriesNames)
			{
				// 系列名から系列を取得
				Series s = GetSeriesByName(name);

				// グラフに系列を追加
				chart.AddSeries(s);
			}

			// グラフ管理リストに追加
			stateChartList.Add(chart);

			return chart;
		}

		/// <summary>
		/// 全てのグラフを削除
		/// </summary>
		public void ClearChart()
		{
			this.stateChartList.Clear();
		}

		/// <summary>
		/// データを追加
		/// </summary>
		/// <param name="state">NXT State</param>
		public void AppendData(NxtState state)
		{
			// 全ての系列に対してデータを追加
			foreach (string name in NxtState.NxtStateDataMember)
			{
				Point p = state.GetTimeSeriesData(name);
				// データポイントを生成
				DataPoint point = new DataPoint(p.X, p.Y);
				// 系列名からデータを追加する系列を取得
				Series s = stateSeriesList.Find(o => o.Name == name);
				// データを追加
				s.Points.Add(point);
			}

		}

		/// <summary>
		/// 全データを削除
		/// </summary>
		public void ClearData()
		{
			// 全系列のデータをクリア
			for (int i = 0; i < stateSeriesList.Count; i++)
			{
				stateSeriesList[i].Points.Clear();
			}
		}

		/// <summary>
		/// 系列名から系列を取得
		/// </summary>
		/// <param name="name">系列名</param>
		/// <returns>系列</returns>
		public Series GetSeriesByName(string name)
		{
			// 系列名が指定文字列と一致する系列を取得
			Series series = stateSeriesList.Find(o => o.Name == name);

			return series;
		}
	}
}
