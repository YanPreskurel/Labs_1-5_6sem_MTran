using System;
using System.Collections.Generic;
using System.Globalization;

namespace mtran
{
	internal class Lexer
	{
		const int indentSpacesCount = 4;
		const string specialSymbols = "(){}[]<>,.:;!@%|&^*-+=/?#";
		internal readonly static List<string> keywords = new List<string>()
		{
			"if",
			"eilf",
			"else",
			"while",
			"for",
			"in",
			"range",
			"import",
			"from"
		};

		readonly List<string> names;
		readonly List<string> consts;
		readonly List<Token> tokens;

		int currentLine = 0;
		int currentRow = -1;

		LexemType currentTokenType;

		internal List<string> Names => names;
		internal List<string> Consts => consts;
		internal List<Token> Tokens => tokens;

		internal Lexer()
		{
			names = new List<string>();
			consts = new List<string>();
			tokens = new List<Token>();
		}

		internal bool Analyse(string text)
		{
			string temp = "";
			char stringOpening = ' ';
			char currentSymbol;
			int indentation = -1;

			currentTokenType = LexemType.NONE;

			bool CheckForString()
			{
				var type = GetTokenType(currentSymbol);
				if (type == LexemType.STRING)
				{
					if (stringOpening != currentSymbol)
					{
						ReportError($"String quotes are inconsistent: {stringOpening} and {currentSymbol}");

						return false;
					}
					if (!AddConst(LexemType.STRING, temp, indentation))
					{
						return false;
					}
					temp = "";
					currentTokenType = LexemType.NONE;
				}
				else
				{
					temp += currentSymbol;
				}

				return true;
			}

			bool SwitchToken(LexemType type)
			{
				switch (currentTokenType)
				{
					case LexemType.NAME:
						AddName(temp, indentation);
						temp = "";
						break;
					case LexemType.NUMBER:
						if (!AddConst(LexemType.NUMBER, temp, indentation))
						{
							return false;
						}
						temp = "";
						break;
					case LexemType.ERROR:
						return false;
					default:
						break;
				}
				currentTokenType = type;

				return true;
			}

			void CheckForConst(LexemType type)
			{
				switch (type)
				{
					case LexemType.NAME:
					case LexemType.NUMBER:
						temp += currentSymbol;
						break;
					case LexemType.SPECIAL:
						tokens.Add(new Token(this, LexemType.SPECIAL, currentLine, currentRow, symbol: currentSymbol, indentation: indentation));
						break;
					case LexemType.STRING:
						currentTokenType = LexemType.STRING;
						stringOpening = currentSymbol;
						temp = "";
						break;
					default:
						break;
				}
			}

			bool ProcessRemainingLexem()
			{
				switch (currentTokenType)
				{
					case LexemType.NAME:
						AddName(temp, indentation);
						break;
					case LexemType.NUMBER:
						if (!AddConst(LexemType.NUMBER, temp, indentation))
						{
							return false;
						}
						break;
					case LexemType.STRING:
						ReportError($"String did not end: {temp}");

						return false;
					default:
						break;
				}

				return true;
			}

			for (int i = 0; i < text.Length; i++)
			{
				currentSymbol = text[i];
				if (currentSymbol == '\n')
				{
					if (currentTokenType == LexemType.STRING)
					{
						ReportError($"String literal end ({stringOpening}) expected but code line ended");

						return false;
					}
					currentLine++;
					currentRow = -1;
					indentation = -1;
				}
				else
				{
					currentRow++;
					if (currentSymbol != ' ' && indentation == -1)
					{
						indentation = currentRow / indentSpacesCount;
					}
				}
				if (currentTokenType == LexemType.STRING)
				{
					if (!CheckForString())
					{
						return false;
					}
				}
				else
				{
					if (currentSymbol == '#')
					{
						while (i < text.Length)
						{
							currentSymbol = text[i++];
							if (currentSymbol == '\n' || currentSymbol == '\r')
							{
								break;
							}
						}
					}
					else
					{
						var lexemType = GetTokenType(currentSymbol);
						if (lexemType != currentTokenType)
						{
							if (!SwitchToken(lexemType))
							{
								return false;
							}
						}

						CheckForConst(lexemType);
					}
				}
			}

			if (!ProcessRemainingLexem())
			{
				return false;
			}

			tokens.Add(new Token(this, LexemType.END, currentLine, currentRow));

			return true;
		}

		void AddName(string name, int indentation)
		{
			int keywordIndex = keywords.IndexOf(name);
			if (keywordIndex != -1)
			{
				tokens.Add(new Token(this, LexemType.KEYWORD, currentLine, currentRow, indentation: indentation, keywordIndex: keywordIndex));
			}
			else
			{
				int nameIndex = names.IndexOf(name);
				if (nameIndex == -1)
				{
					names.Add(name);
					tokens.Add(new Token(this, LexemType.NAME, currentLine, currentRow, indentation: indentation, nameIndex: names.Count - 1));
				}
				else
				{
					tokens.Add(new Token(this, LexemType.NAME, currentLine, currentRow, indentation: indentation, nameIndex: nameIndex));
				}
			}
		}

		bool AddConst(LexemType type, string value, int indentation)
		{
			if (type == LexemType.NUMBER)
			{
				if (!double.TryParse(value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out double _))
				{
					if (!long.TryParse(value, out long _))
					{
						ReportError($"Invalid numeric value: {value}");

						return false;
					}
				}
			}
			int index = consts.IndexOf(value);
			if (index == -1)
			{
				consts.Add(value);
				tokens.Add(new Token(this, type, currentLine, currentRow, indentation: indentation, constIndex: consts.Count - 1));
			}
			else
			{
				tokens.Add(new Token(this, type, currentLine, currentRow, indentation: indentation, constIndex: index));
			}

			return true;
		}

		void ReportError(string error)
		{
			Console.Error.WriteLine($"Lexic error: {error} in {currentLine + 1}:{currentRow}");
		}

		LexemType GetTokenType(char c)
		{
			if (c == '\"' || c == '\'')
			{
				return LexemType.STRING;
			}
			else if (char.IsLetter(c) || c == '_')
			{
				return LexemType.NAME;
			}
			else if (char.IsDigit(c) || (currentTokenType == LexemType.NUMBER && c == '.'))
			{
				return LexemType.NUMBER;
			}
			else if (char.IsWhiteSpace(c))
			{
				return LexemType.SPACE;
			}
			else if (specialSymbols.Contains(c.ToString()))
			{
				return LexemType.SPECIAL;
			}
			else
			{
				ReportError($"Invalid symbol used: {c}");

				return LexemType.ERROR;
			}
		}

		internal void PrintInfo()
		{
			PrintTableLine(false);
			Console.Write("|");
			Console.Write(AddSpaces("Names:", 36));
			Console.WriteLine(AddSpaces("|", 36));
			PrintTableLine();

			foreach (var name in names)
			{
				int maxLen = 16;
				Console.Write('|');
				Console.Write(AddSpaces(name, maxLen - name.Length));
				Console.Write('|');
				string desc = "Name";
				Console.Write(AddSpaces(desc, 61 - desc.Length));
				Console.WriteLine('|');
			}

			PrintTableLine();
			Console.WriteLine();

			PrintTableLine(false);
			Console.Write("|");
			Console.Write(AddSpaces("Keywords:", 34));
			Console.WriteLine(AddSpaces("|", 34));
			PrintTableLine();

			for (int i = 0; i < tokens.Count; i++)
			{
				var token = tokens[i];
				if (token.type == LexemType.KEYWORD)
				{
					var keyword = keywords[token.keywordIndex];
					int maxLen = 16;
					Console.Write('|');
					Console.Write(AddSpaces(keyword, maxLen - keyword.Length));
					Console.Write('|');
					string desc = "Keyword";
					Console.Write(AddSpaces(desc, 61 - desc.Length));
					Console.WriteLine('|');
				}
			}

			PrintTableLine();
			Console.WriteLine();

			PrintTableLine(false);
			Console.Write("|");
			Console.Write(AddSpaces("Consts:", 36));
			Console.WriteLine(AddSpaces("|", 35));
			PrintTableLine();
			for (int i = 0; i < consts.Count; i++)
			{
				var cnst = consts[i];
				var token = tokens.Find(t => t.constIndex == i);
				if (token.type == LexemType.STRING)
				{
					cnst = $"\"{cnst}\"";
				}
				int maxLen = 16;
				Console.Write('|');
				Console.Write(AddSpaces(cnst, maxLen - cnst.Length));
				Console.Write('|');
				if (token.type == LexemType.NUMBER)
				{
					string desc = "Numeric constant";
					Console.Write(AddSpaces(desc, 61 - desc.Length));
				}
				else
				{
					string desc = "String constant";
					Console.Write(AddSpaces(desc, 61 - desc.Length));
				}
				Console.WriteLine('|');
			}

			PrintTableLine();
			Console.WriteLine();

			PrintTableLine(false);
			Console.Write("|");
			Console.Write(AddSpaces("Operations:", 34));
			Console.WriteLine(AddSpaces("|", 33));
			PrintTableLine();
			for (int i = 0; i < tokens.Count; i++)
			{
				var token = tokens[i];
				if (token.symbol != '\0')
				{
					int maxLen = 16;
					Console.Write('|');
					Console.Write(AddSpaces("" + token.symbol, maxLen - 1));
					Console.Write('|');
					string desc = $"{token.symbol} operation";
					Console.Write(AddSpaces(desc, 61 - desc.Length));
					Console.WriteLine('|');
				}
			}

			PrintTableLine();
			Console.WriteLine();
		}

		void PrintTableLine(bool withDelimeter = true)
		{
			if (withDelimeter)
			{
				Console.WriteLine("+================+=============================================================+");
			}
			else
			{
				Console.WriteLine("+==============================================================================+");
			}
		}

		string AddSpaces(string text, int count)
		{
			string s = "";

			for (int i = 0; i < count; i++)
			{
				s += ' ';
			}
			s += text;

			return s;
		}
	}
}