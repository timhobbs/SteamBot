using System;
using System.Threading;
using SteamKit2;
using SteamTrade;

namespace SteamBot
{
    public class Program
    {
        public static void Main(string[] args)
        {
            if (!System.IO.File.Exists("settings.json"))
            {
                Console.WriteLine("Configuration File Does not exist.");
                return;
            }

            Configuration config = Configuration.LoadConfiguration("settings.json");
            Log mainLog = new Log(config.MainLog, null);
            foreach (Configuration.BotInfo info in config.Bots)
            {
                mainLog.Info(String.Format("Launching Bot {0}...", info.DisplayName));
                new Thread(() =>
                {
                    int crashes = 0;
                    while (crashes < 1000)
                    {
                        try
                        {
                            new Bot(info, config.ApiKey, (Bot bot, SteamID sid) => {
                                    
                                return (SteamBot.UserHandler)System.Activator.CreateInstance(Type.GetType(bot.BotControlClass), new object[] { bot, sid });  
                            }, false);

                        }
                        catch (Exception e)
                        {
                            mainLog.Error(String.Format("Error With Bot: {0}", e));
                            crashes++;
                        }
                    }
                }).Start();
                Thread.Sleep(5000);
            }
        }
    }
}
