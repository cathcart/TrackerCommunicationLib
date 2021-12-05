using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using BencodeLibRedo.Interfaces;
using BitTrackerLib;
using BitTrackerLib.Interfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TestProject
{
    public static class TrackerCommunicationTestingExtension
    {
        public static IEnumerable<IPeer> GetPeersTest(this TrackerCommunication trackerCommunication, Dictionary<string,string> testTrackerResponse)
        {

            var encoder = new BencodeEncoder();
            var parser = new BencodeParser();

            var bencodedResponse = encoder.Encode(testTrackerResponse);


            var ttmp = parser.Parse(bencodedResponse);
            var tmp2 = ttmp.Export();

            //Dictionary<string, IBencodeItem> tmp = (Dictionary<string, IBencodeItem>)parser.Parse(bencodedResponse);

            return trackerCommunication.ParseTrackerResponse(tmp2);
        }

        public static IEnumerable<IPeer> GetPeersTest(this TrackerCommunication trackerCommunication, Dictionary<string, IBencodeItem> testTrackerResponse)
        {
            return trackerCommunication.ParseTrackerResponse(testTrackerResponse);
        }
    }

    [TestClass]
    public class MetaDataRun
    {
        [TestMethod]
        public void AssertTrackerFailed()
        {
            var failedResponse = new Dictionary<string,string> { { "failure reason", "faked response" } };

            var tmp = new TrackerCommunication();

            Assert.ThrowsException<Exception>(() => tmp.GetPeersTest(failedResponse));
        }

        [TestMethod]
        public void OnePeerResponse()
        {
            var encoder = new BencodeEncoder();
            var parser = new BencodeParser();

            var peer = new Dictionary<string, string> { { "ip", "192.168.1.1"}, {"port", "42"} };

            string peerBenCode = encoder.Encode(peer);

            var testResponse = new Dictionary<string, string> { { "8:interval", "i666e" }, { "5:peers", peerBenCode } };

            var testResponseStr = string.Format("d8:intervali666e5:peersl{0}ee", peerBenCode);

            Dictionary<string, IBencodeItem> tmp2 = parser.Parse(testResponseStr).Export();

            var tmp = new TrackerCommunication();

            IEnumerable<IPeer> peerList = tmp.GetPeersTest(tmp2);

            Assert.AreEqual(1, peerList.Count());
        }

        [TestMethod]
        public void TwoPeerResponse()
        {
            var encoder = new BencodeEncoder();
            var parser = new BencodeParser();

            var peer1 = new Dictionary<string, string> { { "ip", "192.168.1.1" }, { "port", "42" } };
            var peer2 = new Dictionary<string, string> { { "ip", "192.168.1.2" }, { "port", "42" } };

            string peer1BenCode = encoder.Encode(peer1);
            string peer2BenCode = encoder.Encode(peer2);

            //List<string> peers = new List<string> { peer1BenCode, peer2BenCode };
            //string peerBenCode = encoder.Encode(peers);

            //Dictionary<string, string> testResponse = new Dictionary<string, string> { { "8:interval", "i666e" }, { "5:peers", peerBenCode } };

            var testResponseStr = string.Format("d8:intervali666e5:peersl{0}{1}ee", peer1BenCode, peer2BenCode);

            Dictionary<string, IBencodeItem> tmp2 = parser.Parse(testResponseStr).Export();

            var tmp = new TrackerCommunication();

            IEnumerable<IPeer> peerList = tmp.GetPeersTest(tmp2);

            Assert.AreEqual(2, peerList.Count());
        }

        [TestMethod]
        public void GetPeersFromTracker()
        {
            string filepath = "/Users/cathcart/Downloads/debian-live-11.1.0-amd64-cinnamon.iso.torrent";
            var tmp = new TrackerCommunication(filepath);

            var peers = tmp.GetPeers();
        }
    }
}
