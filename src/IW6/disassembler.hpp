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
    void disassemble(std::vector<std::uint8_t>& data);
    void disassemble_header();
    void disassemble_functions();
    void disassemble_prototype();
    void disassemble_function(const lui::function_ptr& func);
    void disassemble_instruction(const lui::function_ptr& func);
    void disassemble_constant(const lui::function_ptr& func);
    auto find_constant(const lui::function_ptr& func, std::int32_t index) -> lui::kst&;
    void print_function(const lui::function_ptr& func);
    void print_instruction(const lui::function_ptr& func, const lui::instruction_ptr& inst);
    void print_instruction_data(const lui::instruction_ptr& inst); 
};

} // namespace IW6

#endif // _LUI_IW6_DISASSEMBLER_HPP_
