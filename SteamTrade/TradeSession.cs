using System;
using System.Collections.Specialized;
using System.Net;
using Newtonsoft.Json;

namespace SteamTrade
{
    /// <summary>
    /// This class handles the web-based interaction for Steam trades.
    /// </summary>
    public partial class Trade
    {
        static string SteamCommunityDomain = "steamcommunity.com";
        static string SteamTradeUrl = "http://steamcommunity.com/trade/{0}/";

        string sessionId;
        string sessionIdEsc;
        string baseTradeURL;
        string steamLogin;
        CookieContainer cookies;
        

        internal int LogPos { get; set; }

        internal int Version { get; set; }

        StatusObj GetStatus ()
        {
            var data = InitData();
            SetLogPosVersion(data);
            
            string response = GetResult("tradestatus", data);
            return JsonConvert.DeserializeObject<StatusObj> (response);
        }

        #region Trade Web command methods

        /// <summary>
        /// Sends a message to the user over the trade chat.
        /// </summary>
        bool SendMessageWebCmd (string msg)
        {
            var data = InitData();
            SetLogPosVersion(data);
            data.Add("message", msg);

            return Execute("chat", data);
        }
        
        /// <summary>
        /// Adds a specified itom by its itemid.  Since each itemid is
        /// unique to each item, you'd first have to find the item, or
        /// use AddItemByDefindex instead.
        /// </summary>
        /// <returns>
        /// Returns false if the item doesn't exist in the Bot's inventory,
        /// and returns true if it appears the item was added.
        /// </returns>
        bool AddItemWebCmd (ulong itemid, int slot)
        {
            var data = InitData();
            SetItemSlot(data, itemid, slot);

            return Execute("additem", data);
        }
        
        /// <summary>
        /// Removes an item by its itemid.  Read AddItem about itemids.
        /// Returns false if the item isn't in the offered items, or
        /// true if it appears it succeeded.
        /// </summary>
        bool RemoveItemWebCmd (ulong itemid, int slot)
        {
            var data = InitData();
            SetItemSlot(data, itemid, slot);

            return Execute("removeitem", data);
        }
        
        /// <summary>
        /// Sets the bot to a ready status.
        /// </summary>
        bool SetReadyWebCmd (bool ready)
        {
            var data = InitData();
            data.Add ("ready", ready ? "true" : "false");
            data.Add ("version", Version.ToString());

            return Execute("toggleready", data);
        }
        
        /// <summary>
        /// Accepts the trade from the user.  Returns a deserialized
        /// JSON object.
        /// </summary>
        bool AcceptTradeWebCmd ()
        {
            var data = InitData();
            data.Add ("version", Version.ToString());

            return Execute("confirm", data);
        }
        
        /// <summary>
        /// Cancel the trade.  This calls the OnClose handler, as well.
        /// </summary>
        bool CancelTradeWebCmd ()
        {
            var data = InitData();

            return Execute("cancel", data);
        }

        #endregion Trade Web command methods
        
        string Fetch (string url, string method, NameValueCollection data = null)
        {
            return SteamWeb.Fetch (url, method, data, cookies);
        }

        void Init()
        {
            sessionIdEsc = Uri.UnescapeDataString(sessionId);

            Version = 1;

            cookies = new CookieContainer();
            cookies.Add (new Cookie ("sessionid", sessionId, String.Empty, SteamCommunityDomain));
            cookies.Add (new Cookie ("steamLogin", steamLogin, String.Empty, SteamCommunityDomain));

            baseTradeURL = String.Format (SteamTradeUrl, OtherSID.ConvertToUInt64 ());
        }

        private string GetResult(string cmd, NameValueCollection data)
        {
            return Fetch(String.Format("{0}{1}", baseTradeURL, cmd), "POST", data);
        }

        private bool Execute(string cmd, NameValueCollection data)
        {
            string result = GetResult(cmd, data);

            dynamic json = JsonConvert.DeserializeObject(result);

            if (json == null || json.success != "true")
            {
                return false;
            }

            return true;
        }

        private NameValueCollection InitData()
        {
            var data = new NameValueCollection();

            data.Add("sessionid", sessionIdEsc);

            return data;
        }

        private void SetLogPosVersion(NameValueCollection data)
        {
            data.Add("logpos", LogPos.ToString());
            data.Add("version", Version.ToString());
        }

        private void SetItemSlot(NameValueCollection data, ulong itemid, int slot) 
        {
            data.Add ("appid", "440");
            data.Add ("contextid", "2");
            data.Add ("itemid", itemid.ToString());
            data.Add ("slot", slot.ToString());
        }

        public class StatusObj
        {
            public string error { get; set; }
            
            public bool newversion { get; set; }
            
            public bool success { get; set; }
            
            public long trade_status { get; set; }
            
            public int version { get; set; }
            
            public int logpos { get; set; }
            
            public TradeUserObj me { get; set; }
            
            public TradeUserObj them { get; set; }
            
            public TradeEvent[] events { get; set; }
        }

        public class TradeEvent
        {
            public string steamid { get; set; }
            
            public int action { get; set; }
            
            public ulong timestamp { get; set; }
            
            public int appid { get; set; }
            
            public string text { get; set; }
            
            public int contextid { get; set; }
            
            public ulong assetid { get; set; }
        }
        
        public class TradeUserObj
        {
            public int ready { get; set; }
            
            public int confirmed { get; set; }
            
            public int sec_since_touch { get; set; }
        }
    }


}

