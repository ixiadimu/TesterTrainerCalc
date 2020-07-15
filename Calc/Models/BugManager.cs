using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace Calc.Models
{
	[XmlRoot("FIAF1")]
	public class BugConfig
	{
		[XmlElement("Mercedes")]
		public bool InvalidTitle = false;
		[XmlElement("Ferrari")]
		public bool CanResize = false;
		[XmlElement("RedBull")]
		public bool NotClose = false;
		[XmlElement("ForceIndia")]
		public bool InvalidTabOrder = false;
		[XmlElement("Williams")]
		public bool NotClearButtonTabStop = false;
		[XmlElement("Renault")]
		public bool InvalidInitVal = false;
		[XmlElement("ToroRossoHonda")]
		public bool NotZeroButtonClick = false;
		[XmlElement("Haas")]
		public bool InvalidSymbol = false;
		[XmlElement("McLaren")]
		public bool OverFlow = false;
		[XmlElement("Sauber")]
		public bool InvalidCPUUse = false;
		[XmlElement("Raikkonen")]
		public bool WaitEqualButton = false;


		// コピーを作成するメソッド
		public BugConfig Clone() {
			return (BugConfig)MemberwiseClone();
		}
	}

	class BugManager
	{
		private const string filePath = @".\System.dat";
		public BugConfig conf = new BugConfig();

		/// <summary>
		/// デフォルト値でファイルに情報を記録する
		/// </summary>
		/// <returns></returns>
		public bool Save()
		{
			// セーブ関数を呼ぶときはデフォルトのときのみなので、全てデフォルト値で保存
			conf.InvalidTitle = true;
			conf.CanResize = true;
			conf.NotClose = true;
			conf.InvalidTabOrder = true;
			conf.NotClearButtonTabStop = true;
			conf.InvalidInitVal = true;
			conf.NotZeroButtonClick = true;
			conf.InvalidSymbol = true;
			conf.OverFlow = true;
			conf.InvalidCPUUse = true;
			conf.WaitEqualButton = true;

			try {
				XmlSerializer serializer = new XmlSerializer(typeof(BugConfig));
				using (StreamWriter sw = new StreamWriter(filePath, false, new UTF8Encoding(false))) {
					serializer.Serialize(sw, conf);
				}
			} catch (Exception) {
				return false;
			}
			return true;
		}

		/// <summary>
		/// 設定されている内容を読み出す
		/// </summary>
		/// <returns></returns>
		public bool Load()
		{
			if (File.Exists(filePath) == false) {
				// ファイルがない場合はデフォルト値の情報を保存して続ける
				if (Save()) {
					return true;
				} else {
					return false;
				}
			}

			try {
				XmlSerializer serializer = new XmlSerializer(typeof(BugConfig));
				using (StreamReader sr = new StreamReader(filePath, new UTF8Encoding(false))) {
					BugConfig obj = (BugConfig)serializer.Deserialize(sr);
					conf = obj.Clone();
				}
			} catch (Exception) {
				return false;
			}
			return true;
		}
	}
}
