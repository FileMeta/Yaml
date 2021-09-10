using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;

namespace FileMeta.Yaml
{
    using YamlInternal;

    class YamlJsonReader : JsonReader
    {

        // Source code to the parent class:
        // https://github.com/JamesNK/Newtonsoft.Json/blob/master/Src/Newtonsoft.Json/JsonReader.cs
        // Source code to JsonTextReader:
        // https://github.com/JamesNK/Newtonsoft.Json/blob/master/Src/Newtonsoft.Json/JsonTextReader.cs

        YamlLexer m_lexer;

        public YamlJsonReader(TextReader reader, YamlReaderOptions options = null)
        {
            m_lexer = new YamlLexer(reader, options);
        }

        public override bool Read()
        {
            // Upon entry to this function:
            // * This class (YamlJsonReader) contains the current state and value of the reader
            // * m_lexer (YamlLexer) contains the next token to be processed.

            while (m_lexer.TokenType != YamlInternal.TokenType.EOF)
            {
                // Before anything returns, it should call SetToken()
                switch (CurrentState)
                {
                    case State.Start:
                    case State.Property:
                    case State.Array:
                    case State.ArrayStart:
                    case State.Constructor:
                    case State.ConstructorStart:
                        return ParseValue();
                    case State.Object:
                    case State.ObjectStart:
                        return ParseObject();
                    case State.PostValue:
                        // returns true if it hits
                        // end of object or array
                        if (ParsePostValue())
                        {
                            return true;
                        }
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

            return false;
        }

        bool ParseValue()
        {
            for (; ; )
            {
                switch (m_lexer.TokenType)
                {
                    case YamlInternal.TokenType.Null:
                    case YamlInternal.TokenType.BetweenDocs:
                        m_lexer.MoveNext();
                        break;

                    case YamlInternal.TokenType.Scalar:
                    case YamlInternal.TokenType.KeyPrefix:
                    case YamlInternal.TokenType.ValuePrefix:
                    case YamlInternal.TokenType.SequenceIndicator:
                    case YamlInternal.TokenType.BeginDoc:
                    case YamlInternal.TokenType.EndDoc:
                    case YamlInternal.TokenType.EOF:
                        return false;
                }
            }
        }

        bool ParseObject()
        {
            throw new NotImplementedException();
        }

        bool ParsePostValue()
        {
            throw new NotImplementedException();
        }

        #region Nested Classes

        // Collection Stack
        Stack<Collection> m_mollectionStack = new Stack<Collection>();

        enum CollectionType
        {
            Mapping = 1,
            Sequence = 2
        }

        // Used in the collection stack
        class Collection
        {
            public CollectionType Type;
            public int Indentation;
        }

        #endregion Nested Classes
    }
}
