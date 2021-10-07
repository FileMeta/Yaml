using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using FileMeta.Yaml;

// TestML samples are taken from the validation suite at
// https://github.com/yaml/yaml-test-suite/tree/master/test

namespace UnitTests
{
    class TestYamlJsonReader
    {
        public static void PerformTests()
        {
            Console.WriteLine("TestYamlJsonReader");
            var testDir = Path.GetFullPath("./TestDocs");

            // When debugging, put first test here
            //DumpTmlYaml(Path.Combine(testDir, "2JQS-mod.tml"));
            //DumpTmlJson(Path.Combine(testDir, "2JQS-mod.tml"));
            //PerformTmlTest(Path.Combine(testDir, "236B.tml"));

            Console.WriteLine("Raw Tests:");
            foreach (var yamlFilename in Directory.GetFiles(testDir, "*.yml"))
            {
                PerformRawTest(yamlFilename);
            }

            Console.WriteLine("TestML Tests:");
            foreach(var tmlFilename in Directory.GetFiles(testDir, "*.tml"))
            {
                PerformTmlTest(tmlFilename);
            }

            Console.WriteLine("TestYamlJsonReader Success.");
        }

        static void PerformRawTest(string yamlFilename)
        {
            var jsonFilename = Path.Combine(
                Path.GetDirectoryName(yamlFilename),
                Path.GetFileNameWithoutExtension(yamlFilename) + ".json");
            if (!File.Exists(jsonFilename))
            {
                throw new ApplicationException($"TestYamlJsonReader: File '{Path.GetFileName(jsonFilename)}' not found to match with '{Path.GetFileName(yamlFilename)}'.");
            }
            Console.WriteLine($"  {Path.GetFileName(yamlFilename)} = {Path.GetFileName(jsonFilename)}");
            CompareYamlToJson.Compare(yamlFilename, jsonFilename);
        }

        static void PerformTmlTest(string filename)
        {
            using (var tml = new TestML())
            {
                tml.Load(filename);
                Console.WriteLine($"  {Path.GetFileName(filename)}: {tml.Title}");
                tml.PerformTest();
            }
        }

        static void DumpTmlYaml(string filename)
        {
            using (var tml = new TestML())
            {
                tml.Load(filename);
                Console.WriteLine($"  {Path.GetFileName(filename)}: {tml.Title}");
                tml.DumpYaml();
            }
        }

        static void DumpTmlJson(string filename)
        {
            using (var tml = new TestML())
            {
                tml.Load(filename);
                Console.WriteLine($"  {Path.GetFileName(filename)}: {tml.Title}");
                tml.DumpJson();
            }
        }

    }
}
