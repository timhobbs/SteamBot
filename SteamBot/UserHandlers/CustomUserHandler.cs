using System;
using System.Linq;
using SteamKit2;
using SteamTrade;

namespace SteamBot.UserHandlers
{
    /// <summary>
    /// A user handler class that implements basic text-based commands entered in
    /// chat or trade chat.
    /// </summary>
    public class CustomUserHandler : UserHandler
    {
        private const string AddCmd = "add";
        private const string RemoveCmd = "remove";
        private const string AddCratesSubCmd = "crates";
        private const string AddWepsSubCmd = "weapons";
        private const string AddMetalSubCmd = "metal";
        private const string AllSubCmd = "all";
        private const string HelpCmd = "help";
        private const string InventoryCommand = "inv";

        private Bot bot;

        public CustomUserHandler(Bot bot, SteamID sid)
            : base(bot, sid)
        {
            this.bot = bot;
        }

        #region Overrides of UserHandler

        /// <summary>
        /// Called when a the user adds the bot as a friend.
        /// </summary>
        /// <returns>
        /// Whether to accept.
        /// </returns>
        public override bool OnFriendAdd()
        {
            // if the other is an admin then accept add
            if (IsAdmin)
            {
                return true;
            }

            Log.Warn(String.Format("Random SteamID: {0} tried to add the bot as a friend", OtherSID));
            return false;
        }

        public override void OnFriendRemove()
        {
        }

        /// <summary>
        /// Called whenever a message is sent to the bot.
        /// This is limited to regular and emote messages.
        /// </summary>
        public override void OnMessage(string message, EChatEntryType type)
        {
            // TODO: magic command system
        }

        /// <summary>
        /// Called whenever a user requests a trade.
        /// </summary>
        /// <returns>
        /// Whether to accept the request.
        /// </returns>
        public override bool OnTradeRequest()
        {
            if (IsAdmin)
                return true;

            return false;
        }

        public override void OnTradeError(string error)
        {
            Log.Error(error);
        }

        public override void OnTradeTimeout()
        {
            Log.Warn("Trade timed out.");
        }

        public override void OnTradeInit()
        {
            Trade.SendMessage(String.Format("Success. (Type {0} for commands.)", HelpCmd));
        }

        public override void OnTradeAddItem(Schema.Item schemaItem, Inventory.Item inventoryItem)
        {
            // whatever.   
        }

        public override void OnTradeRemoveItem(Schema.Item schemaItem, Inventory.Item inventoryItem)
        {
            // whatever.
        }

        public override void OnTradeMessage(string message)
        {
            ProcessTradeMessage(message);
        }

        public override void OnTradeReady(bool ready)
        {
            if (!IsAdmin)
            {
                Trade.SendMessage("You are not my master.");
                Trade.SetReady(false);
                return;
            }

            Trade.SetReady(true);
        }

        public override void OnTradeAccept()
        {
            if (IsAdmin)
            {
                bool ok = Trade.AcceptTrade();

                if (ok)
                {
                    Log.Success("Trade was Successful!");
                }
                else
                {
                    Log.Warn("Trade might have failed.");
                }
            }
        }

        #endregion

        private void ProcessTradeMessage(string message)
        {
            if (message.Equals(HelpCmd))
            {
                PrintHelpMessage();
                return;
            }

            // View inventory
            if (message.StartsWith(InventoryCommand))
            {
                HandleInventoryCommand(message);
                return;
            } 
            
            if (message.StartsWith(AddCmd))
                HandleAddCommand(message);
            else if (message.StartsWith(RemoveCmd))
                HandleRemoveCommand(message);
        }

        private void PrintHelpMessage()
        {
            Trade.SendMessage(String.Format("{0} {1} - adds all crates", AddCmd, AddCratesSubCmd));
            Trade.SendMessage(String.Format("{0} {1} - adds all metal", AddCmd, AddMetalSubCmd));
            Trade.SendMessage(String.Format("{0} {1} - adds all weapons", AddCmd, AddWepsSubCmd));
            Trade.SendMessage(String.Format("{0} {1} - show bot crates", InventoryCommand, AddCratesSubCmd));
            Trade.SendMessage(String.Format("{0} {1} - show bot metal", InventoryCommand, AddMetalSubCmd));
            Trade.SendMessage(String.Format("{0} {1} - show bot weapons", InventoryCommand, AddWepsSubCmd));
            Trade.SendMessage(String.Format(@"{0} <craft_material_type> [amount] - adds all or a given amount of items of a given crafing type.", AddCmd));
            Trade.SendMessage(String.Format(@"{0} <defindex> [amount] - adds all or a given amount of items of a given defindex.", AddCmd));

            Trade.SendMessage(@"See http://wiki.teamfortress.com/wiki/WebAPI/GetSchema for info about craft_material_type or defindex.");
        }

        private void HandleInventoryCommand(string command)
        {
            var data = command.Split(' ');
            string typeToFind;

            bool subCmdOk = GetSubCommand(data, out typeToFind);

            if (!subCmdOk) return;

            switch (typeToFind)
            {
                case AddMetalSubCmd:
                    typeToFind = "craft_bar";
                    break;
                case AddWepsSubCmd:
                    typeToFind = "weapon";
                    break;
                case AddCratesSubCmd:
                    typeToFind = "supply_crate";
                    break;
            }

            var items = Trade.CurrentSchema.GetItemsByCraftingMaterial(typeToFind);
            var results = new System.Collections.Generic.Dictionary<ushort, string>();

            foreach (var i in items)
            {
                foreach (Inventory.Item item in Trade.MyInventory.GetItemsByDefindex(i.Defindex))
                {
                    if (item != null && !item.IsNotTradeable && !results.ContainsKey(item.Defindex))
                    {
                        results.Add(item.Defindex, i.Name);
                        Trade.SendMessage(String.Format("{0} [{1}]", i.Name, item.Defindex));
                    }
                }
            }
        }

        private void HandleAddCommand(string command)
        {
            var data = command.Split(' ');
            string typeToAdd;

            bool subCmdOk = GetSubCommand(data, out typeToAdd);

            if (!subCmdOk)
                return;

            uint amount = GetAddAmount(data);

            // if user supplies the defindex directly use it to add.
            int defindex;
            if (int.TryParse(typeToAdd, out defindex))
            {
                Trade.AddAllItemsByDefindex(defindex, amount);
                return;
            }

            switch (typeToAdd)
            {
                case AddMetalSubCmd:
                    AddItemsByCraftType("craft_bar", amount);
                    break;
                case AddWepsSubCmd:
                    AddItemsByCraftType("weapon", amount);
                    break;
                case AddCratesSubCmd:
                    AddItemsByCraftType("supply_crate", amount);
                    break;
                default:
                    AddItemsByCraftType(typeToAdd, amount);
                    break;
            }
        }



        private void HandleRemoveCommand(string command)
        {
            var data = command.Split(' ');

            string subCommand;

            bool subCmdOk = GetSubCommand(data, out subCommand);

            if (!subCmdOk)
                return;
        }


        private void AddItemsByCraftType(string typeToAdd, uint amount)
        {
            var items = Trade.CurrentSchema.GetItemsByCraftingMaterial(typeToAdd);

            uint added = 0;

            foreach (var item in items)
            {
                added += Trade.AddAllItemsByDefindex(item.Defindex, amount);

                // if bulk adding something that has a lot of unique
                // defindex (weapons) we may over add so limit here also
                if (amount > 0 && added >= amount)
                    return;
            }
        }

        bool GetSubCommand(string[] data, out string subCommand)
        {
            if (data.Length < 2)
            {
                Trade.SendMessage(String.Format("No parameter for cmd: {0}", AddCmd));
                subCommand = null;
                return false;
            }

            if (String.IsNullOrEmpty(data[1]))
            {
                Trade.SendMessage(String.Format("No parameter for cmd: {0}", AddCmd));
                subCommand = null;
                return false;
            }

            subCommand = data[1];

            return true;
        }

        static uint GetAddAmount(string[] data)
        {
            uint amount = 0;

            if (data.Length > 2)
            {
                // get the optional ammount parameter
                if (!String.IsNullOrEmpty(data[2]))
                {
                    uint.TryParse(data[2], out amount);
                }
            }

            return amount;
        }

        private class InventoryItem
        {
            public string Name { get; set; }

            public ushort Defindex { get; set; }
        }
    }
}