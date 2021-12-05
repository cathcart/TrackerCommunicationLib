using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
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

            return bytes;
        }

        public IEnumerable<IPeer> GetPeers()
        {
            WebClient webClient = new WebClient();

            webClient.QueryString.Add("info_hash", HttpUtility.UrlEncode(_MetaInfoHash));
            webClient.QueryString.Add("peer_id", HttpUtility.UrlEncode(_peer_id));
            string result = webClient.DownloadString(_announceUrl);

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
