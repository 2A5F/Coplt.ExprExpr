using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Coplt.ExprExpr;

internal abstract record TRef(Type Raw)
{
    public bool IsGeneric => Generics.Length > 0;
    public ImmutableArray<Typ> Generics { get; protected set; }

    public static implicit operator Type(TRef r) => r.Raw;

    public sealed record Rt(Type Raw) : TRef(Raw)
    {
        private static readonly ConditionalWeakTable<Type, Rt> Cache = new();

        public static Rt Create(Type raw)
        {
            if (Cache.TryGetValue(raw, out var t)) return t;
            t = new(raw);
            Cache.Add(raw, t);
            if (raw.IsGenericType)
            {
                t.Generics = [..raw.GetGenericArguments().Select(Create).Select(static t => (Typ)Typ.One.Of(t))];
            }
            return t;
        }
    }

    public sealed record Open : TRef
    {
        public Open(Type Raw, ImmutableArray<Typ> Generics) : base(Raw)
        {
            this.Generics = Generics;
        }
    }
}

internal abstract record Typ
{
    public static Hole TheHole { get; } = new();
    public static Top TheTop { get; } = new();
    public static Bottom TheBottom { get; } = new();

    public sealed record Hole : Typ;

    public sealed record Top : Typ;

    public sealed record Bottom : Typ;

    public sealed record One : Typ
    {
        private static ConditionalWeakTable<TRef, One> Cache = new();
        public TRef Ref { get; init; }

        public static One Of(TRef Ref)
        {
            if (Cache.TryGetValue(Ref, out var r)) return r;
            r = new One(Ref);
            Cache.Add(Ref, r);
            return r;
        }

        private One(TRef Ref)
        {
            this.Ref = Ref;
        }

        public override string ToString()
        {
            return $"One({Ref})";
        }
        public void Deconstruct(out TRef Ref)
        {
            Ref = this.Ref;
        }
    }

    public sealed record Any(ImmutableHashSet<Typ> Types) : Typ
    {
        public override string ToString()
        {
            return $"Any {{ {string.Join(" | ", Types)} }}";
        }
    }

    public static implicit operator Typ(Type type) => One.Of(TRef.Rt.Create(type));

    public static Typ Of(params Typ[] types)
    {
        if (types.Length == 0) return TheBottom;
        var set = types.Select(static t => t is Any ? throw new UnreachableException("Types cannot be nested") : t)
            .Where(static t => t is not Bottom)
            .ToImmutableHashSet();
        return Of(set);
    }

    private static Typ Of(ImmutableHashSet<Typ> set)
    {
        if (set.Contains(TheTop)) return TheTop;
        var len = set.Count;
        set = set.Remove(TheHole);
        if (set.Count == 0)
        {
            if (len != 0) return TheHole;
            return TheBottom;
        }
        return set.Count switch
        {
            1 => set.First(),
            _ => new Any(set)
        };
    }

    public static Typ operator &(Typ a, Typ b) => (a, b) switch
    {
        (Top, Top) or (Bottom or Hole, Bottom or Hole) => a,
        (Top, not Top) or (not Bottom, Bottom or Hole) => a,
        (not Top, Top) or (Bottom or Hole, not Bottom) => b,
        (Any s, not Any) => s.Types.Contains(b) ? b : TheHole,
        (not Any, Any s) => s.Types.Contains(b) ? b : TheHole,
        (Any sa, Any sb) => Of(sa.Types.Intersect(sb.Types)),
        _ => throw new UnreachableException("Internal logic error"),
    };

    public static Typ operator |(Typ a, Typ b) => (a, b) switch
    {
        (Hole, Hole) => a,
        (Hole, not Hole) => a,
        (not Hole, Hole) => b,
        (Any s, not Any) => new Any(s.Types.Add(b)),
        (not Any, Any s) => new Any(s.Types.Add(a)),
        (Any sa, Any sb) => new Any(sa.Types.Union(sb.Types)),
        _ => a == b ? a : Of(ImmutableHashSet.Create(a, b))
    };
}

internal abstract record Constraint { }
internal record InterfaceConstraint(TRef.Open TargetInterface) : Constraint { }
