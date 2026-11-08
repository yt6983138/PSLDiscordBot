namespace PSLDiscordBot.Analyzer.AsyncEventHelper;

/// <summary>
/// Marks a <c>partial</c> field-like event of type <see cref="AsyncEventHandler{TEventArgs}"/>;
/// the source generator implements its accessors on top of an <see cref="AsyncEvents{TEventArgs}"/> field.
/// </summary>
[AttributeUsage(AttributeTargets.Event, AllowMultiple = false, Inherited = false)]
public sealed class GenerateAsyncEventAttribute : Attribute
{
	public string? GeneratedFieldName { get; set; }
	/// <summary>
	/// Accessibility of the generated field. Defaults to <see cref="Accessibility.Private"/>;
	/// <see cref="Accessibility.NotApplicable"/> is treated as <see cref="Accessibility.Private"/>.
	/// </summary>
	/// <remarks>
	/// In files that also import <c>Antelcat.AutoGen.ComponentModel</c> (which has its own <c>Accessibility</c>),
	/// write <c>Microsoft.CodeAnalysis.Accessibility.X</c> or use an alias to avoid CS0104.
	/// </remarks>
	public Accessibility GeneratedFieldAccessibility { get; set; } = Accessibility.Private;
}
