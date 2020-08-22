using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;

namespace simplebool_cs
{
    public class binding
    {
        //   D A T A 
        public enum Tag { NameBind, VarBind }
        public readonly Tag tag;
        public readonly ty type;
        private binding(Tag tag, ty type)
        {
            this.tag = tag;
            this.type = type;
        }
        //   C O N S T R U C T O R S
        public static binding newNameBind() => new binding(Tag.NameBind, null);
        public static binding newVarBind(ty type) => new binding(Tag.VarBind, type);
        //   I N T E R N A L
        public binding copy()
        {
            switch (this.tag)
            {
                case Tag.NameBind: return newNameBind();
                case Tag.VarBind: return newVarBind(this.type.copy());
                default: throw new Exception("bruh moment in binding.copy()"); // unreach
            }
        }
        public override string ToString()
        {
            switch (this.tag)
            {
                case Tag.NameBind: return "<NameBind> binding";
                case Tag.VarBind: return $"<VarBind> binding: type:{type}";
                default: return "<< E R R O R >>"; // unreach
            }
        }
    }
    public class ty
    {
        //   D A T A 
        public enum Tag { TyArr, TyBool }
        readonly public Tag tag;
        readonly public ty left; // FN type - argument
        readonly public ty right; // FN type - return 
        private ty(Tag tag, ty left, ty right)
        {
            this.tag = tag;
            this.left = left;
            this.right = right;
        }
        //   C O N S T R U C T O R S 
        public static ty newBool() => new ty(Tag.TyBool, null, null);
        public static ty newArr(ty left, ty right) => new ty(Tag.TyArr, left, right);
        //   I N T E R N A L 
        public static bool operator ==(ty t1, ty t2)
        {
            switch (t1.tag)
            {
                case Tag.TyBool: return t2.tag == Tag.TyBool;
                case Tag.TyArr: return t2.tag == Tag.TyArr ? ((t1.left == t2.left) && (t1.right == t2.right)) : false;
                default: return false;
            }
        }
        public static bool operator !=(ty t1, ty t2) => !(t1 == t2);
        public ty copy()
        {
            switch (this.tag)
            {
                case Tag.TyBool: return newBool();
                case Tag.TyArr: return newArr(this.left.copy(), this.right.copy());
                default: throw new Exception("bruh moment in ty.copy()"); // unreach 
            }
        }
        public override string ToString()
        {
            switch (this.tag)
            {
                case Tag.TyBool: return "B";
                case Tag.TyArr: return $"({left} -> {right})";
                default: return "<< E R R O R >>"; // unreach
            }
        }
        public string print_ty() => this.print_ty_ArrowType(); // this painful set of fns is just so the type doesnt have outer braces
        public string print_ty_ArrowType()
        {
            switch (this.tag)
            {
                case Tag.TyArr: return this.left.print_ty_AType() + " -> " + this.right.print_ty_ArrowType();
                default: return this.print_ty_AType();
            }
        }
        public string print_ty_AType()
        {
            switch (this.tag)
            {
                case Tag.TyBool: return "Bool";
                default: return "(" + this.print_ty() + ")";
            }
        }
    }
    public class term
    {
        //   D A T A 
        public enum Tag { Var, Abs, App, True, False, If, Error }
        public readonly Tag tag;
        // var 
        public readonly int deBr_ind; // variable
        public readonly int DBG_CTX_LEN; // for debugging deBruijn impl 
        // abs 
        public readonly string lexeme; // prev name - for printing & reconstructing variable names (also ERROR)
        public readonly ty type; // 
        // app 
        public readonly term left; // the left part of an APP
        public readonly term right; // right part of APP or ABS 
        // bool
        public readonly bool b; // for booleans 
        // If 
        public readonly term if_clause; // the if has parts: if <if_clause> then <left> else <right> 
        // Properties 
        public bool isval => (tag == Tag.True || tag == Tag.False || tag == Tag.Abs);
        private term(Tag tag, int deBr_ind, int DBG_CTX_LEN, string lexeme, ty type, term left, term right, bool b, term if_clause)
        {
            this.tag = tag;
            this.deBr_ind = deBr_ind;
            this.DBG_CTX_LEN = DBG_CTX_LEN;
            this.lexeme = lexeme;
            this.type = type;
            this.left = left;
            this.right = right;
            this.b = b;
            this.if_clause = if_clause;
        }
        //   C O N S T R U C T O R S 
        public static term newVar(int deBr_ind, int DBG_CTX_LEN) => new term(Tag.Var, deBr_ind, DBG_CTX_LEN, null, null, null, null, false, null);
        public static term newAbs(string prev_name, ty type, term body) => new term(Tag.Abs, -9999, -9999, prev_name, type, null, body, false, null);
        public static term newApp(term left, term right) => new term(Tag.App, -9999, -9999, null, null, left, right, false, null);
        public static term newError(string m) => new term(Tag.Error, -9999, -9999, m, null, null, null, false, null);
        public static term newTrue() => new term(Tag.True, -9999, -9999, null, null, null, null, true, null);
        public static term newFalse() => new term(Tag.False, -9999, -9999, null, null, null, null, false, null);
        public static term newIf(term if_clause, term then_clause, term else_clause) => new term(Tag.If, -9999, -9999, null, null, then_clause, else_clause, false, if_clause);
        //   S T A T I C   M E T H O D S - static because its confusing with t, s.. plus they return new terms
        public static term shift(int d, term t, int cutoff = 0) // returns a new term, with variables shitfed by d (distance, usually 1)
        {
            switch (t.tag) 
            {
                case Tag.Var:
                    var hmm = t.deBr_ind >= cutoff ? d : 0;
                    return term.newVar(t.deBr_ind + hmm, t.DBG_CTX_LEN + d); // we add d to the DBG_CTX_LEN to show that the context size increased
                case Tag.Abs:
                    return term.newAbs(t.lexeme, t.type, shift( d, t.right, cutoff + 1));
                case Tag.App:
                    return term.newApp(shift(d, t.left, cutoff), shift( d, t.right, cutoff));
                case Tag.If: 
                    return term.newIf(shift(d, t.if_clause, cutoff), shift(d, t.left, cutoff), shift(d, t.right, cutoff));
                case Tag.True: return t;
                case Tag.False: return t;
                default: throw new Exception("bruh moment in shift"); // unreachable :P
            }
        }
        public static term subst(int j, term s, term t, int cutoff = 0)
        {
            switch (t.tag)
            {
                case Tag.Var:
                    if (t.deBr_ind == j + cutoff) return shift(cutoff, s); // this is where we shift all at once, hence cutoff going in as d
                    else return term.newVar(t.deBr_ind, t.DBG_CTX_LEN);
                case Tag.Abs:
                    return term.newAbs(t.lexeme, t.type, subst(j, s, t.right, cutoff + 1)); // TODO: probably passing in wrong args
                case Tag.App:
                    return term.newApp(subst(j, s, t.left, cutoff), subst(j, s, t.right, cutoff));
                case Tag.If: 
                    return term.newIf(subst(j, s, t.if_clause, cutoff), subst(j, s, t.left, cutoff), subst(j, s, t.right, cutoff));
                case Tag.True: return t;
                case Tag.False: return t;
                default: throw new Exception("bruh moment in subst"); // unreachable
            }
        }
        public static term termSubstTop(term s, term t) => shift(-1, subst(0, shift(1, s), t)); // aka Beta Reduce
        //   I N T E R N A L 
        public override string ToString()
        {
            switch (this.tag)
            {
                case Tag.Var: return this.deBr_ind;//$"Variable-term: prev_name:{lexeme}, index:{deBr_ind}, ctx:{DBG_CTX_LEN}";
                case Tag.Abs: return $"\\ {lexeme}:{type}. {right}";
                case Tag.App: return $"({left} {right})";
                case Tag.True: return "true";
                case Tag.False: return "false";
                case Tag.If: return $"if {if_clause} then ({left}) else ({right})";
                default: return "<< E R R O R >>"; //unreach
            }
        }
        //   P U B L I C
        public term eval1(Context ctx) // remember to propagate error in recursive calls - same effect as try/with in F#
        {
            switch (this.tag)
            {
                case Tag.App:
                    // case 1 - left is ABS, right is val, so we beta-reduce
                    if (this.left.tag == Tag.Abs && this.right.isval)
                    {
                        return termSubstTop(this.right, this.left.right);
                    }
                    // case 2 - left is a value, semantics say evaluate right side, return APP(left, new_right). 
                    if (this.left.isval)
                    {
                        var t2_ = this.right.eval1(ctx);
                        if (t2_.tag == Tag.Error && t2_.lexeme == "NoRuleApplies") return t2_;
                        return term.newApp(this.left, t2_);
                    }
                    // case 3 - left is not normal, we evaluate left side first, return APP(new_left, right)
                    var t1_ = this.left.eval1(ctx);
                    if (t1_.tag == Tag.Error && t1_.lexeme == "NoRuleApplies") return t1_;
                    return term.newApp(t1_, this.right);
                case Tag.If:
                    switch (this.if_clause.tag)
                    {
                        case Tag.True: return this.left; // E-IfTrue
                        case Tag.False: return this.right; // E-IfFalse
                        default:
                            var if_step = this.if_clause.eval1(ctx);
                            if (if_step.tag == Tag.Error && if_step.lexeme == "NoRuleApplies") return if_step;
                            return term.newIf(if_step, this.left, this.right); // E-If -> Congruence rule
                    }
                default:
                    break;
            }
            return term.newError("NoRuleApplies");
        }
        public term eval(Context ctx)
        {
            term t = this.eval1(ctx);
            if (t.tag == Tag.Error && t.lexeme == "NoRuleApplies") return this; // early termination, for normal forms
            else return t.eval(ctx);
        }
        public ty type_of(Context ctx)
        {
            switch (this.tag)
            {
                case Tag.Var: return ctx.getTypeFromContext(this.deBr_ind);
                case Tag.Abs:
                    var ctx_ = Context.add_binding(ctx, this.lexeme, binding.newVarBind(this.type)); // new ctx with x:T in it
                    return ty.newArr(this.type, this.right.type_of(ctx_)); // \x:T.t2 => yields type:  T -> typeof(t2)
                case Tag.App:
                    var tyT1 = this.left.type_of(ctx); // type of the left side
                    var tyT2 = this.right.type_of(ctx);// type of the right side 
                    switch (tyT1.tag)
                    {
                        case ty.Tag.TyArr:
                            var tyT11 = tyT1.left;  // fn arg type
                            var tyT12 = tyT1.right; // fn ret type
                            if (tyT2 == tyT11) return tyT12; // input type matches arg type, return result_type of function
                            else throw new Exception("parameter type mismatch");
                        default: throw new Exception("arrow type expected");
                    }
                case Tag.True: return ty.newBool();
                case Tag.False: return ty.newBool();
                case Tag.If:
                    if (this.if_clause.type_of(ctx).tag == ty.Tag.TyBool)
                    {
                        var typeT2 = this.left.type_of(ctx);
                        if (typeT2.tag == this.right.type_of(ctx).tag) return typeT2;
                        else throw new Exception("arms of conditional have different types");
                    }
                    else throw new Exception("guard of conditional not a boolean");
                default: break;
            }
            throw new Exception("bruh moment in type_of");
        }
        public string print_tm(Context ctx)
        {
            switch (this.tag)
            {
                case Tag.Abs:
                    var (ctx_, x_) = Context.pick_fresh_name(ctx, this.lexeme);
                    return $"\\ {x_}:{this.type.print_ty()}. {this.right.print_tm(ctx_)}";
                case Tag.If:
                    return $"If {this.if_clause.print_tm(ctx)} then {this.left.print_tm(ctx)} else {this.right.print_tm(ctx)}";
                default: return this.print_tm_AppTerm(ctx);
            }
        }
        public string print_tm_AppTerm(Context ctx)
        {
            switch (this.tag)
            {
                case Tag.App: return $"{this.left.print_tm_AppTerm(ctx)} {this.right.print_tm_ATerm(ctx)})";
                default: return this.print_tm_ATerm(ctx);
            }
        }
        public string print_tm_ATerm(Context ctx)
        {
            switch (this.tag)
            {
                case Tag.Var:
                    if (ctx.ctx_length == this.DBG_CTX_LEN) return ctx.index_2_name(this.deBr_ind);
                    else return "[bad index]";
                case Tag.True: return "true";
                case Tag.False: return "false";
                default: return $"({this.print_tm(ctx)})";
            }
        }
    }
    public class Context
    {
        //   D A T A 
        private List<(string, binding)> entries;
        public int ctx_length => entries.Count;
        //   C O N S T R U C T O R 
        public Context() => entries = new List<(string, binding)>();
        //   S T A T I C   M E T H O D S 
        public static Context add_name(Context ctx, string a) => add_binding(ctx, a, binding.newNameBind());
        public static Context add_binding(Context ctx, string a, binding b) // static because it returns new Context
        {
            Context r = ctx.copy(); // this is the only place copy is called. as it should be?
            r.entries.Insert(0, (a, b));
            return r;
        }
        public static (Context, string) pick_fresh_name(Context ctx, string x) // static bc it returns new ctx
        {
            StringBuilder x_new = new StringBuilder(x);
            while (ctx.is_name_bound(x_new.ToString())) x_new.Append("'"); // add primes to name if it's in ctx
            return (add_name(ctx, x_new.ToString()), x_new.ToString()); // return new context & primed name 
        }
        public ty getTypeFromContext(int i)
        {
            var soy = this.get_binding(i);
            if (soy.tag == binding.Tag.VarBind) return soy.type;
            else throw new Exception("Type Error: getTypeFromContext: wrong kind of binding for variable");
        }
        public binding get_binding(int i) => entries[i].Item2;
        //   I N T E R N A L 
        private Context copy()
        {
            Context ctx = new Context();
            foreach (var pair in this.entries)
            {
                var new_pair = (pair.Item1, pair.Item2.copy()); // TODO: do we really need to copy the binding?
                ctx.entries.Add(new_pair);
            }
            return ctx;
        }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("Context: [");
            foreach (var con in this.entries) sb.Append(con.Item1 + ", ");
            sb.Append("]");
            return sb.ToString();
        }
        //   P U B L I C   M E T H O D S 
        public bool is_name_bound(string s) => ctx_length == 0 ? false : entries.Select(x => x.Item1 == s).Aggregate((x, y) => x || y);
        public string index_2_name(int i) => entries[i].Item1;
    }
    class Program
    {
        static void test(term t, Context ctx)
        {
            var soy = $"{t.eval(ctx).print_tm(ctx)}\t:\t{t.type_of(ctx).print_ty()}"; // PERFECT
            Console.WriteLine(soy);
        }
        static void Main(string[] args)
        {
            var ctx = new Context();


            /* test should evaluate to this:
            
            \ x:Bool. x     :       Bool -> Bool
            \ x:Bool -> Bool. if x false then true else false       :       (Bool -> Bool) -> Bool
            \ x:Bool. if x then false else true     :       Bool -> Bool
            true    :       Bool
            */
            {
                // "λ x:Bool.x "
                var t = term.newAbs("x", ty.newBool(), term.newVar(0, 1));
                test(t, ctx);
            }
            {
                // "(λ x:Bool->Bool. if x false then true else false)"
                //let t = TmAbs("x", TyArr(TyBool, TyBool), TmIf(TmApp(TmVar(0, 1), TmFalse), TmTrue, TmFalse))
                var t = term.newAbs("x", ty.newArr(ty.newBool(), ty.newBool()), term.newIf(term.newApp(term.newVar(0, 1), term.newFalse()), term.newTrue(), term.newFalse()));
                test(t, ctx);
            }
            {
                // "(λ x:Bool. if x then false else true)"
                var t = term.newAbs("x", ty.newBool(), term.newIf(term.newVar(0, 1), term.newFalse(), term.newTrue()));
                test(t, ctx);
            }
            {
                // "(λ x:Bool->Bool. if x false then true else false) (λ x:Bool. if x then false else true)"
                var left = term.newAbs("x", ty.newArr(ty.newBool(), ty.newBool()), term.newIf(term.newApp(term.newVar(0, 1), term.newFalse()), term.newTrue(), term.newFalse()));
                var right = term.newAbs("x", ty.newBool(), term.newIf(term.newVar(0,1),term.newFalse(), term.newTrue()));
                var t = term.newApp(left, right);
                test(t, ctx);
            }

            Console.ReadKey();
        }
    }
}
