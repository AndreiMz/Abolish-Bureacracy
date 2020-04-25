using CommandLine;
using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Utf8Json;

namespace OpenFieldReader
{
	class Program
    {
        static void Main(string[] args)
		{
			Parser.Default.ParseArguments<OpenFieldReaderOptions>(args)
				.WithParsed(opts => Run(opts))
				.WithNotParsed(errs => HandleErrors(errs));
		}

		private static void Run(OpenFieldReaderOptions opts){
			var openFieldReader = new OpenFieldReader(opts);
			openFieldReader.Process();
		}

		private static object HandleErrors(IEnumerable<Error> errs)
		{
			Environment.Exit(1);
			return 1;
		}
	}
		
}
