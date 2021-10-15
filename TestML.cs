using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using FileMeta.Yaml;

namespace UnitTests
{
    /// <summary>
    /// Perform a testml test comparing YAML to the equivalent JSON
    /// </summary>
    /// <remarks>
    /// This doesn't support all TestML features. It only reads the <b>in-yaml</b> and
    /// <b>in-json</b> sections and compares the results. Within the text, the only special
    /// content supported is <b>&lt;SPC&gt;</b> substituting for space.
    /// </remarks>
    class TestML : IDisposable
    {
        Stream m_yamlStream = null;
        Stream m_jsonStream = null;
        bool m_shouldError = false;

        public string Title { get; private set; }

        public bool Trace { get; set; }

        public void Load(string testmlFilename)
        {
            using (var reader = new LineReader(new StreamReader(testmlFilename, Encoding.UTF8, true)))
            {
                for (; ; )
                {
                    var line = reader.ReadLine();
                    if (line == null) break;

                    if (line.StartsWith("=== "))
                    {
                        Title = line.Substring(4).Trim();
                        continue;
                    }

                    if (line.Trim().Equals("--- in-yaml", StringComparison.Ordinal))
                    {
                        ReadSection(reader, ref m_yamlStream);
                        continue;
                    }

                    if (line.Trim().Equals("--- in-yaml(<)", StringComparison.Ordinal))
                    {
                        ReadSectionIndented(reader, ref m_yamlStream);
                        continue;
                    }

                    if (line.Trim().Equals("--- in-json", StringComparison.Ordinal))
                    {
                        ReadSection(reader, ref m_jsonStream);
                        continue;
                    }

                    if (line.Trim().Equals("--- in-json(<)", StringComparison.Ordinal))
                    {
                        ReadSectionIndented(reader, ref m_jsonStream);
                        continue;
                    }

                    if (line.Trim().Equals("--- error", StringComparison.Ordinal))
                    {
                        m_shouldError = true;
                        continue;
                    }
                }
            }
        }

        static void ReadSection (LineReader reader, ref Stream dst)
        {
            // Prep the destination stream
            if (dst != null)
            {
                dst.Dispose();
            }
            dst = new MemoryStream();

            // Read the Section
            using (var writer = new StreamWriter(dst, s_UTF8, 512, true))
            {
                for (; ; )
                {
                    var line = reader.PeekLine();
                    if (line == null) break;
                    if (line.StartsWith("---")) break;
                    reader.ReadLine();

                    line = line.Replace("<SPC>", " ").Replace("<TAB>", "\t");
                    writer.WriteLine(line);
                }
            }
            dst.Position = 0;
        }

        static void ReadSectionIndented(LineReader reader, ref Stream dst)
        {
            // Prep the destination stream
            if (dst != null)
            {
                dst.Dispose();
            }
            dst = new MemoryStream();

            // Read the Section
            using (var writer = new StreamWriter(dst, s_UTF8, 512, true))
            {
                int indentation = 0;
                for (; ; )
                {
                    var line = reader.PeekLine();
                    if (line == null) break;

                    int lineIndent = GetIndentation(line);
                    if (indentation == 0)
                    {
                        if (lineIndent <= 0 || lineIndent == int.MaxValue)
                            throw new InvalidOperationException("First line of TestML indented section must indented and not empty.");
                        indentation = lineIndent;
                    }
                    else if (lineIndent < indentation)
                    {
                        break;
                    }

                    reader.ReadLine();
                    if (line.Length > indentation)
                    {
                        line = line.Substring(indentation);
                    }
                    line = line.Replace("<SPC>", " ").Replace("<TAB>", "\t");

                    // Write the line
                    writer.WriteLine(line);
                }
            }
            dst.Position = 0;
        }

        static int GetIndentation(string line)
        {
            for (int i=0; i<line.Length; ++i)
            {
                if (!char.IsWhiteSpace(line[i])) return i;
            }
            return int.MaxValue;
        }

        /// <summary>
        /// Performs the test. Throws an exception on any failure
        /// </summary>
        public void PerformTest()
        {
            if (m_yamlStream == null || (m_jsonStream == null && m_shouldError == false))
            {
                throw new InvalidOperationException("Must load TestML first.");
            }

            var yamlOptions = new YamlReaderOptions();
            yamlOptions.MergeDocuments = true;
            yamlOptions.IgnoreTextOutsideDocumentMarkers = false;
            yamlOptions.CloseInput = false;

            if (!m_shouldError)
            {
                if (Trace)
                {
                    Console.WriteLine("  JSON Reader:");
                    TraceJson();
                    Console.WriteLine();
                    Console.WriteLine("  YAML Reader:");
                    TraceYaml();
                    Console.WriteLine();
                }
                m_yamlStream.Position = 0;
                m_jsonStream.Position = 0;
                CompareYamlToJson.Compare(m_yamlStream, yamlOptions, m_jsonStream);
            }
            else
            {
                if (Trace)
                {
                    Console.WriteLine("  YAML Reader:");
                    TraceYaml();
                    Console.WriteLine();
                }
                m_yamlStream.Position = 0;
                AssertParseError(m_yamlStream, yamlOptions);
            }
        }

        void AssertParseError(Stream yamlStream, YamlReaderOptions yamlOptions)
        {
            bool hasError = false;
            try
            {
                JToken fromYaml;
                using (var reader = new YamlJsonReader(new StreamReader(yamlStream, Encoding.UTF8, true, 512, true), yamlOptions))
                {
                    fromYaml = JToken.ReadFrom(reader);

                    // Read any remaining content (may throw an error)
                    while (reader.Read()) ;
                }
            }
            catch (YamlReaderException)
            {
                hasError = true;
            }
            if (!hasError)
            {
                throw new ApplicationException("Expected YamlReaderException but parse succeeded.");
            }
        }

        public void DumpYaml()
        {
            Dump(m_yamlStream);
        }

        public void DumpJson()
        {
            Dump(m_jsonStream);
        }

        static void Dump(Stream stream)
        {
            stream.Position = 0;
            using (var reader = new StreamReader(stream, s_UTF8, true, 512, true))
            {
                for (; ; )
                {
                    var line = reader.ReadLine();
                    if (line == null) break;
                    Console.WriteLine(line);
                }
            }
            stream.Position = 0;
        }

        public void Dispose()
        {
            if (m_jsonStream != null)
            {
                m_jsonStream.Dispose();
                m_jsonStream = null;
            }
            if (m_yamlStream != null)
            {
                m_yamlStream.Dispose();
                m_yamlStream = null;
            }
        }

        void TraceYaml()
        {
            var yamlOptions = new YamlReaderOptions();
            yamlOptions.MergeDocuments = true;
            yamlOptions.IgnoreTextOutsideDocumentMarkers = false;
            yamlOptions.CloseInput = false;
            yamlOptions.ThrowOnError = false;

            m_yamlStream.Position = 0;
            using (var reader = new YamlJsonReader(new StreamReader(m_yamlStream, Encoding.UTF8, true, 512, true), yamlOptions))
            {
                TraceReader(reader);

                if (reader.ErrorOccurred)
                {
                    Console.WriteLine("YamlJsonReader errors:");
                    foreach(var err in reader.Errors)
                    {
                        Console.WriteLine("  " + err.Message);
                    }
                }
            }
        }

        void TraceJson()
        {
            m_jsonStream.Position = 0;
            using (var reader = new StreamReader(m_jsonStream, Encoding.UTF8, true, 512, true))
            {
                TraceReader(new JsonTextReader(reader));
            }
        }

        static void TraceReader(JsonReader reader)
        {
            while (reader.Read())
            {
                Console.WriteLine($"({reader.TokenType}, \"{Encode(reader.Value)}\")");
            }
            Console.WriteLine();
        }

        static string Encode(Object obj)
        {
            if (obj == null) return string.Empty;
            return obj.ToString().Replace("\n", "\\n").Replace("\t", "\\t");
        }


        static readonly Encoding s_UTF8 = new UTF8Encoding(false); // UTF8 with no byte-order mark.

        class LineReader : IDisposable
        {
            TextReader m_reader;
            string m_nextLine;

            public LineReader(TextReader reader)
            {
                m_reader = reader;
            }

            public void Dispose()
            {
                if (m_reader != null)
                {
                    m_reader.Dispose();
                }
                m_reader = null;
            }

            public string PeekLine()
            {
                if (m_nextLine == null)
                {
                    m_nextLine = m_reader.ReadLine();
                }
                return m_nextLine;
            }

            public string ReadLine()
            {
                if (m_nextLine != null)
                {
                    var line = m_nextLine;
                    m_nextLine = null;
                    return line;
                }
                else
                {
                    return m_reader.ReadLine();
                }
            }

        }
    }


}
