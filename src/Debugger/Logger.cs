using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Utf8Json;

namespace OpenFieldReader.Debugger
{
    /// <summary>
    /// Preserves debugging messages from olden times. Not an error logger
    /// </summary>
    internal class Logger
    {
        private string OutputPath;
        private TextWriter consoleBuffer;

        public Logger(OpenFieldReaderOptions options)
        {
            this.OutputPath = options.OutputPath;
        }

        public void LogGenericMessage(string message)
        {
            if (this.OutputPath != "std")
            {
                this.redirectConsoleToLogFile();
            }

            Console.WriteLine(message);

            if (this.OutputPath != "std")
            {
                this.redirectConsoleToLogFile();
            }
        }

        public void LogSolutions(int numSol, List<Line> possibleSol, int skipSol)
        {
            

            if (this.OutputPath != "std")
            {
                this.redirectConsoleToLogFile();
            }

            Console.WriteLine("Skip solutions counter: " + skipSol);
            Console.WriteLine(numSol + " : Solution found");
            Console.WriteLine(possibleSol.Count + " : Best solution found");

            if (this.OutputPath != "std")
            {
                this.revertRedirect();
            }

        }

        public void LogResult(List<List<Box>> boxes)
        {
            if (this.OutputPath == "std")
            {
                // Show result on the console.

                Console.WriteLine("Boxes: " + boxes.Count);
                Console.WriteLine();

                int iBox = 1;
                foreach (var box in boxes)
                {
                    Console.WriteLine("Box #" + iBox);

                    foreach (var element in box)
                    {
                        Console.WriteLine("  Element: " +
                            element.TopLeft + "; " +
                            element.TopRight + "; " +
                            element.BottomRight + "; " +
                            element.BottomLeft);
                    }

                    iBox++;
                }
            }
            else
            {
                // Write result to output file.
                var outputPath = this.OutputPath;
                var json = JsonSerializer.ToJsonString(boxes);
                File.WriteAllText(outputPath, json);
            }
        }


        private void redirectConsoleToLogFile()
        {
            FileStream ostrm;
            StreamWriter writer;
            this.consoleBuffer = Console.Out;

            try
            {
                ostrm = new FileStream("./Redirect.txt", FileMode.OpenOrCreate, FileAccess.Write);
                writer = new StreamWriter(ostrm);
            }
            catch (Exception e)
            {
                Console.WriteLine("Cannot open Redirect.txt for writing");
                Console.WriteLine(e.Message);
                return;
            }
            Console.SetOut(writer);
        }

        private void revertRedirect()
        {
            Console.Out.Flush();
            Console.Out.Dispose();
            Console.SetOut(this.consoleBuffer);
            this.consoleBuffer = null; // Force integrity
        }

    }
}
