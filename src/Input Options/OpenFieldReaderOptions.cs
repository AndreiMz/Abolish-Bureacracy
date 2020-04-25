using CommandLine;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenFieldReader
{
    public class OpenFieldReaderOptions
    {
        // Input.

        [Option("input", HelpText = "Path to the image", Required = false, Default = "../../../../Samples/form2.jpg")]
        public string InputFile { get; set; }

        [Option("width", Default = 25, HelpText = "Junction minimum width")]
        public int JunctionWidth { get; set; }

        [Option("height", Default = 15, HelpText = "Junction minimum height")]
        public int JunctionHeight { get; set; }

        [Option("min-num-elements", Default = 4, HelpText = "Minimum number of elements per group of boxes")]
        public int MinNumElements { get; set; }

        // These properties prevent wasting CPU on complex image.

        [Option("max-junctions", Default = 20000, HelpText = "Maximum number of junctions allowed.")]
        public int MaxJunctions { get; set; }

        [Option("max-solutions", Default = 50000, HelpText = "Maximum number of solution allowed in the process.")]
        public int MaxSolutions { get; set; }

        // Debug.

        [Option("generate-debug-image", Default = false, HelpText = "Generate a debug image?")]
        public bool GenerateDebugImage { get; set; }

        // Output.

        [Option("output", Default = "std", HelpText = "Output type (std or path file)")]
        public string OutputFile { get; set; }

        [Option("verbose", Default = false, HelpText = "Verbose")]
        public bool Verbose { get; set; }
    }
}
