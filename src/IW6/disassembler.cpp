// Copyright 2020 xensik. All rights reserved.
//
// Use of this source code is governed by a GNU GPLv3 license
// that can be found in the LICENSE file.

#include "IW6.hpp"

namespace IW6
{

auto disassembler::output() -> std::vector<std::uint8_t>
{
    std::vector<std::uint8_t> output;

    return output;
}

void disassembler::disassemble(std::vector<std::uint8_t>& data)
{
    buffer_ = std::make_unique<utils::byte_buffer>(data);
    file_ = std::make_unique<lui::file>();

    this->disassemble_header();
    this->disassemble_functions();
    
    LOG_INFO("disasembled.");
}

void disassembler::disassemble_header()
{
    file_->header.magic = buffer_->read<std::uint32_t>();
    file_->header.lua_version = buffer_->read<std::uint8_t>();
    file_->header.format_version = buffer_->read<std::uint8_t>();
    file_->header.endianness = buffer_->read<std::uint8_t>();
    file_->header.size_of_int = buffer_->read<std::uint8_t>();
    file_->header.size_of_size_t = buffer_->read<std::uint8_t>();
    file_->header.size_of_intruction = buffer_->read<std::uint8_t>();
    file_->header.size_of_lua_number = buffer_->read<std::uint8_t>();
    file_->header.integral_flag = buffer_->read<std::uint8_t>();
    file_->header.build_flags = buffer_->read<std::uint8_t>();
    file_->header.referenced_mode = buffer_->read<std::uint8_t>();
    file_->header.type_count = buffer_->read<std::uint32_t>();

    for(auto i = 0; i < file_->header.type_count; i++)
    {
        auto id = buffer_->read<std::uint32_t>();
        auto length = buffer_->read<std::uint32_t>();
        auto name = buffer_->read_string();

        file_->header.types.push_back(lui::typeinfo(id, name));
    }
}

void disassembler::disassemble_functions()
{
    file_->main = std::make_unique<lui::function>();
    this->disassemble_function(file_->main);
    this->disassemble_prototype();
}

void disassembler::disassemble_prototype()
{
    auto head_mismatch = buffer_->read<std::uint32_t>();
    auto unk1 = buffer_->read<std::uint32_t>();
    auto unk2 = buffer_->read<std::uint32_t>();
}

void disassembler::disassemble_function(const lui::function_ptr& func)
{
    func->upval_count = buffer_->read<std::uint32_t>();
    func->param_count = buffer_->read<std::uint32_t>();
    func->flags = buffer_->read<std::uint8_t>();
    func->register_count = buffer_->read<std::uint32_t>();
    func->instruction_count = buffer_->read<std::uint64_t>();

    // always a byte here 0x5F
    int pad = 4 - (int)buffer_->pos() % 4;
    if (pad > 0 && pad < 4) buffer_->seek(pad);

    for (int i = 0; i < func->instruction_count; i++)
    {
        this->disassemble_instruction(func);
    }

    func->constant_count = buffer_->read<std::uint32_t>();

    for (int i = 0; i < func->constant_count; i++)
    {
        this->disassemble_constant(func);
    }

    func->debug = buffer_->read<std::uint32_t>();
    func->sub_func_count = buffer_->read<std::uint32_t>();

    for (int i = 0; i < func->sub_func_count; i++)
    {
        func->sub_funcs.push_back(std::make_unique<lui::function>());
        this->disassemble_function(func->sub_funcs.back());
    }
}

void disassembler::disassemble_instruction(const lui::function_ptr& func)
{
    auto value = buffer_->read<std::uint32_t>();

    auto op = (std::uint8_t)((value & 0xFF000000) >> 25);
    auto a = (std::uint32_t)( value & 0x000000FF);
    auto b = (std::uint32_t)((value & 0x01FE0000) >> 17);
    auto c = (std::uint32_t)((value & 0x0001FF00) >> 8);
    auto bx = (std::uint32_t)((value & 0x1FFFF00) >> 8);
    auto sbx = (std::int32_t)(bx - 0x10000 + 1);
    bool szero = false;
    if (c >= 0x100) { c -= 0x100; szero = true; }

    auto inst = std::make_unique<lui::instruction>(value, op, a, b, c, bx, sbx, szero);
    func->instructions.push_back(std::move(inst));
}

void disassembler::disassemble_constant(const lui::function_ptr& func)
{
    std::string data;
    auto type = lui::object_type(buffer_->read<std::uint8_t>());

    switch(type)
    {
        case lui::object_type::TNIL:
        break;
        case lui::object_type::TBOOLEAN:
            data = ((bool)buffer_->read<std::uint8_t>()) ? "true" : "false";
        break;
        case lui::object_type::TLIGHTUSERDATA:
            data = utils::string::va("%lld", buffer_->read<std::uint64_t>());
        break;
        case lui::object_type::TNUMBER:
            data = utils::string::va("%g", buffer_->read<float>());
        break;
        case lui::object_type::TSTRING:
            buffer_->read<std::uint64_t>();
            data = buffer_->read_string();
        break;
        default:
            DISASSEMBLER_ERROR("UNKNOWN CONSTANT TYPE");
        break;
    }

    func->constants.push_back(std::make_unique<lui::constant>(type, data));
}

void disassembler::disassemble_opcode(const lui::instruction_ptr& inst)
{
    switch(opcode(inst->op))
    {
        default:
            DISASSEMBLER_ERROR("Unhandled opcode %s", resolver::opcode_name(opcode(inst->op)).data());
        break;
    }
}

} // namespace IW6
