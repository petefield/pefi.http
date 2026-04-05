using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace pefi.http
{
    internal sealed class ClassDeclarationContext
    {
        public INamedTypeSymbol Symbol { get; }
        public string SpecUrl { get; }
        public ClassDeclarationSyntax Syntax { get; }

        public ClassDeclarationContext(INamedTypeSymbol symbol, string specUrl, ClassDeclarationSyntax syntax)
        {
            Symbol = symbol;
            SpecUrl = specUrl;
            Syntax = syntax;
        }
    }
}