using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;


namespace Chip8_Dx{
    class Memory {

        //0x050-0x0A0 - Used for the built in 4x5 pixel font set(0-F)
        //0x000-0x1FF - Chip 8 interpreter(contains font set in emu)
        //0x200-0xFFF - Program ROM and work RAM

        public static UInt16[] memory = new UInt16[4096];//4k mem
        public const int progMemStart = 0x200;// Decimal == 512
        public static FileStream stream;
        public Memory() { }

        public static int LoadProgram(String file) {//load program into memory
            if (File.Exists(file)) {
                stream = File.Open(file, FileMode.Open);
                byte[] data = new byte[stream.Length];
                stream.Read(data, 0, data.Length);

                for (int i = 0; i < data.Length; i++) {
                    Memory.memory[i + progMemStart] = data[i];
                }
            }
            return 0;
        }
        public static void terminateProgram() { // Load fontset into memory
            stream.Close();
        }

        public static void LoadFontset() { // Load fontset into memory
            for (int i = 0; i < 80; ++i)
                Memory.memory[i+80] = CPU.fontset[i];
        }

        public static void ClearMemory() { //Zero out Mem
            for (int i = 0; i < 4096; ++i)
                memory[i] = 0;
        }


        public static void DisplayMemory(int begin, int end) {
            for (int i = begin; i < end; ++i)
                Console.WriteLine("Mem[" + i + "] = " + Convert.ToByte(Memory.memory[i]).ToString("X"));
        }
    }
}
