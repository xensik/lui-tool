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
    
    LOG_INFO("disasembled.");
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

        file_->header.types.push_back(lui::typeinfo(id, name));
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

    func->upval_count = buffer_->read<std::uint32_t>();
    func->param_count = buffer_->read<std::uint32_t>();
    func->flags = buffer_->read<std::uint8_t>();
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

    output_->write_string(utils::string::va("\n%ssub_%X [instructions: %d, constants: %d]\n", indented(tabsize_).data(), fun_index, func->instruction_count, func->constant_count));
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
    auto type = lui::object_type(buffer_->read<std::uint8_t>());

    switch(type)
    {
        case lui::object_type::TNIL:
            data = "nil";
        break;
        case lui::object_type::TBOOLEAN:
            data = ((bool)buffer_->read<std::uint8_t>()) ? "true" : "false";
        break;
        case lui::object_type::TLIGHTUSERDATA:
            data = utils::string::va("%lld", buffer_->read<std::uint64_t>());
        break;
        case lui::object_type::TNUMBER:
            data = utils::string::va("%g", buffer_->read<float>());
        break;
        case lui::object_type::TSTRING:
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
    //printf("%s\n", resolver::opcode_name(op).data());
                                                    // -------------------------------------
    switch(op)                                      // args    description
    {                                               // -------------------------------------
    case opcode::HKS_OPCODE_GETFIELD:               // A B C   R(A) = R(B)[K(C)]
        inst->mode = lui::instruction_mode::ABC;
        inst->data = find_constant(func, inst->C).value;
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_TEST:                   // A C     if not (R(A) <=> K(C)) then pc++
        inst->mode = lui::instruction_mode::ABC;    //         c != 0, !R(A)
        inst->data = find_constant(func, inst->C).value;  //   c == 0,  R(A)
        print_instruction(inst);                   
        break; // this have a constant somewhere B or C?
    case opcode::HKS_OPCODE_CALL_I:                 //
        inst->mode = lui::instruction_mode::ABC;
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_CALL_C:                 //
        inst->mode = lui::instruction_mode::ABC;
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_EQ:                     //
        inst->mode = lui::instruction_mode::ABC;
        if (inst->C < 0 || inst->sZero) inst->data = find_constant(func, inst->C).value;
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_EQ_BK:                  //
        inst->mode = lui::instruction_mode::ABC;
        inst->data = find_constant(func, inst->B).value;
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_GETGLOBAL:              // A Bx    R(A) := Gbl[Kst(Bx)] 
        inst->mode = lui::instruction_mode::ABC;
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_MOVE:                   // A B     R(A) := R(B)
        inst->mode = lui::instruction_mode::ABC;
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_SELF:                   //
        inst->mode = lui::instruction_mode::ABC;
        if (inst->C < 0 || inst->sZero) inst->data = find_constant(func, inst->C).value;
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_RETURN:                 //
        inst->mode = lui::instruction_mode::ABC;
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_GETTABLE_S:             //
        inst->mode = lui::instruction_mode::ABC;
        if (inst->C < 0 || inst->sZero) inst->data = find_constant(func, inst->C).value;
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_GETTABLE_N:             // 
        inst->mode = lui::instruction_mode::ABC;
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_GETTABLE:               // A B C   R(A) := R(B)[RK(C)]
        inst->mode = lui::instruction_mode::ABC;
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_LOADBOOL:               // A B C   R(A) := (Bool)B; if (C) pc++
        inst->mode = lui::instruction_mode::ABC;
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_TFORLOOP:               //
        inst->mode = lui::instruction_mode::ABC;
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_SETFIELD:               // A B C   R(B)[K(C)] = R(A) ????
        inst->mode = lui::instruction_mode::ABC;
        inst->data = find_constant(func, inst->C).value;
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_SETTABLE_S:             // A B C   R(A)[R(B)] := RK(C)
        inst->mode = lui::instruction_mode::ABC;
        // constant in C
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_SETTABLE_S_BK:          // A B C   R(A)[K(B)] := RK(C)
        inst->mode = lui::instruction_mode::ABC;
        inst->data = find_constant(func, inst->B).value;
        // constant in C
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_SETTABLE_N:             // A B C   R(A)[R(B)] := RK(C)
        inst->mode = lui::instruction_mode::ABC;
        // constant in C
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_SETTABLE_N_BK:          // A B C   R(A)[K(B)] := RK(C)
        inst->mode = lui::instruction_mode::ABC;
        inst->data = find_constant(func, inst->B).value;
        // constant in C
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_SETTABLE:               // A B C   R(A)[R(B)] := RK(C)
        inst->mode = lui::instruction_mode::ABC;
        // constant in C
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_SETTABLE_BK:            // A B C   R(A)[K(B)] := RK(C)
        inst->mode = lui::instruction_mode::ABC;
        inst->data = find_constant(func, inst->B).value;
        // constant in C
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_TAILCALL_I:             //
        inst->mode = lui::instruction_mode::ABC;
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_TAILCALL_C:             //
        inst->mode = lui::instruction_mode::ABC;
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_TAILCALL_M:             //
        inst->mode = lui::instruction_mode::ABC;
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_LOADK:                  // A Bx    R(A) := Kst(Bx)
        inst->mode = lui::instruction_mode::ABC;
        inst->data = find_constant(func, inst->Bx).value;
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_LOADNIL:                // A B     R(A) := ... := R(B) := nil
        inst->mode = lui::instruction_mode::ABC;
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_SETGLOBAL:
        inst->mode = lui::instruction_mode::ABC;
        inst->data = find_constant(func, inst->Bx).value;
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_JMP:
        inst->mode = lui::instruction_mode::AsBx;
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_CALL_M:
        inst->mode = lui::instruction_mode::ABC;
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_CALL:
        inst->mode = lui::instruction_mode::ABC;
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_INTRINSIC_INDEX:
        inst->mode = lui::instruction_mode::ABC;
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_INTRINSIC_NEWINDEX:
        inst->mode = lui::instruction_mode::ABC;
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_INTRINSIC_SELF:
        inst->mode = lui::instruction_mode::ABC;
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_INTRINSIC_INDEX_LITERAL:
        inst->mode = lui::instruction_mode::ABC;
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_INTRINSIC_NEWINDEX_LITERAL:
        inst->mode = lui::instruction_mode::ABC;
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_INTRINSIC_SELF_LITERAL:
        inst->mode = lui::instruction_mode::ABC;
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_TAILCALL:
        inst->mode = lui::instruction_mode::ABC;
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_GETUPVAL:
        inst->mode = lui::instruction_mode::ABC;
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_SETUPVAL:
        inst->mode = lui::instruction_mode::ABC;
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_ADD:
        inst->mode = lui::instruction_mode::ABC;
        if (inst->C < 0 || inst->sZero) inst->data = find_constant(func, inst->C).value;
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_ADD_BK:
        inst->mode = lui::instruction_mode::ABC;
        inst->data = find_constant(func, inst->B).value;
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_SUB:
        inst->mode = lui::instruction_mode::ABC;
        if (inst->C < 0 || inst->sZero) inst->data = find_constant(func, inst->C).value;
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_SUB_BK:
        inst->mode = lui::instruction_mode::ABC;
        inst->data = find_constant(func, inst->B).value;
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_MUL:
        inst->mode = lui::instruction_mode::ABC;
        if (inst->C < 0 || inst->sZero) inst->data = find_constant(func, inst->C).value;
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_MUL_BK:
        inst->mode = lui::instruction_mode::ABC;
        inst->data = find_constant(func, inst->B).value;
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_DIV:
        inst->mode = lui::instruction_mode::ABC;
        if (inst->C < 0 || inst->sZero) inst->data = find_constant(func, inst->C).value;
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_DIV_BK:
        inst->mode = lui::instruction_mode::ABC;
        inst->data = find_constant(func, inst->B).value;
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_MOD:
        inst->mode = lui::instruction_mode::ABC;
        if (inst->C < 0 || inst->sZero) inst->data = find_constant(func, inst->C).value;
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_MOD_BK:
        inst->mode = lui::instruction_mode::ABC;
        inst->data = find_constant(func, inst->B).value;
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_POW:
        inst->mode = lui::instruction_mode::ABC;
        if (inst->C < 0 || inst->sZero) inst->data = find_constant(func, inst->C).value;
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_POW_BK:
        inst->mode = lui::instruction_mode::ABC;
        inst->data = find_constant(func, inst->B).value;
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_NEWTABLE:
        inst->mode = lui::instruction_mode::ABC;
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_UNM:
        inst->mode = lui::instruction_mode::ABC;
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_NOT:
        inst->mode = lui::instruction_mode::ABC;
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_LEN:
        inst->mode = lui::instruction_mode::ABC;
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_LT:
        inst->mode = lui::instruction_mode::ABC;
        if (inst->C < 0 || inst->sZero) inst->data = find_constant(func, inst->C).value;
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_LT_BK:
        inst->mode = lui::instruction_mode::ABC;
        inst->data = find_constant(func, inst->B).value;
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_LE:
        inst->mode = lui::instruction_mode::ABC;
        if (inst->C < 0 || inst->sZero) inst->data = find_constant(func, inst->C).value;
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_LE_BK:
        inst->mode = lui::instruction_mode::ABC;
        inst->data = find_constant(func, inst->B).value;
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_CONCAT:
        inst->mode = lui::instruction_mode::ABC;
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_TESTSET:                // A B C   if (R(B) <=> C) then R(A) := R(B) else pc++
        inst->mode = lui::instruction_mode::AsBx;   //         RK(B) ?
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
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_CLOSE:                  // A       close all variables in the stack up to (>=) R(A)
        inst->mode = lui::instruction_mode::ABC;
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_CLOSURE:                // A Bx    R(A)
        inst->mode = lui::instruction_mode::ABx;
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_VARARG:                 // A B     R(A), R(A+1), ..., R(A+B-1) = vararg
        inst->mode = lui::instruction_mode::ABC;
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_TAILCALL_I_R1:
        inst->mode = lui::instruction_mode::ABC;
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_CALL_I_R1:
        inst->mode = lui::instruction_mode::ABC;
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_SETUPVAL_R1:
        inst->mode = lui::instruction_mode::ABC;
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_TEST_R1:                // same as TEST ????
        inst->mode = lui::instruction_mode::ABC;    //         c != 0, !R(A)
        inst->data = find_constant(func, inst->C).value;  //   c == 0,  R(A)
        print_instruction(inst);  // have a consts in B or C?
        break;
    case opcode::HKS_OPCODE_NOT_R1:
        inst->mode = lui::instruction_mode::ABC;
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_GETFIELD_R1:            // A B C   R(A) = R(B)[K(C)]
        inst->mode = lui::instruction_mode::ABC;
        inst->data = find_constant(func, inst->C).value;
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_SETFIELD_R1:
        inst->mode = lui::instruction_mode::ABC;
        inst->data = find_constant(func, inst->C).value;
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_NEWSTRUCT:
        inst->mode = lui::instruction_mode::ABC;
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_DATA:
        inst->mode = lui::instruction_mode::ABx;
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_SETSLOTN:
        inst->mode = lui::instruction_mode::ABC;
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_SETSLOTI:
        inst->mode = lui::instruction_mode::ABC;
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_SETSLOT:
        inst->mode = lui::instruction_mode::ABC;
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_SETSLOTS:
        inst->mode = lui::instruction_mode::ABC;
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_SETSLOTMT:
        inst->mode = lui::instruction_mode::ABC;
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_CHECKTYPE:
        inst->mode = lui::instruction_mode::ABC;
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_CHECKTYPES:
        inst->mode = lui::instruction_mode::ABC;
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_GETSLOT:
        inst->mode = lui::instruction_mode::ABC;
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_GETSLOTMT:
        inst->mode = lui::instruction_mode::ABC;
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_SELFSLOT:
        inst->mode = lui::instruction_mode::ABC;
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_SELFSLOTMT:
        inst->mode = lui::instruction_mode::ABC;
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_GETFIELD_MM:
        inst->mode = lui::instruction_mode::ABC;
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_CHECKTYPE_D:
        inst->mode = lui::instruction_mode::ABC;
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_GETSLOT_D:
        inst->mode = lui::instruction_mode::ABC;
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_GETGLOBAL_MEM:
        inst->mode = lui::instruction_mode::ABx;
        inst->data = find_constant(func, inst->Bx).value;
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_DELETE:
        inst->mode = lui::instruction_mode::ABC;
        print_instruction(inst);
        break;
    case opcode::HKS_OPCODE_DELETE_BK:
        inst->mode = lui::instruction_mode::ABC;
        inst->data = find_constant(func, inst->B).value;
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
        output_->write_string(utils::string::va("%s%-14s %02d %02d %02d ; %s\n", indented(tabsize_).data(), resolver::opcode_name(opcode(inst->OP)).data(), inst->A, inst->B, inst->C, inst->data.data()));   
    }
    else if(inst->mode == lui::instruction_mode::ABx)
    {
        output_->write_string(utils::string::va("%s%-14s %02d %02d    ; %s\n", indented(tabsize_).data(), resolver::opcode_name(opcode(inst->OP)).data(), inst->A, inst->Bx, inst->data.data()));   
    }
    else if(inst->mode == lui::instruction_mode::AsBx)
    {
        output_->write_string(utils::string::va("%s%-14s %02d %02d    ; %s\n", indented(tabsize_).data(), resolver::opcode_name(opcode(inst->OP)).data(), inst->A, inst->sBx, inst->data.data()));   
    
    }
    else
    {
        DISASSEMBLER_ERROR("INSTRUCTION MODE UNSET");
    }
}

} // namespace IW6
