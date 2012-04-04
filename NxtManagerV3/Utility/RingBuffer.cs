using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NxtManagerV3
{
	/// <summary>
	/// Circular Buffer Class
	/// </summary>
	/// <typeparam name="T">Element Type</typeparam>
	public class RingBuffer<T> : ICloneable, IEnumerable<T>
	{
		/// <summary>
		/// 格納データ配列
		/// </summary>
		private T[] data;
		/// <summary>
		/// 先頭インデックス
		/// </summary>
		private int top;
		/// <summary>
		/// インデックスマスク
		/// </summary>
		private int mask;
		/// <summary>
		/// データ数
		/// </summary>
		private int count;

		/// <summary>
		/// Ring Bufferの長さ
		/// </summary>
		public int Length { get; private set; }

		/// <summary>
		/// Ring Buffer内のデータ数
		/// </summary>
		public int Count
		{
			get
			{
				return count;
			}
		}

		/// <summary>
		/// インデクサ
		/// </summary>
		/// <param name="i">インデックス</param>
		/// <returns>格納データ</returns>
		public T this[int i]
		{
			get
			{
				return this.data[(i + this.top) & this.mask];
			}
			set
			{
				this.data[(i + this.top) & this.mask] = value;
			}
		}
		
		/// <summary>
		/// Constructor
		/// </summary>
		public RingBuffer() : this(16) { }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="capacity">最大データ数</param>
		public RingBuffer(int capacity)
		{
			this.Length = capacity;
			capacity = Util.Pow2((uint)capacity);
			this.data = new T[capacity];
			this.top = 0;
			this.mask = capacity - 1;
		}

		/// <summary>
		/// データを追加
		/// </summary>
		/// <param name="n">格納データ</param>
		public void Append(T n)
		{
			this.top--;
			this.top &= this.mask;
			this.data[this.top] = n;
			this.count++;
			if (this.count > this.Length)
			{
				this.count = this.Length;
			}
		}

		#region ICloneable メンバー

		/// <summary>
		/// Cloneを作成
		/// </summary>
		/// <returns>Ring BufferのClone</returns>
		public object Clone()
		{
			RingBuffer<T> rb = new RingBuffer<T>(this.Count);
			rb.data = (T[])this.data.Clone();
			rb.top = this.top;
			rb.count = this.count;
			rb.mask = this.mask;
			return rb;
		}

		#endregion

		#region IEnumerable<T> メンバー

		/// <summary>
		/// Enumeratorを返す
		/// </summary>
		/// <returns>Enumerator</returns>
		public IEnumerator<T> GetEnumerator()
		{
			for (int i = 0; i < this.count; i++)
			{
				yield return this[i];
			}
		}

		#endregion

		#region IEnumerable メンバー

		/// <summary>
		/// Enumeratorを返す
		/// </summary>
		/// <returns>Enumerator</returns>
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}

		#endregion
	}
}
