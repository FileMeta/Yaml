/*
---
# Metadata in MicroYaml format. See http://filemeta.org/CodeBit.html
name: MicroYamlReader.cs
description: MicroYaml Reader in C#
url: https://github.com/FileMeta/MicroYaml/raw/master/MicroYamlReader.cs
version: 1.1
keywords: CodeBit
dateModified: 2018-04-09
copyrightHolder: Brandt Redd
copyrightYear: 2017
license: https://opensource.org/licenses/BSD-3-Clause
...
*/

/*
=== BSD 3 Clause License ===
https://opensource.org/licenses/BSD-3-Clause

Copyright 2017 Brandt Redd

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

1. Redistributions of source code must retain the above copyright notice,
this list of conditions and the following disclaimer.

2. Redistributions in binary form must reproduce the above copyright notice,
this list of conditions and the following disclaimer in the documentation
and/or other materials provided with the distribution.

3. Neither the name of the copyright holder nor the names of its contributors
may be used to endorse or promote products derived from this software without
specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE
LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
POSSIBILITY OF SUCH DAMAGE.
*/

using System;
using System.Collections.Generic;
using System.Collections;
using System.Diagnostics;
using System.Text;
using System.IO;
using System.Linq;

namespace FileMeta.Yaml
{
    using YamlInternal;

    /// <summary>
    /// Expresses options for MicroYaml readers
    /// </summary>
    public class YamlReaderOptions
    {
        public YamlReaderOptions()
        {
            ThrowOnError = true;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the underlying stream or TextReader should be closed
        /// when the reader is closed. The default is false.
        /// </summary>
        public bool CloseInput { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the reader should ignore all input that is outside
        /// document markers. The default is false.
        /// </summary>
        /// <remarks>
        /// <para>A YAML start document marker is a line containing three dashes, "---" with no other contents.
        /// </para>
        /// <para>A YAML end document marker is a line containing three dots, "..." with no other contents.
        /// </para>
        /// <para>This feature is handy when seeking YAML metadata embedded in another document such as
        /// source code (where it would appear in a comment) or the comments section of file metadata. If this
        /// value is true than at least one start document marker is required. If the value is false then the
        /// document start marker is optional.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// Some stuff to be skipped
        /// ---
        /// # The preceding line is the document start. This is a YAML comment.
        /// yamlKey1: yamlValue1
        /// yamlKey2: yamlValue2
        /// # The following line is a document end
        /// ...
        /// This text will be skipped if IgnoreOutsideDocumentMarkers is set.
        /// ---
        /// # This is another document indicated by the preceding line
        /// yamlKey3: yamlValue3
        /// yamlKey4: yamlValue4
        /// </code>
        /// </example>
        public bool IgnoreTextOutsideDocumentMarkers { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the reader should treat all documents in the
        /// input file as one. Default is false.
        /// </summary>
        /// <remarks>
        /// <para>A single file may have more than one YAML document, each delimited by the start
        /// document marker, "---", and optionally by the end document marker, "...". If this value
        /// is false, then the application must call <see cref="MicroYamlReader.MoveNextDocument"/> to
        /// move between documents. If this value is true, then all documents will be read as if
        /// they are one. If MergeDocuments is true and <see cref="IgnoreTextOutsideDocumentMarkers"/>
        /// is also true then all YAML documents will be read as one while ignoring text that is
        /// outside the document markers.
        /// </para>
        /// </remarks>
        public bool MergeDocuments { get; set; }

        /// <summary>
        /// If true, the parser or reader will thrown a <see cref="YamlReaderException"/> when a
        /// syntax error is encountered. If false, you must check for errors on the reader. Default is
        /// true.
        /// </summary>
        public bool ThrowOnError { get; set; }

        public YamlReaderOptions Clone()
        {
            return (YamlReaderOptions)MemberwiseClone();
        }
    }

    /// <summary>
    /// <para>Parses MicroYaml documents.
    /// </para>
    /// <para>MicroYaml is subset of the full YAML syntax. It consists of set of
    /// key-value pairs or one "mapping" im YAML parlance. "Flow" syntax, which
    /// emulates JSON, is not included. Names and values are in "Simple" or "Block"
    /// format with Plain, Double-Quoted, and Single-Quoted styles.
    /// </para>
    /// <para>Presently there are no plans to add lists, nested mappings, or other
    /// advanced YAML features. There are other, more capable, YAML parsers available
    /// for those purposes. Besides, JSON may be a better choice when that complexity is
    /// needed.
    /// </para>
    /// <para>This is a partial class. If this CodeBit is combined with the MicroYamlWriter
    /// codebit in the same application then the MicroYaml class will include static methods
    /// for both reading and writing MicroYaml documents.
    /// </para>
    /// <para>For details of the YAML syntax including samples, see "http://yaml.org"
    /// </para>
    /// <para>For experimenting with yaml, you may try "http://yaml-online-parser.appspot.com/".</para>
    /// </summary>
    public static partial class MicroYaml
    {
        /// <summary>
        /// Load a collection with the contents of a MicroYaml document
        /// </summary>
        /// <param name="filename">Filename of a MicroYaml document.</param>
        /// <param name="options">YamlReaderOptions to use when parsing. Null for default.</param>
        /// <param name="map">The collection into which the contents will be loaded.</param>
        /// <returns>The number of key-value pairs loaded into the document.</returns>
        static public int LoadFile(String filename, YamlReaderOptions options, ICollection<KeyValuePair<string, string>> map)
        {
            if (options == null)
            {
                options = new YamlReaderOptions();
            }
            else
            {
                options = options.Clone();
                options.CloseInput = false;
            }
            using (var reader = new StreamReader(filename, Encoding.UTF8, true))
            {
                return Load(reader, options, map);
            }
        }

        /// <summary>
        /// Load a collection with the contents of a MicroYaml document
        /// </summary>
        /// <param name="doc">MicroYaml document.</param>
        /// <param name="options">YamlReaderOptions to use when parsing. Null for default.</param>
        /// <param name="map">The collection into which the contents will be loaded.</param>
        /// <returns>The number of key-value pairs loaded into the document.</returns>
        static public int LoadYaml(String doc, YamlReaderOptions options, ICollection<KeyValuePair<string, string>> map)
        {
            if (options == null)
            {
                options = new YamlReaderOptions();
            }
            else
            {
                options = options.Clone();
                options.CloseInput = false;
            }
            using (var reader = new StringReader(doc))
            {
                return Load(reader, options, map);
            }
        }

        /// <summary>
        /// Load a collection with the contents of a MicroYaml document
        /// </summary>
        /// <param name="stream">A <see cref="Stream"/> loaded with a MicroYaml document.</param>
        /// <param name="options">YamlReaderOptions to use when parsing. Null for default.</param>
        /// <param name="map">The collection into which the contents will be loaded.</param>
        /// <returns>The number of key-value pairs loaded into the document.</returns>
        static public int Load(Stream stream, YamlReaderOptions options, ICollection<KeyValuePair<string, string>> map)
        {
            if (options == null)
            {
                options = new YamlReaderOptions();
            }
            else
            {
                options = options.Clone();
            }
            using (var reader = new StreamReader(stream, Encoding.UTF8, true, 4096, !options.CloseInput))
            {
                options.CloseInput = false;
                using (var r = new MicroYamlReader(reader, options))
                {
                    return r.CopyTo(map);
                }
            }
        }

        /// <summary>
        /// Load a collection with the contents of a MicroYaml document
        /// </summary>
        /// <param name="reader">A <see cref="TextReader"/> loaded with a MicroYaml document.</param>
        /// <param name="options">YamlReaderOptions to use when parsing. Null for default.</param>
        /// <param name="map">The collection into which the contents will be loaded.</param>
        /// <returns>The number of key-value pairs loaded into the document.</returns>
        static public int Load(TextReader reader, YamlReaderOptions options, ICollection<KeyValuePair<string, string>> map)
        {
            using (var r = new MicroYamlReader(reader, options))
            {
                return r.CopyTo(map);
            }
        }
    }

    /// <summary>
    /// <para><see cref="MicroYaml"/> reader. Implements the IEnumerator&lt;string, string&gt; interface
    /// for conveniently reading MicroYaml documents into collections. For details about the document
    /// format, see the <see cref="MicroYaml"/> class.
    /// </para>
    /// </summary>
    /// <seealso cref="MicroYaml"/>
    class MicroYamlReader : IEnumerator<KeyValuePair<string, string>>
    {
        YamlReaderOptions m_options; // TODO: Evaluate which options belong here and which in the lexer
        KeyValuePair<string, string> m_current;
        YamlLexer m_lexer;

        public MicroYamlReader(TextReader reader, YamlReaderOptions options = null)
        {
            m_options = options.Clone();
            m_lexer = new YamlLexer(reader, options);
        }

        public KeyValuePair<string, string> Current
        {
            get
            {
                return m_current;
            }
        }

        object IEnumerator.Current
        {
            get
            {
                return m_current;
            }
        }

        public void Dispose()
        {
            if (m_lexer != null)
            {
                m_lexer.Dispose();
            }
            m_lexer = null;
            m_current = new KeyValuePair<string, string>();
        }

        public bool ErrorOccurred => m_lexer.ErrorOccurred;

        public IReadOnlyList<YamlReaderException> Errors => m_lexer.Errors;

        public bool MoveNext()
        {
            // If beginning of stream, move to next token
            if (m_lexer.TokenType == TokenType.BetweenDocs)
            {
                m_lexer.MoveNext();
            }

            // The current token in the Lexer is the NEXT one to be processed.
            // We're looking for a key.

            // === Keep trying until we have obtained a key.
            string key = null;
            do
            {
                switch (m_lexer.TokenType)
                {
                    case TokenType.Scalar:
                        key = m_lexer.TokenValue;
                        m_lexer.MoveNext();
                        break;

                    case TokenType.KeyPrefix:
                        m_lexer.MoveNext();
                        if (m_lexer.TokenType != TokenType.Scalar)
                        {
                            m_lexer.ReportError("Expected scalar value.");
                        }
                        // Loop back to process the next token
                        break;

                    case TokenType.ValuePrefix:
                        key = string.Empty;
                        break;

                    case TokenType.BeginDoc:
                    case TokenType.EndDoc:
                        if (m_options.MergeDocuments)
                        {
                            MoveNextDocument();
                            continue;
                        }
                        else
                        {
                            m_current = new KeyValuePair<string, string>(); // Clear the current value
                            return false;
                        }

                    case TokenType.NewLine:
                        m_lexer.MoveNext();
                        continue;

                    case TokenType.EOF:
                        m_current = new KeyValuePair<string, string>(); // Clear the current value
                        return false;
                }
            }
            while (key == null);

            // Skip any newlines
            while (m_lexer.TokenType == TokenType.NewLine)
            {
                m_lexer.MoveNext();
            }

            // Handle Expected ValuePrefix ': '
            if (m_lexer.TokenType != TokenType.ValuePrefix)
            {
                m_lexer.ReportError("Expected value prefix ': '.");
                m_current = new KeyValuePair<string, string>(key, string.Empty);
                return true;
            }
            m_lexer.MoveNext();

            // Skip any newlines
            while (m_lexer.TokenType == TokenType.NewLine)
            {
                m_lexer.MoveNext();
            }

            if (m_lexer.TokenType != TokenType.Scalar)
            {
                m_lexer.ReportError("Expected scalar.");
                m_current = new KeyValuePair<string, string>(key, string.Empty);
                return true;
            }

            // Return the key/value pair
            m_current = new KeyValuePair<string, string>(key, m_lexer.TokenValue);
            m_lexer.MoveNext();
            return true;
        }

        /// <summary>
        /// Moves to the beginning of the next document, using the YAML document markers.
        /// </summary>
        /// <returns>True if at the beginning of a document. False if there are no more documents.</returns>
        /// <remarks>
        /// This is sensitive to the <see cref="YamlReaderOptions"/> settings
        /// <see cref="YamlReaderOptions.IgnoreTextOutsideDocumentMarkers"/> and
        /// <see cref="YamlReaderOptions.MergeDocuments"/>.
        /// </remarks>
        public bool MoveNextDocument()
        {
            // Clear the current value
            m_current = new KeyValuePair<string, string>();

            return m_lexer.MoveToNextDocument();
        }

        public int CopyTo(ICollection<KeyValuePair<string, string>> map)
        {
            int i = 0;
            while (MoveNext())
            {
                map.Add(Current);
                ++i;
            }
            return i;
        }

        public void Reset()
        {
            throw new InvalidOperationException("MicroYamlReader is read-once. It cannot be reset.");
        }

    }

    class YamlReaderException : Exception
    {
        /// <summary>
        /// Constructs a <see cref="YamlReaderException"/>
        /// </summary>
        /// <param name="lineNumber">The line on which the error occurred.</param>
        /// <param name="linePosition">The character offset on which the error occurred.</param>
        /// <param name="message">The error message.</param>
        public YamlReaderException(int lineNumber, int linePosition, string message)
            : base(message)
        {
            LineNumber = lineNumber;
            LinePosition = linePosition;
        }
        
        /// <summary>
        /// The line on which the error occurred
        /// </summary>
        public int LineNumber { get; private set; }

        /// <summary>
        /// The character offset on which the error occurred
        /// </summary>
        public int LinePosition { get; private set; }

        public override string Message => $"Yaml({LineNumber},{LinePosition}): {base.Message}";
    }
}

namespace YamlInternal
{
    using FileMeta.Yaml;

    internal enum TokenType
    {
        Null,          // Not a valid token
        BetweenDocs,   // At the beginning of the file or between documents
        NewLine,       // A new line that's not embedded in a scalar - indentation updated
        Scalar,        // A string (typically a key or a value)
        KeyPrefix,     // The '? ' string indicating a subsequent key (optional)
        ValuePrefix,   // The ': ' string indicating a subsequent value
        SequenceIndicator, // The '- ' string indicating a sequence entry
        Tag,           // A tag beginning with ! - typically indicating a type.
        BeginDoc,      // A line containing exclusively '---'
        EndDoc,        // A line containing exclusively '...'
        EOF            // End of the file 
    }

    internal class YamlLexer : IDisposable
    {
        YamlReaderOptions m_options;
        TextReader m_reader;

        // Current Token
        TokenType m_tokenType;
        string m_token;

        #region Public Interface

        public YamlLexer(TextReader reader, YamlReaderOptions options)
        {
            m_options = (options != null) ? options.Clone() : new YamlReaderOptions();
            m_reader = reader;
            m_tokenType = TokenType.BetweenDocs;
            m_token = null;
            ChInit();
        }

        public TokenType TokenType => m_tokenType;

        public string TokenValue => m_token;

        public int Indentation => m_lineIndent;

        public void MoveNext()
        {
            // If at the beginning of the file, move to the beginning of the next document (sensitive to options)
            if (m_tokenType == TokenType.BetweenDocs)
            {
                MoveToNextDocument();
                Debug.Assert(m_tokenType != TokenType.BetweenDocs && m_tokenType != TokenType.Null);
                return;
            }

            // Keep trying until we successfully read a token
            m_tokenType = TokenType.Null;
            m_token = null;
            for (;;)
            {
                SkipInlineWhitespace();
                char ch = ChPeek();

                if (ch == 0)
                {
                    m_tokenType = TokenType.EOF;
                    return;
                }

                else if (ch == '\n')
                {
                    // Skip the newline
                    ChRead();

                    if (ReadMatch("...\n"))
                    {
                        ChUnread('\n'); // Leave the newline for the outer loop
                        m_tokenType = TokenType.EndDoc;
                        return;
                    }
                    
                    if (ReadMatch("---\n"))
                    {
                        ChUnread('\n'); // Leave the newline for the outer loop
                        m_tokenType = TokenType.BeginDoc;
                        return;
                    }

                    SkipInlineWhitespace(); // Read whitespace to calculate indentation
                    m_tokenType = TokenType.NewLine;
                    return;
                }

                else if (ch == '#')
                {
                    // Comment
                    SkipBalanceOfLine();
                    continue;
                }

                else if (ch == '\'' || ch == '"')
                {
                    ReadQuoteScalar();
                }

                else if (ch == '|' || ch == '>')
                {
                    ReadBlockScalar();
                }

                else if (ReadPrefix('?')) // Key Prefix
                {
                    m_tokenType = TokenType.KeyPrefix;
                    return;
                }

                else if (ReadPrefix(':')) // value prefix
                {
                    m_tokenType = TokenType.ValuePrefix;
                    return;
                }

                else if (ReadPrefix('-')) // Sequence entry prefix
                {
                    m_tokenType = TokenType.SequenceIndicator;
                    return;
                }

                else if (ch == '!')
                {
                    ReadTag();
                }

                else
                {
                    ReadSimpleScalar();
                }

                if (m_tokenType != TokenType.Null) return;
            }
        }

        /// <summary>
        /// Moves to the beginning of the next document, using the YAML document markers.
        /// </summary>
        /// <returns>True if at the beginning of a document. False if there are no more documents.</returns>
        /// <remarks>
        /// This is sensitive to the <see cref="YamlReaderOptions"/> settings
        /// <see cref="YamlReaderOptions.IgnoreTextOutsideDocumentMarkers"/> and
        /// <see cref="YamlReaderOptions.MergeDocuments"/>.
        /// </remarks>
        public bool MoveToNextDocument()
        {
            // Read the balance of the current document (if any)
            while (m_tokenType != TokenType.Null
                && m_tokenType != TokenType.BetweenDocs
                && m_tokenType != TokenType.EOF
                && m_tokenType != TokenType.BeginDoc
                && m_tokenType != TokenType.EndDoc)
            {
                MoveNext();
            }

            // Optionally skip until the next BeginDoc token
            if (m_options.IgnoreTextOutsideDocumentMarkers
                && (m_tokenType == TokenType.BetweenDocs || m_tokenType == TokenType.EndDoc))
            {
                if (!SkipUntilBeginDoc()) return false; // End of file
            }

            // If we're at the beginning of the stream, read the next token
            else if (m_tokenType == TokenType.BetweenDocs)
            {
                m_tokenType = TokenType.Null;
                MoveNext();
            }

            // Consume an end document token, if present
            else if (m_tokenType == TokenType.EndDoc)
            {
                MoveNext();
                if (m_tokenType != TokenType.BeginDoc
                    && m_tokenType != TokenType.EOF)
                {
                    ReportError("Unexpected text found after end document marker.");
                    return false;
                }
            }

            // If on a begin document token, read the next one to kick things off
            if (m_tokenType == TokenType.BeginDoc)
            {
                MoveNext();
            }

            // Return true if there's something more in the document
            return (m_tokenType != TokenType.EOF);
        }

        public bool SkipUntilBeginDoc()
        {
            if (SkipUntilMatch("\n---\n"))
            {
                ChUnread('\n'); // Leave the newline for the outer loop
                m_tokenType = TokenType.BeginDoc;
                return true;
            }
            else
            {
                m_tokenType = TokenType.EOF;
                return false;
            }
        }

        #endregion Public

        #region Parser / Scanner

        private void SkipBalanceOfLine()
        {
            // Simply read to the end of the line (but not including EOLN
            char ch;
            for (;;)
            {
                ch = ChPeek();
                if (ch == '\n' || ch == '\0') break;
                ChRead();
            }
        }

        private void ReadQuoteScalar()
        {
            // In quote scalars, line breaks are converted to spaces.
            // Leading and trailing spaces on line breaks are stripped.
            // Double-quote scalars use backslash escaping while single-quote scalars allow the quote to be doubled
            char quoteChar = ChRead();
            Debug.Assert(quoteChar == '"' || quoteChar == '\'');
            bool doubleQuote = (quoteChar == '"');
            int spaceCount = 0;
            StringBuilder sb = new StringBuilder();
            for (;;)
            {
                char ch = ChRead();
                if (ch == '\0') break; // End of file
                if (doubleQuote && ch == '\"') break; // End quote

                if (doubleQuote && ch == '\\')
                {
                    sb.Append(ReadEscape());
                    spaceCount = 0;
                }

                else if (!doubleQuote && ch == '\'')
                {
                    // Doubled single-quotes converted to one single-quote.
                    if (ChPeek() == '\'')
                    {
                        ChRead();
                        sb.Append('\'');
                        spaceCount = 0;
                    }

                    // Otherwise, the whole scalar has been read
                    else
                    {
                        break;
                    }
                }

                else if (ch == '\n')
                {
                    // Strip leading and trailing spaces
                    if (spaceCount > 0) sb.Remove(sb.Length - spaceCount, spaceCount);
                    for (;;)
                    {
                        ch = ChPeek();
                        if (ch != ' ' && ch != '\t') break;
                        ChRead();
                    }

                    // Insert one space
                    sb.Append(' ');
                    spaceCount = 1;
                }

                // Add the character and count trailing spaces.
                else
                {
                    sb.Append((char)ch);
                    if (ch == ' ' || ch == '\t')
                        ++spaceCount;
                    else
                        spaceCount = 0;
                }
            }

            // Return the result
            m_tokenType = TokenType.Scalar;
            m_token = sb.ToString();
        }

        private void ReadBlockScalar()
        {
            char blockChar = ChRead();
            Debug.Assert(blockChar == '|' || blockChar == '>');
            bool fold = (blockChar == '>');

            // Read indent value if any
            int indent = 0;
            if (char.IsDigit(ChPeek()))
            {
                indent = ChRead() - '0';
            }

            // Read chomp type if any
            char chomp = '\0';
            char ch = ChPeek();
            if (ch == '-' || ch == '+')
            {
                chomp = ChRead();
            }

            // Skip to the end of the line. Only whitespace and comment should appear.
            SkipInlineWhitespace();
            ch = ChPeek();
            if (ch != '#' && ch != '\n')
            {
                ReportError("Expected comment or newline.");
            }
            SkipBalanceOfLine();
            ChRead();   // Read the newline

            // If not specified, determine the indent level by the indentation of the first line
            if (indent == 0)
            {
                indent = SkipInlineWhitespace();
                if (indent == 0)
                {
                    // Empty value
                    ChUnread('\n'); // Restore the newline to be read by the outer loop
                    m_tokenType = TokenType.Scalar;
                    m_token = string.Empty;
                    return;
                }
            }

            // Body of value is composed of all lines indented at least as much as the first line.
            // Indent characters are stripped. All other characters are preserved including the concluding \n
            // Embedded comments are not permitted.
            int spaceCount = 0;
            bool justFolded = false;
            StringBuilder sb = new StringBuilder();
            for (;;)
            {
                ch = ChRead();
                if (ch == '\0') break;
                if (ch == '\n')
                {
                    // Strip trailing spaces
                    if (spaceCount > 0) sb.Remove(sb.Length - spaceCount, spaceCount);
                    spaceCount = 0;

                    // Read up to indent spaces in the following line
                    int nextIndent;
                    for (nextIndent = 0; nextIndent < indent; ++nextIndent)
                    {
                        ch = ChPeek();
                        if (ch != ' ' && ch != '\t') break;
                        ChRead();
                    }

                    // Scalar ends with a non-empty line that is indented less than "indent" value.
                    if (ch != '\n' && nextIndent < indent)
                    {
                        sb.Append('\n');
                        break; // End of scalar
                    }

                    // Handle line folding (only if didn't just fold a line)
                    if (fold && !justFolded)
                    {
                        sb.Append(' ');
                        ++spaceCount;
                        Debug.Assert(spaceCount == 1);
                        justFolded = true;
                    }
                    else
                    {
                        sb.Append('\n');
                    }
                    continue;
                }
                justFolded = false;

                // Add the character and count trailing white space
                sb.Append(ch);
                if (ch == ' ' || ch == '\t')
                    ++spaceCount;
                else
                    spaceCount = 0;
            }

            // Handle "chomp" options.
            //  chomp == '-': Strip all trailing newlines.
            //  chomp == '\0': Default, strip all but one trailing newline
            //  chomp == '+': Keep all trailing newlines
            if (chomp == '-' || chomp == '\0')
            {
                // Find the end of the text (before the first trailing newline
                int end;
                for (end = sb.Length; end>0; --end)
                {
                    if (sb[end - 1] != '\n') break;
                }

                if (chomp == '\0') ++end;

                if (end < sb.Length) sb.Remove(end, sb.Length - end);
            }

            ChUnread('\n'); // Restore the newline to be read by the outer loop

            // Return the result
            m_tokenType = TokenType.Scalar;
            m_token = sb.ToString();          
        }

        private void ReadSimpleScalar()
        {
            char ch;
            var sb = new StringBuilder();
            for (;;)
            {
                ch = ChRead();
                if (ch == '\0' || ch == '\n') break; // EOF or Newline
                if ((ch == ' ' || ch == '\t') && ChPeek() == '#') break; // Comment
                if ((ch == ':' || ch == '?') && IsWhiteSpace(ChPeek())) break; // Key or value indicator
                // TODO: Make key or value indicator sensitive to whether a key or a value is expected.
                // E.g. an embedded colon is OK in a value but not in a key.
                sb.Append(ch);
            }
            if (ch != '\0') ChUnread(ch);

            // Strip trailing whitespace
            int end;
            for (end = sb.Length; end > 0; --end)
            {
                if (!char.IsWhiteSpace(sb[end - 1])) break;
            }
            sb.Remove(end, sb.Length - end);

            // Return the scalar
            m_tokenType = TokenType.Scalar;
            m_token = sb.ToString();
        }

        private void ReadTag()
        {
            char ch;
            var sb = new StringBuilder();
            for (; ; )
            {
                sb.Append(ChRead());
                ch = ChPeek();
                if (ch == '\0' || IsWhiteSpace(ch)) break;
            }

            // Return the scalar
            m_tokenType = TokenType.Tag;
            m_token = sb.ToString();
        }

        private char ReadEscape()
        {
            // The backslash has already been read, handle the rest.
            char ch = ChRead();

            switch (ch)
            {
                case '0':
                    return '\0';

                case '\n': // You can literally escape a line-end.
                    return '\n';

                case 'a':
                    return '\a';

                case 'b':
                    return '\b';

                case 't':
                    return '\t';

                case 'n':
                    return '\n';

                case 'v':
                    return '\v';

                case 'f':
                    return '\f';

                case 'r':
                    return '\r';

                case 'e':
                    return '\x1B';

                case 'N':
                    return '\x85'; // Unicode next line

                case '_':
                    return '\xA0'; // Unicode non-breaking space

                case 'L':
                    return '\u2028'; // Unicode Line separator

                case 'P':
                    return '\u2029'; // Unicode Paragraph separator

                case 'x':
                    return ReadHex(2);

                case 'u':
                    return ReadHex(4);

                default:
                    return ch;
            }
        }

        private char ReadHex(int charcount)
        {
            int result = 0;
            while (charcount > 0)
            {
                result *= 16;
                int ch = ChPeek();
                if (ch >= '0' && ch <= '9')
                {
                    result += ch - '0';
                }
                else if (ch >= 'A' && ch <= 'F')
                {
                    result += (ch - 'A') + 10;
                }
                else if (ch >= 'a' && ch <= 'f')
                {
                    result += (ch - 'a') + 10;
                }
                else
                {
                    break;
                }

                ChRead();
                --charcount;
            }

            return (char)result;
        }

        // Skip whitespace but not newlines.
        // Depending on context, the number of characters may be significant.
        private int SkipInlineWhitespace()
        {
            int count = 0;
            for (;;)
            {
                char ch = ChPeek();
                if (ch != ' ' && ch != '\t') break;
                ChRead();
                ++count;
            }
            return count;
        }

        #endregion

        #region Character Reader

        /* The character reader functions return one character at a time
           from the input. If end of file, the functions return '\0' and
           CharEof returns true. A '\0' in the input stream is converted
           to '\uFFFD'. All newline combinations of CR, LF, or CRLF are
           converted to LF ('\n'). This is per HTML5 specs which seem
           to be a reasonable option for YAML as well.

           An unlimited number of characters can be "ungotten" and will be
           returned by future InReads. This makes parsing convenient because
           you can look ahead and then back off if something doesn't match.
        */

        private Stack<char> m_readBuf = new Stack<char>();
        private int m_lineNum;
        private int m_linePos;
        private int[] m_lineLengths = new int[4]; // We never go backward more than three line lengths.
        private int m_lineIndent;
        private int[] m_lineIndents = new int[4];

        void ChInit()
        {
            m_readBuf.Clear();
            m_readBuf.Push('\n');
            m_lineNum = 0;
            m_linePos = 0;
            m_lineIndent = 0;
        }

        char ChPeek()
        {
            if (m_readBuf.Count <= 0)
            {
                char ch = ChRead();
                if (ch == '\0') return '\0';
                m_readBuf.Push(ch);
                return ch;
            }
            return m_readBuf.Peek();
        }

        char ChRead()
        {
            int ch;

            if (m_readBuf.Count > 0)
            {
                ch = m_readBuf.Pop();
            }
            else
            {
                ch = m_reader.Read();

                // Normalize newlines according to HTML5 standards (even though this is YAML)
                if (ch == '\r')
                {
                    if (m_reader.Peek() == (int)'\n')
                    {
                        // Suppress the CR in CRLF
                        ch = m_reader.Read();
                    }
                    else
                    {
                        // Replace CR with LF
                        ch = '\n';
                    }
                }
            }

            // Per HTML5 convert '\0' (Yes, this is YAML but we're following the HTML spec on this one.)
            if (ch == 0)
            {
                ch = '\xFFFD';
            }

            // Return the value
            if (ch < 0)
            {
                return '\0'; // EOF
            }
            else if (ch == (int)'\n')
            {
                m_lineLengths[m_lineNum & 0x0003] = m_linePos;  // x & 0x03 is equivalent to mod 4.
                m_lineIndents[m_lineNum & 0x0003] = m_lineIndent;
                ++m_lineNum;
                m_linePos = 0;
                m_lineIndent = 0;
            }
            else if (m_lineIndent == m_linePos && (ch == ' ' || ch == '\t'))
            {
                ++m_lineIndent;
                ++m_linePos;
            }
            else
            {
                ++m_linePos;
            }
            return (char)ch;
        }

        void ChUnread(char ch)
        {
            Debug.Assert(ch != '\0');
            m_readBuf.Push(ch);
            if (ch == '\n')
            {
                --m_lineNum;
                m_linePos = m_lineLengths[m_lineNum & 0x0003]; // x & 0x03 is equivalent to mod 4.
                m_lineIndent = m_lineIndents[m_lineNum & 0x0003];
            }
            else if (m_linePos > 0)
            {
                --m_linePos;
                if (m_lineIndent > m_linePos) m_lineIndent = m_linePos;
            }
        }

        /// <summary>
        /// Look ahead and see if the text matches. If so, consume the text.
        /// </summary>
        /// <param name="value">The text to match.</param>
        /// <returns>True if there's a match and the text was consumed. Otherwise false.</returns>
        /// <remarks>
        /// A special case is that a '\n' at the end of the value will match end-of-file.
        /// </remarks>
        bool ReadMatch(string value)
        {
            int i;
            for (i=0; i<value.Length; ++i)
            {
                if (value[i] != ChPeek()) break;
                ChRead();
            }

            // Special case: newline matches EOF
            if (i == value.Length - 1 && value[i] == '\n' && ChPeek() == '\0') ++i;

            if (i >= value.Length) return true;

            // Undo the character reads
            while (i > 0)
            {
                --i;
                ChUnread(value[i]);
            }
            return false;
        }

        // Looks for a single-character prefix followed by whitespace
        bool ReadPrefix(char value)
        {
            if (value != ChPeek()) return false;
            ChRead();
            if (IsWhiteSpace(ChPeek())) return true;
            ChUnread(value);
            return false;
        }

        /// <summary>
        /// Skip inbound text until a matching sequence is found.
        /// </summary>
        /// <param name="value">The text to match.</param>
        /// <returns>True if the value was found. Else, false and end-of-file.</returns>
        bool SkipUntilMatch(string value)
        {
            int len = value.Length;
            Debug.Assert(len > 0);
            for (;;)
            {
                char ch = ChRead();
                if (ch == '\0') return false;   // EOF
                if (ch != value[0]) continue;   // Fast track

                // See if the balance matches
                int i;
                for (i = 1; i < len; ++i)
                {
                    if (value[i] != ChPeek()) break;
                    ChRead();
                }

                // Found it.
                if (i >= len) return true;

                // if partial match, undo all but one character read
                while (i > 1)
                {
                    --i;
                    ChUnread(value[i]);
                }
            }
        }

        #endregion

        #region Character Types

        static bool IsWhiteSpace(char ch)
        {
            // No need to check for '\r' because that is handled in the character reader.
            return (ch == ' ' || ch == '\t' || ch == '\n');
        }

        #endregion Chacter Types

        #region Error Handling

        // Error handling methods are public because the owning reader/parser classes
        // also use it to report errors.

        List<YamlReaderException> m_errors = null;

        /// <summary>
        /// Report an error in the YamlLexer or a parser that depends on YamlLexer
        /// </summary>
        /// <param name="msg"></param>
        public void ReportError(string msg)
        {
            var err = new YamlReaderException(m_lineNum, m_linePos+1, msg);

            if (m_errors == null)
            {
                m_errors = new List<YamlReaderException>();
            }
            m_errors.Add(err);

            if (m_options.ThrowOnError) throw err;
        }

        /// <summary>
        /// True if any error occurred during Lexing/Parsing
        /// </summary>
        public bool ErrorOccurred => (m_errors != null);

        /// <summary>
        /// List of all errors that occurred during Lexing/Parsing
        /// </summary>
        public IReadOnlyList<YamlReaderException> Errors => m_errors;

        #endregion

        #region IDisposable

        public void Dispose()
        {
            m_tokenType = TokenType.EndDoc;
            m_token = string.Empty;
            if (m_reader != null)
            {
                if (m_options.CloseInput)
                {
                    m_reader.Dispose();
                }
                m_reader = null;
            }
        }

        #endregion
    }

}
