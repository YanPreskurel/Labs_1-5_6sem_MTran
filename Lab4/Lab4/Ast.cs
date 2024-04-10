using System.Collections.Generic;

namespace Lab4
{
	internal enum StatementType
	{
		STATEMENT_TYPE_IMPORT,
		STATEMENT_TYPE_ASSIGNMENT,
		STATEMENT_TYPE_IF,
		STATEMENT_TYPE_ELSE,
		STATEMENT_TYPE_FOR,
		STATEMENT_TYPE_WHILE,
		STATEMENT_TYPE_EXPRESSION,
		STATEMENT_TYPE_FUNCTION_CALL,
	}

	internal enum AssignmentType
	{
		ASSIGNMENT_TYPE_ASSIGNMENT,
		ASSIGNMENT_TYPE_ADD,
		ASSIGNMENT_TYPE_SUB,
		ASSIGNMENT_TYPE_MUL,
		ASSIGNMENT_TYPE_DIV,
	}

	internal enum Expressiontype
	{
		EXPRESSION_TYPE_NONE,
		EXPRESSION_TYPE_NAME,
		EXPRESSION_TYPE_NUMBER,
		EXPRESSION_TYPE_STR,
		EXPRESSION_TYPE_RANGE,
		EXPRESSION_TYPE_ARR,
		EXPRESSION_TYPE_FUNC,
		EXPRESSION_TYPE_SUB,
		EXPRESSION_TYPE_ADD,
		EXPRESSION_TYPE_MUL,
		EXPRESSION_TYPE_DIV,
		EXPRESSION_TYPE_DOT,
		EXPRESSION_TYPE_INDEX,
		EXPRESSION_TYPE_LESS,
		EXPRESSION_TYPE_LESS_OR_EQUALS,
		EXPRESSION_TYPE_GREATER,
		EXPRESSION_TYPE_GREATER_OR_EQUALS,
		EXPRESSION_TYPE_EQUALS,
		EXPRESSION_TYPE_OR,
		EXPRESSION_TYPE_AND,
		EXPRESSION_TYPE_XOR,
		EXPRESSION_TYPE_NOT,
	}

	internal class Ast
	{
		internal List<Statement> statements;

		public override string ToString()
		{
			string result = "";

			foreach (var stat in statements)
			{
				for (int i = 0; i < stat.indentation; i++)
				{
					result += "    ";
				}
				result += stat.ToString() + '\n';
			}

			return result;
		}
	}

	internal class Statement
	{
		internal int line;
		internal int row;
		internal int indentation;
		internal StatementType statementType;

		public Statement(int line, int row, StatementType statementType)
		{
			this.line = line;
			this.row = row;
			this.statementType = statementType;
		}

        public override string ToString()
		{
            return $"{statementType.ToString()}({line}:{row})";
		}
	}

	internal class Import : Statement
	{
		internal string library;
		internal string name;

		public Import(int line, int row) : base(line, row, StatementType.STATEMENT_TYPE_IMPORT) { }

		public override string ToString()
		{
			return $"Importing {name} from {library} library";
		}
	}

	internal class Assignment : Statement
	{
		internal Expression left;
		internal Expression right;
		internal AssignmentType type;

		public Assignment(int line, int row) : base(line, row, StatementType.STATEMENT_TYPE_ASSIGNMENT) { }

		public override string ToString()
		{
			switch (type)
			{
				case AssignmentType.ASSIGNMENT_TYPE_ASSIGNMENT:
					return $"Assigning {left} = {right}";
				case AssignmentType.ASSIGNMENT_TYPE_ADD:
					return $"Assigning {left} += {right}";
				case AssignmentType.ASSIGNMENT_TYPE_SUB:
					return $"Assigning {left} -= {right}";
				case AssignmentType.ASSIGNMENT_TYPE_MUL:
					return $"Assigning {left} *= {right}";
				case AssignmentType.ASSIGNMENT_TYPE_DIV:
					return $"Assigning {left} /= {right}";
				default:
					return "???error???";
			}
		}
	}

	internal class If : Statement
	{
		internal Expression condition;
		internal bool isElif = false;

		public If(int line, int row) : base(line, row, StatementType.STATEMENT_TYPE_IF) { }

		public override string ToString()
		{
			return $"If statement: {condition}";
		}
	}

	internal class Else : Statement
	{
		public Else(int line, int row) : base(line, row, StatementType.STATEMENT_TYPE_ELSE) { }

		public override string ToString()
		{
			return $"Else branch:";
		}
	}

	internal class For : Statement
	{
		internal Expression variable;
		internal Expression range;

		public For(int line, int row) : base(line, row, StatementType.STATEMENT_TYPE_FOR) { }

		public override string ToString()
		{
			return $"For statement: {variable} in {range}";
		}
	}

	internal class While : Statement
	{
		internal Expression condition;

		public While(int line, int row) : base(line, row, StatementType.STATEMENT_TYPE_WHILE) { }

		public override string ToString()
		{
			return $"while statement: {condition}";
		}
	}

	internal class Expression : Statement
	{
		internal Expressiontype expressionType;
		internal Expression left;
		internal Expression right;
		internal string value;

		public Expression(int line, int row, StatementType statementType = StatementType.STATEMENT_TYPE_EXPRESSION) : base(line, row, statementType) { }

		public override string ToString()
		{
			if (expressionType == Expressiontype.EXPRESSION_TYPE_SUB && right == null)
			{
				return $"-{left}";
			}

			switch (expressionType)
			{
				case Expressiontype.EXPRESSION_TYPE_NAME:
					return $"{value}";
				case Expressiontype.EXPRESSION_TYPE_NUMBER:
					return $"{value}";
				case Expressiontype.EXPRESSION_TYPE_STR:
					return $"\'{value}\'";
				case Expressiontype.EXPRESSION_TYPE_RANGE:
					return $"range({left})";
				case Expressiontype.EXPRESSION_TYPE_ARR:
					return $"[]"; // TODO
				case Expressiontype.EXPRESSION_TYPE_SUB:
					return $"{left} - {right}";
				case Expressiontype.EXPRESSION_TYPE_ADD:
					return $"{left} + {right}";
				case Expressiontype.EXPRESSION_TYPE_MUL:
					return $"{left} * {right}";
				case Expressiontype.EXPRESSION_TYPE_DIV:
					return $"{left} / {right}";
				case Expressiontype.EXPRESSION_TYPE_DOT:
					return $"{left}.{right}";
				case Expressiontype.EXPRESSION_TYPE_INDEX:
					return $"{left}[{right}]";
				case Expressiontype.EXPRESSION_TYPE_LESS:
					return $"{left} < {right}";
				case Expressiontype.EXPRESSION_TYPE_LESS_OR_EQUALS:
					return $"{left} <= {right}";
				case Expressiontype.EXPRESSION_TYPE_GREATER:
					return $"{left} > {right}";
				case Expressiontype.EXPRESSION_TYPE_GREATER_OR_EQUALS:
					return $"{left} >= {right}";
				case Expressiontype.EXPRESSION_TYPE_EQUALS:
					return $"{left} == {right}";
				case Expressiontype.EXPRESSION_TYPE_OR:
					return $"{left} | {right}";
				case Expressiontype.EXPRESSION_TYPE_AND:
					return $"{left} & {right}";
				case Expressiontype.EXPRESSION_TYPE_XOR:
					return $"{left} ^ {right}";
				default:
					return "???error???";
			}
		}
	}

	internal class FunctionCall : Expression
	{
		internal List<Expression> parameters;

		public FunctionCall(int line, int row) : base(line, row, StatementType.STATEMENT_TYPE_FUNCTION_CALL) { }

		public override string ToString()
		{
			string args = "";

			for (int i = 0; i < parameters.Count; i++)
			{
				if (i > 0)
				{
					args += ", ";
				}
				var a = parameters[i];
				args += a.ToString();
			}

			return $"calling function {left} with ({args})";
		}
	}
}