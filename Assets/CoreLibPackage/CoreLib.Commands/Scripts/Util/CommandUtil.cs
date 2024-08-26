using System;
using System.Collections.Generic;
using System.Linq;
using CoreLib.Commands.Communication;
using HarmonyLib;
using PugMod;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;

namespace CoreLib.Commands
{
    public static class CommandUtil
    {
        public static CommandOutput ParseItemName(string fullName, out ObjectID objectID)
        {
            if (Enum.TryParse(fullName, true, out ObjectID objId))
            {
                objectID = objId;
                return "";
            }

            string[] keys = CommandsModule.friendlyNameDict.Keys.Where(s => s.Contains(fullName)).ToArray();
            if (keys.Length == 0)
            {
                CoreLibMod.Log.LogDebug($"friendlyNameDict state: {CommandsModule.friendlyNameDict.Count} entries, first entries: {CommandsModule.friendlyNameDict.Keys.Take(10).Join()}");
                objectID = ObjectID.None; 
                return new CommandOutput($"No item named '{fullName}' found!", CommandStatus.Error);
            }

            if (keys.Length > 1)
            {
                try
                {
                    string key = keys.First(s => s.Equals(fullName));
                    objectID = CommandsModule.friendlyNameDict[key];
                    return "";
                }
                catch (Exception)
                {
                    objectID = ObjectID.None;
                    return new CommandOutput(
                        $"Ambigous match ({keys.Length} results):\n{keys.Take(10).Join(null, "\n")}{(keys.Length > 10 ? "\n..." : "")}",
                        CommandStatus.Error);
                }
            }

            objectID = CommandsModule.friendlyNameDict[keys[0]];
            return "";
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
    }
}