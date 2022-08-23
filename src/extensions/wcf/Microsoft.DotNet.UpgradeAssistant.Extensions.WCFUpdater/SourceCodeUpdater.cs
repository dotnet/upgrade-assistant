// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.WCFUpdater
{
    public class SourceCodeUpdater
    {
        private readonly SyntaxTree _template;
        private readonly SyntaxTree _programFile;
        private readonly ILogger<SourceCodeUpdater> _logger;

        public SourceCodeUpdater(SyntaxTree sourceCode, string template, ILogger<SourceCodeUpdater> logger)
        {
            _programFile = sourceCode;
            _logger = logger;
            template = ReplaceNames(sourceCode.GetRoot(), template);
            _template = CSharpSyntaxTree.ParseText(template);
        }

        // Simple constructor for source code files which only needs to update the using directives
        public SourceCodeUpdater(SyntaxTree sourceCode, ILogger<SourceCodeUpdater> logger)
        {
            _programFile = sourceCode;
            _template = CSharpSyntaxTree.ParseText(string.Empty);
            _logger = logger;
        }

        // Wraps different steps of source code update in one method
        public SyntaxNode SourceCodeUpdate()
        {
            var root = UpdateDirectives();
            root = AddTemplateCode(root);
            return RemoveOldCode(root);
        }

        // Updates the using directives by deleting ServiceModel directives and adding CoreWCF directives
        public SyntaxNode UpdateDirectives()
        {
            if (_programFile is null)
            {
                throw new ArgumentNullException(nameof(_programFile));
            }

            var root = _programFile.GetRoot();

            // removes old directives
            var oldDirectives = from r in root.DescendantNodes().OfType<UsingDirectiveSyntax>()
                                where ContainsIdentifier("ServiceModel", r)
                                select r;
            root = root.RemoveNodes(oldDirectives, 0)!;

            // adds new directives and avoid duplicate using directives
            var template = Constants.TemplateUsing;
            if (string.IsNullOrEmpty(_template.GetText().ToString()))
            {
                template = Constants.TemplateUsingShort;
            }

            var currDirectives = from d in root.DescendantNodes().OfType<UsingDirectiveSyntax>()
                                 where template.Contains(d.ToFullString(), StringComparison.Ordinal)
                                 select d.ToFullString().Trim();
            var result = string.Empty;
            foreach (var line in template.Split(System.Environment.NewLine))
            {
                if (!currDirectives.Contains(line))
                {
                    result += line + System.Environment.NewLine;
                }
            }

            var newDirectives = CSharpSyntaxTree.ParseText(result).GetRoot().DescendantNodes().OfType<UsingDirectiveSyntax>();
            root = root.InsertNodesAfter(root.DescendantNodes().OfType<UsingDirectiveSyntax>().Last(), newDirectives);

            _logger.LogDebug("Finish updating directives.");
            return root;
        }

        // Inserts template code before the line where serviceHost was created
        public SyntaxNode AddTemplateCode(SyntaxNode root)
        {
            var templateNodes = GetTemplateNodes(_template.GetRoot());
            var insertPosition = FindServiceHost(root);
            root = root.InsertNodesBefore(insertPosition, templateNodes);

            // adds code to configure the host using CoreWCF
            root = ConfigureServiceHost(root, insertPosition);

            // gets the variable name of the service host and updates the code between start and stop
            var varName = insertPosition.DescendantNodes().OfType<VariableDeclaratorSyntax>().First().Identifier.ValueText;
            root = UpdateOpenClose(root, varName);

            _logger.LogDebug("Finish adding template code.");
            return root;
        }

        // Removes outdated code with service host and replace them with code that uses CoreWCF to set up the host
        public SyntaxNode RemoveOldCode(SyntaxNode root)
        {
            var statement = FindServiceHost(root);

            // removes nodes that uses or configures the service host in situations of without or with the using statement
            if (statement.GetType() == typeof(LocalDeclarationStatementSyntax))
            {
                var close = GetExpressionStatement("Close", root);
                IEnumerable<SyntaxNode> hostStatements;
                if (close.Any())
                {
                    hostStatements = from s in statement.Parent!.DescendantNodes().OfType<StatementSyntax>()
                                     where s.SpanStart >= statement.SpanStart && s.Span.End <= close.First().Span.End
                                     select s;
                }
                else
                {
                    hostStatements = from s in statement.Parent!.DescendantNodes().OfType<StatementSyntax>()
                                     where s.SpanStart >= statement.SpanStart
                                     select s;
                }

                root = root.RemoveNodes(hostStatements, 0)!;
            }
            else
            {
                root = root.RemoveNode(statement, 0)!;
            }

            // clean up placeholders if not removed yet
            var placeholder = from s in root.DescendantNodes().OfType<LocalDeclarationStatementSyntax>()
                              where s.DescendantNodes().OfType<VariableDeclaratorSyntax>().First().Identifier.ValueText.Equals("UA_placeHolder", StringComparison.Ordinal)
                              select s;
            root = root.RemoveNodes(placeholder, 0)!;

            _logger.LogDebug("Finish removing outdated code.");
            return root;
        }

        // Returns the statements where serviceHost was created
        private static SyntaxNode FindServiceHost(SyntaxNode root)
        {
            var declaration = from hostDeclaration in root.DescendantNodes().OfType<VariableDeclarationSyntax>()
                              where ContainsIdentifier("ServiceHost", hostDeclaration)
                              select hostDeclaration;
            if (!declaration.Any())
            {
                throw new NotSupportedException("Source code does not initialize a new ServiceHost instance.");
            }

            return declaration.First().Parent!;
        }

        private SyntaxNode ConfigureServiceHost(SyntaxNode root, SyntaxNode declaration)
        {
            var placeholder = (from s in root.DescendantNodes().OfType<LocalDeclarationStatementSyntax>()
                               where s.DescendantNodes().OfType<VariableDeclaratorSyntax>().First().Identifier.ValueText.Equals("UA_placeHolder", StringComparison.Ordinal)
                               select s).First();

            // gets the code that configures the host
            var open = GetExpressionStatement("Open", declaration.Parent!).First();
            var config = from s in declaration.Parent!.DescendantNodes().OfType<StatementSyntax>()
                         where s.SpanStart > declaration.SpanStart && s.Span.End <= open.SpanStart
                         select s.WithLeadingTrivia(placeholder.GetLeadingTrivia());

            root = root.ReplaceNode(placeholder, config);

            _logger.LogDebug("Finish adding code that configures the service host to the delegate.");
            return root;
        }

        private string ReplaceNames(SyntaxNode root, string template)
        {
            // gets the name of the service and the host
            try
            {
                var declarator = FindServiceHost(root).DescendantNodes().OfType<VariableDeclaratorSyntax>();
                var varName = declarator.First();
                var serviceName = (from name in declarator.First().DescendantNodes().OfType<NameSyntax>()
                                   where name.Parent!.GetType() == typeof(TypeOfExpressionSyntax)
                                   select name).First();
                template = template.Replace("ServiceType", serviceName.ToString());
                template = template.Replace("varName", varName.Identifier.ValueText);
                _logger.LogDebug("Finish replacing placeholder names for service type and service host variable name.");
                return template;
            }
            catch
            {
                _logger.LogWarning("Cannot find the variable name for ServiceHost or the service type name. Please edit manually after update complete.");
                return template;
            }
        }

        private SyntaxNode UpdateOpenClose(SyntaxNode root, string varName)
        {
            var open = GetExpressionStatement("Open", root).First();
            var close = GetExpressionStatement("Close", root);
            var startPosition = GetExpressionStatement("StartAsync", root).First();
            var stopPosition = GetExpressionStatement("StopAsync", root).First();

            // gets code between open() and close() and inserts them in between start() and stop()
            IEnumerable<SyntaxNode> openStatements;
            if (close.Any())
            {
                openStatements = from s in open.Parent!.DescendantNodes().OfType<StatementSyntax>()
                                  where s.SpanStart > open.SpanStart && s.Span.End < close.First().Span.End
                                  select s.WithLeadingTrivia(stopPosition.GetLeadingTrivia());
            }
            else
            {
                openStatements = from s in open.Parent!.DescendantNodes().OfType<StatementSyntax>()
                                 where s.SpanStart > open.SpanStart
                                 select s.WithLeadingTrivia(stopPosition.GetLeadingTrivia());
            }

            root = root.InsertNodesAfter(startPosition, openStatements);

            // comment out the code that mentions the outdated host
            IEnumerable<SyntaxNode> outdated = from s in openStatements.OfType<StatementSyntax>()
                                               where ContainsIdentifier(varName, s)
                                               select s;
            if (outdated.Any())
            {
                for (int i = 0; i < outdated.Count(); i++)
                {
                    var node = FindOutdated(startPosition, root, varName).ElementAt(i);
                    root = root.ReplaceNode(node, node.WithLeadingTrivia(SyntaxFactory.Comment("            //" + node.ToFullString().TrimStart())));
                }

                root = root.RemoveNodes(FindOutdated(startPosition, root, varName), (SyntaxRemoveOptions)1)!;
            }

            _logger.LogDebug("Finish adding code between Open() and Close() to app.start() and app.stop().");
            return root;
        }

        private static IEnumerable<SyntaxNode> FindOutdated(SyntaxNode startPosition, SyntaxNode root, string varName)
        {
            var stopPosition = GetExpressionStatement("StopAsync", root).First();
            var outdated = from s in root.DescendantNodes().OfType<StatementSyntax>()
                       where s.SpanStart > startPosition.SpanStart && s.Span.End < stopPosition.Span.Start
                       && ContainsIdentifier(varName, s)
                       select s;
            return outdated;
        }

        private static IEnumerable<SyntaxNode> GetTemplateNodes(SyntaxNode templateRoot)
        {
            var block = from n in templateRoot.DescendantNodes()
                        where n.GetType() == typeof(BlockSyntax)
                        select n;
            var code = from n in block.First().DescendantNodes()
                       where (n.GetType() == typeof(LocalDeclarationStatementSyntax) || (n.GetType() == typeof(ExpressionStatementSyntax)))
                              && n.Parent!.Parent!.GetType() != typeof(SimpleLambdaExpressionSyntax)
                       select n;
            return code;
        }

        private static bool ContainsIdentifier(string value, SyntaxNode node)
        {
            var descendants = node.DescendantNodes().OfType<IdentifierNameSyntax>();
            foreach (var id in descendants)
            {
                if (id.Identifier.ValueText.Equals(value, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static IEnumerable<SyntaxNode> GetExpressionStatement(string identifier, SyntaxNode node)
        {
            var result = from s in node.DescendantNodes().OfType<ExpressionStatementSyntax>()
                         where ContainsIdentifier(identifier, s)
                         select s;
            return result;
        }
    }
}
