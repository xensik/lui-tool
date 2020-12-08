// Copyright 2020 xensik. All rights reserved.
//
// Use of this source code is governed by a GNU GPLv3 license
// that can be found in the LICENSE file.

#ifndef _LUI_ASSEMBLER_HPP_
#define _LUI_ASSEMBLER_HPP_

namespace lui
{

class assembler
{
public:
    virtual auto output() -> std::vector<std::uint8_t> = 0;
    virtual void assemble(std::vector<std::uint8_t>& data) = 0;
};

} // namespace lui

#endif // _LUI_ASSEMBLER_HPP_
