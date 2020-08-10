﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using luadec.Utilities;

namespace luadec
{
    public class LuaFile_old
    {
        public enum LuaVersion
        {
            Lua50,
            Lua51HKS,
        }

        public LuaVersion Version;

        public static string ReadLuaString(BinaryReaderEx br, LuaVersion version)
        {
            ulong length = br.ReadUInt64();
            if (length > 0)
            {
                string ret;
                if (version == LuaVersion.Lua50)
                {
                    ret = br.ReadShiftJIS((int)length - 1);
                }
                else
                {
                    ret = br.ReadShiftJIS((int)length - 1);
                    //ret = br.ReadUTF8((int)length - 1);
                }
                br.AssertByte(0); // Eat null terminator
                return ret;
            }
            return null;
        }

        public class Header
        {
            public LuaVersion LuaVersion;
            byte Version;
            byte Endianess;
            byte IntSize;
            byte LongSize;
            byte InstructionSize;
            byte OpSize;
            byte ASize;
            byte BSize;
            byte CSize;
            byte LuaNumberSize;

            public Header(BinaryReaderEx br)
            {
                br.ReadUInt32(); // Magic
                Version = br.ReadByte();
                if (Version == 0x50)
                {
                    LuaVersion = LuaVersion.Lua50;
                }
                else if (Version == 0x51)
                {
                    byte format = br.AssertByte(0x0E); // HKS
                    LuaVersion = LuaVersion.Lua51HKS;
                }
                Endianess = br.ReadByte();
                IntSize = br.ReadByte();
                LongSize = br.ReadByte();
                InstructionSize = br.ReadByte();
                if (LuaVersion == LuaVersion.Lua50)
                {
                    OpSize = br.ReadByte();
                    ASize = br.ReadByte();
                    BSize = br.ReadByte();
                    CSize = br.ReadByte();
                }
                LuaNumberSize = br.ReadByte();
                if (LuaVersion == LuaVersion.Lua50)
                {
                    br.ReadDouble(); // test number
                }
                else
                {
                    br.ReadByte(); // Isintegral
                }
            }
        }

        public class Constant
        {
            public enum ConstantType
            {
                TypeString,
                TypeNumber,
                TypeNil
            }

            public ConstantType Type;
            public string StringValue;
            public double NumberValue;

            public Constant(LuaFile_old file, BinaryReaderEx br, LuaVersion version)
            {
                byte type = br.ReadByte();
                if (type == 3)
                {
                    NumberValue = br.ReadDouble();
                    Type = ConstantType.TypeNumber;
                }
                else if (type == 4)
                {
                    StringValue = LuaFile_old.ReadLuaString(br, version);
                    Type = ConstantType.TypeString;
                }
                else
                {
                    Type = ConstantType.TypeNil;
                }
            }

            public override string ToString()
            {
                if (Type == ConstantType.TypeString)
                {
                    return StringValue;
                }
                else if (Type == ConstantType.TypeNumber)
                {
                    return NumberValue.ToString();
                }
                return "NULL";
            }
        }

        public class ConstantHKS
        {
            public enum ConstantType
            {
                TypeNil,
                TypeBoolean,
                TypeLightUserdata,
                TypeNumber,
                TypeString,
                TypeTable,
                TypeFunction,
                TypeUserdata,
                TypeThread,
                TypeIFunction,
                TypeCFunction,
                TypeUInt64,
                TypeStuct,
            }

            public ConstantType Type;
            public bool BoolValue;
            public string StringValue;
            public float NumberValue;

            public ConstantHKS(LuaFile_old file, BinaryReaderEx br)
            {
                byte type = br.ReadByte();
                if (type == 0)
                {
                    Type = ConstantType.TypeNil;
                }
                else if (type == 1)
                {
                    Type = ConstantType.TypeBoolean;
                    BoolValue = br.ReadBoolean();
                }
                else if (type == 3)
                {
                    NumberValue = br.ReadSingle();
                    Type = ConstantType.TypeNumber;
                }
                else if (type == 4)
                {
                    StringValue = LuaFile_old.ReadLuaString(br, LuaVersion.Lua51HKS);
                    Type = ConstantType.TypeString;
                }
                else
                {
                    throw new Exception("Unimplemented HKS type");
                }
            }

            public override string ToString()
            {
                if (Type == ConstantType.TypeString)
                {
                    return StringValue;
                }
                else if (Type == ConstantType.TypeNumber)
                {
                    return NumberValue.ToString();
                }
                else if (Type == ConstantType.TypeBoolean)
                {
                    return BoolValue ? "true" : "false";
                }
                else if (Type == ConstantType.TypeNil)
                {
                    return "nil";
                }
                return "NULL";
            }
        }

        public class Local
        {
            public string Name;
            public int Start;
            public int End;

            public Local(BinaryReaderEx br, LuaVersion version)
            {
                Name = ReadLuaString(br, version);
                Start = br.ReadInt32();
                End = br.ReadInt32();
            }
        }

        public class Upvalue
        {
            public string Name;

            public Upvalue(BinaryReaderEx br, LuaVersion version)
            {
                Name = ReadLuaString(br, version);
            }
        }

        public class Function
        {
            public string Name;
            public string Path;
            public int LineDefined;
            public byte Nups;
            public uint NumParams;
            public uint NumSlots; // HKS
            public byte IsVarArg;
            public byte MaxStackSize;
            public int SizeLineInfo;
            public int LocalVarsCount;
            public int UpValuesCount;
            public Constant[] Constants;
            public ConstantHKS[] ConstantsHKS;
            public Local[] Locals;
            public Dictionary<int, List<Local>> LocalMap;
            public Upvalue[] Upvalues;
            public Function[] ChildFunctions;
            public byte[] Bytecode;

            public Function(LuaFile_old file, LuaVersion version, BinaryReaderEx br)
            {
                if (version == LuaVersion.Lua50)
                {
                    Name = LuaFile_old.ReadLuaString(br, version);
                    LineDefined = br.ReadInt32();
                    Nups = br.ReadByte();
                    NumParams = br.ReadByte();
                    IsVarArg = br.ReadByte();
                    MaxStackSize = br.ReadByte();
                    SizeLineInfo = br.ReadInt32();
                    // Eat line numbers
                    br.ReadInt32s(SizeLineInfo);
                    LocalVarsCount = br.ReadInt32();
                    Locals = new Local[LocalVarsCount];
                    LocalMap = new Dictionary<int, List<Local>>();
                    for (int i = 0; i < LocalVarsCount; i++)
                    {
                        Locals[i] = new Local(br, version);
                        if (!Locals[i].Name.StartsWith("("))
                        {
                            if (!LocalMap.ContainsKey(Locals[i].Start))
                            {
                                LocalMap[Locals[i].Start] = new List<Local>();
                            }
                            LocalMap[Locals[i].Start].Add(Locals[i]);
                        }
                    }
                    UpValuesCount = br.ReadInt32();
                    Upvalues = new Upvalue[UpValuesCount];
                    for (int i = 0; i < UpValuesCount; i++)
                    {
                        Upvalues[i] = new Upvalue(br, version);
                    }
                    int constantsCount = br.ReadInt32();
                    Constants = new Constant[constantsCount];
                    for (int i = 0; i < constantsCount; i++)
                    {
                        Constants[i] = new Constant(file, br, version);
                    }
                    int funcCount = br.ReadInt32();
                    ChildFunctions = new Function[funcCount];
                    for (int i = 0; i < funcCount; i++)
                    {
                        ChildFunctions[i] = new Function(file, version, br);
                    }
                    int bytecodeCount = br.ReadInt32();
                    Bytecode = br.ReadBytes(bytecodeCount * 4);
                }
                else if (version == LuaVersion.Lua51HKS)
                {
                    // Thanks @horkrux for reverse engineering this
                    br.ReadInt32();
                    NumParams = br.ReadUInt32();
                    br.ReadByte(); // Unk
                    NumSlots = br.ReadUInt32();
                    br.ReadUInt32(); // unk
                    int bytecodeCount = br.ReadInt32();
                    br.Pad(4);
                    Bytecode = br.ReadBytes(bytecodeCount * 4);
                    int constantsCount = br.ReadInt32();
                    ConstantsHKS = new ConstantHKS[constantsCount];
                    for (int i = 0; i < constantsCount; i++)
                    {
                        ConstantsHKS[i] = new ConstantHKS(file, br);
                    }
                    br.ReadInt32(); // unk
                    br.ReadInt32(); // unk
                    LocalVarsCount = br.ReadInt32();
                    UpValuesCount = br.ReadInt32();
                    br.ReadInt32(); // line begin
                    br.ReadInt32(); // line end
                    Path = ReadLuaString(br, version);
                    Name = ReadLuaString(br, version);
                    // Eat line numbers
                    br.ReadInt32s(bytecodeCount);
                    Locals = new Local[LocalVarsCount];
                    LocalMap = new Dictionary<int, List<Local>>();
                    for (int i = 0; i < LocalVarsCount; i++)
                    {
                        Locals[i] = new Local(br, version);
                        if (!Locals[i].Name.StartsWith("("))
                        {
                            if (!LocalMap.ContainsKey(Locals[i].Start))
                            {
                                LocalMap[Locals[i].Start] = new List<Local>();
                            }
                            LocalMap[Locals[i].Start].Add(Locals[i]);
                        }
                    }
                    Upvalues = new Upvalue[UpValuesCount];
                    for (int i = 0; i < UpValuesCount; i++)
                    {
                        Upvalues[i] = new Upvalue(br, version);
                    }
                    int funcCount = br.ReadInt32();
                    ChildFunctions = new Function[funcCount];
                    for (int i = 0; i < funcCount; i++)
                    {
                        ChildFunctions[i] = new Function(file, version, br);
                    }
                }
            }

            public List<Local> LocalsAt(int i)
            {
                if (LocalMap.ContainsKey(i))
                {
                    return LocalMap[i];
                }
                return null;
            }

            public override string ToString()
            {
                if (Name != null && Name != "")
                {
                    return Name;
                }
                return base.ToString();
            }
        }

        public Header LuaHeader;
        public Function MainFunction;

        public LuaFile_old(BinaryReaderEx br)
        {
            LuaHeader = new Header(br);
            Version = LuaHeader.LuaVersion;
            if (Version == LuaVersion.Lua51HKS)
            {
                br.BigEndian = true;
                br.Position = 0xee; // lel
            }
            MainFunction = new Function(this, Version, br);
        }
    }
}
