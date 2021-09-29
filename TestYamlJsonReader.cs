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
            Console.WriteLine("TestYamlJsonReader");
            var testDir = Path.GetFullPath("./TestDocs");

            Console.WriteLine("Raw Tests:");
            foreach (var yamlFilename in Directory.GetFiles(testDir, "*.yml"))
            {
                var jsonFilename = Path.Combine(testDir, Path.GetFileNameWithoutExtension(yamlFilename) + ".json");
                if (File.Exists(jsonFilename))
                {
                    Console.WriteLine($"  {Path.GetFileName(yamlFilename)} = {Path.GetFileName(jsonFilename)}");
                    CompareYamlToJson.Compare(yamlFilename, jsonFilename);
                }
            }

            Console.WriteLine("TestML Tests:");
            foreach(var testMlFilename in Directory.GetFiles(testDir, "*.tml"))
            {
                using (var tml = new TestML())
                {
                    tml.Load(testMlFilename);
                    Console.WriteLine($"  {Path.GetFileName(testMlFilename)}: {tml.Title}");
                    tml.PerformTest();
                }
            }

            Console.WriteLine("TestYamlJsonReader Success.");
        }
    }
}
