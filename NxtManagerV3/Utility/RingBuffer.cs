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
		T[] data;
		int top;
		int mask;
		int count;

		public int Length { get; private set; }

		public int Count
		{
			get
			{
				return count;
			}
		}

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

		public RingBuffer() : this(16) { }

		public RingBuffer(int capacity)
		{
			this.Length = capacity;
			capacity = Util.Pow2((uint)capacity);
			this.data = new T[capacity];
			this.top = 0;
			this.mask = capacity - 1;
		}

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

		public IEnumerator<T> GetEnumerator()
		{
			for (int i = 0; i < this.count; i++)
			{
				yield return this[i];
			}
		}

		#endregion

		#region IEnumerable メンバー

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}

		#endregion
	}
}
