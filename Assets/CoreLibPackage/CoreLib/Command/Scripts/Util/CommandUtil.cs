using System;
using System.Linq;
using CoreLib.Submodule.Command.Data;
using HarmonyLib;
using Pug.UnityExtensions;
using PugMod;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace CoreLib.Submodule.Command.Util
{
    /// Provides utility methods for command parsing, execution, and management, such as processing input arguments, retrieving player-related information, and enhancing command outputs.
    public static class CommandUtil
    {
        /// Parses an item name from a given string and attempts to match it to an object ID.
        /// <param name="fullName">The full name of the item to be parsed.</param>
        /// <param name="objectID">An output parameter that will contain the parsed object ID if the parsing is successful.</param>
        /// <returns>A command output representing the result of the parsing. It includes feedback or an error message if parsing fails.</returns>
        public static CommandOutput ParseItemName(string fullName, out ObjectID objectID)
        {
            if (Enum.TryParse(fullName, true, out ObjectID objId))
            {
                objectID = objId;
                return "";
            }

            string[] keys = CommandModule.friendlyNameDict.Keys.Where(s => s.Contains(fullName)).ToArray();
            if (keys.Length == 0)
            {
                CommandModule.log.LogInfo($"friendlyNameDict state: {CommandModule.friendlyNameDict.Count} entries, first entries: {CommandModule.friendlyNameDict.Keys.Take(10).Join()}");
                objectID = ObjectID.None; 
                return new CommandOutput($"No item named '{fullName}' found!", CommandStatus.Error);
            }

            if (keys.Length > 1)
            {
                try
                {
                    string key = keys.First(s => s.Equals(fullName));
                    objectID = CommandModule.friendlyNameDict[key];
                    return "";
                }
                catch (Exception)
                {
                    objectID = ObjectID.None;
                    return new CommandOutput(
                        $"Ambiguous match ({keys.Length} results):\n{keys.Take(10).Join(null, "\n")}{(keys.Length > 10 ? "\n..." : "")}",
                        CommandStatus.Error);
                }
            }

            objectID = CommandModule.friendlyNameDict[keys[0]];
            return "";
        }

        /// Parses a position from player input parameters, adjusting based on the player's current position if necessary.
        /// <param name="parameters">An array of input arguments provided by the player.</param>
        /// <param name="startIndex">The index of the parameter list to begin parsing.</param>
        /// <param name="playerPos">The current position of the player in world space as a float3.</param>
        /// <param name="commandOutput">Optional output parameter that contains feedback or error information if parsing fails.</param>
        /// <returns>A 2D integer vector representing the parsed position in world space.</returns>
        public static int2 ParsePos(string[] parameters, int startIndex, float3 playerPos,
            out CommandOutput? commandOutput)
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

        /// Parses a single axis value from player input text.
        /// <param name="posText">The input string representing the axis value, possibly containing a tilde (~) for relative positioning.</param>
        /// <param name="playerPos">The player's current position along the axis to resolve relative positioning.</param>
        /// <returns>The parsed absolute axis value as an integer.</returns>
        private static int ParsePosAxis(string posText, int playerPos)
        {
            if (posText[0] == '~')
            {
                return playerPos + int.Parse(posText[1..]);
            }

            return int.Parse(posText);
        }

        /// Retrieves a color representation associated with a given command status.
        /// <param name="status">The command status from which to derive the color.</param>
        /// <returns>A color that represents the provided command status.</returns>
        public static Color GetColor(this CommandStatus status)
        {
            return status switch
            {
                CommandStatus.Info => Color.green,
                CommandStatus.Hint => Color.blue,
                CommandStatus.Warning => Color.yellow,
                CommandStatus.Error => Color.red,
                _ => Color.white
            };
        }

        /// Appends a specified prefix to the start of the feedback message in the given CommandOutput instance.
        /// <param name="commandOutput">The CommandOutput instance whose feedback message will be modified.</param>
        /// <param name="prefix">The prefix string to prepend to the feedback message.</param>
        /// <returns>The updated CommandOutput instance with the prefixed feedback message.</returns>
        public static CommandOutput AppendAtStart(this CommandOutput commandOutput, string prefix)
        {
            commandOutput.feedback = $"{prefix}: {commandOutput.feedback}";
            return commandOutput;
        }

        /// Retrieves the player controller associated with a given player connection entity.
        /// <param name="sender">The player connection entity for which the controller is being retrieved.</param>
        /// <returns>The player controller associated with the specified player connection entity.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the player entity does not have a PlayerGhost component.</exception>
        public static PlayerController GetPlayerController(this Unity.Entities.Entity sender)
        {
            EntityManager entityManager = API.Server.World.EntityManager;
            Unity.Entities.Entity playerEntity = GetPlayerEntity(sender);
            if (!entityManager.HasComponent<PlayerGhost>(playerEntity))
                throw new InvalidOperationException("player entity does not have a PlayerGhost component!");
            
            PlayerGhost ghost = entityManager.GetComponentData<PlayerGhost>(playerEntity);

            PlayerController player = Manager.main.allPlayers
                .FirstOrDefault(pc => pc.world.EntityManager.GetComponentData<PlayerGhost>(pc.entity)
                .playerGuid.Equals(ghost.playerGuid));
            return player;
        }

        /// Retrieves the server entity associated with the player connection entity.
        /// <param name="sender">The entity representing the player connection.</param>
        /// <returns>The entity representing the player in the server world.</returns>
        public static Unity.Entities.Entity GetPlayerEntity(this Unity.Entities.Entity sender)
        {
            EntityManager entityManager = API.Server.World.EntityManager;
            Unity.Entities.Entity playerEntity = entityManager.GetComponentData<CommandTarget>(sender).targetEntity;
            return playerEntity;
        }

        /// Retrieves the name of the player associated with the specified server entity.
        /// <param name="playerEntity">The server entity representing the player.</param>
        /// <returns>The player's name as a string.</returns>
        public static string GetPlayerName(this Unity.Entities.Entity playerEntity)
        {
            var entityManager = API.Server.World.EntityManager;
            var customization = entityManager.GetComponentData<PlayerCustomizationCD>(playerEntity).customization;
            return customization.name.Value;
        }

        /// Retrieves the database bank blob reference associated with the specified EntityManager.
        /// <param name="entityManager">The EntityManager instance used to query the database bank.</param>
        /// <returns>A reference to the database bank blob asset.</returns>
        public static BlobAssetReference<PugDatabase.PugDatabaseBank> GetDatabase(this EntityManager entityManager)
        {
            var database = entityManager.CreateEntityQuery(typeof(PugDatabase.DatabaseBankCD))
                .GetSingleton<PugDatabase.DatabaseBankCD>().databaseBankBlob;
            return database;
        }
    }
}