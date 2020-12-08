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
    this->buffer_ = std::make_unique<utils::byte_buffer>(data);

    
}

} // namespace IW6
