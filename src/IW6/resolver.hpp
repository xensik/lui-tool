// Copyright 2020 xensik. All rights reserved.
//
// Use of this source code is governed by a GNU GPLv3 license
// that can be found in the LICENSE file.

#ifndef _LUI_IW6_RESOLVER_HPP_
#define _LUI_IW6_RESOLVER_HPP_

namespace IW6
{

enum class opcode : std::uint8_t;

class resolver
{
    static std::unordered_map<opcode, std::string> opcode_map;

public:
    static auto opcode_id(const std::string& name) -> opcode;
    static auto opcode_name(opcode id) -> std::string;
};

} // namespace IW6

#endif // _LUI_IW6_RESOLVER_HPP_
