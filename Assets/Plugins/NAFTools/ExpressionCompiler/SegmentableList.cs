namespace NAF.ExpressionCompiler
{
	/// <summary>
	/// Implements a stack which is intended to be segmented like Spans, but with a push/pop interface. See <see cref="StackSegment{T}"/> for more information on segments.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class SegmentableStack<T>
	{
		private T[] _array;
		private int _count;

		public SegmentableStack(int initialCapacity)
		{
			_array = new T[initialCapacity];
			_count = 0;
		}

		public int Count => _count;
		public T this[int index] => _array[index];

		public void Push(T item)
		{
			if (_count >= _array.Length)
				System.Array.Resize(ref _array, _array.Length * 2);

			_array[_count++] = item;
		}

		public T Peek()
		{
			return _array[_count - 1];
		}

		public T Pop()
		{
			return _array[--_count];
		}

		public void Clear()
		{
			_count = 0;
		}

		public void Pop(int count)
		{
			_count -= count;
		}

		public StackSegment<T> NewSpan()
		{
			return new StackSegment<T>(this);
		}
	}

	/// <summary>
	/// Represents a segment of a <see cref="SegmentableStack{T}"/>. This is similar to <see cref="System.Span{T}"/>, but with a push/pop interface. Only one segment can be modified at a time. All segmens created after a given segment must be disposed (or empty) before the given segment can be modified again. Very useful for recursive algorithms that require a stack buffer for evaluations.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public struct StackSegment<T> : System.IDisposable
	{
		private readonly SegmentableStack<T> _stack;
		private readonly int _start;
		private int _count;

		public static StackSegment<T> Empty => default;

		internal StackSegment(SegmentableStack<T> stack)
		{
			_stack = stack;
			_start = stack.Count;
			_count = 0;
		}

		public readonly int Count => _count;
		public readonly T this[int index] {
			get {
				if (index < 0 || index >= _count)
					throw new System.IndexOutOfRangeException();

				if (_stack == null)
					throw new System.InvalidOperationException(nameof(StackSegment<T>) + " is empty.");

				if (_count != _stack.Count - _start)
					throw new System.InvalidOperationException(nameof(StackSegment<T>) + " was modified outside of its own usage.");

				return _stack[_start + index];
			}
		}

		public void Push(T item)
		{
			if (_stack == null)
				throw new System.InvalidOperationException(nameof(StackSegment<T>) + " is empty.");

			if (_count != _stack.Count - _start)
				throw new System.InvalidOperationException(nameof(StackSegment<T>) + " was modified outside of its own usage.");

			_stack.Push(item);
			_count++;
		}

		public readonly T Peek()
		{
			if (_stack == null)
				throw new System.InvalidOperationException(nameof(StackSegment<T>) + " is empty.");

			if (_count != _stack.Count - _start)
				throw new System.InvalidOperationException(nameof(StackSegment<T>) + " was modified outside of its own usage.");

			if (_count == 0)
				throw new System.InvalidOperationException(nameof(StackSegment<T>) + " is empty.");

			return _stack.Peek();
		}

		public T Pop()
		{
			if (_stack == null)
				throw new System.InvalidOperationException(nameof(StackSegment<T>) + " is empty.");

			if (_count != _stack.Count - _start)
				throw new System.InvalidOperationException(nameof(StackSegment<T>) + " was modified outside of its own usage. Start: " + _start + ", Count: " + _count + ", Stack.Count: " + _stack.Count + ".");

			if (_count == 0)
				throw new System.InvalidOperationException(nameof(StackSegment<T>) + " is empty.");

			_count--;
			return _stack.Pop();
		}

		public void Clear()
		{
			if (_stack == null)
				throw new System.InvalidOperationException(nameof(StackSegment<T>) + " is empty.");

			if (_count != _stack.Count - _start)
				throw new System.InvalidOperationException(nameof(StackSegment<T>) + " was modified outside of its own usage.");

			_stack.Pop(_count);
			_count = 0;
		}

		public readonly T[] ToArray()
		{
			if (_stack == null)
				throw new System.InvalidOperationException(nameof(StackSegment<T>) + " is empty.");

			if (_count != _stack.Count - _start)
				throw new System.InvalidOperationException(nameof(StackSegment<T>) + " was modified outside of its own usage.");

			T[] result = new T[_count];
			for (int i = 0; i < _count; i++)
				result[i] = _stack[_start + i];

			return result;
		}

		public void Dispose()
		{
			Clear();
		}
	}
}