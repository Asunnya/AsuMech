using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;

namespace AsuMech.Learn.Sources;

// A TypeScript data module (constants holding object and array literals) read as JSON without
// running any code: type annotations and imports are skipped, identifiers resolve to the module's
// own constants or stay as {"$ref": name}, spreads splice the referenced array or object, and a
// call keeps its name and arguments as {"$call": name, "args": […]}. Function bodies are skipped.
public sealed class TsModule
{
    public Dictionary<string, JsonNode?> Symbols { get; } = new();
    public List<string> Exports { get; } = [];
    public List<string> Imports { get; } = [];

    public JsonNode? this[string name] => Symbols.TryGetValue(name, out var n) ? n : null;
}

public static class TsLiteral
{
    private enum Kind { Punct, String, Number, Ident, End }

    private readonly record struct Token(Kind Kind, string Text, int Line);

    public static TsModule Parse(string source, IReadOnlyDictionary<string, JsonNode?>? external = null)
    {
        var tokens = Tokenize(source);
        var module = new TsModule();
        var p = new Parser(tokens, module, external);
        p.ParseModule();
        return module;
    }

    private sealed class Parser(List<Token> tokens, TsModule module, IReadOnlyDictionary<string, JsonNode?>? external)
    {
        private int i;

        private Token Peek(int ahead = 0) => i + ahead < tokens.Count ? tokens[i + ahead] : new Token(Kind.End, "", -1);
        private Token Next() => tokens[i++];
        private bool IsPunct(string text, int ahead = 0) => Peek(ahead).Kind == Kind.Punct && Peek(ahead).Text == text;
        private bool IsIdent(string text, int ahead = 0) => Peek(ahead).Kind == Kind.Ident && Peek(ahead).Text == text;

        public void ParseModule()
        {
            while (Peek().Kind != Kind.End)
            {
                if (IsIdent("import")) { SkipImport(); continue; }
                if (IsIdent("export")) { Next(); if (IsIdent("default")) Next(); ExportNext(); continue; }
                if (IsIdent("const") || IsIdent("let") || IsIdent("var")) { Declaration(false); continue; }
                if (IsIdent("function")) { SkipFunction(); continue; }
                if (IsIdent("type") || IsIdent("interface")) { SkipStatement(); continue; }
                Next();
            }
        }

        private void ExportNext()
        {
            if (IsIdent("const") || IsIdent("let") || IsIdent("var")) Declaration(true);
            else if (IsIdent("function")) SkipFunction();
            else if (IsPunct("{")) { SkipBalanced("{", "}"); if (IsIdent("from")) SkipStatement(); }
            else SkipStatement();
        }

        private void Declaration(bool exported)
        {
            Next();
            while (true)
            {
                if (Peek().Kind != Kind.Ident) { SkipStatement(); return; }
                var name = Next().Text;
                if (IsPunct(":")) { Next(); SkipType(); }
                JsonNode? value = null;
                if (IsPunct("=")) { Next(); value = Expression(); }
                module.Symbols[name] = value;
                if (exported) module.Exports.Add(name);
                if (IsPunct(",")) { Next(); continue; }
                if (IsPunct(";")) Next();
                return;
            }
        }

        private void SkipImport()
        {
            Next();
            var isType = IsIdent("type");
            if (isType) Next();
            if (IsPunct("{"))
            {
                Next();
                while (!IsPunct("}") && Peek().Kind != Kind.End)
                {
                    var t = Next();
                    if (t.Kind == Kind.Ident && t.Text != "as" && t.Text != "type" && !isType) module.Imports.Add(t.Text);
                }
                Next();
            }
            else if (Peek().Kind == Kind.Ident && !IsIdent("from")) { if (!isType) module.Imports.Add(Next().Text); }
            SkipStatement();
        }

        private void SkipStatement()
        {
            while (Peek().Kind != Kind.End && !IsPunct(";"))
            {
                if (IsPunct("{")) SkipBalanced("{", "}");
                else if (IsPunct("(")) SkipBalanced("(", ")");
                else if (IsPunct("[")) SkipBalanced("[", "]");
                else Next();
            }
            if (IsPunct(";")) Next();
        }

        private void SkipFunction()
        {
            Next();
            if (Peek().Kind == Kind.Ident) Next();
            if (IsPunct("<")) SkipBalanced("<", ">");
            if (IsPunct("(")) SkipBalanced("(", ")");
            if (IsPunct(":")) { Next(); SkipType(); }
            if (IsPunct("{")) SkipBalanced("{", "}");
        }

        // A type annotation runs to the "=" (or "," / ";" / ")") at bracket depth zero.
        private void SkipType()
        {
            var depth = 0;
            while (Peek().Kind != Kind.End)
            {
                var t = Peek();
                if (t.Kind == Kind.Punct)
                {
                    if (t.Text is "(" or "[" or "{" or "<") depth++;
                    else if (t.Text is ")" or "]" or "}" or ">") { if (depth == 0) return; depth--; }
                    else if (depth == 0 && t.Text is "=" or "," or ";") return;
                }
                Next();
            }
        }

        private void SkipBalanced(string open, string close)
        {
            var depth = 0;
            while (Peek().Kind != Kind.End)
            {
                var t = Next();
                if (t.Kind != Kind.Punct) continue;
                if (t.Text == open) depth++;
                else if (t.Text == close && --depth == 0) return;
            }
        }

        private JsonNode? Expression()
        {
            var node = Primary();
            // "as Type" assertions and "satisfies" clauses add nothing to the value.
            while (IsIdent("as") || IsIdent("satisfies")) { Next(); SkipType(); }
            return node;
        }

        private JsonNode? Primary()
        {
            var t = Peek();
            switch (t.Kind)
            {
                case Kind.String: Next(); return JsonValue.Create(t.Text);
                case Kind.Number: Next(); return Number(t.Text);
                case Kind.Punct when t.Text == "{": return Object();
                case Kind.Punct when t.Text == "[": return Array();
                case Kind.Punct when t.Text == "(": Next(); var inner = Expression(); if (IsPunct(")")) Next(); return inner;
                case Kind.Punct when t.Text == "-": Next(); return Peek().Kind == Kind.Number ? Number("-" + Next().Text) : null;
                case Kind.Punct when t.Text == "+": Next(); return Primary();
                case Kind.Ident:
                    Next();
                    if (t.Text == "true") return JsonValue.Create(true);
                    if (t.Text == "false") return JsonValue.Create(false);
                    if (t.Text is "null" or "undefined") return null;
                    var name = new StringBuilder(t.Text);
                    while (IsPunct(".") && Peek(1).Kind == Kind.Ident) { Next(); name.Append('.').Append(Next().Text); }
                    if (IsPunct("("))
                    {
                        Next();
                        var args = new JsonArray();
                        while (!IsPunct(")") && Peek().Kind != Kind.End)
                        {
                            args.Add(Expression());
                            if (IsPunct(",")) Next();
                        }
                        if (IsPunct(")")) Next();
                        return new JsonObject { ["$call"] = name.ToString(), ["args"] = args };
                    }
                    return Resolve(name.ToString());
                default:
                    Next();
                    return null;
            }
        }

        private JsonNode? Resolve(string name)
        {
            if (module.Symbols.TryGetValue(name, out var own)) return own?.DeepClone();
            if (external != null && external.TryGetValue(name, out var ext)) return ext?.DeepClone();
            return new JsonObject { ["$ref"] = name };
        }

        private static JsonNode Number(string text)
            => long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var l) ? JsonValue.Create(l)
                : JsonValue.Create(double.Parse(text, NumberStyles.Float, CultureInfo.InvariantCulture));

        private JsonObject Object()
        {
            Next();
            var obj = new JsonObject();
            while (!IsPunct("}") && Peek().Kind != Kind.End)
            {
                if (IsPunct("...")) { Next(); Spread(obj, Expression()); if (IsPunct(",")) Next(); continue; }
                var key = Peek();
                if (key.Kind == Kind.Punct && key.Text == "[") { SkipBalanced("[", "]"); }
                else Next();
                var keyText = key.Text;
                if (IsPunct(":")) { Next(); obj[keyText] = Expression(); }
                else if (IsPunct("(")) { SkipBalanced("(", ")"); if (IsPunct("{")) SkipBalanced("{", "}"); }
                else obj[keyText] = Resolve(keyText);
                if (IsPunct(",")) Next();
            }
            if (IsPunct("}")) Next();
            return obj;
        }

        private static void Spread(JsonObject into, JsonNode? source)
        {
            if (source is not JsonObject other) return;
            foreach (var (k, v) in other.ToList()) into[k] = v?.DeepClone();
        }

        private JsonArray Array()
        {
            Next();
            var arr = new JsonArray();
            while (!IsPunct("]") && Peek().Kind != Kind.End)
            {
                if (IsPunct("..."))
                {
                    Next();
                    var spread = Expression();
                    if (spread is JsonArray items) foreach (var item in items.ToList()) arr.Add(item?.DeepClone());
                    else arr.Add(spread);
                }
                else arr.Add(Expression());
                if (IsPunct(",")) Next();
            }
            if (IsPunct("]")) Next();
            return arr;
        }
    }

    private static List<Token> Tokenize(string s)
    {
        var tokens = new List<Token>();
        var line = 1;
        var i = 0;
        while (i < s.Length)
        {
            var c = s[i];
            if (c == '\n') { line++; i++; continue; }
            if (char.IsWhiteSpace(c)) { i++; continue; }
            if (c == '/' && i + 1 < s.Length && s[i + 1] == '/') { while (i < s.Length && s[i] != '\n') i++; continue; }
            if (c == '/' && i + 1 < s.Length && s[i + 1] == '*')
            {
                var end = s.IndexOf("*/", i + 2, StringComparison.Ordinal);
                var stop = end < 0 ? s.Length : end + 2;
                line += s[i..stop].Count(ch => ch == '\n');
                i = stop;
                continue;
            }
            if (c is '\'' or '"' or '`')
            {
                var (text, next, lines) = ReadString(s, i);
                tokens.Add(new Token(Kind.String, text, line));
                line += lines;
                i = next;
                continue;
            }
            if (char.IsDigit(c) || (c == '.' && i + 1 < s.Length && char.IsDigit(s[i + 1])))
            {
                var start = i;
                while (i < s.Length && (char.IsLetterOrDigit(s[i]) || s[i] == '.' || s[i] == '_' || ((s[i] == '+' || s[i] == '-') && (s[i - 1] == 'e' || s[i - 1] == 'E')))) i++;
                tokens.Add(new Token(Kind.Number, s[start..i].Replace("_", ""), line));
                continue;
            }
            if (char.IsLetter(c) || c == '_' || c == '$')
            {
                var start = i;
                while (i < s.Length && (char.IsLetterOrDigit(s[i]) || s[i] == '_' || s[i] == '$')) i++;
                tokens.Add(new Token(Kind.Ident, s[start..i], line));
                continue;
            }
            if (c == '.' && i + 2 < s.Length && s[i + 1] == '.' && s[i + 2] == '.') { tokens.Add(new Token(Kind.Punct, "...", line)); i += 3; continue; }
            if (c == '=' && i + 1 < s.Length && s[i + 1] == '>') { tokens.Add(new Token(Kind.Punct, "=>", line)); i += 2; continue; }
            tokens.Add(new Token(Kind.Punct, c.ToString(), line));
            i++;
        }
        return tokens;
    }

    private static (string Text, int Next, int Lines) ReadString(string s, int start)
    {
        var quote = s[start];
        var sb = new StringBuilder();
        var lines = 0;
        var i = start + 1;
        while (i < s.Length && s[i] != quote)
        {
            var c = s[i];
            if (c == '\\' && i + 1 < s.Length)
            {
                var e = s[i + 1];
                switch (e)
                {
                    case 'n': sb.Append('\n'); i += 2; break;
                    case 't': sb.Append('\t'); i += 2; break;
                    case 'r': sb.Append('\r'); i += 2; break;
                    case 'u' when i + 5 < s.Length:
                        sb.Append((char)Convert.ToInt32(s.Substring(i + 2, 4), 16)); i += 6; break;
                    case '\n': lines++; i += 2; break;
                    default: sb.Append(e); i += 2; break;
                }
                continue;
            }
            if (c == '\n') lines++;
            sb.Append(c);
            i++;
        }
        return (sb.ToString(), Math.Min(i + 1, s.Length), lines);
    }
}
