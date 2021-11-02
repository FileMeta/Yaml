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

            //PerformTests(Path.GetFullPath(c_testDir));
            PerformTests(@"C:\Users\brand\source\FileMeta\Yaml-Test-Suite-Modified");
        }

        static void PerformTests(string testDir, params string[] filter)
        {
            int tests = 0;
            int loadErrors = 0;
            int aliasErrors = 0;
            int flowErrors = 0;
            int failures = 0;

            Console.WriteLine($"Performing tests in: {testDir}");
            foreach (var tmlFilename in Directory.GetFiles(testDir, "*.tml"))
            {
                ++tests;

                using (var tml = new TestML())
                {
                    // Load (and check for load error)
                    try
                    {
                        tml.Load(tmlFilename);
                    }
                    catch (Exception err)
                    {
                        Console.WriteLine($"{tmlFilename}: Load Error: {err.Message}");
                        ++loadErrors;
                        continue;
                    }

                    if (!PerformTmlTest(tml, out Exception errDetail))
                    {
                        if (tml.Tags.Contains("anchor") || tml.Tags.Contains("alias"))
                        {
                            Console.WriteLine($"    Anchor/Alias Error: {errDetail.Message}");
                            ++aliasErrors;
                        }
                        else if (tml.Tags.Contains("flow"))
                        {
                            Console.WriteLine($"    Flow Error: {errDetail.Message}");
                            ++flowErrors;
                        }
                        else
                        {
                            var saveColor = Console.ForegroundColor;
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"    Failure: {errDetail.Message}");
                            Console.ForegroundColor = saveColor;
                            ++failures;
                        }
                    }
                }
            }
            int passed = tests - (loadErrors + aliasErrors + flowErrors + failures);
            Console.WriteLine($"{tests} Tests");
            Console.WriteLine($"{passed} Passed");
            if (loadErrors > 0)
                Console.WriteLine($"{loadErrors} Load Errors");
            if (aliasErrors > 0)
                Console.WriteLine($"{aliasErrors} Alias Not Supported");
            if (flowErrors > 0)
                Console.WriteLine($"{flowErrors} Flow Not Supported");
            if (failures > 0)
                Console.WriteLine($"{failures} Test Failures");
            if (passed >= tests)
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
                Exception errDetail;
                if (!PerformTmlTest(tml, out errDetail))
                {
                    Console.WriteLine($"  {errDetail.Message}");
                    return false;
                }
                return true;
            }
        }

        static bool PerformTmlTest(TestML tml, out Exception errDetail)
        {
            try
            {
                Console.WriteLine($"  {tml.Filename}: {tml.Title}");
                tml.PerformTest();
            }
            catch (Exception err)
            {
                errDetail = err;
                return false;
            }
            errDetail = null;
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
