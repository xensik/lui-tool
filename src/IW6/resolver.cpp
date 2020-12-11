// Copyright 2020 xensik. All rights reserved.
//
// Use of this source code is governed by a GNU GPLv3 license
// that can be found in the LICENSE file.

#include "IW6.hpp"

namespace IW6
{

auto resolver::opcode_id(const std::string& name) -> opcode
{
    for (auto& opcode : opcode_map)
    {
        if (opcode.second == name)
        {
            return opcode.first;
        }
    }

    LOG_ERROR("Couldn't resolve opcode id for name '%s'!", name.data());
    return opcode::HKS_OPCODE_MAX;
}

auto resolver::opcode_name(opcode id) -> std::string
{
    const auto itr = opcode_map.find(id);

    if (itr != opcode_map.end())
    {
        return itr->second;
    }

    LOG_ERROR("Couldn't resolve opcode name for id '0x%hhX'!", id);
    return "";
}

std::unordered_map<opcode, std::string> resolver::opcode_map
{
    { opcode::HKS_OPCODE_GETFIELD, "GETFIELD" },
    { opcode::HKS_OPCODE_TEST, "TEST" },
    { opcode::HKS_OPCODE_CALL_I, "CALL_I" },
    { opcode::HKS_OPCODE_EQ, "EQ" },
    { opcode::HKS_OPCODE_EQ_BK, "EQ_BK" },
    { opcode::HKS_OPCODE_GETGLOBAL, "GETGLOBAL" },
    { opcode::HKS_OPCODE_MOVE, "MOVE" },
    { opcode::HKS_OPCODE_SELF, "SELF" },
    { opcode::HKS_OPCODE_RETURN, "RETURN" },
    { opcode::HKS_OPCODE_GETTABLE_S, "GETTABLE_S" },
    { opcode::HKS_OPCODE_GETTABLE, "GETTABLE" },
    { opcode::HKS_OPCODE_LOADBOOL, "LOADBOOL" },
    { opcode::HKS_OPCODE_TFORLOOP, "TFORLOOP" },
    { opcode::HKS_OPCODE_SETFIELD, "SETFIELD" },
    { opcode::HKS_OPCODE_SETTABLE_S, "SETTABLE_S" },
    { opcode::HKS_OPCODE_SETTABLE_S_BK, "SETTABLE_S_BK" },
    { opcode::HKS_OPCODE_SETTABLE, "SETTABLE" },
    { opcode::HKS_OPCODE_SETTABLE_BK, "SETTABLE_BK" },
    { opcode::HKS_OPCODE_TAILCALL_I, "TAILCALL_I" },
    { opcode::HKS_OPCODE_LOADK, "LOADK" },
    { opcode::HKS_OPCODE_LOADNIL, "LOADNIL" },
    { opcode::HKS_OPCODE_SETGLOBAL, "SETGLOBAL" },
    { opcode::HKS_OPCODE_JMP, "JMP" },
    { opcode::HKS_OPCODE_CALL, "CALL" },
    { opcode::HKS_OPCODE_TAILCALL, "TAILCALL" },
    { opcode::HKS_OPCODE_GETUPVAL, "GETUPVAL" },
    { opcode::HKS_OPCODE_SETUPVAL, "SETUPVAL" },
    { opcode::HKS_OPCODE_ADD, "ADD" },
    { opcode::HKS_OPCODE_ADD_BK, "ADD_BK" },
    { opcode::HKS_OPCODE_SUB, "SUB" },
    { opcode::HKS_OPCODE_SUB_BK, "SUB_BK" },
    { opcode::HKS_OPCODE_MUL, "MUL" },
    { opcode::HKS_OPCODE_MUL_BK, "MUL_BK" },
    { opcode::HKS_OPCODE_DIV, "DIV" },
    { opcode::HKS_OPCODE_DIV_BK, "DIV_BK" },
    { opcode::HKS_OPCODE_MOD, "MOD" },
    { opcode::HKS_OPCODE_MOD_BK, "MOD_BK" },
    { opcode::HKS_OPCODE_POW, "POW" },
    { opcode::HKS_OPCODE_POW_BK, "POW_BK" },
    { opcode::HKS_OPCODE_NEWTABLE, "NEWTABLE" },
    { opcode::HKS_OPCODE_UNM, "UNM" },
    { opcode::HKS_OPCODE_NOT, "NOT" },
    { opcode::HKS_OPCODE_LEN, "LEN" },
    { opcode::HKS_OPCODE_LT, "LT" },
    { opcode::HKS_OPCODE_LT_BK, "LT_BK" },
    { opcode::HKS_OPCODE_LE, "LE" },
    { opcode::HKS_OPCODE_LE_BK, "LE_BK" },
    { opcode::HKS_OPCODE_CONCAT, "CONCAT" },
    { opcode::HKS_OPCODE_TESTSET, "TESTSET" },
    { opcode::HKS_OPCODE_FORPREP, "FORPREP" },
    { opcode::HKS_OPCODE_FORLOOP, "FORLOOP" },
    { opcode::HKS_OPCODE_SETLIST, "SETLIST" },
    { opcode::HKS_OPCODE_CLOSE, "CLOSE" },
    { opcode::HKS_OPCODE_CLOSURE, "CLOSURE" },
    { opcode::HKS_OPCODE_VARARG, "VARARG" },
    { opcode::HKS_OPCODE_TAILCALL_I_R1, "TAILCALL_I_R1" },
    { opcode::HKS_OPCODE_CALL_I_R1, "CALL_I_R1" },
    { opcode::HKS_OPCODE_SETUPVAL_R1, "SETUPVAL_R1" },
    { opcode::HKS_OPCODE_TEST_R1, "TEST_R1" },
    { opcode::HKS_OPCODE_NOT_R1, "NOT_R1" },
    { opcode::HKS_OPCODE_GETFIELD_R1, "GETFIELD_R1" },
    { opcode::HKS_OPCODE_SETFIELD_R1, "SETFIELD_R1" },
    { opcode::HKS_OPCODE_DATA, "DATA" },
    { opcode::HKS_OPCODE_GETGLOBAL_MEM, "GETGLOBAL_MEM" },
};

} // namespace IW6
