using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

/*
NOTES
- currently using super wasteful Context bc im too lazy to make a immutable linked list like in F#
- when a variable is substituted, say [x->bob], bob is added to the context at position 0. 
    everything else is adjusted accordingly: 
    inside of ABS, if the ABS has x lambdas, any value x+n refers to variable n in the context, 
    while x' < x refers to one of the x binders. we go in the ABS and shift anything over n by 1. 
- the act of beta-reduction, is just a substitution. 
    - if a variable has number n in the context, it has number n+1 inside an ABS. 
    - to substitute [n->s]t, where t is an ABS, we thus substitute n+1 inside its body to SHIFT(s)
        because since we are going inside a lambda, we likewise shift the new name by 1 
        to make up for the low numbers that refer to binders. 
- these deBruijn shananigans only affect evaluation in beta-reduction, that's the only place 
    where variables appear (the other evlauation rule is for APP). use the nameless subst here. 
    when we reduce a redex, we use up the bound variable. 
    (so everything else must go down by 1?) - yes. x is now removed from the context. 
    (\x.t12) v2    =>    [x->v2]t12
    ALSO, if v2 has variables, we shift them up by 1 - 
        t12 is in a larger context than v2, because it additionally contains x.
        thus we shift the variables of v2 up by 1. 
        then we substitute it into t12, and now the shifted variables definitely don't refer to x.
        then we down shift everything by 1 since we are getting rid of the lambda. 
        so we only shifted the variables in v2 up so that we don't lose them when we shift the entire
            body of t12, now with v2 replacing x, down by 1.
- beta reduction works by substituting term with deBruijn index 0 in the ABS body.
    it is replaced with 0 because, when we are inside the body, 0 refers to the first binding,
        which is the only binding we are currently interested in.
    we shift up the term v2, and replace 0 in t12 with it. then shift all the variables down by 1
        to show that we are getting rid of the lambda. 
QUESTION: 
when does a variable explicitly enter or exit a context?
add_name is only ever called by create_fresh_context, which itself is only called by print_term.
so in \x.x with empty ctx we would check if x is in there - its not - so create a copy of the context,
with x in it, and return it from pick_fresh_name. 
when we print the body, Var(x), it is actually just 0 - the deBruijn index, and we are passed the new 
context to use. since x is the only thing there, 0 is its index, so x is returned. 
    and the debug number is 1, saying that x is already in the ctx.
consider \x.(x \x.x)
when it goes into print_tm, 
we call pick_fresh_name (empty_ctx, x) so now x is in the context
then it build the string (\x.  and then calls print on the body, which is an APP, with the new ctx
that just calls print on the left and right parts 
the left part is VAR. its deB index should be 0 and the ctx len should be 1, which returns x.
the string is now (\x.(x ____ ))
now the right part is an ABS. we go to call pick_fresh_name. since x is in the context, 
    we generate x', and a new context that now contains the old x and new x', and return that.
now the string is (\x. (x \x'.___ )) 
    and we now print the body. that "body" should be Var(0,2) since x' is the newest variable, 
and is at the bottom of the variable stack. so index 0 will refer to it in that context. 
the debug number should be 2, so the total number of lambdas it is nested in. looking it up will 
return x' so the total string will be (\x.(x \x'.x'))
*/

namespace untyped_cs
{
    public class binding
    {
        // empty class for now. later will be used to store stuff like type info.
        private enum Tag { NameBind, NOT_USED_I_JUST_PUT_IT_HERE_SO_ENUM_HAS_2_THINGS_LULZ }
        private readonly Tag tag;
        private binding(Tag tag) => this.tag = tag;
        public static binding newNameBind() => new binding(Tag.NameBind);
    }
    public class Context
    {
        //   D A T A 
        private List<(string, binding)> entries;
        public int ctx_length => entries.Count;
        //   C ON S T R U C T O R 
        public Context() => entries = new List<(string, binding)>();
        //   P U B L I C   M E T H O D S 
        public bool is_name_bound(string s) => ctx_length == 0 ? false : entries.Select(x => x.Item1 == s).Aggregate((x, y) => x || y);
        public string index_2_name(int i) => entries[i].Item1;
        public int name_2_index(string x)
        {
            int c = 0;
            for (; entries[c].Item1 != x; c++) { }
            return c;
        }
        public static string print_tm(term t, Context ctx)
        {
            switch (t.tag)
            {
                case term.Tag.Abs:
                    // pick fresh name is static, it returns a new context (copy with bonus entry). 
                    var (ctx_prime, x_prime) = pick_fresh_name(ctx, t.lexeme); // this is where the previous name helper is used
                    return $"(\\ {x_prime}. {print_tm(t.right, ctx_prime)})"; 
                    // so if this used to be a \x, we generate some sort of \x', add it to the context 
                    // so names dont clash as we recursively print its body. this is why we need the copy / F# immutable 
                    // list: so that outside of this context we can start afresh. but since before i reused the same one, two lambdas with y became y and y'
                case term.Tag.App:
                    return $"({print_tm(t.left, ctx)} {print_tm(t.right, ctx)})";
                case term.Tag.Var:
                    if (ctx.ctx_length == t.DBG_CTX_LEN) return ctx.index_2_name(t.deBr_ind);
                    else return "[bad index]";
                default: return "<< E R R O R >>";
            }
        }
        //   S T A T I C   M E T H O D S 
        private Context copy()
        {
            Context ctx = new Context(); // will need to deep copy binding when it becomes useful
            foreach (var pair in this.entries) ctx.entries.Add(pair);
            return ctx;
        }
        private static Context add_binding(Context ctx, string a, binding b)
        {
            Context r = ctx.copy(); // this is the only place copy is called. as it should be?
            r.entries.Insert(0, (a, b));
            return r;
        }
        /// <summary>
        /// Creates new context with the variable, or a prime of the variable if it's already in the previous context. Returns a pair: (new_context, new_variable) 
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="x"></param>
        /// <returns></returns>
        public static (Context, string) pick_fresh_name(Context ctx, string x)
        {
            StringBuilder test = new StringBuilder(x);
            while (ctx.is_name_bound(test.ToString())) test.Append("'");

            // UPDATE: made these all static since they return a copy. No more returning same context
            return (add_name(ctx, test.ToString()), test.ToString()); 
        }
        /// <summary>
        /// Creates a new context which is a copy of the old one, with an additional entry: the new variable.
        /// </summary>
        /// <param name="a"></param>
        /// <returns></returns>
        public static Context add_name(Context ctx, string a) => add_binding(ctx, a, binding.newNameBind());



        //   I N T E R N A L 
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("Context: [");
            foreach (var con in this.entries) sb.Append(con.Item1 + ", ");
            sb.Append("]");
            return sb.ToString();
        }
    }
    public class term
    {
        //   D A T A 
        public enum Tag { Var, Abs, App, Error };
        public readonly Tag tag;
        //   type V A R 
        public readonly int deBr_ind; // variable
        public readonly int DBG_CTX_LEN; // for debugging deBruijn impl 
        //   type A B S   or   A P P 
        public readonly term left; // left half of APP
        public readonly term right; // used as right half of APP or as body of ABS
        public readonly string lexeme; // for re-creating similar names (part of ABS), also ERROR message
        //   properties 
        public bool isval => tag == Tag.Abs;
        private term (Tag tag, int deBruijnIndex, int DBG_CTX_LET, string prev_name, term left, term right)
        {
            this.tag = tag;
            this.deBr_ind = deBruijnIndex;
            this.DBG_CTX_LEN = DBG_CTX_LET;
            this.lexeme = prev_name;
            this.left = left;
            this.right = right;
        }
        //   C O N S T R U C T O R S 
        public static term newVar(int deBruijnIndex, int DBG_CTX_LET) => new term(Tag.Var, deBruijnIndex, DBG_CTX_LET, null, null, null);
        public static term newAbs(string prev_name, term body) => new term(Tag.Abs, -9999, -9999, prev_name, null, body);
        public static term newApp(term left, term right) => new term(Tag.App, -9999, -9999, null, left, right);
        public static term newError(string m) => new term(Tag.Error, -9999, -9999, m, null, null);
        //   S T A T I C   M E T H O D S 
        public static term shift(term t, int d, int cutoff=0) // returns a new term, with variables shitfed by d (distance, usually 1)
        {
            switch (t.tag) // TODO: see if i can turn this into a loop
            {
                case Tag.Var:
                    var hmm = t.deBr_ind >= cutoff ? d : 0;
                    return term.newVar(t.deBr_ind + hmm, t.DBG_CTX_LEN + d); // we add d to the DBG_CTX_LEN to show that the context size increased
                case Tag.Abs:
                    return term.newAbs(t.lexeme, shift(t.right, d, cutoff + 1));
                case Tag.App:
                    return term.newApp(shift(t.left, d, cutoff), shift(t.right, d, cutoff));
                default: throw new Exception("bruh moment in shift"); // unreachable :P
            }
        }
        public term subst(int j, term s)
        {
            term walk(int cut, term t) // TODO: figure out whats going on here
            {
                switch (t.tag)
                {
                    case Tag.Var: // j is the variable's debInd. add the total cutoff (nested lambda depth) 
                        // for example, w = 7, \.\.\.9  => \.\.\.w since 0,1,2 for the lambdas, so 9 for w.
                        // shift s by cutoff all at once instead of doing it incrementally upon entering one ABS. 
                        if (t.deBr_ind == j + cut) return shift(s, cut);
                        else return term.newVar(t.deBr_ind, t.DBG_CTX_LEN);
                    case Tag.Abs:
                        return term.newAbs(t.lexeme, walk(cut + 1, t.right));
                    case Tag.App:
                        return term.newApp(walk(cut, t.left), walk(cut, t.right));
                    default:
                        throw new Exception("bruh moment in subst/walk"); // unreachable
                }
            }
            return walk(0, this);
        }
        //   P U B L I C   M E T H O D S
        public term beta_reduce(term s) => shift(this.subst(0, shift(s, 1)), -1);
        public term eval1(Context ctx)
        {
            if (this.tag == Tag.App)
            {
                // case 1 - both sides are ABS, so we beta-reduce
                if (this.left.tag == Tag.Abs && this.right.isval) 
                {
                    return this.left.right.beta_reduce(this.right);
                }
                // case 2 - this is the case when left is a value, aka ABS, so the semantics say 
                // to evaluate the right side, and return an APP(left, new_right). 
                var t2_ = this.right.eval1(ctx); 
                if (t2_.tag == Tag.Error && t2_.lexeme == "NoRuleApplies") return t2_;
                if (this.left.isval) return term.newApp(this.left, t2_);

                // case 3 - neither left nor right is normal, we evaluate them both, return APP(new_left, new_right)
                var t1_ = this.left.eval1(ctx); 
                if (t1_.tag == Tag.Error && t1_.lexeme == "NoRuleApplies") return t1_;
                return term.newApp(t1_, this.right);
            }
            else return term.newError("NoRuleApplies");
        }
        public term eval(Context ctx)
        {
            term t = this.eval1(ctx);
            if (t.tag == Tag.Error && t.lexeme == "NoRuleApplies") return this; // early termination, for normal forms
            else return t.eval(ctx);
        }
    }
    class Program
    {
        static void Main(string[] args)
        {
            { // basic tests
                var ctx1 = new Context();
                ctx1 = Context.add_name(ctx1, "un");
                ctx1 = Context.add_name(ctx1, "deux");
                ctx1 = Context.add_name(ctx1, "trois");

                { // name 2 index 
                    var t1 = ctx1.name_2_index("un");
                    var t2 = ctx1.name_2_index("trois");
                    test(t1 == 2); // should be 2 - TEST 0 
                    test(t2 == 0); // should be 0 
                }
                { // index 2 name
                    var t1 = ctx1.index_2_name(0);
                    var t2 = ctx1.index_2_name(2);
                    test(t1 == "trois"); // should be "trois" - TEST 2
                    test(t2 == "un"); // should be "un"
                }
                { // is name bound 
                    var t1 = ctx1.is_name_bound("trois");
                    var t2 = ctx1.is_name_bound("un");
                    var t3 = ctx1.is_name_bound("soyboy");
                    test(t1 == true); // true - TEST 4
                    test(t2 == true); // true 
                    test(t3 == false); // false 
                }

                { // pick fresh name 
                    var t1 = Context.pick_fresh_name(ctx1, "trois");
                    var t2 = Context.pick_fresh_name(ctx1, "soyboy");
                    var t3 = Context.pick_fresh_name(ctx1, "trois");
                    test(t1.Item2 == "trois'"); // should be trois' - TEST 7
                    test(t2.Item2 == "soyboy"); // should be soyboy
                    test(t3.Item2 == "trois'"); // should be trois''
                }
            }



            var ctx = new Context();
            { // example 1    (λx. x) (λx. x x)
                var left = term.newAbs("x", term.newVar(0, 1));
                var right = term.newAbs("x", term.newApp(term.newVar(0, 1), term.newVar(0, 1)));
                var t_ = term.newApp(left, right);
                var t = Context.print_tm(t_, ctx);
                test(t == @"((\ x. x) (\ x. (x x)))"); // - TEST 10 

                var t2 = Context.print_tm(t_.eval(ctx), ctx);
                Console.WriteLine(t2);
            }
            { // example 2     (λy. y) (λz. z)
                var left = term.newAbs("y", term.newVar(0, 1));
                var right = term.newAbs("z", term.newVar(0, 1));
                var t_ = term.newApp(left, right);
                var t = Context.print_tm(t_, ctx);

                test(t == @"((\ y. y) (\ z. z))"); // - TEST 11

                var t2 = Context.print_tm(t_.eval(ctx), ctx);
                Console.WriteLine(t2);
            }
            { // example 3     λx. (λy. x y) x
                var left = term.newAbs("y", term.newApp(term.newVar(1, 2), term.newVar(0, 2)));
                var right = term.newVar(0, 1);
                var t_ = term.newAbs("x", term.newApp(left, right));
                var t = Context.print_tm(t_, ctx);
                test(t == @"(\ x. ((\ y. (x y)) x))"); // - TEST 12

                var t2 = Context.print_tm(t_.eval(ctx), ctx);
                Console.WriteLine(t2);
            }
            { // example 4        (λx.λy. x y) (λy. y)
                var right = term.newAbs("y", term.newVar(0, 1));
                var left = term.newAbs("x", term.newAbs("y", term.newApp(term.newVar(1, 2), term.newVar(0, 2))));
                var t_ = term.newApp(left, right);
                var t = Context.print_tm(t_, ctx);
                test(t == @"((\ x. (\ y. (x y))) (\ y. y))"); // - TEST 13

                var t2 = Context.print_tm(t_.eval(ctx), ctx);
                Console.WriteLine(t2);
            }
            { // example 5        λx.(x (λx. x)) 
                var right = term.newAbs("x", term.newVar(0, 2));
                var left = term.newVar(0, 1);
                var body = term.newApp(left, right);
                var t_ = term.newAbs("x", body);
                var t = Context.print_tm(t_, ctx);
                Console.WriteLine(t);
                var t2 = Context.print_tm(t_.eval(ctx), ctx);
                Console.WriteLine(t2);
            }
            { // example 6     (x x)   with ctx = [x]
                var ctx2 = Context.add_name(ctx, "x");
                
                var right = term.newVar(0, 1);
                var left = term.newVar(0, 1);
                var t_ = term.newApp(left, right);
                var t = Context.print_tm(t_, ctx2);
                Console.WriteLine(t);
                var t2 = Context.print_tm(t_.eval(ctx2), ctx2);
                Console.WriteLine(t2);
            }
            { // example 7     (\x.x x)   with ctx = [x]
                var ctx2 = Context.add_name(ctx, "x");

                var left = term.newAbs("x", term.newVar(0, 2));
                var right = term.newVar(0, 1);
                var t_ = term.newApp(left, right);
                var t = Context.print_tm(t_, ctx2);
                Console.WriteLine(t);
                var t2 = Context.print_tm(t_.eval(ctx2), ctx2);
                Console.WriteLine(t2);
            }
            { // example 8       ((\x.x \x.x) (\x.x \x.x))
                var ctx2 = new Context();

                var LLL = term.newAbs("x", term.newVar(0, 1));
                var LLR = term.newAbs("x", term.newVar(0, 1));
                var LL = term.newApp(LLL, LLR);
                var LRL = term.newAbs("x", term.newVar(0, 1));
                var LRR = term.newAbs("x", term.newVar(0, 1));
                var LR = term.newApp(LRL, LRR);
                var L = term.newApp(LL, LR);

                var RLL = term.newAbs("x", term.newVar(0, 1));
                var RLR = term.newAbs("x", term.newVar(0, 1));
                var RL = term.newApp(RLL, RLR);
                var RRL = term.newAbs("x", term.newVar(0, 1));
                var RRR = term.newAbs("x", term.newVar(0, 1));
                var RR = term.newApp(RRL, RRR);
                var R = term.newApp(RL, RR);

                var t_ = term.newApp(RL, L);

                var t = Context.print_tm(t_, ctx2);
                Console.WriteLine(t);
                var t2 = Context.print_tm(t_.eval(ctx2), ctx2);
                Console.WriteLine(t2);
            }

            Console.ReadKey();
        }
        static int test_number = 0;
        static void test(bool b)
        {
            if (!b)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Test {test_number.ToString().PadLeft(3, ' ')} failing");
                Console.ForegroundColor = ConsoleColor.White;
            }
            test_number++;
        }
    }
}

/*

B A C K U P S

public term subst(int j, term s)
        {
            term walk(int cut, term t) // TODO: figure out whats going on here
            {
                switch (t.tag)
                {
                    case Tag.Var:
                        if (t.deBr_ind == j + cut) return s.shift(cut);
                        else return term.newVar(t.deBr_ind, t.DBG_CTX_LEN);
                    case Tag.Abs:
                        return term.newAbs(t.lexeme, walk(cut + 1, t.right));
                    case Tag.App:
                        return term.newApp(walk(cut, t.left), walk(cut, t.right));
                    default:
                        throw new Exception("bruh moment in subst/walk"); // unreachable
                }
            }
            return walk(0, this);
        }


public term shift_WORKING(int d) 
        {
            term walk(int cutoff, term t) // this is recursive in nature so i cant switch to a while loop :(
            {
                switch (t.tag)
                {
                    case Tag.Var:
                        var hmm = t.deBr_ind >= cutoff ? d : 0;
                        return term.newVar(t.deBr_ind + hmm, t.DBG_CTX_LEN + d); // we add d to the DBG_CTX_LEN to show that the context size increased
                    case Tag.Abs:
                        return term.newAbs(t.lexeme, walk(cutoff + 1, t.right));
                    case Tag.App:
                        return term.newApp(walk(cutoff, t.left), walk(cutoff, t.right));
                    default: throw new Exception("bruh moment in walk/shift"); // unreachable :P
                }
;           }
            return walk(0, this);
        }

*/
