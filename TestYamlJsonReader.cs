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
    class TestYamlJsonReader
    {
        public static void PerformTests()
        {
            CompareYamlToJson(@"TestDocs\test1.yml", @"TestDocs\test1.json");
        }

        static void CompareYamlToJson(string yamlFilename, string jsonFilename)
        {
            // Parse the YAML into a structure
            var yamlOptions = new YamlReaderOptions();
            yamlOptions.MergeDocuments = true;
            yamlOptions.IgnoreTextOutsideDocumentMarkers = true;
            yamlOptions.CloseInput = true;
            JObject fromYaml;
            using (var reader = new YamlJsonReader(new StreamReader(yamlFilename, Encoding.UTF8, true), yamlOptions))
            {
                fromYaml = (JObject)JToken.ReadFrom(reader);
            }

            // Parse the JSON into a structure
            JObject fromJson;
            using(var reader = new StreamReader(jsonFilename, Encoding.UTF8, true))
            {
                fromJson = (JObject)JToken.ReadFrom(new JsonTextReader(reader));
            }

            CompareJsonTrees(fromYaml, fromJson);
        }

        static void CompareJsonTrees(JToken value, JToken expected)
        {
            if (value.Type != expected.Type)
            {
                ReportCompareJsonError(value, $"Type mismatch: found='{value.Type}' expected='{expected.Type}'");
            }

            if (value is JProperty)
            {
                var v = (JProperty)value;
                var e = (JProperty)value;
                if (!string.Equals(v.Name, e.Name))
                {
                    ReportCompareJsonError(value, $"Property name mismatch: found='{v.Name}' expected='{e.Name}");
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
            /*
            // Climb up the tree to get the path to the element.
            string path = string.Empty;
            for (var t = token; t != null; t = t.Parent)
            {
                var p = t as JProperty;
                if (p != null)
                {
                    path = string.Concat("/", p.Name, path);
                }
                var a = t.Parent as JArray;
                if (a != null)
                {
                    for (int i=0; i<a.Count; ++i)
                    {
                        if (Object.ReferenceEquals(a[i], t))
                        {
                            path = string.Concat("[", i, "]", path);
                        }
                    }
                }
            }
            */

            throw new UnitTestException($"Error at '{token.Path}': {msg}");
        }
    }
}
