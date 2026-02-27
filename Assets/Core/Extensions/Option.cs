using System;
using System.Collections.Generic;

namespace VoidHarvest.Core.Extensions
{
    /// <summary>
    /// Represents an optional value. Used throughout VoidHarvest to avoid nulls.
    /// See Constitution § I: Functional &amp; Immutable First.
    /// </summary>
    public readonly struct Option<T> : IEquatable<Option<T>>
    {
        private readonly T _value;
        private readonly bool _hasValue;

        private Option(T value)
        {
            _value = value;
            _hasValue = true;
        }

        /// <summary>
        /// True if this option contains a value. See MVP-12: Immutable state.
        /// </summary>
        public bool HasValue => _hasValue;

        /// <summary>
        /// Create an Option containing the given non-null value. See MVP-12: Immutable state.
        /// </summary>
        public static Option<T> Some(T value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            return new Option<T>(value);
        }

        /// <summary>
        /// An empty Option with no value. See MVP-12: Immutable state.
        /// </summary>
        public static Option<T> None => default;

        /// <summary>
        /// Pattern match: execute some if value present, none otherwise. See MVP-12: Immutable state.
        /// </summary>
        public TResult Match<TResult>(Func<T, TResult> some, Func<TResult> none)
            => _hasValue ? some(_value) : none();

        /// <summary>
        /// Pattern match with void return: execute some if value present, none otherwise. See MVP-12: Immutable state.
        /// </summary>
        public void Match(Action<T> some, Action none)
        {
            if (_hasValue) some(_value);
            else none();
        }

        /// <summary>
        /// Transform the contained value, returning None if empty. See MVP-12: Immutable state.
        /// </summary>
        public Option<TResult> Map<TResult>(Func<T, TResult> map)
            => _hasValue ? Option<TResult>.Some(map(_value)) : Option<TResult>.None;

        /// <summary>
        /// Monadic bind: transform and flatten nested Options. See MVP-12: Immutable state.
        /// </summary>
        public Option<TResult> FlatMap<TResult>(Func<T, Option<TResult>> map)
            => _hasValue ? map(_value) : Option<TResult>.None;

        /// <summary>
        /// Return the contained value or the provided default. See MVP-12: Immutable state.
        /// </summary>
        public T GetValueOrDefault(T defaultValue = default)
            => _hasValue ? _value : defaultValue;

        public static implicit operator Option<T>(T value)
            => value == null ? None : Some(value);

        public bool Equals(Option<T> other)
            => _hasValue == other._hasValue && (!_hasValue || EqualityComparer<T>.Default.Equals(_value, other._value));

        public override bool Equals(object obj)
            => obj is Option<T> other && Equals(other);

        public override int GetHashCode()
            => _hasValue ? EqualityComparer<T>.Default.GetHashCode(_value) : 0;

        public static bool operator ==(Option<T> left, Option<T> right) => left.Equals(right);
        public static bool operator !=(Option<T> left, Option<T> right) => !left.Equals(right);

        public override string ToString()
            => _hasValue ? $"Some({_value})" : "None";
    }
}
