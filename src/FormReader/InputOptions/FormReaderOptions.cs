﻿using CommandLine;
using System;
using System.Collections.Generic;
using System.Text;

namespace FormReader
{
    public class FormReaderOptions
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

        [Option("min-cell-width", Default = 15, HelpText = "Min estimated cell width (should not be less than 15.)")]
        public int minX { get; set; }

        [Option("max-cell-width", Default = 70, HelpText = "Max estimated cell width (should not be greater than 85. Most of the time, it can be reduced to 50 or 60.)")]
        public int maxX { get; set; }

        [Option("y-variation", Default = 3, HelpText = "Variation of y to find next cell. (should be really small for faster result)")]
        public int variationY { get; set; }

        [Option("min-element-perecent", Default = 60, HelpText = "Minimum percent of elements to determine the direction")]
        public int minElementPercent { get; set; }

        // These properties prevent wasting CPU on complex image.

        [Option("max-proximity", Default = 10, HelpText = "Helps Speed up CPU by discarding black spots earlier in the process")]
        public int maxProximity { get; set; }

        [Option("max-junctions", Default = 20000, HelpText = "Maximum number of junctions allowed.")]
        public int MaxJunctions { get; set; }

        [Option("max-solutions", Default = 50000, HelpText = "Maximum number of solution allowed in the process.")]
        public int MaxSolutions { get; set; }

        // Debug.

        [Option("generate-debug-image", Default = true, HelpText = "Generate a debug image?")]
        public bool GenerateDebugImage { get; set; }

        // Output.

        [Option("output", Default = "std", HelpText = "Output type (std or path file)")]
        public string OutputPath { get; set; }

        [Option("verbose", Default = true, HelpText = "Verbose")]
        public bool Verbose { get; set; }
    }
}
