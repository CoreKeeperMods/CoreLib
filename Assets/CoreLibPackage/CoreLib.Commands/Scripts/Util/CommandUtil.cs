using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CoreLib.Commands.Communication;
using HarmonyLib;
using Pug.UnityExtensions;
using PugMod;
using PugTilemap;
using QFSW.QC.Utilities;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;

namespace CoreLib.Commands
{
    public static class CommandUtil
    {
        public static int maxMatchDistance = 20;
        
        public static CommandOutput ParseItemName(string fullName, out ObjectID objectID)
        {
            if (Enum.TryParse(fullName, true, out ObjectID objId))
            {
                objectID = objId;
                return "";
            }

            fullName = fullName.Replace("_", " ");

            string[] keys = CommandsModule.friendlyNameDict.Keys.Where(s => s.Contains(fullName)).ToArray();
            if (keys.Length == 0)
            {
                objectID = ObjectID.None; 
                return new CommandOutput($"No item named '{fullName}' found!", CommandStatus.Error);
            }

            var pairs = new Dictionary<ObjectID, string>();
            foreach (var key in keys)
            {
                objectID = CommandsModule.friendlyNameDict[key];
                if (pairs.TryAdd(objectID, key)) continue;
                
                var otherKey = pairs[objectID];
                if (key.Count(c => c == ' ') > otherKey.Count(c => c == ' ') &&
                    key.Count(c => c == ':') < otherKey.Count(c => c == ':'))
                {
                    pairs[objectID] = otherKey;
                }
            }
            
            if (pairs.Count > 1)
            {
                try
                {
                    var key = pairs.First(s => s.Value.Equals(fullName));
                    objectID = key.Key;
                    return "";
                }
                catch (Exception)
                {
                    objectID = ObjectID.None;
                    return new CommandOutput(
                        $"Ambiguous match ({pairs.Count} results):\n{pairs.Values.Take(10).Join(null, "\n")}{(pairs.Count > 10 ? "\n..." : "")}",
                        CommandStatus.Error);
                }
            }

            objectID = pairs.First().Key;
            return "";
        }

        public static Tuple<string, int>[] FindMatchesForObjectName(CommandToken token)
        {
            var fullName = token.text.Replace("_", " ");
            return CommandsModule.friendlyNameDict.Keys
                .Select(name => new Tuple<string, int>(name, name.ComputeLevenshteinDistance(fullName)))
                .OrderBy(pair => pair.Item2)
                .Take(10)
                .Where(pair => pair.Item2 <= maxMatchDistance)
                .ToArray();
        }

        public static Tuple<string, int>[] FindMatchesForEnum<T>(CommandToken token) where T : struct, Enum
        {
            var names = Enum.GetNames(typeof(T));
                        
            return names.Select(name => new Tuple<string, int>(name, name.ComputeLevenshteinDistance(token.text)))
                .OrderBy(pair => pair.Item2)
                .Take(10)
                .Where(pair => pair.Item2 <= maxMatchDistance)
                .ToArray();
        }
        
        /// <summary>
        /// Splits the string passed in by the delimiters passed in.
        /// Quoted sections are not split, and all tokens have whitespace
        /// trimmed from the start and end.
        public static string[] SmartSplit(this string input, params char[] delimiters)
        {
            List<string> results = new List<string>();

            bool inQuote = false;
            StringBuilder currentToken = new StringBuilder();
            for (int index = 0; index < input.Length; ++index)
            {
                char currentCharacter = input[index];
                if (currentCharacter == '"')
                {
                    // When we see a ", we need to decide whether we are
                    // at the start or send of a quoted section...
                    inQuote = !inQuote;
                }
                else if (delimiters.Contains(currentCharacter) && inQuote == false)
                {
                    // We've come to the end of a token, so we find the token,
                    // trim it and add it to the collection of results...
                    string result = currentToken.ToString().Trim();
                    if (result != "") results.Add(result);

                    // We start a new token...
                    currentToken = new StringBuilder();
                }
                else
                {
                    // We've got a 'normal' character, so we add it to
                    // the curent token...
                    currentToken.Append(currentCharacter);
                }
            }

            // We've come to the end of the string, so we add the last token...
            string lastResult = currentToken.ToString().Trim();
            if (lastResult != "") results.Add(lastResult);

            return results.ToArray();
        }

        /// <summary>
        /// Parse position data from player input
        /// </summary>
        /// <param name="parameters">List of all arguments</param>
        /// <param name="startIndex">Index from which to look</param>
        /// <param name="playerPos">Player's world position</param>
        /// <param name="commandOutput">If not not null, error message to the player</param>
        /// <returns>Parsed position in world space</returns>
        public static int2 ParsePos(string[] parameters, int startIndex, float3 playerPos, out CommandOutput? commandOutput)
        {
            string xPosStr = parameters[startIndex - 1];
            string zPosStr = parameters[startIndex];
            
            int xPos;
            int zPos;

            int2 intPlayerPos = playerPos.RoundToInt2();

            try
            {
                xPos = ParsePosAxis(xPosStr, intPlayerPos.x);
                zPos = ParsePosAxis(zPosStr, intPlayerPos.y);
            }
            catch (Exception)
            {
                commandOutput = new CommandOutput("Failed to parse position parameters!", CommandStatus.Error);
                return int2.zero;
            }

            commandOutput = null;
            return new int2(xPos, zPos);
        }

        private static int ParsePosAxis(string posText, int playerPos)
        {
            if (posText[0] == '~')
            {
                return playerPos + int.Parse(posText[1..]);
            }

            return int.Parse(posText);
        }

        public static Color GetColor(this CommandStatus status)
        {
            switch (status)
            {
                case CommandStatus.None:
                    return Color.white;
                case CommandStatus.Info:
                    return Color.green;
                case CommandStatus.Hint:
                    return Color.blue;
                case CommandStatus.Warning:
                    return Color.yellow;
                case CommandStatus.Error:
                    return Color.red;
            }

            return Color.white;
        }
        
        public static CommandOutput AppendAtStart(this CommandOutput commandOutput, string prefix)
        {
            commandOutput.feedback = $"{prefix}: {commandOutput.feedback}";
            return commandOutput;
        }

        /// <summary>
        /// Get player controller for connection
        /// </summary>
        /// <param name="sender">Target player connection entity</param>
        public static PlayerController GetPlayerController(this Entity sender)
        {
            EntityManager entityManager = API.Server.World.EntityManager;
            Entity playerEntity = GetPlayerEntity(sender);
            if (!entityManager.HasComponent<PlayerGhost>(playerEntity))
                throw new InvalidOperationException("player entity does not have a PlayerGhost component!");
            
            PlayerGhost ghost = entityManager.GetComponentData<PlayerGhost>(playerEntity);

            PlayerController player = Manager.main.allPlayers.FirstOrDefault(pc =>
            {
                return pc.world.EntityManager.GetComponentData<PlayerGhost>(pc.entity)
                    .playerGuid.Equals(ghost.playerGuid);
            });
            return player;
        }

        /// <summary>
        /// Return server entity representing player
        /// </summary>
        /// <param name="sender">Target player connection entity</param>
        public static Entity GetPlayerEntity(this Entity sender)
        {
            EntityManager entityManager = API.Server.World.EntityManager;
            Entity playerEntity = entityManager.GetComponentData<CommandTarget>(sender).targetEntity;
            return playerEntity;
        }

        /// <summary>
        /// Get player name
        /// </summary>
        /// <param name="playerEntity">Server entity representing player</param>
        public static string GetPlayerName(this Entity playerEntity)
        {
            var entityManager = API.Server.World.EntityManager;
            PlayerCustomization customization = entityManager.GetComponentData<PlayerCustomizationCD>(playerEntity).customization;
            return customization.name.Value;
        }

        public static BlobAssetReference<PugDatabase.PugDatabaseBank> GetDatabase(this EntityManager entityManager)
        {
            var database = entityManager.CreateEntityQuery(typeof(PugDatabase.DatabaseBankCD))
                .GetSingleton<PugDatabase.DatabaseBankCD>().databaseBankBlob;
            return database;
        }
        
        public static int AsInt(this string parsedValue) => int.Parse(parsedValue);
        public static bool AsBool(this string parsedValue) => bool.Parse(parsedValue);
        
        public static ObjectID AsObjectID(this string parsedValue) => AsEnum<ObjectID>(parsedValue);
        public static Tileset AsTileset(this string parsedValue) => AsEnum<Tileset>(parsedValue);
        public static TileType AsTileType(this string parsedValue) => AsEnum<TileType>(parsedValue);
        
        
        public static T AsEnum<T>(this string parsedValue) where T : struct, Enum
        {
            if (Enum.TryParse(parsedValue, out T value))
            {
                return value;
            }

            return default;
        }
    }
}