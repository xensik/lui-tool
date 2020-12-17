// Copyright 2020 xensik. All rights reserved.
//
// Use of this source code is governed by a GNU GPLv3 license
// that can be found in the LICENSE file.

#ifndef _LUI_IW6_DECOMPILER_HPP_
#define _LUI_IW6_DECOMPILER_HPP_

namespace IW6
{

class decompiler : public lui::decompiler
{
    lui::file_ptr file_;
    lui::script_ptr script_;

    std::int32_t var_index;

public:
    auto output() -> std::vector<std::uint8_t>;
    void decompile(lui::file_ptr file);
    void decompile_function(lui::function& func);
    void decompile_instruction(lui::function& func,  std::uint32_t& index);
    auto decompile_call(lui::function& func, const lui::instruction_ptr& inst) -> lui::child;
    auto find_constant(const lui::function& func, std::int32_t index) -> lui::kst;
    void debug_print(const lui::function& func, const lui::instruction_ptr& inst);
    auto get_new_variable() -> std::string;
};

} // namespace IW6

#endif // _LUI_IW6_DECOMPILER_HPP_
