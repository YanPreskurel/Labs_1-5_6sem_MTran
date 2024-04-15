using System;
using System.Collections.Generic;

namespace mtran
{
	internal enum RuntimeType
	{
		TYPE_NONE,
		TYPE_BOOL,
		TYPE_INTEGER,
		TYPE_REAL,
		TYPE_STRING,
		TYPE_ARRAY,
		TYPE_OBJECT,
		TYPE_RANGE,
	}

	internal class RangeValue
	{
		internal RuntimeValue rangeStart;
		internal RuntimeValue rangeEnd;
		internal RuntimeValue rangeStep;
	}

	internal class RuntimeValue
	{
		internal RuntimeType runtimeType = RuntimeType.TYPE_NONE;
		internal bool boolValue;
		internal long integerValue;
		internal double realValue;
		internal string stringValue;
		internal List<RuntimeValue> arrayValue;
		internal RangeValue rangeValue;

		internal bool IsBool() => runtimeType == RuntimeType.TYPE_BOOL;
		internal bool IsInt() => runtimeType == RuntimeType.TYPE_INTEGER;
		internal bool IsReal() => runtimeType == RuntimeType.TYPE_REAL;
		internal bool IsString() => runtimeType == RuntimeType.TYPE_STRING;
		internal bool IsArray() => runtimeType == RuntimeType.TYPE_ARRAY;
		internal bool IsRange() => runtimeType == RuntimeType.TYPE_RANGE;

		public override string ToString()
		{
			switch (runtimeType)
			{
				default:
					return Interpreter.ValueToString(this);
			}
		}
	}

	internal class Variable
	{
		internal string name;
		internal RuntimeValue value;

		public override string ToString()
		{
			if (value != null)
			{
				return $"{value.runtimeType} {name} = {value}";
			}
			else
			{
				return name;
			}
		}
	}

	internal class Interpreter
	{
		Ast ast;
		int statementIndex = 0;
		List<string> importedModules;
		List<Variable> variables;

		internal Interpreter(Ast ast)
		{
			this.ast = ast;
			this.importedModules = new List<string>();
			this.variables = new List<Variable>();
		}

		internal bool Run()
		{
			while (statementIndex != -1 && statementIndex < ast.statements.Count)
			{
				var stat = ast.statements[statementIndex];
				var result = InterpretStatement(stat);
				if (!result)
				{
					return result;
				}
			}

			return true;
		}

		private bool InterpretStatement(Statement stat)
		{
			//Console.WriteLine($"Interpeting {stat}");
			statementIndex++;
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
						var result = InterpretFunctionCall(stat as FunctionCall);
						if (result == null)
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

			return true;
		}

		private Variable GetVariable(string name)
		{
			var var = variables.Find(v => v.name == name);

			if (var == null)
			{
				var = new Variable()
				{
					name = name,
					value = null,
				};
				variables.Add(var);
			}

			return var;
		}

		private RuntimeValue GetValue(bool b)
		{
			return new RuntimeValue()
			{
				runtimeType = RuntimeType.TYPE_BOOL,
				boolValue = b,
			};
		}

		private RuntimeValue GetValue(long l)
		{
			return new RuntimeValue()
			{
				runtimeType = RuntimeType.TYPE_INTEGER,
				integerValue = l,
			};
		}

		private RuntimeValue GetValue(double d)
		{
			return new RuntimeValue()
			{
				runtimeType = RuntimeType.TYPE_REAL,
				realValue = d,
			};
		}

		private RuntimeValue GetValue(string s)
		{
			return new RuntimeValue()
			{
				runtimeType = RuntimeType.TYPE_STRING,
				stringValue = s,
			};
		}

		private RuntimeValue GetValue(List<RuntimeValue> values)
		{
			return new RuntimeValue()
			{
				runtimeType = RuntimeType.TYPE_ARRAY,
				arrayValue = values,
			};
		}

		private RuntimeValue GetValue(RangeValue range)
		{
			return new RuntimeValue()
			{
				runtimeType = RuntimeType.TYPE_RANGE,
				rangeValue = range,
			};
		}

		private RuntimeValue GetValue(Expression expr)
		{
			switch (expr.expressionType)
			{
				case Expressiontype.EXPRESSION_TYPE_NAME:
					var v = GetVariable(expr.value);
					if (v != null)
					{
						return v.value;
					}

					return null;
				case Expressiontype.EXPRESSION_TYPE_NUMBER:
					{
						if (long.TryParse(expr.value, out long l))
						{
							return GetValue(l);
						}
						else if (double.TryParse(expr.value, out double d))
						{
							return GetValue(d);
						}

						return null;
					}
				case Expressiontype.EXPRESSION_TYPE_STR:
					return GetValue(expr.value);
				case Expressiontype.EXPRESSION_TYPE_RANGE:
					var range = new RangeValue()
					{
						rangeStart = GetValue(0),
						rangeEnd = GetValue(expr.left),
						rangeStep = GetValue(1),
					};

					return GetValue(range);
				case Expressiontype.EXPRESSION_TYPE_ARR:
					return GetValue(new List<RuntimeValue>());
				case Expressiontype.EXPRESSION_TYPE_FUNC:
					return InterpretFunctionCall(expr as FunctionCall);
				case Expressiontype.EXPRESSION_TYPE_SUB:
					return CalcSub(GetValue(expr.left), GetValue(expr.right));
				case Expressiontype.EXPRESSION_TYPE_ADD:
					return CalcAdd(GetValue(expr.left), GetValue(expr.right));
				case Expressiontype.EXPRESSION_TYPE_MUL:
					return CalcMul(GetValue(expr.left), GetValue(expr.right));
				case Expressiontype.EXPRESSION_TYPE_DIV:
					return CalcDiv(GetValue(expr.left), GetValue(expr.right));
				case Expressiontype.EXPRESSION_TYPE_DOT:
					return CalcDot(GetValue(expr.left), GetValue(expr.right));
				case Expressiontype.EXPRESSION_TYPE_INDEX:
					return GetValueByIndex(GetValue(expr.left), GetValue(expr.right));
				case Expressiontype.EXPRESSION_TYPE_LESS:
				case Expressiontype.EXPRESSION_TYPE_LESS_OR_EQUALS:
				case Expressiontype.EXPRESSION_TYPE_GREATER:
				case Expressiontype.EXPRESSION_TYPE_GREATER_OR_EQUALS:
				case Expressiontype.EXPRESSION_TYPE_EQUALS:
					return CalcComp(GetValue(expr.left), GetValue(expr.right), expr.expressionType);
				case Expressiontype.EXPRESSION_TYPE_OR:
					return CalcOr(GetValue(expr.left), GetValue(expr.right));
				case Expressiontype.EXPRESSION_TYPE_AND:
					return CalcAnd(GetValue(expr.left), GetValue(expr.right));
				case Expressiontype.EXPRESSION_TYPE_XOR:
					return CalcXor(GetValue(expr.left), GetValue(expr.right));
				case Expressiontype.EXPRESSION_TYPE_NOT:
					return CalcNot(GetValue(expr.left));
				default:
					return null;
			}
		}

		private RuntimeValue CalcAdd(RuntimeValue left, RuntimeValue right)
		{
			if (left.IsInt() && right.IsInt())
			{
				return GetValue(left.integerValue + right.integerValue);
			}
			else if (left.IsReal() && right.IsReal())
			{
				return GetValue(left.realValue + right.realValue);
			}
			else if (left.IsInt() && right.IsReal())
			{
				return GetValue(left.integerValue + right.realValue);
			}
			else if (left.IsReal() && right.IsInt())
			{
				return GetValue(left.realValue + right.integerValue);
			}
			else if (left.IsString())
			{
				return GetValue(left.stringValue + ValueToString(right));
			}
			else if (left.IsArray() && right.IsArray())
			{
				var newArray = new List<RuntimeValue>();

				newArray.AddRange(left.arrayValue);
				newArray.AddRange(right.arrayValue);

				return GetValue(newArray);
			}
			else if (left.IsArray())
			{
				var arr = new List<RuntimeValue>(left.arrayValue);
				arr.Add(right);

				return GetValue(arr);
			}

			return null;
		}

		private RuntimeValue CalcSub(RuntimeValue left, RuntimeValue right)
		{
			if (left.IsInt() && right.IsInt())
			{
				return GetValue(left.integerValue - right.integerValue);
			}
			else if (left.IsReal() && right.IsReal())
			{
				return GetValue(left.realValue - right.realValue);
			}
			else if (left.IsInt() && right.IsReal())
			{
				return GetValue(left.integerValue - right.realValue);
			}
			else if (left.IsReal() && right.IsInt())
			{
				return GetValue(left.realValue - right.integerValue);
			}

			return null;
		}

		private RuntimeValue CalcMul(RuntimeValue left, RuntimeValue right)
		{
			if (left.IsInt() && right.IsInt())
			{
				return GetValue(left.integerValue * right.integerValue);
			}
			else if (left.IsReal() && right.IsReal())
			{
				return GetValue(left.realValue * right.realValue);
			}
			else if (left.IsInt() && right.IsReal())
			{
				return GetValue(left.integerValue * right.realValue);
			}
			else if (left.IsReal() && right.IsInt())
			{
				return GetValue(left.realValue * right.integerValue);
			}

			return null;
		}

		private RuntimeValue CalcDiv(RuntimeValue left, RuntimeValue right)
		{
			if (left.IsInt() && right.IsInt())
			{
				return GetValue(left.integerValue / right.integerValue);
			}
			else if (left.IsReal() && right.IsReal())
			{
				return GetValue(left.realValue / right.realValue);
			}
			else if (left.IsInt() && right.IsReal())
			{
				return GetValue(left.integerValue / right.realValue);
			}
			else if (left.IsReal() && right.IsInt())
			{
				return GetValue(left.realValue / right.integerValue);
			}

			return null;
		}

		private RuntimeValue CalcComp(RuntimeValue left, RuntimeValue right, Expressiontype comp)
		{
			switch (comp)
			{
				case Expressiontype.EXPRESSION_TYPE_LESS:
					{
						if (left.IsInt() && right.IsInt())
						{
							return GetValue(left.integerValue < right.integerValue);
						}
						else if (left.IsReal() && right.IsReal())
						{
							return GetValue(left.realValue < right.realValue);
						}
						else if (left.IsInt() && right.IsReal())
						{
							return GetValue(left.integerValue < right.realValue);
						}
						else if (left.IsReal() && right.IsInt())
						{
							return GetValue(left.realValue < right.integerValue);
						}
					}

					return null;
				case Expressiontype.EXPRESSION_TYPE_LESS_OR_EQUALS:
					{
						if (left.IsInt() && right.IsInt())
						{
							return GetValue(left.integerValue <= right.integerValue);
						}
						else if (left.IsReal() && right.IsReal())
						{
							return GetValue(left.realValue <= right.realValue);
						}
						else if (left.IsInt() && right.IsReal())
						{
							return GetValue(left.integerValue <= right.realValue);
						}
						else if (left.IsReal() && right.IsInt())
						{
							return GetValue(left.realValue <= right.integerValue);
						}
					}

					return null;
				case Expressiontype.EXPRESSION_TYPE_GREATER:
					{
						if (left.IsInt() && right.IsInt())
						{
							return GetValue(left.integerValue > right.integerValue);
						}
						else if (left.IsReal() && right.IsReal())
						{
							return GetValue(left.realValue > right.realValue);
						}
						else if (left.IsInt() && right.IsReal())
						{
							return GetValue(left.integerValue > right.realValue);
						}
						else if (left.IsReal() && right.IsInt())
						{
							return GetValue(left.realValue > right.integerValue);
						}
					}

					return null;
				case Expressiontype.EXPRESSION_TYPE_GREATER_OR_EQUALS:
					{
						if (left.IsInt() && right.IsInt())
						{
							return GetValue(left.integerValue >= right.integerValue);
						}
						else if (left.IsReal() && right.IsReal())
						{
							return GetValue(left.realValue >= right.realValue);
						}
						else if (left.IsInt() && right.IsReal())
						{
							return GetValue(left.integerValue >= right.realValue);
						}
						else if (left.IsReal() && right.IsInt())
						{
							return GetValue(left.realValue >= right.integerValue);
						}
					}

					return null;
				case Expressiontype.EXPRESSION_TYPE_EQUALS:
					{
						if (left.IsInt() && right.IsInt())
						{
							return GetValue(left.integerValue == right.integerValue);
						}
						else if (left.IsReal() && right.IsReal())
						{
							return GetValue(left.realValue == right.realValue);
						}
						else if (left.IsInt() && right.IsReal())
						{
							return GetValue(left.integerValue == right.realValue);
						}
						else if (left.IsReal() && right.IsInt())
						{
							return GetValue(left.realValue == right.integerValue);
						}
					}

					return null;
			}

			return null;
		}

		private RuntimeValue CalcInRange(RuntimeValue left, RuntimeValue right)
		{
			if (right.IsRange())
			{
				return CalcComp(left, right.rangeValue.rangeEnd, Expressiontype.EXPRESSION_TYPE_LESS_OR_EQUALS);
			}

			return null;
		}

		private RuntimeValue CalcDot(RuntimeValue left, RuntimeValue right)
		{
			return null;
		}

		private RuntimeValue CalcOr(RuntimeValue left, RuntimeValue right)
		{
			if (left.IsBool() && right.IsBool())
			{
				return GetValue(left.boolValue || right.boolValue);
			}

			return null;
		}

		private RuntimeValue CalcAnd(RuntimeValue left, RuntimeValue right)
		{
			if (left.IsBool() && right.IsBool())
			{
				return GetValue(left.boolValue && right.boolValue);
			}

			return null;
		}

		private RuntimeValue CalcXor(RuntimeValue left, RuntimeValue right)
		{
			if (left.IsBool() && right.IsBool())
			{
				return GetValue(left.boolValue ^ right.boolValue);
			}

			return null;
		}

		private RuntimeValue CalcNot(RuntimeValue left)
		{
			if (left.IsBool())
			{
				return GetValue(!left.boolValue);
			}

			return null;
		}

		private bool SetValue(Expression expr, RuntimeValue value)
		{
			switch (expr.expressionType)
			{
				case Expressiontype.EXPRESSION_TYPE_NAME:
					{
						var name = expr.value;
						var var = GetVariable(name);
						var.value = value;
					}

					return true;
				case Expressiontype.EXPRESSION_TYPE_INDEX:
					{
						if (!SetValueByIndex(GetValue(expr.left), GetValue(expr.right), value))
						{
							// report

							return false;
						}

						return true;
					}
				case Expressiontype.EXPRESSION_TYPE_DOT:
					return false;
				default:
					return false;
			}
		}

		private bool IsTrue(RuntimeValue value)
		{
			if (value == null)
			{
				return true;
			}

			switch (value.runtimeType)
			{
				case RuntimeType.TYPE_NONE:
					return false;
				case RuntimeType.TYPE_BOOL:
					return value.boolValue;
				case RuntimeType.TYPE_INTEGER:
					return value.integerValue > 0;
				case RuntimeType.TYPE_REAL:
					return value.realValue > 0.0d;
				case RuntimeType.TYPE_STRING:
					return value.stringValue.Length > 0;
				case RuntimeType.TYPE_ARRAY:
					return value.arrayValue.Count > 0;
				case RuntimeType.TYPE_OBJECT:
					return true;
				default:
					return false;
			}
		}

		private int GetNextStatementWithThisIndentationOrLess(int n)
		{
			for (int i = statementIndex + 1; i < ast.statements.Count; i++)
			{
				var stat = ast.statements[i];
				if (stat.indentation <= n)
				{
					return i;
				}
			}

			return -1;
		}

		private int GetNextLastStatementWithThisIndentation(int n)
		{
			for (int i = statementIndex + 1; i < ast.statements.Count; i++)
			{
				var stat = ast.statements[i];
				if (stat.indentation < n)
				{
					return i - 1;
				}
			}

			return -1;
		}

		private bool InterpretImport(Import importStatement)
		{
			var module = importStatement.library;

			if (!importedModules.Contains(module))
			{
				importedModules.Add(module);
			}

			return true;
		}

		private bool InterpretAssignment(Assignment assignmentStatement)
		{
			switch (assignmentStatement.assignmentType)
			{
				case AssignmentType.ASSIGNMENT_TYPE_ASSIGNMENT:
					{
						var rhs = GetValue(assignmentStatement.right);

						if (!SetValue(assignmentStatement.left, rhs))
						{
							// report

							return false;
						}
					}
					break;
				case AssignmentType.ASSIGNMENT_TYPE_ADD:
					{
						var rhs = CalcAdd(GetValue(assignmentStatement.left), GetValue(assignmentStatement.right));
						if (!SetValue(assignmentStatement.left, rhs))
						{
							// report

							return false;
						}
					}
					break;
				case AssignmentType.ASSIGNMENT_TYPE_SUB:
					{
						var rhs = CalcSub(GetValue(assignmentStatement.left), GetValue(assignmentStatement.right));
						if (!SetValue(assignmentStatement.left, rhs))
						{
							// report

							return false;
						}
					}
					break;
				case AssignmentType.ASSIGNMENT_TYPE_MUL:
					{
						var rhs = CalcMul(GetValue(assignmentStatement.left), GetValue(assignmentStatement.right));
						if (!SetValue(assignmentStatement.left, rhs))
						{
							// report

							return false;
						}
					}
					break;
				case AssignmentType.ASSIGNMENT_TYPE_DIV:
					{
						var rhs = CalcDiv(GetValue(assignmentStatement.left), GetValue(assignmentStatement.right));
						if (!SetValue(assignmentStatement.left, rhs))
						{
							// report

							return false;
						}
					}
					break;
				default:
					return false;
			}

			return true;
		}

		private bool InterpretIf(If ifStatement)
		{
			var condition = GetValue(ifStatement.condition);
			if (IsTrue(condition))
			{
				var nextStatementAfterIfIndex = GetNextStatementWithThisIndentationOrLess(ifStatement.indentation);
				var statements = ast.statements.GetRange(statementIndex, nextStatementAfterIfIndex - statementIndex);
				statements.ForEach((stat) =>
				{
					if (stat.indentation == ifStatement.indentation + 1)
					{
						bool result = InterpretStatement(stat);
						if (!result)
						{
							// TODO
						}
					}
				});
			}
			else
			{
				statementIndex = GetNextStatementWithThisIndentationOrLess(ifStatement.indentation);
				var nextStatementAfterIf = ast.statements[statementIndex];
				if (nextStatementAfterIf.statementType == StatementType.STATEMENT_TYPE_IF)
				{
					var ifStat = nextStatementAfterIf as If;
					if (ifStat.isElif)
					{
						bool result = InterpretStatement(ifStat);
						if (!result)
						{
							// TODO
						}
						statementIndex = GetNextStatementWithThisIndentationOrLess(ifStatement.indentation);
					}
				}
			}

			return true;
		}

		private bool InterpretElse(Else elseStatement)
		{
			var nextStatementAfterIfIndex = GetNextStatementWithThisIndentationOrLess(elseStatement.indentation);
			for (int i = statementIndex + 1; i < nextStatementAfterIfIndex; i++)
			{
				var stat = ast.statements[i];
				if (stat.indentation == elseStatement.indentation + 1)
				{
					statementIndex = i;
					bool result = InterpretStatement(stat);
					if (!result)
					{
						// TODO
					}
				}
			}

			return true;
		}

		private bool InterpretFor(For forStatement)
		{
			var range = GetValue(forStatement.range);
			var iterator = GetVariable(forStatement.expression.value);
			iterator.value = range.rangeValue.rangeStart;
			var nextStatementAfterForIndex = GetNextLastStatementWithThisIndentation(forStatement.indentation + 1);
			var statements = ast.statements.GetRange(statementIndex, nextStatementAfterForIndex - statementIndex + 1);
			int firstStatIndex = statementIndex;
			while (IsTrue(CalcInRange(iterator.value, range)))
			{
				statementIndex = firstStatIndex;
				statements.ForEach((stat) =>
				{
					if (stat.indentation == forStatement.indentation + 1)
					{
						bool result = InterpretStatement(stat);
						if (!result)
						{
							// TODO
						}
					}
				});
				iterator.value = CalcAdd(iterator.value, range.rangeValue.rangeStep);
			}

			statementIndex = nextStatementAfterForIndex + 1;

			return true;
		}

		private bool InterpretWhile(While whileStatement)
		{
			var nextStatementAfterWhileIndex = GetNextLastStatementWithThisIndentation(whileStatement.indentation + 1);
			var statements = ast.statements.GetRange(statementIndex, nextStatementAfterWhileIndex - statementIndex + 1);
			int firstStatIndex = statementIndex;
			while (IsTrue(GetValue(whileStatement.condition)))
			{
				statementIndex = firstStatIndex;
				statements.ForEach((stat) =>
				{
					if (stat.indentation == whileStatement.indentation + 1)
					{
						bool result = InterpretStatement(stat);
						if (!result)
						{
							// TODO
						}
					}
				});
			}

			statementIndex = nextStatementAfterWhileIndex + 1;

			return true;
		}

		private static RuntimeValue GetValueByIndex(RuntimeValue arrayValue, RuntimeValue indexValue)
		{
			if (arrayValue.runtimeType != RuntimeType.TYPE_ARRAY)
			{
				return null;
			}

			var array = arrayValue.arrayValue;
			var index = (int)indexValue.integerValue;

			if (index < 0 && index >= array.Count)
			{
				return null;
			}

			return array[index];
		}

		private static bool SetValueByIndex(RuntimeValue arrayValue, RuntimeValue indexValue, RuntimeValue value)
		{
			if (arrayValue.runtimeType != RuntimeType.TYPE_ARRAY)
			{
				return false;
			}

			var array = arrayValue.arrayValue;
			var index = (int)indexValue.integerValue;

			if (index < 0)
			{
				return false;
			}
			else if (index >= array.Count)
			{
				while (array.Count < index + 1)
				{
					array.Add(new RuntimeValue());
				}
			}

			array[index] = value;

			return true;
		}

		internal static string ValueToString(RuntimeValue value)
		{
			switch (value.runtimeType)
			{
				case RuntimeType.TYPE_NONE:
					return null;
				case RuntimeType.TYPE_BOOL:
					return value.boolValue.ToString().ToLower();
				case RuntimeType.TYPE_INTEGER:
					return value.integerValue.ToString();
				case RuntimeType.TYPE_REAL:
					return value.realValue.ToString();
				case RuntimeType.TYPE_STRING:
					return value.stringValue;
				case RuntimeType.TYPE_ARRAY:
					{
						string text = "";

						text += "[";

						for (int i = 0; i < value.arrayValue.Count; i++)
						{
							if (i > 0)
							{
								text += ", ";
							}
							text += ValueToString(value.arrayValue[i]);
						}

						text += "]";

						return text;
					}
				case RuntimeType.TYPE_OBJECT:
					{
						string text = "";

						text += "???";

						return text;
					}
				case RuntimeType.TYPE_RANGE:
					{
						return $"range({value.rangeValue.rangeStart}-{value.rangeValue.rangeEnd}:{value.rangeValue.rangeStep})";
					}
				default:
					return null;
			}
		}

		private RuntimeValue InterpretFunctionCall(FunctionCall functionCallStatement)
		{
			//return true;

			var name = functionCallStatement.left.value;

			if (name == null)
			{
				return null;
			}

			switch (name)
			{
				case "len":
				{
					var arr = GetValue(functionCallStatement.parameters[0]);
					if (!arr.IsArray())
					{
						// report

						return null;
					}

					return GetValue(arr.arrayValue.Count);
				}
				case "randint":
				{
					return null;
				}
				case "append":
				{
					return null;
				}
				case "print":
				{
					Console.WriteLine(ValueToString(GetValue(functionCallStatement.parameters[0])));
				}
				break;
			}

			return GetValue(true);
		}

		void ReportError(string error, Statement stat)
		{
			Console.Error.WriteLine($"Interpreter error {error} in line {stat.line + 1} ({stat.statementType})");
		}
	}
}
