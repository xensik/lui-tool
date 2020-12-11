// Copyright 2020 xensik. All rights reserved.
//
// Use of this source code is governed by a GNU GPLv3 license
// that can be found in the LICENSE file.

#include "IW6.hpp"

namespace IW6
{

int disassembler::tabsize_ = 0;

std::string indented(std::uint32_t indent)
{
    static char buff[100];
    snprintf(buff, sizeof(buff), "%*s", indent, "");
    return std::string(buff);
}

auto disassembler::output() -> std::vector<std::uint8_t>
{
    std::vector<std::uint8_t> output;

    output.resize(output_->pos());
    std::memcpy(output.data(), output_->buffer().data(), output.size());
    
    return output;
}

void disassembler::disassemble(std::vector<std::uint8_t>& data)
{
    output_ = std::make_unique<utils::byte_buffer>(0x1000000);
    buffer_ = std::make_unique<utils::byte_buffer>(data);
    file_ = std::make_unique<lui::file>();

    this->disassemble_header();
    this->disassemble_functions();
}

void disassembler::disassemble_header()
{
    file_->header.magic = buffer_->read<std::uint32_t>();
    file_->header.lua_version = buffer_->read<std::uint8_t>();
    file_->header.format_version = buffer_->read<std::uint8_t>();
    file_->header.endianness = buffer_->read<std::uint8_t>();
    file_->header.size_of_int = buffer_->read<std::uint8_t>();
    file_->header.size_of_size_t = buffer_->read<std::uint8_t>();
    file_->header.size_of_intruction = buffer_->read<std::uint8_t>();
    file_->header.size_of_lua_number = buffer_->read<std::uint8_t>();
    file_->header.integral_flag = buffer_->read<std::uint8_t>();
    file_->header.build_flags = buffer_->read<std::uint8_t>();
    file_->header.referenced_mode = buffer_->read<std::uint8_t>();
    file_->header.type_count = buffer_->read<std::uint32_t>();

    for(auto i = 0; i < file_->header.type_count; i++)
    {
        auto id = buffer_->read<std::uint32_t>();
        auto length = buffer_->read<std::uint32_t>();
        auto name = buffer_->read_string();

        file_->header.types.push_back(lui::type_info(id, name));
    }
}

void disassembler::disassemble_functions()
{
    file_->main = std::make_unique<lui::function>();
    this->disassemble_function(file_->main);
    this->disassemble_prototype();
}

void disassembler::disassemble_prototype()
{
    auto head_mismatch = buffer_->read<std::uint32_t>();
    auto unk1 = buffer_->read<std::uint32_t>();
    auto unk2 = buffer_->read<std::uint32_t>();
}

void disassembler::disassemble_function(const lui::function_ptr& func)
{
    auto fun_index =  buffer_->pos();

    func->upval_count = buffer_->read<std::uint32_t>(); // always 0
    func->param_count = buffer_->read<std::uint32_t>();
    func->vararg_flags = buffer_->read<std::uint8_t>();
    func->register_count = buffer_->read<std::uint32_t>();
    func->instruction_count = buffer_->read<std::uint64_t>();

    // always a byte here 0x5F
    int pad = 4 - (int)buffer_->pos() % 4;
    if (pad > 0 && pad < 4) buffer_->seek(pad);

    for (int i = 0; i < func->instruction_count; i++)
    {
        this->disassemble_instruction(func);
    }

    func->constant_count = buffer_->read<std::uint32_t>();

    for (int i = 0; i < func->constant_count; i++)
    {
        this->disassemble_constant(func);
    }

    func->debug = buffer_->read<std::uint32_t>();
    func->sub_func_count = buffer_->read<std::uint32_t>();

    auto info = utils::string::va("flag: %d, params: %d, upvals: %d, registers: %d, instructions: %d, constants: %d", func->vararg_flags, func->param_count, func->upval_count, func->register_count, func->instruction_count, func->constant_count);
    output_->write_string(utils::string::va("\n%ssub_%X [%s]\n", indented(tabsize_).data(), fun_index, info.data()));
    tabsize_ += 4;
    for (int i = 0; i < func->instruction_count; i++)
    {
        this->disassemble_opcode(func, func->instructions.at(i));
    }
    
    // go to subfunctions
    for (int i = 0; i < func->sub_func_count; i++)
    {
        func->sub_funcs.push_back(std::make_unique<lui::function>());
        this->disassemble_function(func->sub_funcs.back());
    }

    tabsize_ -= 4;
    output_->write_string(utils::string::va("%send_%X\n", indented(tabsize_).data(), fun_index));
}

void disassembler::disassemble_instruction(const lui::function_ptr& func)
{
    auto index = buffer_->pos();
    auto value = buffer_->read<std::uint32_t>();

    auto OP = (std::uint8_t)((value & MASK_OP) >> POS_OP);
    auto A  = (std::int32_t)((value & MASK_A) >> POS_A);
    auto C  = (std::int32_t)((value & MASK_C) >> POS_C);
    auto B  = (std::int32_t)((value & MASK_B) >> POS_B);
    auto Bx = (std::int32_t)((value & MASK_Bx) >> POS_Bx);
    auto sBx = (std::int32_t)(Bx - 0x10000 + 1);
    bool sZero = false;
    if (C >= 0x100) { C -= 0x100; sZero = true; }

    auto inst = std::make_unique<lui::instruction>(index, value, OP, A, B, C, Bx, sBx, sZero);
    func->instructions.push_back(std::move(inst));
}

void disassembler::disassemble_constant(const lui::function_ptr& func)
{
    std::string data;
    auto type = lui::data_type(buffer_->read<std::uint8_t>());

    switch(type)
    {
        case lui::data_type::TNIL:
            data = "nil";
        break;
        case lui::data_type::TBOOLEAN:
            data = ((bool)buffer_->read<std::uint8_t>()) ? "true" : "false";
        break;
        case lui::data_type::TLIGHTUSERDATA:
            data = utils::string::va("%lld", buffer_->read<std::uint64_t>());
        break;
        case lui::data_type::TNUMBER:
            data = utils::string::va("%g", buffer_->read<float>());
        break;
        case lui::data_type::TSTRING:
            buffer_->read<std::uint64_t>();
            data = "\"" + buffer_->read_string() + "\"";
        break;
        default:
            DISASSEMBLER_ERROR("UNKNOWN CONSTANT TYPE");
        break;
    }

    func->constants.push_back(std::make_unique<lui::constant>(type, data));
}

void disassembler::disassemble_opcode(const lui::function_ptr& func, const lui::instruction_ptr& inst)
{
    auto op = opcode(inst->OP);

#define RK(f, c, z) (c < 0 || z) ? std::string(find_constant(f, c)) : std::string(lui::reg(c));
                                                    // -------------------------------------
    switch(op)                                      // args    description
    {                                               // -------------------------------------
    case opcode::HKS_OPCODE_GETFIELD:               // A B C   R(A) := R(B)[K(C)]
        inst->mode = lui::instruction_mode::ABC;
        inst->data += std::string(lui::reg(inst->A)) + ", ";
        inst->data += std::string(lui::reg(inst->B)) + ", ";
        inst->data += std::string(find_constant(func, inst->C));
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_TEST:                   // A C     if not (R(A) <=> C) then pc++
        inst->mode = lui::instruction_mode::ABC;
        inst->data += std::string(lui::reg(inst->A)) + ", ";
        inst->data += utils::string::va("BOOL(%d)", inst->C); // && use 0, || use 1
        print_instruction(inst);                   
        break;
    case opcode::HKS_OPCODE_CALL_I:                 // A B C   ?
        inst->mode = lui::instruction_mode::ABC;
        inst->data += std::string(lui::reg(inst->A)) + ", "; // call pointer
        inst->data += utils::string::va("ARG(%d)", inst->B) + ", "; // last_arg + 1
        inst->data += utils::string::va("RET(%d)", inst->C); // last_ret + 1
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_EQ:                     // A B C   if ((R(B) == RK(C)) ~= A) then PC++
        inst->mode = lui::instruction_mode::ABC;
        inst->data += std::string(lui::reg(inst->A)) + ", ";
        inst->data += std::string(lui::reg(inst->B)) + ", ";
        inst->data += RK(func, inst->C, inst->sZero);
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_EQ_BK:                  // A B C   if ((K(B) == R(C)) ~= A) then PC++
        inst->mode = lui::instruction_mode::ABC;
        inst->data += std::string(lui::reg(inst->A)) + ", ";
        inst->data += std::string(find_constant(func, inst->B)) + ", ";
        inst->data += std::string(lui::reg(inst->C));
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_GETGLOBAL:              // A Bx    R(A) := Gbl[Kst(Bx)] 
        inst->mode = lui::instruction_mode::ABx;
        inst->data += std::string(lui::reg(inst->A)) + ", ";
        inst->data += std::string(find_constant(func, inst->Bx));
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_MOVE:                   // A B     R(A) := R(B)
        inst->mode = lui::instruction_mode::ABC;
        inst->data += std::string(lui::reg(inst->A)) + ", ";
        inst->data += std::string(lui::reg(inst->B));
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_SELF:                   // A B C   R(A+1) := R(B); R(A) := R(B)[RK(C)]
        inst->mode = lui::instruction_mode::ABC;
        inst->data += std::string(lui::reg(inst->A)) + ", ";
        inst->data += std::string(lui::reg(inst->B)) + ", ";
        inst->data += RK(func, inst->C, inst->sZero);
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_RETURN:                 // A B    return R(A), ... ,R(A+B-2)
        inst->mode = lui::instruction_mode::ABC;
        inst->data += std::string(lui::reg(inst->A)) + ", "; // if B == 1, no rets. B == 0, R(A) to stack top
        inst->data += utils::string::va("OPT(%d)", inst->B); // if B >= 2,  (B-1) returns
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_GETTABLE_S:             // A B C   R(A) := R(B)[RK(C)]
        inst->mode = lui::instruction_mode::ABC;
        inst->data += std::string(lui::reg(inst->A)) + ", ";
        inst->data += std::string(lui::reg(inst->B)) + ", ";
        inst->data += RK(func, inst->C, inst->sZero);
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_GETTABLE:               // A B C   R(A) := R(B)[RK(C)]
        inst->mode = lui::instruction_mode::ABC;
        inst->data += std::string(lui::reg(inst->A)) + ", ";
        inst->data += std::string(lui::reg(inst->B)) + ", ";
        inst->data += RK(func, inst->C, inst->sZero);
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_LOADBOOL:               // A B C   R(A) := (Bool)B; if (C) pc++
        inst->mode = lui::instruction_mode::ABC;
        inst->data += std::string(lui::reg(inst->A)) + ", ";
        inst->data += utils::string::va("BOOL(%d)", inst->B) + ", ";
        inst->data += utils::string::va("BOOL(%d)", inst->C);
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_TFORLOOP:               //
        inst->mode = lui::instruction_mode::ABC;
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_SETFIELD:               // A B C   R(A)[K(B)] := RK(C)
        inst->mode = lui::instruction_mode::ABC;
        inst->data += std::string(lui::reg(inst->A)) + ", ";
        inst->data += std::string(find_constant(func, inst->B)) + ", ";
        inst->data += RK(func, inst->C, inst->sZero);
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_SETTABLE_S:             // A B C   R(A)[R(B)] := RK(C)
        inst->mode = lui::instruction_mode::ABC;
        inst->data += std::string(lui::reg(inst->A)) + ", ";
        inst->data += std::string(lui::reg(inst->B)) + ", ";
        inst->data += RK(func, inst->C, inst->sZero);
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_SETTABLE_S_BK:          // A B C   R(A)[K(B)] := RK(C)
        inst->mode = lui::instruction_mode::ABC;
        inst->data += std::string(lui::reg(inst->A)) + ", ";
        inst->data += std::string(find_constant(func, inst->B)) + ", ";
        inst->data += RK(func, inst->C, inst->sZero);
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_SETTABLE:               // A B C   R(A)[R(B)] := RK(C)
        inst->mode = lui::instruction_mode::ABC;
        inst->data += std::string(lui::reg(inst->A)) + ", ";
        inst->data += std::string(lui::reg(inst->B)) + ", ";
        inst->data += RK(func, inst->C, inst->sZero);
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_SETTABLE_BK:            // A B C   R(A)[K(B)] := RK(C)
        inst->mode = lui::instruction_mode::ABC;
        inst->data += std::string(lui::reg(inst->A)) + ", ";
        inst->data += std::string(find_constant(func, inst->B)) + ", ";
        inst->data += RK(func, inst->C, inst->sZero);
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_TAILCALL_I:             // A B C   return R(A)(R(A+1), ... ,R(A+B-1))
        inst->mode = lui::instruction_mode::ABC;
        inst->data += std::string(lui::reg(inst->A)) + ", "; // call pointer
        inst->data += utils::string::va("ARG(%d)", inst->B); // last_arg + 1
        print_instruction(inst);                             // C always 0
        break;
    case opcode::HKS_OPCODE_LOADK:                  // A Bx    R(A) := Kst(Bx)
        inst->mode = lui::instruction_mode::ABx;
        inst->data += std::string(lui::reg(inst->A)) + ", ";
        inst->data += std::string(find_constant(func, inst->Bx));print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_LOADNIL:                // A B     R(A) := ... := R(B) := nil
        inst->mode = lui::instruction_mode::ABC;
        inst->data += utils::string::va("R(%d .. %d)", inst->A, inst->B);
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_SETGLOBAL:              // A Bx    Gbl[Kst(Bx)] := R(A)
        inst->mode = lui::instruction_mode::ABx;
        inst->data += std::string(lui::reg(inst->A)) + ", ";
        inst->data += std::string(find_constant(func, inst->Bx));
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_JMP:                    // sBx      pc += sBx
        inst->mode = lui::instruction_mode::AsBx;
        inst->data += utils::string::va("PC(%d)", inst->sBx);
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_CALL:                   // A B C   ?
        inst->mode = lui::instruction_mode::ABC;
        inst->data += std::string(lui::reg(inst->A)) + ", "; // call pointer
        inst->data += utils::string::va("ARG(%d)", inst->B) + ", "; // last_arg + 1
        inst->data += utils::string::va("RET(%d)", inst->C) + ", "; // last_ret + 1
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_TAILCALL:               // A B C   return R(A)(R(A+1), ... ,R(A+B-1))
        inst->mode = lui::instruction_mode::ABC;
        inst->data += std::string(lui::reg(inst->A)) + ", "; // call pointer
        inst->data += utils::string::va("ARG(%d)", inst->B); // last_arg + 1
        print_instruction(inst);                             // C always 0
        break;
    case opcode::HKS_OPCODE_GETUPVAL:               // A B      R(A) := UpValue[B]
        inst->mode = lui::instruction_mode::ABC;
        inst->data += std::string(lui::reg(inst->A)) + ", ";
        inst->data += utils::string::va("UPVAL(%d)",inst->B);
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_SETUPVAL:               // A B      UpValue[B] := R(A)
        inst->mode = lui::instruction_mode::ABC;
        inst->data += std::string(lui::reg(inst->A)) + ", ";
        inst->data += utils::string::va("UPVAL(%d)",inst->B);
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_ADD:                    // A B C   R(A) := R(B) + RK(C)
        inst->mode = lui::instruction_mode::ABC;
        inst->data += std::string(lui::reg(inst->A)) + ", ";
        inst->data += std::string(lui::reg(inst->B)) + ", ";
        inst->data += RK(func, inst->C, inst->sZero);
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_ADD_BK:                 // A B C   R(A) := K(B) + R(C)
        inst->mode = lui::instruction_mode::ABC;
        inst->data += std::string(lui::reg(inst->A)) + ", ";
        inst->data += std::string(find_constant(func, inst->B));
        inst->data += ", " + std::string(lui::reg(inst->C));
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_SUB:                    //  A B C   R(A) := R(B) – RK(C)
        inst->mode = lui::instruction_mode::ABC;
        inst->data += std::string(lui::reg(inst->A)) + ", ";
        inst->data += std::string(lui::reg(inst->B)) + ", ";
        inst->data += RK(func, inst->C, inst->sZero);
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_SUB_BK:                 //  A B C   R(A) := K(B) – R(C)
        inst->mode = lui::instruction_mode::ABC;
        inst->data += std::string(lui::reg(inst->A)) + ", ";
        inst->data += std::string(find_constant(func, inst->B));
        inst->data += ", " + std::string(lui::reg(inst->C));
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_MUL:                    //  A B C   R(A) := R(B) * RK(C)
        inst->mode = lui::instruction_mode::ABC;
        inst->data += std::string(lui::reg(inst->A)) + ", ";
        inst->data += std::string(lui::reg(inst->B)) + ", ";
        inst->data += RK(func, inst->C, inst->sZero);
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_MUL_BK:                 //  A B C   R(A) := K(B) * R(C)
        inst->mode = lui::instruction_mode::ABC;
        inst->data += std::string(lui::reg(inst->A)) + ", ";
        inst->data += std::string(find_constant(func, inst->B));
        inst->data += ", " + std::string(lui::reg(inst->C));
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_DIV:                    // A B C   R(A) := R(B) / RK(C)
        inst->mode = lui::instruction_mode::ABC;
        inst->data += std::string(lui::reg(inst->A)) + ", ";
        inst->data += std::string(lui::reg(inst->B)) + ", ";
        inst->data += RK(func, inst->C, inst->sZero);
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_DIV_BK:                 // A B C   R(A) := K(B) / R(C)
        inst->mode = lui::instruction_mode::ABC;
        inst->data += std::string(lui::reg(inst->A)) + ", ";
        inst->data += std::string(find_constant(func, inst->B));
        inst->data += ", " + std::string(lui::reg(inst->C));
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_MOD:                    // A B C   R(A) := R(B) % RK(C)
        inst->mode = lui::instruction_mode::ABC;
        inst->data += std::string(lui::reg(inst->A)) + ", ";
        inst->data += std::string(lui::reg(inst->B)) + ", ";
        inst->data += RK(func, inst->C, inst->sZero);
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_MOD_BK:                 // A B C   R(A) := K(B) % R(C)
        inst->mode = lui::instruction_mode::ABC;
        inst->data += std::string(lui::reg(inst->A)) + ", ";
        inst->data += std::string(find_constant(func, inst->B));
        inst->data += ", " + std::string(lui::reg(inst->C));
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_POW:                    // A B C   R(A) := R(B) ^ RK(C)
        inst->mode = lui::instruction_mode::ABC;
        inst->data += std::string(lui::reg(inst->A)) + ", ";
        inst->data += std::string(lui::reg(inst->B)) + ", ";
        inst->data += RK(func, inst->C, inst->sZero);
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_POW_BK:                 // A B C   R(A) := K(B) ^ R(C)
        inst->mode = lui::instruction_mode::ABC;
        inst->data += std::string(lui::reg(inst->A)) + ", ";
        inst->data += std::string(find_constant(func, inst->B));
        inst->data += ", " + std::string(lui::reg(inst->C));
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_NEWTABLE:               // A B C   R(A) := array=B hash=C
        inst->mode = lui::instruction_mode::ABC;
        inst->data += std::string(lui::reg(inst->A)) + ", ";
        inst->data += utils::string::va("ARRAY(%d), ", inst->B);
        inst->data += utils::string::va("HASH(%d)", inst->C);
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_UNM:                    // A B     R(A) := -R(B)
        inst->mode = lui::instruction_mode::ABC;
        inst->data += std::string(lui::reg(inst->A)) + ", ";
        inst->data += std::string(lui::reg(inst->B));
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_NOT:                    // A B     R(A) := not R(B)
        inst->mode = lui::instruction_mode::ABC;
        inst->data += std::string(lui::reg(inst->A)) + ", ";
        inst->data += std::string(lui::reg(inst->B));
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_LEN:                    // A B     R(A) := length of R(B)
        inst->mode = lui::instruction_mode::ABC;
        inst->data += std::string(lui::reg(inst->A)) + ", ";
        inst->data += std::string(lui::reg(inst->B));
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_LT:                     // A B C   if ((R(B) < RK(C)) ~= A) then PC++
        inst->mode = lui::instruction_mode::ABC;
        inst->data += std::string(lui::reg(inst->A)) + ", ";
        inst->data += std::string(lui::reg(inst->B)) + ", ";
        inst->data += RK(func, inst->C, inst->sZero);
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_LT_BK:                  // A B C   if ((K(B) < R(C)) ~= A) then PC++
        inst->mode = lui::instruction_mode::ABC;
        inst->data += std::string(lui::reg(inst->A)) + ", ";
        inst->data += std::string(find_constant(func, inst->B)) + ", ";
        inst->data += std::string(lui::reg(inst->C));
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_LE:                     //  A B C if ((R(B) <= RK(C)) ~= A) then PC++
        inst->mode = lui::instruction_mode::ABC;
        inst->data += std::string(lui::reg(inst->A)) + ", ";
        inst->data += std::string(lui::reg(inst->B)) + ", ";
        inst->data += RK(func, inst->C, inst->sZero);
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_LE_BK:                  //  A B C if ((K(B) <= R(C)) ~= A) then PC++
        inst->mode = lui::instruction_mode::ABC;
        inst->data += std::string(lui::reg(inst->A)) + ", ";
        inst->data += std::string(find_constant(func, inst->B)) + ", ";
        inst->data += std::string(lui::reg(inst->C));
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_CONCAT:                 // A B C   R(A) := R(B).. ... ..R(C)
        inst->mode = lui::instruction_mode::ABC;
        inst->data += std::string(lui::reg(inst->A)) + ", ";
        inst->data += utils::string::va("R(%d .. %d)", inst->B, inst->C);
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_TESTSET:                // A B C   if (R(B) <=> C) then R(A) := R(B) else pc++
        inst->mode = lui::instruction_mode::ABC;
        inst->data += std::string(lui::reg(inst->A)) + ", ";
        inst->data += std::string(lui::reg(inst->B)) + ", ";
        inst->data += utils::string::va("BOOL(%d)", inst->C); // && use 0, || use 1
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_FORPREP:
        inst->mode = lui::instruction_mode::AsBx;
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_FORLOOP:
        inst->mode = lui::instruction_mode::AsBx;
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_SETLIST:                // A B C   R(A)[(C-1)*FPF+i] := R(A+i), 1 <= i <= B
        inst->mode = lui::instruction_mode::ABC;
        inst->data += std::string(lui::reg(inst->A)) + ", ";
        inst->data += utils::string::va("R(%d .. %d)", inst->C, inst->B); // regs(C, B), for B < 50
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_CLOSE:                  // A       close all variables in the stack up to (>=) R(A)
        inst->mode = lui::instruction_mode::ABC;
        inst->data += std::string(lui::reg(inst->A));
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_CLOSURE:                // A Bx    R(A) := closure(KPROTO[Bx], R(A), ... ,R(A+n))
        inst->mode = lui::instruction_mode::ABx;
        inst->data += std::string(lui::reg(inst->A)) + ", ";
        inst->data += utils::string::va("SUB_FUNC(%d)", inst->Bx);
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_VARARG:                 // A B     R(A), R(A+1), ..., R(A+B-1) = vararg
        inst->mode = lui::instruction_mode::ABC;
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_TAILCALL_I_R1:          // A B C   return R(A)(R(A+1), ... ,R(A+B-1))
        inst->mode = lui::instruction_mode::ABC;
        inst->data += std::string(lui::reg(inst->A)) + ", "; // call pointer
        inst->data += utils::string::va("ARG(%d)", inst->B); // last_arg + 1
        print_instruction(inst);                             // C always 0
        break;
    case opcode::HKS_OPCODE_CALL_I_R1:              // A B C   ?
        inst->mode = lui::instruction_mode::ABC;
        inst->data += std::string(lui::reg(inst->A)) + ", "; // call pointer
        inst->data += utils::string::va("ARG(%d)", inst->B) + ", "; // last_arg + 1
        inst->data += utils::string::va("RET(%d)", inst->C); // last_ret + 1
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_SETUPVAL_R1:            // A B      UpValue[B] := R(A)
        inst->mode = lui::instruction_mode::ABC;
        inst->data += std::string(lui::reg(inst->A)) + ", ";
        inst->data += utils::string::va("UPVAL(%d)",inst->B);
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_TEST_R1:                // A C     if not (R(A) <=> C) then pc++
        inst->mode = lui::instruction_mode::ABC;
        inst->data += std::string(lui::reg(inst->A)) + ", ";
        inst->data += utils::string::va("BOOL(%d)", inst->C); // && use 0, || use 1
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_NOT_R1:                 // A B     R(A) := not R(B)
        inst->mode = lui::instruction_mode::ABC;
        inst->data += std::string(lui::reg(inst->A)) + ", ";
        inst->data += std::string(lui::reg(inst->B));
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_GETFIELD_R1:            // A B C   R(A) = R(B)[K(C)]
        inst->mode = lui::instruction_mode::ABC;
        inst->data += std::string(lui::reg(inst->A)) + ", ";
        inst->data += std::string(lui::reg(inst->B)) + ", ";
        inst->data += std::string(find_constant(func, inst->C));
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_SETFIELD_R1:            // A B C   R(A)[K(B)] = RK(C)
        inst->mode = lui::instruction_mode::ABC;
        inst->data += std::string(lui::reg(inst->A)) + ", ";
        inst->data += std::string(find_constant(func, inst->B));
        inst->data += RK(func, inst->C, inst->sZero);
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_DATA:                   // A Bx    ????
// This not exits in vm executer!!! is parsed before??
// if A == 00, Bx is 0 
// if A == 20, Bx is a constant(nil)
//      inst->mode = lui::instruction_mode::ABx;
        break;
    case opcode::HKS_OPCODE_GETGLOBAL_MEM:          // A Bx    R(A) := Gbl[Kst(Bx)]
        inst->mode = lui::instruction_mode::ABx;
        inst->data += std::string(lui::reg(inst->A)) + ", ";
        inst->data += std::string(find_constant(func, inst->Bx));
        print_instruction(inst);
        break;
        default:
            DISASSEMBLER_ERROR("Unhandled opcode %s", resolver::opcode_name(opcode(inst->OP)).data());
        break;
    }
}

auto disassembler::find_constant(const lui::function_ptr& func, std::int32_t index) -> lui::constant
{
    if (index < 0) index = -index;

    auto result = *func->constants.at(index);

    return result;
}

void disassembler::print_instruction(const lui::instruction_ptr& inst)
{
    if(inst->mode == lui::instruction_mode::ABC)   
    {
        output_->write_string(utils::string::va("%s%-14s %02X %02X %02X ; %s\n", indented(tabsize_).data(), resolver::opcode_name(opcode(inst->OP)).data(), inst->A, inst->B, inst->C, inst->data.data()));   
    }
    else if(inst->mode == lui::instruction_mode::ABx)
    {
        output_->write_string(utils::string::va("%s%-14s %02X %02X    ; %s\n", indented(tabsize_).data(), resolver::opcode_name(opcode(inst->OP)).data(), inst->A, inst->Bx, inst->data.data()));   
    }
    else if(inst->mode == lui::instruction_mode::AsBx)
    {
        output_->write_string(utils::string::va("%s%-14s %02X %02X    ; %s\n", indented(tabsize_).data(), resolver::opcode_name(opcode(inst->OP)).data(), inst->A, inst->sBx, inst->data.data()));   
    
    }
    else
    {
        //DISASSEMBLER_ERROR("INSTRUCTION MODE UNSET");
    }
}

} // namespace IW6
