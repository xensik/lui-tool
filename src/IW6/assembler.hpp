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
public:
    auto output() -> std::vector<std::uint8_t>;
    void assemble(std::vector<std::uint8_t>& data);
    void assemble(lui::file_ptr data);
};

} // namespace IW6

#endif // _LUI_IW6_ASSEMBLER_HPP_
