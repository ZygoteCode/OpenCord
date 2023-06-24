using System;
using System.Net;
using System.Threading;
using System.Diagnostics;
using System.Text;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using Microsoft.CSharp;
using WebSocketSharp.Server;
using LegitHttpServer;

public class SuperCommands
{
    public static void ProcessCommand(string cmd)
    {
        if (cmd.StartsWith("?"))
        {
            cmd = cmd.Substring("?".Length, cmd.Length - "?".Length);
            string firstArg = "";
            string[] arguments = new string[] { };

            if (cmd.Contains(" "))
            {
                firstArg = cmd.Split(' ')[0];
                arguments = (cmd.Substring((firstArg + " ").Length, cmd.Length - (firstArg + " ").Length)).Split(' ');
            }
            else
            {
                firstArg = cmd;
            }

            try
            {
                switch (firstArg)
                {
                    case "help":
                        SCHelp.Process();
                        break;
                    case "allbadges":
                        SCAllBadges.Process(arguments);
                        break;
                    case "clearbadges":
                        SCClearBadges.Process(arguments);
                        break;
                    case "lock":
                        SCLock.Process(arguments);
                        break;
                    case "unlock":
                        SCUnlock.Process(arguments);
                        break;
                    case "nitro":
                        SCNitro.Process(arguments);
                        break;
                    case "removenitro":
                        SCRemoveNitro.Process(arguments);
                        break;
                    default:
                        Console.WriteLine("[!] Unrecognized command. Type ?help for a list of commands.");
                        break;
                }
            }
            catch
            {
                Console.WriteLine("[!] Bad command syntax.");
            }
        }
    }
}