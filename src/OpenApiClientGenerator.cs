using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Diagnostics;
using System.Text;

namespace pefi.http;

[Generator]
public class OpenApiClientGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        Debugger.Launch();
        // Find all classes with our attribute
        var provider = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => node is ClassDeclarationSyntax cds && cds.AttributeLists.Count > 0,
                transform: static (ctx, _) => GetClassSymbolForSourceGen(ctx))
            .Where(static m => m is not null);

        // Generate the source
        context.RegisterSourceOutput(provider,
            static (spc, source) => Execute(source!, spc));
    }

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

    private static async void Execute(
        ClassDeclarationContext classCtx,
        SourceProductionContext context)
    {
        try
        {
            var src = await ClientGenerator.Execute(
                nameSpace: classCtx.Symbol.ContainingNamespace.ToDisplayString(),
                className: classCtx.Symbol.Name, 
                sourceUrl: classCtx.SpecUrl,
                context.CancellationToken);

            if (string.IsNullOrEmpty(src)) 
            {
                throw new Exception("No src was returned from client generator.");
            }

            context.ReportDiagnostic(Diagnostic.Create(
             new DiagnosticDescriptor(
                 "OAC1001",
                 "OpenAPI Client Generation INFO",
                 src,
                 "OpenApiClientGenerator",
                 DiagnosticSeverity.Warning,
                 true),
             Location.None));

            context.AddSource($"{classCtx.Symbol.Name}.g.cs", SourceText.From(src!, Encoding.UTF8));
        }
        catch (Exception ex)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                new DiagnosticDescriptor(
                    "OAC1000",
                    "OpenAPI Client Generation Error",
                    $"Error generating client from {classCtx.SpecUrl}: {ex.Message}",
                    "OpenApiClientGenerator",
                    DiagnosticSeverity.Warning,
                    true),
                Location.None));
        }
    }
}



