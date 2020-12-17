// Copyright 2020 xensik. All rights reserved.
//
// Use of this source code is governed by a GNU GPLv3 license
// that can be found in the LICENSE file.

#ifndef _LUI_ASSEMBLY_HPP_
#define _LUI_ASSEMBLY_HPP_

namespace lui
{

struct data
{
enum class t
{
    NIL, BOOLEAN, LIGHTUSERDATA, NUMBER, STRING, TABLE, FUNCTION,
    USERDATA, THREAD, IFUNCTION, CFUNCTION, UI64, STRUCT,
};
    t type_;
    std::string value_;

    data(t type, const std::string& value): type_(type), value_(value) {}

    auto type() -> t
    {
        return type_;
    }

    auto value() -> std::string
    {
        return value_;
    }

    void to_literal()
    {
        for(auto i = 0; i < value_.size(); i++)
        {
            if(value_.at(i) == '"')
            {
                value_.insert(value_.begin() + i, '\\');
                i++;
            }
        }
        value_ = "\""s += value_ += "\"";
    }
};

struct kst
{
    data data_;

    kst(data::t type, const std::string& value): data_(data(type, value)) {}

    auto print() -> std::string
    {
        return "KST(" + data_.value_ + ")";
    }

    auto to_node() -> lui::node_ptr
    {
        switch(data_.type_)
        {
            case data::t::NIL:
                return std::make_shared<lui::node_nil>();
                break;
            case data::t::BOOLEAN:
                return std::make_shared<lui::node_boolean>((data_.value_ == "true") ? true : false);
                break;
            case data::t::NUMBER:
                return std::make_shared<lui::node_number>(data_.value_);
                break;
            case data::t::STRING:
                if(data_.value_.size() == 0)
                {
                    data_.to_literal();
                    return std::make_shared<lui::node_string>(data_.value_);
                }
                if(data_.value_.at(0) == '\"')
                    return std::make_shared<lui::node_string>(data_.value_);
                else
                    return std::make_shared<lui::node_identifier>(data_.value_);                
                break;
            default:
                LOG_ERROR("constant node type not supported");
                break;
        }
    }
};

struct reg
{
    std::uint32_t   index_;
    data            data_;

    reg(std::uint32_t index): index_(index), data_(lui::data(data::t::NIL, "nil")) {}

    auto print() -> std::string
    {
        return utils::string::va("REG(%02X)", index_);
    }
};

struct type_info
{
    std::uint32_t id;
    std::string name;

    type_info(std::uint32_t id, const std::string& name) : id(id), name(name) {}
};

struct instruction
{
    std::uint32_t index;
    std::uint32_t value;
    std::string data;

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
            sZero(sZero) {}
};

struct function
{
    std::uint32_t upval_count;
    std::uint32_t param_count;
    std::uint8_t  vararg_flags;         // 1=VARARG_HASARG, 2=VARARG_ISVARARG, 4=VARARG_NEEDSARG
    std::uint32_t register_count;       // -- maximum stack size
    // List instructions
    std::uint64_t instruction_count;
    std::vector<std::unique_ptr<instruction>> instructions;
    // List constants
    std::uint32_t constant_count;
    std::vector<kst> constants;

    std::uint32_t debug;
    std::uint32_t sub_func_count;
    std::vector<function> sub_funcs;

    // ----------------------------------------------------------    
    std::string name;
    std::vector<std::string> params;
    std::vector<lui::node_ptr> stack;
    function_ptr node;
    std::map<std::uint32_t, std::string> labels;
};

struct header
{
    std::uint32_t magic;                // 0x61754C1B HKS_DUMP_SIGNATURE "\x1BLua" 
    std::uint8_t lua_version;           // 0x51
    std::uint8_t format_version;        // 0x0D
    std::uint8_t endianness;            // 0x01
    std::uint8_t size_of_int;           // 0x04
    std::uint8_t size_of_size_t;        // 0x08
    std::uint8_t size_of_intruction;    // 0x04
    std::uint8_t size_of_lua_number;    // 0x04
    std::uint8_t integral_flag;         // 0x01
    std::uint8_t build_flags;           // 0x03
    std::uint8_t referenced_mode;       // 0x01
    std::uint32_t type_count;           // 0x0D
    std::vector<type_info> types;
};

struct file
{
    header header;
    function main;
};

using instruction_ptr = std::unique_ptr<instruction>;
using file_ptr = std::unique_ptr<file>;

} // namespace lui

#endif // _LUI_ASSEMBLY_HPP_
