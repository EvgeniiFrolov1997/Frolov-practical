using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace CalculatorMVC.Model
{
    /// <summary>
    /// Ошибка разбора или вычисления математического выражения.
    /// </summary>
    public class ExpressionException : Exception
    {
        public ExpressionException(string message) : base(message)
        {
        }
    }

    internal enum TokenType
    {
        Number,
        Function,
        Operator,
        LeftParen,
        RightParen
    }

    /// <summary>
    /// Лексема выражения.
    /// </summary>
    internal sealed class Token
    {
        public TokenType Type { get; private set; }
        public string Text { get; private set; }
        public double Value { get; private set; }

        public Token(TokenType type, string text)
        {
            Type = type;
            Text = text;
        }

        public Token(double value)
        {
            Type = TokenType.Number;
            Value = value;
            Text = value.ToString(CultureInfo.InvariantCulture);
        }
    }

    /// <summary>
    /// Вычислитель математических выражений.
    /// Порядок действий обеспечивается алгоритмом сортировочной станции
    /// (shunting-yard, Э. Дейкстра): инфиксная запись преобразуется в обратную
    /// польскую (ОПЗ), после чего ОПЗ вычисляется стековой машиной.
    /// </summary>
    public sealed class ExpressionEvaluator
    {
        private static readonly HashSet<string> KnownFunctions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "sin", "cos", "tan", "tg", "ctg", "cot",
            "asin", "arcsin", "acos", "arccos", "atan", "arctg",
            "ln", "log", "lg", "sqrt", "exp", "abs", "sign", "round"
        };

        /// <summary>
        /// Режим измерения углов: true — градусы, false — радианы.
        /// </summary>
        public bool UseDegrees { get; set; }

        public ExpressionEvaluator()
        {
            UseDegrees = true;
        }

        /// <summary>
        /// Вычисляет значение выражения, записанного в инфиксной форме.
        /// </summary>
        public double Evaluate(string expression)
        {
            if (string.IsNullOrWhiteSpace(expression))
            {
                throw new ExpressionException("Выражение не задано");
            }

            List<Token> tokens = Tokenize(expression);
            tokens = InsertImplicitMultiplication(tokens);
            List<Token> rpn = ConvertToRpn(tokens);
            return EvaluateRpn(rpn);
        }

        #region Лексический анализ

        private static List<Token> Tokenize(string source)
        {
            List<Token> tokens = new List<Token>();
            int i = 0;

            while (i < source.Length)
            {
                char c = source[i];

                if (char.IsWhiteSpace(c))
                {
                    i++;
                    continue;
                }

                // Число (разделителем дробной части считаем и точку, и запятую)
                if (char.IsDigit(c) || c == '.' || c == ',')
                {
                    StringBuilder number = new StringBuilder();
                    bool separatorUsed = false;

                    while (i < source.Length && (char.IsDigit(source[i]) || source[i] == '.' || source[i] == ','))
                    {
                        if (source[i] == '.' || source[i] == ',')
                        {
                            if (separatorUsed)
                            {
                                throw new ExpressionException("В числе больше одной десятичной точки");
                            }

                            separatorUsed = true;
                            number.Append('.');
                        }
                        else
                        {
                            number.Append(source[i]);
                        }

                        i++;
                    }

                    double value;
                    if (!double.TryParse(number.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                    {
                        throw new ExpressionException("Некорректное число: " + number);
                    }

                    tokens.Add(new Token(value));
                    continue;
                }

                // Идентификатор: имя функции или константа
                if (char.IsLetter(c))
                {
                    StringBuilder name = new StringBuilder();
                    while (i < source.Length && (char.IsLetter(source[i]) || char.IsDigit(source[i])))
                    {
                        name.Append(source[i]);
                        i++;
                    }

                    string identifier = name.ToString().ToLowerInvariant();

                    if (identifier == "pi" || identifier == "\u03c0")
                    {
                        tokens.Add(new Token(Math.PI));
                    }
                    else if (identifier == "e")
                    {
                        tokens.Add(new Token(Math.E));
                    }
                    else if (KnownFunctions.Contains(identifier))
                    {
                        tokens.Add(new Token(TokenType.Function, identifier));
                    }
                    else
                    {
                        throw new ExpressionException("Неизвестная функция или константа: " + identifier);
                    }

                    continue;
                }

                switch (c)
                {
                    case '(':
                        tokens.Add(new Token(TokenType.LeftParen, "("));
                        break;
                    case ')':
                        tokens.Add(new Token(TokenType.RightParen, ")"));
                        break;
                    case '\u221a': // знак корня на кнопке интерфейса
                        tokens.Add(new Token(TokenType.Function, "sqrt"));
                        break;
                    case '\u00d7':
                        tokens.Add(new Token(TokenType.Operator, "*"));
                        break;
                    case '\u00f7':
                        tokens.Add(new Token(TokenType.Operator, "/"));
                        break;
                    case '+':
                    case '-':
                    case '*':
                    case '/':
                    case '^':
                    case '%':
                    case '!':
                        tokens.Add(new Token(TokenType.Operator, c.ToString()));
                        break;
                    default:
                        throw new ExpressionException("Недопустимый символ: " + c);
                }

                i++;
            }

            if (tokens.Count == 0)
            {
                throw new ExpressionException("Выражение не задано");
            }

            return tokens;
        }

        /// <summary>
        /// Вставляет знак умножения там, где он опущен: 2pi, 3(4+5), (1+2)(3+4), 2sin(30).
        /// </summary>
        private static List<Token> InsertImplicitMultiplication(List<Token> tokens)
        {
            List<Token> result = new List<Token>();

            for (int i = 0; i < tokens.Count; i++)
            {
                Token current = tokens[i];

                if (i > 0)
                {
                    Token previous = tokens[i - 1];

                    bool previousIsValue = previous.Type == TokenType.Number
                                           || previous.Type == TokenType.RightParen
                                           || (previous.Type == TokenType.Operator && previous.Text == "!");

                    bool currentStartsValue = current.Type == TokenType.Number
                                              || current.Type == TokenType.Function
                                              || current.Type == TokenType.LeftParen;

                    if (previousIsValue && currentStartsValue)
                    {
                        result.Add(new Token(TokenType.Operator, "*"));
                    }
                }

                result.Add(current);
            }

            return result;
        }

        #endregion

        #region Преобразование в ОПЗ

        private static int GetPrecedence(string op)
        {
            switch (op)
            {
                case "+":
                case "-":
                    return 1;
                case "*":
                case "/":
                case "%":
                    return 2;
                case "u-":
                case "u+":
                    return 4;
                case "^":
                    return 5;
                case "!":
                    return 6;
                default:
                    throw new ExpressionException("Неизвестная операция: " + op);
            }
        }

        private static bool IsLeftAssociative(string op)
        {
            return op != "^" && op != "u-" && op != "u+";
        }

        private static bool IsUnaryPrefix(string op)
        {
            return op == "u-" || op == "u+";
        }

        private static List<Token> ConvertToRpn(List<Token> tokens)
        {
            List<Token> output = new List<Token>();
            Stack<Token> stack = new Stack<Token>();
            Token previous = null;

            foreach (Token raw in tokens)
            {
                Token token = raw;

                // Определяем, является ли + или - унарным знаком числа
                if (token.Type == TokenType.Operator && (token.Text == "+" || token.Text == "-"))
                {
                    bool unary = previous == null
                                 || previous.Type == TokenType.LeftParen
                                 || previous.Type == TokenType.Function
                                 || (previous.Type == TokenType.Operator && previous.Text != "!");

                    if (unary)
                    {
                        token = new Token(TokenType.Operator, token.Text == "-" ? "u-" : "u+");
                    }
                }

                switch (token.Type)
                {
                    case TokenType.Number:
                        output.Add(token);
                        break;

                    case TokenType.Function:
                        stack.Push(token);
                        break;

                    case TokenType.Operator:
                        if (IsUnaryPrefix(token.Text))
                        {
                            // Префиксная операция ничего не выталкивает: её операнд ещё не прочитан
                            stack.Push(token);
                            break;
                        }

                        while (stack.Count > 0)
                        {
                            Token top = stack.Peek();

                            bool popTop = top.Type == TokenType.Function
                                          || (top.Type == TokenType.Operator
                                              && (GetPrecedence(top.Text) > GetPrecedence(token.Text)
                                                  || (GetPrecedence(top.Text) == GetPrecedence(token.Text)
                                                      && IsLeftAssociative(token.Text))));

                            if (!popTop)
                            {
                                break;
                            }

                            output.Add(stack.Pop());
                        }

                        stack.Push(token);
                        break;

                    case TokenType.LeftParen:
                        stack.Push(token);
                        break;

                    case TokenType.RightParen:
                        while (stack.Count > 0 && stack.Peek().Type != TokenType.LeftParen)
                        {
                            output.Add(stack.Pop());
                        }

                        if (stack.Count == 0)
                        {
                            throw new ExpressionException("Непарная закрывающая скобка");
                        }

                        stack.Pop();

                        if (stack.Count > 0 && stack.Peek().Type == TokenType.Function)
                        {
                            output.Add(stack.Pop());
                        }

                        break;
                }

                previous = token;
            }

            while (stack.Count > 0)
            {
                Token top = stack.Pop();

                if (top.Type == TokenType.LeftParen)
                {
                    throw new ExpressionException("Непарная открывающая скобка");
                }

                output.Add(top);
            }

            return output;
        }

        #endregion

        #region Вычисление ОПЗ

        private double EvaluateRpn(List<Token> rpn)
        {
            Stack<double> stack = new Stack<double>();

            foreach (Token token in rpn)
            {
                if (token.Type == TokenType.Number)
                {
                    stack.Push(token.Value);
                    continue;
                }

                if (token.Type == TokenType.Function)
                {
                    stack.Push(ApplyFunction(token.Text, Pop(stack)));
                    continue;
                }

                switch (token.Text)
                {
                    case "u-":
                        stack.Push(-Pop(stack));
                        break;
                    case "u+":
                        stack.Push(Pop(stack));
                        break;
                    case "!":
                        stack.Push(Factorial(Pop(stack)));
                        break;
                    default:
                        double right = Pop(stack);
                        double left = Pop(stack);
                        stack.Push(ApplyBinary(token.Text, left, right));
                        break;
                }
            }

            if (stack.Count != 1)
            {
                throw new ExpressionException("Некорректное выражение");
            }

            double result = stack.Pop();

            if (double.IsNaN(result) || double.IsInfinity(result))
            {
                throw new ExpressionException("Результат не определён");
            }

            return result;
        }

        private static double Pop(Stack<double> stack)
        {
            if (stack.Count == 0)
            {
                throw new ExpressionException("Не хватает операнда");
            }

            return stack.Pop();
        }

        private static double ApplyBinary(string op, double left, double right)
        {
            switch (op)
            {
                case "+":
                    return left + right;
                case "-":
                    return left - right;
                case "*":
                    return left * right;
                case "/":
                    if (Math.Abs(right) < double.Epsilon)
                    {
                        throw new ExpressionException("Деление на ноль");
                    }

                    return left / right;
                case "%":
                    if (Math.Abs(right) < double.Epsilon)
                    {
                        throw new ExpressionException("Деление на ноль");
                    }

                    return left % right;
                case "^":
                    double power = Math.Pow(left, right);
                    if (double.IsNaN(power))
                    {
                        throw new ExpressionException("Возведение в степень не определено");
                    }

                    return power;
                default:
                    throw new ExpressionException("Неизвестная операция: " + op);
            }
        }

        private double ApplyFunction(string name, double argument)
        {
            double k = UseDegrees ? Math.PI / 180.0 : 1.0;

            switch (name)
            {
                case "sin":
                    return Math.Sin(argument * k);
                case "cos":
                    return Math.Cos(argument * k);
                case "tan":
                case "tg":
                    if (Math.Abs(Math.Cos(argument * k)) < 1e-12)
                    {
                        throw new ExpressionException("Тангенс не определён");
                    }

                    return Math.Tan(argument * k);
                case "ctg":
                case "cot":
                    if (Math.Abs(Math.Sin(argument * k)) < 1e-12)
                    {
                        throw new ExpressionException("Котангенс не определён");
                    }

                    return Math.Cos(argument * k) / Math.Sin(argument * k);
                case "asin":
                case "arcsin":
                    if (argument < -1 || argument > 1)
                    {
                        throw new ExpressionException("Аргумент arcsin вне диапазона [-1; 1]");
                    }

                    return Math.Asin(argument) / k;
                case "acos":
                case "arccos":
                    if (argument < -1 || argument > 1)
                    {
                        throw new ExpressionException("Аргумент arccos вне диапазона [-1; 1]");
                    }

                    return Math.Acos(argument) / k;
                case "atan":
                case "arctg":
                    return Math.Atan(argument) / k;
                case "ln":
                    if (argument <= 0)
                    {
                        throw new ExpressionException("Логарифм определён только для положительных чисел");
                    }

                    return Math.Log(argument);
                case "log":
                case "lg":
                    if (argument <= 0)
                    {
                        throw new ExpressionException("Логарифм определён только для положительных чисел");
                    }

                    return Math.Log10(argument);
                case "sqrt":
                    if (argument < 0)
                    {
                        throw new ExpressionException("Корень из отрицательного числа");
                    }

                    return Math.Sqrt(argument);
                case "exp":
                    return Math.Exp(argument);
                case "abs":
                    return Math.Abs(argument);
                case "sign":
                    return Math.Sign(argument);
                case "round":
                    return Math.Round(argument, MidpointRounding.AwayFromZero);
                default:
                    throw new ExpressionException("Неизвестная функция: " + name);
            }
        }

        private static double Factorial(double value)
        {
            if (Math.Abs(value - Math.Round(value)) > 1e-9)
            {
                throw new ExpressionException("Факториал определён только для целых чисел");
            }

            int n = (int)Math.Round(value);

            if (n < 0)
            {
                throw new ExpressionException("Факториал определён только для неотрицательных чисел");
            }

            if (n > 170)
            {
                throw new ExpressionException("Переполнение при вычислении факториала");
            }

            double result = 1.0;
            for (int i = 2; i <= n; i++)
            {
                result *= i;
            }

            return result;
        }

        #endregion
    }
}
