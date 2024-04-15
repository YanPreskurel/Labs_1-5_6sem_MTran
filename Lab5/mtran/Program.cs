using System;
using System.IO;
using System.Diagnostics;

namespace mtran
{
	internal class Program
	{
		static void Main()
		{
			Environment.CurrentDirectory += "/files";

			PrintResults();

            //PrintResults("input.txt");

            //PrintResults("test2.txt");

            PrintResults("test.txt");

            Console.ReadKey();
        }


		static void PrintResults(string fileName)
		{
			return;
			Console.WriteLine($"Analysing {fileName}\n");

			var text = File.ReadAllText(fileName);
			var lexer = new Lexer();
			if (lexer.Analyse(text))
			{
				//lexer.PrintInfo();

				var parser = new Parser(lexer);
				if (parser.Analyse())
				{
					//parser.PrintInfo();

					var semanticAnalyser = new SemanticAnalyser(parser.Ast);
					if (semanticAnalyser.Analyse())
					{
						var interpreter = new Interpreter(parser.Ast);
						if (interpreter.Run())
						{
							Console.WriteLine("Great success!");
						}
						else
						{
							Console.Error.WriteLine("Error occured in interpreter.");
						}
					}
					else
					{
						Console.Error.WriteLine("Error occured in semantic analyser.");
					}
				}
				else
				{
					Console.Error.WriteLine("Error occured in parser.");
				}
			}
			else
			{
				Console.Error.WriteLine("Error occured in lexer.");
			}

			Console.WriteLine();
		}
		
		static void PrintResults()
		{
            string pythonPath = @"C:\Users\37529\AppData\Local\Programs\Python\Python312\python.exe";
            string scriptPath = @"C:\Users\37529\Desktop\Лекции\МТран\Labs\Lab5\test.py";

            // Создание процесса для запуска Python-скрипта
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = pythonPath;
            startInfo.Arguments = scriptPath;
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;

            // Запуск процесса
            using (Process process = Process.Start(startInfo))
            {
                // Чтение вывода Python-скрипта
                using (System.IO.StreamReader reader = process.StandardOutput)
                {
                    string result = reader.ReadToEnd();
                    Console.WriteLine(result);
                }
            }
        }
	}
}
