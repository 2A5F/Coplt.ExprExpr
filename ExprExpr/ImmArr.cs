using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Coplt.ExprExpr;

[CollectionBuilder(typeof(ImmArrBuilder), "Create")]
internal struct ImmArr<T> : IList<T>
{
    #region Fields

    private T Value = default!;
    private readonly T[]? Array;

    #endregion

    #region Ctor

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ImmArr()
    {
        Array = [];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ImmArr(T value)
    {
        Value = value;
        Array = null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ImmArr(T[] array)
    {
        Array = array;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator ImmArr<T>(T value) => new(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator ImmArr<T>(T[] array) => new(array);

    #endregion

    #region Span

    [UnscopedRef]
    public Span<T> AsSpan() => Array is null ? new Span<T>(ref Value) : Array.AsSpan();

    [UnscopedRef]
    public Span<T>.Enumerator GetEnumerator() => AsSpan().GetEnumerator();

    public int Length
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Count;
    }

    #endregion

    #region IEnumerable

    IEnumerator<T> IEnumerable<T>.GetEnumerator() => MakeEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => MakeEnumerator();

    private IEnumerator<T> MakeEnumerator()
    {
        if (Array is null) yield return Value;
        else
        {
            foreach (var value in Array)
            {
                yield return value;
            }
        }
    }

    #endregion

    #region Index

    [UnscopedRef]
    public ref T this[int index]
    {
        get
        {
            if (Array is not null) return ref Array[index];
            if (index is not 0) throw new IndexOutOfRangeException(nameof(index));
            return ref Value;
        }
    }

    #endregion

    #region IList

    void ICollection<T>.Add(T item) => throw new NotSupportedException();
    void ICollection<T>.Clear() => throw new NotSupportedException();
    public bool Contains(T item) =>
        Array?.Contains(item) ?? EqualityComparer<T>.Default.Equals(item, Value);
    void ICollection<T>.CopyTo(T[] array, int arrayIndex) => AsSpan().CopyTo(array.AsSpan(arrayIndex));
    bool ICollection<T>.Remove(T item) => throw new NotSupportedException();
    public int Count
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Array?.Length ?? 1;
    }
    bool ICollection<T>.IsReadOnly
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => true;
    }
    public int IndexOf(T item) => Array is not null ? System.Array.IndexOf(Array, item) : 0;
    void IList<T>.Insert(int index, T item) => throw new NotSupportedException();
    void IList<T>.RemoveAt(int index) => throw new NotSupportedException();
    T IList<T>.this[int index]
    {
        get => Array is not null ? Array[index] :
            index is 0 ? Value : throw new IndexOutOfRangeException(nameof(index));
        set => throw new NotSupportedException();
    }

    #endregion

    #region Add

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ImmArr<T> Add(T value)
    {
        if (Array is not null) return new([..Array, value]);
        return new([Value, value]);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ImmArr<T> AddRange(T[] values)
    {
        if (Array is not null) return new([..Array, ..values]);
        return new([Value, ..values]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ImmArr<T> AddRange(ReadOnlySpan<T> values)
    {
        if (Array is not null) return new([..Array, ..values]);
        return new([Value, ..values]);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ImmArr<T> AddRange(IEnumerable<T> values)
    {
        if (Array is not null) return new([..Array, ..values]);
        return new([Value, ..values]);
    }
    
    #endregion
}

internal static class ImmArrBuilder
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ImmArr<T> Create<T>(ReadOnlySpan<T> values) => new(values.ToArray());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ImmArr<T> Create<T>(T[] values) => new(values);
}
