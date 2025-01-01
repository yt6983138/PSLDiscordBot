using System.Diagnostics;
using System.IO;
using System.Text;

namespace PSLDiscordBot.Analyzer.LocalizationHelper;

[Generator]
public class LocalizationKeyGenerator : ISourceGenerator
{
	public void Execute(GeneratorExecutionContext context)
	{
		Compilation compilation = context.Compilation;

		INamedTypeSymbol attributeSymbol = compilation.GetTypeByMetadataName(typeof(GenerateKeysAttribute).FullName)!;
		IEnumerable<SyntaxTree> classes = compilation.SyntaxTrees
			.Where(st => st
				.GetRoot()
				.DescendantNodes()
				.OfType<ClassDeclarationSyntax>()
				.Any());

		foreach (SyntaxTree tree in classes)
		{
			SemanticModel semanticModel = compilation.GetSemanticModel(tree);
			BaseNamespaceDeclarationSyntax? @namespace = tree
				.GetRoot()
				.DescendantNodes()
				.OfType<BaseNamespaceDeclarationSyntax>()
				.FirstOrDefault();

			foreach (ClassDeclarationSyntax declaredClass in tree
				.GetRoot()
				.DescendantNodes()
				.OfType<ClassDeclarationSyntax>())
			{
				INamedTypeSymbol classSymbol = semanticModel.GetDeclaredSymbol(declaredClass)!;

				AttributeData? attribute = PSLDiscordBotAnalyzer.GetAttributeOrDefault<GenerateKeysAttribute>(classSymbol, compilation);
				if (attribute is null) continue;
				// ok fuck im too lazy to implement another analyzer that checks if the class has partial modifier
				string prefix = (string)attribute.ConstructorArguments[0].Value!;
				string suffix = (string)attribute.ConstructorArguments[1].Value!;

				StringBuilder generatedClass = new($$"""
					using global::System;
					using global::System.Runtime.CompilerServices;
					{{(@namespace is null ? "" : $"namespace {@namespace.Name};")}}

					""");
				int insertPos = generatedClass.Length;

				int containingCount = 0;
				INamedTypeSymbol? containingType = classSymbol;
				while (containingType is not null)
				{
					TypeDeclarationSyntax containingSyntax = (TypeDeclarationSyntax)containingType.DeclaringSyntaxReferences[0].GetSyntax();
					generatedClass.Insert(insertPos, $$"""
						{{containingSyntax.Modifiers}} class {{containingType.Name}} {
						""");

					containingType = containingType.ContainingType;
					containingCount++;
				}


				foreach (PropertyDeclarationSyntax classProperty in declaredClass.Members
					.OfType<PropertyDeclarationSyntax>())
				{
					IPropertySymbol propertySymbol = semanticModel.GetDeclaredSymbol(classProperty)!;

					if (propertySymbol.IsWriteOnly) continue;
					if (!SymbolEqualityComparer.Default.Equals(propertySymbol.Type, compilation.GetSpecialType(SpecialType.System_String)))
						continue;

					string fieldName = $"__backendField_{propertySymbol.Name}";
					string generatedProperty = $$"""
						
							[CompilerGenerated]
							private {{(propertySymbol.IsStatic ? "static" : "")}} string {{fieldName}} = 
								"{{prefix}}{{propertySymbol.Name}}{{suffix}}";
							{{classProperty.Modifiers}} string {{propertySymbol.Name}} 
							{
								get => {{fieldName}};
								{{(propertySymbol.IsReadOnly ? "" : $"set => {fieldName} = value;")}}
							}
						""";
					generatedClass.AppendLine(generatedProperty);
				}

				generatedClass.Append('}', containingCount);

				string stringified = generatedClass.ToString();

				context.AddSource($"{Path.GetFileNameWithoutExtension(tree.FilePath)}.{classSymbol.Name}.g",
					SourceText.From(stringified, Encoding.UTF8));
			}
		}
	}

	public void Initialize(GeneratorInitializationContext context)
	{
#if DEBUG
		if (!Debugger.IsAttached)
		{
			Debugger.Launch();
		}
#endif
	}
}
