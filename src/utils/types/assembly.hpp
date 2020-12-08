// Copyright 2020 xensik. All rights reserved.
//
// Use of this source code is governed by a GNU GPLv3 license
// that can be found in the LICENSE file.

#ifndef _LUI_ASSEMBLY_HPP_
#define _LUI_ASSEMBLY_HPP_

namespace lui
{

enum class object_type
{
    TNIL,
    TBOOLEAN,
    TLIGHTUSERDATA,
    TNUMBER,
    TSTRING,
    TTABLE,
    TFUNCTION,
    TUSERDATA,
    TTHREAD,
    TIFUNCTION,
    TCFUNCTION,
    TUI64,
    TSTRUCT,
};

struct instruction
{
    std::uint8_t A;
    std::uint8_t B;
    std::uint8_t C;
    std::uint8_t op;
};

struct constant
{
    object_type type;
    std::string data;
};

struct data
{
    std::uint32_t id;
    std::uint32_t unk; // a count, something about name, pointer size? length?
    std::string name;
};

struct file_header
{
    std::uint32_t magic;                // HKS_DUMP_SIGNATURE 1B 4C 75 61 : 0x61754C1B
    std::uint8_t lua_version;           // 0x51
    std::uint8_t format_version;        // 0x0D
    std::uint8_t endian_swap;           // 0x01
    std::uint8_t size_of_int;           // 0x04
    std::uint8_t size_of_sizeT;         //  ?
    std::uint8_t size_of_intruction;    // 0x04
    std::uint8_t size_of_lua_number;    // 0x04
    std::uint8_t integral_flag;         // 0x01
    std::uint8_t game_byte;             // 0x03
    std::uint8_t ref_mode;              // 0x01
    std::uint32_t datatype_count;       // 13
    std::vector<data> datatypes;
};

struct function
{
    // prologue
    std::uint32_t upval_count;
    std::uint32_t param_count;
    std::uint8_t  flags;
    std::uint32_t register_count;
    std::uint32_t instruction_count;
    
    // somebytes

    // insts
    std::vector<instruction> instructions; // read inst[num_instructions]; 4 bytes * num
    
    // consts
    std::uint32_t constant_count;
    std::vector<constant> constants; // read const type: 1 byte, read const data ...

    // epilogue
    std::uint32_t unknown_debug;
    std::uint32_t sub_func_count;
    std::vector<function> sub_funcs;
};

} // namespace lui

#endif // _LUI_ASSEMBLY_HPP_
