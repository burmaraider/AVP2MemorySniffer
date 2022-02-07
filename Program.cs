using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

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

            float lastSpeed = 0.0f;
            List<float> totalSpeedToAverage = new List<float>();



            //timer setup


            while (true)
            {
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

                if ((int)pm.getBaseAddress == 0)
                {
                    launched = false;
                    continue;
                }

                Console.SetCursorPosition(0, 0);
                ClearCurrentConsoleLine();
                Console.WriteLine("AVP2 Running");

                _process.Refresh(); //get all modules each loop
                try //fuck it, lets go for it
                {
                    //byte loading = pm.ReadByte((IntPtr)pm.GetModuleAddress("d3d.ren") + 0x55E73);
                    GetLevelName(pm);
                    var loading = IsLoading(pm);
                    GetHeadTrophyCount(pm);
                    GetHealth(pm);
                    PositionSpeed(pm, ref lastPosition, ref lastTime);

                }
                catch
                { }

            }
        }

        private static void GetLevelName(ProcessMemory pm)
        {
            var levelName = pm.ReadStringASCII((IntPtr)pm.GetModuleAddress("object.lto") + 0x2FD9B4, 32);

            if (levelName != string.Empty)
            {
                Console.SetCursorPosition(0, 1);
                ClearCurrentConsoleLine();
                Console.WriteLine(String.Format("Level loaded: {0}", levelName));
            }
        }

        private static void GetHealth(ProcessMemory pm)
        {
            var health = pm.TraverseInt32((IntPtr)pm.GetModuleAddress("cshell.dll") + 0x1c5868, new int[] { 0x798 }, -0x10);
            Console.SetCursorPosition(0, 5);
            ClearCurrentConsoleLine();
            Console.WriteLine(String.Format("Health: {0}", health));
        }

        private static void GetHeadTrophyCount(ProcessMemory pm)
        {
            var headCount = pm.TraverseInt32((IntPtr)pm.GetModuleAddress("cshell.dll") + 0x1c5868, new int[] { 0x798 });
            Console.SetCursorPosition(0, 4);
            ClearCurrentConsoleLine();
            Console.WriteLine(String.Format("Head Trophys: {0}", headCount));
        }

        private static byte IsLoading(ProcessMemory pm)
        {
            byte loading = pm.ReadByte((IntPtr)pm.GetModuleAddress("d3d.ren") + 0x5627C);
            Console.SetCursorPosition(0, 3);
            ClearCurrentConsoleLine();
            Console.WriteLine(String.Format("Status: {0}", Enum.GetName(typeof(GameState), loading)));
            return loading;
        }

        private static void PositionSpeed(ProcessMemory pm, ref Vector3 lastPosition, ref float lastTime)
        {
            float x = (float)pm.TraverseFloat((IntPtr)pm.getBaseAddress + 0xE4994, new int[] { 0x14, 0x414, 0x7d4 });
            float y = (float)pm.TraverseFloat((IntPtr)pm.getBaseAddress + 0xE4994, new int[] { 0x14, 0x414, 0x7d4 }, 4);
            float z = (float)pm.TraverseFloat((IntPtr)pm.getBaseAddress + 0xE4994, new int[] { 0x14, 0x414, 0x7d4 }, 8);

            Vector3 newPos = new Vector3(x, y, z);
            ConsoleWriteColor(String.Format("Position [{0:0.00}], [{1:0.00}], [{2:0.00}]", newPos.X, newPos.Y, newPos.Z), 0, 6, ConsoleColor.Yellow);


            //var currentTime = (float)pm.ReadFloat((IntPtr)pm.getBaseAddress + 0xE49A0);
            var currentTime = (float)pm.ReadFloat((IntPtr)pm.GetModuleAddress("d3d.ren") + 0x53278);

            float deltaTime = currentTime - lastTime;
            Vector3 deltaPos = newPos - lastPosition;

            ConsoleWriteColor(String.Format("delta [{0:0.00}]", deltaTime), 0, 7, ConsoleColor.Yellow);

            if (deltaTime <= 0.5f)
            {
                Vector3 velocity = deltaPos / deltaTime;
                float sum = velocity.X * velocity.X + 0 * 0 + velocity.Z * velocity.Z;//only care about horizontal speed

                ConsoleWriteColor(String.Format("Speed [{0:0.00}]", Math.Sqrt(sum) * 15), 0, 8, ConsoleColor.Yellow);

            }

            lastTime = currentTime;
            lastPosition = newPos;
        }

        public static void ClearCurrentConsoleLine()
        {
            int currentLineCursor = Console.CursorTop;
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write(new string(' ', Console.WindowWidth));
            Console.SetCursorPosition(0, currentLineCursor);
        }

        public static void ConsoleWriteColor(string inputString, int left, int right, ConsoleColor fgColor = ConsoleColor.White, ConsoleColor bgColor = ConsoleColor.Black)
        {
            var pieces = Regex.Split(inputString, @"(\[[^\]]*\])");

            Console.SetCursorPosition(left, right);
            ClearCurrentConsoleLine();

            for (int i = 0; i < pieces.Length; i++)
            {
                string piece = pieces[i];

                if (piece.StartsWith("[") && piece.EndsWith("]"))
                {
                    Console.ForegroundColor = fgColor;
                    piece = piece.Substring(1, piece.Length - 2);
                }

                Console.Write(piece);
                Console.ResetColor();
            }


        }
    }
}
