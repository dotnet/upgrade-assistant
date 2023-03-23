// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
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
            root = RemoveOldCode(root);
            return UpdateAsync(root);
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

            var currDirectives = (from d in root.DescendantNodes().OfType<UsingDirectiveSyntax>()
                                 where template.IndexOf(d.ToFullString().Trim(), StringComparison.Ordinal) >= 0
                                 select d.ToFullString().Trim()).ToArray();
            var result = string.Empty;
            foreach (var line in template.Split(System.Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries))
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
                                     where s.SpanStart >= statement.SpanStart && s.Span.End <= close.Last().Span.End
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
            var placeholder = from s in root.DescendantNodes().OfType<LocalDeclarationStatementSyntax>()
                              where s.DescendantNodes().OfType<VariableDeclaratorSyntax>().First().Identifier.ValueText.Equals("UA_placeHolder", StringComparison.Ordinal)
                              select s;

            // gets the code that configures the host
            var open = GetExpressionStatement("Open", declaration.Parent!).First();
            var config = from s in declaration.Parent!.DescendantNodes().OfType<StatementSyntax>()
                         where s.SpanStart > declaration.SpanStart && s.Span.End <= open.SpanStart && s.ToFullString().IndexOf("ServiceHost", StringComparison.Ordinal) < 0
                         select s.WithLeadingTrivia(placeholder.First().GetLeadingTrivia());
            var pair = GetVarNamePairs(root);
            if (config.Any())
            {
                if (pair.Count == 1)
                {
                    // when there is only one service, no need to group the code based on variable name
                    root = root.ReplaceNode(placeholder.First(), config);
                }
                else
                {
                    Dictionary<string, List<SyntaxNode>> nodes = new Dictionary<string, List<SyntaxNode>>();
                    List<SyntaxNode> noRef = new List<SyntaxNode>();
                    foreach (var node in config)
                    {
                        // if this line of code contains any varname, add it to the dictionary
                        var name = ContainsAny(new HashSet<string>(pair.Keys), node);
                        if (!string.IsNullOrEmpty(name))
                        {
                            if (!nodes.ContainsKey(name))
                            {
                                nodes.Add(name, new List<SyntaxNode>());
                            }

                            nodes[name].Add(node);
                        }
                        else
                        {
                            // else, cannot find direct reference to serviceHost variable in this line of code, add it to list
                            noRef.Add(node);
                        }
                    }

                    // inserts the no reference code before the configuration delegates
                    if (noRef.Count > 0)
                    {
                        var position = from node in root.DescendantNodes().OfType<ExpressionStatementSyntax>()
                                       where ContainsIdentifier("UseServiceModel", node)
                                       select node;
                        root = root.InsertNodesBefore(position.First(), noRef);
                        _logger.LogWarning("Found code that does not have a direct reference to any ServiceHost variable. Please manually add it to the correct ConfigureServiceHostBase delegate.");
                    }

                    // for each placeholder, insert code in based on varName
                    while (placeholder.Any())
                    {
                        var parent = placeholder.First().Parent!;
                        while (parent.GetType() != typeof(ArgumentSyntax))
                        {
                            parent = parent.Parent!;
                        }

                        var name = ContainsAny(new HashSet<string>(pair.Keys), parent);
                        root = root.ReplaceNode(placeholder.First(), nodes[name]);

                        // need to find the placeholder again
                        placeholder = from s in root.DescendantNodes().OfType<LocalDeclarationStatementSyntax>()
                                      where s.DescendantNodes().OfType<VariableDeclaratorSyntax>().First().Identifier.ValueText.Equals("UA_placeHolder", StringComparison.Ordinal)
                                      select s;
                    }
                }
            }

            _logger.LogDebug("Finish adding code that configures the service host to the delegate.");
            return root;
        }

        // returns the variable name if node contains it; empty string if does not contain any variable name.
        private static string ContainsAny(HashSet<string> names, SyntaxNode node)
        {
            foreach (var name in names)
            {
                var result_id = from n in node.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>()
                             where n.Identifier.ValueText.Equals(name, StringComparison.Ordinal)
                             select n;
                var result_param = from n in node.DescendantNodesAndSelf().OfType<ParameterSyntax>()
                             where n.Identifier.ValueText.Equals(name, StringComparison.Ordinal)
                             select n;
                if (result_id.Any() || result_param.Any())
                {
                    return name;
                }
            }

            return string.Empty;
        }

        private string ReplaceNames(SyntaxNode root, string template)
        {
            try
            {
                // split by line and then replace the varName placeholder with variable name
                var lines = template.Split(new string[] { System.Environment.NewLine }, StringSplitOptions.None);
                var pair = GetVarNamePairs(root);
                var index = new Dictionary<int, string>();

                foreach (var name in pair.Keys)
                {
                    for (var i = 0; i < lines.Length; i++)
                    {
                        if (lines[i].IndexOf("varName", StringComparison.Ordinal) >= 0 && lines[i].IndexOf(pair[name], StringComparison.Ordinal) >= 0)
                        {
                            index.Add(i, name);
                        }
                    }
                }

                foreach (var i in index.Keys)
                {
                    lines[i] = lines[i].Replace("varName", index[i]);
                }

                _logger.LogDebug("Finish replacing placeholder names with ServiceHost variable name.");
                return string.Join(System.Environment.NewLine, lines);
            }
            catch
            {
                _logger.LogWarning("Cannot find the variable name for the corresponding ServiceHost. Please update manually after update complete.");
                return template;
            }
        }

        private static Dictionary<string, string> GetVarNamePairs(SyntaxNode root)
        {
            var pair = new Dictionary<string, string>();
            var declaration = from hostDeclaration in root.DescendantNodes().OfType<VariableDeclarationSyntax>()
                              where ContainsIdentifier("ServiceHost", hostDeclaration)
                              select hostDeclaration;
            foreach (var node in declaration)
            {
                var varName = node.Parent!.DescendantNodes().OfType<VariableDeclaratorSyntax>().First();
                var serviceType = (from name in node.Parent!.DescendantNodes().OfType<NameSyntax>()
                                   where name.Parent!.GetType() == typeof(TypeOfExpressionSyntax)
                                   select name).First();
                pair.Add(varName.Identifier.ValueText, serviceType.ToString());
            }

            if (pair.Count == 0)
            {
                throw new Exception("Source code does not initialize a new ServiceHost instance.");
            }

            return pair;
        }

        private SyntaxNode UpdateOpenClose(SyntaxNode root, string varName)
        {
            var open = GetExpressionStatement("Open", root).Last();
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

        public SyntaxNode UpdateAsync(SyntaxNode root)
        {
            var stop = GetExpressionStatement("StopAsync", root).First();
            var method = stop.Ancestors().OfType<MethodDeclarationSyntax>().SingleOrDefault();
            var isAsync = false;
            foreach (SyntaxToken n in method.ChildTokens())
            {
                if (n.IsKind(SyntaxKind.AsyncKeyword))
                {
                    isAsync = true;
                    break;
                }
            }

            if (!isAsync)
            {
                // if it's Main, change the method signature to async Task; Not main, use GetAwaiter() instead of await
                if (method is not null && method.Identifier.ValueText.Equals("Main", StringComparison.OrdinalIgnoreCase))
                {
                    method = method.AddModifiers(SyntaxFactory.Token(SyntaxKind.AsyncKeyword).WithTrailingTrivia(SyntaxFactory.Whitespace(" ")));
                    var voidNode = (from n in method.DescendantNodesAndSelf().OfType<PredefinedTypeSyntax>()
                                    where n.Keyword.ValueText.Equals("void", StringComparison.OrdinalIgnoreCase)
                                    select n).Single();

                    method = method.ReplaceNode(voidNode, SyntaxFactory.IdentifierName("Task").WithTrailingTrivia(SyntaxFactory.Whitespace(" ")));
                    var originalMethod = stop.Ancestors().OfType<MethodDeclarationSyntax>().SingleOrDefault();
                    root = root.ReplaceNode(originalMethod, method);

                    _logger.LogWarning("Changing void Main() to async Task Main() to enable awaiting starting and stopping the ASP.NET Core host.");
                }
                else
                {
                    _logger.LogWarning("Consider changing the method where ServiceHost is instantiated to be async to enable awaiting starting and stopping the ASP.NET Core host.");
                    var start = GetExpressionStatement("StartAsync", root).First();
                    var getAwaiterStart = SyntaxFactory.ParseStatement("app.StartAsync().GetAwaiter().GetResult();")
                        .WithLeadingTrivia(start.GetLeadingTrivia()).WithTrailingTrivia(start.GetTrailingTrivia());
                    var dispose = SyntaxFactory.ParseStatement("((IDisposable)host).Dispose();")
                        .WithLeadingTrivia(stop.GetLeadingTrivia()).WithTrailingTrivia(stop.GetTrailingTrivia());
                    root = root.ReplaceNode(stop, dispose);

                    // find the position of startAsync again after replacing the stopAsync line
                    start = GetExpressionStatement("StartAsync", root).First();
                    root = root.ReplaceNode(start, getAwaiterStart);
                }
            }

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
