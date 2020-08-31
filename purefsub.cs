using System;
using System.Data;
using System.Collections.Generic;
using System.Text;
using System.Linq;


// TODO: figure out how to print mid-evaluation while we have access to the context... lol
// for the full thing im definitely gonna make all the fields private and make a bunch of getters. 


/* F# META CODE GENERATOR FOR ABSTRACT SYNTAX TREE WITHOUT FILE INFO :D 
// to print for CS, change the term constructors and this line in print_lines:
| Eval(_,t) ->  p  ("{\n\t// Test " + i.ToString() + "\n\tvar t = " + (sevprintterm t) + ";\n\ttest(t);\n}")

let test t = 
    let p x = printfn "%A" x in 
    let ctx = [] in 
    let t_ = typeof ctx t in 
    let e = eval ctx t in 
    p t_; 
    p e 

let translate_to_FS () = 
    let p x = printfn "%s" x 
    let inFile = @"C:\Users\pwnag\source\repos\TAPL\purefsub_fs_FULL\test.f" in 

    let ctx = [] 
    let rec sevprinttype typ = 
        match typ with 
        | TyVar(i1,i2) -> "TyVar(" + i1.ToString() + ", " + i2.ToString() + ")"
        | TyAll(s, ty1, ty2) -> "TyAll(\"" + s + "\", " + (sevprinttype ty1) + ", " + (sevprinttype ty2) + ")"
        | TyTop -> "TyTop"
        | TyArr(ty1, ty2) -> "TyArr(" + (sevprinttype ty1) + ", " + (sevprinttype ty2) + ")"

    let rec sevprintterm t = 
        match t with 
        | TmTAbs(_,s,ty,term) -> "TmTAbs(\"" + s + "\", " + (sevprinttype ty) + ", " + (sevprintterm term) + ")"
        | TmTApp(_, term, ty)-> "TmTApp(" + (sevprintterm term) + ", " + (sevprinttype ty) + ")"
        | TmVar(_, i1, i2)-> "TmVar(" + i1.ToString() + ", " + i2.ToString() + ")"
        | TmAbs(_, s,ty,term)-> "TmAbs(\"" + s + "\", " + (sevprinttype ty) + ", " + (sevprintterm term) + ")"
        | TmApp(_, t1, t2)-> "TmApp(" + (sevprintterm t1) + ", " + (sevprintterm t2) + ")"

    let rec print_lines cmds i = 
        match cmds with 
        | [] -> ()
        | x::rest -> 
            match x with 
            | Eval(_,t) ->  p  ("\n// Test " + i.ToString() + "\nlet t = " + (sevprintterm t) + "\nin test t")
            | Bind(_,s, b) -> failwith "soy"
            print_lines rest (i + 1)
        | _ -> failwith "soy"

    let (cmds, _) = parseFile inFile ctx in 
    print_lines cmds 0
    ()

*/

namespace purefsub_cs
{
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
                if (this.entries[i].Item1 == x) return i; // i corresponds with the index - 1st is 0, last is Count-1, etc...
            }
            throw new Exception($"{x} is unbound!");
        }
        public binding get_binding(int i) => binding.bindingShift(i + 1, entries[i].Item2); // TODO: why do we shift the binding up by 1? before we just return the original.
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
        /* gotta figure out a way to incorporate a context into printing.. which is only possible if i evaluate it mid way.. lol
        public string print_type(ty t)
        {
            switch (t.tag)
            {
                case ty.Tag.Var: return this.index_2_name(t.deBruin);
                case ty.Tag.Error: return t.lexeme;
                case ty.Tag.Top: return "Top";
                case ty.Tag.Arr: return $"{print_type(t.left)}->{print_type(t.right)}";

                case ty.Tag.All: return $"A{t.lexeme}. {print_type(t.left)}_{print_type(t.right)}"; // TODO: fix this representation
                default: return "<< E R R O R >>";
            }
        }
        public string print_term(term t)
        {
            switch(t.tag)
            {
                case term.Tag.Var: return Context.pick_fresh_name(this, t.lexeme).Item2;
                case term.Tag.TApp: return $"({print_term(t.left)} {print_type(t.type)})";
                case term.Tag.Error: return t.lexeme;
                case term.Tag.App: return $"({print_term(t.left)} {print_term(t.right)})";
                case term.Tag.Abs: return $"\\{t.lexeme}:{print_type(t.type)} . {print_term(t.right)}";

                case term.Tag.TAbs: return $"\\{t.lexeme}.{print_type(t.type)} . {print_term(t.right)}"; // TODO: FIX 
                default: return "<< E R R O R >>";
            }
        }*/
    }

    public class binding
    {
        public enum Tag { NameBind, TyVarBind, VarBind }
        public readonly Tag tag;
        public readonly ty type;
        private binding(Tag tag, ty type)
        {
            this.tag = tag;
            this.type = type;
        }
        public static binding newNameBind() => new binding(Tag.NameBind, null);
        public static binding newVarBind(ty type) => new binding(Tag.VarBind, type);
        public static binding newTyVarBind(ty type) => new binding(Tag.TyVarBind, type);
        //   I N T E R N A L
        public override string ToString()
        {
            switch (this.tag)
            {
                case Tag.NameBind: return "NameBind";
                case Tag.VarBind: return $"VarBind:{type}";
                case Tag.TyVarBind: return $"TyVarBind:{type}";
                default: return "<< ERROR IN BINDING TOSTR() >>";
            }
        }
        //   S T A T I C
        public static binding bindingShift(int d, binding bind)
        {
            switch (bind.tag)
            {
                case Tag.NameBind: return binding.newNameBind();
                // in this case we have a type variable. for instance inside a ALL binding that 
                // contains type variables in its body, we'd need to indeed shift them all up since the bound variable takes a new slot, 0.
                // for example: t : AX.X->Y, with X,Y type-variables. then Z is introduced into the ctx. so we must shift X and Y up by 1. 
                case Tag.TyVarBind: return binding.newTyVarBind(ty.typeShift(d, bind.type));
                case Tag.VarBind: return binding.newVarBind(ty.typeShift(d, bind.type));
                // we didnt have these features before because we didn't have type variables before. 
                default: throw new Exception("unknown tag in binding shift");
            }
        }
    }
    public class ty
    {
        public enum Tag { Var, All, Top, Arr, Error }
        readonly public Tag tag;
        // Var
        readonly public int deBruin;
        readonly public int DBG_CTX_LEN;
        // Arrow 
        readonly public ty left; // also used by Universal
        readonly public ty right;
        // Universal Quantifier
        readonly public string lexeme; // also used for error
        private ty(Tag tag, int deBruin, int DBG_CTX_LEN, ty left, ty right, string lexeme)
        {
            this.tag = tag;
            this.deBruin = deBruin;
            this.DBG_CTX_LEN = DBG_CTX_LEN;
            this.left = left;
            this.right = right;
            this.lexeme = lexeme;
        }
        //   C O N S T R U C T O R S 
        public static ty newVar(int deBruin, int DBG_CTX_LEN) => new ty(Tag.Var, deBruin, DBG_CTX_LEN, null, null, null);
        public static ty newAll(string lexeme, ty left, ty right) => new ty(Tag.All, -9999, -9999, left, right, lexeme);
        public static ty newTop() => new ty(Tag.Top, -9999, -9999, null, null, null);
        public static ty newArr(ty left, ty right) => new ty(Tag.Arr, -9999, -9999, left, right, null);
        public static ty newError(string m) => new ty(Tag.Error, -9999, -9999, null, null, m);
        //   I N T E R N A L 
        public override string ToString()
        {
            switch (this.tag)
            {
                case Tag.Var: return $"tv{this.deBruin}";
                case Tag.All: return $"Forall {this.lexeme}<:{this.left}. {this.right}"; 
                case Tag.Top: return "Top";
                case Tag.Arr: return $"({this.left}->{this.right})";
                case Tag.Error: return this.lexeme;
                default: return "<< E R R O R >>";
            }
        }
        public static bool operator ==(ty t1, ty t2)
        {
            switch (t1.tag)
            {
                case Tag.Var: return t2.tag == Tag.Var ? t1.deBruin == t2.deBruin : false; // TODO: not sure if correct
                case Tag.Top: return t2.tag == Tag.Top;
                case Tag.Error: throw new Exception("comparing error types, wtf bruv");
                case Tag.Arr: return t2.tag == Tag.Arr ? ((t1.left == t2.left) && (t1.right == t2.right)) : false;
                default: return false;
            }
        }
        public static bool operator !=(ty t1, ty t2) => !(t1 == t2);
        //   P U B L I C 
        public static ty ty_map(Func<int, int, int, ty> on_var, int c, ty tyT)
        {
            ty walk(int c, ty tyT)  
            {
                switch (tyT.tag)
                {
                    case Tag.Var: return on_var(c, tyT.deBruin, tyT.DBG_CTX_LEN);
                    // why do we add 1 to the right side cutoff? because its the same as in ABS: 
                    // the variable 0 is bound to the outer "Forall" so in the context itll occupy slot 0, so we shift the rest up by 1
                    case Tag.All: return ty.newAll(tyT.lexeme, walk(c, tyT.left), walk(c + 1, tyT.right)); 
                    case Tag.Arr: return ty.newArr(walk(c, tyT.left), walk(c, tyT.right));
                    case Tag.Top: return ty.newTop(); // can just return this
                    default: throw new Exception("bruh moment in ty_map");
                }
            }
            return walk(c, tyT);
        }
        public static ty typeShiftAbove(int d, int c, ty tyT)
        {
            Func<int, int, int, ty> on_var = ((c, x, n) => x >= c ? ty.newVar(x + d, n + d) : ty.newVar(x, n + d));
            return ty_map(on_var, c, tyT);
        }
        public static ty typeShift(int d, ty tyT) => typeShiftAbove(d, 0, tyT);
        public static ty typeSubst(ty tyS, int j, ty tyT)
        {
            Func<int, int, int, ty> on_var = ((j, x, n) => x == j ? typeShift(j, tyS) : ty.newVar(x, n));
            return ty.ty_map(on_var, j, tyT);
        }
        public static ty typeSubstTop(ty tyS, ty tyT) => typeShift(-1, typeSubst(typeShift(1, tyS), 0, tyT));
        public static term tytermSubst(ty tyS, int j, term t)
        {
            // If there is a type variable in a term, it performs substitution inside the term.
            Func<int, int, int, term> on_var = ((c,x,n)=> term.newVar(x,n)); // replicate the variable, no action needed.
            Func<int, ty, ty> on_type = ((j, tyT) => typeSubst(tyS, j, tyT)); // replace possible type with the substitutee. [j->tyS]tyT
            return term.term_map(on_var, on_type, j, t);
        }
        public static term tytermSubstTop(ty tyS, term t) => term.termShift(-1, tytermSubst(typeShift(1, tyS), 0, t));
        //   S U B T Y P I N G 
        public static bool subtype(Context ctx, ty tyS, ty tyT)
        {
            //if (tyS == tyT) return true; dont trust myself, plus that function does the same as here 
            if (tyS.tag == Tag.Var)
            {
                var soy = tyS.promote(ctx);
                if (soy.tag == Tag.Error && soy.lexeme == "NoRuleApplies") throw new Exception("promotion failed bruv");
                return subtype(ctx, soy, tyT); // TODO: what is the effect of promote here?
            }
            if (tyS.tag == Tag.All && tyT.tag == Tag.All)
            {
                var lefts = subtype(ctx, tyS.left, tyT.left) && subtype(ctx, tyT.left, tyS.left);
                var ctx1 = Context.add_binding(ctx, tyS.lexeme, binding.newTyVarBind(tyT.left));
                var rights = subtype(ctx1, tyS.right, tyT.right);
                return lefts && rights; // TODO: no idea whats going on 
            }
            if (tyT.tag == Tag.Top) return true;
            if (tyS.tag == Tag.Arr && tyT.tag == Tag.Arr)
            {
                return subtype(ctx, tyT.left, tyS.left) && subtype(ctx, tyS.right, tyT.right);
            }
            return false;
        }
        public ty promote(Context ctx)
        {
            switch(this.tag)
            {
                case Tag.Var:
                    // if its a type variable, first extract its binding
                    var bind = ctx.get_binding(this.deBruin); // this is where the binding is shifted up by 1 
                    switch (bind.tag)
                    {
                        case binding.Tag.TyVarBind: return bind.type; // this just returns its binding in the context
                        default: return ty.newError("NoRuleApplies"); 
                        // i suppose there are more interesting cases, like for Arrows, in the full version? - yes there is.
                    }
                default: return ty.newError("NoRuleApplies");
            }
        }
        public static ty lcst(Context ctx, ty tyS)
        {
            var tyS_prom = tyS.promote(ctx);
            if (tyS_prom.tag == Tag.Error && tyS_prom.lexeme == "NoRuleApplies") return tyS;
            else return lcst(ctx, tyS_prom); // wtf does this do
        }
    }
    public class term
    {
        public enum Tag { TAbs, TApp, Var, Abs, App, Error }
        // Var 
        readonly public Tag tag;
        readonly public int deBruin;
        readonly public int DBG_CTX_LEN;
        readonly public ty type; // type in Abs, type in TAbs, and type in TApp right side
        readonly public string lexeme;
        readonly public term left;
        readonly public term right;
        private term(Tag tag, int deBruin, int DBG_CTX_LEN, ty type, string lexeme, term left, term right)
        {
            this.tag = tag;
            this.deBruin = deBruin;
            this.DBG_CTX_LEN = DBG_CTX_LEN;
            this.type = type;
            this.lexeme = lexeme;
            this.left = left;
            this.right = right;
        }
        //   C O N S T R U C T O R S 
        public static term newTAbs(string lexeme, ty type, term body) => new term(Tag.TAbs, -9999, -9999, type, lexeme, null, body);
        public static term newTApp(term left, ty type) => new term(Tag.TApp, -9999, -9999, type, null, left, null);
        public static term newVar(int deBruin, int DBG_CTX_LEN) => new term(Tag.Var, deBruin, DBG_CTX_LEN, null, null, null, null);
        public static term newAbs(string lexeme, ty type, term body) => new term(Tag.Abs, -9999, -9999, type, lexeme, null, body);
        public static term newApp(term left, term right) => new term(Tag.App, -9999, -9999, null, null, left, right);
        public static term newError(string m) => new term(Tag.Error, -9999, -9999, null, m, null, null);
        //   I N T E R N A L
        public override string ToString()
        {
            switch (this.tag)
            {
                case Tag.TAbs: return $"\\{this.lexeme}.{this.type} . {this.right}"; // TODO: FIX 
                case Tag.TApp: return $"({this.left} {this.type})";
                case Tag.Var: return $"v{this.deBruin}";
                case Tag.Abs: return $"\\{this.lexeme}:{this.type} . {this.right}";
                case Tag.App: return $"({this.left} {this.right})";
                case Tag.Error: return this.lexeme;
                default: return "<< E R R O R >>";
            }
        }
        //   S T A T I C
        public static term term_map(Func<int, int, int, term> on_var, Func<int, ty, ty> on_type, int c, term t)
        {
            // ontype is used when we have a type expression as a type rather than a concrete type. like AX.X->X 
            term walk(int c, term t) // yes im lazy. 
            {
                switch (t.tag)
                {
                    case Tag.TAbs: return term.newTAbs(t.lexeme, on_type(c, t.type), walk(c + 1, t.right)); // TODO: figure out its shape/ find examples: some are AX.T2 or somehting
                    case Tag.TApp: return term.newTApp(walk(c, t.left), on_type(c, t.type));
                    case Tag.Var: return on_var(c, t.deBruin, t.DBG_CTX_LEN);
                    case Tag.Abs: return term.newAbs(t.lexeme, on_type(c, t.type), walk(c + 1, t.right));
                    case Tag.App: return term.newApp(walk(c, t.left), walk(c, t.right));
                    default: throw new Exception("bruh moment in term_map");
                }
            }
            return walk(c, t);
        }
        public static term termShiftAbove(int d, int c, term t)
        {
            Func<int, int, int, term> on_var = ((c, x, n) => x >= c ? term.newVar(x + d, n + d) : term.newVar(x, n + d));
            
            ty typeShiftAbove_CURRIED(int c, ty tyT) // yes i jus copy pasted it. 
            {
                Func<int, int, int, ty> on_var = ((c, x, n) => x >= c ? ty.newVar(x + d, n + d) : ty.newVar(x, n + d));
                return ty.ty_map(on_var, c, tyT);
            }
            return term_map(on_var, typeShiftAbove_CURRIED, c, t);
        }
        public static term termShift(int d, term t) => termShiftAbove(d, 0, t);
        public static term termSubst(int j, term s, term t)
        {
            // if the variable index from the ctx match, shift it by j (the index of the result variable) otherwise return a copy
            Func<int, int, int, term> on_var = ((j, x, n) => x == j ? termShift(j, s) : term.newVar(x, n));
            Func<int, ty, ty> on_type = ((j,tyT)=>tyT);
            return term_map(on_var, on_type, j, t);
        }
        public static term termSubstTop(term s, term t) => termShift(-1, termSubst(0, termShift(1, s), t)); // beta reduce
        //   P U B L I C
        public bool isval() => (tag == Tag.Abs || tag == Tag.TAbs);
        public term eval1(Context ctx)
        {
            switch (this.tag)
            {

                case Tag.TApp:
                    {
                        // case 1: the left side is a value (Abstraction) which is ready to accept the type input, aka beta-reduction. 
                        // also note: we didn't check if the right side is a "value", it could be any type expression.
                        if (this.left.tag == Tag.TAbs)
                        {
                            return ty.tytermSubstTop(this.type, this.left.right); // TODO: double check it works. 
                                                                                  // this.left is the TAbs term. 
                                                                                  // this.left.right is the TAbs term's body, stored in the "right" field. 
                        }
                        // case 2: left side is not a value, take a step in it. 
                        var left_step = this.left.eval1(ctx);
                        return term.newTApp(left_step, this.type);
                    }
                case Tag.App:
                    {
                        // case 1: left is ABS, right is value 
                        if (this.left.tag == Tag.Abs && right.isval()) return termSubstTop(this.right, this.left.right);
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
                    }
                default: return term.newError("NoRuleApplies");
            }
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
                case Tag.TAbs:
                    {
                        var tyX = this.lexeme;
                        var tyT1 = this.type;
                        var t2 = this.right;
                        var ctx_ = Context.add_binding(ctx, tyX, binding.newTyVarBind(tyT1)); // TODO: possible bug idk why he names it the same thing
                        var tyT2 = t2.type_of(ctx_);
                        return ty.newAll(tyX, tyT1, tyT2); // TODO: wat goin on 
                    }
                case Tag.TApp:
                    {
                        var t1 = this.left;
                        var tyT2 = this.type;
                        var tyT1 = t1.type_of(ctx);

                        var soy = ty.lcst(ctx, tyT1);
                        switch (soy.tag)
                        {
                            case ty.Tag.All:
                                var tyT11 = soy.left;
                                var tyT12 = soy.right;
                                if (!ty.subtype(ctx, tyT2, tyT11)) throw new Exception("type parameter type mismatch");
                                return ty.typeSubstTop(tyT2, tyT12);
                            default: throw new Exception("universal type expected");
                        }
                    }
                case Tag.Var: return ctx.get_type_from_context(this.deBruin);
                case Tag.Abs:
                    {
                        var x = this.lexeme;
                        var tyT1 = this.type;
                        var t2 = this.right;

                        var ctx_ = Context.add_binding(ctx, x, binding.newVarBind(tyT1));
                        var tyT2 = t2.type_of(ctx_);
                        return ty.newArr(tyT1, ty.typeShift(-1, tyT2)); // TODO: wat going on 
                    }
                case Tag.App:
                    {
                        var t1 = this.left;
                        var t2 = this.right;
                        var tyT1 = t1.type_of(ctx);
                        var tyT2 = t2.type_of(ctx);

                        var soy = ty.lcst(ctx, tyT1);

                        switch (soy.tag)
                        {
                            case ty.Tag.Arr:
                                var tyT11 = soy.left;
                                var tyT12 = soy.right;
                                if (ty.subtype(ctx, tyT2, tyT11)) return tyT12;
                                else throw new Exception("parameter type mismatch");
                            default: throw new Exception("arrow type expected");
                        }
                    }
                default: throw new Exception("unknown tag in type_of");

            }
        }
    }
    class Program
    {
        static void test(term t)
        {
            Context ctx = new Context();
            var t_ = t.type_of(ctx);
            var e = t.eval(ctx);
            Console.WriteLine($"{e}\t:\t{t_}");
        }
        static void Main(string[] args)
        {
            {
                // Test 0
                var t = term.newAbs("x", ty.newTop(), term.newVar(0, 1));
                test(t);
            }
            {
                // Test 1
                var t = term.newApp(term.newAbs("x", ty.newTop(), term.newVar(0, 1)), term.newAbs("x", ty.newTop(), term.newVar(0, 1)));
                test(t);
            }
            {
                // Test 2
                var t = term.newApp(term.newAbs("x", ty.newArr(ty.newTop(), ty.newTop()), term.newVar(0, 1)), term.newAbs("x", ty.newTop(), term.newVar(0, 1)));
                test(t);
            }
            {
                // Test 3
                var t = term.newTAbs("X", ty.newTop(), term.newAbs("x", ty.newVar(0, 1), term.newVar(0, 2)));
                test(t);
            }
            {
                // Test 4
                var t = term.newTApp(term.newTAbs("X", ty.newTop(), term.newAbs("x", ty.newVar(0, 1), term.newVar(0, 2))), ty.newAll("X", ty.newTop(), ty.newArr(ty.newVar(0, 1), ty.newVar(0, 1))));
                test(t);
            }
            {
                // Test 5
                var t = term.newTAbs("X", ty.newArr(ty.newTop(), ty.newTop()), term.newAbs("x", ty.newVar(0, 1), term.newApp(term.newVar(0, 2), term.newVar(0, 2))));
                test(t);
            }
            Console.ReadKey();
        }
    }
}
