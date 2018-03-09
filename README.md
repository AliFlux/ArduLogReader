## ArduLogReader

Reader for ArduPilot/APM binary logs made in C#

![ArduLogReader](blob/master/promo.png)

### So what does it do?

This handy piece of code allows you to read and parse the ArduPilot binary system logs (.bin) format, also known as SDLogs. It also supports PixHawk/PX4 firmware logs that use the same format.

### Basic Example

```C#
var parser = new ArduLogParser("example.bin");
parser.Parse();
// `parser.Results` contains the all the extracted data

// Generate and output CSV to console
var csv = parser.GenerateCSV();
Console.Write(csv);

// Write CSV to a file
System.IO.File.WriteAllText("Huge.csv", csv);
```

See [`/ArduLogReader/Program.cs`](https://github.com/AliFlux/ArduLogReader/blob/master/ArduLogReader/Program.cs) for a running example.


### Using Async

The recommended approach towards reading a binary file is to do blocking operations asynchronously. This example demonstrates an async call.

```C#
var parser = new ArduLogParser("example.bin");
await parser.Parse();
// `parser.Results` contains the all the extracted data
```

### Getting Progress

For large flight logs, ArduLogReader can give the progress so that it can be used in the UI, and make waiting a bit more bearable. **Note: This only works with async parsing**

```C#
parser.OnProgress += Parser_OnProgress;
await parser.Parse();

private static void Parser_OnProgress(object sender, double e)
{
	// where `e` is the progress from 0 to 1
	Console.WriteLine(e);
}
```

### Motivation

There are implementations for binary log reading in python and C++, but not C#. C++ implementation could be used using C++/CLI, but the code is heavy and bloated. This implementation is lightweight.

### Features

- Tiny API that provides both structured output and CSV
- Supports both async and sync functions for all-purpose use.
- Supports logging progress with events (`OnStart`, `OnProgress`, `OnEnd`)
- Can load interrupted flight logs
- Parts of the code can be recycled to fit other logging formats (ulog, tlog, ...)

### Contribution

Bug reports, suggestions and pull requests are all welcome. Please submit them to the [GitHub issue tracker](https://github.com/AliFlux/ArduLogReader/issues).

### Changelog

Details changes for each release will be documented in the [release notes](https://github.com/AliFlux/ArduLogReader/releases).

### Stay In Touch

For latest releases and announcements, check out my site: [aliashraf.net](http://aliashraf.net)

### License

This software is released under the [MIT License](LICENSE). Please read LICENSE for information on the
software availability and distribution.

Copyright (c) 2018 [Ali Ashraf](http://aliashraf.net)