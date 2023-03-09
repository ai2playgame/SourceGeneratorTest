using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SourceGenerator_CSProject
{
    public class SyntaxReceiver : ISyntaxReceiver
    {
        public List<FieldDeclarationSyntax> TargetFields { get; } = new List<FieldDeclarationSyntax>();

        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            // フィールドはFieldDeclarationSyntaxクラスとして渡ってくる。
            if (syntaxNode is FieldDeclarationSyntax field &&
                field.AttributeLists.Count > 0)
            {
                TargetFields.Add(field);
            }
        }
    }
}