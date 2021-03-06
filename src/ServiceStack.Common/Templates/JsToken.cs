using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using ServiceStack.Text;

#if NETSTANDARD2_0
using Microsoft.Extensions.Primitives;
#endif

namespace ServiceStack.Templates
{
    public abstract class JsToken : IRawString
    {
        public abstract string ToRawString();

        public string JsonValue(object value)
        {
            if (value == null || value == JsNull.Value)
                return "null";
            if (value is JsToken jt)
                return jt.ToRawString();
            if (value is string s)
                return s.EncodeJson();
            return value.ToString();
        }

        public override string ToString() => ToRawString();

        public abstract object Evaluate(TemplateScopeContext scope);

        public static object UnwrapValue(JsToken token)
        {
            if (token is JsLiteral literal)
                return literal.Value;
            return null;
        }        
    }
    
    public class JsNull : JsToken
    {
        public const string String = "null";
        
        private JsNull() {} //this is the only one
        public static JsNull Value = new JsNull();
        public override string ToRawString() => String;

        public override object Evaluate(TemplateScopeContext scope) => null;
    }

    public class JsLiteral : JsToken
    {
        public static JsLiteral True = new JsLiteral(true);
        public static JsLiteral False = new JsLiteral(false);
        
        public object Value { get; }
        public JsLiteral(object value) => Value = value;
        public override string ToRawString() => JsonValue(Value);

        public override int GetHashCode() => (Value != null ? Value.GetHashCode() : 0);
        protected bool Equals(JsLiteral other) => Equals(Value, other.Value);
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == this.GetType() && Equals((JsLiteral) obj);
        }

        public override string ToString() => ToRawString();

        public override object Evaluate(TemplateScopeContext scope) => Value;
    }

    public class JsIdentifier : JsToken
    {
        public StringSegment Name { get; }

        private string nameString;
        public string NameString => nameString ?? (nameString = Name.HasValue ? Name.Value : null);

        public JsIdentifier(string name) => Name = name.ToStringSegment();
        public JsIdentifier(StringSegment name) => Name = name;
        public override string ToRawString() => ":" + Name;

        protected bool Equals(JsIdentifier other) => string.Equals(Name, other.Name);
        public override int GetHashCode() => Name.GetHashCode();

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((JsIdentifier) obj);
        }

        public override string ToString() => ToRawString();
        
        public override object Evaluate(TemplateScopeContext scope)
        {
            var ret = scope.PageResult.GetValue(NameString, scope);
            return ret;
        }
    }

    public class JsArrayExpression : JsToken
    {
        public JsToken[] Elements { get; }

        public JsArrayExpression(params JsToken[] elements) => Elements = elements.ToArray();
        public JsArrayExpression(IEnumerable<JsToken> elements) : this(elements.ToArray()) {}

        public override object Evaluate(TemplateScopeContext scope)
        {
            var to = new List<object>();
            foreach (var element in Elements)
            {
                var value = element.Evaluate(scope);
                to.Add(value);
            }
            return to;
        }

        public override string ToRawString()
        {
            var sb = StringBuilderCache.Allocate();
            sb.Append("[");
            for (var i = 0; i < Elements.Length; i++)
            {
                if (i > 0) 
                    sb.Append(",");
                
                var element = Elements[i];
                sb.Append(element.ToRawString());
            }
            sb.Append("]");
            return StringBuilderCache.ReturnAndFree(sb);
        }

        protected bool Equals(JsArrayExpression other)
        {
            return Elements.EquivalentTo(other.Elements);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((JsArrayExpression) obj);
        }

        public override int GetHashCode()
        {
            return (Elements != null ? Elements.GetHashCode() : 0);
        }
    }

    public class JsObjectExpression : JsToken
    {
        public JsProperty[] Properties { get; }

        public JsObjectExpression(params JsProperty[] properties) => Properties = properties;
        public JsObjectExpression(IEnumerable<JsProperty> properties) : this(properties.ToArray()) {}

        public static string GetKey(JsToken token)
        {
            if (token is JsLiteral literalKey)
                return literalKey.Value.ToString();
            if (token is JsIdentifier identifierKey)
                return identifierKey.NameString;
            
            throw new ArgumentException($"Invalid Key. Expected a Literal or Identifier but was '{token}'");
        }

        public override object Evaluate(TemplateScopeContext scope)
        {
            var to = new Dictionary<string, object>();
            foreach (var prop in Properties)
            {
                var keyString = GetKey(prop.Key);
                var value = prop.Value.Evaluate(scope);
                to[keyString] = value;
            }
            return to;
        }

        public override string ToRawString()
        {
            var sb = StringBuilderCache.Allocate();
            sb.Append("{");
            for (var i = 0; i < Properties.Length; i++)
            {
                if (i > 0) 
                    sb.Append(",");
                
                var prop = Properties[i];
                sb.Append(prop.Key.ToRawString());
                if (!prop.Shorthand)
                {
                    sb.Append(":");
                    sb.Append(prop.Value.ToRawString());
                }
            }
            sb.Append("}");
            return StringBuilderCache.ReturnAndFree(sb);
        }

        protected bool Equals(JsObjectExpression other)
        {
            return Properties.EquivalentTo(other.Properties);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((JsObjectExpression) obj);
        }

        public override int GetHashCode()
        {
            return (Properties != null ? Properties.GetHashCode() : 0);
        }
    }

    public class JsProperty
    {
        public JsToken Key { get; }
        public JsToken Value { get; }
        public bool Shorthand { get; }

        public JsProperty(JsToken key, JsToken value) : this(key, value, false){}
        public JsProperty(JsToken key, JsToken value, bool shorthand)
        {
            Key = key;
            Value = value;
            Shorthand = shorthand;
        }

        protected bool Equals(JsProperty other)
        {
            return Equals(Key, other.Key) && 
                   Equals(Value, other.Value) && 
                   Shorthand == other.Shorthand;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((JsProperty) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (Key != null ? Key.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Value != null ? Value.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ Shorthand.GetHashCode();
                return hashCode;
            }
        }
    }

    public static class JsTokenUtils
    {
        private static readonly byte[] ValidNumericChars;
        private static readonly byte[] ValidVarNameChars;
        private static readonly byte[] OperatorChars;
        private const byte True = 1;
        
        public static readonly HashSet<char> ExpressionTerminator = new HashSet<char> {
            ')',
            '}',
            ';',
            ',',
            ']',
        };

        public static readonly Dictionary<string, int> OperatorPrecedence = new Dictionary<string, int> {
            {")", 0},
            {";", 0},
            {",", 0},
            {"=", 0},
            {"]", 0},
            {"||", 1},
            {"&&", 2},
            {"|", 3},
            {"^", 4},
            {"&", 5},
            {"==", 6},
            {"!=", 6},
            {"===", 6},
            {"!==", 6},
            {"<", 7},
            {">", 7},
            {"<=", 7},
            {">=", 7},
            {"<<", 8},
            {">>", 8},
            {">>>", 8},
            {"+", 9},
            {"-", 9},
            {"*", 11},
            {"/", 11},
            {"%", 11},
        };

        public static int GetBinaryPrecedence(string token)
        {
            return OperatorPrecedence.TryGetValue(token, out var precedence)
                ? precedence
                : 0;
        }

        static JsTokenUtils()
        {
            var n = new byte['e' + 1];
            n['0'] = n['1'] = n['2'] = n['3'] = n['4'] = n['5'] = n['6'] = n['7'] = n['8'] = n['9'] = n['.'] = True;
            ValidNumericChars = n;

            var o = new byte['|' + 1];
            o['<'] = o['>'] = o['='] = o['!'] = o['+'] = o['-'] = o['*'] = o['/'] = o['%'] = o['|'] = o['&'] = o['^'] = True;
            OperatorChars = o;

            var a = new byte['z' + 1];
            for (var i = (int) '0'; i < a.Length; i++)
            {
                if (i >= 'A' && i <= 'Z' || i >= 'a' && i <= 'z' || i >= '0' && i <= '9' || i == '_')
                    a[i] = True;
            }
            ValidVarNameChars = a;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNumericChar(this char c) => c < ValidNumericChars.Length && ValidNumericChars[c] == True;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsValidVarNameChar(this char c) => c < ValidVarNameChars.Length && ValidVarNameChars[c] == True;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsOperatorChar(this char c) => c < OperatorChars.Length && OperatorChars[c] == True;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsBindingExpressionChar(this char c) => c == '.' || c == '(' || c == '[';

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static StringSegment AdvancePastWhitespace(this StringSegment literal)
        {
            var i = 0;
            while (i < literal.Length && literal.GetChar(i).IsWhiteSpace())
                i++;

            return i == 0 ? literal : literal.Subsegment(i < literal.Length ? i : literal.Length);
        }

        public static bool EvaluateToBool(this JsToken token, TemplateScopeContext scope)
        {
            var ret = token.Evaluate(scope);
            if (ret is bool b)
                return b;
            
            throw new ArgumentException($"Expected bool expression but instead received '{token}'");
        }

        internal static string StripQuotes(this StringSegment arg) => arg.HasValue ? StripQuotes(arg.Value) : string.Empty;
        internal static string StripQuotes(this string arg)
        {
            if (arg == null || arg.Length < 2)
                return arg;

            switch (arg[0])
            {
                case '"':
                case '`':
                case '\'':
                case '′':
                    return arg.Substring(1, arg.Length - 2);
            }

            return arg;
        }

        public static StringSegment AdvancePastChar(this StringSegment literal, char delim)
        {
            var i = 0;
            var c = (char) 0;
            while (i < literal.Length && (c = literal.GetChar(i)) != delim)
                i++;

            if (c == delim)
                return literal.Subsegment(i + 1);

            return i == 0 ? literal : literal.Subsegment(i < literal.Length ? i : literal.Length);
        }
        
        public static StringSegment ParseJsToken(this StringSegment literal, out JsToken token) => ParseJsToken(literal, out token, false);
        public static StringSegment ParseJsToken(this StringSegment literal, out JsToken token, bool filterExpression)
        {
            literal = literal.AdvancePastWhitespace();

            if (literal.IsNullOrEmpty())
            {
                token = null;
                return literal;
            }
            
            var c = literal.GetChar(0);
            if (c == '(')
            {
                literal = literal.Advance(1);
                literal = literal.ParseJsExpression(out var bracketsExpr);
                literal = literal.AdvancePastWhitespace();

                c = literal.GetChar(0);
                if (c == ')')
                {
                    literal = literal.Advance(1);
                    token = bracketsExpr;
                    return literal;
                }
                
                throw new SyntaxErrorException($"Expected ')' but instead found '{c}': {literal.SubstringWithElipsis(0, 50)}");
            }

            token = null;
            c = (char) 0;

            if (literal.IsNullOrEmpty())
                return TypeConstants.EmptyStringSegment;

            var i = 0;
            literal = literal.AdvancePastWhitespace();

            var firstChar = literal.GetChar(0);
            if (firstChar == '\'' || firstChar == '"' || firstChar == '`' || firstChar == '′')
            {
                i = 1;
                var hasEscapeChar = false;
                while (i < literal.Length && ((c = literal.GetChar(i)) != firstChar || literal.GetChar(i - 1) == '\\'))
                {
                    i++;
                    if (!hasEscapeChar)
                        hasEscapeChar = c == '\\';
                }

                if (i >= literal.Length || literal.GetChar(i) != firstChar)
                    throw new SyntaxErrorException($"Unterminated string literal: {literal}");

                var str = literal.Substring(1, i - 1);
                token = new JsLiteral(str);

                if (hasEscapeChar)
                {
                    var sb = StringBuilderCache.Allocate();
                    for (var j = 0; j < str.Length; j++)
                    {
                        // strip the back-slash used to escape quote char in strings
                        var ch = str[j];
                        if (ch != '\\' || (j + 1 >= str.Length || str[j + 1] != firstChar))
                            sb.Append(ch);
                    }
                    token = new JsLiteral(StringBuilderCache.ReturnAndFree(sb));
                }
                
                return literal.Advance(i + 1);
            }
            if (firstChar >= '0' && firstChar <= '9')
            {
                i = 1;
                var hasExponent = false;
                var hasDecimal = false;

                while (i < literal.Length && IsNumericChar(c = literal.GetChar(i)) ||
                       (hasExponent = (c == 'e' || c == 'E')))
                {
                    if (c == '.')
                        hasDecimal = true;

                    i++;

                    if (hasExponent)
                    {
                        i += 2; // [e+1]0

                        while (i < literal.Length && IsNumericChar(literal.GetChar(i)))
                            i++;

                        break;
                    }
                }

                var numLiteral = literal.Subsegment(0, i);

                //don't convert into ternary to avoid Type coercion
                if (hasDecimal || hasExponent)
                    token = new JsLiteral(numLiteral.TryParseDouble(out double d) ? d : default(double));
                else
                    token = new JsLiteral(numLiteral.ParseSignedInteger());

                return literal.Advance(i);
            }
            if (firstChar == '{')
            {
                var props = new List<JsProperty>();

                literal = literal.Advance(1);
                while (!literal.IsNullOrEmpty())
                {
                    literal = literal.AdvancePastWhitespace();
                    if (literal.GetChar(0) == '}')
                    {
                        literal = literal.Advance(1);
                        break;
                    }

                    literal = literal.ParseJsToken(out var mapKeyToken);

                    if (!(mapKeyToken is JsLiteral) && !(mapKeyToken is JsIdentifier)) 
                        throw new SyntaxErrorException($"'{mapKeyToken}' is not a valid Object key, expected literal or identifier.");

                    JsToken mapValueToken;
                    bool shorthand = false;

                    literal = literal.AdvancePastWhitespace();
                    if (literal.Length > 0 && literal.GetChar(0) == ':')
                    {
                        literal = literal.Advance(1);
                        literal = literal.ParseJsExpression(out mapValueToken);

                    }
                    else 
                    {
                        shorthand = true;
                        if (literal.Length == 0 || (c = literal.GetChar(0)) != ',' && c != '}')
                            throw new SyntaxErrorException($"Unterminated object literal near: {literal.SubstringWithElipsis(0, 50)}");
                            
                        mapValueToken = mapKeyToken;
                    }
                    
                    props.Add(new JsProperty(mapKeyToken, mapValueToken, shorthand));

                    literal = literal.AdvancePastWhitespace();
                    if (literal.IsNullOrEmpty())
                        break;

                    if (literal.GetChar(0) == '}')
                    {
                        literal = literal.Advance(1);
                        break;
                    }

                    literal = literal.AdvancePastChar(',');
                    literal = literal.AdvancePastWhitespace();
                }

                token = new JsObjectExpression(props);
                return literal;
            }
            if (firstChar == '[')
            {
                literal = literal.ParseArguments(out var elements, termination: ']');

                token = new JsArrayExpression(elements);
                return literal;
            }
            if (firstChar.IsOperatorChar())
            {
                if (literal.StartsWith(">="))
                {
                    token = JsGreaterThanEqual.Operator;
                    return literal.Advance(2);
                }
                if (literal.StartsWith("<="))
                {
                    token = JsLessThanEqual.Operator;
                    return literal.Advance(2);
                }
                if (literal.StartsWith("!=="))
                {
                    token = JsStrictNotEquals.Operator;
                    return literal.Advance(3);
                }
                if (literal.StartsWith("!="))
                {
                    token = JsNotEquals.Operator;
                    return literal.Advance(2);
                }
                if (literal.StartsWith("==="))
                {
                    token = JsStrictEquals.Operator;
                    return literal.Advance(3);
                }
                if (literal.StartsWith("=="))
                {
                    token = JsEquals.Operator;
                    return literal.Advance(2);
                }
                if (literal.StartsWith("||"))
                {
                    token = JsOr.Operator;
                    return literal.Advance(2);
                }
                if (literal.StartsWith("&&"))
                {
                    token = JsAnd.Operator;
                    return literal.Advance(2);
                }
                if (literal.StartsWith("<<"))
                {
                    token = JsBitwiseLeftShift.Operator;
                    return literal.Advance(2);
                }
                if (literal.StartsWith(">>"))
                {
                    token = JsBitwiseRightShift.Operator;
                    return literal.Advance(2);
                }

                switch (firstChar)
                {
                    case '>':
                        token = JsGreaterThan.Operator;
                        return literal.Advance(1);
                    case '<':
                        token = JsLessThan.Operator;
                        return literal.Advance(1);
                    case '=':
                        token = JsAssignment.Operator;
                        return literal.Advance(1);
                    case '!':
                        token = JsNot.Operator;
                        return literal.Advance(1);
                    case '+':
                        token = JsAddition.Operator;
                        return literal.Advance(1);
                    case '-':
                        token = JsSubtraction.Operator;
                        return literal.Advance(1);
                    case '*':
                        token = JsMultiplication.Operator;
                        return literal.Advance(1);
                    case '/':
                        token = JsDivision.Operator;
                        return literal.Advance(1);
                    case '&':
                        token = JsBitwiseAnd.Operator;
                        return literal.Advance(1);
                    case '|':
                        token = JsBitwiseOr.Operator;
                        return literal.Advance(1);
                    case '^':
                        token = JsBitwiseXOr.Operator;
                        return literal.Advance(1);
                    case '%':
                        token = JsMod.Operator;
                        return literal.Advance(1);
                    default:
                        throw new SyntaxErrorException($"Invalid Operator found near: '{literal.SubstringWithElipsis(0, 50)}'");
                }
            }

            // identifier
            var preIdentifierLiteral = literal;

            literal = literal.ParseIdentifier(out var node);

            literal = literal.AdvancePastWhitespace();
            if (!literal.IsNullOrEmpty())
            {
                c = literal.GetChar(i);
                if (c == '.' || c == '[')
                {
                    while (true)
                    {
                        literal = literal.Advance(1);

                        if (c == '.')
                        {
                            literal = literal.AdvancePastWhitespace();
                            literal = literal.ParseIdentifier(out var property);
                            node = new JsMemberExpression(node, property, computed: false);
                        }
                        else if (c == '[')
                        {
                            literal = literal.AdvancePastWhitespace();
                            literal = literal.ParseJsExpression(out var property);
                            node = new JsMemberExpression(node, property, computed: true);

                            literal = literal.AdvancePastWhitespace();
                            if (literal.IsNullOrEmpty() || literal.GetChar(0) != ']')
                                throw new SyntaxErrorException($"Expected ']' but was '{literal.GetChar(0)}'");

                            literal = literal.Advance(1);
                        }

                        literal = literal.AdvancePastWhitespace();
                        
                        if (literal.IsNullOrWhiteSpace())
                            break;

                        c = literal.GetChar(0);
                        if (c == '(')
                            throw new SyntaxErrorException("Call expression found on member expression. Only filters can be invoked.");
                        
                        if (!(c == '.' || c == '['))
                            break;
                    }
                }
                else if (c == '(' || (filterExpression && c == ':'))
                {
                    literal = preIdentifierLiteral.ParseJsCallExpression(out var callExpr, filterExpression:filterExpression);
                    token = callExpr;
                    return literal;
                }
            }

            token = node;
            return literal;
        }

        internal static StringSegment ParseIdentifier(this StringSegment literal, out JsToken token)
        {
            var i = 0;

            literal = literal.AdvancePastWhitespace();

            if (literal.IsNullOrEmpty())
                throw new SyntaxErrorException("Expected identifier before end");

            var c = literal.GetChar(i);
            if (!c.IsValidVarNameChar())
                throw new SyntaxErrorException($"Expected start of identifier but was '{c}'");

            i++;
            
            while (i < literal.Length)
            {
                c = literal.GetChar(i);

                if (IsValidVarNameChar(c))
                    i++;
                else
                    break;
            }

            var identifier = literal.Subsegment(0, i).TrimEnd();
            literal = literal.Advance(i);
            
            if (identifier.Equals("true"))
                token = JsLiteral.True;
            else if (identifier.Equals("false"))
                token = JsLiteral.False;
            else if (identifier.Equals("null"))
                token = JsNull.Value;
            else if (identifier.Equals("and"))
                token = JsAnd.Operator;
            else if (identifier.Equals("or"))
                token = JsOr.Operator;
            else
                token = new JsIdentifier(identifier);

            return literal;
        }
        
    }

    public static class CallExpressionUtils
    {
        public static StringSegment ParseJsCallExpression(this StringSegment literal, out JsCallExpression expression, bool filterExpression=false)
        {
            literal = literal.ParseIdentifier(out var token);
            
            if (!(token is JsIdentifier identifier))
                throw new SyntaxErrorException($"Expected identifier but instead found '{token}'");

            literal = literal.AdvancePastWhitespace();

            if (literal.IsNullOrEmpty() || literal.GetChar(0) != '(')
            {
                var isWhitespaceSyntax = filterExpression && literal.GetChar(0) == ':';
                if (isWhitespaceSyntax)
                {
                    literal = literal.Advance(1);
                    
                    // replace everything after ':' up till new line and rewrite as single string to method
                    var endStringPos = literal.IndexOf("\n");
                    var endStatementPos = literal.IndexOf("}}");
    
                    if (endStringPos == -1 || (endStatementPos != -1 && endStatementPos < endStringPos))
                        endStringPos = endStatementPos;
                            
                    if (endStringPos == -1)
                        throw new SyntaxErrorException($"Whitespace sensitive syntax did not find a '\\n' new line to mark the end of the statement, near '{literal.SubstringWithElipsis(0,50)}'");

                    var originalArg = literal.Subsegment(0, endStringPos).Trim().ToString();
                    var rewrittenArgs = originalArg.Replace("{","{{").Replace("}","}}");
                    var strArg = new JsLiteral(rewrittenArgs);
    
                    expression = new JsCallExpression(identifier, strArg);
                    return literal.Subsegment(endStringPos);
                }
                
                expression = new JsCallExpression(identifier);
                return literal;
            }

            literal.Advance(1);

            literal = literal.ParseArguments(out var args, termination: ')');
            
            expression = new JsCallExpression(identifier, args.ToArray());
            return literal;
        }

        internal static StringSegment ParseArguments(this StringSegment literal, out List<JsToken> arguments, char termination)
        {
            arguments = new List<JsToken>();

            literal = literal.Advance(1);
            while (!literal.IsNullOrEmpty())
            {
                literal = literal.AdvancePastWhitespace();
                if (literal.GetChar(0) == termination)
                {
                    literal = literal.Advance(1);
                    break;
                }

                literal = literal.ParseJsExpression(out var listValue);
                arguments.Add(listValue);

                literal = literal.AdvancePastWhitespace();
                if (literal.IsNullOrEmpty())
                    break;
                    
                if (literal.GetChar(0) == termination)
                {
                    literal = literal.Advance(1);
                    break;
                }

                literal = literal.AdvancePastWhitespace();
                var c = literal.GetChar(0);
                if (c == termination)
                {
                    literal = literal.Advance(1);
                    break;
                }
                    
                if (c != ',')
                    throw new ArgumentException($"Unterminated arguments expression near: {literal.SubstringWithElipsis(0, 50)}");
                    
                literal = literal.Advance(1);
                literal = literal.AdvancePastWhitespace();
            }

            literal = literal.AdvancePastWhitespace();

            return literal;
        }
    }
}