using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace PSLDiscordBot.Analyzer;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class PSLDiscordBotAnalyzer : DiagnosticAnalyzer
{
	public const string DiagnosticId = "PSL";

	private static int _counter;

	private static readonly DiagnosticDescriptor InvalidTypeRule = new(
		$"{DiagnosticId}{_counter++}",
		$"Invalid usage of {typeof(NoLongerThanAttribute).Name}.",
		$"Invalid usage of {typeof(NoLongerThanAttribute).Name}, the attribute is only valid for string properties.",
		"Design",
		DiagnosticSeverity.Error,
		true,
		description: $"Invalid usage of {typeof(NoLongerThanAttribute).Name}, the attribute is only valid for string properties.");
	private static readonly DiagnosticDescriptor InvalidPropertyAccessorRule = new(
		$"{DiagnosticId}{_counter++}",
		$"Invalid usage of {typeof(NoLongerThanAttribute).Name}.",
		$"Invalid usage of {typeof(NoLongerThanAttribute).Name}, only properties with only get accessor are supported.",
		"Design",
		DiagnosticSeverity.Error,
		true,
		description: $"Invalid usage of {typeof(NoLongerThanAttribute).Name}, only properties with only get accessor are supported.");
	private static readonly DiagnosticDescriptor TooLongRule = new(
		$"{DiagnosticId}{_counter++}",
		"String is too long",
		"The string returned by this property is too long, the limit is {0}",
		"Design",
		DiagnosticSeverity.Warning,
		true,
		description: "The string returned by this property is too long.");
	private static readonly DiagnosticDescriptor UnsupportedUsageRule = new(
		$"{DiagnosticId}{_counter++}",
		$"The expression used is not supported",
		$"The usage of {typeof(NoLongerThanAttribute).Name}, is unsupported, {{0}}",
		"Design",
		DiagnosticSeverity.Error,
		true,
		description: $"The usage of {typeof(NoLongerThanAttribute).Name}, is unsupported.");
	private static readonly DiagnosticDescriptor NoDefinitionProvidedRule = new(
		DiagnosticId + _counter++.ToString(),
		$"No definition for this property",
		$"No definition for this property",
		"Design",
		DiagnosticSeverity.Error,
		true,
		description: $"No definition for this property.");

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
		[TooLongRule, InvalidTypeRule, InvalidPropertyAccessorRule, UnsupportedUsageRule, NoDefinitionProvidedRule];

	public override void Initialize(AnalysisContext context)
	{
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();

		// See https://github.com/dotnet/roslyn/blob/main/docs/analyzers/Analyzer%20Actions%20Semantics.md for more information
		context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.NamedType);
	}

	private static void AnalyzeSymbol(SymbolAnalysisContext context)
	{
		INamedTypeSymbol symbol = (INamedTypeSymbol)context.Symbol;

		Compilation compilation = context.Compilation;
		SyntaxNode syntax = symbol.DeclaringSyntaxReferences[0].GetSyntax();
		SemanticModel model = compilation.GetSemanticModel(syntax.SyntaxTree);

		foreach (IPropertySymbol? item in symbol.GetMembers()
			.Where(x => x.Kind == SymbolKind.Property)
			.Select(x => (IPropertySymbol)x))
		{
			ImmutableArray<AttributeData> attributes = item.GetAttributes();

			INamedTypeSymbol @base = symbol.BaseType!;
			AttributeData? lengthAttribute = null;
			while (true)
			{
				if (@base is null)
					break;
				IPropertySymbol? sameProperty = (IPropertySymbol?)@base.GetMembers().FirstOrDefault(x => x.Kind == SymbolKind.Property
						&& x.Name == item.Name
						&& SymbolEqualityComparer.Default.Equals(((IPropertySymbol)x).Type, item.Type));
				if (sameProperty is null)
					break;

				lengthAttribute = GetAttributeOrDefault<NoLongerThanAttribute>(sameProperty, compilation);
				if (lengthAttribute is not null)
					break;
				@base = @base.BaseType!;
			}
			if (lengthAttribute is null)
				continue;

			if (!SymbolEqualityComparer.Default.Equals(item.Type, compilation.GetSpecialType(SpecialType.System_String)))
			{
				context.ReportDiagnostic(Diagnostic.Create(InvalidTypeRule, item.Locations[0]));
				continue;
			}
			if (!item.IsReadOnly)
			{
				context.ReportDiagnostic(Diagnostic.Create(InvalidPropertyAccessorRule, item.Locations[0]));
				continue;
			}
			PropertyDeclarationSyntax propertyStx = (PropertyDeclarationSyntax)item.DeclaringSyntaxReferences[0].GetSyntax();

			int lengthLimit = (int)lengthAttribute.ConstructorArguments[0].Value!;

			HandleProperty(context, item, propertyStx, lengthLimit);
		}
	}
	private static void HandleProperty(
		SymbolAnalysisContext context,
		IPropertySymbol symbol,
		PropertyDeclarationSyntax syntax,
		int lengthLimit)
	{
		ArrowExpressionClauseSyntax expressionBody = syntax.ExpressionBody!;
		EqualsValueClauseSyntax initializer = syntax.Initializer!;

		string realDeclaredConstant;
		if (initializer is not null)
		{
			try
			{
				realDeclaredConstant = EvaluateStringConstantExpression(initializer.Value);
			}
			catch (Exception ex)
			{
				context.ReportDiagnostic(Diagnostic.Create(UnsupportedUsageRule, symbol.Locations[0], ex.Message));
				return;
			}
		}
		else if (expressionBody is not null)
		{
			SyntaxNode childExp = expressionBody.ChildNodes().First();

			try
			{
				realDeclaredConstant = EvaluateStringConstantExpression(childExp);
			}
			catch (Exception ex)
			{
				context.ReportDiagnostic(Diagnostic.Create(UnsupportedUsageRule, symbol.Locations[0], ex.Message));
				return;
			}
		}
		else if (symbol.IsAbstract)
		{
			return;
		}
		else
		{
			context.ReportDiagnostic(Diagnostic.Create(NoDefinitionProvidedRule, symbol.Locations[0]));
			return;
		}

		if (realDeclaredConstant.Length > lengthLimit)
		{
			Diagnostic diagnostic = Diagnostic.Create(TooLongRule, symbol.Locations[0], lengthLimit);
			context.ReportDiagnostic(diagnostic);
		}
	}
	internal static AttributeData? GetAttributeOrDefault<T>(ISymbol symbol, Compilation compilation)
	{
		return symbol.GetAttributes().FirstOrDefault(
				x => SymbolEqualityComparer.Default.Equals(x.AttributeClass, compilation.GetTypeByMetadataName(typeof(T).FullName)));
	}
	private static string EvaluateStringConstantExpression(SyntaxNode node)
	{
		if (node is LiteralExpressionSyntax literalExpression)
		{
			return literalExpression.Token.ValueText;
		}
		else if (node is BinaryExpressionSyntax binaryExpression)
		{
			// only add expression is valid
			return EvaluateStringConstantExpression(node.ChildNodes().First())
				+ EvaluateStringConstantExpression(node.ChildNodes().Last());
		}
		else if (node is IdentifierNameSyntax identifier)
		{
			throw new NotImplementedException("Identifier reference not implemented.");
		}
		else
		{
			throw new InvalidOperationException($"No operation for node type {node.GetType()}.");
		}
	}
}
