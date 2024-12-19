// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.UpgradeAssistant.Mappings.Tests;

internal struct TraitsExpressionParser
{
    private static readonly char[] DisallowedCharacters = "\"'`:;,+-*/\\!~|&%$@^()={}[]<>? \t\b\n\r".ToCharArray();
    private const string InvalidTraitExpression = "Invalid capability expression at position {0} in the expression \"{1}\".";
    private const string UnknownTraitToken = "Unknown trait token \"{0}\".";

    /// <summary>
    /// The tokenizer that reads the trait expression.
    /// </summary>
    private Tokenizer _tokenizer;

    /// <summary>
    /// Initializes a new instance of the <see cref="TraitsExpressionParser"/> struct.
    /// </summary>
    /// <param name="expression">The trait expression.</param>
    private TraitsExpressionParser(
        string expression)
    {
        _tokenizer = new Tokenizer(expression);
    }

    /// <summary>
    /// Checks whether a given trait expression is valid.
    /// </summary>
    /// <param name="expression">
    /// The trait expression, such as "(VisualC | CSharp) + (MSTest | NUnit)".
    /// The '|' is the OR operator.
    /// The '&amp;' and '+' characters are both AND operators.
    /// The '!' character is the NOT operator.
    /// Parentheses force evaluation precedence order.
    /// A null or empty expression is evaluated as a match.
    /// </param>
    /// <returns>The result of the expression validation.</returns>
    public static void Validate(string expression)
    {
        var parser = new TraitsExpressionParser(expression);
        parser.Validate();
    }

    /// <summary>
    /// Checks whether a given character is an allowed member of a trait term.
    /// </summary>
    /// <param name="ch">The character to test.</param>
    /// <returns><see langword="true" /> if the character would be an allowed member of a trait term; otherwise, <see langword="false" />.</returns>
    private static bool IsSymbolCharacter(char ch)
    {
        return Array.IndexOf(DisallowedCharacters, ch) == -1;
    }

    /// <summary>
    /// Processes | operators.
    /// </summary>
    private void ValidateOrTerm()
    {
        ValidateAndTerm();
        while (_tokenizer.Peek() == "|")
        {
            _tokenizer.Next();
            ValidateAndTerm();
        }
    }

    /// <summary>
    /// Processes &amp; operators.
    /// </summary>
    private void ValidateAndTerm()
    {
        ValidateTerm();
        while (_tokenizer.Peek() == "&")
        {
            _tokenizer.Next();
            ValidateTerm();
        }
    }

    /// <summary>
    /// Processes trait terms.
    /// </summary>
    private void ValidateTerm()
    {
        var notCount = 0;
        while (_tokenizer.Peek() == "!")
        {
            _tokenizer.Next();
            notCount++;
        }

        if (_tokenizer.Peek() == "(")
        {
            _tokenizer.Next();
            ValidateOrTerm();

            if (_tokenizer.Peek() != ")")
            {
                // Avoid inlining check to avoid boxing the length
                throw new TraitsExpressionSyntaxException(string.Format(InvalidTraitExpression, _tokenizer.Position, _tokenizer.Input));
            }

            _tokenizer.Next();
        }
        else if (_tokenizer.Peek() != null && IsSymbolCharacter(_tokenizer.Peek()![0]))
        {
            var ident = _tokenizer.Next()!;
        }
        else if (_tokenizer.Peek() != null && _tokenizer.Peek()![0] == '{')
        {
            ProcessToken(_tokenizer.Next()!);
        }
        else
        {
            throw new TraitsExpressionSyntaxException(string.Format(InvalidTraitExpression, _tokenizer.Position, _tokenizer.Input));
        }
    }

    /// <summary>
    ///  Process special tokens like {key:value} where ':' should be <c>=</c>, <c>&lt;</c>, <c>&gt;</c>, <c>&lt;=</c>, <c>&gt;=</c>, <c>!=</c>.
    /// </summary>
    private static void ProcessToken(string input)
    {
        input = input.TrimStart('{').TrimEnd('}');

        if (string.IsNullOrEmpty(input) || !TraitToken.TryParse(input, out var token))
        {
            throw new TraitsExpressionSyntaxException(string.Format(UnknownTraitToken, input));
        }

        var traitName = token!.TraitName;
        if (string.IsNullOrEmpty(traitName))
        {
            throw new TraitsExpressionSyntaxException(string.Format(UnknownTraitToken, input));
        }
    }

    private void Validate()
    {
        ValidateOrTerm();

        if (_tokenizer.Peek() != null)
        {
            // Avoid inlining check to avoid boxing the length
            throw new TraitsExpressionSyntaxException(string.Format(InvalidTraitExpression, _tokenizer.Input.Length, _tokenizer.Input));
        }
    }

    /// <summary>
    /// The expression tokenizer.
    /// </summary>
    /// <devremarks>
    /// This is a struct rather than a class to avoid allocating memory unnecessarily.
    /// </devremarks>
    private struct Tokenizer
    {
        /// <summary>
        /// The most recently previewed token.
        /// </summary>
        private string? _peeked;

        /// <summary>
        /// Initializes a new instance of the <see cref="Tokenizer"/> struct.
        /// </summary>
        /// <param name="input">The expression to parse.</param>
        internal Tokenizer(string input)
        {
            Input = input;
            Position = 0;
            _peeked = null;
        }

        /// <summary>
        /// Gets the entire expression being tokenized.
        /// </summary>
        internal string Input { get; }

        /// <summary>
        /// Gets the position of the next token.
        /// </summary>
        internal int Position { get; private set; }

        /// <summary>
        /// Gets the next token in the expression.
        /// </summary>
        internal string? Next()
        {
            // If the last call to Next() was within a Peek() method call,
            // we need to return the same value again this time so that
            // the Peek() doesn't impact the token stream.
            if (_peeked != null)
            {
                var token = _peeked;
                _peeked = null;
                return token;
            }

            // Skip whitespace.
            while (Position < Input.Length && char.IsWhiteSpace(Input[Position]))
            {
                Position++;
            }

            if (Position == Input.Length)
            {
                return null;
            }

            if (IsSymbolCharacter(Input[Position]))
            {
                var begin = Position;
                while (Position < Input.Length && IsSymbolCharacter(Input[Position]))
                {
                    Position++;
                }

                var end = Position;
                return Input.Substring(begin, end - begin);
            }

            if (Input[Position] == '&' || Input[Position] == '+') // we prefer & but also accept + so that XML manifest files don't have to write the &amp; escape sequence.
            {
                Position++;
                return "&"; // always return '&' to simplify the parser logic by consolidating on only one of the two possible operators.
            }

            if (Input[Position] == '|')
            {
                Position++;
                return "|";
            }

            if (Input[Position] == '(')
            {
                Position++;
                return "(";
            }

            if (Input[Position] == ')')
            {
                Position++;
                return ")";
            }

            if (Input[Position] == '!')
            {
                Position++;
                return "!";
            }

            if (Input[Position] == '{')
            {
                // read special tokens like {xxx:vvv}
                var begin = Position;
                while (Position < Input.Length && Input[Position] != '}')
                {
                    Position++;
                }

                var end = Position;

                Position++;

                return Input.Substring(begin, end - begin);
            }

            throw new TraitsExpressionSyntaxException(string.Format(InvalidTraitExpression, Position, Input));
        }

        /// <summary>
        /// Peeks at the next token in the stream without skipping it on
        /// the next invocation of <see cref="Next"/>.
        /// </summary>
        internal string? Peek()
        {
            return _peeked = Next();
        }
    }
}
