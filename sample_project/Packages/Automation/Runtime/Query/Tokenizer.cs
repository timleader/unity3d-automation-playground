

using System;
using System.Collections.Generic;

namespace Automation.Runtime.QueryLanguage
{

    /*
     * QUERY:
     * 
     *  "/NAME/NAME"
     *  "/NAME/*"
     *  "/** /NAME"
     *  "[@instance_id=1234]"
     *  "[@automation_id=1234]"
     *  "[@components=MeshRenderer]"     //  this is odd as there is multiple components ... 
     *  "[1]"
     *  "[last()]"
     *  "@attribute"
     * 
     *  /* / -  Recursive Wildcard - one step 
     *  /** / - Recursive Wildcard - many steps 
     *  
     *  
     *  //  support for AND / OR in filters ... 
     * 
     * 
     *   // tag 
     *   // layer
     *   
     *   // at any point a query might represent a single or multiple nodes ... 
     *   //     maybe it always represents multiple nodes, and the whole query is more of a filter... 
     *   
     *   '[...]' - Filter
     *   
     *   '=' - Match
     *   '^' - Contains  // is Contains just the same as Match but for one in an array ?? 
     *   
     *   ':' - Function
     * 
     *  EXECUTE:
     *  
     *   "fn:enable()"
     * 
     */

    //  eg. "/application/info/@unityVersion"
    //  eg. "/scene/**[@automation_id='main_player']/@position"
    //  eg. "/scene/Camera/*[contains(@component,'MeshRenderer') and @tag='water']/@enable()"

    public enum TokenType
    {
        String,
        AttributeMarker,
        Slash,

        Identifier,
        Namespacing,

        Comma,
        Equals,

        OpenParen,
        CloseParen,

        OpenBracket,
        CloseBracket,

        Match,  //  Matching Function

        Boolean,
        Number,

        Wildcard,
        RecursiveWildcard,

        Function,

        Or,
        And,
        Not,
    }

    public struct Token
    {
        public TokenType Type;
        public string Value;

        //  Location
        //      line and column -- could probably just be a character index ...

        public static readonly Token Slash = new Token
        {
            Type = TokenType.Slash,
            Value = "/"
        };

        public static readonly Token AttributeMarker = new Token
        {
            Type = TokenType.AttributeMarker,
            Value = "@"
        };

        public static readonly Token Namspacing = new Token
        {
            Type = TokenType.Namespacing,
            Value = ":"
        };

        public static readonly Token OpenParen = new Token
        {
            Type = TokenType.OpenParen,
            Value = "("
        };

        public static readonly Token CloseParen = new Token
        {
            Type = TokenType.CloseParen,
            Value = ")"
        };

        public static readonly Token Comma = new Token
        {
            Type = TokenType.Comma,
            Value = ","
        };

        public static readonly Token Equals = new Token
        {
            Type = TokenType.Equals,
            Value = "="
        };

        public static readonly Token OpenBracket = new Token
        {
            Type = TokenType.OpenBracket,
            Value = "["
        };

        public static readonly Token CloseBracket = new Token
        {
            Type = TokenType.CloseBracket,
            Value = "]"
        };
    }

    public static class Symbols
    {
        public const char Slash = (char)0x2f;               // /
        public const char Asterisk = (char)0x2a;               // *
        public const char At = (char)0x40;               // @
        public const char SquareBracketOpen = (char)0x5b;   // [
        public const char SquareBracketClose = (char)0x5d;  // ]
        public const char Equal = (char)0x3d;               // =
        public const char LessThan = (char)0x3c;            // <
        public const char GreaterThan = (char)0x3e;         // >
        public const char Comma = (char)0x2c;               // ,
        public const char SingleQuote = (char)0x27;         // '
        public const char DoubleQuote = (char)0x22;         // "
        public const char ParenOpen = (char)0x28;           // (
        public const char ParenClose = (char)0x29;          // )
        public const char Colon = (char)0x3a;               // :
    }

    /*

        /
        *
        **
        [...]
        @...

        =
        <
        >
        <=
        >=
        '...'
        contains(...)
        last()
        and
        or
        not
        @...


        slicing - [1:5]
        number and string expressions ??? 

     */

    public class Tokenizer
    {

        //-------------------------------------------------------------------------
        private string mCharacterSequence;

        //-------------------------------------------------------------------------
        private char mCurrentChar;
        private int mCursor;
        private int mLength;

        //-------------------------------------------------------------------------
        public Token NextToken()
        {
            SkipWhitespace();

            //  consider invalid symbols ... 

            //  Pattern Matching ...  
            Token token = mCurrentChar switch
            {
                Symbols.Slash => TokenizeSingleCharacter(Token.Slash),
                Symbols.Asterisk => TokenizeWildcard(),
                Symbols.At => TokenizeSingleCharacter(Token.AttributeMarker),
                Symbols.SingleQuote => TokenizeQuotedString(Symbols.SingleQuote),
                Symbols.DoubleQuote => TokenizeQuotedString(Symbols.DoubleQuote),
                Symbols.Colon => TokenizeSingleCharacter(Token.Namspacing),
                Symbols.ParenOpen => TokenizeSingleCharacter(Token.OpenParen),
                Symbols.ParenClose => TokenizeSingleCharacter(Token.CloseParen),
                Symbols.Comma => TokenizeSingleCharacter(Token.Comma),
                Symbols.Equal => TokenizeSingleCharacter(Token.Equals),
                Symbols.SquareBracketOpen => TokenizeSingleCharacter(Token.OpenBracket),
                Symbols.SquareBracketClose => TokenizeSingleCharacter(Token.CloseBracket),
                //  number
                //  operators   equal
                _ when mCurrentChar != '\0' => TokenizeIdentifier(),
                _ => default(Token),
            };

            return token;
        }

        //-------------------------------------------------------------------------
        private Token TokenizeSingleCharacter(Token token)
        {
            MoveCursor();
            return token;
        }

        //-------------------------------------------------------------------------
        private Token TokenizeWildcard()
        {
            MoveCursor();

            if (mCurrentChar == Symbols.Asterisk)
            {
                MoveCursor();
                return new Token { Type = TokenType.RecursiveWildcard };
            }
            else
            {
                return new Token { Type = TokenType.Wildcard };
            }
        }

        //-------------------------------------------------------------------------
        private Token TokenizeQuotedString(char quoteCharacter = Symbols.DoubleQuote)
        {
            MoveCursor();

            int start = mCursor;
            int end = mCharacterSequence.IndexOf(quoteCharacter, start);

            if (end == -1)
            {
                throw new Exception("Unterminated string");
            }

            int length = end - start;
            string value = mCharacterSequence.Substring(start, length);

            MoveCursor(length);

            MoveCursor();

            return new Token
            {
                Type = TokenType.String,
                Value = value
            };
        }

        //-------------------------------------------------------------------------
        private Token TokenizeIdentifier()      //  refactor to TokenizeIdentifierOrKeyword
        {
            //  what are the constraint of an identifier ...
            //    a-z, A-Z, 0-9, _, -

            string identifier = string.Empty;

            while (!IsEOF())
            {
                // should function this up 
                if (char.IsLetterOrDigit(mCurrentChar) || mCurrentChar == '_' || mCurrentChar == '-')
                {
                    identifier += mCurrentChar;
                    MoveCursor();
                }
                else
                {
                    break;
                }
            }

            //  is keyword 
            switch (identifier)
            {
                case "true":
                    return new Token { Type = TokenType.Boolean, Value = "true" };
                default:
                    return new Token { Type = TokenType.Identifier, Value = identifier };
            }
        }

        //-------------------------------------------------------------------------
        private bool IsEOF() => mCursor >= mLength;

        //-------------------------------------------------------------------------
        private void MoveCursor(int count = 1)
        {
            mCursor += count;
            mCurrentChar = IsEOF() ? '\0' : mCharacterSequence[mCursor];
        }

        //-------------------------------------------------------------------------
        private void SkipWhitespace()
        {
            while (char.IsWhiteSpace(mCurrentChar))
            {
                MoveCursor();
            }
        }

        //-------------------------------------------------------------------------
        public List<Token> Tokenize(string query)
        {
            mCursor = 0;
            mLength = query.Length;
            mCharacterSequence = query;
            mCurrentChar = mCharacterSequence[mCursor];

            List<Token> tokenSequence = new List<Token>();

            while (IsEOF() == false)
            {
                Token token = NextToken();

                tokenSequence.Add(token);
            }

            return tokenSequence;
        }

    }

}