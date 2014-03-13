using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.IO.MemoryMappedFiles;
using System.Threading;
using System.Net;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Gw2Mapping
{
    public partial class Main : Form
    {
        public Main()
        {
            InitializeComponent();
        }

        private void Main_Load(object sender, EventArgs e)
        {
            backgroundWorker1.RunWorkerAsync();
        }
        public unsafe struct LinkedMem
        {
            public uint uiVersion;
            public uint uiTick;
            public fixed float fAvatarPosition[3];
            public fixed float fAvatarFront[3];
            public fixed float fAvatarTop[3];
            public fixed byte name[512];
            public fixed float fCameraPosition[3];
            public fixed float fCameraFront[3];
            public fixed float fCameraTop[3];
            public fixed byte identity[512];
            public uint context_len;
            public fixed byte context[512];
            public fixed byte description[4096];
        };

        public class PlayerInfo
        {
            public float x, y, z = 0;
            public int map = 0;
            public string identity = "";
            public double camRotationX = 0;
            public double camRotationY = 0;
            public double playerRotationX = 0;
            public double playerRotationY = 0;
        }

        // Number of inches per meter
        const float InchesPerMeter = 39.37010F;

        //
        static MemoryMappedFile mappedFile;
        static MemoryMappedViewAccessor accessor;
        static LinkedMem data = new LinkedMem();
        static PlayerInfo playerInfo = new PlayerInfo();

        public static void OpenMumbleLink()
        {
            // Open the mapped memory file
            mappedFile = MemoryMappedFile.CreateOrOpen("MumbleLink", Marshal.SizeOf(data));
            accessor = mappedFile.CreateViewAccessor(0, Marshal.SizeOf(data));
        }

        public static void GetData()
        {
            // Make sure the map memory file is loaded
            if (mappedFile == null) OpenMumbleLink();

            // Read mapped memory file
            accessor.Read(0, out data);

            unsafe
            {
                fixed (LinkedMem* _data = &data)
                {
                    // Parse info
                    playerInfo.x = (float)(_data->fAvatarPosition[0]) * InchesPerMeter;
                    playerInfo.y = (float)(_data->fAvatarPosition[1]) * InchesPerMeter;
                    playerInfo.z = (float)(_data->fAvatarPosition[2]) * InchesPerMeter;
                    playerInfo.camRotationX = (double)(_data->fCameraFront[0]);
                    playerInfo.camRotationY = (double)(_data->fCameraFront[2]);
                    playerInfo.playerRotationX = (double)(_data->fAvatarFront[0]);
                    playerInfo.playerRotationY = (double)(_data->fAvatarFront[2]); 
                    playerInfo.map = (int)_data->context[28] + ((int)_data->context[29] * 256);

                }
            }
        }
   
        void updateMap()
        {
            if (playerInfo.map != GlobalVars.currentMap)
            {
                //Get JSON data from gw2 map API
                string mapID = playerInfo.map.ToString();
                WebClient c = new WebClient();
                var data = c.DownloadString("https://api.guildwars2.com/v1/maps.json?map_id=" + mapID);
                JObject o = JObject.Parse(data);
                dynamic mapData = JsonConvert.DeserializeObject(data);

                //Gets X,Y coordinates for map_rect and continent_rect
                GlobalVars.mLeft = mapData.maps[mapID].map_rect[0][0];
                GlobalVars.mTop = mapData.maps[mapID].map_rect[0][1];
                GlobalVars.mWidth = mapData.maps[mapID].map_rect[1][0] - mapData.maps[mapID].map_rect[0][0];
                GlobalVars.mHeight = mapData.maps[mapID].map_rect[1][1] - mapData.maps[mapID].map_rect[0][1];
                GlobalVars.cLeft = mapData.maps[mapID].continent_rect[0][0];
                GlobalVars.cTop = mapData.maps[mapID].continent_rect[0][1];
                GlobalVars.cWidth = mapData.maps[mapID].continent_rect[1][0] - mapData.maps[mapID].continent_rect[0][0];
                GlobalVars.cHeight = mapData.maps[mapID].continent_rect[1][1] - mapData.maps[mapID].continent_rect[0][1];
                GlobalVars.mapName = mapData.maps[mapID].map_name;
            }

            //Creates the relative percent of the map for player X,Y
            float mapPctZero = ((playerInfo.x) - GlobalVars.mLeft) / GlobalVars.mWidth;
            float mapPctOne = ((playerInfo.z) - GlobalVars.mTop) / GlobalVars.mHeight;

            GlobalVars.playerPosX = (GlobalVars.cLeft + (GlobalVars.cWidth * mapPctZero));
            GlobalVars.playerPosY = ((GlobalVars.cTop + GlobalVars.cHeight) - (GlobalVars.cHeight * mapPctOne));
            //Gets final continent coordinates for player position                   
            xLabel.Text = "X: " + GlobalVars.playerPosX.ToString();
            yLabel.Text = "Y: " + GlobalVars.playerPosY.ToString();

            double camRadians = Math.Atan2(playerInfo.camRotationY, playerInfo.camRotationX);
            double camDegrees = (camRadians * (180 / Math.PI)) + 180 ;
            double playerRadians = Math.Atan2(playerInfo.playerRotationY, playerInfo.playerRotationX);
            double playerDegrees = (playerRadians * (180 / Math.PI)) + 180;

            rotationLabel.Text = "Rotation: " + camDegrees.ToString();
            mapLabel.Text = "Map: " + GlobalVars.mapName;
            

            Coordinate coord = new Coordinate();
            coord.posX = GlobalVars.playerPosX.ToString();
            coord.posY = GlobalVars.playerPosY.ToString();
            coord.camRotation = camDegrees.ToString();
            coord.playerRotation = playerDegrees.ToString();
            coord.mapName = GlobalVars.mapName.ToString();

            GlobalVars.json = JsonConvert.SerializeObject(coord);
            if (GlobalVars.startWeb != 1)
            {
                backgroundWorker2.RunWorkerAsync();
                GlobalVars.startWeb = 1;
            }


        }
        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            updateMap();
        }
        private static class GlobalVars
        {
            public static int currentMap = 0;
            public static int mLeft;
            public static int mTop;
            public static int mWidth;
            public static int mHeight;
            public static int cLeft;
            public static int cTop;
            public static int cWidth;
            public static int cHeight;
            public static float playerPosX;
            public static float playerPosY;
            public static string json;
            public static string mapName = "";
            public static int startWeb = 0;

                 
        }
        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            while (true)
            {
                System.Threading.Thread.Sleep(500);
                GetData();
                backgroundWorker1.ReportProgress(0, "Reporting in");
            }
        }
 
        class Coordinate
        {
            public string posX { get; set; }
            public string posY { get; set; }
            public string camRotation { get; set; }
            public string playerRotation { get; set; }
            public string mapName { get; set; }
        }

        private void backgroundWorker2_DoWork(object sender, DoWorkEventArgs e)
        {
            
                HttpListener server = new HttpListener();
                server.Prefixes.Add("http://localhost:1337/");
                server.Start();
                while (true)
                {
                    HttpListenerContext context = server.GetContext();
                    HttpListenerResponse response = context.Response;
                    response.AppendHeader("Access-Control-Allow-Origin", "*");

                    string page = GlobalVars.json;
                   
                        byte[] buffer = Encoding.UTF8.GetBytes(page);
                        response.ContentLength64 = buffer.Length;
                        Stream st = response.OutputStream;
                        st.Write(buffer, 0, buffer.Length);

                        context.Response.Close();
  
                }
           

        }

        private void button1_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("http://mshelley.net/gw2");
        }
    }
}
