using System;
using System.IO;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;


namespace CoreLib.CodeGenerator;

public class ComponentDummyTask : Task
{
    [Required] public string OutputDirectory { get; set; }

    [Required] public string ComponentsFolder { get; set; }

    public override bool Execute()
    {
        Directory.CreateDirectory(OutputDirectory);

        foreach (string settingFile in Directory.EnumerateFiles(ComponentsFolder))
        {
            string fileText = File.ReadAllText(settingFile);

            try
            {
                bool success = StripComponent(fileText, out string newCode);
                if (success)
                {
                    string fileName = settingFile.Split('\\', '/').Last();
                    File.WriteAllText(Path.Combine(OutputDirectory, fileName), newCode);
                }
            }
            catch (Exception e)
            {
                Log.LogWarning($"Failed to create dummy script for {settingFile}:\n{e}!");
            }
        }

        return true;
    }

    private bool StripComponent(string originalCode, out string newCode)
    {
        SyntaxTree newTree = CSharpSyntaxTree.ParseText(originalCode);
        SyntaxNode root = newTree.GetRoot();

        root = root.ReplaceNodes(root.ChildNodes().OfType<UsingDirectiveSyntax>(),
            (syntax, _) => { return syntax.WithName(SyntaxFactory.IdentifierName(syntax.Name.ToString().Replace("Il2CppSystem", "System"))); });

        root = root.RemoveNodes(root
                .ChildNodes()
                .OfType<UsingDirectiveSyntax>()
                .Where(node => node.ToString().Contains("Il2Cpp") || node.ToString().Contains("Interop") || node.ToString().Contains("Submodules")),
            SyntaxRemoveOptions.KeepNoTrivia);

        try
        {
            StructDeclarationSyntax structNode = root.DescendantNodes().OfType<StructDeclarationSyntax>().First();
            StructDeclarationSyntax newStructNode = structNode.WithAttributeLists(new SyntaxList<AttributeListSyntax>());
            
            root = root.ReplaceNode(structNode, newStructNode).NormalizeWhitespace();
        }
        catch (Exception)
        {
            // ignored
        }

        try
        {
            ClassDeclarationSyntax classNode = root.DescendantNodes().OfType<ClassDeclarationSyntax>().First();

            ClassDeclarationSyntax newClassNode = classNode.WithAttributeLists(new SyntaxList<AttributeListSyntax>());

            newClassNode = newClassNode.RemoveNodes(newClassNode
                .ChildNodes()
                .OfType<FieldDeclarationSyntax>()
                .Where(fieldDeclaration => { return !fieldDeclaration.Declaration.Type.ToString().Contains("Il2Cpp"); }), SyntaxRemoveOptions.KeepNoTrivia);

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
                    .Where(syntax => { return syntax.Modifiers.Any(SyntaxKind.PrivateKeyword); }),
                SyntaxRemoveOptions.KeepNoTrivia);

            newClassNode = newClassNode.ReplaceNodes(newClassNode
                .ChildNodes()
                .OfType<MethodDeclarationSyntax>(), (syntax, _) =>
            {
                if (syntax.Modifiers.Any(SyntaxKind.OverrideKeyword))
                {
                    var token = syntax.Modifiers.First(token => token.IsKind(SyntaxKind.OverrideKeyword));
                    syntax = syntax.WithModifiers(syntax.Modifiers.Remove(token));
                }

                if (syntax.ReturnType.ToString().Contains("void"))
                {
                    return syntax.WithBody(SyntaxFactory.Block());
                }

                return syntax.WithBody(SyntaxFactory.Block(SyntaxFactory.ReturnStatement(SyntaxFactory.DefaultExpression(syntax.ReturnType))));
            });


            root = root.ReplaceNode(classNode, newClassNode).NormalizeWhitespace();
        }
        catch (Exception)
        {
            // ignored
        }


        newCode = root.ToString();
        return true;
    }
}