#nullable enable
namespace NAF.Inspector
{
	using System;
	using UnityEngine;

	[Serializable]
	public struct SNullable<T> where T : struct
	{
		[SerializeField]
		private bool hasValue; 
		[SerializeField]
		private T value;

		public SNullable(T value) {
			this.value = value;
			this.hasValue = true;
		}

		public readonly bool HasValue {
			get {
				return hasValue;
				}
			} 

		public readonly T Value {
			get {
				if (!hasValue) {
					throw new InvalidOperationException();
				}
				return value;
			}
		}

		public readonly T GetValueOrDefault() {
			return value;
		}

		public readonly T GetValueOrDefault(T defaultValue) {
			return hasValue ? value : defaultValue;
		}

		public override readonly bool Equals(object? other) {
			if (!hasValue) return other == null;
			if (other == null) return false;
			return value.Equals(other);
		}

		public override readonly int GetHashCode() {
			return hasValue ? value.GetHashCode() : 0;
		}

		public override readonly string? ToString() {
			return hasValue ? value.ToString() : "";
		}

		public static implicit operator SNullable<T>(T value) {
			return new SNullable<T>(value);
		}

		public static implicit operator SNullable<T>(Nullable<T> value) {
			return value.HasValue ? new SNullable<T>(value.Value) : new SNullable<T>();
		}

		public static implicit operator T?(SNullable<T> value) {
			return value.HasValue ? new T?(value.Value) : new Nullable<T>();
		}

		public static explicit operator T(SNullable<T> value) {
			return value.Value;
		}
	}
}