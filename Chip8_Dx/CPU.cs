using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Chip8_Dx {
    class CPU {
        Memory memory = new Memory();
        public static GFX gfx = new GFX();
        public static String[] file = new String[] { @"D:\Projects\Chip8_Dx\pong2.c8", @"D:\Projects\Chip8_Dx\invaders.c8", @"D:\Projects\Chip8_Dx\tetris.c8" };
        //public static String file =  @"D:\Projects\Chip8_Dx\invaders.c8";
        //public static String file = @"D:\Projects\Chip8_Dx\tetris.c8";
        //public static String file =  @"D:\Projects\Chip8_Dx\pong2.c8";
        public static bool drawFlag = true;
        public static UInt16 opcode;//35 opcodes
        public static UInt16[] V = new UInt16[16];//15 reg gen purpose -- V[15] carryflag
        public static UInt16 I;//Index Register
        public static short pc;//Program Counter
        public static char[] gfxx = new char[64 * 32];//2048 pix
        public static UInt16 delay_timer;//spd:60htz
        public static UInt16 sound_timer;
        public static short[] stack = new short[16];
        public static short sp;//stack point
        public static String dbgMsg = "";
        public static String[] opOut = new String []{ "", "", "", "", "", "", "", "", "", ""};

        public static void initializeInterpreter() {
            pc = Memory.progMemStart;     // Program counter starts at 0x200 (Start adress program)
            opcode = 0;     // Reset current opcode	
            I = 0;          // Reset index register
            sp = 0;         // Reset stack pointer


            for (int i = 0; i < 2048; ++i) { GFX.gfxOut[i] = 0; }// Clear display
            for (int i = 0; i < 16; ++i) { stack[i] = 0; }// Clear stack
            //for (int i = 0; i < 16; ++i)
            //key[i] = V[i] = 0;
            Memory.ClearMemory();
            Memory.LoadFontset();
            delay_timer = 0;// Reset timers
            sound_timer = 0;
            drawFlag = true;// Clear screen once
            Memory.LoadProgram(file[1]);
            gfx.RefreshMemDisplay();
            gfx.Run();
        }




        //      0   0   0   0       0   0   0   0
        //     128  64  32  16      8   4   2   1  

        public static byte[] fontset = new byte[80]{ //Use Nibble
                            0xF0, 0x90, 0x90, 0x90, 0xF0, //0
                            0x20, 0x60, 0x20, 0x20, 0x70, //1
                            0xF0, 0x10, 0xF0, 0x80, 0xF0, //2
                            0xF0, 0x10, 0xF0, 0x10, 0xF0, //3
                            0x90, 0x90, 0xF0, 0x10, 0x10, //4
                            0xF0, 0x80, 0xF0, 0x10, 0xF0, //5
                            0xF0, 0x80, 0xF0, 0x90, 0xF0, //6
                            0xF0, 0x10, 0x20, 0x40, 0x40, //7
                            0xF0, 0x90, 0xF0, 0x90, 0xF0, //8
                            0xF0, 0x90, 0xF0, 0x10, 0xF0, //9
                            0xF0, 0x90, 0xF0, 0x90, 0x90, //A
                            0xE0, 0x90, 0xE0, 0x90, 0xE0, //B
                            0xF0, 0x80, 0x80, 0x80, 0xF0, //C
                            0xE0, 0x90, 0x90, 0x90, 0xE0, //D
                            0xF0, 0x80, 0xF0, 0x80, 0xF0, //E
                            0xF0, 0x80, 0xF0, 0x80, 0x80  //F
                        };


        public CPU() {
            
        }



        public static void setKeys() { }

        public static void clearOpOutput() {
            for (int i = 0; i < 10; i++) {
                opOut[i] = "";
            }
        }

        public static void SYS_STATE() {
            Console.WriteLine("___________________________________________________________________________________");
            Console.WriteLine("\nCycle Number = " + emuCycle++);
            Console.WriteLine("OPCODE HEX = 0x" + (Memory.memory[pc] << 8 | Memory.memory[pc + 1]).ToString("X"));
            Console.WriteLine("OPCODE DEC = " + (Memory.memory[pc] << 8 | Memory.memory[pc + 1]));
            Console.WriteLine("OPCODE BIN = " + Convert.ToString(Memory.memory[pc] << 8 | Memory.memory[pc + 1], 2));


            Console.WriteLine("I  = " + I);
            Console.WriteLine("PC  = " + pc);
            Console.WriteLine("\n");
            //Console.ReadKey();// 0xA2F6==1010001011110110==41718
        }






        /// <summary>
        /// Fetch Decode and Execute OPCODES  -->  Update Timers
        /// 
        /// nnn or addr - A 12 - bit value, the lowest 12 bits of the instruction
        /// n or nibble - A 4 - bit value, the lowest 4 bits of the instruction
        /// x - A 4 - bit value, the lower 4 bits of the high byte of the instruction
        /// y - A 4 - bit value, the upper 4 bits of the low byte of the instruction
        /// kk or byte -An 8 - bit value, the lowest 8 bits of the instruction
        ///
        /// 
        ///  A bitwise AND compares the corrseponding bits from two values, and if both bits are 1, then the same bit in the result is also 1. Otherwise, it is 0. 
        ///  A bitwise OR compares the corrseponding bits from two values, and if either bit is 1, then the same bit in the result is also 1. Otherwise, it is 0.
        ///  An exclusive OR compares the corrseponding bits from two values, and if the bits are not both the same, then the corresponding bit in the result is set to 1. Otherwise, it is 0.
        /// 
        /// 
        /// 
        /// Opcode == wwwwxxxx yyyyzzzz
        /// 0xF000 == 11110000 00000000
        /// Result == wwww0000 00000000
        ///
        /// </summary>
        public static int emuCycle = 0;
        public static void emulateCycle() {
            clearOpOutput();
            // Fetch Opcode -- Shift left 8 then bitwise or to add pc+1 to right
            //Memory.DisplayMemory(0,4095);
            SYS_STATE();
            opcode = (UInt16)(Memory.memory[pc] << 8 | Memory.memory[pc + 1]);

            Console.WriteLine("Switch to" + (opcode & 0xF000).ToString("X"));
            switch (opcode & 0xF000) {
                case 0x0000://
                    Console.WriteLine("Embedded Switch to" + (opcode & 0x000F).ToString("X"));
                    switch (opcode & 0x000F) {

                        //Actual 0x0000 is:
                        //SYS addr -- Jump to a machine code routine at nnn.
                        //This instruction is only used on the old computers on which Chip-8 was originally implemented. 
                        //It is ignored by modern interpreters.

                        case 0x0000://00E0 - CLS == Clear the display.
                            opOut[0] = "00E0: CLS (Clear Screen)";
                            opOut[1] = "STATUS: Assumed Working";
                            opOut[3] = "gfxOut is zeroed, Drawflag is true, Increment PC";
                            
                            for (int i = 0; i < 2048; ++i) { GFX.gfxOut[i] = 0; }
                            drawFlag = true;
                            pc += 2;
                            //debug
                            break;

                        case 0x000E://00EE - RET == Return from a subroutine
                            opOut[0] = "00EE: RET (Return from Subroutine)";
                            opOut[1] = "STATUS: Possibly Broken";
                            opOut[3] = "The interpreter sets the program counter to the address at the top of the stack, then subtracts 1 from the stack pointer.";
                            opOut[4] = "Increment PC";
                            
                            pc = stack[sp--];//The interpreter sets the program counter to the address at the top of the stack, then subtracts 1 from the stack pointer.
                            pc += 2;
                            break;

                        default:
                            opOut[0] = "ERROR-- - OPCODE: " + opcode.ToString("X") + " DNE";
                            break;
                    }
                    break;

                case 0x1000:// 1nnn JMP addr
                    opOut[0] = "1nnn: JMP to Addr nnn";
                    opOut[1] = "STATUS: Assumed Working";
                    opOut[3] = "JMP to 0x" + (opcode & 0x0FFF).ToString("X") + "(DEC: " + (opcode & 0x0FFF) + ")";

                    pc = Convert.ToInt16(opcode & 0x0FFF); //0x0FFF==00001111 11111111 == 4096-1
                    break;

                case 0x2000:// 2nnn CALL subroutine at addr
                    opOut[0] = "2nnn: CALL Subroutine at Addr nnn";
                    opOut[1] = "STATUS: Assumed Working";
                    opOut[3] = "CALL to 0x" + (opcode & 0x0FFF).ToString("X") + " (DEC:" + (opcode & 0x0FFF) + ")";
                    opOut[4] = "The interpreter increments the stack pointer, then puts the current PC on the top of the stack.";
                    opOut[5] = "The PC is then set to nnn.";
                    stack[++sp] = pc;//The interpreter increments the stack pointer, then puts the current PC on the top of the stack.
                    pc = Convert.ToInt16(opcode & 0x0FFF);// The PC is then set to nnn.
                    break;

                case 0x3000:// 3xkk - SE Vx, byte == Skip next instruction if register Vx = kk.
                    opOut[0] = "3xkk: - SE Vx, byte (Skip next instruction if register Vx = kk)";
                    opOut[1] = "STATUS: Assumed Working";
                    opOut[3] = "IF (Vx == kk)  -> PC+=4 (skip next)";
                    opOut[4] = "ELSE: PC+=2";

                    if ((V[(opcode & 0x0F00) >> 8]) == (opcode & 0x00FF)) {
                        opOut[5] = "RETURNED: True -- PC+=4";
                        pc += 4;
                    }
                    else {
                        opOut[5] = "RETURNED: False -- PC+=2";
                        pc += 2;
                    }
                    break;

                case 0x4000:// 4xkk - SnE Vx, byte == Skip next instruction if Vx != kk.
                    opOut[0] = "4xkk - SnE Vx, byte == Skip next instruction if Vx != kk.";
                    opOut[1] = "STATUS: Assumed Working";
                    opOut[3] = "IF (Vx != kk)  -> PC+=4 (skip next)";
                    opOut[4] = "ELSE: PC+=2";
                    if ((V[(opcode & 0x0F00) >> 8]) != (opcode & 0x00FF)) {
                        opOut[5] = "RETURNED: True -- PC+=4";
                        pc += 4;
                    }
                    else {
                        opOut[5] = "RETURNED: False -- PC+=2";
                        pc += 2; }
                    break;

                case 0x5000:// 5xy0 - SE Vx, Vy == Skip next instruction if Vx = Vy.
                    opOut[0] = "5xy0 - SE Vx, Vy == Skip next instruction if Vx = Vy";
                    opOut[1] = "STATUS: Assumed Working";
                    opOut[3] = "IF (Vx == Vy)  -> PC+=4 (skip next)";
                    opOut[4] = "ELSE: PC+=2";
                    if ((V[(opcode & 0x0F00) >> 8]) == (V[(opcode & 0x00F0) >> 4])) {
                        opOut[5] = "RETURNED: True -- PC+=4";
                        pc += 4;
                    }
                    else {
                        opOut[5] = "RETURNED: False -- PC+=2";
                        pc += 2;
                    }
                    break;

                case 0x6000:// 6xkk - LD Vx, byte == Set Vx = kk.
                    opOut[0] = "6xkk - LD Vx, byte == Set Vx = kk";
                    opOut[1] = "STATUS: Assumed Working";
                    opOut[3] = "Puts the value kk into register Vx. PC+=2 ";
                    V[(opcode & 0x0F00) >> 8] = (UInt16)(opcode & 0x00FF);//The interpreter puts the value kk into register Vx.
                    pc += 2;
                    break;

                case 0x7000:// 7xkk - ADD Vx, byte -- Set Vx = Vx + kk.
                    opOut[0] = "7xkk - ADD Vx, byte == Set Vx = Vx + kk";
                    opOut[1] = "STATUS: Assumed Working";
                    opOut[3] = "Adds the value kk to the value of register Vx, then stores the result in Vx. PC+=2";
                    V[(opcode & 0x0F00) >> 8] += (UInt16)(opcode & 0x00FF);//Adds the value kk to the value of register Vx, then stores the result in Vx.
                    pc += 2;
                    break;


                case 0x8000:
                    switch (opcode & 0x000F) {
                        case 0x0000://8xy0 - LD Vx, Vy -- Set Vx = Vy.
                            opOut[0] = "8xy0 - LD Vx, Vy -- Set Vx = Vy";
                            opOut[1] = "STATUS: Assumed Working";
                            opOut[3] = "Stores the value of register Vy in register Vx";
                            opOut[4] = "PC += 2";
                            V[(opcode & 0x0F00)>>8] = V[(opcode & 0x00F0)>>4];//Stores the value of register Vy in register Vx.
                            pc += 2;
                            break;

                        case 0x0001://8xy1 - OR Vx, Vy == Set Vx = Vx OR Vy.
                            opOut[0] = "8xy1 - OR Vx, Vy -- Set Vx = Vx OR Vy";
                            opOut[1] = "STATUS: Assumed Working";
                            opOut[3] = "Performs a bitwise OR on the values of Vx and Vy, then stores the result in Vx.  ";
                            opOut[4] = "PC += 2";
                            V[(opcode & 0x0F00) >> 8] |= V[(opcode & 0x00F0) >> 4];//Performs a bitwise OR on the values of Vx and Vy, then stores the result in Vx.
                            pc += 2;
                            break;

                        case 0x0002://8xy2 - AND Vx, Vy == Set Vx = Vx AND Vy.
                            opOut[0] = "8xy2 - AND Vx, Vy -- Set Vx = Vx AND Vy";
                            opOut[1] = "STATUS: Assumed Working";
                            opOut[3] = "Bitwise AND on values of Vx and Vy, then stores the result in Vx.";
                            opOut[4] = "PC += 2";
                            V[(opcode & 0x0F00) >> 8] &= V[(opcode & 0x00F0) >> 4];//Performs a bitwise AND on the values of Vx and Vy, then stores the result in Vx.
                            pc += 2;
                            break;

                        case 0x0003://8xy3 - XOR Vx, Vy == Set Vx = Vx XOR Vy.
                            opOut[0] = "8xy3 - XOR Vx, Vy -- Set Vx = Vx XOR Vy";
                            opOut[1] = "STATUS: Assumed Working";
                            opOut[3] = "Performs a bitwise exclusive OR on the values of Vx and Vy, then stores the result in Vx";
                            opOut[4] = "PC += 2";
                            V[(opcode & 0x0F00) >> 8] ^= V[(opcode & 0x00F0) >> 4];//Performs a bitwise exclusive OR on the values of Vx and Vy, then stores the result in Vx. 
                            pc += 2;
                            break;



                        case 0x0004://8xy4 - ADD Vx, Vy == Set Vx = Vx + Vy, set VF = carry.
                            opOut[0] = "8xy4 - ADD Vx, Vy == Set Vx = Vx + Vy, set VF = carry";
                            opOut[1] = "STATUS: Likely Broken";
                            opOut[3] = "The values of Vx and Vy are added together.If the result is greater than 8 bits(i.e., > 255,) VF is set to 1, otherwise 0.Only the lowest 8 bits of the result are kept, and stored in Vx.";
                            
                            //The values of Vx and Vy are added together.If the result is greater than 8 bits(i.e., > 255,) VF is set to 1, otherwise 0.Only the lowest 8 bits of the result are kept, and stored in Vx.
                            if (V[(opcode & 0x00F0) >> 4] > (0xFF - V[(opcode & 0x0F00) >> 8])) {
                                opOut[4] = "RETURN: True (Carry Flag = 1)";
                                V[0xF] = 1; //carry
                            }
                            else {
                                opOut[4] = "RETURN: False (Carry Flag = 0)";
                                V[0xF] = 0;
                            }
                            opOut[5] = "Store result in Vx";
                            opOut[6] = "PC += 2";
                            V[(opcode & 0x0F00) >> 8] += V[(opcode & 0x00F0) >> 4];
                            pc += 2;
                            break;



                        case 0x0005://8xy5 - SUB Vx, Vy == Set Vx = Vx - Vy, set VF = NOT borrow.
                            opOut[0] = "8xy5 - SUB Vx, Vy -- Set Vx = Vx - Vy, set VF = NOT borrow";
                            opOut[1] = "STATUS: Assumed Working";
                            opOut[3] = "IF (Vx > Vy) VF = 1";
                            opOut[3] = "ELSE: VF = 0";
                            //If Vx > Vy, then VF is set to 1, otherwise 0. Then Vy is subtracted from Vx, and the results stored in Vx.
                            if (V[(opcode & 0x00F0) >> 4] > V[(opcode & 0x0F00) >> 8]) {
                                opOut[4] = "RETURN: True (VF = 0 -- Borrow)";
                                V[0xF] = 0; // there is a borrow
                            }
                            else {
                                opOut[4] = "RETURN: False (VF = 1 -- NOT Borrow)";
                                V[0xF] = 1;
                            }
                            opOut[4] = "Vy is subtracted from Vx, and the results stored in Vx";
                            opOut[4] = "PC += 2";
                            V[(opcode & 0x0F00) >> 8] -= V[(opcode & 0x00F0) >> 4];
                            pc += 2;
                            break;



                        case 0x0006://8xy6 - SHR Vx {, Vy} == Set Vx = Vx SHR 1
                                    //If the least-significant bit of Vx is 1, then VF is set to 1, otherwise 0. Then Vx is divided by 2.
                                    //V[0xF] = (V[(opcode & 0x0F00) >> 8]) & Convert.ToChar(0x0001);
                            V[(opcode & 0x0F00) >> 8] >>= 1;
                            pc += 2;
                            break;




                        case 0x0007://8xy7 - SUBN Vx, Vy == Set Vx = Vy - Vx, set VF = NOT borrow.
                                    //If Vy > Vx, then VF is set to 1, otherwise 0.Then Vx is subtracted from Vy, and the results stored in Vx.
                            if (V[(opcode & 0x0F00) >> 8] > V[(opcode & 0x00F0) >> 4])  // VY-VX
                                V[0xF] = 0; // there is a borrow
                            else
                                V[0xF] = 1;
                            //V[(opcode & 0x0F00) >> 8] = V[(opcode & 0x00F0) >> 4] - V[(opcode & 0x0F00) >> 8];
                            pc += 2;
                            break;




                        case 0x000E://8xyE - SHL Vx {, Vy} == Set Vx = Vx SHL 1.
                            //If the most - significant bit of Vx is 1, then VF is set to 1, otherwise to 0.Then Vx is multiplied by 2.
                            //V[0xF] = V[(opcode & 0x0F00) >> 8] >> 7;
                            V[(opcode & 0x0F00) >> 8] <<= 1;
                            pc += 2;
                            break;
                    }
                    break;




                case 0x9000:// 9xy0 - SNE Vx, Vy == Skip next instruction if Vx != Vy.
                    //The values of Vx and Vy are compared, and if they are not equal, the program counter is increased by 2.
                    break;

                case 0xA000://Annn - LD I, addr == Set I = nnn.
                    I = (UInt16)(opcode & 0x0FFF);
                    pc += 2;
                    break;

                case 0xB000://Bnnn - JP V0, addr == Jump to location nnn + V0.
                    //The program counter is set to nnn plus the value of V0.
                    break;

                case 0xC000://Cxkk - RND Vx, byte == Set Vx = random byte AND kk.
                    Random byt = new Random();
                    V[(opcode & 0x0F00) >> 8] = (UInt16)((byt.Next(0, 255) % 0xFF) & (opcode & 0x00FF));
                    pc += 2;//The interpreter generates a random number from 0 to 255, which is then ANDed with the value kk. The results are stored in Vx.See instruction 8xy2 for more information on AND.
                    break;

                case 0xD000://Dxyn - DRW Vx, Vy, nibble == Display n-byte sprite starting at memory location I at (Vx, Vy), set VF = collision.
                            //The interpreter reads n bytes from memory, starting at the address stored in I.These bytes are then displayed as sprites on screen at coordinates(Vx, Vy). 
                            //Sprites are XORed onto the existing screen.If this causes any pixels to be erased, VF is set to 1, otherwise it is set to 0.If the sprite is positioned so part of it is outside the coordinates of the display, 
                            //it wraps around to the opposite side of the screen.See instruction 8xy3 for more information on XOR, and section 2.4, Display, for more information on the Chip - 8 screen and sprites.
                    UInt16 x = V[(opcode & 0x0F00) >> 8];
                    UInt16 y = V[(opcode & 0x00F0) >> 4];
                    UInt16 height = (UInt16)(opcode & 0x000F);
                    UInt16 pixel;

                    V[0xF] = 0;
                    for (int yline = 0; yline < height; yline++) {
                        pixel = Memory.memory[I + yline];
                        for (int xline = 0; xline < 8; xline++) {
                            if ((pixel & (0x80 >> xline)) != 0) {
                                Console.WriteLine(x + xline + ((y + yline) * 64));
                                if (GFX.gfxOut[(x + xline + ((y + yline) * 64))] == 1) {
                                    V[0xF] = 1;
                                }
                                GFX.gfxOut[x + xline + ((y + yline) * 64)] ^= 1;
                            }
                        }
                    }

                    drawFlag = true;
                    pc += 2;
                    break;

                case 0xE000:
                    switch (opcode & 0x000F) {
                        case 0x000E://Ex9E - SKP Vx  Skip next instruction if key with the value of Vx is pressed.
                                    //Checks the keyboard, and if the key corresponding to the value of Vx is currently in the down position, PC is increased by 2.
                            pc += 2;
                            break;
                        case 0x0001://ExA1 - SKNP Vx  Skip next instruction if key with the value of Vx is not pressed.
                                    //Checks the keyboard, and if the key corresponding to the value of Vx is currently in the up position, PC is increased by 2.
                            pc += 2;
                            break;
                        default:
                            Console.Write("ERROR---OPCODE: " + opcode.ToString("X") + " DNE");
                            break;
                    }
                    break;

                case 0xF000:
                    switch (opcode & 0x00FF) {
                        case 0x0007: //Fx07 - LD Vx, DT: Sets VX to the value of the delay timer
                            V[(opcode & 0x0F00) >> 8] = delay_timer;
                            pc += 2;
                            break;

                        case 0x000A: // FX0A: A key press is awaited, and then stored in VX		
                            Console.WriteLine("BROKEN++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++");
                            bool keyPress = false;

                            for (int i = 0; i < 16; ++i) {
                                //if (key[i] != 0) {
                                //   V[(opcode & 0x0F00) >> 8] = i;
                                //   keyPress = true;
                                //}
                            }

                            // If we didn't received a keypress, skip this cycle and try again.
                            if (!keyPress) {
                                return;
                            }
                            pc += 2;

                            break;

                        case 0x0015: // FX15: Sets the delay timer to VX
                            delay_timer = V[(opcode & 0x0F00) >> 8];
                            pc += 2;
                            break;

                        case 0x0018: // FX18: Sets the sound timer to VX
                            sound_timer = V[(opcode & 0x0F00) >> 8];
                            pc += 2;
                            break;

                        case 0x001E: // FX1E: Adds VX to I
                            if (I + V[(opcode & 0x0F00) >> 8] > 0xFFF)  // VF is set to 1 when range overflow (I+VX>0xFFF), and 0 when there isn't.
                                V[0xF] = 1;
                            else
                                V[0xF] = 0;
                            I += V[(opcode & 0x0F00) >> 8];
                            pc += 2;
                            break;

                        case 0x0029: // FX29: Sets I to the location of the sprite for the character in VX. Characters 0-F (in hexadecimal) are represented by a 4x5 font
                            I = (UInt16)(V[(opcode & 0x0F00) >> 8] * 0x5);
                            pc += 2;
                            break;

                        case 0x0033: // FX33: Stores the Binary-coded decimal representation of VX at the addresses I, I plus 1, and I plus 2
                            Memory.memory[I] = (UInt16)(V[(opcode & 0x0F00) >> 8] / 100);
                            Memory.memory[I + 1] = (UInt16)((V[(opcode & 0x0F00) >> 8] / 10) % 10);
                            Memory.memory[I + 2] = (UInt16)((V[(opcode & 0x0F00) >> 8] % 100) % 10);
                            pc += 2;
                            break;

                        case 0x0055: // FX55: Stores V0 to VX in memory starting at address I					
                            for (int i = 0; i <= ((opcode & 0x0F00) >> 8); ++i)
                                Memory.memory[I + i] = V[i];

                            // On the original interpreter, when the operation is done, I = I + X + 1.
                            I += (UInt16)(((opcode & 0x0F00) >> 8) + 1);
                            pc += 2;
                            break;

                        case 0x0065: // FX65: Fills V0 to VX with values from memory starting at address I					
                            for (int i = 0; i <= ((opcode & 0x0F00) >> 8); ++i)
                                V[i] = Memory.memory[I + i];

                            // On the original interpreter, when the operation is done, I = I + X + 1.
                            I += (UInt16)(((opcode & 0x0F00) >> 8) + 1);
                            pc += 2;
                            break;

                        default:
                            Console.Write("ERROR---OPCODE: " + opcode.ToString("X") + " DNE");

                            break;
                    }
                    break;
                default:
                    Console.Write("ERROR---OPCODE: " + opcode.ToString("X") + " DNE");
                    break;



            }


            // Update timers
            if (delay_timer > 0)
                --delay_timer;
            if (sound_timer > 0) {
                if (sound_timer == 1)
                    dbgMsg = "BEEP!";
                --sound_timer;
            }
        }


    }
}
