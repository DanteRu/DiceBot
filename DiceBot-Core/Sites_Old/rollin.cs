﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DiceBotCore.Sites_Old
{
    class rollin: DiceSite
    {
        string server_hash = "";
        string client = "";
        string username = "";
        Random R = new Random();
        HttpClientHandler ClientHandlr;// = new HttpClientHandler();
        HttpClient Client;
        public rollin()
        {
            maxRoll = 99;
            
            AutoWithdraw = true;
            AutoInvest = false;
            Tip = true;
            TipUsingName = true;
            ChangeSeed = true;
            Name = "RollinIO";
            Thread t = new Thread(new ThreadStart(SyncThread));
            t.Start();
            SiteURL = "https://rollin.io/ref/8c4";
            
            
        }
        DateTime lastbet = DateTime.Now;
        DateTime LastBalance = DateTime.Now;
        void SyncThread()
        {
            while (isRollin)
            {
                if (Token!="" && Token!=null && username!="" && (DateTime.Now - LastBalance).TotalSeconds>15)
                {
                    try
                    {


                        string sEmitResponse2 = Client.GetStringAsync("customer/sync").Result;
                        RollinBet tmpStats2 = Helpers.json.JsonDeserialize<RollinBet>(sEmitResponse2);
                        if (tmpStats2.success)
                        {
                            balance = (double.Parse(tmpStats2.customer.balance, System.Globalization.NumberFormatInfo.InvariantInfo) / 1000.0);
                            //Parent.updateBalance(balance);
                        }
                        LastBalance = DateTime.Now;
                    }
                    catch (Exception e)
                    {
                        //Parent.updateStatus(e.Message);
                    }
                }
                System.Threading.Thread.Sleep(500);
            }
        }
        int retrycount = 0;
        void PlaceBetThread(object _High)
        {
            try
            {
                lastbet = DateTime.Now;
                bool High = (bool)_High;
                double tmpchance = High ? 99.99 - chance : chance;
                //Parent.updateStatus(string.Format("Betting: {0:0.00000000} at {1:0.00000000} {2}", amount, chance, High ? "High" : "Low"));
                List<KeyValuePair<string, string>> pairs = new List<KeyValuePair<string, string>>();
                pairs.Add(new KeyValuePair<string, string>("bet_amount", (amount * 1000).ToString("0.00000", System.Globalization.NumberFormatInfo.InvariantInfo)));
                pairs.Add(new KeyValuePair<string, string>("bet_number", tmpchance.ToString("0", System.Globalization.NumberFormatInfo.InvariantInfo)));
                pairs.Add(new KeyValuePair<string, string>("prediction", High ? "bigger" : "smaller"));
                pairs.Add(new KeyValuePair<string, string>("seed", R.Next(int.MaxValue).ToString()));
                
                FormUrlEncodedContent Content = new FormUrlEncodedContent(pairs);
                string sEmitResponse = Client.PostAsync("games/dice/play", Content).Result.Content.ReadAsStringAsync().Result;
                RollinBet tmp = Helpers.json.JsonDeserialize<RollinBet>(sEmitResponse);
                if (tmp.errors != null && tmp.errors.Length>0)
                {
                    //Parent.updateStatus(tmp.errors[0]);
                }
                else
                {
                    Bet tmp2 = tmp.ToBet();
                    tmp2.ServerHash = server_hash;
                    server_hash = tmp.customer.server_hash;
                    balance = double.Parse(tmp.customer.balance, System.Globalization.NumberFormatInfo.InvariantInfo) / 1000.0;
                    bets = tmp.statistics.bets;
                    
                    losses = tmp.statistics.losses;
                    profit = double.Parse(tmp.statistics.profit, System.Globalization.CultureInfo.InvariantCulture) / 1000.0;
                    
                    wagered = double.Parse(tmp.statistics.wagered, System.Globalization.CultureInfo.InvariantCulture) / 1000.0;
                    wins = (tmp.statistics.wins);
                    LastBalance = DateTime.Now;
                    FinishedBet(tmp2);
                    retrycount = 0;
                }

            }
            catch (Exception E)
            {
                if (retrycount++ < 3)
                {
                    PlaceBetThread(High);
                    return;
                }
                //Parent.updateStatus(E.Message);
               /* if (//Parent.logging > 1)
                using (StreamWriter sw = File.AppendText("log.txt"))
                {
                    sw.WriteLine(E.Message);
                    sw.WriteLine(E.StackTrace);
                    sw.WriteLine(Helpers.json.JsonSerializer<System.Collections.IDictionary>(E.Data));
                }*/
            }

        }
        protected override void internalPlaceBet(bool High)
        {

            Thread T = new Thread(new ParameterizedThreadStart(PlaceBetThread));
            T.Start(High);
        }

        public override void ResetSeed()
        {
            
            
            string sEmitResponse = Client.GetStringAsync("customer/seed/randomize").Result;
            RollinRandomize rand = Helpers.json.JsonDeserialize<RollinRandomize>(sEmitResponse);
            if (rand.success)
            {
                this.client = rand.client_seed;
                this.server_hash = rand.server_hash;
            }
        }

        public override void SetClientSeed(string Seed)
        {
            throw new NotImplementedException();
        }

        protected override bool internalWithdraw(double Amount, string Address)
        {
            try
            {
                List<KeyValuePair<string, string>> pairs = new List<KeyValuePair<string, string>>();
                pairs.Add(new KeyValuePair<string, string>("address", Address));
                pairs.Add(new KeyValuePair<string, string>("amount", (Amount * 1000).ToString("0.00000", System.Globalization.NumberFormatInfo.InvariantInfo)));
                FormUrlEncodedContent Content = new FormUrlEncodedContent(pairs);
                string sEmitResponse = Client.PostAsync("transaction/withdraw", Content).Result.Content.ReadAsStringAsync().Result;
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void GetDeposit()
        {
            string sEmitResponse2 = Client.GetStringAsync("customer/address").Result;
            RollinDeposit tmp = Helpers.json.JsonDeserialize<RollinDeposit>(sEmitResponse2);
            if (tmp.success)
            {
                //Parent.updateDeposit(tmp.address);
            }

        }

        public override void Donate(double Amount)
        {
            SendTip("seuntjie", Amount);
        }

        public override void SendTip(string User, double amount)
        {
            try
            {
                List<KeyValuePair<string, string>> pairs = new List<KeyValuePair<string, string>>();
                pairs.Add(new KeyValuePair<string, string>("username", User));
                pairs.Add(new KeyValuePair<string, string>("private", "0"));
                pairs.Add(new KeyValuePair<string, string>("amount", (amount * 1000).ToString("0.00000", System.Globalization.NumberFormatInfo.InvariantInfo)));
                FormUrlEncodedContent Content = new FormUrlEncodedContent(pairs);
                string sEmitResponse = Client.PostAsync("tipsy/tip", Content).Result.Content.ReadAsStringAsync().Result;
                if (sEmitResponse!="")
                {

                }
                return;
            }
            catch
            {
                return ;
            }
        }

        
        CookieContainer Cookies = new CookieContainer();
        string Token = "";
        public override void Login(string Username, string Password, string twofa)
        {

            try
            {
                this.username = Username;
                HttpWebRequest getHeaders = (HttpWebRequest)HttpWebRequest.Create("https://rollin.io/ref/8c4");
                if (Prox != null)
                    getHeaders.Proxy = Prox;
                var cookies = new CookieContainer();
                getHeaders.CookieContainer = cookies;

                try
                {
                    HttpWebResponse Response = (HttpWebResponse)getHeaders.GetResponse();
                    string s1 = new StreamReader(Response.GetResponseStream()).ReadToEnd();
                    foreach (Cookie C in Response.Cookies)
                    {
                        cookies.Add(C);
                    }
                    s1 = s1.Substring(s1.IndexOf("<input name=\"_token\" type=\"hidden\""));
                    s1 = s1.Substring("<input name=\"_token\" type=\"hidden\" value=\"".Length);
                    Token = s1.Substring(0, s1.IndexOf("\""));
                }
                catch
                {
                    finishedlogin(false);
                    return;
                }


                HttpWebRequest betrequest = (HttpWebRequest)HttpWebRequest.Create("https://rollin.io/api/customer/login");
                if (Prox != null)
                    betrequest.Proxy = Prox;
                betrequest.CookieContainer = cookies;

                betrequest.Method = "POST";

                string post = string.Format("username={0}&password={1}&code={2}", Username, Password, twofa);
                betrequest.ContentLength = post.Length;
                betrequest.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
                betrequest.Headers.Add("X-CSRF-Token", Token);
                using (var writer = new StreamWriter(betrequest.GetRequestStream()))
                {

                    writer.Write(post);
                }
                HttpWebResponse EmitResponse = (HttpWebResponse)betrequest.GetResponse();
                string sEmitResponse = new StreamReader(EmitResponse.GetResponseStream()).ReadToEnd();
                if (!sEmitResponse.ToLower().Contains("true"))
                {
                    finishedlogin(false);
                    return;
                }
                this.Cookies = cookies;
                HttpWebRequest betrequest2 = (HttpWebRequest)HttpWebRequest.Create("https://rollin.io/api/customer/info?username=" + username);
                if (Prox != null)
                    betrequest2.Proxy = Prox;
                betrequest2.CookieContainer = cookies;
                betrequest2.Headers.Add("X-CSRF-Token", Token);
                HttpWebResponse EmitResponse2 = (HttpWebResponse)betrequest2.GetResponse();
                string sEmitResponse2 = new StreamReader(EmitResponse2.GetResponseStream()).ReadToEnd();
                //RollinLoginStats tmpStats = json.JsonDeserialize<RollinLoginStats>(sEmitResponse2);

                //https://rollin.io/api/customer/sync
                betrequest2 = (HttpWebRequest)HttpWebRequest.Create("https://rollin.io/api/customer/sync");
                if (Prox != null)
                    betrequest2.Proxy = Prox;
                betrequest2.CookieContainer = cookies;
                betrequest2.Headers.Add("X-CSRF-Token", Token);
                EmitResponse2 = (HttpWebResponse)betrequest2.GetResponse();
                sEmitResponse2 = new StreamReader(EmitResponse2.GetResponseStream()).ReadToEnd();
                //RollinBet tmpStats2 = json.JsonDeserialize<RollinBet>(sEmitResponse2);

                /*if (tmpStats.success && tmpStats2.success)
                {
                    ClientHandlr = new HttpClientHandler { UseCookies = true, AutomaticDecompression= DecompressionMethods.Deflate| DecompressionMethods.GZip };;
                    Client = new HttpClient(ClientHandlr) { BaseAddress = new Uri("https://rollin.io/api/") };
                    Client.DefaultRequestHeaders.AcceptEncoding.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("gzip"));
                    Client.DefaultRequestHeaders.AcceptEncoding.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("deflate"));
                    ClientHandlr.CookieContainer = this.Cookies;
                    Client.DefaultRequestHeaders.Add("X-CSRF-Token", Token);
                    
                    GetDeposit();
                    balance = double.Parse(tmpStats2.customer.balance, System.Globalization.NumberFormatInfo.InvariantInfo) / 1000.0; //i assume
                    bets = tmpStats.user.bets;
                    profit = double.Parse(tmpStats.user.profit, System.Globalization.NumberFormatInfo.InvariantInfo) / 1000.0;

                    //Parent.updateBalance((decimal)(balance));
                    //Parent.updateBets(tmpStats.user.bets);
                    //Parent.updateLosses(tmpStats.user.losses);
                    //Parent.updateProfit(double.Parse(tmpStats.user.profit, System.Globalization.NumberFormatInfo.InvariantInfo) / 1000.0);
                    //Parent.updateWagered(double.Parse(tmpStats.user.wagered, System.Globalization.NumberFormatInfo.InvariantInfo) / 1000.0);
                    //Parent.updateWins(tmpStats.user.wins);

                    finishedlogin(true);
                    return;
                }*/
            }
            catch
            {

            }
            finishedlogin(false);
        }

        public override bool Register(string username, string password)
        {
            this.username = username;
            HttpWebRequest getHeaders = (HttpWebRequest)HttpWebRequest.Create("https://rollin.io/ref/8c4");
            if (Prox != null)
                getHeaders.Proxy = Prox;
            var cookies = new CookieContainer();
            getHeaders.CookieContainer = cookies;

            try
            {
                HttpWebResponse Response = (HttpWebResponse)getHeaders.GetResponse();
                string s1 = new StreamReader(Response.GetResponseStream()).ReadToEnd();
                foreach (Cookie C in Response.Cookies)
                {
                    cookies.Add(C);
                }
                s1 = s1.Substring(s1.IndexOf("<input name=\"_token\" type=\"hidden\""));
                s1 = s1.Substring("<input name=\"_token\" type=\"hidden\" value=\"".Length);
                Token = s1.Substring(0, s1.IndexOf("\""));
            }
            catch
            {
                finishedlogin(false);
                return false;
            }
            HttpWebRequest betrequest = (HttpWebRequest)HttpWebRequest.Create("https://rollin.io/api/customer/settings/username");
            if (Prox != null)
                betrequest.Proxy = Prox;
            betrequest.CookieContainer = cookies;
            betrequest.Method = "POST";
            string post = string.Format("username={0}", username);
            betrequest.ContentLength = post.Length;
            betrequest.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
            betrequest.Headers.Add("X-CSRF-Token", Token);
            using (var writer = new StreamWriter(betrequest.GetRequestStream()))
            {

                writer.Write(post);
            }
            HttpWebResponse EmitResponse = (HttpWebResponse)betrequest.GetResponse();
            string sEmitResponse = new StreamReader(EmitResponse.GetResponseStream()).ReadToEnd();
            betrequest = (HttpWebRequest)HttpWebRequest.Create("https://rollin.io/api/customer/settings/password");
            if (Prox != null)
                betrequest.Proxy = Prox;
            betrequest.CookieContainer = cookies;
            betrequest.Method = "POST";
            post = string.Format("old=&new={0}&confirm={0}", password);
            betrequest.ContentLength = post.Length;
            betrequest.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
            betrequest.Headers.Add("X-CSRF-Token", Token);
            using (var writer = new StreamWriter(betrequest.GetRequestStream()))
            {
                writer.Write(post);
            }
            EmitResponse = (HttpWebResponse)betrequest.GetResponse();
            sEmitResponse = new StreamReader(EmitResponse.GetResponseStream()).ReadToEnd();

            HttpWebRequest betrequest2 = (HttpWebRequest)HttpWebRequest.Create("https://rollin.io/api/customer/info?username=" + username);
            if (Prox != null)
                betrequest2.Proxy = Prox;
            betrequest2.CookieContainer = cookies;
            betrequest2.Headers.Add("X-CSRF-Token", Token);
            HttpWebResponse EmitResponse2 = (HttpWebResponse)betrequest2.GetResponse();
            string sEmitResponse2 = new StreamReader(EmitResponse2.GetResponseStream()).ReadToEnd();
            RollinLoginStats tmpStats = Helpers.json.JsonDeserialize<RollinLoginStats>(sEmitResponse2);

            //https://rollin.io/api/customer/sync
            betrequest2 = (HttpWebRequest)HttpWebRequest.Create("https://rollin.io/api/customer/sync");
            if (Prox != null)
                betrequest2.Proxy = Prox;
            betrequest2.CookieContainer = cookies;
            betrequest2.Headers.Add("X-CSRF-Token", Token);
            EmitResponse2 = (HttpWebResponse)betrequest2.GetResponse();
            sEmitResponse2 = new StreamReader(EmitResponse2.GetResponseStream()).ReadToEnd();
            RollinBet tmpStats2 = Helpers.json.JsonDeserialize<RollinBet>(sEmitResponse2);

            if (tmpStats.success && tmpStats2.success)
            {
                ClientHandlr = new HttpClientHandler { UseCookies = true, AutomaticDecompression= DecompressionMethods.Deflate| DecompressionMethods.GZip };;
                Client = new HttpClient(ClientHandlr) { BaseAddress = new Uri("https://rollin.io/api/") };
                Client.DefaultRequestHeaders.AcceptEncoding.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("gzip"));
                Client.DefaultRequestHeaders.AcceptEncoding.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("deflate"));
                ClientHandlr.CookieContainer = this.Cookies;
                Client.DefaultRequestHeaders.Add("X-CSRF-Token", Token);

                GetDeposit();
                balance = double.Parse(tmpStats2.customer.balance, System.Globalization.NumberFormatInfo.InvariantInfo) / 1000.0; //i assume
                bets = tmpStats.user.bets;
                profit = double.Parse(tmpStats.user.profit, System.Globalization.NumberFormatInfo.InvariantInfo) / 1000.0;

                //Parent.updateBalance((decimal)(balance));
                //Parent.updateBets(tmpStats.user.bets);
                //Parent.updateLosses(tmpStats.user.losses);
                //Parent.updateProfit(double.Parse(tmpStats.user.profit, System.Globalization.NumberFormatInfo.InvariantInfo) / 1000.0);
                //Parent.updateWagered(double.Parse(tmpStats.user.wagered, System.Globalization.NumberFormatInfo.InvariantInfo) / 1000.0);
                //Parent.updateWins(tmpStats.user.wins);

                finishedlogin(true);
                return true;
            }
            finishedlogin(false);
            return false;
        }

       
        public override bool ReadyToBet()
        {
            //return true;
            if (amount == 0)
                return (DateTime.Now - lastbet).TotalSeconds >= 5;
            else if (amount < 0.00000010)
                return (DateTime.Now - lastbet).TotalSeconds >= 3;
            else if (amount < 0.00000100)
                return (DateTime.Now - lastbet).TotalSeconds >= 2;
            else if (amount < 0.00001000)
                return (DateTime.Now - lastbet).TotalSeconds >= 500;
            else
                return (DateTime.Now - lastbet).TotalMilliseconds >= 100;
        }

        bool isRollin = true;
        
        public override void Disconnect()
        {
            isRollin = false;
        }

        public override void GetSeed(long BetID)
        {
            throw new NotImplementedException();
        }

        public override void SendChatMessage(string Message)
        {
            //Parent.updateStatus("Cannot chat at this moment. Sorry!");
        }

        public override double GetLucky(string server, string client, int nonce)
        {

            server = "3fba0c51b98de5b3dce8c8c7df0505a4041b4eb44a983f94d5f8c34a6b86366e";
            client = "0679f25b2c3e3ed93f80be4bf1f7930115279cd7";
            HMACSHA512 betgenerator = new HMACSHA512();
            List<byte> serverb = new List<byte>();

            for (int i = 0; i < server.Length; i++)
            {
                serverb.Add(Convert.ToByte(server[i]));
            }
            
            
            List<byte> buffer = new List<byte>();
            string msg = client;
            foreach (char c in msg)
            {
                buffer.Add(Convert.ToByte(c));
            }
            betgenerator.Key = buffer.ToArray();
            byte[] hash = betgenerator.ComputeHash(serverb.ToArray());

            StringBuilder hex = new StringBuilder(hash.Length * 2);
            foreach (byte b in hash)
                hex.AppendFormat("{0:x2}", b);
            string s = hex.ToString().Substring((hex.ToString().Length / 2) - 4,8);
            UInt32 seed = UInt32.Parse(s, System.Globalization.NumberStyles.HexNumber);
            //DiceBotCore.MT19937 twist = new MT19937();
            //twist.init_genrand(Convert.ToUInt32(seed));
            //MersenneTwister twist = new MersenneTwister(seed);
            int t =0;
            
            
            //    t = twist.Next(99);
                //t = twist.RandomRange(0,100);
            
            return t;
        }

        new public static double sGetLucky(string server, string client, int nonce)
        {
            HMACSHA512 betgenerator = new HMACSHA512();
            List<byte> serverb = new List<byte>();

            for (int i = 0; i < server.Length; i++)
            {
                serverb.Add(Convert.ToByte(server[i]));
            }
            betgenerator.Key = serverb.ToArray();

            List<byte> buffer = new List<byte>();
            string msg = client;
            foreach (char c in msg)
            {
                buffer.Add(Convert.ToByte(c));
            }
            byte[] hash = betgenerator.ComputeHash(buffer.ToArray());

            StringBuilder hex = new StringBuilder(hash.Length * 2);
            foreach (byte b in hash)
                hex.AppendFormat("{0:x2}", b);
            string s = hex.ToString().Substring(hex.ToString().Length / 2 - 4, 8);
            int seed = int.Parse(s, System.Globalization.NumberStyles.HexNumber);
            int t = 0;
            //MersenneTwister twist = new MersenneTwister(Convert.ToUInt32(seed));
            //t = twist.Next(100);
            return t;
        }
    }

    public class RollinBet
    {
        public bool success { get; set; }
        public RollinGame game { get; set; }
        public RollinCustomer customer { get; set; }
        public RollinStats statistics { get; set; }
        public double fee { get; set; }
        public string[] errors { get; set; }
        public Bet ToBet()
        {
            Bet tmp = new Bet
            {
                Amount=double.Parse(game.bet_amount, System.Globalization.CultureInfo.InvariantCulture)/1000.0,
                Date = DateTime.Now,
                ID = statistics.bets,
                
                Roll = (double)game.number,
                High = game.prediction=="bigger",
                Chance = (double)game.odds,
                ServerHash = customer.server_hash
                

            };
            decimal Profit = decimal.Parse(game.profit, System.Globalization.CultureInfo.InvariantCulture) / 1000m;
            if ((tmp.High && tmp.Roll>(99-tmp.Chance)) || (!tmp.High && tmp.Roll < tmp.Chance))
            {
                tmp.Profit = (double)Profit;
            }
            else
            {
                tmp.Profit = -double.Parse(game.bet_amount, System.Globalization.CultureInfo.InvariantCulture) / 1000.0;
            }
            return tmp;
        }


    }

    public class RollinLoginStats
    {
        public bool success { get; set; }
        public string[] errors { get; set; }
        public RollinStats user { get; set; }
    }

    public class RollinGame
    {
        public bool status { get; set; }
        public decimal number { get; set; }
        public decimal odds { get; set; }
        public string multiplier { get; set; }
        public string bet_number { get; set; }
        public string prediction { get; set; }
        public string profit { get; set; }
        public string bet_amount { get; set; }
    }
    public class RollinCustomer
    {
        public string balance { get; set; }
        public string server_hash { get; set; }
    }
    public class RollinStats
    {
        public int level { get; set; }
        public string profit { get; set; }
        public string wagered { get; set; }
        public int bets { get; set; }
        public int wins { get; set; }
        public int losses { get; set; }
        public string date { get; set; }
    }
    public class RollinRandomize
    {
        public bool success { get; set; }
        public string[] errors { get; set; }
        public string client_seed { get; set; }
        public string server_hash { get; set; }
    }
    public class RollinBalance
    {
        public bool success { get; set; }
        public RollinCustomer customer { get; set; }
    }
    public class RollinDeposit
    {
        public bool success { get; set; }
        public string address { get; set; }
        public string[] errors { get; set; }
    }

}