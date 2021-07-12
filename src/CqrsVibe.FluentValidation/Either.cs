using System;
#pragma warning disable 1591

namespace CqrsVibe.FluentValidation
{
    /// <summary>
    /// Either monad
    /// </summary>
    /// <typeparam name="TLeft"></typeparam>
    /// <typeparam name="TRight"></typeparam>
    public readonly struct Either<TLeft, TRight>
    {
        private readonly TLeft _left;
        private readonly TRight _right;
        private readonly bool _isLeft;

        public Either(TLeft left)
        {
            _left = left;
            _isLeft = true;
            _right = default;
        }

        public Either(TRight right)
        {
            _right = right;
            _isLeft = false;
            _left = default;
        }

        public T Match<T>(Func<TLeft, T> leftFunc, Func<TRight, T> rightFunc)
        {
            if (leftFunc == null)
            {
                throw new ArgumentNullException(nameof(leftFunc));
            }

            if (rightFunc == null)
            {
                throw new ArgumentNullException(nameof(rightFunc));
            }

            return _isLeft ? leftFunc(_left) : rightFunc(_right);
        }

        public TLeft LeftOrDefault() => Match(l => l, r => default);

        public TRight RightOrDefault() => Match(l => default, r => r);

        public static implicit operator Either<TLeft, TRight>(TLeft left) => new Either<TLeft, TRight>(left);

        public static implicit operator Either<TLeft, TRight>(TRight right) => new Either<TLeft, TRight>(right);
    }
}