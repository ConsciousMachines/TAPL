using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

// TODO: is there some syntax for binding a variable?

namespace rcdsubbot_cs
{
    public class binding
    {
        public enum Tag { NameBind, VarBind }
        public Tag tag;
        public readonly ty type; // for VarBind
        private binding(Tag tag, ty type)
        {
            this.tag = tag;
            this.type = type;
        }
        public static binding newNameBind() => new binding(Tag.NameBind, null);
        public static binding newVarBind(ty type) => new binding(Tag.VarBind, type);
        //   I N T E R N A L
        public override string ToString()
        {
            switch (this.tag)
            {
                case Tag.NameBind: return "NameBind";
                case Tag.VarBind: return $"VarBind:{type}";
                default: return "<< ERROR IN BINDING TOSTR() >>";
            }
        }
    }
    public class ty
    {
        public enum Tag { Top, Bot, Record, Arr, Error }
        public readonly Tag tag;
        // Arrow
        public readonly ty left;
        public readonly ty right;
        // Record 
        public readonly List<(string, ty)> entries;
        // Error 
        string lexeme;
        //   C O N S T R U C T O R 
        private ty (Tag tag, ty ty1, ty ty2, List<(string, ty)> entries, string m)
        {
            this.tag = tag;
            this.left = ty1;
            this.right = ty2;
            this.entries = entries;
            this.lexeme = m;
        }
        public static ty newTop() => new ty(Tag.Top, null, null, null, null);
        public static ty newBot() => new ty(Tag.Bot, null, null, null, null);
        public static ty newRecord(List<(string, ty)> entries) => new ty(Tag.Record, null, null, entries, null);
        public static ty newArr(ty ty1, ty ty2) => new ty(Tag.Arr, ty1, ty2, null, null);
        public static ty newError(string m) => new ty(Tag.Error, null, null, null, m);
        //   I N T E R N A L
        public static bool operator ==(ty t1, ty t2)
        {
            switch (t1.tag)
            {
                case Tag.Top: return t2.tag == Tag.Top;
                case Tag.Bot: return t2.tag == Tag.Bot;
                case Tag.Error: throw new Exception("comparing error types, wtf bruv");
                case Tag.Record:
                    if (t1.entries.Count != t2.entries.Count) return false;
                    for (int i = 0; i < t1.entries.Count; i++)
                    {
                        if (t1.entries[i] != t2.entries[i]) return false;
                    }
                    return true;
                case Tag.Arr: return t2.tag == Tag.Arr ? ((t1.left == t2.left) && (t1.right == t2.right)) : false;
                default: return false;
            }
        }
        public static bool operator !=(ty t1, ty t2) => !(t1 == t2);
        public override string ToString()
        {
            switch (this.tag)
            {
                case Tag.Top: return "Top";
                case Tag.Bot: return "Bot";
                case Tag.Arr: return $"({left} -> {right})";
                case Tag.Error: return $"ERROR:{lexeme}";
                case Tag.Record:
                    StringBuilder sb = new StringBuilder("[");
                    foreach (var ele in entries)
                    {
                        sb.Append(ele.Item1);
                        sb.Append(":");
                        sb.Append(ele.Item2);
                        sb.Append(", ");

                    }
                    sb.Append("]");
                    return sb.ToString();
                default: return "<< ERROR IN TY TOSTR() >>";
            }
        }
        public ty findRecordElement(string m)
        {
            if (this.tag == Tag.Record) foreach (var ele in this.entries) if (ele.Item1 == m) return ele.Item2;
            return ty.newError("failed to find element");
        }
        //   S T A T I C 
        public static bool subtype(ty tyS, ty tyT)
        {
            if (tyS == tyT) return true;
            // case 1: tyT is a Top
            else if (tyT.tag == Tag.Top) return true;
            // case 2: tyS is a Bot
            else if (tyS.tag == Tag.Bot) return true;
            // case 3: Arrow 
            else if (tyS.tag == Tag.Arr && tyT.tag == Tag.Arr) return subtype(tyT.left, tyS.left) && subtype(tyS.right, tyT.right);
            // case 4: record 
            else if (tyS.tag == Tag.Record && tyT.tag == Tag.Record)
            {
                var fT = tyT.entries;

                bool walk(string li, ty tyTi)
                {
                    // try find item in list 
                    var tySi = tyS.findRecordElement(li);
                    if (tySi.tag == Tag.Error && tySi.lexeme == "failed to find element") return false;
                    return subtype(tySi, tyTi);
                }
                return fT.Select(x => walk(x.Item1, x.Item2)).Aggregate((x, y) => x && y);
            }
            else return false;
        }
        public static ty type_of(Context ctx, term t)
        {
            switch (t.tag)
            {
                case term.Tag.Record:
                    var fieldtys = t.entries.Select(x => (x.Item1, type_of(ctx, x.Item2))).ToList();
                    return ty.newRecord(fieldtys);
                case term.Tag.Var:
                    return ctx.get_type_from_context(t.deBruin);
                case term.Tag.Abs:
                    var ctx_ = Context.add_binding(ctx, t.lexeme, binding.newVarBind(t.type));
                    var ret_type = type_of(ctx_, t.right);
                    return ty.newArr(t.type, ret_type);
                case term.Tag.App:
                    var fn_type = type_of(ctx, t.left);
                    var arg_type = type_of(ctx, t.right);
                    switch (fn_type.tag)
                    {
                        case Tag.Arr:
                            var fn_arg = fn_type.left;
                            var fn_ret = fn_type.right;
                            if (subtype(arg_type, fn_arg)) return fn_ret;
                            else return ty.newError("parameter type mismatch");
                        case Tag.Bot:
                            return ty.newBot();
                        default:
                            return ty.newError("arrow type expected");
                    }
                case term.Tag.Proj:
                    var left_side = type_of(ctx, t.left);
                    switch (left_side.tag)
                    {
                        case Tag.Record:
                            // try find the field in fieldtys
                            var result = left_side.findRecordElement(t.lexeme);
                            if (result.tag == Tag.Error) return ty.newError($"label {t.lexeme} not found");
                            return result;
                        case Tag.Bot: return ty.newBot();
                        default: return ty.newError("Expected record type");
                    }
                default:
                    return ty.newError("UNIDENTIFIED TYPE");
            }
        }
    }
    public class term
    { 
        public enum Tag { Var, Abs, App, Record, Proj, Error }
        public readonly Tag tag;
        // Var 
        public readonly int deBruin;
        public readonly int DBG_CTX_LEN;
        // Abs 
        public readonly string lexeme; // also for error 
        public readonly ty type;
        // App
        public readonly term left;
        public readonly term right; // right side of App or Abs 
        // Record 
        public readonly List<(string, term)> entries;
        // Projection will use the string m for the "method", and the term will be the left one. 
        public bool isval()
        {
            if (tag == Tag.Abs) return true;
            else if (tag == Tag.Record)
            {
                if (this.entries.Count == 0) throw new Exception("record is empty bruv"); // not sure wat else to do 
                return entries.Select(x => x.Item2.isval()).Aggregate((x, y) => x && y);
            }
            else return false;
        }
        //   C O N S T R U C T O R S
        private term(Tag tag, int deBruin, int DBG_CTX_LEN, string m, ty type, term left, term right ,List<(string, term)> entries)
        {
            this.tag = tag;
            this.deBruin = deBruin;
            this.DBG_CTX_LEN = DBG_CTX_LEN;
            this.lexeme = m;
            this.type = type;
            this.left = left;
            this.right = right;
            this.entries = entries;
        }
        public static term newVar(int deBruin, int DBG_CTX_LEN) => new term(Tag.Var, deBruin, DBG_CTX_LEN, null, null, null, null, null);
        public static term newAbs(string name, ty type, term body) => new term(Tag.Abs, -9999, -9999, name, type, null, body, null);
        public static term newApp(term left, term right) => new term(Tag.App, -9999, -9999, null, null, left, right, null);
        public static term newRecord(List<(string, term)> entries) => new term(Tag.Record, -9999, -9999, null, null, null, null, entries);
        public static term newProj(term left, string method) => new term(Tag.Proj, -9999, -9999, method, null, left, null, null);
        public static term newError(string m) => new term(Tag.Error, -9999, -9999, m, null, null, null, null);
        //   I N T E R N A L
        public override string ToString()
        {
            switch (this.tag)
            {
                case Tag.Var: return $"v{deBruin}";
                case Tag.Abs: return $"(\\ {lexeme}:{type}. {right})";
                case Tag.App: return $"({left} {right})";
                case Tag.Proj: return $"{left}.{lexeme}";
                case Tag.Record:
                    StringBuilder sb = new StringBuilder("[");
                    foreach (var ele in entries)
                    {
                        sb.Append(ele.Item1);
                        sb.Append(":");
                        sb.Append(ele.Item2);
                    }
                    sb.Append("]");
                    return sb.ToString();
                default: return "<< ERROR IN TERM TOSTR() >>";
            }
        }
        //   S T A T I C   M E T H O D S - static because they return new terms
        public static term term_map(Func<int, int, int, term> on_var, int c, term t)
        {
            term walk(int c, term t) 
            {
                switch (t.tag)
                {
                    case Tag.Var: return on_var(c, t.deBruin, t.DBG_CTX_LEN); 
                    case Tag.Abs: return term.newAbs(t.lexeme, t.type, walk(c + 1, t.right));
                    case Tag.App: return term.newApp(walk(c, t.left), walk(c, t.right));
                    case Tag.Proj: return term.newProj(walk(c, t.left), t.lexeme);
                    case Tag.Record: return term.newRecord(t.entries.Select(e => (e.Item1, walk(c, e.Item2))).ToList());
                    default: throw new Exception("bruh moment in term_map");
                }
            }
            return walk(c, t);
        }
        public static term termShiftAbove(int d, int c, term t) => term_map((c, x, n) => x >= c ? term.newVar(x + d, n + d) : term.newVar(x, n + d), c, t);
        public static term termShift(int d, term t) => termShiftAbove(d, 0, t);
        public static term termSubst(int j, term s, term t) => term_map((j, x, n) => x == j ? termShift(j, s) : term.newVar(x, n), j, t);
        public static term beta_reduce(term s, term t) => termShift(-1, termSubst(0, termShift(1, s), t));
        //   P U B L I C 
        public term findRecordElement(string m)
        {
            if (this.tag == Tag.Record) foreach (var ele in this.entries) if (ele.Item1 == m) return ele.Item2;
            return term.newError("failed to find element");
        }
        private term eval1(Context ctx)
        {
            switch (this.tag)
            {
                case Tag.App:
                    // case 1: left is ABS, right is value 
                    if (this.left.tag == Tag.Abs && right.isval()) return beta_reduce(this.right, this.left.right);
                    // case 2: left is value, take a step for right
                    if (this.left.isval())
                    {
                        var right_step = right.eval1(ctx);
                        // error-checking after each recursive eval!
                        if (right_step.tag == Tag.Error && right_step.lexeme == "NoRuleApplies") return right_step;
                        return term.newApp(left, right_step);
                    }
                    // case 3: take step for left side, which is not a value!
                    var left_step = left.eval1(ctx);
                    if (left_step.tag == Tag.Error && left_step.lexeme == "NoRuleApplies") return left_step;
                    return term.newApp(left_step, right);
                case Tag.Record:
                    if (this.entries.Count == 0) return term.newError("NoRuleApplies");
                    for (int i = 0; i < this.entries.Count; i++)
                    {
                        // entry is a value, pass over it. 
                        if (entries[i].Item2.isval()) continue;
                        // otherwise eval term 
                        var term_step = entries[i].Item2.eval1(ctx);
                        if (term_step.tag == Tag.Error && term_step.lexeme == "NoRuleApplies") return term_step;
                        entries[i] = (entries[i].Item1, term_step); // a bit of bullshit since item2 is not an l-val
                    }
                    return term.newError("NoRuleApplies"); // after we eval the whole record, raise error so outer call returns this
                case Tag.Proj:
                    // case 1: left side is a record value
                    if (this.left.tag == Tag.Record && this.left.isval())
                    {
                        var query = this.left.findRecordElement(this.lexeme); // try to find method in record 
                        if (query.tag == Tag.Error && query.lexeme == "failed to find element")
                            return term.newError("NoRuleApplies");
                        else return query;
                    }
                    // case 2: left side is a term, take a step in it
                    var left_step_ = left.eval1(ctx);
                    if (left_step_.tag == Tag.Error && left_step_.lexeme == "NoRuleApplies") return left_step_;
                    return term.newProj(left_step_, this.lexeme);
                default:
                    return term.newError("NoRuleApplies");
            }
        }
        public term eval(Context ctx)
        {
            term t = this.eval1(ctx);
            if (t.tag == Tag.Error && t.lexeme == "NoRuleApplies") return this; // early termination, for normal forms
            else return t.eval(ctx);
        }
    }
    public class Context
    {
        private readonly List<(string, binding)> entries;
        public int ctx_length => this.entries.Count;
        public Context() => this.entries = new List<(string, binding)>();
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
        //   P U B L I C   M E T H O D S 
        public bool is_name_bound(string s) => ctx_length == 0 ? false : entries.Select(x => x.Item1 == s).Aggregate((x, y) => x || y);
        public string index_2_name(int i) => entries[i].Item1;
        public int name_2_index(string x)
        {
            for (int i = 0; i < ctx_length; i++)
            {
                if (this.entries[i].Item1 == x) return i;
            }
            throw new Exception($"{x} is unbound!");
        }
        public binding get_binding(int i) => entries[i].Item2;
        public ty get_type_from_context(int i)
        {
            var soy = this.get_binding(i);
            if (soy.tag == binding.Tag.VarBind) return soy.type;
            else throw new Exception("Type Error: getTypeFromContext: wrong kind of binding for variable");
        }
        //   I N T E R N A L 
        private Context copy()
        {
            Context ctx = new Context();
            foreach (var pair in this.entries)
            {
                var new_pair = (pair.Item1, pair.Item2); // possibly copy binding here if types change... but they dont!
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
    }
    class rcdsubbot
    {
        static void Main(string[] args)
        {
            var Γ = new Context();

            void test(term t)
            {
                var tyT = ty.type_of(Γ, t);
                var te = t.eval(Γ);
                Console.WriteLine($"{te}\t:\t{tyT}");
            }

            term λ(string name, ty type, term body) => term.newAbs(name, type, body);
            term V(int deBruin) => term.newVar(deBruin, -9999); // DBG not used today
            term App(term left, term right) => term.newApp(left, right);
            term Proj(term left, string method) => term.newProj(left, method);
            var T = ty.newTop();
            var B = ty.newBot();
            var T2T = ty.newArr(T, T);
            var B2B = ty.newArr(T, T);

            //  λx:T. x
            var t2t = λ("x", T, V(0));
            //  λx:B. x
            var b2b = λ("x", B, V(0));

            

            {
                // test 1
                //    { x = (λx:T. x), y = (λx:B. x)}
                var arg = term.newRecord(new List<(string, term)>() { ("x", t2t), ("y", b2b) });
                //    λ r:{x:T->T}. r.x r.x 
                var rec_T2T = ty.newRecord(new List<(string, ty)>() { ("x", ty.newArr(T, T)) }); // type
                var body = App(Proj(V(0), "x"), Proj(V(0), "x")); // body 
                var fn = λ("r", rec_T2T, body);
                //    (λ r:{x:T->T}. r.x r.x)   { x = (λx:T. x), y = (λx:B. x)}       <Application>
                var t = App(fn, arg);
                test(t);
            }
            {
                // test 2 
                // r2 <: r1 by Width
                var r1 = term.newRecord(new List<(string, term)>() { ("Bob", t2t) });
                var r2 = term.newRecord(new List<(string, term)>() { ("Bob", t2t), ("Karl", b2b) });
                Console.WriteLine(ty.subtype(ty.type_of(Γ, r2), ty.type_of(Γ, r1)));

                // s2 <: s1 by Depth 
                var s1 = term.newRecord(new List<(string, term)>() { ("Yui", r1) });
                var s2 = term.newRecord(new List<(string, term)>() { ("Yui", r2) });
                Console.WriteLine(ty.subtype(ty.type_of(Γ, s2), ty.type_of(Γ, s1)));

                // s2 <: s1 by Width & Depth 
                var t1 = term.newRecord(new List<(string, term)>() { ("Yui", r1) });
                var t2 = term.newRecord(new List<(string, term)>() { ("Yui", r2), ("Hachiman", r2) });
                Console.WriteLine(ty.subtype(ty.type_of(Γ, t2), ty.type_of(Γ, t1)));
            }
            {
                // test 3: 
                /*  λ x:{one : T, two : T, three : T}. {x.one, x.three }
                 *  λ has type S1 -> S2, {T,T,T}->{T,T}
                 *  we want to check if λ is a subtype of {T,T,T,T}->{T}.
                 *  can also check {T,B,T,B}->{T} is a subtype of λ.
                 *           T2                         {T}
                 *           |                           | 
                 *     S1 -> S2             {T,T,T} -> {T, T}
                 *      |                      | 
                 *     T1                  {T,B,T,B}
                 */
                var T2 = ty.newRecord(new List<(string, ty)>() { ("Yui", T) });
                var S2 = ty.newRecord(new List<(string, ty)>() { ("Yui", T), ("Hachiman", T) });
                var S1 = ty.newRecord(new List<(string, ty)>() { ("Yui", T), ("Hachiman", T), ("Yukinon", T) });
                var T1 = ty.newRecord(new List<(string, ty)>() { ("Yui", T), ("Hachiman", T), ("Yukinon", T), ("Saki", T) });
                var S1_S2 = ty.newArr(S1, S2);
                var T1_T2 = ty.newArr(T1, T2);
                Console.WriteLine(ty.subtype(T1, S1));
                Console.WriteLine(ty.subtype(S2, T2));
                Console.WriteLine(ty.subtype(S1_S2, T1_T2));
                

                //     λ x:{T,T,T}. {Yui = x.Yui, Hachiman = x.Hachiman} 
                var fn1 = λ("x", S1, term.newRecord(new List<(string, term)>() { ("Yui", Proj(V(0),"Yui")), ("Hachiman", Proj(V(0), "Hachiman")) }));
                //     λ x:{T,B,T,B}. {Yui = x.Yui}
                var fn2 = λ("x", T1, term.newRecord(new List<(string, term)>() { ("Yui", Proj(V(0),"Yui")) }));
                
                // show that S1->S2 <: T1->T2
                var fn1t = ty.type_of(Γ, fn1);
                var fn2t = ty.type_of(Γ, fn2);
                Console.WriteLine(ty.subtype(fn1t, fn2t));


                // apply the functions to args
                var t2 = term.newRecord(new List<(string, term)>() { ("Yui", t2t) });
                var s2 = term.newRecord(new List<(string, term)>() { ("Yui", t2t), ("Hachiman", t2t) });
                var s1 = term.newRecord(new List<(string, term)>() { ("Yui", t2t), ("Hachiman", t2t), ("Yukinon", t2t) });
                var t1 = term.newRecord(new List<(string, term)>() { ("Yui", t2t), ("Hachiman", b2b), ("Yukinon", t2t), ("Saki", b2b) });

                var app_fn1 = App(fn1, s1);
                var fn1_res_ty = ty.type_of(Γ, app_fn1); // should be S2
                Console.WriteLine(fn1_res_ty);
                Console.WriteLine(S2);
                Console.WriteLine(fn1_res_ty == S2);

                var app_fn2 = App(fn2, t1);
                var fn2_res_ty = ty.type_of(Γ, app_fn2); // should be T2
                Console.WriteLine(fn2_res_ty);
                Console.WriteLine(T2);
                Console.WriteLine(fn2_res_ty == T2);

                // the record field has type T, but nothing can be of type T, so we pass in T->T,
                // which works out since lambdas are values under call-by-value rules (no beta-subs inside lambdas)
                // for B, nothing can be B, and we cant even pass in B->B since B is a subtype of that. so any field of type B can't take anything!

                test(app_fn1);
                test(app_fn2);
            }
            {
                // test 4
                var r = term.newRecord(new List<(string, term)>() { ("Yui", t2t) });
                var rt = ty.newRecord(new List<(string, ty)>() { ("Yui", T) }); // ty.newArr(T, T) works too
                // \r:{Yui:T->T}. r.Yui    - SHOULD WORKD WITH Yui:T as well!!
                var fn = λ("r", rt, Proj(V(0), "Yui"));

                var t = App(fn, r);
                test(t);
            }
            {
                // test 5 - Church encoding  
                term soy;
                //  λx:T. λy:T. x
                var tru = λ("x", T, λ("y", T, V(1)));
                var fls = λ("x", T, λ("y", T, V(0)));
                var fst = λ("p", T, App(V(0), tru));
                var snd = λ("p", T, App(V(0), fls));
                var pair = λ("f", T, λ("s", T, λ("b", T, App(App(V(0), V(2)), V(1)))));

                //   ( Bot, Top ) -> taking fst should yield Bot.
                var pair_b_t = App(App(pair, b2b), t2t);
                soy = App(fst, pair_b_t);
                test(soy);

                // alright! got Church encoding to work. our only values in this language are t2t and b2b, 
                // and also records which are combinations of them. 
                // of course the type checker doesn't agree with this random copy-pasting. a function that 
                // takes a T and returns a B would be a subtype of every function. But we can't instantiate B
                // nor specify explicitly a function's return type so we can't fake untyped-ness here. 
            }

            Console.ReadKey();
        }
    }
}
