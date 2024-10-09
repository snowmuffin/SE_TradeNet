using System;
using System.IO;
using System.Text;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Weapons;
using System.Collections.Generic;
using System.Linq;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Game;
using VRage.Utils;

namespace SE_TradeNet
{

    public class Config
    {

        public static MyConfig Instance;

        public static void Load()
        {
            // Load config xml
            if (MyAPIGateway.Utilities.FileExistsInWorldStorage("SE_TradeNetConfig.xml", typeof(MyConfig)))
            {
                try
                {
                    TextReader reader = MyAPIGateway.Utilities.ReadFileInWorldStorage("SE_TradeNetConfig.xml", typeof(MyConfig));
                    var xmlData = reader.ReadToEnd();
                    Instance = MyAPIGateway.Utilities.SerializeFromXML<MyConfig>(xmlData);
                    reader.Dispose();
                    MyLog.Default.WriteLine("SE_TradeNet: found and loaded");
                }
                catch (Exception e)
                {
                    MyLog.Default.WriteLine("SE_TradeNet: loading failed, generating new Config");
                }
            }

            if (Instance == null)
            {
                MyLog.Default.WriteLine("SE_TradeNet: No Loot Config found, creating New");
                // Create default values
                Instance = new MyConfig()
                {
                    webadd = ""
                };
            }


      
            Write();
        }


        public static void Write()
        {
            if (Instance == null) return;

            try
            {
                MyLog.Default.WriteLine("SE_TradeNet: Serializing to XML... ");
                string xml = MyAPIGateway.Utilities.SerializeToXML<MyConfig>(Instance);
                MyLog.Default.WriteLine("SE_TradeNet: Writing to disk... ");
                TextWriter writer = MyAPIGateway.Utilities.WriteFileInWorldStorage("SE_TradeNetConfig.xml", typeof(MyConfig));
                writer.Write(xml);
                writer.Flush();
                writer.Close();
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine("SE_TradeNet: Error saving XML!" + e.StackTrace);
            }
        }
    }
}