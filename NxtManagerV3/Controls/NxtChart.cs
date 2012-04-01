using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

using Chart = System.Windows.Forms.DataVisualization.Charting;

namespace NxtManagerV3
{
	public partial class NxtChart : UserControl
	{
		/// <summary>
		/// X座標カーソル
		/// </summary>
		Chart.Cursor cursorY;

		/// <summary>
		/// Y座標カーソル
		/// </summary>
		Chart.Cursor cursorX;

		/// <summary>
		/// 系列名リスト
		/// </summary>
		public IEnumerable<string> SeriesNameList
		{
			get 
			{
				// 系列リストから名前リストを取得
				return chart.Series.Select(c => c.Name);
			}
		}

		/// <summary>
		/// Constructor
		/// </summary>
		public NxtChart()
		{
			InitializeComponent();

			// グラフ領域の初期化
			InitializeGraph();
		}

		/// <summary>
		/// Gaphの初期化
		/// </summary>
		private void InitializeGraph()
		{
			// グラフ初期化
			this.chart.Series.Clear();

			// カーソルの取得
			this.cursorX = this.chart.ChartAreas[0].CursorX;
			this.cursorY = this.chart.ChartAreas[0].CursorY;

			// X軸カーソルの設定
			cursorX.LineWidth = 1;
			cursorX.LineDashStyle = ChartDashStyle.Solid;
			cursorX.LineColor = Color.Red;
			cursorX.SelectionColor = SystemColors.Highlight;

			// Y軸カーソルの設定
			cursorY.LineWidth = 1;
			cursorY.LineDashStyle = ChartDashStyle.Solid;
			cursorY.LineColor = Color.Red;
			cursorY.SelectionColor = SystemColors.Highlight;

			// Enable range selection and zooming end user interface
			chart.ChartAreas[0].CursorX.IsUserEnabled = true;
			chart.ChartAreas[0].CursorX.IsUserSelectionEnabled = true;
			chart.ChartAreas[0].CursorY.IsUserEnabled = true;
			chart.ChartAreas[0].CursorY.IsUserSelectionEnabled = true;

			chart.ChartAreas[0].AxisX.ScaleView.Zoomable = true;
			chart.ChartAreas[0].AxisX.ScrollBar.IsPositionedInside = true;
			chart.ChartAreas[0].AxisY.ScaleView.Zoomable = true;
			chart.ChartAreas[0].AxisY.ScrollBar.IsPositionedInside = true;
		}

		/// <summary>
		/// 系列を追加
		/// </summary>
		/// <param name="series">系列</param>
		public void AddSeries(Series series)
		{
			// グラフに追加
			chart.Series.Add(series);
		}

		/// <summary>
		/// 系列を新規作成
		/// </summary>
		/// <param name="name">系列名</param>
		/// <param name="type">グラフの種類</param>
		public void CreateNewSeries(string name, SeriesChartType type)
		{
			// 新規系列を生成
			Series series = new Series(name);

			// グラフの種類を設定
			series.ChartType = type;

			// グラフに追加
			chart.Series.Add(series);
		}

		/// <summary>
		/// グラフにデータを追加
		/// </summary>
		/// <param name="name">系列名</param>
		/// <param name="x">X座標データ</param>
		/// <param name="y">Y座標データ</param>
		public void AppendData(string name, double x, double y)
		{
			// データポイントを生成
			DataPoint point = new DataPoint(x, y);
			// 系列名からデータを追加する系列を取得
			Series s = chart.Series.FindByName(name);
			// データを追加
			s.Points.Add(point);
		}

		/// <summary>
		/// 全データを削除
		/// </summary>
		public void ClearAllData()
		{
			// 全系列のデータをクリア
			foreach (Series s in chart.Series)
			{
				s.Points.Clear();
			}
		}

	}
}
