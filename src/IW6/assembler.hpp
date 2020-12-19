// Copyright 2020 xensik. All rights reserved.
//
// Use of this source code is governed by a GNU GPLv3 license
// that can be found in the LICENSE file.

#ifndef _LUI_IW6_ASSEMBLER_HPP_
#define _LUI_IW6_ASSEMBLER_HPP_

namespace IW6
{

class assembler : public lui::assembler
{
    utils::byte_buffer_ptr output_;

public:
    auto output() -> std::vector<std::uint8_t>;
    void assemble(std::vector<std::uint8_t>& data);
    void assemble(lui::file_ptr data);

private:
    void assemble_header();
    void assemble_function(const lui::function& func);
    void assemble_constant(const lui::function& func, std::uint32_t index);
    void assemble_prototype();

    void assemble_instruction();
};

} // namespace IW6

#endif // _LUI_IW6_ASSEMBLER_HPP_
