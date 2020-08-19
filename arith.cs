using System;

namespace arith
{
    public class term
    {
        //   D A T A   F I E L D S 
        private enum Tag { True, False, If, Zero, Succ, Pred, IsZero, Error }
        private readonly Tag tag;
        private readonly term t; // shared by Succ, Pred, IsZero (one node), and by the guard of if-clause
        private readonly term if2;
        private readonly term if3;
        private readonly string m; // built-in Term-Option lol. room for error message
        private term(Tag tag, term t, term if2, term if3, string m)
        {
            this.tag = tag;
            this.t = t;
            this.if2 = if2;
            this.if3 = if3;
            this.m = m;
        }
        //   C O N S T R U C T O R S 
        public static term newTrue() => new term(Tag.True, null, null, null, null);
        public static term newFalse() => new term(Tag.False, null, null, null, null);
        public static term newZero() => new term(Tag.Zero, null, null, null, null);
        public static term newSucc(term t) => new term(Tag.Succ, t, null, null, null);
        public static term newPred(term t) => new term(Tag.Pred, t, null, null, null);
        public static term newIsZero(term t) => new term(Tag.IsZero, t, null, null, null);
        public static term newIf(term t, term if2, term if3) => new term(Tag.If, t, if2, if3, null);
        public static term newError(string m) => new term(Tag.Error, null, null, null, m); 
        //   P R I V A T E   H E L P E R S 
        private bool isNumericalVal()
        {
            switch (this.tag)
            {
                case Tag.Zero: return true;
                case Tag.Succ: //return this.t.isNumericalVal();
                    term next = this.t; // no recursion :D 
                    while (true)
                    {
                        switch (next.tag)
                        {
                            case Tag.Zero: return true;
                            case Tag.Succ:
                                next = next.t;
                                break;
                            default: return false;
                        }
                    }
                default: return false;
            }
        }
        private bool isVal()
        {
            if (this.isNumericalVal()) return true;
            else switch (this.tag)
                {
                    case Tag.True: 
                    case Tag.False: return true;
                    default: return false;
                }
        }
        //   E V A L 
        public term eval()
        {
            term t = this.eval1();
            if (t.tag == Tag.Error && t.m == "NoRuleApplies")
            {
                return this;
            }
            else return t.eval();
        }
        public term eval1() // recursive calls <congruences> must be checked to propagate error
        {
            switch (this.tag)
            {
                case Tag.If:
                    switch (this.t.tag)                                     // the guard of the if-clause 
                    {
                        case Tag.True: return this.if2;                       // E-IfTrue
                        case Tag.False: return this.if3;                     // E-IfFalse
                        default:
                            term t1 = this.t.eval1();
                            if (t1.tag == Tag.Error) return t1;// error propagating
                            else return term.newIf(t1, this.if2, this.if3); // E-If <CONG1>
                    }
                case Tag.Succ:
                    term t_ = this.t.eval1();
                    if (t_.tag == Tag.Error) return t_; // error propagating
                    return term.newSucc(t_); // <CONG2>
                case Tag.Pred:
                    switch (this.t.tag)
                    {
                        case Tag.Zero: return term.newZero();            // Pred(Zero) -> Zero 
                        case Tag.Succ:
                            term nv1 = this.t.t;                             // Pred(Succ(nv1)) -> nv1
                            if (nv1.isNumericalVal()) return nv1;
                            else goto NoRuleApplies; // subtree not numerical, error
                        default:
                            term t1 = this.t.eval1();
                            if (t1.tag == Tag.Error) return t1;// error propagating
                            return term.newPred(t1);    // Pred(t1) -> Pred(t1')    <CONG3>
                    }
                case Tag.IsZero:
                    switch (this.t.tag)
                    {
                        case Tag.Zero: return term.newTrue(); // IsZero(Zero) -> true
                        case Tag.Succ:
                            term nv1 = this.t.t; // IsZero(Succ(nv1)) -> false
                            if (nv1.isNumericalVal()) return term.newFalse();
                            else goto NoRuleApplies; // subtree not numerical, error
                        default:
                            term t1 = this.t.eval1();
                            if (t1.tag == Tag.Error) return t1;// error propagating
                            return term.newIsZero(t1); // IsZero(t1) -> IsZero(t1')     <CONG4>
                    }
                default: goto NoRuleApplies; // Tag.Error goes here too
            }
        NoRuleApplies: // using goto because of nested switch-stmts
            return term.newError("NoRuleApplies");
        }
        //   I N T E R N A L 
        public int succ_to_int()
        {
            term e = this.copy();
            int counter = 0;
            while (e.tag == Tag.Succ)
            {
                counter++;
                e = e.t;
            }
            return counter;
        }
        public term copy()
        {
            switch (this.tag)
            {
                case Tag.Zero: return newZero();
                case Tag.True: return newTrue();
                case Tag.False: return newFalse();
                case Tag.Succ: return newSucc(this.t.copy());
                case Tag.Pred: return newPred(this.t.copy());
                case Tag.IsZero: return newIsZero(this.t.copy());
                case Tag.Error: return newError(this.m);
                case Tag.If: return newIf(this.t.copy(), this.if2.copy(), this.if3.copy());
                default: return newError("error in copy()");
            }
        }
        public override string ToString()
        {
            switch (this.tag)
            {
                case Tag.True: return "true";
                case Tag.False: return "false";
                case Tag.If: return $"If ({t}) then ({if2}) else ({if3})";
                case Tag.Zero: return "zero";
                case Tag.Succ: return $"Succ({t})"; //return this.succ_to_int().ToString();
                case Tag.Pred: return $"Pred({t})";
                case Tag.IsZero: return $"IsZero({t})";
                case Tag.Error: return this.m;
                default: return "<< E R R O R >>";
            }
        }
        public static bool operator ==(term t1, term t2)
        {
            switch (t1.tag)
            {
                case Tag.Zero: return t2.tag == Tag.Zero;
                case Tag.True: return t2.tag == Tag.True;
                case Tag.False: return t2.tag == Tag.False;
                case Tag.Succ: return t2.tag == Tag.Succ ? t1.t == t2.t : false;
                case Tag.Pred: return t2.tag == Tag.Pred ? t1.t == t2.t : false;
                case Tag.IsZero: return t2.tag == Tag.IsZero ? t1.t == t2.t : false;
                case Tag.If: return t2.tag == Tag.If ? ((t1.t == t2.t) && (t1.if2 == t2.if2) && (t1.if3 == t2.if3)) : false;
                case Tag.Error: return t2.tag == Tag.Error ? t1.m == t2.m : false;
                default: return false; //  throw new Exception("Error in operator=="); // unreachable?
            }
        }
        public static bool operator !=(term t1, term t2) => !(t1 == t2);
        public static void test(term should_be, term test) => Console.WriteLine($"{should_be} -> {test}\n{(should_be == test ? "--passed--" : "[ F A I L E D ]")}");
    }
    class Program
    {
        static void Main(string[] args)
        {
            term tru = term.newTrue();
            term fals = term.newFalse();
            term zero = term.newZero();
            term one = term.newSucc(zero);
            term two = term.newSucc(one);
            term three = term.newSucc(two);
            term four = term.newSucc(three);
            term five = term.newSucc(four);


            // E-IsZero
            {
                term pred_one = term.newPred(one);
                term is_zero1 = term.newIsZero(pred_one);
                term.test(tru, is_zero1.eval()); // IsZero(Pred(Succ(Zero))) , eval 2 steps 
            }
            // E-IfTrue
            {
                term if_true_then_2 = term.newIf(tru, two, zero);
                term.test(two, if_true_then_2.eval1());
            }
            // E-IfFalse
            {
                term if_false_then_2 = term.newIf(fals, zero, two);
                term.test(two, if_false_then_2.eval1());
            }
            // E-If
            {
                term t1 = term.newIf(fals, tru, tru);
                term main = term.newIf(t1, two, two);
                term should_be = term.newIf(tru, two, two);
                term.test(should_be, main.eval1());
            }
            // eval 
            {
                term q1 = term.newIf(fals, one, zero); // -> zero
                term q2 = term.newIsZero(q1);            // -> true
                term q3 = term.newIf(q2, two, one);       // -> two 
                term q4 = term.newPred(term.newPred(q3)); // -> 0
                term q5 = term.newIsZero(q4);             // -> true
                term q6 = term.newIf(q5, one, two);       // -> one
                term q7 = term.newIsZero(term.newPred(q6)); // -> true 
                term q8 = term.newIf(q7, five, zero); // -> two
                term.test(five, q8.eval());
                Console.WriteLine(q8);
            }
            
            Console.WriteLine(term.newSucc(term.newTrue()).eval()); // wrong things come out in normal form

            Console.ReadKey();
        }
    }
}
