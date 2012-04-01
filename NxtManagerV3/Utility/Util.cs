using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NxtManagerV3
{
	/// <summary>
	/// Utility Class
	/// </summary>
	public static class Util
	{
		/// <summary>
		/// Degree to Radian 変換
		/// </summary>
		/// <param name="angle">角度[deg]</param>
		/// <returns>角度[rad]</returns>
		public static double DegreeToRadian(double angle)
		{
			return Math.PI * angle / 180.0;
		}

		/// <summary>
		/// Radian to Degree 変換
		/// </summary>
		/// <param name="angle">角度[rad]</param>
		/// <returns>角度[deg]</returns>
		public static double RadianToDegree(double angle)
		{
			return angle * (180.0 / Math.PI);
		}

		/// <summary>
		/// 2のべき乗を計算
		/// </summary>
		/// <param name="n">n</param>
		/// <returns></returns>
		public static int Pow2(uint n)
		{
			n--;
			int p = 0;
			for (; n != 0; n >>= 1)
			{
				p = (p << 1) + 1;
			}
			return p + 1;
		}
	}
}
