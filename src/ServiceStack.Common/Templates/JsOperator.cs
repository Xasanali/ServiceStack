namespace ServiceStack.Templates
{
    public abstract class JsOperator : JsToken
    {
        public abstract string Token { get; }
        public override string ToRawString() => Token;
        public override object Evaluate(TemplateScopeContext scope) => this;
    }

    public abstract class JsBinaryOperator : JsOperator
    {
        public abstract object Evaluate(object lhs, object rhs);
    }

    public abstract class JsUnaryOperator : JsOperator
    {
        public abstract object Evaluate(object target);

        public static JsUnaryOperator GetUnaryOperator(JsOperator op) =>
            (JsUnaryOperator) (
                op == JsSubtraction.Operator
                    ? JsMinus.Operator
                    : op == JsNot.Operator
                        ? op
                        : null);
    }

    public abstract class JsLogicOperator : JsBinaryOperator
    {
        public abstract bool Test(object lhs, object rhs);
        public override object Evaluate(object lhs, object rhs) => Test(lhs, rhs);
    }

    public class JsGreaterThan : JsLogicOperator
    {
        public static JsGreaterThan Operator = new JsGreaterThan();
        private JsGreaterThan() { }
        public override bool Test(object lhs, object rhs) => TemplateDefaultFilters.Instance.greaterThan(lhs, rhs);
        public override string Token => ">";
    }

    public class JsGreaterThanEqual : JsLogicOperator
    {
        public static JsGreaterThanEqual Operator = new JsGreaterThanEqual();
        private JsGreaterThanEqual() { }
        public override bool Test(object lhs, object rhs) => TemplateDefaultFilters.Instance.greaterThanEqual(lhs, rhs);
        public override string Token => ">=";
    }

    public class JsLessThanEqual : JsLogicOperator
    {
        public static JsLessThanEqual Operator = new JsLessThanEqual();
        private JsLessThanEqual() { }
        public override bool Test(object lhs, object rhs) => TemplateDefaultFilters.Instance.lessThanEqual(lhs, rhs);
        public override string Token => "<=";
    }

    public class JsLessThan : JsLogicOperator
    {
        public static JsLessThan Operator = new JsLessThan();
        private JsLessThan() { }
        public override bool Test(object lhs, object rhs) => TemplateDefaultFilters.Instance.lessThan(lhs, rhs);
        public override string Token => "<";
    }

    public class JsEquals : JsLogicOperator
    {
        public static JsEquals Operator = new JsEquals();
        private JsEquals() { }
        public override bool Test(object lhs, object rhs) => TemplateDefaultFilters.Instance.equals(lhs, rhs);
        public override string Token => "==";
    }

    public class JsNotEquals : JsLogicOperator
    {
        public static JsNotEquals Operator = new JsNotEquals();
        private JsNotEquals() { }
        public override bool Test(object lhs, object rhs) => TemplateDefaultFilters.Instance.notEquals(lhs, rhs);
        public override string Token => "!=";
    }

    public class JsStrictEquals : JsLogicOperator
    {
        public static JsStrictEquals Operator = new JsStrictEquals();
        private JsStrictEquals() { }
        public override bool Test(object lhs, object rhs) => TemplateDefaultFilters.Instance.equals(lhs, rhs);
        public override string Token => "===";
    }

    public class JsStrictNotEquals : JsLogicOperator
    {
        public static JsStrictNotEquals Operator = new JsStrictNotEquals();
        private JsStrictNotEquals() { }
        public override bool Test(object lhs, object rhs) => TemplateDefaultFilters.Instance.notEquals(lhs, rhs);
        public override string Token => "!==";
    }

    public class JsAssignment : JsBinaryOperator
    {
        public static JsAssignment Operator = new JsAssignment();
        private JsAssignment() { }
        public override string Token => "=";
        public override object Evaluate(object lhs, object rhs) => rhs;
    }

    public class JsOr : JsLogicOperator
    {
        public static JsOr Operator = new JsOr();
        private JsOr() { }

        public override bool Test(object lhs, object rhs) =>
            TemplateDefaultFilters.isTrue(lhs) || TemplateDefaultFilters.isTrue(rhs);

        public override string Token => "||";
    }

    public class JsAnd : JsLogicOperator
    {
        public static JsAnd Operator = new JsAnd();
        private JsAnd() { }

        public override bool Test(object lhs, object rhs) =>
            TemplateDefaultFilters.isTrue(lhs) && TemplateDefaultFilters.isTrue(rhs);

        public override string Token => "&&";
    }

    public class JsNot : JsUnaryOperator
    {
        public static JsNot Operator = new JsNot();
        private JsNot() { }
        public override string Token => "!";
        public override object Evaluate(object target) => !TemplateDefaultFilters.isTrue(target);
    }

    public class JsBitwiseAnd : JsBinaryOperator
    {
        public static JsBitwiseAnd Operator = new JsBitwiseAnd();
        private JsBitwiseAnd() { }
        public override string Token => "&";

        public override object Evaluate(object lhs, object rhs) =>
            DynamicNumber.GetNumber(lhs, rhs).bitwiseAnd(lhs, rhs);
    }

    public class JsBitwiseOr : JsBinaryOperator
    {
        public static JsBitwiseOr Operator = new JsBitwiseOr();
        private JsBitwiseOr() { }
        public override string Token => "|";

        public override object Evaluate(object lhs, object rhs) =>
            DynamicNumber.GetNumber(lhs, rhs).bitwiseOr(lhs, rhs);
    }

    public class JsBitwiseXOr : JsBinaryOperator
    {
        public static JsBitwiseXOr Operator = new JsBitwiseXOr();
        private JsBitwiseXOr() { }
        public override string Token => "^";

        public override object Evaluate(object lhs, object rhs) =>
            DynamicNumber.GetNumber(lhs, rhs).bitwiseXOr(lhs, rhs);
    }

    public class JsBitwiseLeftShift : JsBinaryOperator
    {
        public static JsBitwiseLeftShift Operator = new JsBitwiseLeftShift();
        private JsBitwiseLeftShift() { }
        public override string Token => "<<";

        public override object Evaluate(object lhs, object rhs) =>
            DynamicNumber.GetNumber(lhs, rhs).bitwiseLeftShift(lhs, rhs);
    }

    public class JsBitwiseRightShift : JsBinaryOperator
    {
        public static JsBitwiseRightShift Operator = new JsBitwiseRightShift();
        private JsBitwiseRightShift() { }
        public override string Token => ">>";

        public override object Evaluate(object lhs, object rhs) =>
            DynamicNumber.GetNumber(lhs, rhs).bitwiseRightShift(lhs, rhs);
    }

    public class JsAddition : JsBinaryOperator
    {
        public static JsAddition Operator = new JsAddition();
        private JsAddition() { }
        public override string Token => "+";

        public override object Evaluate(object lhs, object rhs)
        {
            if (lhs is string || rhs is string)
            {
                var lhsString = lhs.ConvertTo<string>();
                var rhsString = rhs.ConvertTo<string>();
                return string.Concat(lhsString, rhsString);
            }

            return DynamicNumber.GetNumber(lhs, rhs).add(lhs, rhs);
        }
    }

    public class JsSubtraction : JsBinaryOperator
    {
        public static JsSubtraction Operator = new JsSubtraction();
        private JsSubtraction() { }
        public override string Token => "-";

        public override object Evaluate(object lhs, object rhs) =>
            DynamicNumber.GetNumber(lhs, rhs).sub(lhs, rhs);
    }

    public class JsMultiplication : JsBinaryOperator
    {
        public static JsMultiplication Operator = new JsMultiplication();
        private JsMultiplication() { }
        public override string Token => "*";

        public override object Evaluate(object lhs, object rhs) =>
            DynamicNumber.GetNumber(lhs, rhs).mul(lhs, rhs);
    }

    public class JsDivision : JsBinaryOperator
    {
        public static JsDivision Operator = new JsDivision();
        private JsDivision() { }
        public override string Token => "/";

        public override object Evaluate(object lhs, object rhs) =>
            DynamicNumber.GetNumber(lhs, rhs).div(lhs, rhs);
    }

    public class JsMod : JsBinaryOperator
    {
        public static JsMod Operator = new JsMod();
        private JsMod() { }
        public override string Token => "%";

        public override object Evaluate(object lhs, object rhs) =>
            DynamicNumber.GetNumber(lhs, rhs).mod(lhs, rhs);
    }

    public class JsMinus : JsUnaryOperator
    {
        public static JsMinus Operator = new JsMinus();
        private JsMinus() { }
        public override string Token => "-";

        public override object Evaluate(object target) => target == null
            ? 0
            : DynamicNumber.Multiply(target, -1).ConvertTo(target.GetType());
    }

    public class JsPlus : JsUnaryOperator
    {
        public static JsPlus Operator = new JsPlus();
        private JsPlus() { }
        public override string Token => "+";
        public override object Evaluate(object target) => target ?? 0;
    }
}