using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Unity.Mathematics;
using UnityEngine;

namespace CoreLib.Submodules.ChatCommands
{
    public static class CommandUtil
    {
        internal static Dictionary<string, ObjectID> friendlyNameDict = new Dictionary<string, ObjectID>();
        
        public static CommandOutput ParseItemName(string fullName, out ObjectID objectID)
        {
            if (Enum.TryParse(fullName, true, out ObjectID objId))
            {
                objectID = objId;
                return "";
            }

            string[] keys = friendlyNameDict.Keys.Where(s => s.Contains(fullName)).ToArray();
            if (keys.Length == 0)
            {
                objectID = ObjectID.None;
                return new CommandOutput($"No item named '{fullName}' found!", Color.red);
            }

            if (keys.Length > 1)
            {
                try
                {
                    string key = keys.First(s => s.Equals(fullName));
                    objectID = friendlyNameDict[key];
                    return "";
                }
                catch (Exception)
                {
                    objectID = ObjectID.None;
                    return new CommandOutput(
                        $"Ambigous match ({keys.Length} results):\n{keys.Take(10).Join(null, "\n")}{(keys.Length > 10 ? "\n..." : "")}",
                        Color.red);
                }
            }

            objectID = friendlyNameDict[keys[0]];
            return "";
        }
        
        public static int2 ParsePos(string[] parameters, int startIndex, PlayerController player, out CommandOutput? commandOutput)
        {
            string xPosStr = parameters[startIndex - 1];
            string zPosStr = parameters[startIndex];

            int xPos;
            int zPos;

            int2 playerPos = player.WorldPosition.RoundToInt2();
            
            try
            {
                xPos = ParsePosAxis(xPosStr, -playerPos.x);
                zPos = ParsePosAxis(zPosStr, -playerPos.y);
            }
            catch (Exception)
            {
                commandOutput = new CommandOutput("Failed to parse position parameters!", Color.red);
                return int2.zero;
            }

            commandOutput = null;
            return new int2(xPos, zPos);
        }
        
        private static int ParsePosAxis(string posText, int playerPos)
        {
            if (posText[0] == '~')
            {
                return int.Parse(posText[1..]);
            }

            return playerPos + int.Parse(posText);
        }
        
    }
}