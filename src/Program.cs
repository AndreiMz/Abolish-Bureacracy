using CommandLine;
using System;
using System.Collections.Generic;

namespace FormReader
{
    class Program
    {
        static void Main(string[] args)
		{
			Parser.Default.ParseArguments<FormReaderOptions>(args)
				.WithParsed(opts => Run(opts))
				.WithNotParsed(errs => HandleErrors(errs));
		}

		private static void Run(FormReaderOptions opts){
			var openFieldReader = new FormReader(opts);
			openFieldReader.Process();
		}

		private static object HandleErrors(IEnumerable<Error> errs)
		{
			Environment.Exit(1);
			return 1;
		}
	}
		
}
