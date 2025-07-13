using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

using Livet;
using Livet.Commands;
using Livet.Messaging;
using Livet.Messaging.IO;
using Livet.EventListeners;
using Livet.Messaging.Windows;

using Calc.Models;
using System.Threading.Tasks;

namespace Calc.ViewModels
{
	public class MainWindowViewModel : ViewModel
	{
		/// <summary>
		/// ボタンの種類
		/// </summary>
		private enum EnumButtonKind
		{
			None, Number, Mark, Func,
		}

		/// <summary>
		/// 操作の種類
		/// </summary>
		private enum EnumOperate
		{
			None, Addition, Subtraction, Multiplication, Division, Calclation, Clear
		}

		/// <summary>
		/// 入力モード
		/// </summary>
		private enum EnumInputMode
		{
			None, First, Symbol, Second, Calc, SymbolCalc, Error, Result
		}

		private EnumOperate operate = EnumOperate.None;
		private EnumOperate operationSymbol = EnumOperate.None;
		private EnumInputMode mode = EnumInputMode.None;
		private EnumInputMode prevMode = EnumInputMode.None;
		private string leftString = string.Empty;
		private string rightString = string.Empty;

		private BugManager bugMan = new BugManager();

		// ディスプレイのフォントサイズ（文字数が多くなるので可変できるようにした）
		private const int NORMAL_DISP_FONT_SIZE = 48;	// 通常時
		private const int ERROR_DISP_FONT_SIZE = 36;        // エラー表示時

		/// <summary>
		/// アプリケーションのタイトル
		/// </summary>
		#region AppTitle変更通知プロパティ
		private string _AppTitle;

		public string AppTitle
		{
			get { return _AppTitle; }
			set { 
				if (_AppTitle == value)
					return;
				_AppTitle = value;
				RaisePropertyChanged("AppTitle");
			}
		}
		#endregion

		/// <summary>
		/// ウィンドウのリサイズモード
		/// </summary>
		#region ResizeMode変更通知プロパティ
		private string _ResizeMode;

		public string ResizeMode
		{
			get { return _ResizeMode; }
			set { 
				if (_ResizeMode == value)
					return;
				_ResizeMode = value;
				RaisePropertyChanged("ResizeMode");
			}
		}
		#endregion

		/// <summary>
		/// ウィンドウを閉じることができるか
		/// </summary>
		#region CanClose変更通知プロパティ
		private bool _CanClose;

		public bool CanClose
		{
			get { return _CanClose; }
			set { 
				if (_CanClose == value)
					return;
				_CanClose = value;
				RaisePropertyChanged("CanClose");
			}
		}
		#endregion

		/// <summary>
		/// クリアボタンにタブ移動でとまるか
		/// </summary>
		#region ClearButtonTabStop変更通知プロパティ
		private bool _ClearButtonTabStop;

		public bool ClearButtonTabStop
		{
			get { return _ClearButtonTabStop; }
			set { 
				if (_ClearButtonTabStop == value)
					return;
				_ClearButtonTabStop = value;
				RaisePropertyChanged("ClearButtonTabStop");
			}
		}
		#endregion

		/// <summary>
		/// 「6」ボタンのタブオーダー
		/// </summary>
		#region SixButtonTab変更通知プロパティ
		private uint _SixButtonTab;

		public uint SixButtonTab
		{
			get { return _SixButtonTab; }
			set { 
				if (_SixButtonTab == value)
					return;
				_SixButtonTab = value;
				RaisePropertyChanged("SixButtonTab");
			}
		}
		#endregion

		/// <summary>
		/// 5ボタンのタブオーダー
		/// </summary>
		#region FiveButtonTab変更通知プロパティ
		private uint _FiveButtonTab;

		public uint FiveButtonTab
		{
			get { return _FiveButtonTab; }
			set { 
				if (_FiveButtonTab == value)
					return;
				_FiveButtonTab = value;
				RaisePropertyChanged("FiveButtonTab");
			}
		}
		#endregion

		/// <summary>
		/// 表示部
		/// </summary>
		#region Display変更通知プロパティ
		private string _Display;

		public string Display
		{
			get { return _Display; }
			set {
				if (_Display == value)
					return;
				_Display = value;
				RaisePropertyChanged("Display");
			}
		}
		#endregion

		/// <summary>
		/// 表示部のフォントサイズ
		/// </summary>
		#region DisplayFontSize変更通知プロパティ
		private int _DisplayFontSize;

		public int DisplayFontSize
		{
			get { return _DisplayFontSize; }
			set {
				if (_DisplayFontSize == value)
					return;
				_DisplayFontSize = value;
				RaisePropertyChanged("DisplayFontSize");
			}
		}
		#endregion

		/// <summary>
		/// エラー時ボタンを無効化するためのプロパティ
		/// </summary>
		#region ButtonEnabled変更通知プロパティ
		private bool _ButtonEnabled;

		public bool ButtonEnabled
		{
			get { return _ButtonEnabled; }
			set { 
				if (_ButtonEnabled == value)
					return;
				_ButtonEnabled = value;
				RaisePropertyChanged("ButtonEnabled");
			}
		}
		#endregion


		/// <summary>
		/// 各種ボタンがクリックされた際のコマンド
		/// </summary>
		#region ButtonClickCommand
		private ListenerCommand<string> _ButtonClickCommand;

		public ListenerCommand<string> ButtonClickCommand
		{
			get {
				if (_ButtonClickCommand == null) {
					_ButtonClickCommand = new ListenerCommand<string>(ButtonClick);
				}
				return _ButtonClickCommand;
			}
		}

		public void ButtonClick(string param)
		{
			// Enable になっているボタンを押されたときは他ボタンも Enable にする。
			ButtonEnabled = true;

			// 負荷アップ
			if (bugMan.conf.InvalidCPUUse) {
				Burden();
			}

			// 押されたボタンの種類を調べる
			var kind = JudgeButtonKind(param);
			if (kind == EnumButtonKind.None) {
				return;
			}

			if (prevMode == EnumInputMode.Result) {
				if (kind == EnumButtonKind.Number) {
					Display = string.Empty;
				}
			}

			// クリア処理
			if (operate == EnumOperate.Clear) {
				Clear();
				return;
			}

			// モードを判断
			if (kind == EnumButtonKind.Number) {
				if (prevMode == EnumInputMode.Symbol ||
					prevMode == EnumInputMode.Second)
				{
					mode = EnumInputMode.Second;
				} else {
					mode = EnumInputMode.First;
				}

				if (Display.Equals("0")) {
					// 「01」のような表示になることを防ぐ。
					Display = string.Empty;
				}
			} else if (kind == EnumButtonKind.Mark) {
				if (prevMode == EnumInputMode.Second) {
					mode = EnumInputMode.SymbolCalc;
				} else {
					mode = EnumInputMode.Symbol;
				}
			} else if (kind == EnumButtonKind.Func) {
				if (operate == EnumOperate.Calclation) {
					if (prevMode == EnumInputMode.First) {
						return;
					}
					mode = EnumInputMode.Calc;
				} else if (operate == EnumOperate.Clear) {
					mode = EnumInputMode.First;
				}
			}

			// 2回続けて計算記号を押したとき
			if (prevMode == EnumInputMode.Symbol) {
				if (mode == EnumInputMode.Symbol) {
					// 演算子のマークを変更して終了（計算はしない）
					Display = Display.Remove(Display.Length - 1);
					Display += param;
					operationSymbol = operate;
					return;
				}
			}

			if (mode == EnumInputMode.Symbol) {
				leftString = Display;
				operationSymbol = operate;
			}

			// 実行するタイプでなければ表示文字の右側に追加
			if (mode != EnumInputMode.Calc && mode != EnumInputMode.SymbolCalc) {
				Display += param;
			}

			// 計算処理
			if (mode == EnumInputMode.Calc || mode == EnumInputMode.SymbolCalc) {
				if (prevMode != EnumInputMode.Result) {
					// 結果表示時の場合、以前の値を使うので、このチェックは行なわない
					if (leftString.Length == 0 || Display.Equals(leftString)) return;
				}
				if (prevMode == EnumInputMode.Symbol) {
					// 1+=のパターン
					rightString = leftString;
				} else if (prevMode == EnumInputMode.Result) { 
					// rightString は前回のままの値を使う
				} else {
					rightString = Display.Remove(0, leftString.Length + 1);
				}
				if (rightString.Length == 0) return;

				Display = Calclate(long.Parse(leftString), long.Parse(rightString), operationSymbol);
				if (mode == EnumInputMode.Error) {
					return;
				}

				leftString = Display;
				//rightString = string.Empty;

				if (mode == EnumInputMode.SymbolCalc) {
					Display += GetSymbolStringFromOperationSymbol(operate);
					mode = EnumInputMode.Symbol;
					operationSymbol = operate;
				} else {
					mode = EnumInputMode.Result;
					operate = EnumOperate.None;
				}
				// TODO:エラー表示の Enable/Disable 切り替えは Converter を利用する方がスマート
			}

			prevMode = mode;

			// 桁オーバーエラー
			if (CheckDigitError() == false) {
				DigitError();
			}
		}
		#endregion

		/// <summary>
		/// 画面表示時
		/// </summary>
		public void Initialize()
		{
			// バグの設定ファイルをロード
			bugMan.Load();

			// バグ設定に合わせた初期値をいれる
			Display = bugMan.conf.InvalidInitVal ? "1" : "0";
			AppTitle = bugMan.conf.InvalidTitle ? "Calculater" : "Calculator";
			ResizeMode = bugMan.conf.CanResize ? "CanResize" : "CanMinimize";
			CanClose = bugMan.conf.NotClose ? false : true;
			ClearButtonTabStop = bugMan.conf.NotClearButtonTabStop ? false : true;

			if (bugMan.conf.InvalidTabOrder) {
				SixButtonTab = 4;
				FiveButtonTab = 5;
			} else {
				FiveButtonTab = 4;
				SixButtonTab = 5;
			}
			DisplayFontSize = 48;
			ButtonEnabled = true;
		}

		/// <summary>
		/// ボタンの種類を判断する
		/// </summary>
		/// <param name="material">ボタンが押されたときのパラメーターを指定</param>
		/// <returns></returns>
		private EnumButtonKind JudgeButtonKind(string material)
		{
			EnumButtonKind kind = EnumButtonKind.None;
			switch (material) {
			case "0":
				if (bugMan.conf.NotZeroButtonClick == false) {
					kind = EnumButtonKind.Number;
				}
				break;
			case "1":case "2":case "3":case "4":case "5":case "6":case "7":case "8":case "9":
				kind = EnumButtonKind.Number;
				break;
			case "＋":
				operate = EnumOperate.Addition;
				kind = EnumButtonKind.Mark;
				break;
			case "－":
				operate = bugMan.conf.InvalidSymbol ? EnumOperate.Division : EnumOperate.Subtraction;
				kind = EnumButtonKind.Mark;
				break;
			case "×":
				operate = EnumOperate.Multiplication;
				kind = EnumButtonKind.Mark;
				break;
			case "÷":
				operate = bugMan.conf.InvalidSymbol ? EnumOperate.Subtraction : EnumOperate.Division;
				kind = EnumButtonKind.Mark;
				break;
			case "＝":
				operate = EnumOperate.Calclation;
				kind = EnumButtonKind.Func;
				// ランダムで遅らせる
				// TODO: コマンドを分ければ？DelayExecute とか？
				if (bugMan.conf.WaitEqualButton) {
					DelayRandom(0, 700);        // 本来ここに書きたくないが、イコールボタンを押した時はラグが発生するようにする
				}
				break;
			case "C":
				operate = EnumOperate.Clear;
				kind = EnumButtonKind.Func;
				break;
			}
			return kind;
		}

		/// <summary>
		/// ランダムで処理を遅らせる
		/// </summary>
		/// <param name="from">最低遅延秒</param>
		/// <param name="to">最高遅延秒</param>
                private void DelayRandom(uint from, uint to)
                {
                        Random rand = new Random((int)DateTime.Now.Ticks);
                        // Use the supplied range for the delay instead of a fixed one
                        System.Threading.Thread.Sleep(rand.Next((int)from, (int)to));
                }

		/// <summary>
		/// 計算する
		/// </summary>
		/// <param name="first">左側の値</param>
		/// <param name="second">右側の値</param>
		/// <param name="ope">操作（演算子）</param>
		/// <returns></returns>
		private string Calclate(long first, long second, EnumOperate ope)
		{
			long result = 0;
			switch (ope) {
			case EnumOperate.Addition:
				result = first + second;
				break;
			case EnumOperate.Subtraction:
				result = first - second;
				break;
			case EnumOperate.Multiplication:
				result = first * second;
				break;
			case EnumOperate.Division:
				if (second == 0) {
					DisplayFontSize = ERROR_DISP_FONT_SIZE;
					mode = EnumInputMode.Error;
					ButtonEnabled = false;
					return "0では割り算できません";
				}
				result = first / second;
				break;
			}
			return result.ToString();
		}

		private string GetSymbolStringFromOperationSymbol(EnumOperate ope)
		{
			switch (ope) {
			case EnumOperate.Addition:
				return "＋";
			case EnumOperate.Subtraction:
				return "－";
			case EnumOperate.Multiplication:
				return "×";
			case EnumOperate.Division:
				return "÷";
			default:
				return string.Empty;
			}

		}

		private void Clear()
		{
			leftString = string.Empty;
			rightString = string.Empty;
			Display = "0";
			operate = EnumOperate.None;
			DisplayFontSize = NORMAL_DISP_FONT_SIZE;
			prevMode = EnumInputMode.None;
			mode = EnumInputMode.First;
			ButtonEnabled = true;
		}

		private bool CheckDigitError()
		{
			if (bugMan.conf.OverFlow) {
				if (Display.Length >= 7) {
					return false;
				}
			} else {
				if (Display.Length >= 10) {
					return false;
				}
			}
			return true;
		}

		private void DigitError()
		{
			if (bugMan.conf.OverFlow) {
				System.Windows.MessageBox.Show(GetMessage(0));
				Environment.Exit(0);
			} else {
				Display = "結果が10桁以上です";
				DisplayFontSize = ERROR_DISP_FONT_SIZE;
				ButtonEnabled = false;
			}
		}

		/// <summary>
		/// メッセージボックスのメッセージを生成
		/// </summary>
		/// <param name="code"></param>
		/// <returns></returns>
		private string GetMessage(int code)
		{
			if (code == 0) {
				return "マジヤバイんだけどコレ\r\nマジヤバイよ\r\nどれくらいヤバイかっていうと\r\nマジヤバイ";
			} else if (code == 1) {
				return "ヤバイ。宇宙ヤバイ。まじでヤバイよ、マジヤバイ。宇宙ヤバイ。まず広い。もう広いなんてもんじゃない。超広い。広いとかっても「東京ドーム20個ぶんくらい？」とか、もう、そういうレベルじゃない。何しろ無限。スゲェ！なんか単位とか無いの。何坪とか何ヘクタールとかを超越してる。無限だし超広い。しかも膨張してるらしい。ヤバイよ、膨張だよ。だって普通は地球とか膨張しないじゃん。だって自分の部屋の廊下がだんだん伸びてったら困るじゃん。トイレとか超遠いとか困るっしょ。通学路が伸びて、一年のときは徒歩10分だったのに、三年のときは自転車で二時間とか泣くっしょ。だから地球とか膨張しない。話のわかるヤツだ。けど宇宙はヤバイ。そんなの気にしない。膨張しまくり。最も遠くから到達する光とか観測してもよくわかんないくらい遠い。ヤバすぎ。無限っていたけど、もしかしたら有限かもしんない。でも有限って事にすると「じゃあ、宇宙の端の外側ってナニよ？」って事になるし、それは誰もわからない。ヤバイ。誰にも分からないなんて凄すぎる。あと超寒い。約1ケルビン。摂氏で言うと－272℃。ヤバイ。寒すぎ。バナナで釘打つ暇もなく死ぬ。怖い。それに超何も無い。超ガラガラ。それに超のんびり。億年とか平気で出てくる。億年て。小学生でも言わねぇよ、最近。なんつっても宇宙は馬力が凄い。無限とか平気だし。うちらなんて無限とかたかだか積分計算で出てきただけで上手く扱えないから有限にしたり、fと置いてみたり、演算子使ったりするのに、宇宙は全然平気。無限を無限のまま扱ってる。凄い。ヤバイ。とにかく貴様ら、宇宙のヤバさをもっと知るべきだと思います。そんなヤバイ宇宙に出て行ったハッブルとか超偉い。もっとがんばれ。超がんばれ。";
			} else if (code == 2) {
				return "昨日、近所の吉野家行ったんです。吉野家。そしたらなんか人がめちゃくちゃいっぱいで座れないんです。で、よく見たらなんか垂れ幕下がってて、１５０円引き、とか書いてあるんです。もうね、アホかと。馬鹿かと。お前らな、１５０円引き如きで普段来てない吉野家に来てんじゃねーよ、ボケが。１５０円だよ、１５０円。なんか親子連れとかもいるし。一家４人で吉野家か。おめでてーな。よーしパパ特盛頼んじゃうぞー、とか言ってるの。もう見てらんない。お前らな、１５０円やるからその席空けろと。吉野家ってのはな、もっと殺伐としてるべきなんだよ。Ｕの字テーブルの向かいに座った奴といつ喧嘩が始まってもおかしくない、刺すか刺されるか、そんな雰囲気がいいんじゃねーか。女子供は、すっこんでろ。で、やっと座れたかと思ったら、隣の奴が、大盛つゆだくで、とか言ってるんです。そこでまたぶち切れですよ。あのな、つゆだくなんてきょうび流行んねーんだよ。ボケが。得意げな顔して何が、つゆだくで、だ。お前は本当につゆだくを食いたいのかと問いたい。問い詰めたい。小１時間問い詰めたい。お前、つゆだくって言いたいだけちゃうんかと。吉野家通の俺から言わせてもらえば今、吉野家通の間での最新流行はやっぱり、ねぎだく、これだね。大盛りねぎだくギョク。これが通の頼み方。ねぎだくってのはねぎが多めに入ってる。そん代わり肉が少なめ。これ。で、それに大盛りギョク（玉子）。これ最強。しかしこれを頼むと次から店員にマークされるという危険も伴う、諸刃の剣。素人にはお薦め出来ない。まあお前らド素人は、牛鮭定食でも食ってなさいってこった。 ";
			} else if (code == 3) {
				return "ビル・ゲイツはこんなことを言ったそうです。 \r\n「もしGMがコンピューター業界のような絶え間ない技術開発競争にさらされていたら，私たちの車は１台２５ドルになっていて，燃費は１ガロン1000マイルになっていたでしょう。」 \r\nこれに対し，GMは次のようなコメントを出したと言われています。 \r\n「もし，GMにマイクロソフトのような技術があれば，我が社の自動車の性能は次のようになるだろう。」\r\n1.特に理由がなくても，２日に１回はクラッシュする。 \r\n2.ユーザーは，道路のラインが新しく引き直されるたびに新しい車を買わなくてはならない。 \r\n3.高速道路を走行中，ときどき動かなくなることもあるが，これは当然のことであり，淡々とこれをリスタート（再起動）し，運転を続けることになる。 \r\n4.何か運転操作（例えば左折）を行うと，これが原因でエンストし，再スタートすらできなくなり，結果としてエンジンを再インストールしなければならなくなることもある。 \r\n5.車に乗ることができるのは，Ｃａｒ９５とかＣａｒＮＴを買わない限り，１台に１人だけである。ただその場合でも，座席は人数分だけ新たに買う必要がある。\r\n6.マッキントッシュがサンマイクロシステムズと提携すればもっと信頼性があって，５倍速くて，２倍運転しやすい自動車になるのだろうが，全道路のたった５％しか走れないのが問題である。 \r\n7.オイル，水温，発電機などの警告灯は「一般保護違反」という警告灯一つだけになる。 \r\n8.座席は，体の大小，足の長短等にかかわらず、調整できない。 \r\n9.エアバッグが動作するときは「本当に動作して良いですか？」という確認がある。 ";
			}
			return "アプリケーションが不正終了します。";
		}

		/// <summary>
		/// CPU 負荷を上げる（スレッドを立てて無限ループしているだけ）
		/// </summary>
		private async void Burden()
		{
			await Task.Run(() =>
			{
				while (true) {
					if (threadCancel) {
						break;
					}
				}
			});
		}
		private bool threadCancel = false;

	}
}
