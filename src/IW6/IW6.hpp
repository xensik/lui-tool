// Copyright 2020 xensik. All rights reserved.
//
// Use of this source code is governed by a GNU GPLv3 license
// that can be found in the LICENSE file.

#ifndef _LUI_IW6_HPP_
#define _LUI_IW6_HPP_

#include <utils.hpp>

#include "disassembler.hpp"
#include "resolver.hpp"

namespace IW6
{
/*=======================================================
Instruction format

    'OP'  : 7 bits
    'A'   : 8 bits
    'B'   : 8 bits
    'C'   : 9 bits
    'Bx'  : 17 bits ('B' and 'C' together)
    'sBx' : signed Bx
=======================================================*/

#define SIZE_A          8
#define SIZE_C          9
#define SIZE_B          8
#define SIZE_Bx         17
#define SIZE_OP         7

#define POS_A           0
#define POS_C           8
#define POS_B           17
#define POS_Bx          8
#define POS_OP          25

#define MASK_A  0x000000FF
#define MASK_C  0x0001FF00
#define MASK_B  0x01FE0000
#define MASK_Bx 0x01FFFF00
#define MASK_OP 0xFE000000

#define MAXARG_Bx        ((1 << SIZE_Bx) - 1)
#define MAXARG_sBx       (MAXARG_Bx >> 1)

#define GET_OPCODE(i)   ((int)(value & MASK_OP) >> POS_OP)
#define GETARG_A(i)     ((int)(value & MASK_A) >> POS_A)
#define GETARG_B(i)     ((int)(value & MASK_B) >> POS_B)
#define GETARG_C(i)    ((int)(value & MASK_C) >> POS_C)
#define GETARG_Bx(i)    ((int)(value & MASK_Bx) >> POS_Bx)
#define GETARG_sBx(i)   (GETARG_Bx(i) - MAXARG_sBx)

enum class opcode : std::uint8_t
{
    HKS_OPCODE_GETFIELD = 0x0,
    HKS_OPCODE_TEST = 0x1,
    HKS_OPCODE_CALL_I = 0x2,
    HKS_OPCODE_CALL_C = 0x3,
    HKS_OPCODE_EQ = 0x4,
    HKS_OPCODE_EQ_BK = 0x5,
    HKS_OPCODE_GETGLOBAL = 0x6,
    HKS_OPCODE_MOVE = 0x7,
    HKS_OPCODE_SELF = 0x8,
    HKS_OPCODE_RETURN = 0x9,
    HKS_OPCODE_GETTABLE_S = 0xA,
    HKS_OPCODE_GETTABLE_N = 0xB,
    HKS_OPCODE_GETTABLE = 0xC,
    HKS_OPCODE_LOADBOOL = 0xD,
    HKS_OPCODE_TFORLOOP = 0xE,
    HKS_OPCODE_SETFIELD = 0xF,
    HKS_OPCODE_SETTABLE_S = 0x10,
    HKS_OPCODE_SETTABLE_S_BK = 0x11,
    HKS_OPCODE_SETTABLE_N = 0x12,
    HKS_OPCODE_SETTABLE_N_BK = 0x13,
    HKS_OPCODE_SETTABLE = 0x14,
    HKS_OPCODE_SETTABLE_BK = 0x15,
    HKS_OPCODE_TAILCALL_I = 0x16,
    HKS_OPCODE_TAILCALL_C = 0x17,
    HKS_OPCODE_TAILCALL_M = 0x18,
    HKS_OPCODE_LOADK = 0x19,
    HKS_OPCODE_LOADNIL = 0x1A,
    HKS_OPCODE_SETGLOBAL = 0x1B,
    HKS_OPCODE_JMP = 0x1C,
    HKS_OPCODE_CALL_M = 0x1D,
    HKS_OPCODE_CALL = 0x1E,
    HKS_OPCODE_INTRINSIC_INDEX = 0x1F,
    HKS_OPCODE_INTRINSIC_NEWINDEX = 0x20,
    HKS_OPCODE_INTRINSIC_SELF = 0x21,
    HKS_OPCODE_INTRINSIC_INDEX_LITERAL = 0x22,
    HKS_OPCODE_INTRINSIC_NEWINDEX_LITERAL = 0x23,
    HKS_OPCODE_INTRINSIC_SELF_LITERAL = 0x24,
    HKS_OPCODE_TAILCALL = 0x25,
    HKS_OPCODE_GETUPVAL = 0x26,
    HKS_OPCODE_SETUPVAL = 0x27,
    HKS_OPCODE_ADD = 0x28,
    HKS_OPCODE_ADD_BK = 0x29,
    HKS_OPCODE_SUB = 0x2A,
    HKS_OPCODE_SUB_BK = 0x2B,
    HKS_OPCODE_MUL = 0x2C,
    HKS_OPCODE_MUL_BK = 0x2D,
    HKS_OPCODE_DIV = 0x2E,
    HKS_OPCODE_DIV_BK = 0x2F,
    HKS_OPCODE_MOD = 0x30,
    HKS_OPCODE_MOD_BK = 0x31,
    HKS_OPCODE_POW = 0x32,
    HKS_OPCODE_POW_BK = 0x33,
    HKS_OPCODE_NEWTABLE = 0x34,
    HKS_OPCODE_UNM = 0x35,
    HKS_OPCODE_NOT = 0x36,
    HKS_OPCODE_LEN = 0x37,
    HKS_OPCODE_LT = 0x38,
    HKS_OPCODE_LT_BK = 0x39,
    HKS_OPCODE_LE = 0x3A,
    HKS_OPCODE_LE_BK = 0x3B,
    HKS_OPCODE_CONCAT = 0x3C,
    HKS_OPCODE_TESTSET = 0x3D,
    HKS_OPCODE_FORPREP = 0x3E,
    HKS_OPCODE_FORLOOP = 0x3F,
    HKS_OPCODE_SETLIST = 0x40,
    HKS_OPCODE_CLOSE = 0x41,
    HKS_OPCODE_CLOSURE = 0x42,
    HKS_OPCODE_VARARG = 0x43,
    HKS_OPCODE_TAILCALL_I_R1 = 0x44,
    HKS_OPCODE_CALL_I_R1 = 0x45,
    HKS_OPCODE_SETUPVAL_R1 = 0x46,
    HKS_OPCODE_TEST_R1 = 0x47,
    HKS_OPCODE_NOT_R1 = 0x48,
    HKS_OPCODE_GETFIELD_R1 = 0x49,
    HKS_OPCODE_SETFIELD_R1 = 0x4A,
    HKS_OPCODE_NEWSTRUCT = 0x4B,
    HKS_OPCODE_DATA = 0x4C,
    HKS_OPCODE_SETSLOTN = 0x4D,
    HKS_OPCODE_SETSLOTI = 0x4E,
    HKS_OPCODE_SETSLOT = 0x4F,
    HKS_OPCODE_SETSLOTS = 0x50,
    HKS_OPCODE_SETSLOTMT = 0x51,
    HKS_OPCODE_CHECKTYPE = 0x52,
    HKS_OPCODE_CHECKTYPES = 0x53,
    HKS_OPCODE_GETSLOT = 0x54,
    HKS_OPCODE_GETSLOTMT = 0x55,
    HKS_OPCODE_SELFSLOT = 0x56,
    HKS_OPCODE_SELFSLOTMT = 0x57,
    HKS_OPCODE_GETFIELD_MM = 0x58,
    HKS_OPCODE_CHECKTYPE_D = 0x59,
    HKS_OPCODE_GETSLOT_D = 0x5A,
    HKS_OPCODE_GETGLOBAL_MEM = 0x5B,
    HKS_OPCODE_DELETE = 0x5C,
    HKS_OPCODE_DELETE_BK = 0x5D,
    HKS_OPCODE_MAX = 0x5E,
};

} // namespace IW6

#endif // _LUI_IW6_HPP_
