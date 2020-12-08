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

public:
    auto output() -> std::vector<std::uint8_t>;
    void disassemble(std::vector<std::uint8_t>& data);

private:

};

} // namespace IW6

#endif // _LUI_IW6_DISASSEMBLER_HPP_
