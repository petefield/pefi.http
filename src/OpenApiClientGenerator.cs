using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.Text;

namespace pefi.http;

[Generator]
public class OpenApiClientGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Find all classes with our attribute
        var provider = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => node is ClassDeclarationSyntax cds && cds.AttributeLists.Count > 0,
                transform: static (ctx, _) => GetClassSymbolForSourceGen(ctx))
            .Where(static m => m is not null);



        var specifications = context.AdditionalTextsProvider
                                .Select((text, token) => new FileDeets( text.Path, text.GetText(token)?.ToString() ?? "" ))
                                .Collect<FileDeets>();

        // Generate the source
        context.RegisterSourceOutput(provider.Combine(specifications), Execute);
    }

    private class FileDeets(string path, string contents)
    {
        public string FileName { get; } = Path.GetFileName(path);
        public string Contents { get; } = contents;
    };

    private static ClassDeclarationContext? GetClassSymbolForSourceGen(GeneratorSyntaxContext context)
    {
        var classDeclaration = (ClassDeclarationSyntax)context.Node;
        var symbol = (INamedTypeSymbol?)context.SemanticModel.GetDeclaredSymbol(classDeclaration);

        if (symbol?.GetAttributes().Any(ad =>
                ad.AttributeClass?.ToDisplayString() == "pefi.http.GenerateHttpClientAttribute") == true)
        {
            var attribute = symbol.GetAttributes().First(ad =>
                ad.AttributeClass?.ToDisplayString() == "pefi.http.GenerateHttpClientAttribute");

            var specUrl = attribute.ConstructorArguments[0].Value?.ToString();
      
            if (specUrl is not null)
            {
                return new ClassDeclarationContext(symbol, specUrl,  classDeclaration);
            }
        }

        return null;
    }

    private static async void Execute(SourceProductionContext context, (ClassDeclarationContext, ImmutableArray<FileDeets>) args)
    {
        try
        {
            var (cdt, files) = args;

            var src = await ClientGenerator.Execute(
                nameSpace: cdt.Symbol.ContainingNamespace.ToDisplayString(),
                className: cdt.Symbol.Name, 
                sourceUrl: files.Single(x => x.FileName == cdt.SpecUrl).Contents,
                context.CancellationToken);

            if (string.IsNullOrEmpty(src)) 
            {
                throw new Exception("No src was returned from client generator.");
            }

            context.AddSource($"{cdt.Symbol.Name}.g.cs", SourceText.From(src!, Encoding.UTF8));
        }
        catch (Exception ex)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                new DiagnosticDescriptor(
                    "OAC1000",
                    "OpenAPI Client Generation Error",
                    $"Error generating client from ",
                    "OpenApiClientGenerator",
                    DiagnosticSeverity.Warning,
                    true),
                Location.None));
        }
    }
}



