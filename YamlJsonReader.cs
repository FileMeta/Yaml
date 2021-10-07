using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
using System.Diagnostics;

namespace FileMeta.Yaml
{
    using YamlInternal;

    class YamlJsonReader : JsonReader
    {

        // Source code to the parent class:
        // https://github.com/JamesNK/Newtonsoft.Json/blob/master/Src/Newtonsoft.Json/JsonReader.cs
        // Source code to JsonTextReader on which this is modeled
        // https://github.com/JamesNK/Newtonsoft.Json/blob/master/Src/Newtonsoft.Json/JsonTextReader.cs

        YamlLexer m_lexer;
        int m_currentIndent;

        public YamlJsonReader(TextReader reader, YamlReaderOptions options = null)
        {
            m_lexer = new YamlLexer(reader, options);
            m_currentIndent = -1; // Indentation of the unnamed owner object.

            // TODO: Init stack with an object so we don't have to check for empty stackTop all the time.
        }

        // Problem: when a scalar is posted on a new line, and indented more than
        // the preceding label, I cannot tell whether it's a value or a label until I have read the
        // subsequent token. This can all be handled by a state machine, of course, but the state
        // machine given to me by NewtonSoft doesn't fit.
        // Rough plan: Create my own state machine, put scalars on the stack and read he next
        // token before deciding how to process the scalar.

        public override bool Read()
        {
            // Upon entry to this function:
            // * This class (YamlJsonReader) contains the current state and value of the reader
            // * m_lexer (YamlLexer) contains the next token to be processed.

            for (; ; )
            {
                // If something in the queue, return it
                if (m_tokenQueue.Count > 0)
                {
                    var entry = m_tokenQueue.Dequeue();
                    //Console.WriteLine($"({entry.Token}, '{entry.Value}')");
                    SetToken(entry.Token, entry.Value, entry.UpdateIndex);
                    return entry.Token != JsonToken.Undefined;
                }

                // TODO: Update the state machine. The basic framework was inherited from
                // NewtonSoft Json.Net but YAML is different. Most likely, ParseValue and
                // ParseObject can be merged.
                // Regardless, the task is to interpret the next token in context of the
                // last token emitted and the type of collection at the top of the stack.

                // Before anything breaks, it must call m_lexer.MoveNext(), call EnqueueToken, or do both.
                // Otherwise this becomes an infinite loop.
                switch (m_lexer.TokenType)
                {
                    case YamlInternal.TokenType.Null:
                    case YamlInternal.TokenType.BetweenDocs:
                    case YamlInternal.TokenType.BeginDoc:
                        m_lexer.MoveNext();
                        break;

                    case YamlInternal.TokenType.NewLine:
                        {
                            var indentation = m_lexer.Indentation;
                            m_lexer.MoveNext(); // Do this first so that we can look ahead

                            // Ignore blank lines
                            if (m_lexer.TokenType == YamlInternal.TokenType.NewLine
                                || m_lexer.TokenType == YamlInternal.TokenType.EndDoc
                                || m_lexer.TokenType == YamlInternal.TokenType.EOF) break;

                            if (indentation < m_currentIndent)
                            {
                                EndElements(m_lexer.Indentation);
                            }
                            if (indentation > m_currentIndent)
                            {
                                if (ExpectingKey)
                                {
                                    m_lexer.ReportError("Indentation mismatch.");
                                }
                            }
                            if (m_stackTop != null && m_stackTop.Type == StackEntryType.Sequence
                                && indentation <= m_currentIndent
                                && m_lexer.TokenType != YamlInternal.TokenType.SequenceIndicator)
                            {
                                m_lexer.ReportError("Expected '-' sequence indicator.");
                            }
                        }
                        break;

                    case YamlInternal.TokenType.ValuePrefix:
                        // If expecting a key, then that key is empty.
                        if (ExpectingKey)
                        {
                            // Write out an empty key
                            EnqueueKey(m_lexer.Indentation, string.Empty);
                        }
                        m_lexer.MoveNext();
                        break;

                    case YamlInternal.TokenType.KeyPrefix:
                        // If expecting a value, then that value is empty.
                        if (!ExpectingKey)
                        {
                            EnqueueToken(JsonToken.String, string.Empty);
                        }
                        m_lexer.MoveNext();
                        break;

                    case YamlInternal.TokenType.Scalar:
                        if (ExpectingKey)
                        {
                            EnqueueToken(JsonToken.PropertyName, m_lexer.TokenValue);
                            m_lexer.MoveNext();
                            if (m_lexer.TokenType != YamlInternal.TokenType.ValuePrefix)
                            {
                                m_lexer.ReportError("Expected ':' (colon).");
                            }
                        }
                        else
                        {
                            // Save and read ahead so we know what to do
                            var scalar = m_lexer.TokenValue;
                            var indent = m_lexer.Indentation;
                            m_lexer.MoveNext();

                            if (m_lexer.TokenType == YamlInternal.TokenType.ValuePrefix)
                            {
                                EnqueueKey(indent, scalar);
                            }
                            else
                            {
                                EnqueueToken(JsonToken.String, scalar);
                            }
                        }
                        break;

                    case YamlInternal.TokenType.SequenceIndicator:
                        // Start a new sequence if appropriate
                        if (m_lexer.Indentation > m_currentIndent)
                        {
                            StartElement(m_lexer.Indentation, JsonToken.StartArray);
                        }
                        if (m_stackTop.Type != StackEntryType.Sequence || m_stackTop.PrevIndent >= m_lexer.Indentation)
                        {
                            m_lexer.ReportError("Unexpected sequence indicator '-'.");
                        }
                        m_lexer.MoveNext();
                        break;

                    case YamlInternal.TokenType.EndDoc:
                    case YamlInternal.TokenType.EOF:
                        EndElements(-1);
                        EnqueueToken(JsonToken.Undefined); // End of file
                        break;

                    // This parser simply ignores tags.
                    case YamlInternal.TokenType.Tag:
                        m_lexer.MoveNext();
                        break;
                }
            }
        }

        bool ExpectingKey
        {
            get
            {
                // No keys in a sequence (without an encapsulated object)
                if (m_stackTop != null && m_stackTop.Type == StackEntryType.Sequence) return false;

                // Otherwise, base it on the preceding token
                switch (TokenType)
                {
                    case JsonToken.StartObject:
                    case JsonToken.EndObject:
                    case JsonToken.EndArray:
                    case JsonToken.String:
                        return true;
                }

                return false;
            }
        }

        void EnqueueKey(int indent, string value)
        {
            if (indent > m_currentIndent)
            {
                // New Object
                StartElement(indent, JsonToken.StartObject);
                EnqueueToken(JsonToken.PropertyName, value);
            }
            else if (indent == m_currentIndent)
            {
                // Empty Value (for the previous element)
                EnqueueToken(JsonToken.String, string.Empty);
                EnqueueToken(JsonToken.PropertyName, value);
            }
            else
            {
                Debugger.Break();
                throw new ApplicationException("Not sure how we get here.");
            }
        }

        void StartElement(int indent, JsonToken token)
        {
            Debug.Assert(indent > m_currentIndent);
            Debug.Assert(token == JsonToken.StartObject || token == JsonToken.StartArray);
            Push((token == JsonToken.StartObject) ? StackEntryType.Mapping : StackEntryType.Sequence,
                m_currentIndent);
            EnqueueToken(token);
            m_currentIndent = indent;
        }

        void EndElements(int indent)
        {
            while (m_stackTop != null && m_stackTop.PrevIndent >= indent)
            {
                EnqueueToken((m_stackTop.Type == StackEntryType.Mapping) ? JsonToken.EndObject : JsonToken.EndArray);
                Pop();
            }
            if (m_currentIndent != indent)
            {
                m_lexer.ReportError("Indentation mismatch.");
                m_currentIndent = indent;
            }
        }

        #region Queue

        void EnqueueToken(JsonToken token, string value = null, bool updateIndex = true)
        {
            m_tokenQueue.Enqueue(new QueueEntry
            {
                Token = token,
                Value = value,
                UpdateIndex = updateIndex
            });
        }

        Queue<QueueEntry> m_tokenQueue = new Queue<QueueEntry>();

        class QueueEntry
        {
            public JsonToken Token;
            public string Value;
            public bool UpdateIndex;
        }

        #endregion Queue

        #region Stack

        Stack<StackEntry> m_stack = new Stack<StackEntry>();
        StackEntry m_stackTop;

        void Push(StackEntryType type, int indentation)
        {
            m_stack.Push(m_stackTop);
            m_stackTop = new StackEntry
            {
                Type = type,
                PrevIndent = indentation
            };
        }

        void Pop()
        {
            m_currentIndent = m_stackTop.PrevIndent;
            m_stackTop = m_stack.Pop();
        }

        enum StackEntryType
        {
            Label = 1,
            Mapping = 2,
            Sequence = 3
        }

        class StackEntry
        {
            public StackEntryType Type; // Type of containing entity
            public int PrevIndent;      // Indentation level of the containing entity (not of its members)
        }

        #endregion Stack
    }
}
