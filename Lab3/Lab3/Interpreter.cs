using System;
using System.Collections.Generic;

namespace Lab3
{
	internal class Interpreter
	{
		Ast ast;

		internal Interpreter(Ast ast)
		{
			this.ast = ast;
		}

		internal bool Analyse()
		{
			foreach (var stat in ast.statements)
			{
				switch (stat.statementType)
				{
					case StatementType.STATEMENT_TYPE_IMPORT:
						{
							bool result = InterpretImport(stat as Import);
							if (!result)
							{
								ReportError("", stat);

								return false;	
							}
						}
						break;
					case StatementType.STATEMENT_TYPE_ASSIGNMENT:
						{
							bool result = InterpretAssignment(stat as Assignment);
							if (!result)
							{
								ReportError("", stat);

								return false;
							}
						}
						break;
					case StatementType.STATEMENT_TYPE_IF:
						{
							bool result = InterpretIf(stat as If);
							if (!result)
							{
								ReportError("", stat);

								return false;
							}
						}
						break;
					case StatementType.STATEMENT_TYPE_ELSE:
						{
							bool result = InterpretElse(stat as Else);
							if (!result)
							{
								ReportError("", stat);

								return false;
							}
						}
						break;
					case StatementType.STATEMENT_TYPE_FOR:
						{
							bool result = InterpretFor(stat as For);
							if (!result)
							{
								ReportError("", stat);

								return false;
							}
						}
						break;
					case StatementType.STATEMENT_TYPE_WHILE:
						{
							bool result = InterpretWhile(stat as While);
							if (!result)
							{
								ReportError("", stat);

								return false;
							}
						}
						break;
					case StatementType.STATEMENT_TYPE_FUNCTION_CALL:
						{
							bool result = InterpretFunctionCall(stat as FunctionCall);
							if (!result)
							{
								ReportError("", stat);

								return false;
							}
						}
						break;
					default:
					case StatementType.STATEMENT_TYPE_EXPRESSION:
						return false;
				}
			}

			return true;
		}

		private bool InterpretImport(Import importStatement)
		{
			return false;
		}

		private bool InterpretAssignment(Assignment assignmentStatement)
		{
			return false;
		}

		private bool InterpretIf(If ifStatement)
		{
			return false;
		}

		private bool InterpretElse(Else elseStatement)
		{
			return false;
		}

		private bool InterpretFor(For forStatement)
		{
			return false;
		}

		private bool InterpretWhile(While whileStatement)
		{
			return false;
		}

		private bool InterpretFunctionCall(FunctionCall functionCallStatement)
		{
			return false;
		}

		void ReportError(string error, Statement stat)
		{
			Console.Error.WriteLine($"Interpreter error {error} in line {stat.line + 1} ({stat.statementType})");
		}
	}
}