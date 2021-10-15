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
        const string c_testDir = "./TestDocs";

        public static void PerformTests()
        {
            Console.WriteLine("TestYamlJsonReader");

            // When debugging, put first test here
            //PerformTmlTest(Path.Combine(Path.GetFullPath(c_testDir), "JR7V-mod.tml"), true);

            PerformTests(Path.GetFullPath(c_testDir));
            //PerformTests(@"C:\Users\brand\source\temp\yaml-test-suite\test", "alias", "flow");
        }

        static void PerformTests(string testDir, params string[] filter)
        {
            int tests = 0;
            int skipped = 0;
            int passed = 0;

            Console.WriteLine($"Performing tests in: {testDir}");
            foreach (var tmlFilename in Directory.GetFiles(testDir, "*.tml"))
            {
                ++tests;
                bool skip = false;
                using (var tml = new TestML())
                {
                    try
                    {
                        tml.Load(tmlFilename);
                        foreach (var tag in filter)
                        {
                            if (tml.Tags.Contains(tag))
                            {
                                skip = true;
                                break;
                            }
                        }
                    }
                    catch (Exception err)
                    {
                        Console.WriteLine($"{tmlFilename}: {err.Message}");
                        skip = true;
                    }
                    if (skip)
                    {
                        ++skipped;
                    }
                    else if (PerformTmlTest(tml))
                    {
                        ++passed;
                    }
                }
            }
            int failed = tests - skipped - passed;
            Console.WriteLine($"{tests} Tests");
            Console.WriteLine($"{passed} Passed");
            if (skipped > 0)
                Console.WriteLine($"{skipped} Skipped");
            if (failed > 0)
                Console.WriteLine($"{failed} Failed");
            if (failed == 0)
                Console.WriteLine("Success!");
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

        static bool PerformTmlTest(string filename, bool trace = false)
        {
            using (var tml = new TestML())
            {
                tml.Load(filename);
                tml.Trace = trace;
                return PerformTmlTest(tml);
            }
        }

        static bool PerformTmlTest(TestML tml)
        {
            try
            {
                Console.WriteLine($"  {tml.Filename}: {tml.Title}");
                tml.PerformTest();
            }
            catch (Exception err)
            {
                Console.WriteLine($"    {err.Message}");
                return false;
            }
            return true;
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
