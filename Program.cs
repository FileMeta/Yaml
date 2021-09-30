/*
---
Title: MicroYaml Unit Tests
Filename: Program.cs
...
*/

using System;
using System.Collections.Generic;
using System.IO;
using FileMeta.Yaml;

namespace UnitTests
{
    class Program
    {
        static bool m_trace = false; // Manually set to true for verbose output

        static int m_successCount;
        static int m_failureCount;

        // Execute all unit tests.
        static void Main(string[] args)
        {
            try
            {
                TestYamlJsonReader.PerformTests();

                m_successCount = 0;
                m_failureCount = 0;

                ExecuteTest("0", m_cTest0, m_cTest0Expected, false);
                ExecuteTest("1", m_cTest1, m_cTest1Expected, false);
                ExecuteTest("2", m_cTest2, m_cTest2Expected, true);

                if (m_failureCount == 0)
                {
                    Console.WriteLine("All {0} tests passed.", m_successCount);
                }
                else
                {
                    Console.WriteLine("{0} tests passed.", m_successCount);
                    Console.WriteLine("{0} tests failed.", m_failureCount);
                }
            }
            catch (Exception err)
            {
                Console.WriteLine(err.ToString());
            }

            Win32Interop.ConsoleHelper.PromptAndWaitIfSoleConsole();
        }

        static bool ExecuteTest(string description, string yaml, IEnumerable<KeyValuePair<string, string>> expected, bool ignoreTextOutside = false)
        {
            Console.WriteLine("Peforming Test '{0}'...", description);
            bool success = true;
            Trace("--- MergeDocs off ---");
            if (!CompareDocWithExpected(yaml, expected, false, ignoreTextOutside)) success = false;
            Trace("--- MergeDocs on ---");
            if (!CompareDocWithExpected(yaml, expected, true, ignoreTextOutside)) success = false;
            Trace("--- Iterative EOF ---");
            if (!TestIterativeEof(yaml)) success = false;

            // If not a multi-document test
            if (!ignoreTextOutside)
            {
                // Repeat the test with document markers
                string yamlDelimited = string.Concat("---\r\n", yaml, "...\r\n");
                Trace("--- With document markers ---");
                if (!CompareDocWithExpected(yamlDelimited, expected, false, true)) success = false;

                // Repeat the test with simple newlines
                Trace("--- With simple newlines ---");
                if (!CompareDocWithExpected(yamlDelimited.Replace("\r\n", "\n"), expected, true, true)) success = false;

                // Test YamlWriter
                Trace("--- YamlWriter ---");
                string yaml2 = MicroYaml.SaveToString(expected);
                if (!CompareDocWithExpected(yaml2, expected, true, true)) success = false;
            }

            Console.WriteLine("Test '{0}' {1}.", description, success ? "Success" : "Failure");
            Console.WriteLine();

            if (success)
            {
                ++m_successCount;
            }
            else
            {
                ++m_failureCount;
            }

            return success;
        }

        static bool CompareDocWithExpected(string yaml, IEnumerable<KeyValuePair<string, string>> expected, bool mergeDocs, bool ignoreTextOutside = false)
        {
            bool success = true;

            MicroYamlReader reader = null;
            IEnumerator<KeyValuePair<string, string>> comp = null;
            try
            {
                var options = new YamlReaderOptions();
                options.MergeDocuments = mergeDocs;
                options.IgnoreTextOutsideDocumentMarkers = ignoreTextOutside;
                reader = new MicroYamlReader(new StringReader(yaml), options);
                comp = expected.GetEnumerator();

                bool eofComp = false;
                for (;;)
                {
                    bool docHasValue = reader.MoveNext();

                    bool compHasValue = false;
                    if (!eofComp)
                    {
                        compHasValue = comp.MoveNext();
                        if (mergeDocs)
                        {
                            while (compHasValue && comp.Current.Key == null)
                            {
                                compHasValue = comp.MoveNext();
                            }
                        }
                        eofComp = !compHasValue;
                    }

                    if (!docHasValue)
                    {
                        if (!reader.MoveNextDocument())
                        {
                            break;
                        }
                        Trace("--- Document Break ---");
                        if (compHasValue && comp.Current.Key != null)
                        {
                            Trace("   Unexpected document break.");
                            success = false;
                        }
                        continue;
                    }

                    if (eofComp)
                    {
                        ReportError("Expected result at EOF but YAML input remains.");
                        success = false;
                    }

                    Trace("(\"{0}\", \"{1}\")", EscapeString(reader.Current.Key), EscapeString(reader.Current.Value));

                    if (reader.ErrorOccurred)
                    {
                        ReportError(reader.Errors[0].Message);
                        success = false;
                    }

                    if (!eofComp)
                    {
                        if (comp.Current.Key == null)
                        {
                            Trace("   Expected document break.");
                        }
                        else
                        {
                            if (!string.Equals(reader.Current.Key, comp.Current.Key, StringComparison.Ordinal))
                            {
                                Trace("   Keys don't match:\r\n      \"{0}\"\r\n      \"{1}\"", EscapeString(reader.Current.Key), EscapeString(comp.Current.Key));
                                success = false;
                            }
                            if (!string.Equals(reader.Current.Value, comp.Current.Value))
                            {
                                Trace("   Values don't match:\r\n      \"{0}\"\r\n      \"{1}\"", EscapeString(reader.Current.Value), EscapeString(comp.Current.Value));
                                success = false;
                            }
                        }
                    }
                }

                if (comp.MoveNext())
                {
                    ReportError("YAML at EOF but still values in expected result.");
                }
            }
            catch (Exception err)
            {
                ReportError(err.ToString());
                success = false;
            }
            finally
            {
                if (comp != null)
                {
                    comp.Dispose();
                    comp = null;
                }
                if (reader != null)
                {
                    reader.Dispose();
                    reader = null;
                }
            }

            return success;
        }

        // Cut the file at one character shorter until it is zero length.
        // Each time attempt to parse using all four option permutations.
        // This will yield numerous syntax errors but should not crash
        // the parser.
        static bool TestIterativeEof(string yaml)
        {
            try
            {
                YamlReaderOptions options = new YamlReaderOptions();
                options.ThrowOnError = false;
                while (yaml.Length > 0)
                {
                    for (int i=0; i<4; ++i)
                    {
                        options.IgnoreTextOutsideDocumentMarkers = (i & 1) != 0;
                        options.MergeDocuments = (i & 2) != 0;
                        using (var reader = new MicroYamlReader(new StringReader(yaml), options))
                        {
                            while (reader.MoveNextDocument())
                            {
                                while (reader.MoveNext())
                                {
                                    // Do nothing
                                    //Trace("(\"{0}\", \"{1}\")", EscapeString(reader.Current.Key), EscapeString(reader.Current.Value));
                                }
                            }
                        }
                    }

                    // Sorten by one character
                    yaml = yaml.Substring(0, yaml.Length - 1);
                }
            }
            catch (Exception err)
            {
                ReportError(err.ToString());
                return false;
            }
            return true;
        }

        static string EscapeString(string str)
        {
            str = str.Replace("\"", "\\\"");
            str = str.Replace("\n", "\\n");
            str = str.Replace("\r", "\\r");
            str = str.Replace("\t", "\\t");
            return str;
        }

        static void ReportError(string error)
        {
            ConsoleColor oldColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("   Error: " + error);
            Console.WriteLine();
            Console.ForegroundColor = oldColor;
        }

        static void Trace(string msg, params object[] args)
        {
            if (m_trace)
            {
                Console.WriteLine(msg, args);
            }
        }

        #region Test 0
        // Test 0 is an empty file
        const string m_cTest0 = "";
        static KeyValuePair<string, string>[] m_cTest0Expected = new KeyValuePair<string, string>[] {};

        #endregion Test 0

        #region Test 1
        // Test 1 is a sample of all of the legitimate key and value formats
        // It's also a nice example of how to compose MicroYaml documents.
        const string m_cTest1 = @"
# This is a comment followed by a key-value pair.
key: value

# In simple format, the ""scalar"" is terminated with a delimiter or a new line.

simple1: simple-value
simple2: simple value with ' quotes "" that are preserved literally
simple3: All alphanumeric and many other @-!$*:?${};'""[]~()_+=~`|<> literal characters are acceptable
simple4: A comment # May follow the value.
simple5: A colon followed by a space delimits the value. When embedded in a simple value, the :colon must not be followed by a space.

# In the following entries, the name is in simple format while the value is in single-quote format.
single-quote1: 'This is the value. ''Embedded single-quotes'' are doubled up.'
single-quote2: 'In single-quoted format you   
may have line breaks. Line breaks in this format use line-folding    
meaning that they are converted to a single space character and  
not preserved literally.'

# The following entries have values in double-quote format.
double-quote1: ""This a double-quote value.\nIt uses c-style escaping.""
double-quote2: ""Like single-quote values, double-quote   
values may have embedded line breaks. Also like single-quote   
values the newlines are converted into spaces  
and trailing spaces are stripped. To embed
a literal newline, use the \n escape. To embed quotes, use the \""quote\"" escape.""
double-quote3: ""You may escape the \
newline itself thereby supporting trailing spaces and\
embedded newlines.""
double-quote4: ""Hex \x7E and Unicode \u007B escapes are also supported.""

# Literal block format is indicated by the | character
literal1: |
 Values in literal block format must be indented.


 Newlines are significant and preserved.    
 Trailing spaces are trimmed.     

 Indentation beyond the amount of the first line
  is also preserved.
literal2: |-
 A dash after the block indicator means to ""chomp"" the   
 terminating newline in literal block format.
literal3: |+
 A plus after the block indicator means to include the

 terminating newline and any subsequent blank lines.

literal4: |1-
   A numeral after the block indicator indicates the
 number of indentation characters thereby allowing the
 first line of the value to have leading whitespace.

# Folded block format is indicated by the > character
folded1: >
 In folded format, newlines are converted to spaces
 and trailing spaces are trimmed.
folded2: >1-
  Folded format also supports the indentation indicator
 and the chomping indicator.
folded3: >
 In folded format, a newline followed by a blank line

 results in ONE embedded newline.

# All of the preceding examples have used simple keys and
# complex values. But keys may use all of the same formats
# that values can.
simple key: value
'single quote key': value
""double quote
 key"": value
|-
 Literal block key
: value


# A few more edge cases for testing purposes
edge1 : Trailing spaces on a simple key.
""edge2"" : Trailing spaces on a double-quote key.
""edge3 "": Preserved trailing spaces on a double-quote key.
edge4: |
 Literal block followed by empty lines




edge5: |
 Literal block without any trailing empty lines.
edge6: |-
 Literal block with chomping followed by empty lines



edge7: |+
 Literal block with keeping followed by empty lines



edge8: >
 Folded block with trailing empty lines.



Edge9: >-
 Folded block with chomping followed by empty lines




Edge10: >+
 Folded block with keeping followed by empty lines



Edge11: >-
 Folded block with embedded and trailing empty lines


 and chomping.



Edge12: End of file.
";

        static KeyValuePair<string, string>[] m_cTest1Expected = new KeyValuePair<string, string>[]
        {
            new KeyValuePair<string, string>("key", "value"),
            new KeyValuePair<string, string>("simple1", "simple-value"),
            new KeyValuePair<string, string>("simple2", "simple value with ' quotes \" that are preserved literally"),
            new KeyValuePair<string, string>("simple3", "All alphanumeric and many other @-!$*:?${};'\"[]~()_+=~`|<> literal characters are acceptable"),
            new KeyValuePair<string, string>("simple4", "A comment"),
            new KeyValuePair<string, string>("simple5", "A colon followed by a space delimits the value. When embedded in a simple value, the :colon must not be followed by a space."),
            new KeyValuePair<string, string>("single-quote1", "This is the value. 'Embedded single-quotes' are doubled up."),
            new KeyValuePair<string, string>("single-quote2", "In single-quoted format you may have line breaks. Line breaks in this format use line-folding meaning that they are converted to a single space character and not preserved literally."),
            new KeyValuePair<string, string>("double-quote1", "This a double-quote value.\nIt uses c-style escaping."),
            new KeyValuePair<string, string>("double-quote2", "Like single-quote values, double-quote values may have embedded line breaks. Also like single-quote values the newlines are converted into spaces and trailing spaces are stripped. To embed a literal newline, use the \n escape. To embed quotes, use the \"quote\" escape."),
            new KeyValuePair<string, string>("double-quote3", "You may escape the \nnewline itself thereby supporting trailing spaces and\nembedded newlines."),
            new KeyValuePair<string, string>("double-quote4", "Hex ~ and Unicode { escapes are also supported."),
            new KeyValuePair<string, string>("literal1", "Values in literal block format must be indented.\n\n\nNewlines are significant and preserved.\nTrailing spaces are trimmed.\n\nIndentation beyond the amount of the first line\n is also preserved.\n"),
            new KeyValuePair<string, string>("literal2", "A dash after the block indicator means to \"chomp\" the\nterminating newline in literal block format."),
            new KeyValuePair<string, string>("literal3", "A plus after the block indicator means to include the\n\nterminating newline and any subsequent blank lines.\n\n"),
            new KeyValuePair<string, string>("literal4", "   A numeral after the block indicator indicates the\nnumber of indentation characters thereby allowing the\nfirst line of the value to have leading whitespace."),
            new KeyValuePair<string, string>("folded1", "In folded format, newlines are converted to spaces and trailing spaces are trimmed.\n"),
            new KeyValuePair<string, string>("folded2", "  Folded format also supports the indentation indicator and the chomping indicator."),
            new KeyValuePair<string, string>("folded3", "In folded format, a newline followed by a blank line\nresults in ONE embedded newline.\n"),
            new KeyValuePair<string, string>("simple key", "value"),
            new KeyValuePair<string, string>("single quote key", "value"),
            new KeyValuePair<string, string>("double quote key", "value"),
            new KeyValuePair<string, string>("Literal block key", "value"),
            new KeyValuePair<string, string>("edge1", "Trailing spaces on a simple key."),
            new KeyValuePair<string, string>("edge2", "Trailing spaces on a double-quote key."),
            new KeyValuePair<string, string>("edge3 ", "Preserved trailing spaces on a double-quote key."),
            new KeyValuePair<string, string>("edge4", "Literal block followed by empty lines\n"),
            new KeyValuePair<string, string>("edge5", "Literal block without any trailing empty lines.\n"),
            new KeyValuePair<string, string>("edge6", "Literal block with chomping followed by empty lines"),
            new KeyValuePair<string, string>("edge7", "Literal block with keeping followed by empty lines\n\n\n\n"),
            new KeyValuePair<string, string>("edge8", "Folded block with trailing empty lines.\n"),
            new KeyValuePair<string, string>("Edge9", "Folded block with chomping followed by empty lines"),
            new KeyValuePair<string, string>("Edge10", "Folded block with keeping followed by empty lines\n\n\n"),
            new KeyValuePair<string, string>("Edge11", "Folded block with embedded and trailing empty lines\n\nand chomping."),
            new KeyValuePair<string, string>("Edge12", "End of file.")
        };

        #endregion Test 1

        #region Test 2
        // Test 2 measures the ability to embed MicroYaml documents in other text
        // and also tests the multi-document function.
        const string m_cTest2 = @"
Some non-YAML Stuff
More non-YAML stuff - to be skipped because we haven't
yet reached the doc start marker.
---
# In the document now - this is a comment
Key1: Value1
Key2: |
 A longer value written out
 in literal form.
...
Outside the document, more stuff to be skipped.
This line has an embedded --- three dashes.
--- This line starts with three dashes.
And this line ends with three ---
---
Key3: Value3
#  The value should be ..., not end-of-doc
Key4: ...
...
Some more out-of-document experiences.
---
Key5: Fife # At one point comments at the end of the document messed things up
...
Out
of
doc
---
Key6: |
---
# That was an empty block value at the end of a document
# going right into another document
Key7: Lucky Seven
...
Some between-document stuff
And more
---
Key8: Ate
# Followed by blank lines


...
End of File
";

        static KeyValuePair<string, string>[] m_cTest2Expected = new KeyValuePair<string, string>[]
        {
            new KeyValuePair<string, string>("Key1", "Value1"),
            new KeyValuePair<string, string>("Key2", "A longer value written out\nin literal form.\n"),
            new KeyValuePair<string, string>(null, null),   // Null values indicate a document break
            new KeyValuePair<string, string>("Key3", "Value3"),
            new KeyValuePair<string, string>("Key4", "..."),
            new KeyValuePair<string, string>(null, null),
            new KeyValuePair<string, string>("Key5", "Fife"),
            new KeyValuePair<string, string>(null, null),
            new KeyValuePair<string, string>("Key6", ""),
            new KeyValuePair<string, string>(null, null),
            new KeyValuePair<string, string>("Key7", "Lucky Seven"),
            new KeyValuePair<string, string>(null, null),
            new KeyValuePair<string, string>("Key8", "Ate")
        };

        #endregion Test 2

    }
}
