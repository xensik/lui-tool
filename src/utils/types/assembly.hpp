// Copyright 2020 xensik. All rights reserved.
//
// Use of this source code is governed by a GNU GPLv3 license
// that can be found in the LICENSE file.

#ifndef _LUI_ASSEMBLY_HPP_
#define _LUI_ASSEMBLY_HPP_

namespace lui
{

enum class instruction_mode
{
    unset,
    ABC,
    ABx,
    AsBx,
};

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

struct constant
{
    object_type type;
    std::string value;

    constant(object_type type, const std::string& value): type(type), value(value) {}
};

struct typeinfo
{
    std::uint32_t id;
    std::string name;

    typeinfo(std::uint32_t id, const std::string& name) : id(id), name(name) {}
};

struct instruction
{
    std::uint32_t index;
    std::uint32_t value;
    std::string data;
    instruction_mode mode;
    std::uint8_t OP;
    std::uint32_t A;
    std::uint32_t B;
    std::uint32_t C;
    std::uint32_t Bx;
    std::int32_t sBx;
    bool sZero;

    instruction(std::uint32_t index, std::uint32_t value, std::uint8_t OP, std::uint32_t A,
        std::uint32_t B, std::uint32_t C, std::uint32_t Bx, std::int32_t sBx, bool sZero)
        : index(index),value(value), OP(OP), A(A), B(B), C(C), Bx(Bx), sBx(sBx), 
            sZero(sZero), mode(instruction_mode::unset) {}
};

struct function
{
    std::uint32_t upval_count;
    std::uint32_t param_count;
    std::uint8_t  flags;
    std::uint32_t register_count;
    std::uint64_t instruction_count;
    std::vector<std::unique_ptr<instruction>> instructions;
    std::uint32_t constant_count;
    std::vector<std::unique_ptr<constant>> constants;
    std::uint32_t debug;
    std::uint32_t sub_func_count;
    std::vector<std::unique_ptr<function>> sub_funcs;
};

struct header
{
    std::uint32_t magic;                // HKS_DUMP_SIGNATURE 1B 4C 75 61 : 0x61754C1B
    std::uint8_t lua_version;           // 0x51
    std::uint8_t format_version;        // 0x0D
    std::uint8_t endianness;            // 0x01
    std::uint8_t size_of_int;           // 0x04
    std::uint8_t size_of_size_t;        //  ?
    std::uint8_t size_of_intruction;    // 0x04
    std::uint8_t size_of_lua_number;    // 0x04
    std::uint8_t integral_flag;         // 0x01
    std::uint8_t build_flags;           // 0x03
    std::uint8_t referenced_mode;       // 0x01
    std::uint32_t type_count;           // 13
    std::vector<typeinfo> types;
};

struct file
{
    header header;
    std::unique_ptr<function> main;
};

using constant_ptr = std::unique_ptr<constant>;
using header_ptr = std::unique_ptr<header>;
using instruction_ptr = std::unique_ptr<instruction>;
using function_ptr = std::unique_ptr<function>;
using file_ptr = std::unique_ptr<file>;

} // namespace lui

#endif // _LUI_ASSEMBLY_HPP_
