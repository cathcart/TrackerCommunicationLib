using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
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
        public async Task GetPeersFromTracker()
        {
            string filepath = "/Users/cathcart/Downloads/debian-live-11.1.0-amd64-cinnamon.iso.torrent";
            var tmp = new TrackerCommunication(filepath);

            var peers = await tmp.GetPeers();
        }

        [TestMethod]
        public async Task NoPeersReturnedFromShitTracker()
        {
            string filepath = "/Users/cathcart/Downloads/The.Wheel.of.Time.S01E05.Blood.Calls.Blood.2160p.AMZN.WEB-DL.x265.10bit.HDR10Plus.DDP5.1.Atmos-MZABI[rartv]-[rarbg.to].torrent";
            var tmp = new TrackerCommunication(filepath);

            var peers = await tmp.GetPeers();

            Assert.AreEqual(peers.Count<IPeer>(), 0);

        }

        

        [TestMethod]
        public async Task InvalidTrackerProtocall()
        {
            string filepath = "/Users/cathcart/Downloads/60CAC4AB2E2B1EFC6A4742BB740B21761E36D7C0.torrent";
            var tmp = new TrackerCommunication(filepath);

            Task<IEnumerable<IPeer>> GetPeersTask = tmp.GetPeers();

            //var peers = await GetPeersTask;

            //Assert.ThrowsException<IOException>(() => GetPeersTask);
            Assert.ThrowsExceptionAsync<IOException>(() => GetPeersTask);

        }


        //[TestMethod]
        //public async Task AsyncTest()
        //{
        //    Task<bool> longRunningTask = ReturnBoolAsync(10000);
        //    // independent work which doesn't need the result of LongRunningOperationAsync can be done here
        //    Console.WriteLine("waiting");
        //    //and now we call await on the task 
        //    bool result = await longRunningTask;

        //    var hostname = Dns.GetHostName();
        //    var asyncName = Dns.GetHostEntryAsync(Dns.GetHostName()).GetAwaiter().GetResult().AddressList[0];

        //    var client1 = new HttpClient();
        //    var uriString = "http://www.google.com";
        //    //Task<HttpResponseMessage> httpWait1 = client1.GetAsync(uriString);

        //    var client2 = new HttpClient();
        //    //uriString = "https://nope.porn/";

        //    IPAddress[] addresslist = Dns.GetHostAddresses(uriString);

        //    //Task<HttpResponseMessage> httpWait2 = client2.GetAsync(uriString);

        //    //var code = await httpWait1;
        //    //var code2 = await httpWait2;

        //    //use the result 
        //    Console.WriteLine(result);
        //}

        //private async Task<bool> ReturnBoolAsync(int waitTime)
        //{
        //    await Task.Delay(waitTime); // 1 second delay
        //    return true;
        //}

        //[TestMethod]
        //public async Task IP()
        //{
        //    var client2 = new System.Net.Http.HttpClient();
        //    var uriString = "http://www.google.com";
        //    uriString = "https://www.nope.porn/";

        //    Task<HttpResponseMessage> httpWait2 = client2.GetAsync(uriString);
        //    var code2 = await httpWait2;

        //    Console.WriteLine(code2.StatusCode);
        //    Console.WriteLine(code2.Headers);
        //}
    }
}
