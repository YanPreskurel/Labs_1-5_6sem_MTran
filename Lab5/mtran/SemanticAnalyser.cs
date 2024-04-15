using System;
using System.Collections.Generic;

namespace mtran
{
	internal class FunctionPrototype
	{
		internal string className;
		internal string functionName;
		internal int argCount;
	}

	internal class SemanticAnalyser
	{
		readonly Ast ast;

		private static readonly List<FunctionPrototype> functions = new List<FunctionPrototype>()
		{
			new FunctionPrototype()
			{
				functionName = "randint",
				argCount = 2,
			},
			new FunctionPrototype()
			{
				className = "array",
				functionName = "append",
				argCount = 1,
			},
			new FunctionPrototype()
			{
				functionName = "print",
				argCount = 1,
			},
		};

		internal SemanticAnalyser(Ast ast)
		{
			this.ast = ast;
		}

		internal bool Analyse()
		{
			int prevIndentation = 0;
			bool expectIndent = false;

			foreach (var stat in ast.statements)
			{
				if (expectIndent)
				{
					if (stat.indentation != prevIndentation + 1)
					{
						ReportError($"Uxpected indentation in line {stat.line + 1}, expected {prevIndentation + 1}");

						return false;
					}
				}
				else
				{
					if (stat.indentation > prevIndentation)
					{
						ReportError($"Uxpected indentation in line {stat.line + 1}, expected {prevIndentation} or less");

						return false;
					}
				}

				if (stat.statementType == StatementType.STATEMENT_TYPE_IF ||
					stat.statementType == StatementType.STATEMENT_TYPE_ELSE ||
					stat.statementType == StatementType.STATEMENT_TYPE_FOR ||
					stat.statementType == StatementType.STATEMENT_TYPE_WHILE)
				{
					expectIndent = true;
				}
				else
				{
					expectIndent = false;
				}

				if (stat.statementType == StatementType.STATEMENT_TYPE_FUNCTION_CALL)
				{
					var functionCall = stat as FunctionCall;
					var funcStatement = functionCall.left;

					string objectName = null;
					string functionName = null;

					if (funcStatement.expressionType == Expressiontype.EXPRESSION_TYPE_NAME)
					{
						functionName = funcStatement.value;
					}
					else if (funcStatement.expressionType == Expressiontype.EXPRESSION_TYPE_DOT)
					{
						objectName = funcStatement.left.value;
						functionName = funcStatement.right.value;
					}
					else
					{
						ReportError($"Unexpected function type in {stat.line + 1}");

						return false;
					}

					var funcEntry = functions.Find(f => f.functionName == functionName);
					if (funcEntry != null)
					{
						int paramCount = functionCall.parameters.Count;
						if (funcEntry.argCount != paramCount)
						{
							ReportError($"Parameter count in function {functionName} does not match ({funcEntry.argCount} expected, {paramCount} provided) in line {stat.line + 1}");

							return false;
						}
					}
					else
					{
						if (objectName != null)
						{
							ReportError($"Method {objectName}.{functionName} was not found");
						}
						else
						{
							ReportError($"Function {functionName} was not found");
						}

						return false;
					}
				}

				prevIndentation = stat.indentation;
			}

			return true;
		}

		void ReportError(string error)
		{
			Console.Error.WriteLine($"Semantic error: {error}");
		}
	}
}