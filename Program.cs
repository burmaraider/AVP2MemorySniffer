using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AVP2MemorySniffer
{
    class Program
    {

        public enum GameState
        {
            InGame = 0x88,
            PauseMenu = 0xA0,
            MainMenu = 0xC8,
            Loading = 0x98,
            GameNotLoaded = 0x0
        };
        
        static void Main(string[] args)
        {
            bool launched = false;
            Process _process = null;
            ProcessMemory pm = new ProcessMemory();

            Vector3 lastPosition = Vector3.Zero;
            float lastTime = 0.0f;
            while (true)
            {
                //Console.Clear();
                if (!launched)
                {
                    Console.Clear();
                    Console.SetCursorPosition(0, 0);
                    Console.WriteLine("Waiting for AVP2 to be launched");
                    var tempProcesses = Process.GetProcessesByName("lithtech");
                    if (tempProcesses.Length < 1) continue;
                    _process = tempProcesses[0];
                    pm = new ProcessMemory(_process, (IntPtr)_process.Id);
                    launched = true;
                    continue;
                }

                if((int)pm.getBaseAddress == 0)
                {
                    launched = false;
                    continue;
                }

                Console.SetCursorPosition(0, 0);
                ClearCurrentConsoleLine();
                Console.WriteLine("AVP2 Running");


                bool objectlto = false;
                bool d3dren = false;
                _process.Refresh();
                try
                {
                    var levelName = pm.ReadStringASCII((IntPtr)pm.GetModuleAddress("object.lto") + 0x2FD9B4, 32);

                    if (levelName != string.Empty)
                    {
                        Console.SetCursorPosition(0, 1);
                        ClearCurrentConsoleLine();
                        Console.WriteLine(String.Format("Level loaded: {0}", levelName));
                    }

                    byte loading = pm.ReadByte((IntPtr)pm.GetModuleAddress("d3d.ren") + 0x55E73);

                    var status = loading > 0 ? "false" : "true";
                    Console.SetCursorPosition(0, 2);
                    ClearCurrentConsoleLine();
                    Console.WriteLine(String.Format("Loading: {0}", status));


                    loading = pm.ReadByte((IntPtr)pm.GetModuleAddress("d3d.ren") + 0x5627C);
                    Console.SetCursorPosition(0, 3);
                    ClearCurrentConsoleLine();
                    Console.WriteLine(String.Format("Status: {0}", Enum.GetName(typeof(GameState), loading)));

                    var headCount = pm.TraverseInt32((IntPtr)pm.GetModuleAddress("cshell.dll") + 0x1c5868, new int[] { 0x798 });
                    Console.SetCursorPosition(0, 4);
                    ClearCurrentConsoleLine();
                    Console.WriteLine(String.Format("Head Trophys: {0}", headCount));

                    var health = pm.TraverseInt32((IntPtr)pm.GetModuleAddress("cshell.dll") + 0x1c5868, new int[] { 0x798 }, -0x10);
                    Console.SetCursorPosition(0, 5);
                    ClearCurrentConsoleLine();
                    Console.WriteLine(String.Format("Health: {0}", health));


                    float x = (float)pm.TraverseFloat((IntPtr)pm.getBaseAddress + 0xE4994, new int[] { 0x14, 0x414, 0x7d4 });
                    float y = (float)pm.TraverseFloat((IntPtr)pm.getBaseAddress + 0xE4994, new int[] { 0x14, 0x414, 0x7d4 }, 4);
                    float z = (float)pm.TraverseFloat((IntPtr)pm.getBaseAddress + 0xE4994, new int[] { 0x14, 0x414, 0x7d4 }, 8);
                    // lithtech.exe + E49A0

                    Vector3 newPos = new Vector3(x, y, z);
                    Console.SetCursorPosition(0, 6);
                    ClearCurrentConsoleLine();
                    Console.WriteLine(String.Format("Position {0:0.00}, {1:0.00}, {2:0.00}", newPos.X, newPos.Y, newPos.Z));


                    var currentTime = (float)pm.ReadFloat((IntPtr)pm.getBaseAddress + 0xE49A0);

                    float deltaTime = currentTime - lastTime;
                    Vector3 deltaPos = newPos - lastPosition;

                    Console.SetCursorPosition(0, 8);
                    ClearCurrentConsoleLine();
                    Console.WriteLine(String.Format("delta {0:0.00}", deltaTime));

                    if (deltaTime < 1.4f)
                    {
                        Vector3 velocity = deltaPos / deltaTime;
                        float sum = velocity.X * velocity.X + velocity.Y * velocity.Y + velocity.Z * velocity.Z;
                        Console.SetCursorPosition(0, 7);
                        ClearCurrentConsoleLine();
                        
                        Console.WriteLine(String.Format("Speed {0:0.00}", Math.Sqrt(sum) * 40));
                        
                    }

                    lastTime = currentTime;
                    lastPosition = newPos;

                }
                catch
                { }

            }
        }
        public static void ClearCurrentConsoleLine()
        {
            int currentLineCursor = Console.CursorTop;
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write(new string(' ', Console.WindowWidth));
            Console.SetCursorPosition(0, currentLineCursor);
        }
       
    }
}
