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
                    if (entry.Token == JsonToken.String) // Just wrote out a value.
                    {
                        SetStateBasedOnCurrent();
                    }
                    // JsonReader.SetToken changes JsonReader.CurrentState
                    return entry.Token != JsonToken.Null;
                }

                // Before anything returns, it must call m_lexer.Read(), call EnqueueToken, or do both.
                // Otherwise this becomes an infinite loop.
                switch (CurrentState)
                {
                    case State.Start:
                    case State.Property:
                    case State.Array:
                    case State.ArrayStart:
                    case State.Constructor:
                    case State.ConstructorStart:
                        ParseValue();
                        break;

                    case State.Object:
                    case State.ObjectStart:
                    case State.PostValue:
                        ParseObject();
                        break;

                    case State.Finished:
                        SetToken(JsonToken.None);
                        return false;

                    default:
                        m_lexer.ReportError($"Unexpected state: {CurrentState}.");
                        m_lexer.MoveNext(); // Make sure we don't get stuck in an infinite error loop.
                        break;
                }
            }
        }

        void ParseValue()
        {
            for (; ; )
            {
                switch (m_lexer.TokenType)
                {
                    case YamlInternal.TokenType.Null:
                    case YamlInternal.TokenType.BetweenDocs:
                    case YamlInternal.TokenType.ValuePrefix:
                    case YamlInternal.TokenType.BeginDoc:
                        m_lexer.MoveNext();
                        break;

                    case YamlInternal.TokenType.KeyPrefix:
                        m_lexer.ReportError("Expected value.");
                        m_lexer.MoveNext();
                        break;

                    case YamlInternal.TokenType.Scalar:
                        {
                            // We have to look ahead to tell how to handle this scalar
                            var scalar = m_lexer.TokenValue;
                            var scalarIndent = m_lexer.Indentation;
                            m_lexer.MoveNext();

                            // If next token type is a value prefix, this scalar is a label
                            if (m_lexer.TokenType == YamlInternal.TokenType.ValuePrefix)
                            {
                                // If indentation of this label is greater than current, start a new object/mapping
                                if (scalarIndent > m_currentIndent)
                                {
                                    StartElement(scalarIndent, JsonToken.StartObject);
                                    EnqueueToken(JsonToken.PropertyName, scalar);
                                }

                                // If indentation of this label is equal to current, the value is empty string
                                else if (scalarIndent == m_currentIndent)
                                {
                                    EnqueueToken(JsonToken.String, string.Empty);
                                    EnqueueToken(JsonToken.PropertyName, scalar);
                                }

                                // Indentation is less than current, close the object stack to this level
                                else
                                {
                                    EndElements(scalarIndent);
                                    EnqueueToken(JsonToken.PropertyName, scalar);
                                }
                            }

                            // Not a value prefix, this scalar is a value
                            else
                            {
                                EnqueueToken(JsonToken.String, scalar);
                            }
                        }
                        return;

                    case YamlInternal.TokenType.SequenceIndicator:
                        if (m_stackTop.Type == StackEntryType.Sequence && m_stackTop.PrevIndent <= m_lexer.Indentation)
                        {
                            // This is a continuation of a sequence
                            m_lexer.MoveNext();
                            break;
                        }
                        if (m_lexer.Indentation >= m_stackTop.PrevIndent)
                        {
                            // This is the beginning of a new sequence
                            StartElement(m_lexer.Indentation, JsonToken.StartArray);
                            m_lexer.MoveNext();
                            return;
                        }
                        m_lexer.ReportError("Unexpected sequence indicator '-'.");
                        m_lexer.MoveNext();
                        break;

                    case YamlInternal.TokenType.EndDoc:
                    case YamlInternal.TokenType.EOF:
                        // In YAML, end of document is a legitimate empty value.
                        EnqueueToken(JsonToken.String, string.Empty);
                        return;
                }
            }
        }

        void ParseObject()
        {
            for (; ; )
            {
                switch (m_lexer.TokenType)
                {
                    case YamlInternal.TokenType.Null:
                    case YamlInternal.TokenType.BetweenDocs:
                    case YamlInternal.TokenType.ValuePrefix:
                    case YamlInternal.TokenType.BeginDoc:
                        m_lexer.MoveNext();
                        break;

                    case YamlInternal.TokenType.KeyPrefix:
                        m_lexer.MoveNext();
                        break;

                    case YamlInternal.TokenType.Scalar:
                        EndElements(m_lexer.Indentation);
                        EnqueueToken(JsonToken.PropertyName, m_lexer.TokenValue);
                        m_lexer.MoveNext();
                        if (m_lexer.TokenType == YamlInternal.TokenType.ValuePrefix)
                        {
                            m_lexer.MoveNext();
                        }
                        else
                        {
                            m_lexer.ReportError("Expected colon ':'.");
                        }
                        return;

                    case YamlInternal.TokenType.SequenceIndicator:
                        if (m_lexer.Indentation <= m_stackTop.PrevIndent)
                        {
                            EndElements(m_lexer.Indentation);
                            return;
                        }
                        m_lexer.ReportError("Unexpected sequence indicator '-'.");
                        m_lexer.MoveNext();
                        break;

                    case YamlInternal.TokenType.EndDoc:
                    case YamlInternal.TokenType.EOF:
                        EndElements(int.MinValue);
                        EnqueueToken(JsonToken.Null);
                        return;
                }
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
