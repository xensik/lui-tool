// Copyright 2020 xensik. All rights reserved.
//
// Use of this source code is governed by a GNU GPLv3 license
// that can be found in the LICENSE file.

#include "IW6.hpp"

namespace IW6
{

auto decompiler::output() -> std::vector<std::uint8_t>
{
    std::string data;

    data += "-- IW6 PC LUI\n-- Decompiled by https://github.com/xensik/lui-tool\n";
    data += script_->print();

    std::vector<std::uint8_t> output;

    output.resize(data.size());
    std::memcpy(output.data(), data.data(), output.size());

    return output;
}

void decompiler::decompile(lui::file_ptr file)
{
    file_ = std::move(file);
    script_ = std::make_shared<lui::node_script>();
    var_index = -1;
    this->decompile_function(file_->main);
    
    script_->main = lui::child(file_->main.node);
}

void decompiler::decompile_function(lui::function& func)
{
    auto name = std::make_shared<lui::node_identifier>(func.name);

    auto params = std::make_shared<lui::node_parameters>();

    if(func.vararg_flags == 2)
    {
        params->vararg = true;
    }
    else
    {
        for(auto i = 0; i < func.param_count; i ++)
        {
            auto arg = std::make_shared<lui::node_identifier>(utils::string::va("arg%d", i));
            func.stack.push_back(arg);
            params->list.push_back(lui::child(arg));
        }
    }

    for(auto i = func.param_count; i < func.register_count; i++)
    {
        auto var = std::make_shared<lui::node_nil>();
        func.stack.push_back(var);
    }

    auto block = std::make_shared<lui::node_block>();

    func.node = std::make_shared<lui::node_function>(lui::child(name), lui::child(params), lui::child(block));

    for(std::uint32_t i = 0; i < func.instruction_count; i++)
    {
        this->decompile_instruction(func, i);
    }

    for(auto& sub : func.sub_funcs)
    {
        this->decompile_function(sub);
        func.node->sub_funcs.push_back(lui::child(sub.node));
    }
}

void decompiler::decompile_instruction(lui::function& func, std::uint32_t& index)
{
/*#define GET_REG(id) func->registers.at(id).node_
#define SET_REG(id, n) func->registers.at(id).node_ = n
#define KST(id) func->constants.at(id)
#define KST_VAL(id) func->constants.at(id).data_.value()*/
#define RK(f, c, z) (c < 0 || z) ? std::string(this->find_constant(f, c).print()) : std::string(lui::reg(c).print());
    
    auto& inst = func.instructions.at(index);
    auto op = opcode(inst->OP);
    auto loc = utils::string::va("%X", inst->index);
    
    auto it = func.labels.find(inst->index);
    if(it != func.labels.end())
    {
        auto node = std::make_shared<lui::node_label>(it->second);
        func.node->block.as_block->stmts.push_back(lui::child(node));
    }
    
    debug_print(func, inst);
    
    switch(op)
    {
    case opcode::HKS_OPCODE_GETFIELD: // A B C   R(A) := R(B)[K(C)]
    {
        auto field_id = lui::child(find_constant(func, inst->C).to_node());
        auto obj = lui::child(func.stack.at(inst->B));
        func.stack.at(inst->A) = std::make_shared<lui::node_field>(std::move(obj), std::move(field_id));
    }
    break;
    case opcode::HKS_OPCODE_TEST: // A C     if not (R(A) <=> C) then pc++
    {
        auto test = std::make_shared<lui::node_test>(lui::child(func.stack.at(inst->A)), inst->C == 1);
        func.node->block.as_block->stmts.push_back(lui::child(test));
    }
    break;
    case opcode::HKS_OPCODE_CALL_I: // A B C   ?
    {
        func.node->block.as_block->stmts.push_back(decompile_call(func, inst));
    }
    break;
    case opcode::HKS_OPCODE_EQ: // A B C   if ((R(B) == RK(C)) ~= A) then PC++
    {
        if(inst->A == 0)
        {
            auto node = std::make_shared<lui::node_not_equal>(func.stack.at(inst->B), (inst->C < 0 || inst->sZero) ? this->find_constant(func, inst->C).to_node() : func.stack.at(inst->C));
            func.node->block.as_block->stmts.push_back(lui::child(node));
        }
        else
        {
            auto node = std::make_shared<lui::node_equal>(func.stack.at(inst->B),  (inst->C < 0 || inst->sZero) ? this->find_constant(func, inst->C).to_node() : func.stack.at(inst->C));
            func.node->block.as_block->stmts.push_back(lui::child(node));
        }
    }
    break;
    case opcode::HKS_OPCODE_EQ_BK: // A B C   if ((K(B) == R(C)) ~= A) then PC++
    {
        if(inst->A == 0)
        {
            auto node = std::make_shared<lui::node_not_equal>(find_constant(func, inst->B).to_node(), func.stack.at(inst->C));
            func.node->block.as_block->stmts.push_back(lui::child(node));
        }
        else
        {
            auto node = std::make_shared<lui::node_equal>(find_constant(func, inst->B).to_node(), func.stack.at(inst->C));
            func.node->block.as_block->stmts.push_back(lui::child(node));
        }
    }
    break;
    case opcode::HKS_OPCODE_GETGLOBAL: // A Bx    R(A) := Gbl[Kst(Bx)] 
    {
        func.stack.at(inst->A) = find_constant(func, inst->Bx).to_node();
    }
    break;
    case opcode::HKS_OPCODE_MOVE: // A B     R(A) := R(B)
    {
        func.stack.at(inst->A) = func.stack.at(inst->B);
    }
    break;
    case opcode::HKS_OPCODE_SELF: // A B C   R(A+1) := R(B); R(A) := R(B)[RK(C)]
    {
        auto field_id = lui::child(find_constant(func, inst->C).to_node());
        auto obj = lui::child(func.stack.at(inst->B));
        auto field = std::make_shared<lui::node_method>(std::move(obj), std::move(field_id));
        func.stack.at(inst->A) = field;
        func.stack.at(inst->A + 1) = std::make_shared<lui::node_identifier>("this");
        // R(A + 1), store the 'this' pointer
    }
    break;
    case opcode::HKS_OPCODE_RETURN: // A B    return R(A), ... ,R(A+B-2)
    {
        if (func.instructions.back()->index == inst->index)
            break;

        if(inst->B >= 1)
        {
            auto ret = std::make_shared<lui::node_return>();

            for(auto i = inst->A; i < inst->A + inst->B -1; i++)
            {
                ret->stmts.push_back(lui::child(func.stack.at(i)));
            }

            func.node->block.as_block->stmts.push_back(lui::child(ret));
        }
        else if (inst->B == 0)
        {
            
        }
    }
    break;
    case opcode::HKS_OPCODE_GETTABLE_S: // A B C   R(A) := R(B)[RK(C)]
    {
        auto field_id = lui::child((inst->C < 0 || inst->sZero) ? find_constant(func, inst->C).to_node() : func.stack.at(inst->C));
        auto obj = lui::child(func.stack.at(inst->B));
        auto field = std::make_shared<lui::node_field>(std::move(obj), std::move(field_id));
        func.stack.at(inst->A) = field;
    }
    break;
    case opcode::HKS_OPCODE_GETTABLE: // A B C   R(A) := R(B)[RK(C)]
    {
        auto field_id = lui::child((inst->C < 0 || inst->sZero) ? find_constant(func, inst->C).to_node() : func.stack.at(inst->C));
        auto obj = lui::child(func.stack.at(inst->B));
        auto field = std::make_shared<lui::node_field>(std::move(obj), std::move(field_id));
        func.stack.at(inst->A) = field;
    }
    break;
    case opcode::HKS_OPCODE_LOADBOOL: // A B C   R(A) := (Bool)B; if (C) pc++  
    {
        auto node = std::make_shared<lui::node_boolean>((bool)inst->B);
        func.stack.at(inst->A) = node;
    }
    break;
    case opcode::HKS_OPCODE_TFORLOOP: // A C    3 internal vars, and user vars in R(A+3) to C
    break;
    case opcode::HKS_OPCODE_SETFIELD: // A B C   R(A)[K(B)] := RK(C)
    {
        auto obj = lui::child(func.stack.at(inst->A));
        auto field_id = lui::child(find_constant(func, inst->B).to_node());
        auto data = (inst->C < 0 || inst->sZero) ? this->find_constant(func, inst->C).to_node() : func.stack.at(inst->C);
        auto field = std::make_shared<lui::node_field>(std::move(obj), std::move(field_id));
        auto node = std::make_shared<lui::node_assign>(lui::child(field), data);
        func.node->block.as_block->stmts.push_back(lui::child(node));
    }
    break;
    case opcode::HKS_OPCODE_SETTABLE_S:             // A B C   R(A)[R(B)] := RK(C)
    break;
    case opcode::HKS_OPCODE_SETTABLE_S_BK:          // A B C   R(A)[K(B)] := RK(C)
    break;
    case opcode::HKS_OPCODE_SETTABLE:               // A B C   R(A)[R(B)] := RK(C)
    break;
    case opcode::HKS_OPCODE_SETTABLE_BK:            // A B C   R(A)[K(B)] := RK(C)
    break;
    case opcode::HKS_OPCODE_TAILCALL_I:             // A B C   return R(A)(R(A+1), ... ,R(A+B-1))
    break;
    case opcode::HKS_OPCODE_LOADK:                  // A Bx    R(A) := Kst(Bx)
    {
        auto kst = find_constant(func, inst->Bx);
        if(kst.data_.type_ == lui::data::t::STRING)
            kst.data_.to_literal();

        auto node = kst.to_node();
        func.stack.at(inst->A) = node;
    }
    break;
    case opcode::HKS_OPCODE_LOADNIL:                // A B     R(A) := ... := R(B) := nil
    break;
    case opcode::HKS_OPCODE_SETGLOBAL:              // A Bx    Gbl[Kst(Bx)] := R(A)
    {
        auto kst = find_constant(func, inst->Bx).to_node();
        auto node = std::make_shared<lui::node_assign>(lui::child(kst), func.stack.at(inst->A));

        func.node->block.as_block->stmts.push_back(lui::child(node));
    }
    break;
    case opcode::HKS_OPCODE_JMP:                    // sBx      pc += sBx
    {
        auto id = std::make_shared<lui::node_identifier>(utils::string::va("LOC_%X", (inst->index + 4 + (inst->sBx * 4))));
        auto jmp = std::make_shared<lui::node_jump>(lui::child(id));
        func.node->block.as_block->stmts.push_back(lui::child(jmp));
    }
    break;
    case opcode::HKS_OPCODE_CALL:                   // A B C   ?
    break;
    case opcode::HKS_OPCODE_TAILCALL:               // A B C   return R(A)(R(A+1), ... ,R(A+B-1))
    break;
    case opcode::HKS_OPCODE_GETUPVAL:               // A B      R(A) := UpValue[B]
    break;
    case opcode::HKS_OPCODE_SETUPVAL:               // A B      UpValue[B] := R(A)
    break;
    case opcode::HKS_OPCODE_ADD:                    // A B C   R(A) := R(B) + RK(C)
    break;
    case opcode::HKS_OPCODE_ADD_BK:                 // A B C   R(A) := K(B) + R(C)
    break;
    case opcode::HKS_OPCODE_SUB:                    //  A B C   R(A) := R(B) – RK(C)
    break;
    case opcode::HKS_OPCODE_SUB_BK:                 //  A B C   R(A) := K(B) – R(C)
    break;
    case opcode::HKS_OPCODE_MUL:                    //  A B C   R(A) := R(B) * RK(C)
    break;
    case opcode::HKS_OPCODE_MUL_BK:                 //  A B C   R(A) := K(B) * R(C)
    break;
    case opcode::HKS_OPCODE_DIV:                    // A B C   R(A) := R(B) / RK(C)
    break;
    case opcode::HKS_OPCODE_DIV_BK:                 // A B C   R(A) := K(B) / R(C)
    break;
    case opcode::HKS_OPCODE_MOD:                    // A B C   R(A) := R(B) % RK(C)
    break;
    case opcode::HKS_OPCODE_MOD_BK:                 // A B C   R(A) := K(B) % R(C)
    break;
    case opcode::HKS_OPCODE_POW:                    // A B C   R(A) := R(B) ^ RK(C)
    break;
    case opcode::HKS_OPCODE_POW_BK:                 // A B C   R(A) := K(B) ^ R(C)
    break;
    case opcode::HKS_OPCODE_NEWTABLE:               // A B C   R(A) := array=B hash=C
    {
        auto node = std::make_shared<lui::node_identifier>(get_new_variable());
        func.stack.at(inst->A) = node;

        auto table = std::make_shared<lui::node_newtable>();

        // set local ?
        // make a initializer list for array or hash  != 0
        auto assign = std::make_shared<lui::node_assign>(lui::child(node), lui::child(table));
    
        func.node->block.as_block->stmts.push_back(lui::child(assign));
    }
    break;
    case opcode::HKS_OPCODE_UNM:                    // A B     R(A) := -R(B)
    break;
    case opcode::HKS_OPCODE_NOT: // A B     R(A) := not R(B)
    break;
    case opcode::HKS_OPCODE_LEN: // A B     R(A) := length of R(B)
    {
        auto node = std::make_shared<lui::node_length>(lui::child(func.stack.at(inst->B)));
        func.stack.at(inst->A) = node;
        //func.node->block.as_block->stmts.push_back(lui::child(node));
    }
    break;
    case opcode::HKS_OPCODE_LT:                     // A B C   if ((R(B) < RK(C)) ~= A) then PC++
    break;
    case opcode::HKS_OPCODE_LT_BK:                  // A B C   if ((K(B) < R(C)) ~= A) then PC++
    break;
    case opcode::HKS_OPCODE_LE:                     //  A B C if ((R(B) <= RK(C)) ~= A) then PC++
    break;
    case opcode::HKS_OPCODE_LE_BK:                  //  A B C if ((K(B) <= R(C)) ~= A) then PC++
    break;
    case opcode::HKS_OPCODE_CONCAT:                 // A B C   R(A) := R(B).. ... ..R(C)
    {
        auto concat = std::make_shared<lui::node_concat>();

        auto num =  (inst->C - inst->B) + 1;

        for(auto i = 0; i < num; i++)
        {
            concat->list.push_back(func.stack.at(inst->B + i));
        }

        func.stack.at(inst->A) = concat;
    }
    break;
    case opcode::HKS_OPCODE_TESTSET:                // A B C   if (R(B) <=> C) then R(A) := R(B) else pc++
        break;
    case opcode::HKS_OPCODE_FORPREP:                // A sBx   R(A) -= R(A+2); PC += sBx
        break;
    case opcode::HKS_OPCODE_FORLOOP:                // A sBx   R(A) += R(A+2) if R(A) <?= R(A+1) then { PC += sBx; R(A+3) = R(A) }
        break;
    case opcode::HKS_OPCODE_SETLIST:                // A B C   R(A)[(C-1)*FPF+i] := R(A+i), 1 <= i <= B
        break;
    case opcode::HKS_OPCODE_CLOSE:                  // A       close all variables in the stack up to (>=) R(A)
        break;
    case opcode::HKS_OPCODE_CLOSURE:                // A Bx    R(A) := closure(KPROTO[Bx], R(A), ... ,R(A+n))
    {
        auto node = std::make_shared<lui::node_identifier>(func.sub_funcs.at(inst->Bx).name);
        func.stack.at(inst->A) = node;
    }
    break;
    case opcode::HKS_OPCODE_VARARG:                 // A B     R(A), R(A+1), ..., R(A+B-1) = vararg
    {
        auto node = std::make_shared<lui::node_vararg>();
        func.stack.at(inst->A) = node;
    }
    break;
    case opcode::HKS_OPCODE_TAILCALL_I_R1:          // A B C   return R(A)(R(A+1), ... ,R(A+B-1))
        break;
    case opcode::HKS_OPCODE_CALL_I_R1:              // A B C   ?
        func.node->block.as_block->stmts.push_back(decompile_call(func, inst));
        break;
    case opcode::HKS_OPCODE_SETUPVAL_R1:            // A B      UpValue[B] := R(A)
        break;
    case opcode::HKS_OPCODE_TEST_R1:                // A C     if not (R(A) <=> C) then pc++
    {
        bool is_not = inst->C == 1;
        auto test = std::make_shared<lui::node_test>(lui::child(func.stack.at(inst->A)), is_not);
        func.node->block.as_block->stmts.push_back(lui::child(test));
    }
    break;
    case opcode::HKS_OPCODE_NOT_R1:                 // A B     R(A) := not R(B)
        break;
    case opcode::HKS_OPCODE_GETFIELD_R1:            // A B C   R(A) = R(B)[K(C)]
    {
        auto field_id = lui::child(find_constant(func, inst->C).to_node());
        auto obj = lui::child(func.stack.at(inst->B));
        auto field = std::make_shared<lui::node_field>(std::move(obj), std::move(field_id));
        func.stack.at(inst->A) = field;
    }
    break;
    case opcode::HKS_OPCODE_SETFIELD_R1:            // A B C   R(A)[K(B)] = RK(C)
    {
        auto obj = lui::child(func.stack.at(inst->A));
        auto field_id = lui::child(find_constant(func, inst->B).to_node());
        auto data = (inst->C < 0 || inst->sZero) ? this->find_constant(func, inst->C).to_node() : func.stack.at(inst->C);
        auto field = std::make_shared<lui::node_field>(std::move(obj), std::move(field_id));
        auto node = std::make_shared<lui::node_assign>(lui::child(field), data);
        func.node->block.as_block->stmts.push_back(lui::child(node));
    }
    break;
    case opcode::HKS_OPCODE_DATA:                   // A Bx    ????
        break;
    case opcode::HKS_OPCODE_GETGLOBAL_MEM:          // A Bx    R(A) := Gbl[Kst(Bx)]
    {
        auto konst = find_constant(func, inst->Bx).to_node();
        func.stack.at(inst->A) = konst;
    }
    break;
    default:
        DISASSEMBLER_ERROR("Unhandled opcode %s", opcode_name(opcode(inst->OP)).data());
        break;
    }
}

auto decompiler::decompile_call(lui::function& func, const lui::instruction_ptr& inst) -> lui::child
{
    std::int32_t arg_num = inst->B - 1;
    std::int32_t ret_num = inst->C - 1;

    auto params = std::make_shared<lui::node_parameters>();

    if(arg_num > 0)
    {
        auto ncall = lui::child(func.stack.at(inst->A));
        auto i = 1;
        if(ncall.as_node->type == lui::node_type::method) i = 2;

        for(; i <= arg_num; i++)
        {
            params->list.push_back(lui::child(func.stack.at(inst->A + i)));
        }
    }
    
    auto call = std::make_shared<lui::node_call>();
    call->name = lui::child(func.stack.at(inst->A));
    call->params = lui::child(params);

    if(ret_num > 0) // ipairs call store rets in R(A+3) and TFORLOOP C count
    {
        auto retlist = std::make_shared<lui::node_parameters>();
        // special ipairs
        if(call->name.as_node->type == lui::node_type::identifier && call->name.as_identifier->value == "ipairs")
        {
            for(auto i = 0; i < 2; i++)
            {
                auto var = std::make_shared<lui::node_identifier>(get_new_variable());
                func.stack.at(inst->A + 3 + i) = var;
                retlist->list.push_back(lui::child(var));
            }
        }
        else
        {
            for(auto i = 0; i < ret_num; i++)
            {
                auto var = std::make_shared<lui::node_identifier>(get_new_variable());
                func.stack.at(inst->A + i) = var;
                retlist->list.push_back(lui::child(var));
            }
        }
        
        return lui::child(std::make_shared<lui::node_assign>(lui::child(retlist), lui::child(call)));
    }
    else return lui::child(call);
}

void decompiler::debug_print(const lui::function& func, const lui::instruction_ptr& inst)
{
    //auto data = utils::string::va("%-14s %s", opcode_name(opcode(inst->OP)).data(), inst->data.data());
    //auto node = std::make_shared<lui::node_debug>(data);
    //func.node->block.as_block->stmts.push_back(lui::child(node));
}

auto decompiler::find_constant(const lui::function& func, std::int32_t index) -> lui::kst
{
    if (index < 0) index = -index;

    return func.constants.at(index);
}

auto decompiler::get_new_variable() -> std::string
{
    var_index++;
    return utils::string::va("var%d", var_index);
}

} // namespace IW6
