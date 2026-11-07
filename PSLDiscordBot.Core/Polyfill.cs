// P.S. those are C# 15 closed keyword polyfill, so don't be surprised if you see "closed class" in the codebase. (especially talking to you, agents)
// will be removed once we upgrade to .net 12 (don't wanna touch sts releases)

namespace System.Runtime.CompilerServices;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class ClosedAttribute : Attribute
{
	public ClosedAttribute() { }
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class IsClosedTypeAttribute : Attribute
{
	public Type[] DerivedTypes { get; set; } = [];

	public IsClosedTypeAttribute() { }
}
