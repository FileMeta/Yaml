using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

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

        public string Title { get; private set; }

        public void Load(string testmlFilename)
        {
            using (var reader = new StreamReader(testmlFilename, Encoding.UTF8, true))
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
                    }

                    if (line.Trim().Equals("--- in-json", StringComparison.Ordinal))
                    {
                        ReadSection(reader, ref m_jsonStream);
                    }
                }
            }
        }

        static void ReadSection (TextReader reader, ref Stream dst)
        {
            // Prep the stream stream
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
                    var line = reader.ReadLine();
                    if (line == null) break;

                    line = line.Replace("<SPC>", " ");
                    writer.WriteLine(line);
                    if (line.Length == 0) break;
                }
            }
            dst.Position = 0;
        }

        /// <summary>
        /// Performs the test. Throws an exception on any failure
        /// </summary>
        public void PerformTest()
        {
            if (m_yamlStream == null || m_jsonStream == null)
            {
                throw new InvalidOperationException("Must load TestML first.");
            }

            var yamlOptions = new FileMeta.Yaml.YamlReaderOptions();
            yamlOptions.MergeDocuments = true;
            yamlOptions.IgnoreTextOutsideDocumentMarkers = false;
            yamlOptions.CloseInput = false;

            m_yamlStream.Position = 0;
            m_jsonStream.Position = 0;
            CompareYamlToJson.Compare(m_yamlStream, yamlOptions, m_jsonStream); ;
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

        static readonly Encoding s_UTF8 = new UTF8Encoding(false); // UTF8 with no byte-order mark.
    }


}
