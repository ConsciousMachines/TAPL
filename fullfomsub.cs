using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;

// TODO: find how the type option / kind option is used in binding / if i can replace it w null ptr checks

namespace fullfomsub_cs
{
    public class binding
    {
        // getterz 
        // TyVar
        public ty TyVar_type => type;
        // Var
        public ty Var_type => type;
        // TyAbb
        public ty TyAbb_type => type;
        public kind TyAbb_kind => k;
        // TmAbb 
        public term TmAbb_term => t;
        public ty TmAbb_type => type;
        // data 
        public enum Tag { NameBind, TyVarBind, VarBind, TyAbbBind, TmAbbBind }
        public readonly Tag tag;
        private readonly ty type;
        private readonly term t;
        private readonly kind k; // the option can be a null ptr :P
        private binding(Tag tag, ty type, term t, kind k)
        {
            this.t = t;
            this.k = k;
            this.tag = tag;
            this.type = type;
        }
        public static binding newNameBind() => new binding(Tag.NameBind, null, null, null);
        public static binding newVarBind(ty type) => new binding(Tag.VarBind, type, null, null);
        public static binding newTyVarBind(ty type) => new binding(Tag.TyVarBind, type, null, null);
        public static binding newTmAbbBind(term t, ty type) => new binding(Tag.TmAbbBind, type, t, null);
        public static binding newTyAbbBind(ty type, kind k) => new binding(Tag.TyAbbBind, type, null, k);
        // internal 
        public override string ToString()
        {
            switch (this.tag)
            {
                case Tag.NameBind: return $"NameBind";
                case Tag.VarBind: return $"{this.TyVar_type}";
                case Tag.TyVarBind: return $"{this.TyVar_type}";
                case Tag.TyAbbBind: return $"{this.TyAbb_type}:{this.TyAbb_kind}";
                case Tag.TmAbbBind: return $"{this.TmAbb_term}:{this.TmAbb_type}";
                default: return "<< E R R O R >>";
            }
        }
    }
    public class kind
    {
        // getterz 
        public kind Arr_Left => left;
        public kind Arr_Right => right;
        // data 
        public enum Tag { Star, Arr }
        public readonly Tag tag;
        private readonly kind left;
        private readonly kind right;
        private kind(Tag tag, kind left, kind right)
        {
            this.tag = tag;
            this.left = left;
            this.right = right;
        }
        // constructors
        public static kind newStar() => new kind(Tag.Star, null, null);
        public static kind newArr(kind left, kind right) => new kind(Tag.Arr, left, right);
        // internal 
        public override string ToString()
        {
            switch (this.tag)
            {
                case Tag.Star: return "Star";
                case Tag.Arr: return $"({this.Arr_Left}->{this.Arr_Right})";
                default: return "<< E R R O R >>";
            }
        }
    }
    public class ty
    {
        // getterz
        // var
        public int Var_deBruin => deBruin;
        public int Var_DBG_CTX_LEN => DBG_CTX_LEN;
        // id 
        public string Id_symbol => lexeme;
        // Arr
        public ty Arr_Left => left;
        public ty Arr_Right => right;
        // Record 
        public List<(string, ty)> Rec_Entries => entries;
        // All
        public string All_symbol => lexeme;
        public ty All_bound => left;
        public ty All_body => right;
        // Some
        public string Some_symbol => lexeme;
        public ty Some_bound => left;
        public ty Some_body => right;
        // Abs
        public string Abs_symbol => lexeme;
        public kind Abs_kind => k;
        public ty Abs_type => right;
        // App
        public ty App_left => left;
        public ty App_right => right;
        // data 
        public enum Tag { Var, Id, Top, Arr, Bool, Record, String, Unit, Float, All, Nat, Some, Abs, App }
        public readonly Tag tag;
        private readonly kind k;
        private readonly ty left;
        private readonly ty right;
        private readonly int deBruin;
        private readonly string lexeme;
        private readonly int DBG_CTX_LEN;
        private readonly List<(string, ty)> entries;
        private ty(Tag tag, int deBruin, int DBG_CTX_LEN, string lexeme, ty left, ty right, List<(string, ty)> entries, kind k)
        {
            this.k = k;
            this.tag = tag;
            this.left = left;
            this.right = right;
            this.lexeme = lexeme;
            this.deBruin = deBruin;
            this.entries = entries;
            this.DBG_CTX_LEN = DBG_CTX_LEN;
        }
        // constructors 
        public static ty newNat() => new ty(Tag.Nat, -9999, -9999, null, null, null, null, null);
        public static ty newTop() => new ty(Tag.Top, -9999, -9999, null, null, null, null, null);
        public static ty newBool() => new ty(Tag.Bool, -9999, -9999, null, null, null, null, null);
        public static ty newUnit() => new ty(Tag.Unit, -9999, -9999, null, null, null, null, null);
        public static ty newFloat() => new ty(Tag.Float, -9999, -9999, null, null, null, null, null);
        public static ty newString() => new ty(Tag.String, -9999, -9999, null, null, null, null, null);
        public static ty newId(string symbol) => new ty(Tag.Id, -9999, -9999, symbol, null, null, null, null);
        public static ty newArr(ty left, ty right) => new ty(Tag.Arr, -9999, -9999, null, left, right, null, null);
        public static ty newApp(ty left, ty right) => new ty(Tag.App, -9999, -9999, null, left, right, null, null);
        public static ty newAbs(string symbol, kind k, ty type) => new ty(Tag.Abs, -9999, -9999, symbol, null, type, null, k);
        public static ty newRecord(List<(string, ty)> entries) => new ty(Tag.Record, -9999, -9999, null, null, null, entries, null);
        public static ty newAll(string symbol, ty bound, ty body) => new ty(Tag.All, -9999, -9999, symbol, bound, body, null, null);
        public static ty newVar(int deBruin, int DBG_CTX_LEN) => new ty(Tag.Var, deBruin, DBG_CTX_LEN, null, null, null, null, null);
        public static ty newSome(string symbol, ty bound, ty body) => new ty(Tag.Some, -9999, -9999, symbol, bound, body, null, null);
        // internal 
        public override string ToString()
        {
            switch (this.tag)
            {
                case Tag.Top: return $"Top";
                case Tag.Nat: return $"Nat";
                case Tag.Bool: return $"Bool";
                case Tag.Unit: return $"Unit";
                case Tag.Float: return $"Float";
                case Tag.String: return $"String";
                case Tag.Id: return $"{this.Id_symbol}";
                case Tag.Var: return $"tv{this.Var_deBruin}";
                case Tag.Arr: return $"{this.Arr_Left}->{this.Arr_Right}";
                case Tag.App: return $"({this.App_left} {this.App_right})";
                case Tag.Abs: return $"\\ {this.Abs_symbol}:{this.Abs_kind}. {this.Abs_type}";
                case Tag.All: return $"Forall {this.All_symbol}<:{this.All_bound}. {this.All_body}";
                case Tag.Some: return $"Some {this.Some_symbol}<:{this.Some_bound}. {this.Some_body}";
                case Tag.Record:
                    StringBuilder sb = new StringBuilder("{");
                    foreach (var entry in this.Rec_Entries)
                    {
                        sb.Append(entry.Item1);
                        sb.Append(":");
                        sb.Append(entry.Item2);
                        sb.Append(", ");
                    }
                    sb.Append("}");
                    return sb.ToString();
                default: return "<< E R R O R >>";
            }
        }
    }
    public class term
    {
        // getterz
        // fix 
        public term Fix_term => left;
        // succ 
        public term Succ_term => left;
        // pred 
        public term Pred_term => left;
        // isZero
        public term IsZero_term => left;
        // string
        public string String_value => lexeme1;
        // float 
        public float Float_value => f;
        // inert
        public ty Inert_type => type;
        // ascribe
        public term Ascribe_term => left;
        public ty Ascribe_type => type;
        // timesFloat 
        public term TimesFloat_left => left;
        public term TimesFloat_right => right;
        // var 
        public int Var_deBruin => deBruin;
        public int Var_DBG_CTX_LEN => DBG_CTX_LEN;
        // Abs 
        public string Abs_symbol => lexeme1;
        public ty Abs_type => type;
        public term Abs_body => right;
        // App 
        public term App_left => left;
        public term App_right => right;
        // If 
        public term If_guard => guard;
        public term If_ThenClause => left;
        public term If_ElseClause => right;
        // Record 
        public List<(string, term)> Rec_entries => entries;
        // Proj
        public term Proj_left => left;
        public string Proj_right => lexeme1;
        // Let 
        public string Let_symbol => lexeme1;
        public term Let_binding => left;
        public term Let_body => right;
        // TAbs 
        public string TAbs_symbol => lexeme1;
        public ty TAbs_type => type;
        public term TAbs_body => right;
        // TApp 
        public term TApp_left => left;
        public ty TApp_right => type;
        // Pack 
        public ty Pack_asType => type;
        public term Pack_asTerm => right;
        public ty Pack_package => type2;
        // Unpack 
        public string Unpack_typeName => lexeme1;
        public string Unpack_termName => lexeme2;
        public term Unpack_left => left;
        public term Unpack_right => right;


        // data 
        public enum Tag { True, False, Unit, Zero, Fix, Succ, Pred, IsZero, String, Float, Inert, Ascribe, TimesFloat, Var, Abs, App, If, Record, Proj, Let, TAbs, TApp, Pack, Unpack }
        public readonly Tag tag;
        float f;
        ty type;
        ty type2;
        term left;
        term guard;
        term right;
        int deBruin;
        string lexeme1;
        string lexeme2;
        int DBG_CTX_LEN;
        List<(string, term)> entries;
        private term(Tag tag, int deBruin, int DBG_CTX_LEN, ty type, ty type2, float f, string lexeme1, string lexeme2, term guard, term left, term right, List<(string, term)> entries)
        {
            this.f = f;
            this.tag = tag;
            this.left = left;
            this.type = type;
            this.type2 = type2;
            this.guard = guard;
            this.right = right;
            this.lexeme1 = lexeme1;
            this.lexeme2 = lexeme2;
            this.entries = entries;
            this.deBruin = deBruin;
            this.DBG_CTX_LEN = DBG_CTX_LEN;
        }
        public static term newTrue() => new term(Tag.True, -9999, -9999, null, null, -9999.0f, null, null, null, null, null, null);
        public static term newUnit() => new term(Tag.Unit, -9999, -9999, null, null, -9999.0f, null, null, null, null, null, null);
        public static term newZero() => new term(Tag.Zero, -9999, -9999, null, null, -9999.0f, null, null, null, null, null, null);
        public static term newFix(term t) => new term(Tag.Fix, -9999, -9999, null, null, -9999.0f, null, null, null, t, null, null);
        public static term newFloat(float f) => new term(Tag.Float, -9999, -9999, null, null, f, null, null, null, null, null, null);
        public static term newFalse() => new term(Tag.False, -9999, -9999, null, null, -9999.0f, null, null, null, null, null, null);
        public static term newSucc(term t) => new term(Tag.Succ, -9999, -9999, null, null, -9999.0f, null, null, null, t, null, null);
        public static term newPred(term t) => new term(Tag.Pred, -9999, -9999, null, null, -9999.0f, null, null, null, t, null, null);
        public static term newIsZero(term t) => new term(Tag.IsZero, -9999, -9999, null, null, -9999.0f, null, null, null, t, null, null);
        public static term newTApp(term t, ty type) => new term(Tag.TApp, -9999, -9999, type, null, -9999.0f, null, null, null, t, null, null);
        public static term newString(string m) => new term(Tag.String, -9999, -9999, null, null, -9999.0f, m, null, null, null, null, null);
        public static term newInert(ty type) => new term(Tag.Inert, -9999, -9999, type, null, -9999.0f, null, null, null, null, null, null);
        public static term newAscribe(term t, ty type) => new term(Tag.Ascribe, -9999, -9999, type, null, -9999.0f, null, null, null, t, null, null);
        public static term newApp(term left, term right) => new term(Tag.App, -9999, -9999, null, null, -9999.0f, null, null, null, left, right, null);
        public static term newProj(term left, string right) => new term(Tag.Proj, -9999, -9999, null, null, -9999.0f, right, null, null, left, null, null);
        public static term newAbs(string symbol, ty type, term body) => new term(Tag.Abs, -9999, -9999, type, null, -9999.0f, symbol, null, null, null, body, null);
        public static term newTimesFloat(term left, term right) => new term(Tag.TimesFloat, -9999, -9999, null, null, -9999.0f, null, null, null, left, right, null);
        public static term newTAbs(string symbol, ty type, term body) => new term(Tag.TAbs, -9999, -9999, type, null, -9999.0f, symbol, null, null, null, body, null);
        public static term newRecord(List<(string, term)> entries) => new term(Tag.Record, -9999, -9999, null, null, -9999.0f, null, null, null, null, null, entries);
        public static term newVar(int deBruin, int DBG_CTX_LEN) => new term(Tag.Var, deBruin, DBG_CTX_LEN, null, null, -9999.0f, null, null, null, null, null, null);
        public static term newPack(ty asType, term asTerm, ty package) => new term(Tag.Pack, -9999, -9999, asType, package, -9999.0f, null, null, null, null, asTerm, null);
        public static term newLet(string symbol, term binding, term body) => new term(Tag.Let, -9999, -9999, null, null, -9999.0f, symbol, null, null, binding, body, null);
        public static term newIf(term guard, term thenClause, term elseClause) => new term(Tag.If, -9999, -9999, null, null, -9999.0f, null, null, guard, thenClause, elseClause, null);
        public static term newUnpack(string typeName, string termName, term left, term right) => new term(Tag.Unpack, -9999, -9999, null, null, -9999.0f, typeName, termName, null, left, right, null);

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
    }




    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
        }
    }
}
