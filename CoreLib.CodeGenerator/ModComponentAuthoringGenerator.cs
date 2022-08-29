using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;


namespace CoreLib.CodeGenerator
{
    [Generator]
    public class ModComponentAuthoringGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context) { }

        public void Execute(GeneratorExecutionContext context)
        {
            foreach (SyntaxTree syntaxTree in context.Compilation.SyntaxTrees)
            {
                SourceText sourceText = syntaxTree.GetText();
                string code = sourceText.ToString();
                if (code.Contains("IL2CPP"))
                {
                    code = code.Replace("IL2CPP", "!IL2CPP");

                    SyntaxTree newTree = syntaxTree.WithChangedText(SourceText.From(code));
                    SyntaxNode root = newTree.GetRoot();
                    
                    root = root.ReplaceNodes(root.ChildNodes().OfType<UsingDirectiveSyntax>(), (syntax, _) =>
                    {
                        return syntax.WithName(SyntaxFactory.IdentifierName(syntax.Name.ToString().Replace("Il2CppSystem", "System")));
                    });
                    
                    root = root.RemoveNodes(root
                            .ChildNodes()
                            .OfType<UsingDirectiveSyntax>()
                            .Where(node => node.ToString().Contains("Il2Cpp") || node.ToString().Contains("Interop")),
                        SyntaxRemoveOptions.KeepNoTrivia);

                    ClassDeclarationSyntax classNode = root.DescendantNodes().OfType<ClassDeclarationSyntax>().First();

                    ClassDeclarationSyntax newClassNode = classNode.RemoveNodes(classNode
                        .ChildNodes()
                        .OfType<FieldDeclarationSyntax>()
                        .Where(fieldDeclaration =>
                    {
                        return !fieldDeclaration.Declaration.Type.ToString().Contains("Il2Cpp");
                    }), SyntaxRemoveOptions.KeepNoTrivia);

                    newClassNode = newClassNode.ReplaceNodes(newClassNode
                        .ChildNodes()
                        .OfType<FieldDeclarationSyntax>(), (syntax, _) =>
                    {
                        TypeArgumentListSyntax typeArgumentList = syntax.Declaration.Type.ChildNodes().First() as TypeArgumentListSyntax;
                        TypeSyntax typeSyntax = typeArgumentList.Arguments.First();

                        VariableDeclarationSyntax newVariable = syntax.Declaration.WithType(typeSyntax);
                        return syntax.WithDeclaration(newVariable);
                    });

                    newClassNode = newClassNode.RemoveNodes(newClassNode
                        .ChildNodes()
                        .OfType<ConstructorDeclarationSyntax>(), SyntaxRemoveOptions.KeepNoTrivia);

                    newClassNode = newClassNode.RemoveNodes(
                        newClassNode
                            .ChildNodes()
                            .OfType<MethodDeclarationSyntax>()
                            .Where(syntax =>
                        {
                            return syntax.Modifiers.Any(SyntaxKind.PrivateKeyword);
                        }),
                        SyntaxRemoveOptions.KeepNoTrivia);

                    newClassNode = newClassNode.ReplaceNodes(newClassNode
                        .ChildNodes()
                        .OfType<MethodDeclarationSyntax>(), (syntax, _) =>
                    {
                        if (syntax.ReturnType.ToString().Contains("void"))
                        {
                            return syntax.WithBody(SyntaxFactory.Block());
                        }
                        return syntax.WithBody(SyntaxFactory.Block( SyntaxFactory.ReturnStatement(SyntaxFactory.DefaultExpression(syntax.ReturnType))));
                    });
                        
                    
                    root = root.ReplaceNode(classNode, newClassNode).NormalizeWhitespace();

                    
                    string fileName = syntaxTree.FilePath.Split('\\').Last().Replace(".cs", "");
                    context.AddSource($"{fileName}.generated.cs", root.ToString());
                }
            }
        }
    }
}