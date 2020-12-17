// Copyright 2020 xensik. All rights reserved.
//
// Use of this source code is governed by a GNU GPLv3 license
// that can be found in the LICENSE file.

#ifndef _LUI_NODETREE_HPP_
#define _LUI_NODETREE_HPP_

namespace lui
{

enum class node_type
{
    null,
    nil,
    boolean,
    identifier,
    string,
    number,
    length,
    field,
    method,
    vararg,
    newtable,
    concat,
    call,
    assign,
    equal,
    not_equal,
    stmt_return,
    block,
    parameters,
    function,
    script,

    test,
    jump,
    label,
    debug,
};

struct node;
struct node_nil;
struct node_boolean;
struct node_identifier;
struct node_string;
struct node_number;
struct node_length;
struct node_field;
struct node_method;
struct node_vararg;
struct node_newtable;
struct node_concat;
struct node_call;
struct node_assign;
struct node_equal;
struct node_not_equal;
struct node_return;
struct node_block;
struct node_parameters;
struct node_function;
struct node_script;
struct node_test;
struct node_jump;
struct debug;

using node_ptr = std::shared_ptr<node>;
using nil_ptr = std::shared_ptr<node_nil>;
using boolean_ptr = std::shared_ptr<node_boolean>;
using identifier_ptr = std::shared_ptr<node_identifier>;
using string_ptr = std::shared_ptr<node_string>;
using number_ptr = std::shared_ptr<node_number>;
using length_ptr = std::shared_ptr<node_length>;
using field_ptr = std::shared_ptr<node_field>;
using method_ptr = std::shared_ptr<node_method>;
using vararg_ptr = std::shared_ptr<node_vararg>;
using newtable_ptr = std::shared_ptr<node_newtable>;
using concat_ptr = std::shared_ptr<node_concat>;
using call_ptr = std::shared_ptr<node_call>;
using assign_ptr = std::shared_ptr<node_assign>;
using equal_ptr = std::shared_ptr<node_equal>;
using not_equal_ptr = std::shared_ptr<node_not_equal>;
using return_ptr = std::shared_ptr<node_return>;
using block_ptr = std::shared_ptr<node_block>;
using parameters_ptr = std::shared_ptr<node_parameters>;
using function_ptr = std::shared_ptr<node_function>;
using script_ptr = std::shared_ptr<node_script>;
using test_ptr = std::shared_ptr<node_test>;
using jump_ptr = std::shared_ptr<node_jump>;
using debug_ptr = std::shared_ptr<debug>;

union child
{
    node_ptr as_node;
    identifier_ptr as_identifier;
    string_ptr as_string;
    number_ptr as_number;
    length_ptr as_length;
    field_ptr as_field;
    method_ptr as_method;
    vararg_ptr as_vararg;
    newtable_ptr as_newtable;
    concat_ptr as_concat;
    call_ptr as_call;
    assign_ptr as_assign;
    equal_ptr as_equal;
    not_equal_ptr as_not_equal;
    block_ptr as_block;
    parameters_ptr as_params;
    script_ptr as_script;
    test_ptr as_test;
    jump_ptr as_jump;

    ~child() {}
    child() {}
    child(node_ptr val): as_node(std::move(val)) {}
    
    child(child && val)
    {
        new(&as_node) node_ptr(std::move(val.as_node));
    }

    child& operator=(child && val)
    {
        new(&as_node) node_ptr(std::move(val.as_node));
        return *(child*)&as_node;
    }
};

struct node
{
    node_type type;
    std::string location;

    node() : type(node_type::null) {}
    node(node_type type) : type(type) {}
    node(node_type type, const std::string& location) : type(type), location(location) {}
    
    virtual ~node() = default;
    virtual auto print() -> std::string { return ""; };
     
protected:
    static std::uint32_t indent;
    static void reset_indentation() { indent = 0; }
    static std::string indented(std::uint32_t indent)
    {
        static char buff[100];
        snprintf(buff, sizeof(buff), "%*s", indent, "");
        return std::string(buff);
    }
};

struct node_nil : public node
{
    node_nil(const std::string& location) : node(node_type::nil, location) {}

    node_nil() : node(node_type::nil) {}

    auto print() -> std::string override
    {
        return  "nil";
    }
};

struct node_boolean : public node
{
    bool value;
    node_boolean(const std::string& location, bool value)
        : node(node_type::boolean, location), value(value) {}

    node_boolean(bool value) : node(node_type::boolean), value(value) {}

    auto print() -> std::string override
    {
        return  value ? "true" : "false";
    }
};

struct node_identifier : public node
{
    std::string value;

    node_identifier(const std::string& location, const std::string& value)
        : node(node_type::identifier, location), value(value) {}

    node_identifier(const std::string& value)
        : node(node_type::identifier), value(value) {}

    auto print() -> std::string override
    {
        return value;
    }
};

struct node_string : public node
{
    std::string value;

    node_string(const std::string& location, const std::string& value)
        : node(node_type::string, location), value(value) {}
    
    node_string(const std::string& value)
        : node(node_type::string), value(value) {}

    auto print() -> std::string override
    {
        return value;
    }
};

struct node_number : public node
{
    std::string value;

    node_number(const std::string& location, const std::string& value)
        : node(node_type::number, location), value(std::move(value)) {}

    node_number(const std::string& value)
        : node(node_type::number), value(std::move(value)) {}

    auto print() -> std::string override
    {
        return value;
    }
};

struct node_length: public node
{
    child obj;

    node_length(const std::string& location, child obj)
        : node(node_type::length, location), obj(std::move(obj)) {}

    node_length(child obj)
        : node(node_type::length), obj(std::move(obj)) {}

    auto print() -> std::string override
    {
        return "#" + obj.as_node->print();
    }
};

struct node_field : public node
{
    child obj;
    child field;

    node_field(const std::string& location, child obj, child field)
        : node(node_type::field, location), obj(std::move(obj)), field(std::move(field)) {}

    node_field(child obj, child field)
        : node(node_type::field), obj(std::move(obj)), field(std::move(field)) {}

    auto print() -> std::string override
    {
        return obj.as_node->print() + "." + field.as_node->print();
    }
};

struct node_method : public node
{
    child obj;
    child field;

    node_method(const std::string& location, child obj, child field)
        : node(node_type::method, location), obj(std::move(obj)), field(std::move(field)) {}

    node_method(child obj, child field)
        : node(node_type::method), obj(std::move(obj)), field(std::move(field)) {}

    auto print() -> std::string override
    {
        return obj.as_node->print() + ":" + field.as_node->print();
    }
};

struct node_vararg : public node
{
    node_vararg(const std::string& location) : node(node_type::vararg, location) {}

    node_vararg() : node(node_type::vararg) {}

    auto print() -> std::string override
    {
        return "...";
    }
};

struct node_newtable : public node
{
    node_newtable(const std::string& location) : node(node_type::newtable, location) {}

    node_newtable() : node(node_type::newtable) {}

    auto print() -> std::string override
    {
        return "{}";
    }
};

struct node_concat : public node
{
    std::vector<child> list;

    node_concat(const std::string& location) : node(node_type::concat, location) {}

    node_concat() : node(node_type::concat) {}

    auto print() -> std::string override
    {
        std::string data;

        for (const auto& param : list)
        {
            data += param.as_node->print();
            if(&param != &list.back()) data += " .. ";
        }

        return data;
    }
};

struct node_call : public node
{
    child name;
    child params;

    node_call(const std::string& location) : node(node_type::call, location) {}

    node_call() : node(node_type::call) {}

    auto print() -> std::string override
    {
        return name.as_node->print() + "(" + params.as_node->print() + ")";
    }
};

struct node_assign : public node
{
    child lvalue;
    child rvalue;

    node_assign(const std::string& location, child lvalue, child rvalue)
        : node(node_type::assign, location), lvalue(std::move(lvalue)), rvalue(std::move(rvalue)) {}

    node_assign(child lvalue, child rvalue)
        : node(node_type::assign), lvalue(std::move(lvalue)), rvalue(std::move(rvalue)) {}

    auto print() -> std::string override
    {
        return lvalue.as_node->print() + " = " + rvalue.as_node->print();
    }
};

struct node_not_equal : public node
{
    child lvalue;
    child rvalue;

    node_not_equal(const std::string& location, child lvalue, child rvalue)
        : node(node_type::not_equal, location), lvalue(std::move(lvalue)), rvalue(std::move(rvalue)) {}

    node_not_equal(child lvalue, child rvalue)
        : node(node_type::not_equal), lvalue(std::move(lvalue)), rvalue(std::move(rvalue)) {}

    auto print() -> std::string override
    {
        return lvalue.as_node->print() + " ~= " + rvalue.as_node->print();
    }
};

struct node_equal : public node
{
    child lvalue;
    child rvalue;

    node_equal(const std::string& location, child lvalue, child rvalue)
        : node(node_type::equal, location), lvalue(std::move(lvalue)), rvalue(std::move(rvalue)) {}

    node_equal(child lvalue, child rvalue)
        : node(node_type::equal), lvalue(std::move(lvalue)), rvalue(std::move(rvalue)) {}

    auto print() -> std::string override
    {
        return lvalue.as_node->print() + " == " + rvalue.as_node->print();
    }
};

struct node_return : public node
{
    std::vector<child> stmts;

    node_return(const std::string& location) : node(node_type::stmt_return, location) {}

    node_return() : node(node_type::stmt_return) {}

    auto print() -> std::string override
    {
        std::string data;

        data += "return";
        for(auto& stmt : stmts)
        {
            data += " " + stmt.as_node->print();
            if(&stmt != &stmts.back())
            {
                data += ",";
            }
        }

        return data;
    }
};

struct node_block : public node
{
    std::vector<child> stmts;

    node_block(const std::string& location) : node(node_type::block, location) {}

    node_block() : node(node_type::block) {}

    auto print() -> std::string override
    {
        std::string data;

        std::string pad = indented(indent);

        for(auto& stmt : stmts)
        {
            data += "\n" + pad + stmt.as_node->print();
        }

        return data;
    }
};

struct node_parameters : public node
{
    std::vector<child> list;
    bool vararg;

    node_parameters(const std::string& location) : node(node_type::parameters, location) {}

    node_parameters() : node(node_type::parameters) {}

    auto print() -> std::string override
    {
        if(vararg) { return " ... "; }
    
        std::string data;

        for (const auto& param : list)
        {
            data += param.as_node->print();
            if(&param != &list.back()) data += ", ";
        }

        return data;
    }
};

struct node_function : public node
{
    child name;
    child params;
    child block;
    std::vector<child> sub_funcs;

    node_function(const std::string& location, child name, child params, child block)
        : node(node_type::function, location), name(std::move(name)), params(std::move(params)),
            block(std::move(block)) {}

    node_function(child name, child params, child block)
        : node(node_type::function), name(std::move(name)), params(std::move(params)),
            block(std::move(block)) {}

    auto print() -> std::string override
    {
        std::string data;

        if(name.as_node->print() == "_init_")
        {
            data += block.as_node->print();
        
            for(auto& f : sub_funcs)
            {
                data += "\n" + f.as_node->print();
            }
        }
        else
        {
            std::string pad = indented(indent);

            data += "\n" + pad + "local function " + name.as_node->print() + "(" + params.as_node->print() + ")";

            indent += 4;

            data += block.as_node->print();

            for(auto& f : sub_funcs)
            {
                data += "\n" + f.as_node->print();
            }

            indent -= 4;

            data += "\n" + pad + "end";
        }
        
        return data;
    }
};

struct node_script : public node
{
    child main;
    
    node_script(const std::string& location)
        : node(node_type::script, location) {}

    node_script()
        : node(node_type::script) {}

    auto print() -> std::string override
    {
        return main.as_node->print() + "\n";
    }
};

struct node_test : public node
{
    child cond;
    bool is_not;
    
    node_test(const std::string& location, child cond, bool is_not)
        : node(node_type::test, location), cond(std::move(cond)), is_not(is_not) {}

    node_test(child cond, bool is_not)
        : node(node_type::test), cond(std::move(cond)), is_not(is_not) {}

    auto print() -> std::string override
    {
        std::string data;
        data += "if ";
        data += (is_not) ? "not " : "";
        data += cond.as_node->print() + " then";
        return data;
    }
};

struct node_jump : public node
{
    child loc;
    
    node_jump(const std::string& location, child loc)
        : node(node_type::jump, location), loc(std::move(loc)) {}

    node_jump(child loc)
        : node(node_type::jump), loc(std::move(loc)) {}

    auto print() -> std::string override
    {
        return "jump(" + loc.as_node->print() + ")";
    }
};

struct node_label : public node
{
    std::string loc;
    
    node_label(const std::string& location, const std::string& loc)
        : node(node_type::label, location), loc(std::move(loc)) {}

    node_label(const std::string& loc)
        : node(node_type::label), loc(std::move(loc)) {}

    auto print() -> std::string override
    {
        return "-- " + loc + ":";
    }
};

struct node_debug : public node
{
    std::string data;
    
    node_debug(const std::string& data) : node(node_type::debug), data(std::move(data)) {}

    auto print() -> std::string override
    {
        return "-- " + data;
    }
};

} // namespace lui

#endif // _LUI_NODETREE_HPP_
