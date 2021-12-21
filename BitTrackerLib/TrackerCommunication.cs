using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using BencodeLibRedo.Interfaces;
using BitTrackerLib.Classes;
using BitTrackerLib.Interfaces;

namespace BitTrackerLib
{
    public class TrackerCommunication
    {
        private string _announceUrl;
        private byte[] _MetaInfoHash;
        private BencodeParser _parser = new BencodeParser();
        private byte[] _peer_id;
        private DateTime _nextTrackerCommunication = DateTime.Now;
        private bool _isHTTPURL;
        private int _timeOut = 2*1000;

        public TrackerCommunication() { }

        public TrackerCommunication(string filepath)
        {
            var bytes = ParseMetaInfo(filepath);

            using (SHA1 sha = SHA1.Create())
            {
                _MetaInfoHash = sha.ComputeHash(bytes);

                DateTime now = DateTime.Now;

                string hostName = Dns.GetHostName(); // Retrive the Name of HOST
                //string myIP = Dns.GetHostByName(hostName).AddressList[0].ToString(); // Get the IP
                string myIP = Dns.GetHostEntry(hostName).AddressList[0].ToString();

                var peerSeed = Encoding.UTF8.GetBytes(string.Format("{0}{1}", now.ToString(), myIP));
                _peer_id = sha.ComputeHash(peerSeed);
            }
        }

        private byte[] ParseMetaInfo(string filepath)
        {
            FileStream fs = File.OpenRead(filepath);

            var item = _parser.Parse(fs);

            Dictionary<string, IBencodeItem> metaInfo = item.Export();

            IBencodeItem info = metaInfo["info"];

            int len = info.StopPos - info.StartPos;
            var bytes = new byte[len];

            fs.Position = info.StartPos;
            fs.Read(bytes, 0, len);

            fs.Close();

            _announceUrl = metaInfo["announce"].Export();

            var protocall =_announceUrl.Split(':')[0];

            _isHTTPURL = protocall == "http";

            return bytes;
        }

        //// HttpClient is intended to be instantiated once per application, rather than per-use. See Remarks.
        //static readonly HttpClient client = new HttpClient();

        //static async Task TestHttp(string uri)
        //{
        //    // Call asynchronous network methods in a try/catch block to handle exceptions.
        //    try
        //    {
        //        HttpResponseMessage response = await client.GetAsync(uri);
        //        response.EnsureSuccessStatusCode();
        //        string responseBody = await response.Content.ReadAsStringAsync();
        //        // Above three lines can be replaced with new helper method below
        //        // string responseBody = await client.GetStringAsync(uri);

        //        Console.WriteLine(responseBody);
        //    }
        //    catch (HttpRequestException e)
        //    {
        //        Console.WriteLine("\nException Caught!");
        //        Console.WriteLine("Message :{0} ", e.Message);
        //    }
        //}

      
        public async Task<IEnumerable<IPeer>> GetPeers()
        {
            

            if(_isHTTPURL == false)
            {
                throw new IOException(string.Format("Tracker url invalid, not http\n{0}", _announceUrl));
            }

            HttpClient client = new HttpClient();
            CancellationTokenSource s_cts = new CancellationTokenSource();
            CancellationToken cts = s_cts.Token;
            //client.DefaultRequestHeaders.Add("User-Agent", "C# App");

            var one = HttpUtility.UrlEncode(_MetaInfoHash);
            var two = HttpUtility.UrlEncode(Encoding.UTF8.GetString(_MetaInfoHash));

            var uri = new Uri(string.Format("{0}?info_hash={1}&peer_id={2}", _announceUrl, HttpUtility.UrlEncode(_MetaInfoHash), HttpUtility.UrlEncode(_peer_id)));
            var result = "";
            //TestHttp(uri);

            s_cts.CancelAfter(_timeOut);
            Task<HttpResponseMessage> httpResponseTask = client.GetAsync(uri.OriginalString, cts);
            Console.WriteLine("waiting for mail");

            try
            {
                
                HttpResponseMessage response = await httpResponseTask.ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                result = await response.Content.ReadAsStringAsync();
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("\nTasks cancelled: timed out.\n");
                return new List<IPeer>();
            }
            catch (Exception e)
            {
                Console.WriteLine("GET request to http timed out.", e);
                return new List<IPeer>();
            }
            finally
            {
                s_cts.Dispose();
            }


            if (result == "")
            {
                return new List<IPeer>();
            }

            _parser = new BencodeParser();

            var item = _parser.Parse(result);

            Dictionary<string, IBencodeItem> trackerResponse = item.Export();

            return ParseTrackerResponse(trackerResponse);
        }

        public IEnumerable<IPeer> ParseTrackerResponse(Dictionary<string, IBencodeItem> trackerResponse)
        {
            if (trackerResponse.ContainsKey("failure reason"))
            {
                string issue = trackerResponse["failure reason"].Export();

                throw new Exception(issue);
            }

            var interval = trackerResponse["interval"].Export();

            _nextTrackerCommunication = DateTime.Now.AddSeconds(interval);

            var peers = trackerResponse["peers"].Export();

            var returnList = new List<IPeer>();
            foreach (IBencodeItem peer in peers)
            {
                //don't think we have to care about compact/non-compact representations here
                string ip = peer.Export()["ip"].Export();
                int port = peer.Export()["port"].Export();

                var r_peer = new Peer(ip, port);
                returnList.Add(r_peer);
            }
            return returnList;
        }
    }
}
