/*
---
# Metadata in MicroYaml format. See http://filemeta.org/CodeBit.html
name: MicroYamlWriter.cs
description: MicroYaml Writer in C#
url: https://github.com/FileMeta/MicroYaml/raw/master/MicroYamlWriter.cs
version: 1.1
keywords: CodeBit
dateModified: 2018-04-09
copyrightHolder: Brandt Redd
copyrightYear: 2018
license: https://opensource.org/licenses/BSD-3-Clause
...
*/

/*
=== BSD 3 Clause License ===
https://opensource.org/licenses/BSD-3-Clause

Copyright 2018 Brandt Redd

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
using System.Text;
using System.IO;

namespace UnitTests
{

    /// <summary>
    /// <para>Encodes MicroYaml documents.
    /// </para>
    /// <para>MicroYaml is subset of the full YAML syntax. It consists of set of
    /// key-value pairs or one "mapping" im YAML parlance.
    /// </para>
    /// <para>Presently there are no plans to add lists, nested mappings, or other
    /// advanced YAML features. There are other, more capable, YAML parsers and encoders
    /// available for those purposes. Besides, JSON may be a better choice when
    /// that complexity is needed.
    /// </para>
    /// <para>This is a partial class. If this CodeBit is combined with the MicroYamlReader
    /// CodeBit in the same application then the MicroYaml class will include static methods
    /// for both reading and writing MicroYaml documents.
    /// </para>
    /// <para>For details of the YAML syntax including samples, see "http://yaml.org"
    /// </para>
    /// <para>For experimenting with yaml, you may try "http://yaml-online-parser.appspot.com/".</para>
    /// </summary>
    public static partial class MicroYaml
    {
        /// <summary>
        /// Write a key-value collection to a file.
        /// </summary>
        /// <param name="map">The collection to be written to the file in YAML format.</param>
        /// <param name="filename">Filename of a MicroYaml document.</param>
        /// <param name="includeDocumentMarkers">If true, includes the document start marker "---" and document end marker. "...".</param>
        /// <param name="append">If true, appends the output to the end of the file. Otherwise overwrites.</param>
        /// <returns>The number of key-value pairs written to the file.</returns>
        static public int Save<TKey, TValue>(IEnumerable<KeyValuePair<TKey, TValue> > map, string filename, bool includeDocumentMarkers = true, bool append = false)
        {
            using (var yamlWriter = new MicroYamlWriter(filename, includeDocumentMarkers, append))
            {
                return yamlWriter.Write(map);
            }
        }

        /// <summary>
        /// Write a key-value collection to a stream.
        /// </summary>
        /// <param name="map">The collection to be written to the stream in YAML format.</param>
        /// <param name="stream">Stream to which the YAML should be written.</param>
        /// <param name="includeDocumentMarkers">If true, includes the document start marker "---" and document end marker. "...".</param>
        /// <returns>The number of key-value pairs written to the stream.</returns>
        static public int Save<TKey, TValue>(IEnumerable<KeyValuePair<TKey, TValue>> map, Stream stream, bool includeDocumentMarkers = true)
        {
            using (var yamlWriter = new MicroYamlWriter(stream, includeDocumentMarkers, true))
            {
                return yamlWriter.Write(map);
            }
        }

        /// <summary>
        /// Write a key-value collection to a TextWriter.
        /// </summary>
        /// <param name="map">The collection to be written to the TextWriter in YAML format.</param>
        /// <param name="writer">TextWriter to which the YAML should be written.</param>
        /// <param name="includeDocumentMarkers">If true, includes the document start marker "---" and document end marker. "...".</param>
        /// <returns>The number of key-value pairs written to the writer.</returns>
        static public int Save<TKey, TValue>(IEnumerable<KeyValuePair<TKey, TValue>> map, TextWriter writer, bool includeDocumentMarkers = true)
        {
            using (var yamlWriter = new MicroYamlWriter(writer, includeDocumentMarkers, true))
            {
                return yamlWriter.Write(map);
            }
        }

        /// <summary>
        /// Write a key-value collection to a string in YAML format.
        /// </summary>
        /// <param name="map">The collection to be written to the string in YAML format.</param>
        /// <param name="includeDocumentMarkers">If true, includes the document start marker "---" and document end marker. "...".</param>
        /// <returns>A string containing the YAML content.</returns>
        static public string SaveToString<TKey, TValue>(IEnumerable<KeyValuePair<TKey, TValue>> map, bool includeDocumentMarkers = true)
        {
            using (var sw = new StringWriter())
            {
                using (var yamlWriter = new MicroYamlWriter(sw, includeDocumentMarkers))
                {
                    yamlWriter.Write(map);
                }
                return sw.ToString();
            }
        }

    } // Class MicroYaml

    class MicroYamlWriter : IDisposable
    {
        static UTF8Encoding s_utf8EncodingNoBom = new UTF8Encoding(false, false);
        static int s_bufferSize = 1024;

        TextWriter m_writer;
        bool m_includeDocumentMarkers;
        bool m_leaveOpen;

        public MicroYamlWriter(TextWriter writer, bool includeDocumentMarkers = true, bool leaveOpen = false)
        {
            Init(writer, includeDocumentMarkers, leaveOpen);
        }

        public MicroYamlWriter(Stream stream, bool includeDocumentMarkers = true, bool leaveOpen = false)
        {
            Init(new StreamWriter(stream, s_utf8EncodingNoBom, s_bufferSize, leaveOpen), includeDocumentMarkers, false);
        }

        public MicroYamlWriter(string filename, bool includeDocumentMarkers = true, bool append = false)
        {
            Init(new StreamWriter(filename, append, s_utf8EncodingNoBom), includeDocumentMarkers, false);
        }

        private void Init(TextWriter writer, bool includeDocumentMarkers, bool leaveOpen)
        {
            m_writer = writer;
            m_includeDocumentMarkers = includeDocumentMarkers;
            m_leaveOpen = leaveOpen;

            if (m_includeDocumentMarkers)
            {
                m_writer.WriteLine("---");   // Write the "start of document" marker
            }
        }

        /* The state of the writer between calls to public methods is at the
         * beginning of a new line without any indentation.
         */

        public int Write<TKey, TValue>(IEnumerable<KeyValuePair<TKey, TValue>> map)
        {
            int count = 0;
            foreach(var pair in map)
            {
                Write(pair);
                ++count;
            }
            return count;
        }

        public void Write<TKey, TValue>(KeyValuePair<TKey, TValue> pair)
        {
            string key = Convert.ToString(pair.Key, System.Globalization.CultureInfo.InvariantCulture);
            string value = Convert.ToString(pair.Value, System.Globalization.CultureInfo.InvariantCulture);
            Write(key, value);
        }

        public int Write(IEnumerable<KeyValuePair<string, string>> map)
        {
            int count = 0;
            foreach(var pair in map)
            {
                Write(pair.Key, pair.Value);
                ++count;
            }
            return count;
        }

        public void Write(KeyValuePair<string, string> pair)
        {
            Write(pair.Key, pair.Value);
        }

        public void Write(string key, string value)
        {
            m_writer.Write(Encode(key));
            m_writer.Write(": ");
            m_writer.Write(Encode(value));
            m_writer.WriteLine();
            return;
        }

        static char[] s_invalidPlainChars = new char[] { '\r', '\n', '\t', ':', '#', ',', '[', ']', '{', '}', '"', '\'' };

        private string Encode(string value)
        {
            int len = value.Length;
            if (len > 0 &&
                (char.IsWhiteSpace(value[0])
                || char.IsWhiteSpace(value[len-1])
                || value.IndexOfAny(s_invalidPlainChars) >= 0))
            {
                // Use double-quote Yaml encoding
                return EncodeDoubleQuote(value);
            }
            else
            {
                // Use plain encoding
                return value;
            }
        }

        private string EncodeDoubleQuote(string value)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append('"');
            foreach (char ch in value)
            {
                switch (ch)
                {
                    case '\0':
                        sb.Append(@"\0");
                        break;

                    case '\t':
                        sb.Append(@"\t");
                        break;

                    case '\n':
                        sb.Append(@"\n");
                        break;

                    case '\r':
                        sb.Append(@"\r");
                        break;

                    case '"':
                        sb.Append("\\\"");
                        break;

                    case '\\':
                        sb.Append("\\\\");
                        break;

                    default:
                        if (ch < '\x20')
                        {
                            sb.AppendFormat(System.Globalization.CultureInfo.InvariantCulture, @"\x{0:x2}", ch);
                        }
                        else
                        {
                            sb.Append(ch);
                        }
                        break;
                }
            }
            sb.Append('"');

            return sb.ToString();
        }

        #region IDisposable Support

        protected virtual void Dispose(bool disposing)
        {
            if (m_writer != null)
            {
                if (m_includeDocumentMarkers)
                {
                    m_writer.WriteLine("..."); // Write the "end of document" marker.
                }

                if (m_leaveOpen)
                {
                    m_writer.Flush();
                }
                else
                {
                    m_writer.Dispose();
                }
                m_writer = null;

#if DEBUG
                if (!disposing)
                {
                    System.Diagnostics.Debug.Fail("Failed to dispose MicroYamlWriter.");
                }
#endif
            }
        }

#if DEBUG
        ~MicroYamlWriter()
        {
            Dispose(false);
        }
#endif

        public void Dispose()
        {
            Dispose(true);
#if DEBUG
            GC.SuppressFinalize(this);
#endif
        }
        #endregion


    }
}
