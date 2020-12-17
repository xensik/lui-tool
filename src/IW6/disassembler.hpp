// Copyright 2020 xensik. All rights reserved.
//
// Use of this source code is governed by a GNU GPLv3 license
// that can be found in the LICENSE file.

#ifndef _LUI_IW6_DISASSEMBLER_HPP_
#define _LUI_IW6_DISASSEMBLER_HPP_

namespace IW6
{

class disassembler : public lui::disassembler
{
    utils::byte_buffer_ptr buffer_;
    utils::byte_buffer_ptr output_;
    lui::file_ptr file_;
    static int tabsize_;

public:
    auto output() -> std::vector<std::uint8_t>;
    auto output_d() -> lui::file_ptr;
    void disassemble(std::vector<std::uint8_t>& data);
    void disassemble_header();
    void disassemble_functions();
    void disassemble_prototype();
    void disassemble_function(lui::function& func);
    void disassemble_instruction(lui::function& func);
    void disassemble_constant(lui::function& func);
    void disassemble_fields(lui::function& func, std::uint32_t index);
    auto find_constant(const lui::function& func, std::int32_t index) -> lui::kst;
    void print_function(const lui::function& func);
};

} // namespace IW6

#endif // _LUI_IW6_DISASSEMBLER_HPP_
