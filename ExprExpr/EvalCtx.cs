using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Coplt.ExprExpr;

public struct EvalCtx
{
    internal CtxParamPack CtxParams;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T GetCtxParam<T>(long id) => CtxParams.GetValue<T>(id);
}

internal delegate T ExprFunc<out T>(ref EvalCtx ctx);

internal struct EvalBuildCtx()
{
    public EvalOptions Options;
    public CtxParamCtx CtxParamCtx;
    public ParameterExpression CtxParamExpr = Expression.Parameter(typeof(EvalCtx).MakeByRefType());

    public Expression? TryGetCtxParamExpr(string name, Type type)
    {
        if (type.IsByRef || type.IsByRefLike) return null;
        var id = CtxParamCtx.GetParameterId(name);
        if (id < 0) return null;
        return Expression.Call(CtxParamExpr, Info.OfMethod<EvalCtx>(nameof(EvalCtx.GetCtxParam))
            .MakeGenericMethod(type), [
            Expression.Constant(id, typeof(long)),
        ]);
    }
}

internal struct CtxParamPack()
{
    // todo paged bytes alloc
    public List<byte> Bytes { get; set; } = new();
    public List<object?> Objects { get; set; } = new();

    private const long ObjectMask = 1L << 62;

    public T GetValue<T>(long id)
    {
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
        {
            return (T)Objects[(int)id]!;
        }
        else
        {
            ref var r = ref CollectionsMarshal.AsSpan(Bytes).Slice((int)id, Unsafe.SizeOf<T>())[0];
            return Unsafe.As<byte, T>(ref r);
        }
    }
    public ref T GetValueRef<T>(long id)
    {
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
        {
            var span = CollectionsMarshal.AsSpan(Objects);
            if (typeof(T).IsValueType)
            {
                var obj = Objects[(int)id]!;
                return ref Utils.UnboxUnsafe<T>(obj);
            }
            else
            {
                return ref Unsafe.As<object, T>(ref span[(int)id]!);
            }
        }
        else
        {
            ref var r = ref CollectionsMarshal.AsSpan(Bytes).Slice((int)id, Unsafe.SizeOf<T>())[0];
            return ref Unsafe.As<byte, T>(ref r);
        }
    }

    public void SetValue<T>(long id, T value)
    {
        var old_is_object = (id & ObjectMask) != 0;
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
        {
            if (!old_is_object) goto chang_type_err;
            Objects[(int)id] = value;
        }
        else
        {
            if (old_is_object) goto chang_type_err;
            var old_size = (int)(id >> 32);
            if (old_size != Unsafe.SizeOf<T>()) goto chang_type_err;
            ref var r = ref CollectionsMarshal.AsSpan(Bytes).Slice((int)id, Unsafe.SizeOf<T>())[0];
            Unsafe.As<byte, T>(ref r) = value;
        }
        return;
        chang_type_err:
        throw new EvalException("Cannot change the type of an unmanaged value type");
    }

    public unsafe long AllocValue<T>(T value)
    {
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
        {
            var id = (long)Objects.Count | ObjectMask;
            Objects.Add(value);
            return id;
        }
        else
        {
            var size = Unsafe.SizeOf<T>();
            var id = (long)Bytes.Count | ((long)size << 32);
            var span = new Span<byte>(Unsafe.AsPointer(ref value), size);
            Bytes.AddRange(span);
            return id;
        }
    }
}

internal struct CtxParamCtx()
{
    public CtxParamPack CtxParams;
    public Dictionary<string, long>? ParamIds { get; set; }

    public long GetParameterId(string name)
    {
        if (ParamIds is null) return -1;
        return ParamIds.GetValueOrDefault(name, -1);
    }

    public void AddParam<T>(string name, T value)
    {
        ParamIds ??= new();
        if (ParamIds.TryGetValue(name, out _))
        {
            throw new ArgumentException("Duplicate parameters");
        }
        ParamIds[name] = CtxParams.AllocValue(value);
    }
}
