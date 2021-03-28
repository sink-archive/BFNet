﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BFNet.Execution;
using BFNet.PreProcessing;

namespace BFNet.Cli
{
	// ReSharper disable once ArrangeTypeModifiers
	// ReSharper disable once ClassNeverInstantiated.Global
	class Program
	{
		private static void Main(string[] args)
		{
			char?  lineCommentChar = null;
			string input           = null;
			for (var i = 0; i < args.Length; i++)
			{
				switch (args[i])
				{
					// ReSharper disable once NotResolvedInText
					case "-lcc" when args[i + 1].Length != 1:
						throw new Exception($"Line comment character value '{args[i + 1]}' is not valid.");
					case "-lcc":
						lineCommentChar = args[i + 1][0];
						break;
					case "-i":
						input = args[i + 1];
						break;
				}
			}

			var argsResult = CheckArgs(args);
			var bf = argsResult switch
			{
				CheckArgsResult.File     => File.ReadAllText(args[0]),
				CheckArgsResult.BrainFck => args[0],
				CheckArgsResult.None     => InteractiveMode(),
				_                        => throw new ArgumentOutOfRangeException()
			};

			var processed = bf.ProcessAndParse(lineCommentChar).Optimize();
			var interpreter = new Interpreter(processed, new InterpreterSettings
			{
				Input      = string.IsNullOrWhiteSpace(input) ? default : input,
				UseConsole = string.IsNullOrWhiteSpace(input)
			});

			// interpret and also hide cursor because looks nice™
			Console.CursorVisible = false;
			interpreter.StartInterpret();
			Console.CursorVisible = true;
		}

		private static CheckArgsResult CheckArgs(IReadOnlyList<string> args)
		{
			// must be at least one arg
			if (args.Count == 0) return CheckArgsResult.None;

			return CheckForFile()
				? CheckArgsResult.File // see if its a file
				: CheckForBrainFck()
					? CheckArgsResult.BrainFck // if not is it possibly brainfuck?
					: CheckArgsResult.None;    // nothing

			bool CheckForFile() => File.Exists(args[0]) // file must exist
								&& File.ReadAllText(args[0]).ProcessAndParse().Tree
									   .Any(); // must contain *some* BrainF*ck

			bool CheckForBrainFck() => args[0].ProcessAndParse().Tree.Any(); // see above comment
		}

		private static string InteractiveMode()
		{
			// run interactive mode
			var enteredBf = InteractiveMode(out var lineCount);
			
			// go to top of interactive mode result
			Console.CursorTop -= lineCount;
			
			// make a string to clear out the screen
			var xSb = new StringBuilder();
			for (var j = 0; j < Console.WindowWidth; j++) xSb.Append(' ');
			var ySb = new StringBuilder();
			for (var i = 0; i < lineCount; i++) ySb.Append(xSb);
			// write it
			Console.Write(ySb);
			
			// go to top again
			Console.CursorTop -= lineCount;

			return enteredBf;
		}

		private static string InteractiveMode(out int lineCount)
		{
			const string welcomeMsg = @"BFNet by Cain Atkinson https://github.com/cainy-a/BFNet
Interactive Mode - please enter BrainF*ck code below.
Press enter twice to exit and begin execution
";
			Console.WriteLine(welcomeMsg);
			
			IList<string> lines         = new List<string>();
			var           lastLineEmpty = false;
			while (true)
			{
				// get line from user
				var line = Console.ReadLine();
				
				// add to list
				lines.Add(line);
				
				// if two consecutive empty lines exit
				if (!string.IsNullOrWhiteSpace(line)) continue;
				if (lastLineEmpty) break;
				lastLineEmpty = true;
			}

			lineCount = welcomeMsg.Split('\n').Length + lines.Count;

			return lines.Aggregate(string.Empty, (current, next) => current + '\n' + next);
		}
	}

	internal enum CheckArgsResult
	{
		File,
		BrainFck,
		None
	}
}