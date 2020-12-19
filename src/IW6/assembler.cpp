// Copyright 2020 xensik. All rights reserved.
//
// Use of this source code is governed by a GNU GPLv3 license
// that can be found in the LICENSE file.

#include "IW6.hpp"

namespace IW6
{

auto assembler::output() -> std::vector<std::uint8_t>
{
    std::vector<std::uint8_t> output;

    output.resize(output_->pos());
    std::memcpy(output.data(), output_->buffer().data(), output.size());

    return output;
}

void assembler::assemble(std::vector<std::uint8_t>& data)
{
    // TODO: make the text parser
}

void assembler::assemble(lui::file_ptr data)
{
    output_ = std::make_unique<utils::byte_buffer>(0x1000000);
    assemble_header();
    assemble_function(data->main);
    assemble_prototype();
}

void assembler::assemble_header()
{
    output_->write<std::uint32_t>(0x61754C1B); // signature
    output_->write<std::uint8_t>(0x51);     // lua_version
    output_->write<std::uint8_t>(0xD);      // format_version
    output_->write<std::uint8_t>(0x1);      // endianness
    output_->write<std::uint8_t>(0x4);      // size_of_int
    output_->write<std::uint8_t>(0x8);      // size_of_size_t
    output_->write<std::uint8_t>(0x4);      // size_of_intruction
    output_->write<std::uint8_t>(0x4);      // size_of_lua_number
    output_->write<std::uint8_t>(0x00);     // integral_flag
    output_->write<std::uint8_t>(0x03);     // build_flags
    output_->write<std::uint8_t>(0x00);     // referenced_mode
    output_->write<std::uint32_t>(0x0D);    // type_count

    static std::vector<lui::type_info> types = {
        {0, "TNIL"},
        {1, "TBOOLEAN"},
        {2, "TLIGHTUSERDATA"},
        {3, "TNUMBER"},
        {4, "TSTRING"},
        {5, "TTABLE"},
        {6, "TFUNCTION"},
        {7, "TUSERDATA"},
        {8, "TTHREAD"},
        {9, "TIFUNCTION"},
        {10, "TCFUNCTION"},
        {11, "TUI64"},
        {12, "TSTRUCT"},
    };

    for(auto type : types)
    {
        output_->write<std::uint32_t>(type.id);
        output_->write<std::uint32_t>(type.name.size() + 1);
        output_->write_c_string(type.name);
    }
}

void assembler::assemble_function(const lui::function& func)
{
    output_->write<std::uint32_t>(func.upval_count); //upvals
    output_->write<std::uint32_t>(func.param_count); // params
    output_->write<std::uint8_t>(func.vararg_flags);  // varargs
    output_->write<std::uint32_t>(func.register_count); // registers
    output_->write<std::uint64_t>(func.instruction_count); // instructions
    
    // padding
    int pad = 4 - (int)output_->pos() % 4;
    if (pad > 0 && pad < 4)
    {
        for(auto i = 0; i < pad; i++)
            output_->write<std::uint8_t>(0x5F);
    }

    // instructions
    for(auto i = 0; i < func.instruction_count; i++)
    {
        output_->write<std::uint32_t>(func.instructions.at(i)->value);
    }

    output_->write<std::uint32_t>(func.constant_count); // constants

    for(auto i = 0; i < func.constant_count; i++)
    {
        assemble_constant(func, i);
    }

    output_->write<std::uint32_t>(0); // debug

    output_->write<std::uint32_t>(func.sub_func_count); // sub_funcs

    for(auto i = 0; i < func.sub_func_count; i++)
    {
        assemble_function(func.sub_funcs.at(i));
    }
}

void assembler::assemble_constant(const lui::function& func, std::uint32_t index)
{
    auto kst = func.constants.at(index);

    output_->write<std::uint8_t>(std::uint8_t(kst.data_.type_));

    switch(kst.data_.type_)
    {
        case lui::data::t::NIL:
        break;
        case lui::data::t::BOOLEAN:
            output_->write<std::uint8_t>((kst.data_.value_ == "true") ? 1 : 0);
        break;
        /*case lui::data::t::LIGHTUSERDATA:
            value = utils::string::va("%lld", buffer_->read<std::uint64_t>());
        break;*/
        case lui::data::t::NUMBER:
            output_->write<float>(std::stof(kst.data_.value_));
        break;
        case lui::data::t::STRING:
            output_->write<std::uint64_t>(kst.data_.value_.size() + 1);
            output_->write_c_string(kst.data_.value_);
        break;
        default:
            DISASSEMBLER_ERROR("UNKNOWN CONSTANT TYPE");
        break;
    }
}

void assembler::assemble_prototype()
{
    output_->write<std::uint32_t>(1);
    output_->write<std::uint32_t>(0);
    output_->write<std::uint32_t>(0);
}

void assembler::assemble_instruction()
{
    // TODO: convert instruction fields and glue int32 value.
}

} // namespace IW6
