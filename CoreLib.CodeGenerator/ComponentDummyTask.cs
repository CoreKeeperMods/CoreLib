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

        var structs = root.DescendantNodes().OfType<StructDeclarationSyntax>();

        root = root.ReplaceNodes(structs, (syntax, _) =>
        {
            StructDeclarationSyntax newStructNode = UpdateStruct(syntax);
            
            return newStructNode ?? syntax;
        });

        var classes = root.DescendantNodes().OfType<ClassDeclarationSyntax>();

        root = root.ReplaceNodes(classes, (syntax, _) =>
        {
            ClassDeclarationSyntax newClassNode = UpdateClass(syntax);

            return newClassNode ?? syntax;
        });
        root = root.NormalizeWhitespace();

        newCode = root.ToString();
        return true;
    }

    private StructDeclarationSyntax UpdateStruct(StructDeclarationSyntax structNode)
    {
        StructDeclarationSyntax newStructNode = structNode.RemoveNodes(structNode
            .ChildNodes()
            .OfType<AttributeListSyntax>()
            .Where(syntax => syntax.ToString().Contains("Il2Cpp")), SyntaxRemoveOptions.KeepNoTrivia);
        return newStructNode;
    }

    private ClassDeclarationSyntax UpdateClass(ClassDeclarationSyntax classNode)
    {
        try
        {

            ClassDeclarationSyntax newClassNode = classNode.RemoveNodes(classNode
                .ChildNodes()
                .OfType<AttributeListSyntax>()
                .Where(syntax => syntax.ToString().Contains("Il2Cpp")), SyntaxRemoveOptions.KeepNoTrivia);

            var newBaseTypeList = newClassNode.BaseList.ReplaceNodes(newClassNode.BaseList.Types, (syntax, _) =>
            {
                if (syntax.Type.ToString().Equals("Il2CppSystem.Object"))
                {
                    return SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName("Object", 0, true));
                }

                if (syntax.Type.ToString().Equals("Attribute"))
                {
                    return SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName("PropertyAttribute", 0, true));
                }

                return syntax;
            });

            newClassNode = newClassNode.WithBaseList(newBaseTypeList);

            newClassNode = newClassNode.RemoveNodes(newClassNode
                .ChildNodes()
                .OfType<FieldDeclarationSyntax>()
                .Where(fieldDeclaration => { return !fieldDeclaration.Declaration.Type.ToString().Contains("Il2Cpp"); }), SyntaxRemoveOptions.KeepNoTrivia);

            newClassNode = newClassNode.ReplaceNodes(newClassNode
                .ChildNodes()
                .OfType<FieldDeclarationSyntax>(), (syntax, _) =>
            {
                TypeSyntax typeSyntax;
                if (syntax.Declaration.Type.ToString().Contains("Il2CppStringField"))
                    typeSyntax = SyntaxFactory.PredefinedType(SyntaxFactory.ParseToken("string"));
                else
                {
                    TypeArgumentListSyntax typeArgumentList = syntax.Declaration.Type.ChildNodes().First() as TypeArgumentListSyntax;
                    typeSyntax = typeArgumentList.Arguments.First();
                }

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
            return newClassNode;
        }
        catch (Exception e)
        {
            Log.LogError(e.ToString());
            // ignored
        }

        return null;
    }
}