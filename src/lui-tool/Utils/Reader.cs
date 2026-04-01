using System;
using System.Collections.Generic;
using System.Buffers.Binary;

namespace LuiTool.Utils;

class Reader
{
    private readonly byte[] data;
    private int cursor;
    private bool littleEndian;
    private readonly Stack<int> cursorStack;

    public Reader(byte[] data, bool littleEndian = true)
    {
        this.data = data;
        this.littleEndian = littleEndian;
        cursor = 0;
        cursorStack = new Stack<int>();
    }

    public void SetLittleEndian(bool isLittleEndian)
    {
        littleEndian = isLittleEndian;
    }

    public sbyte ReadInt8()
    {
        return (sbyte)ReadUInt8();
    }

    public byte ReadUInt8()
    {

        byte ret = data[cursor];
        cursor += sizeof(byte);
        return ret;
    }

    public short ReadInt16()
    {
        Span<byte> span = new Span<byte>(data)[cursor..];
        short ret = littleEndian ? BinaryPrimitives.ReadInt16LittleEndian(span) : BinaryPrimitives.ReadInt16BigEndian(span);
        cursor += sizeof(short);
        return ret;
    }

    public ushort ReadUInt16()
    {
        return (ushort)ReadInt16();
    }

    public int ReadInt32()
    {
        Span<byte> span = new Span<byte>(data)[cursor..];
        int ret = littleEndian ? BinaryPrimitives.ReadInt32LittleEndian(span) : BinaryPrimitives.ReadInt32BigEndian(span);
        cursor += sizeof(int);
        return ret;
    }

    public uint ReadUInt32()
    {
        return (uint)ReadInt32();
    }

    public long ReadInt64()
    {
        Span<byte> span = new Span<byte>(data)[cursor..];
        long ret = littleEndian ? BinaryPrimitives.ReadInt64LittleEndian(span) : BinaryPrimitives.ReadInt64BigEndian(span);
        cursor += sizeof(long);
        return ret;
    }

    public ulong ReadUInt64()
    {
        return (ulong)ReadInt64();
    }

    public float ReadFloat()
    {
        Span<byte> span = new Span<byte>(data)[cursor..];
        float ret = littleEndian ? BinaryPrimitives.ReadSingleLittleEndian(span) : BinaryPrimitives.ReadSingleBigEndian(span);
        cursor += sizeof(float);
        return ret;
    }

    public double ReadDouble()
    {
        Span<byte> span = new Span<byte>(data)[cursor..];
        double ret = littleEndian ? BinaryPrimitives.ReadDoubleLittleEndian(span) : BinaryPrimitives.ReadDoubleBigEndian(span);
        cursor += sizeof(double);
        return ret;
    }

    public string ReadString(int length)
    {
        string ret = System.Text.Encoding.UTF8.GetString(data, cursor, length);
        cursor += length;
        return ret;
    }

    public byte[] ReadBytes(int length)
    {
        byte[] ret = data[cursor..(cursor + length)];
        cursor += length;
        return ret;
    }

    public void Skip(int offset)
    {
        cursor += offset;
    }

    public void Seek(int position)
    {
        cursor = position;
    }

    public void PushCursor()
    {
        cursorStack.Push(cursor);
    }

    public int PopCursor()
    {
        return cursorStack.Pop();
    }

    public void Pad(int padding)
    {
        if (cursor % padding != 0)
        {
            Skip(padding - (cursor % padding));
        }
    }

    public int GetPosition()
    {
        return cursor;
    }
}
