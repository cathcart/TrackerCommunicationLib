using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using BencodeLibRedo.Interfaces;
using BencodeLibRedo.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TestProject
{
    [TestClass]
    public class MetaDataRun
    {
        

        [TestMethod]
        public void TestFileRead()
        {
            var parser = new BencodeParser();


            string filepath = "/Users/cathcart/Downloads/debian-10.0.0-amd64-DVD-1.iso.torrent";
            filepath = "/Users/cathcart/Downloads/debian-live-11.1.0-amd64-cinnamon.iso.torrent";
            //var file = new FileStream(filepath);

            FileStream fs = File.OpenRead(filepath);

            var item = parser.Parse(fs);
            //ßßvar output = item.Export();
            //var output = 32;

            Dictionary<string, IBencodeItem> tmp = item.Export();

            var ann = tmp["announce"].Export();



            IBencodeItem info = tmp["info"];

            int len = info.StopPos - info.StartPos;
            var bytes = new byte[len];

            fs.Position = info.StartPos;
            fs.Read(bytes, 0, len);


            var infotext = Encoding.UTF8.GetString(bytes);

            var sha = SHA1.Create();

            var hash = sha.ComputeHash(bytes);

            var text = HttpUtility.UrlEncode(hash);


            Assert.AreEqual<int>(1, 1);

            
        }

    }
}
