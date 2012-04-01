using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms.Integration;
using System.Windows.Media;
using System.Windows.Threading;
using FormChart = System.Windows.Forms.DataVisualization.Charting;
using Microsoft.Win32;
using System.ComponentModel;
using System.Text;
using System.Linq;

namespace NxtManagerV3
{
	/// <summary>
	/// MainWindow.xaml の相互作用ロジック
	/// </summary>
	public partial class MainWindow : Window
	{
		/// <summary>
		/// Append Messagte Delegate
		/// </summary>
		private AppendMessegeDelegate appendMessageDelegate;
		
		/// <summary>
		/// Port状態監視タイマ
		/// </summary>
		private DispatcherTimer stateControlTimer;
		
		/// <summary>
		/// Log Output TextBox更新用ストップウォッチ
		/// </summary>
		private Stopwatch txtLogOutputStopwatch;

		/// <summary>
		/// State Output TextBox更新用ストップウォッチ
		/// </summary>
		private Stopwatch txtStateOutputStopwatch;

		/// <summary>
		/// Logファイルロード用 Background Worker
		/// </summary>
		private BackgroundWorker backWorkLogLoad;
		
		/// <summary>
		/// ログ受信
		/// </summary>
		private NxtLogReceiver logReceiver;
		/// <summary>
		/// ログ管理
		/// </summary>
		private NxtLogManager logManager;
		/// <summary>
		/// 状態管理
		/// </summary>
		private NxtStateManager stateManager;
		/// <summary>
		/// ロググラフ管理
		/// </summary>
		private NxtLogChartManager logChartManager;
		/// <summary>
		/// 状態グラフ管理
		/// </summary>
		private NxtStateChartManager stateChartManager;

		/// <summary>
		/// ログテキスト一時格納用バッファ
		/// </summary>
		private string bufLogTextBox;
		/// <summary>
		/// 状態テキスト一時格納用バッファ
		/// </summary>
		private string bufStateTextBox;

		/// <summary>
		/// ロググラフタブリスト
		/// </summary>
		private List<TabItem> logChartTabList = new List<TabItem>();
		/// <summary>
		/// 状態グラフタブリスト
		/// </summary>
		private List<TabItem> stateChartTabList = new List<TabItem>();

		/// <summary>
		/// Constructor
		/// </summary>
		public MainWindow()
		{
			InitializeComponent();

			// Dispatch Timerを初期化
			InitializeStateControlTimer();

			// ログ受信時に実行するメソッドを登録
			appendMessageDelegate = new AppendMessegeDelegate(AppendList);
			appendMessageDelegate += new AppendMessegeDelegate(AppendTextBox);
			appendMessageDelegate += new AppendMessegeDelegate(AppendLogFile);
			appendMessageDelegate += new AppendMessegeDelegate(AppendLogGraph);
			appendMessageDelegate += new AppendMessegeDelegate(AppendState);
			appendMessageDelegate += new AppendMessegeDelegate(AppendStateTextBox);
			appendMessageDelegate += new AppendMessegeDelegate(AppendStateGraph);

			// ログメッセージ作成開始
			logReceiver = new NxtLogReceiver(Dispatcher, appendMessageDelegate);

			// StopWatchを初期化
			txtLogOutputStopwatch = new Stopwatch();
			txtStateOutputStopwatch = new Stopwatch();

			// Log Managerを初期化
			logManager = new NxtLogManager();
			// State Managerを初期化
			stateManager = new NxtStateManager(new Point(0, 0), 0);

			// Log Chart Managerを初期化
			logChartManager = new NxtLogChartManager();
			// State Chart Managerを初期化
			stateChartManager = new NxtStateChartManager();

			// CheckListBoxに項目を追加
			for (int i = 1; i < NxtLog.LogDataMember.Length; i++)
			{
				chkListGraph.Items.Add(NxtLog.LogDataMember[i]);
			}

			// CheckListBoxに項目を追加
			for (int i = 1; i < NxtState.NxtStateDataMember.Length; i++)
			{
				chkListStateGraph.Items.Add(NxtState.NxtStateDataMember[i]);
			}
		}

		#region Initialize Component

		/// <summary>
		/// 接続状態監視タイマ初期化
		/// </summary>
		private void InitializeStateControlTimer()
		{
			// 1秒タイマ
			stateControlTimer = new DispatcherTimer();
			stateControlTimer.Interval = new TimeSpan(0, 0, 1);
			stateControlTimer.Tick += new EventHandler(StateControlTimer_Tick);
			stateControlTimer.Start();
		}

		/// <summary>
		/// ログファイル読み込み用Background Worker
		/// </summary>
		private void InitializeBgWorker()
		{
			backWorkLogLoad = new BackgroundWorker();
			backWorkLogLoad.WorkerReportsProgress = true;
			backWorkLogLoad.DoWork += new DoWorkEventHandler(backWorkLogLoad_DoWork);
			backWorkLogLoad.ProgressChanged += new ProgressChangedEventHandler(backWorkLogLoad_ProgressChanged);
			backWorkLogLoad.RunWorkerCompleted += new RunWorkerCompletedEventHandler(backWorkLogLoad_RunWorkerCompleted);
		}

		#endregion

		#region Initialize GUI

		// ポート番号を取得しコンボボックスにセット
		private void PortNoLoad()
		{
			// ソート済みのポート名一覧を取得
			string[] portNames = PortControl.GetSortedPortNames();

			// 取得したSerial Port名をコンボボックスにセット
			cmbPortName.Items.Clear();

			foreach (string port in portNames)
			{
				this.cmbPortName.Items.Add(port);
			}

			// コンボボックスのデフォルト値を選択
			this.cmbPortName.Text = "COM3";

			// ラベル文字を非アクティブ表示
			this.toolStatusCTS.Foreground = Brushes.LightGray;
			this.toolStatusDSR.Foreground = Brushes.LightGray;
		}

		#endregion


		#region Control Event Handler
		
		/// <summary>
		/// Window Load Event
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			// Serial Port番号読み込み
			PortNoLoad();

			// テキストボックスを初期化
			txtLogOutput.Text = string.Format("  Time, Data1, Data2, Battery, MotorA, MotorB, MotorC, ADC1, ADC2, ADC3, ADC4, I2C\r\n");
			txtStateOutput.Text = string.Format("  RunTime, MoveDist, MoveSpd, Direction, TurnAngle, Posture, TailAngle, Position.X, Position.Y\r\n");
		}

		/// <summary>
		/// Window Closed Event
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Window_Closed(object sender, EventArgs e)
		{
			// シリアルポートインスタンス破棄
			this.logReceiver.Port.Dispose();
		}

		/// <summary>
		/// 通信状態監視タイマ Event
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void StateControlTimer_Tick(object sender, EventArgs e)
		{
			// Serial Portに接続中
			if (logReceiver.IsConnected)
			{
				// ハードウェアフロー制御端子(CTS : Clear To Send)監視
				if (this.logReceiver.CtsHolding)
				{
					// ラベル文字をアクティブ表示
					this.toolStatusCTS.Foreground = Brushes.Black;
				}
				else
				{
					// ラベル文字を非アクティブ表示
					this.toolStatusCTS.Foreground = Brushes.LightGray;
				}

				// ハードウェアフロー制御端子(DSR : Data Set Ready)監視
				if (this.logReceiver.DsrHolding)
				{
					// ラベル文字をアクティブ表示
					this.toolStatusDSR.Foreground = Brushes.Black;
				}
				else
				{
					// ラベル文字を非アクティブ表示
					this.toolStatusDSR.Foreground = Brushes.LightGray;
				}

				// フロー制御端子が異常
				if ((this.logReceiver.CtsHolding && this.logReceiver.DsrHolding) == false)
				{
					// CONNECTボタンをアンチェック状態
					this.chkConnect.IsChecked = false;
				}
			}
			else
			{
				// ラベル文字を非アクティブ表示
				this.toolStatusCTS.Foreground = Brushes.LightGray;
				this.toolStatusDSR.Foreground = Brushes.LightGray;
			}
		}

		/// <summary>
		/// ConnectボタンON Event
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void chkConnect_Checked(object sender, RoutedEventArgs e)
		{
			// 表示されているログデータを全てクリア
			ClearAllLogData();

			// Serial Port 番号設定
			logReceiver.Connect(this.cmbPortName.Text);

			// COMポート番号選択コンボボックスを無効化
			cmbPortName.IsEnabled = false;

			// 現在日時の取得
			DateTime timeNow = DateTime.Now;

			// 日時をログファイル名に指定
			string logFileName = timeNow.ToString("yyyyMMdd_HHmmss") + ".csv";

			// ログファイル生成
			logManager.CreateLogFile(logFileName);

			// ログファイル名を表示
			toolStatusLogFileName.Text = "Log File : " + logFileName;
		}

		/// <summary>
		/// Connectボタン OFF Event
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void chkConnect_Unchecked(object sender, RoutedEventArgs e)
		{
			// ポートの切断
			logReceiver.Disconnect();

			// COMポート番号選択コンボボックスを有効化
			cmbPortName.IsEnabled = true;
		}

		/// <summary>
		/// Add GraphボタンクリックEvent
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void btnAddLogGraph_Click(object sender, RoutedEventArgs e)
		{
			// アイテムが一つもチェックされていない
			if (chkListGraph.SelectedItems.Count == 0)
			{
				// Error Messageを表示して、処理を終了
				MessageBox.Show("There are no selected items!!", "Add Graph Error", MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}

			// 選択された系列名リストからグラフを生成
			NxtChart chart = logChartManager.CreateChart(chkListGraph.SelectedItems);

			// 新規WinFormHostを生成
			WindowsFormsHost formHost = new WindowsFormsHost();
			// グラフを追加
			formHost.Child = chart;

			// 新規タブを生成
			TabItem tabItem = new TabItem();

			// タブ名 = Graph + 番号
			tabItem.Header = string.Format("Log Graph{0}", logChartManager.ChartCount);
			// WinFormHostをメンバに追加
			tabItem.Content = formHost;
			// タブ管理リストに追加
			logChartTabList.Add(tabItem);

			// タブコントロールに新規タブを追加
			tabControl.Items.Add(tabItem);

			// 全ての系列の選択を解除
			chkListGraph.SelectedIndex = -1;
		}

		/// <summary>
		/// Clear All Graph Tabボタンクリック Event
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void btnClearAllLogGraphTab_Click(object sender, RoutedEventArgs e)
		{
			// 全てのGraph TabをTab Controlから削除
			for (int i = 0; i < logChartTabList.Count; i++)
			{
				TabItem tab = logChartTabList[i];
				tabControl.Items.Remove(tab);
			}

			// リストから全てのグラフを削除
			logChartManager.ClearChart();
			// リストから全てのタブを削除
			logChartTabList.Clear();
		}

		/// <summary>
		/// Load Menu クリック Event
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void MenuItemLoad_Click(object sender, RoutedEventArgs e)
		{
			// Background Workerを初期化
			InitializeBgWorker();

			// ファイルオープンダイアログ
			OpenFileDialog dlg = new OpenFileDialog();

			// CSVファイルをフィルタリング
			dlg.Filter = "CSVファイル(*.csv)|*.csv|すべてのファイル(*.*)|*.*";
			dlg.CheckFileExists = true;
			dlg.CheckPathExists = true;

			// ファイルオープンが成功
			if (dlg.ShowDialog() == true)
			{
				// 全てのログを削除
				ClearAllLogData();

				// ログファイル名を表示
				toolStatusLogFileName.Text = "Log File : " + dlg.FileName;

				// UIを無効化
				this.chkConnect.IsEnabled = false;
				this.chkListGraph.IsEnabled = false;
				this.btnAddGraph.IsEnabled = false;
				this.btnClearGraph.IsEnabled = false;
				this.chkListStateGraph.IsEnabled = false;
				this.btnAddStateGraph.IsEnabled = false;
				this.btnClearStateGraph.IsEnabled = false;


				//ファイル名を指定して、処理を開始する
				backWorkLogLoad.RunWorkerAsync(dlg.FileName);
			}
		}

		/// <summary>
		/// Background Worker Completed Event
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void backWorkLogLoad_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			if (e.Error != null)
			{
				//エラーが発生したとき
				toolStatusLogFileName.Text = "File Open Error!!" + e.Error.Message;
			}
			else
			{
				// テキストボックスをクリア
				this.txtLogOutput.Clear();
				this.txtStateOutput.Clear();

				// テキストボックスに結果を出力
				this.txtLogOutput.AppendText(logManager.LogText);

				this.txtStateOutput.AppendText(stateManager.StateText);

				// ログデータからグラフデータを生成
				foreach (NxtLog log in logManager.LogList)
				{
					logChartManager.AppendData(log);
				}

				// 状態データからグラフデータを生成
				foreach (NxtState state in stateManager.StateLogList)
				{
					stateChartManager.AppendData(state);
				}

				// Progress Barをクリア
				this.toolStatusProgressBar.Value = 0;

				// UIを有効化
				this.chkConnect.IsEnabled = true;
				this.chkListGraph.IsEnabled = true;
				this.btnAddGraph.IsEnabled = true;
				this.btnClearGraph.IsEnabled = true;
				this.chkListStateGraph.IsEnabled = true;
				this.btnAddStateGraph.IsEnabled = true;
				this.btnClearStateGraph.IsEnabled = true;
			}
		}

		/// <summary>
		/// Background Worker Progress Changed Event
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void backWorkLogLoad_ProgressChanged(object sender, ProgressChangedEventArgs e)
		{
			// Progress Barを進める
			this.toolStatusProgressBar.Value = e.ProgressPercentage;
		}

		/// <summary>
		/// Background Worker Do Work Event
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void backWorkLogLoad_DoWork(object sender, DoWorkEventArgs e)
		{
			// ファイル名を取得
			string filename = (string)e.Argument;

			// 呼び出し元 Background Workerを取得
			BackgroundWorker bgWorker = (BackgroundWorker)sender;

			// ログファイルを開く
			logManager.OpenLogFile(filename, bgWorker, stateManager);

			// テキストボックス出力用文字列を結果に格納
			e.Result = logManager.LogText;
		}

		#endregion		

		#region ログデータ処理

		/// <summary>
		/// Log Manaagerへログデータを追加
		/// </summary>
		private void AppendList(NxtLog logMessage)
		{
			// リストにログを追加
			logManager.AppendList(logMessage);
		}

		/// <summary>
		/// ログファイルへログデータを追加
		/// </summary>
		private void AppendLogFile(NxtLog logMessage)
		{
			// ファイルにログを追加
			logManager.AppendLogFile(logMessage);
		}

		/// <summary>
		/// テキストボックスへログデータを追加
		/// </summary>
		private void AppendTextBox(NxtLog logMessage)
		{
			bufLogTextBox += logMessage.ToString();

			// テキストボックスへの書込み（AppendText）を頻繁に繰り返すと
			// 実行速度が低下するためバッファーを介して書込みをまとめて行う。

			// ストップウォッチ停止
			txtLogOutputStopwatch.Stop();

			// 更新時間が一定時間内であればスキップ
			if (txtLogOutputStopwatch.ElapsedMilliseconds > 20)
			{
				// テキストボックスへ追記
				txtLogOutput.AppendText(bufLogTextBox);

				// 最終行までスクロール
				txtLogOutput.ScrollToEnd();

				// 書込みバッファーをクリア
				bufLogTextBox = String.Empty;

				// ストップウォッチをリセット
				txtLogOutputStopwatch.Reset();
			}

			// ストップウオッチを（再）スタート
			txtLogOutputStopwatch.Start();
		}

		/// <summary>
		/// グラフへログデータを追加
		/// </summary>
		private void AppendLogGraph(NxtLog logMessage)
		{
			// グラフにデータを追加
			logChartManager.AppendData(logMessage);
		}

		/// <summary>
		/// State Manaagerへログデータを追加
		/// </summary>
		private void AppendState(NxtLog logMessage)
		{
			// リストにログを追加
			stateManager.AppendList(logMessage);
		}

		/// <summary>
		/// テキストボックスへログデータを追加
		/// </summary>
		private void AppendStateTextBox(NxtLog logMessage)
		{
			bufStateTextBox += stateManager.CurrentState.ToString();

			// テキストボックスへの書込み（AppendText）を頻繁に繰り返すと
			// 実行速度が低下するためバッファーを介して書込みをまとめて行う。

			// ストップウォッチ停止
			txtStateOutputStopwatch.Stop();

			// 更新時間が一定時間内であればスキップ
			if (txtStateOutputStopwatch.ElapsedMilliseconds > 20)
			{
				// テキストボックスへ追記
				txtStateOutput.AppendText(bufStateTextBox);

				// 最終行までスクロール
				txtStateOutput.ScrollToEnd();

				// 書込みバッファーをクリア
				bufStateTextBox = String.Empty;

				// ストップウォッチをリセット
				txtStateOutputStopwatch.Reset();
			}

			// ストップウオッチを（再）スタート
			txtStateOutputStopwatch.Start();
		}

		/// <summary>
		/// グラフへ状態データを追加
		/// </summary>
		private void AppendStateGraph(NxtLog logMessage)
		{
			// グラフにデータを追加
			stateChartManager.AppendData(stateManager.CurrentState);
		}

		/// <summary>
		/// ログデータをクリア
		/// </summary>
		private void ClearAllLogData()
		{
			// テキストボックスを初期化
			txtLogOutput.Text = string.Format("  Time, Data1, Data2, Battery, MotorA, MotorB, MotorC, ADC1, ADC2, ADC3, ADC4, I2C\r\n");
			txtStateOutput.Text = string.Format("  RunTime, MoveDist, MoveSpd, Direction, TurnAngle, Posture, TailAngle\r\n");

			// ログデータの取得リストをクリア            
			logManager.ClearList();

			// 状態データリストをクリア
			stateManager.ClearList();
			stateManager = new NxtStateManager(new Point(), 0);

			// グラフのデータを全て削除
			logChartManager.ClearData();
			stateChartManager.ClearData();

		}

		#endregion

		/// <summary>
		/// Clear All State Graph Tab クリック Event
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void btnClearAllStateGraphTab_Click(object sender, RoutedEventArgs e)
		{
			// 全てのGraph TabをTab Controlから削除
			for (int i = 0; i < stateChartTabList.Count; i++)
			{
				TabItem tab = stateChartTabList[i];
				tabControl.Items.Remove(tab);
			}

			// リストから全てのグラフを削除
			stateChartManager.ClearChart();
			// リストから全てのタブを削除
			stateChartTabList.Clear();
		}

		/// <summary>
		/// Add State Graph クリック Event
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void btnAddStateGraph_Click(object sender, RoutedEventArgs e)
		{
			// アイテムが一つもチェックされていない
			if (chkListStateGraph.SelectedItems.Count == 0)
			{
				// Error Messageを表示して、処理を終了
				MessageBox.Show("There are no selected items!!", "Add Graph Error", MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}

			// 選択された系列名リストからグラフを生成
			NxtChart chart = stateChartManager.CreateChart(chkListStateGraph.SelectedItems);

			// 新規WinFormHostを生成
			WindowsFormsHost formHost = new WindowsFormsHost();
			// グラフを追加
			formHost.Child = chart;

			// 新規タブを生成
			TabItem tabItem = new TabItem();
			// タブ名 = Graph + 番号
			tabItem.Header = string.Format("State Graph{0}", stateChartManager.ChartCount);
			// WinFormHostをメンバに追加
			tabItem.Content = formHost;
			// タブ管理リストに追加
			stateChartTabList.Add(tabItem);

			// タブコントロールに新規タブを追加
			tabControl.Items.Add(tabItem);

			// 全ての系列の選択を解除
			chkListStateGraph.SelectedIndex = -1;
		}
	}
}
