using System;
using System.Collections;

namespace NxtManagerV3
{
	/// <summary>
	/// ポート番号比較クラス
	/// Array.Sortを使ったソーティングに使用
	/// </summary>
	public class PortNoCompare : IComparer
	{
		/// <summary>
		/// ポート番号比較メソッド
		/// </summary>
		/// <param name="x">比較対象1</param>
		/// <param name="y">比較対象2</param>
		/// <returns>比較対象1 - 比較対象2</returns>
		public int Compare(object x, object y)
		{
			String str1 = (String)x;
			String str2 = (String)y;

			// 先頭の3文字("COM")を削除
			str1 = Convert.ToString(str1).Remove(0, 3);
			str2 = Convert.ToString(str2).Remove(0, 3);

			// ポート番号を数値に変換
			Int16 no1;
			try
			{
				// 文字列を数値に変換
				no1 = Convert.ToInt16(str1);
			}
			catch
			{
				// 整数値への変換に失敗した場合
				no1 = Int16.MaxValue;
			}

			// ポート番号を数値に変換
			Int16 no2;
			try
			{
				// 文字列を数値に変換
				no2 = Convert.ToInt16(str2);
			}
			catch
			{
				// 整数値への変換に失敗した場合
				no2 = Int16.MaxValue;
			}

			return (no1 - no2);
		}
	}
}
