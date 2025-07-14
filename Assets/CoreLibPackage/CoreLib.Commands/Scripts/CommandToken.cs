using System;
using System.Globalization;
using System.Linq;
using CoreLib.Commands.CoreLibPackage.CoreLib.Commands.Scripts.Commands;
using PugTilemap;
using QFSW.QC.Utilities;

// ReSharper disable MemberCanBePrivate.Global

namespace CoreLib.Commands
{
    public enum TokenType
    {
        Unknown,
        Text,
        Number,
        Boolean,
        ObjectID,
        Tileset,
        TileType,
        Position,
        Custom
    }
    
    public struct CommandToken
    {
        public CommandToken(TokenType tokenType, string text, string parsedValue) : this()
        {
            this.text = text;
            this.tokenType = tokenType;
            this.parsedValue = parsedValue;
        }

        public CommandToken(int customTokenType, string text, string parsedValue) : this()
        {
            this.text = text;
            tokenType = TokenType.Custom;
            this.customTokenType = customTokenType;
            this.parsedValue = parsedValue;
        }

        public readonly string text;
        public readonly TokenType tokenType;
        public int customTokenType;
        
        public string parsedValue;

        public bool TryAutocomplete(ICommandParser parser, out string newValue)
        {
            Tuple<string, int>[] matches;
            switch (tokenType)
            {
                case TokenType.Boolean:
                    if ("true".StartsWith(text, true, CultureInfo.InvariantCulture))
                    {
                        newValue = "true";
                        return true;
                    }
                    if ("false".StartsWith(text, true, CultureInfo.InvariantCulture)){
                        newValue = "false";
                        return true;
                    }
                    break;
                
                case TokenType.ObjectID:
                    matches = CommandUtil.FindMatchesForObjectName(this);
                    if (matches.Length == 0) break;

                    newValue = matches[0].Item1.ToLower();
                    
                    if (text.Contains('_'))
                        newValue = newValue.Replace(' ', '_');
                    
                    return true;
                case TokenType.Tileset:
                    matches = CommandUtil.FindMatchesForEnum<Tileset>(this);
                    if (matches.Length == 0) break;

                    newValue = matches[0].Item1.ToLower();
                    
                    if (text.Contains('_'))
                        newValue = newValue.Replace(' ', '_');
                    
                    return true;
                case TokenType.TileType:
                    matches = CommandUtil.FindMatchesForEnum<TileType>(this);
                    if (matches.Length == 0) break;

                    newValue = matches[0].Item1.ToLower();
                    
                    if (text.Contains('_'))
                        newValue = newValue.Replace(' ', '_');
                    
                    return true;
                case TokenType.Custom:
                    return parser.TryAutocomplete(this, out newValue);
            }
            
            newValue = null;
            return false;
        }
    }
}