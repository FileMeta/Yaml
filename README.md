# FileMeta.Yaml

*I have abandoned this project. It passes about 80% of the YAML validation tests for the features it implements. See the Limitations section for the features that I deliberately didn't implement and the philosophy behind that. If you want to pick up the project and carry it forward you are welcome to do so. It is under a BSD 3-clause open license. I recommend creating your own fork. If you get it to a point where you like the result, please contact me and I'll put a link here to your continuation. I have many, complicated, reasons for abanding this but the main one is that I switched to a different data format for a project that was originally to use YAML.*

A simple [YAML](http://www.yaml.org/) scanner/reader/parser.

FileMeta.Yaml is a YAML reader written in C# that can manifest the following interfaces:

* System.Collections.IEnumerable<KeyValuePair<string,string>>
* NewtonSoft.Json.JsonReader
* System.Xml.XmlReader

This parser does not implement the full [YAML 1.1](http://yaml.org/spec/1.1/) specification. It matches the [StrictYAML](https://hitchdev.com/strictyaml/) limitations plus a few more. For details, see the [Limitations](#Limitations) section below.

## About CodeBits

A CodeBit is a way to share common code that's lighter weight than NuGet. CodeBits are contained in one source code file. A structured comment at the beginning of the file indicates where to find the master copy so that automated tools can retrieve and update CodeBits to the latest version. For more information see http://FileMeta.org/CodeBit.html.

## Why YAML?

YAML is a simple and intuitive format where newlines and indentation are significant to the parser just as they are to the writer. Most people encountering YAML can successfully add or edit information without needing to learn the syntax and without creating syntax errors.

[YAML](http://www.yaml.org/) is a convenient format for human-written metadata such as that used for [CodeBits](http://FileMeta.org/CodeBit.html).

Here's a sample:
```yml
# This Yaml document expresses the five elements from the Dublin Core
Title: The Hitchhiker's Guide to the Galaxy
Creator: Douglas Adams
Subject: "Fiction"
Description: >
   The misadventures of Arthur Dent, the last surviving man following
demolition of Planet Earth by a Vogon constructor fleet to make way
   for a hyperspace bypass.
Date: 1979-07-15
```

## Limitations

FileMeta.Yaml has the following exceptions to the [official YAML 1.1](http://yaml.org/spec/1.1/) that are expected to be retained perpetually. They include all of the limitations of [StrictYAML](https://hitchdev.com/strictyaml/) plus a few more.

* No implicit or explicit type conversion. All data are parsed as strings. This prevents the [Norway Problem](https://hitchdev.com/strictyaml/why/implicit-typing-removed/), loss of leading zeros, conversion of version numbers to float and a host of other unexpected outcomes. Type conversion should be performed by the FileMeta.json client. (StrictYAML prohibits implicit conversion but includes explicit).
* No direct representations of objects (no Node Tags). See the [StrictYAML reasoning](https://hitchdev.com/strictyaml/why/binary-data-removed)) for this.
* No node anchors or references. They can be confusing to non-programmers. Plus they risk unintended side effects when a naive user edits the data. (StrictYAML has the [same limitation](https://hitchdev.com/strictyaml/why/node-anchors-and-references-removed/))
* No explicit keys (see [YAML 1.1 section 10.2.1](http://yaml.org/spec/1.1/#simple%20key/)). Keys have no identifying mark, they consist of a simple sequence of characters, and they are limited to one line unless surrounded by double quotes.

The following limitations may be removed in the future if people advocate for the features or submit a clean pull request.

* No directives. You cannot use %YAML to specify the version. or %TAG to create key shorthand.
* No flow style. [YAML 1.2](http://yaml.org/spec/1.2/) allows you to use JSON syntax within a YAML document. This limitation _may_ be removed in the future. However, there are [arguments against it](https://hitchdev.com/strictyaml/why/flow-style-removed/)

### Why the limitations?
While YAML starts out simple, some of the constructs, like Complex Mapping Keys, Compound Values, and embedded JSON can make it more challenging. Humans may not understand what's going on and parsers have to produce a complicated DOM to represent the document.

These limitations are intended to keep YAML simple and intuitive for users who have never read the documentation.

## Extended YAML Sample
This sample demonstrates most MicroYaml features.

```yml
# YAML comments start with a # sign

# A Key is delimited from a value with a colon and a space. The space is mandatory.
key: value

# In simple format, the value is terminated with a new line.
simple1: simple value
simple2: simple value with ' quotes "" that are preserved literally
simple3: All alphanumeric and many other @-!$*:?${};'""[]~()_+=~`|<> literal characters are acceptable
simple4: simple value # A comment may follow a value. To be a comment, the # must be preceded by a space.
simple5: A colon followed by a space delimits the value from the key.
simple6: When embedded in a simple value, the :colon must not be followed by a space.

# In the following entries, the key is in simple format while the value is in single-quote format.
single-quote1: 'This is the value. ''Embedded single-quotes'' are doubled.'
single-quote2: 'In single-quoted format you   
may have line breaks. Line breaks in this format use line-folding    
meaning that they are converted to a single space character and  
not preserved literally.'

# The following entries, the value is in double-quote format.
double-quote1: "This a double-quote value."
double-quote2: "Unlike single-quote values, double-quote values use \"c-style\" escaping."
double-quote2: "Like single-quote values, double-quote   
values may have embedded line breaks. Also like single-quote   
values the newlines are converted into spaces
and trailing spaces are stripped. To embed
a literal newline, use the \n escape. To embed quotes, use the \"quote\" escape."
double-quote3: "You may escape the \
newline itself thereby supporting trailing spaces and\
embedded newlines."
double-quote4: "Hex \x7E and Unicode \u007B escapes are also supported."

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
 While a single newline
 is replaced with a space.

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
>-
 Folded block key
: value
```
