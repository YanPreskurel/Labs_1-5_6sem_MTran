using System;
using System.Collections.Generic;

namespace mtran
{
	internal class Parser
	{
		readonly List<string> names;
		readonly List<string> consts;
		readonly List<Token> tokens;

		int currentTokenIndex;

		Token CurrentToken
		{
			get => tokens[currentTokenIndex];
		}

		Token GetCurrentToken
		{
			get => tokens[currentTokenIndex++];
		}

		Ast ast;

		internal Ast Ast => ast;

		internal Parser(Lexer lexer)
		{
			names = lexer.Names;
			consts = lexer.Consts;
			tokens = lexer.Tokens;

			currentTokenIndex = 0;
		}

		internal bool Analyse()
		{
			var statements = new List<Statement>();
			ast = null;

			while (!IsEnd())
			{
				if (!IsStatement(out Statement statement))
				{
					return false;
				}
				statements.Add(statement);
			}

			ast = new Ast()
			{
				statements = statements
			};

			return true;
		}

		bool IsStatement(out Statement statement)
		{
			var token = CurrentToken;
			int savedIndex = currentTokenIndex;
			var indentation = CurrentToken.indentation;
			statement = null;

			if (IsImport(out Import import))
			{
				statement = import;
				statement.indentation = indentation;

				return true;
			}
			currentTokenIndex = savedIndex;
			if (IsAssignmentStatement(out Assignment ass))
			{
				statement = ass;
				statement.indentation = indentation;

				return true;
			}
			currentTokenIndex = savedIndex;
			if (IsCompoundStatement(out Assignment compoundAss))
			{
				statement = compoundAss;
				statement.indentation = indentation;

				return true;
			}
			currentTokenIndex = savedIndex;
			if (IsFunctionCall(out FunctionCall funcCall))
			{
				statement = funcCall;
				statement.indentation = indentation;

				return true;
			}
			currentTokenIndex = savedIndex;
			if (IsIfStatement(out If ifStatement))
			{
				statement = ifStatement;
				statement.indentation = indentation;

				return true;
			}
			currentTokenIndex = savedIndex;
			if (IsElseStatement(out Else elseStatement))
			{
				statement = elseStatement;
				statement.indentation = indentation;

				return true;
			}
			currentTokenIndex = savedIndex;
			if (IsElifStatement(out If elifStatement))
			{
				statement = elifStatement;
				statement.indentation = indentation;

				return true;
			}
			currentTokenIndex = savedIndex;
			if (IsForStatement(out For forStatement))
			{
				statement = forStatement;
				statement.indentation = indentation;

				return true;
			}
			currentTokenIndex = savedIndex;
			if (IsWhileStatement(out While whileStatement))
			{
				statement = whileStatement;
				statement.indentation = indentation;

				return true;
			}
			currentTokenIndex = savedIndex;
			ReportError(token, "Statement expected");

			return false;
		}

		bool IsImport(out Import import)
		{
			var token = CurrentToken;
			import = null;

			if (!IsKeyword("from"))
			{
				return false;
			}
			if (!IsName(out string libName))
			{
				ReportError(token, "Library name expected in import statement");

				return false;
			}
			if (!IsKeyword("import"))
			{
				return false;
			}
			if (!IsName(out string funName))
			{
				ReportError(token, "Function name expected in import statement");

				return false;
			}

			import = new Import(token.line, token.row)
			{
				library = libName,
				name = funName
			};

			return true;
		}

		bool IsAssignmentStatement(out Assignment ass)
		{
			var token = CurrentToken;
			ass = null;

			if (!IsExpression(out Expression left))
			{
				return false;
			}
			if (!IsSymbol('='))
			{
				return false;
			}
			if (!IsExpression(out Expression right))
			{
				ReportError(token, "Expression expected after \'=\'");

				return false;
			}

			ass = new Assignment(token.line, token.row)
			{
				left = left,
				right = right,
			};

			return true;
		}

		bool IsCompoundStatement(out Assignment ass)
		{
			var token = CurrentToken;
			ass = null;

			if (!IsDotOrIndexOrNameOrConst(out Expression left))
			{
				return false;
			}
			if (!IsCompoundOperation(out AssignmentType type))
			{
				return false;
			}
			if (!IsSymbol('='))
			{
				return false;
			}
			if (!IsExpression(out Expression right))
			{
				ReportError(token, "Expression expected after \'=\'");

				return false;
			}

			ass = new Assignment(token.line, token.row)
			{
				assignmentType = type,
				left = left,
				right = right
			};

			return true;
		}

		bool IsCompoundOperation(out AssignmentType type)
		{
			int savedIndex = currentTokenIndex;
			type = AssignmentType.ASSIGNMENT_TYPE_ASSIGNMENT;

			if (IsSymbol('-'))
			{
				type = AssignmentType.ASSIGNMENT_TYPE_SUB;

				return true;
			}
			currentTokenIndex = savedIndex;
			if (IsSymbol('+'))
			{
				type = AssignmentType.ASSIGNMENT_TYPE_ADD;

				return true;
			}
			currentTokenIndex = savedIndex;
			if (IsSymbol('/'))
			{
				type = AssignmentType.ASSIGNMENT_TYPE_DIV;

				return true;
			}
			currentTokenIndex = savedIndex;
			if (IsSymbol('*'))
			{
				type = AssignmentType.ASSIGNMENT_TYPE_MUL;

				return true;
			}
			currentTokenIndex = savedIndex;

			return false;
		}

		bool IsFunctionCall(out FunctionCall e)
		{
			var token = CurrentToken;
			e = null;

			if (!IsFunction(out Expression func))
			{
				return false;
			}
			if (!IsSymbol('('))
			{
				return false;
			}
			if (!IsParamList(out List<Expression> list))
			{
				return false;
			}
			if (!IsSymbol(')'))
			{
				ReportError(token, "\')\' expected after condition");

				return false;
			}

			e = new FunctionCall(token.line, token.row)
			{
				left = func,
				parameters = list
			};

			return true;
		}

		bool IsFunction(out Expression e)
		{
			int savedIndex = currentTokenIndex;
			e = null;

			if (IsMethod(out Expression meth))
			{
				e = meth;

				return true;
			}
			currentTokenIndex = savedIndex;
			var token = CurrentToken;
			if (IsName(out string fun))
			{
				e = new Expression(token.line, token.row)
				{
					value = fun,
					expressionType = Expressiontype.EXPRESSION_TYPE_NAME,
				};

				return true;
			}
			currentTokenIndex = savedIndex;

			return false;
		}

		bool IsMethod(out Expression e)
		{
			var token = CurrentToken;
			e = null;

			if (!IsName(out string obj))
			{
				return false;
			}
			if (!IsSymbol('.'))
			{
				return false;
			}
			if (!IsName(out string method))
			{
				return false;
			}

			e = new Expression(token.line, token.row)
			{
				expressionType = Expressiontype.EXPRESSION_TYPE_DOT,
				left = new Expression(token.line, token.row)
				{
					expressionType = Expressiontype.EXPRESSION_TYPE_NAME,
					value = obj
				},
				right = new Expression(token.line, token.row)
				{
					expressionType = Expressiontype.EXPRESSION_TYPE_NAME,
					value = method
				}
			};

			return true;
		}

		bool IsIfStatement(out If ifStatement)
		{
			var token = CurrentToken;
			ifStatement = null;

			if (!IsKeyword("if"))
			{
				return false;
			}
			if (!IsExpression(out Expression condition))
			{
				ReportError(token, "Condition expected after \"if\"");

				return false;
			}
			if (!IsSymbol(':'))
			{
				ReportError(token, "Colon expected after condition");

				return false;
			}

			ifStatement = new If(token.line, token.row)
			{
				condition = condition
			};

			return true;
		}

		bool IsElifStatement(out If ifStatement)
		{
			var token = CurrentToken;
			ifStatement = null;

			if (!IsKeyword("elif"))
			{
				return false;
			}
			if (!IsExpression(out Expression condition))
			{
				ReportError(token, "Condition expected after \"elif\"");

				return false;
			}
			if (!IsSymbol(':'))
			{
				ReportError(token, "Colon expected after condition");

				return false;
			}

			ifStatement = new If(token.line, token.row)
			{
				condition = condition,
				isElif = true
			};

			return true;
		}

		bool IsElseStatement(out Else elseStatement)
		{
			var token = CurrentToken;
			elseStatement = null;

			if (!IsKeyword("else"))
			{
				return false;
			}
			if (!IsSymbol(':'))
			{
				ReportError(token, "Colon expected in else branch");

				return false;
			}

			elseStatement = new Else(token.line, token.row);

			return true;
		}

		bool IsForStatement(out For forStatement)
		{
			var token = CurrentToken;
			forStatement = null;

			if (!IsKeyword("for"))
			{
				return false;
			}
			if (!IsExpression(out Expression variable))
			{
				ReportError(token, "Variable expected after \"for\"");

				return false;
			}
			if (!IsKeyword("in"))
			{
				return false;
			}
			if (!IsExpression(out Expression range))
			{
				ReportError(token, "Range expected after variable");

				return false;
			}
			if (!IsSymbol(':'))
			{
				ReportError(token, "Colon expected after condition");

				return false;
			}

			forStatement = new For(token.line, token.row)
			{
				expression = variable,
				range = range
			};

			return true;
		}

		bool IsWhileStatement(out While whileStatement)
		{
			var token = CurrentToken;
			whileStatement = null;

			if (!IsKeyword("while"))
			{
				return false;
			}
			if (!IsExpression(out Expression condition))
			{
				ReportError(token, "Variable expected after \"while\"");

				return false;
			}
			if (!IsSymbol(':'))
			{
				ReportError(token, "Colon expected after condition");

				return false;
			}

			whileStatement = new While(token.line, token.row)
			{
				condition = condition
			};

			return true;
		}

		bool IsParamList(out List<Expression> list)
		{
			var token = CurrentToken;
			list = null;

			if (!IsExpression(out Expression first))
			{
				return false;
			}
			list = new List<Expression>() { first };
			while (true)
			{
				if (CurrentToken.symbol == ')')
				{
					break;
				}
				if (!IsSymbol(','))
				{
					list = null;
					ReportError(token, "\',\' expected in parameter list");

					return false;
				}
				if (!IsExpression(out Expression following))
				{
					list = null;
					ReportError(token, "Expression expected in parameter list");

					return false;
				}
				list.Add(following);
			}

			return true;
		}

		bool IsExpression(out Expression e)
		{
			int savedIndex = currentTokenIndex;
			e = null;

			if (IsExpressionInBraces(out Expression braces))
			{
				e = braces;

				return true;
			}
			currentTokenIndex = savedIndex;
			if (IsFunctionCall(out FunctionCall funcCall))
			{
				e = funcCall;

				return true;
			}
			currentTokenIndex = savedIndex;
			if (IsBinaryExpression(out Expression binary))
			{
				e = binary;

				return true;
			}
			currentTokenIndex = savedIndex;
			if (IsRange(out Expression range))
			{
				e = range;

				return true;
			}
			currentTokenIndex = savedIndex;
			if (IsDotOrIndexOrNameOrConst(out Expression dotOrIndexOrConst))
			{
				e = dotOrIndexOrConst;

				return true;
			}
			currentTokenIndex = savedIndex;

			return false;
		}

		bool IsExpressionInBraces(out Expression e)
		{
			var token = CurrentToken;
			e = null;

			if (!IsSymbol('('))
			{
				return false;
			}
			if (!IsExpression(out Expression temp))
			{
				return false;
			}
			if (!IsSymbol(')'))
			{
				ReportError(token, "\')\' expected after expression");

				return false;
			}

			e = temp;

			return true;
		}

		bool IsBinaryExpression(out Expression binary)
		{
			binary = null;

			if (!IsDotOrIndexOrNameOrConst(out Expression left))
			{
				return false;
			}
			if (!IsBinaryOperation(out Expressiontype type))
			{
				return false;
			}
			if (!IsExpression(out Expression right))
			{
				return false;
			}

			binary = new Expression(left.line, left.row)
			{
				left = left,
				expressionType = type,
				right = right
			};

			return true;
		}

		bool IsBinaryOperation(out Expressiontype type)
		{
			int savedIndex = currentTokenIndex;
			type = Expressiontype.EXPRESSION_TYPE_NONE;

			if (IsSymbol('-'))
			{
				type = Expressiontype.EXPRESSION_TYPE_SUB;

				return true;
			}
			currentTokenIndex = savedIndex;
			if (IsSymbol('+'))
			{
				type = Expressiontype.EXPRESSION_TYPE_ADD;

				return true;
			}
			currentTokenIndex = savedIndex;
			if (IsSymbol('/'))
			{
				type = Expressiontype.EXPRESSION_TYPE_DIV;

				return true;
			}
			currentTokenIndex = savedIndex;
			if (IsSymbol('*'))
			{
				type = Expressiontype.EXPRESSION_TYPE_MUL;

				return true;
			}
			currentTokenIndex = savedIndex;
			if (IsSymbol('<'))
			{
				type = Expressiontype.EXPRESSION_TYPE_LESS;

				return true;
			}
			currentTokenIndex = savedIndex;
			if (IsSymbol('>'))
			{
				type = Expressiontype.EXPRESSION_TYPE_GREATER;

				return true;
			}
			currentTokenIndex = savedIndex;
			if (IsSymbol('|'))
			{
				type = Expressiontype.EXPRESSION_TYPE_OR;

				return true;
			}
			currentTokenIndex = savedIndex;
			if (IsSymbol('&'))
			{
				type = Expressiontype.EXPRESSION_TYPE_AND;

				return true;
			}
			currentTokenIndex = savedIndex;
			if (IsSymbol('^'))
			{
				type = Expressiontype.EXPRESSION_TYPE_XOR;

				return true;
			}
			currentTokenIndex = savedIndex;
			if (IsLessOrEquals())
			{
				type = Expressiontype.EXPRESSION_TYPE_LESS_OR_EQUALS;

				return true;
			}
			currentTokenIndex = savedIndex;
			if (IsGreaterOrEquals())
			{
				type = Expressiontype.EXPRESSION_TYPE_GREATER_OR_EQUALS;

				return true;
			}
			currentTokenIndex = savedIndex;
			if (IsEquals())
			{
				type = Expressiontype.EXPRESSION_TYPE_EQUALS;

				return true;
			}
			currentTokenIndex = savedIndex;

			return false;
		}

		bool IsLessOrEquals()
		{
			if (!IsSymbol('<'))
			{
				return false;
			}
			if (!IsSymbol('='))
			{
				return false;
			}

			return true;
		}

		bool IsGreaterOrEquals()
		{
			if (!IsSymbol('>'))
			{
				return false;
			}
			if (!IsSymbol('='))
			{
				return false;
			}

			return true;
		}

		bool IsEquals()
		{
			if (!IsSymbol('='))
			{
				return false;
			}
			if (!IsSymbol('='))
			{
				return false;
			}

			return true;
		}

		bool IsIndexExpression(out Expression e)
		{
			var token = CurrentToken;
			e = null;

			if (!IsName(out string left))
			{
				return false;
			}
			if (!IsSymbol('['))
			{
				return false;
			}
			if (!IsExpression(out Expression right))
			{
				return false;
			}
			if (!IsSymbol(']'))
			{
				ReportError(token, "\']\' expected after expression");

				return false;
			}

			e = new Expression(token.line, token.row)
			{
				expressionType = Expressiontype.EXPRESSION_TYPE_INDEX,
				left = new Expression(token.line, token.row)
				{
					expressionType = Expressiontype.EXPRESSION_TYPE_NAME,
					value = left
				},
				right = right
			};

			return true;
		}

		bool IsNameOrIndex(out Expression e)
		{
			int savedIndex = currentTokenIndex;
			e = null;

			if (IsIndexExpression(out Expression index))
			{
				e = index;

				return true;
			}
			currentTokenIndex = savedIndex;
			var token = CurrentToken;
			if (IsName(out string name))
			{
				e = new Expression(token.line, token.row)
				{
					expressionType = Expressiontype.EXPRESSION_TYPE_NAME,
					value = name
				};

				return true;
			}
			currentTokenIndex = savedIndex;

			return false;
		}

		bool IsDotExpression(out Expression e)
		{
			e = null;

			if (!IsNameOrIndex(out Expression left))
			{
				return false;
			}
			if (!IsSymbol('.'))
			{
				return false;
			}
			if (!IsDotOrIndexOrNameOrConst(out Expression right))
			{
				return false;
			}

			e = new Expression(left.line, left.row)
			{
				left = left,
				right = right
			};

			return true;
		}

		bool IsDotOrIndexOrNameOrConst(out Expression e)
		{
			int savedIndex = currentTokenIndex;
			e = null;

			if (IsDotExpression(out Expression dot))
			{
				e = dot;

				return true;
			}
			currentTokenIndex = savedIndex;
			if (IsIndexExpression(out Expression index))
			{
				e = index;

				return true;
			}
			currentTokenIndex = savedIndex;
			var token = CurrentToken;
			if (IsName(out string name))
			{
				e = new Expression(token.line, token.row)
				{
					expressionType = Expressiontype.EXPRESSION_TYPE_NAME,
					value = name
				};

				return true;
			}
			currentTokenIndex = savedIndex;
			if (IsConst(out Expression cnst))
			{
				e = cnst;

				return true;
			}
			currentTokenIndex = savedIndex;
			if (IsEmptyArray(out Expression arr))
			{
				e = arr;

				return true;
			}
			currentTokenIndex = savedIndex;

			return false;
		}

		bool IsRange(out Expression e)
		{
			var token = CurrentToken;
			e = null;

			if (!IsKeyword("range"))
			{
				return false;
			}
			if (!IsSymbol('('))
			{
				return false;
			}
			if (!IsExpression(out Expression expr))
			{
				return false;
			}
			if (!IsSymbol(')'))
			{
				ReportError(token, "\')\' expected after expression");

				return false;
			}

			e = new Expression(token.line, token.row)
			{
				expressionType = Expressiontype.EXPRESSION_TYPE_RANGE,
				left = expr
			};

			return true;
		}

		bool IsEmptyArray(out Expression e)
		{
			var token = CurrentToken;
			e = null;

			if (!IsSymbol('['))
			{
				return false;
			}
			if (!IsSymbol(']'))
			{
				ReportError(token, "\']\' expected after expression");

				return false;
			}

			e = new Expression(token.line, token.row)
			{
				expressionType = Expressiontype.EXPRESSION_TYPE_ARR
			};

			return true;
		}

		bool IsName(out string name)
		{
			if (CurrentToken.type != LexemType.NAME)
			{
				name = null;

				return false;
			}

			name = names[GetCurrentToken.nameIndex];

			return true;
		}

		bool IsConst(out Expression e)
		{
			var token = GetCurrentToken;
			e = null;

			if (token.type == LexemType.NUMBER)
			{
				e = new Expression(token.line, token.row)
				{
					expressionType = Expressiontype.EXPRESSION_TYPE_NUMBER,
					value = consts[token.constIndex]
				};

				return true;
			}
			else if (token.type == LexemType.STRING)
			{
				e = new Expression(token.line, token.row)
				{
					expressionType = Expressiontype.EXPRESSION_TYPE_STR,
					value = consts[token.constIndex]
				};

				return true;
			}

			return false;
		}

		bool IsKeyword(string keyword)
		{
			if (!GetCurrentToken.IsKeyword(keyword))
			{
				return false;
			}

			return true;
		}

		bool IsSymbol(char c)
		{
			var token = GetCurrentToken;

			if (token.type == LexemType.SPECIAL && token.symbol == c)
			{
				return true;
			}

			return false;
		}

		bool IsEnd()
		{
			return CurrentToken.type == LexemType.END;
		}

		void ReportError(Token token, string error)
		{
			Console.Error.WriteLine($"Syntax error: {error} in line {token.line + 1}");
		}

		internal void PrintInfo()
		{
			if (ast != null)
			{
				Console.WriteLine(ast);
			}
			else
			{
				Console.WriteLine("Ast was not generated!");
			}
		}
	}
}
