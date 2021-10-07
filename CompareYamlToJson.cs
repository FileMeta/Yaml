// Uncomment to enable debugging tools
//#define TRACE_YAML_TO_JSON

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using FileMeta.Yaml;

namespace UnitTests
{
    static class CompareYamlToJson
    {
        public static void Compare(string yamlFilename, string jsonFilename)
        {
            var yamlOptions = new YamlReaderOptions();
            yamlOptions.MergeDocuments = true;
            yamlOptions.IgnoreTextOutsideDocumentMarkers = true;
            yamlOptions.CloseInput = true;

            using (Stream yaml = new FileStream(yamlFilename, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                using (Stream json = new FileStream(jsonFilename, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    Compare(yaml, yamlOptions, json);
                }
            }
        }

        public static void Compare(Stream yamlStream, YamlReaderOptions yamlOptions, Stream jsonStream)
        {
            // Parse the YAML into a structure
            JToken fromYaml;
            using (var reader = new YamlJsonReader(new StreamReader(yamlStream, Encoding.UTF8, true, 512, true), yamlOptions))
            {
                fromYaml = JToken.ReadFrom(reader);
            }
#if TRACE_YAML_TO_JSON
            Dump(fromYaml);
#endif

            // Parse the JSON into a structure
            JToken fromJson;
            using (var reader = new StreamReader(jsonStream, Encoding.UTF8, true, 512, true))
            {
                fromJson = JToken.ReadFrom(new JsonTextReader(reader));
            }

            CompareJsonTrees(fromYaml, fromJson);
        }

        static void CompareJsonTrees(JToken value, JToken expected)
        {
            // Perform type conversion
            // (FileMeta.Yaml does not attempt to detect type as JSON does)
            if (value.Type == JTokenType.String)
            {
                switch (expected.Type)
                {
                    case JTokenType.Integer:
                        value = new JValue(int.Parse((string)value));
                        break;

                    case JTokenType.Float:
                        value = new JValue(double.Parse((string)value));
                        break;

                    case JTokenType.Boolean:
                        value = new JValue(string.Compare((string)value, "true", StringComparison.OrdinalIgnoreCase));
                        break;
                }
            }

            if (value.Type != expected.Type)
            {
                ReportCompareJsonError(value, $"Type mismatch: found='{value.Type}' expected='{expected.Type}'");
            }

            if (value is JProperty)
            {
                var v = (JProperty)value;
                var e = (JProperty)expected;
                if (!string.Equals(v.Name, e.Name))
                {
                    ReportCompareJsonError(value, $"Property name mismatch: found='{v.Name}' expected='{e.Name}'");
                }
            }

            if (value is JContainer)
            {
                var v = (JContainer)value;
                var e = (JContainer)expected;
                if (v.Count != e.Count)
                {
                    ReportCompareJsonError(value, $"{value.Type} count mismatch: found={v.Count} expected={e.Count}");
                }

                var vi = v.First;
                var ei = e.First;
                while (vi != null)
                {
                    CompareJsonTrees(vi, ei);
                    vi = vi.Next;
                    ei = ei.Next;
                }
            }

            else // Must be JValue
            {
                var v = (JValue)value;
                var e = (JValue)expected;

                if (!v.Value.Equals(e.Value))
                {
                    ReportCompareJsonError(value, $"Value mismatch: found='{v.Value}' expected='{e.Value}'");
                }
            }
        }

        static void ReportCompareJsonError(JToken token, string msg)
        {
            throw new UnitTestException($"Error at '{token.Path}': {msg}");
        }

        static void Dump(JToken jtoken)
        {
            using (var writer = new JsonTextWriter(Console.Out))
            {
                writer.Formatting = Formatting.Indented;
                jtoken.WriteTo(writer);
            }
            Console.Out.WriteLine();
        }

    }
}

