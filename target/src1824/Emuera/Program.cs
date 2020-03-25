using System;
using System.Drawing;
using System.Collections.Generic;
using System.Windows.Forms;

using MinorShift._Library;
using MinorShift.Emuera.GameView;
using MinorShift.Emuera.GameData.Expression;
using System.IO;

namespace MinorShift.Emuera
{
	static class Program
	{
		/*
		コードの開始地点。代码的开始地点
		ここでMainWindowを作り、这里生成了MainWindow
		MainWindowがProcessを作り、MainWindow生成了Process
		ProcessがGameBase・ConstantData・Variableを作る。Process生成了GameBase、ConstantData和Variable
		
		
		*.ERBの読み込み、実行、その他の処理をProcessが、*.ERB的读取、运行、其他处理由Process完成
		入出力をMainWindowが、输入输出由MainWindow完成
		定数の保存をConstantDataが、常量由ConstantData管理
		変数の管理をVariableが行う。变量由Variable进行管理
		 
		と言う予定だったが改変するうちに境界が曖昧になってしまった。但是在开发过程中各个模块的边界变得模糊起来……
		 
		後にEmueraConsoleを追加し、それに入出力を担当させることに。以后会追加EmueraConsole，
        
        1750 DebugConsole追加
         Debugを全て切り離すことはできないので一部EmueraConsoleにも担当させる由于无法将所有Debug分开，因此还需要一些EmueraConsole
		
		TODO: 1819 MainWindow & Consoleの入力・表示組とProcess&Dataのデータ処理組だけでも分離したい

		*/
		/// <summary>
		/// アプリケーションのメイン エントリ ポイントです。
		/// 应用程序的主要入口点。
		/// </summary>
		[STAThread]
		static void Main(string[] args)
		{

			ExeDir = Sys.ExeDir;
#if DEBUG
			//debugMode = true;

			//ExeDirにバリアントのパスを代入することでテスト実行するためのコード。
			//通过用替代路径替换ExeDir来执行测试的代码。
			//在本地路径的末尾“\”是必须要的。
			//ローカルパスを記載した場合は頒布前に削除すること。
			//如果列出了本地路径，请在分发之前将其删除。（隐私考虑）
			ExeDir = @"";
			
#endif
			CsvDir = ExeDir + "csv\\";
			ErbDir = ExeDir + "erb\\";
			DebugDir = ExeDir + "debug\\";
			DatDir = ExeDir + "dat\\";
			ContentDir = ExeDir + "resources\\";
			//エラー出力用
			//用于ERROR Output
			//1815 .exeが東方板のNGワードに引っかかるそうなので除去
			ExeName = Path.GetFileNameWithoutExtension(Sys.ExeName);

			//解析モードの判定だけ先に行う
			//首先确定分析模式
			int argsStart = 0;
			if ((args.Length > 0) && (args[0].Equals("-DEBUG", StringComparison.CurrentCultureIgnoreCase)))
			{
				//在调试模式和分析模式下跳过第一个（-DEBUG）参数
				argsStart = 1;//デバッグモードかつ解析モード時に最初の1っこ(-DEBUG)を飛ばす
				debugMode = true;
			}
			if (args.Length > argsStart)
			{
				//必要なファイルのチェックにはConfig読み込みが必須なので、ここではフラグだけ立てておく
				//由于必须读取Config来检查必要的文件，因此此处仅设置标志。
				AnalysisMode = true;
			}

			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			ConfigData.Instance.LoadConfig();
			//二重起動の禁止かつ二重起動
			//是否允许多实例
			if ((!Config.AllowMultipleInstances) && (Sys.PrevInstance()))
			{
				MessageBox.Show("多重起動を許可する場合、emuera.configを書き換えて下さい", "既に起動しています");
				MessageBox.Show("请重写emuera.config以允许多个启动。", "程序已经在运行了");
				return;
			}
			if (!Directory.Exists(CsvDir))
			{
				MessageBox.Show("csvフォルダが見つかりません", "フォルダなし");
				MessageBox.Show("找不到csv资料夹", "没有资料夹");
				return;
			}
			if (!Directory.Exists(ErbDir))
			{
				MessageBox.Show("erbフォルダが見つかりません", "フォルダなし");
				MessageBox.Show("找不到erb资料夹", "没有资料夹");
				return;
			}
			if (debugMode)
			{
				ConfigData.Instance.LoadDebugConfig();
				if (!Directory.Exists(DebugDir))
				{
					try
					{
						Directory.CreateDirectory(DebugDir);
					}
					catch
					{
						MessageBox.Show("debugフォルダの作成に失敗しました", "フォルダなし");
						MessageBox.Show("创建调试文件夹失败", "没有资料夹");
						return;
					}
				}
			}
			if (AnalysisMode)
			{
				AnalysisFiles = new List<string>();
				for (int i = argsStart; i < args.Length; i++)
				{
					if (!File.Exists(args[i]) && !Directory.Exists(args[i]))
					{
						MessageBox.Show("与えられたファイル・フォルダは存在しません");
						MessageBox.Show("给定的文件/文件夹不存在");
						return;
					}
					if ((File.GetAttributes(args[i]) & FileAttributes.Directory) == FileAttributes.Directory)
					{
						List<KeyValuePair<string, string>> fnames = Config.GetFiles(args[i] + "\\", "*.ERB");
						for (int j = 0; j < fnames.Count; j++)
						{
							AnalysisFiles.Add(fnames[j].Value);
						}
					}
					else
					{
						if (Path.GetExtension(args[i]).ToUpper() != ".ERB")
						{
							MessageBox.Show("ドロップ可能なファイルはERBファイルのみです");
							MessageBox.Show("只能删除ERB文件");
							return;
						}
						AnalysisFiles.Add(args[i]);
					}
				}
			}
			MainWindow win = null;
			while (true)
			{
				StartTime = WinmmTimer.TickCount;
				using (win = new MainWindow())
				{
					Application.Run(win);
					Content.AppContents.UnloadContents();
					if (!Reboot)
						break;

					RebootWinState = win.WindowState;
					if (win.WindowState == FormWindowState.Normal)
					{
						RebootClientY = win.ClientSize.Height;
						RebootLocation = win.Location;
					}
					else
					{
						RebootClientY = 0;
						RebootLocation = new Point();
					}
				}
				//条件次第ではParserMediatorが空でない状態で再起動になる場合がある
				ParserMediator.ClearWarningList();
				ParserMediator.Initialize(null);
				GlobalStatic.Reset();
				//GC.Collect();
				Reboot = false;
				ConfigData.Instance.ReLoadConfig();
			}
		}

		/// <summary>
		/// 実行ファイルのディレクトリ。最後に\を付けたstring
		/// </summary>
		public static string ExeDir { get; private set; }
		public static string CsvDir { get; private set; }
		public static string ErbDir { get; private set; }
		public static string DebugDir { get; private set; }
		public static string DatDir { get; private set; }
		public static string ContentDir { get; private set; }
		public static string ExeName { get; private set; }

		public static bool Reboot = false;
		//public static int RebootClientX = 0;
		public static int RebootClientY = 0;
		public static FormWindowState RebootWinState = FormWindowState.Normal;
		public static Point RebootLocation;

		public static bool AnalysisMode = false;
		public static List<string> AnalysisFiles = null;

		public static bool debugMode = false;
		public static bool DebugMode { get { return debugMode; } }


		public static uint StartTime { get; private set; }

	}
}