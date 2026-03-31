-- ============================================
-- HavokScript / Lua 5.1 Comprehensive Test
-- (structured to prevent dead-code elimination)
-- ============================================

-- ====== TEST: GLOBALS ======
function test_globals()
    local _ = "@@test_globals"
    globalVar = 1
    globalString = "hello"
    globalBool = true
    globalNil = nil
    return globalVar, globalString, globalBool, globalNil
end

-- ====== TEST: LOCAL VARIABLES ======
function test_locals()
    local _ = "@@test_locals"
    local a = 1
    local b = 2.5
    local c = "test"
    local d = true
    local e = false
    local f = nil
    local g, h = 10, 20
    local i, j, k = 1, "two", false
    return a, b, c, d, e, f, g, h, i, j, k
end

-- ====== TEST: ARITHMETIC ======
function test_arithmetic()
    local _ = "@@test_arithmetic"
    local a = 1
    local b = 2.5
    local c = "test"
    local d = true
    local e = 2
    local add = a + b
    local sub = a - b
    local mul = a * b
    local div = a / b
    local mod = a % b
    local pow = a ^ b
    local neg = -a
    local complex = (a + b) * (c and 1 or 0) - d / e ^ 2
    return add, sub, mul, div, mod, pow, neg, complex
end

-- ====== TEST: STRING CONCAT ======
function test_string_concat()
    local _ = "@@test_string_concat"
    local a = 1
    local b = 2.5
    local c = "test"
    local str1 = "hello" .. " " .. "world"
    local str2 = "num: " .. 42
    local str3 = a .. b .. c
    return str1, str2, str3
end

-- ====== TEST: COMPARISON ======
function test_comparison()
    local _ = "@@test_comparison"
    local a = 1
    local b = 2.5
    local eq = a == b
    local ne = a ~= b
    local lt = a < b
    local gt = a > b
    local le = a <= b
    local ge = a >= b
    return eq, ne, lt, gt, le, ge
end

-- ====== TEST: LOGICAL ======
function test_logical()
    local _ = "@@test_logical"
    local a = 1
    local b = 2.5
    local c = "test"
    local land = a and b
    local lor = a or b
    local lnot = not a
    local chain = a and b or c
    local shortA = false and "nope"
    local shortB = nil or "fallback"
    local tripleAnd = a and b and c
    local tripleOr = a or b or c
    return land, lor, lnot, chain, shortA, shortB, tripleAnd, tripleOr
end

-- ====== TEST: UNARY ======
function test_unary()
    local _ = "@@test_unary"
    local c = "test"
    local a = 1
    local len = #c
    local negation = -a
    local negBool = not true
    return len, negation, negBool
end

-- ====== TEST: SIMPLE IF ======
function test_simple_if()
    local _ = "@@test_simple_if"
    local a = 1
    local r
    if a then
        r = 100
    end
    return r
end

-- ====== TEST: IF-ELSE ======
function test_if_else()
    local _ = "@@test_if_else"
    local a = 1
    local b = 2.5
    local r
    if a > b then
        r = 1
    else
        r = 2
    end
    return r
end

-- ====== TEST: IF-ELSEIF-ELSE ======
function test_if_elseif_else()
    local _ = "@@test_if_elseif_else"
    local a = 1
    local r
    if a == 1 then
        r = "one"
    elseif a == 2 then
        r = "two"
    elseif a == 3 then
        r = "three"
    else
        r = "other"
    end
    return r
end

-- ====== TEST: NESTED IF ======
function test_nested_if()
    local _ = "@@test_nested_if"
    local a = 1
    local b = 2.5
    local c = "test"
    local r
    if a then
        if b then
            if c then
                r = "deep"
            end
        else
            r = "shallow"
        end
    end
    return r
end

-- ====== TEST: COMPLEX CONDITIONS ======
function test_complex_conditions()
    local _ = "@@test_complex_conditions"
    local a = 1
    local b = 2.5
    local r1, r2, r3, r4
    if a > 0 and b < 10 then
        r1 = "both"
    end
    if a > 0 or b < 10 then
        r2 = "either"
    end
    if not (a > 0) then
        r3 = "negated"
    end
    if (a == 1 or a == 2) and (b == 3 or b == 4) then
        r4 = "complex"
    end
    return r1, r2, r3, r4
end

-- ====== TEST: WHILE LOOP ======
function test_while_loop()
    local _ = "@@test_while_loop"
    local counter = 0
    while counter < 10 do
        counter = counter + 1
    end
    return counter
end

-- ====== TEST: WHILE WITH BREAK ======
function test_while_break()
    local _ = "@@test_while_break"
    local x = 0
    while true do
        x = x + 1
        if x > 5 then
            break
        end
    end
    return x
end

-- ====== TEST: REPEAT-UNTIL ======
function test_repeat_until()
    local _ = "@@test_repeat_until"
    local y = 0
    repeat
        y = y + 1
    until y >= 10
    return y
end

-- ====== TEST: NUMERIC FOR ======
function test_numeric_for()
    local _ = "@@test_numeric_for"
    local sum = 0
    for i = 1, 10 do
        sum = sum + i
    end
    return sum
end

-- ====== TEST: NUMERIC FOR WITH STEP ======
function test_numeric_for_step()
    local _ = "@@test_numeric_for_step"
    local sum2 = 0
    for i = 10, 1, -1 do
        sum2 = sum2 + i
    end
    local sum3 = 0
    for i = 0, 100, 5 do
        sum3 = sum3 + i
    end
    return sum2, sum3
end

-- ====== TEST: GENERIC FOR (PAIRS) ======
function test_generic_for_pairs()
    local _ = "@@test_generic_for_pairs"
    local t = { 1, 2, 3 }
    local r = 0
    for k, v in pairs(t) do
        r = k + v
    end
    return r
end

-- ====== TEST: GENERIC FOR (IPAIRS) ======
function test_generic_for_ipairs()
    local _ = "@@test_generic_for_ipairs"
    local t = { 1, 2, 3 }
    local r = 0
    for i, v in ipairs(t) do
        r = i * v
    end
    return r
end

-- ====== TEST: NESTED LOOPS ======
function test_nested_loops()
    local _ = "@@test_nested_loops"
    local r = 0
    for i = 1, 3 do
        for j = 1, 3 do
            r = i * j
        end
    end
    return r
end

-- ====== TEST: LOOP WITH SKIP PATTERN ======
function test_loop_skip()
    local _ = "@@test_loop_skip"
    local r = 0
    for i = 1, 10 do
        if i == 5 then
            -- skip
        else
            r = i
        end
    end
    return r
end

-- ====== TEST: DO BLOCK (SCOPE) ======
function test_do_block()
    local _ = "@@test_do_block"
    local r
    do
        local scoped = "I'm scoped"
        r = scoped
    end
    return r
end

-- ====== TEST: EMPTY TABLE ======
function test_empty_table()
    local _ = "@@test_empty_table"
    local emptyTable = {}
    return emptyTable
end

-- ====== TEST: ARRAY TABLE ======
function test_array_table()
    local _ = "@@test_array_table"
    local arrayTable = { 1, 2, 3, 4, 5 }
    return arrayTable
end

-- ====== TEST: HASH TABLE ======
function test_hash_table()
    local _ = "@@test_hash_table"
    local hashTable = {
        name = "test",
        value = 42,
        flag = true,
        sub = nil,
    }
    return hashTable
end

-- ====== TEST: MIXED TABLE ======
function test_mixed_table()
    local _ = "@@test_mixed_table"
    local mixedTable = {
        "first",
        "second",
        key1 = "value1",
        key2 = "value2",
        "third",
    }
    return mixedTable
end

-- ====== TEST: NESTED TABLE ======
function test_nested_table()
    local _ = "@@test_nested_table"
    local nestedTable = {
        inner = {
            deep = {
                value = 999,
            },
            list = { 1, 2, 3 },
        },
        flat = "top",
    }
    return nestedTable
end

-- ====== TEST: BRACKET KEYS TABLE ======
function test_bracket_keys()
    local _ = "@@test_bracket_keys"
    local bracketTable = {
        [1] = "one",
        [2] = "two",
        ["string key"] = "value",
        [true] = "bool key",
    }
    return bracketTable
end

-- ====== TEST: EXPRESSION KEYS TABLE ======
function test_expr_keys()
    local _ = "@@test_expr_keys"
    local exprTable = {
        [1 + 1] = "two",
        ["he" .. "llo"] = "greeting",
    }
    return exprTable
end

-- ====== TEST: TABLE ACCESS ======
function test_table_access()
    local _ = "@@test_table_access"
    local hashTable = { name = "test", value = 42 }
    local arrayTable = { 10, 20, 30 }
    local nestedTable = { inner = { deep = { value = 999 } } }
    local dotAccess = hashTable.name
    local bracketAccess = hashTable["name"]
    local numAccess = arrayTable[1]
    local chainAccess = nestedTable.inner.deep.value
    local dynKey = "name"
    local dynAccess = hashTable[dynKey]
    return dotAccess, bracketAccess, numAccess, chainAccess, dynAccess
end

-- ====== TEST: TABLE ASSIGNMENT ======
function test_table_assignment()
    local _ = "@@test_table_assignment"
    local hashTable = { name = "test" }
    local arrayTable = { 1, 2, 3, 4, 5 }
    local nestedTable = { inner = { deep = {} } }
    hashTable.newField = "added"
    hashTable["another"] = "also added"
    arrayTable[6] = 6
    nestedTable.inner.deep.newVal = true
    return hashTable, arrayTable, nestedTable
end

-- ====== TEST: SIMPLE FUNCTION ======
function test_simple_func()
    local _ = "@@test_simple_func"
    local function simpleFunc()
        return 1
    end
    return simpleFunc()
end

-- ====== TEST: FUNCTION WITH PARAMS ======
function test_func_params()
    local _ = "@@test_func_params"
    local function addFunc(x, y)
        return x + y
    end
    return addFunc(3, 4)
end

-- ====== TEST: MULTIPLE RETURNS ======
function test_multi_return()
    local _ = "@@test_multi_return"
    local function multiReturn()
        return 1, 2, 3
    end
    local r1, r2, r3 = multiReturn()
    return r1, r2, r3
end

-- ====== TEST: FUNCTION EXPRESSION ======
function test_func_expr()
    local _ = "@@test_func_expr"
    local funcExpr = function(x)
        return x * 2
    end
    return funcExpr(5)
end

-- ====== TEST: FUNCTION NO RETURN ======
function test_func_no_return()
    local _ = "@@test_func_no_return"
    local r = 0
    local function noReturn(x)
        r = x
    end
    noReturn(42)
    return r
end

-- ====== TEST: RECURSIVE FUNCTION ======
function test_recursive()
    local _ = "@@test_recursive"
    local function factorial(n)
        if n <= 1 then
            return 1
        else
            return n * factorial(n - 1)
        end
    end
    return factorial(5)
end

-- ====== TEST: MUTUAL RECURSION ======
function test_mutual_recursion()
    local _ = "@@test_mutual_recursion"
    local isEven, isOdd
    isEven = function(n)
        if n == 0 then return true end
        return isOdd(n - 1)
    end
    isOdd = function(n)
        if n == 0 then return false end
        return isEven(n - 1)
    end
    return isEven(4), isOdd(3)
end

-- ====== TEST: VARARGS ======
function test_varargs()
    local _ = "@@test_varargs"
    local function varargFunc(...)
        local args = { ... }
        local n = #args
        return n
    end
    local function varargWithFixed(first, second, ...)
        local rest = { ... }
        return first, second, #rest
    end
    local function varargPass(...)
        return varargFunc(...)
    end
    local r1 = varargFunc(1, 2, 3)
    local r2, r3, r4 = varargWithFixed("a", "b", "c", "d")
    local r5 = varargPass(10, 20)
    return r1, r2, r3, r4, r5
end

-- ====== TEST: CLOSURES & UPVALUES ======
function test_closures()
    local _ = "@@test_closures"
    local function makeCounter()
        local count = 0
        local function increment()
            count = count + 1
            return count
        end
        local function decrement()
            count = count - 1
            return count
        end
        local function getCount()
            return count
        end
        return increment, decrement, getCount
    end
    local inc, dec, get = makeCounter()
    inc()
    inc()
    dec()
    return get()
end

-- ====== TEST: NESTED CLOSURES ======
function test_nested_closures()
    local _ = "@@test_nested_closures"
    local function outerFunc(x)
        local function middleFunc(y)
            local function innerFunc(z)
                return x + y + z
            end
            return innerFunc
        end
        return middleFunc
    end
    return outerFunc(1)(2)(3)
end

-- ====== TEST: CLOSURE OVER LOOP VAR ======
function test_closure_loop()
    local _ = "@@test_closure_loop"
    local funcs = {}
    for i = 1, 5 do
        funcs[i] = function()
            return i
        end
    end
    return funcs[1](), funcs[3](), funcs[5]()
end

-- ====== TEST: UPVALUE MODIFICATION ======
function test_upvalue_mod()
    local _ = "@@test_upvalue_mod"
    local function makeAccumulator(init)
        local total = init
        return function(n)
            total = total + n
            return total
        end
    end
    local acc = makeAccumulator(10)
    acc(5)
    return acc(3)
end

-- ====== TEST: METHOD CALLS ======
function test_method_calls()
    local _ = "@@test_method_calls"
    local obj = {}
    obj.name = "myObj"
    obj.getValue = function(self)
        return self.name
    end
    local val1 = obj.getValue(obj)
    local val2 = obj:getValue()
    return val1, val2
end

-- ====== TEST: METHOD DEFINITION ======
function test_method_def()
    local _ = "@@test_method_def"
    local obj = {}
    obj.name = "myObj"
    function obj:setName(newName)
        self.name = newName
    end
    obj:setName("renamed")
    return obj.name
end

-- ====== TEST: SELF PATTERN / CLASS ======
function test_self_pattern()
    local _ = "@@test_self_pattern"
    local MyClass = {}
    function MyClass.new(name)
        local self = {}
        self.name = name
        self.count = 0
        function self:increment()
            self.count = self.count + 1
        end
        function self:getName()
            return self.name
        end
        function self:getCount()
            return self.count
        end
        return self
    end
    local instance = MyClass.new("test")
    instance:increment()
    instance:increment()
    return instance:getName(), instance:getCount()
end

-- ====== TEST: TAILCALL ======
function test_tailcall()
    local _ = "@@test_tailcall"
    local function tailHelper(n, acc)
        if n <= 0 then
            return acc
        end
        return tailHelper(n - 1, acc + n)
    end
    local function tailSum(n)
        return tailHelper(n, 0)
    end
    return tailSum(10)
end

-- ====== TEST: STRING METHODS ======
function test_string_methods()
    local _ = "@@test_string_methods"
    local upper = string.upper("hello")
    local lower = string.lower("HELLO")
    local found = string.find("hello world", "world")
    local formatted = string.format("x=%d y=%s", 10, "test")
    local subbed = string.sub("hello", 1, 3)
    local repped = string.rep("ab", 3)
    local lenStr = string.len("hello")
    return upper, lower, found, formatted, subbed, repped, lenStr
end

-- ====== TEST: TABLE METHODS ======
function test_table_methods()
    local _ = "@@test_table_methods"
    local tbl = { 3, 1, 4, 1, 5 }
    table.sort(tbl)
    table.insert(tbl, 9)
    table.remove(tbl, 1)
    local concat = table.concat(tbl, ", ")
    return tbl, concat
end

-- ====== TEST: MATH METHODS ======
function test_math_methods()
    local _ = "@@test_math_methods"
    local abs = math.abs(-5)
    local floor = math.floor(3.7)
    local ceil = math.ceil(3.2)
    local sqrt = math.sqrt(16)
    local sin = math.sin(0)
    local cos = math.cos(0)
    local max = math.max(1, 2, 3)
    local min = math.min(1, 2, 3)
    local random = math.random(1, 100)
    local pi = math.pi
    local huge = math.huge
    return abs, floor, ceil, sqrt, sin, cos, max, min, random, pi, huge
end

-- ====== TEST: PCALL / ERROR HANDLING ======
function test_pcall()
    local _ = "@@test_pcall"
    local ok, err = pcall(function()
        error("test error")
    end)
    local ok2, val = pcall(function()
        return 42
    end)
    local ok3, err3 = pcall(error, "direct error")
    return ok, err, ok2, val, ok3, err3
end

-- ====== TEST: XPCALL ======
function test_xpcall()
    local _ = "@@test_xpcall"
    local function errorHandler(err)
        return "caught: " .. err
    end
    local ok4, result = xpcall(function()
        error("xpcall test")
    end, errorHandler)
    return ok4, result
end

-- ====== TEST: TYPE CHECKING ======
function test_type_checking()
    local _ = "@@test_type_checking"
    local typeStr = type("hello")
    local typeNum = type(42)
    local typeBool = type(true)
    local typeNil = type(nil)
    local typeTbl = type({})
    local typeFunc = type(print)
    return typeStr, typeNum, typeBool, typeNil, typeTbl, typeFunc
end

-- ====== TEST: TOSTRING / TONUMBER ======
function test_conversions()
    local _ = "@@test_conversions"
    local numToStr = tostring(42)
    local strToNum = tonumber("42")
    local hexToNum = tonumber("FF", 16)
    return numToStr, strToNum, hexToNum
end

-- ====== TEST: SELECT ======
function test_select()
    local _ = "@@test_select"
    local function testSelect(...)
        local n = select("#", ...)
        local first = select(1, ...)
        return n, first
    end
    return testSelect(10, 20, 30)
end

-- ====== TEST: UNPACK ======
function test_unpack()
    local _ = "@@test_unpack"
    local unpacked = { 10, 20, 30 }
    local u1, u2, u3 = unpack(unpacked)
    return u1, u2, u3
end

-- ====== TEST: COMPLEX EXPRESSIONS ======
function test_complex_expr()
    local _ = "@@test_complex_expr"
    local a = 1
    local f = nil
    local obj = { getValue = function(self) return "val" end }
    local ternary = a > 0 and "positive" or "non-positive"
    local nilCoalesce = f or "default"
    local guardedCall = obj and obj.getValue and obj:getValue()
    return ternary, nilCoalesce, guardedCall
end

-- ====== TEST: CHAINED CALLS ======
function test_chained_calls()
    local _ = "@@test_chained_calls"
    local chain1 = string.format("%d", math.floor(3.14))
    local chain2 = tostring(math.abs(math.floor(-3.7)))
    return chain1, chain2
end

-- ====== TEST: MULTILINE EXPRESSION ======
function test_multiline_expr()
    local _ = "@@test_multiline_expr"
    local a = 1
    local b = 2.5
    local g = 10
    local h = 20
    local multiline = a
        + b
        + g
        + h
    return multiline
end

-- ====== TEST: FUNCTION AS TABLE VALUE ======
function test_func_table_value()
    local _ = "@@test_func_table_value"
    local callbacks = {
        onCreate = function(self)
            self.created = true
        end,
        onDestroy = function(self)
            self.created = false
        end,
        onUpdate = function(self, dt)
            self.time = (self.time or 0) + dt
        end,
    }
    local obj = { time = 0 }
    callbacks.onCreate(obj)
    callbacks.onUpdate(obj, 0.16)
    callbacks.onDestroy(obj)
    return obj.created, obj.time
end

-- ====== TEST: IMMEDIATE FUNCTION CALL ======
function test_iife()
    local _ = "@@test_iife"
    local result2 = (function(x, y)
        return x + y
    end)(10, 20)
    return result2
end

-- ====== TEST: NESTED TABLE WITH FUNCTIONS ======
function test_nested_table_funcs()
    local _ = "@@test_nested_table_funcs"
    local config = {
        width = 800,
        height = 600,
        title = "Test",
        callbacks = {
            onInit = function()
                return true
            end,
            onTick = function(dt)
                return dt
            end,
        },
        flags = { true, false, true },
    }
    return config.width, config.callbacks.onInit(), config.callbacks.onTick(0.5)
end

-- ====== TEST: COMPLEX CONTROL FLOW ======
function test_complex_flow()
    local _ = "@@test_complex_flow"
    local function complexFlow(x, y, z)
        local result = 0
        if x > 0 then
            if y > 0 then
                for i = 1, x do
                    for j = 1, y do
                        if i == j then
                            result = result + i
                        elseif i > j then
                            result = result - 1
                        else
                            result = result + 1
                        end
                    end
                end
            else
                local i = 0
                while i < x do
                    result = result + i
                    i = i + 1
                    if result > 100 then
                        break
                    end
                end
            end
        else
            repeat
                result = result + z
                z = z - 1
            until z <= 0 or result > 50
        end
        return result
    end
    return complexFlow(3, 3, 0), complexFlow(5, -1, 0), complexFlow(-1, 0, 5)
end

-- ====== TEST: DEEPLY NESTED CLOSURES ======
function test_deep_closures()
    local _ = "@@test_deep_closures"
    local function deepNest(a)
        local x = a * 2
        return function(b)
            local y = b + x
            return function(c)
                local z = c + y + x
                return function(d)
                    return a + b + c + d + x + y + z
                end
            end
        end
    end
    return deepNest(1)(2)(3)(4)
end

-- ====== TEST: SETLIST (large table) ======
function test_setlist()
    local _ = "@@test_setlist"
    local bigArray = {
        1, 2, 3, 4, 5, 6, 7, 8, 9, 10,
        11, 12, 13, 14, 15, 16, 17, 18, 19, 20,
        21, 22, 23, 24, 25, 26, 27, 28, 29, 30,
        31, 32, 33, 34, 35, 36, 37, 38, 39, 40,
        41, 42, 43, 44, 45, 46, 47, 48, 49, 50,
        51, 52, 53, 54, 55, 56, 57, 58, 59, 60,
    }
    return bigArray, #bigArray
end

-- ====== TEST: TESTSET PATTERN (and/or assignment) ======
function test_testset()
    local _ = "@@test_testset"
    local a = 1
    local b = 2.5
    local c = "test"
    local d = true
    local ts1 = a or b
    local ts2 = a and b
    local ts3 = a and b or c
    local ts4 = (a or b) and (c or d)
    return ts1, ts2, ts3, ts4
end

-- ====== TEST: MULTIPLE ASSIGNMENT ======
function test_multi_assign()
    local _ = "@@test_multi_assign"
    local a = 1
    local b = 2
    local c = 3
    a, b = b, a
    a, b, c = 1, 2, 3
    return a, b, c
end

-- ====== TEST: GLOBAL FUNCTION ======
function test_global_func()
    local _ = "@@test_global_func"
    function globalFunc(x)
        return x + 1
    end
    return globalFunc(10)
end

-- ====== TEST: GLOBAL TABLE FUNCTION ======
function test_global_table_func()
    local _ = "@@test_global_table_func"
    GlobalTable = {}
    function GlobalTable.method(self, x)
        return x
    end
    function GlobalTable:method2(x)
        return self and x
    end
    return GlobalTable.method(GlobalTable, 5), GlobalTable:method2(7)
end

-- ====== TEST: LOADBOOL SKIP PATTERN ======
function test_loadbool()
    local _ = "@@test_loadbool"
    local function boolFunc(x)
        if x > 0 then
            return true
        else
            return false
        end
    end
    return boolFunc(1), boolFunc(-1)
end

-- ====== TEST: CONCAT IN ERROR ======
function test_concat_error()
    local _ = "@@test_concat_error"
    local function concatTest(name, value)
        error("Error: " .. name .. " = " .. tostring(value))
    end
    local ok, err = pcall(concatTest, "x", 42)
    return ok, err
end

-- ====== TEST: MULTIPLE RETURNS AS ARGS ======
function test_multi_return_args()
    local _ = "@@test_multi_return_args"
    local function returnTwo()
        return 1, 2
    end
    local function useMultiReturn()
        local a, b = returnTwo()
        return a + b
    end
    return useMultiReturn()
end

-- ====== TEST: TABLE AS ARGUMENT ======
function test_table_arg()
    local _ = "@@test_table_arg"
    local function takesTable(opts)
        return opts.x + opts.y
    end
    return takesTable({ x = 10, y = 20 })
end

-- ====== TEST: FUNCTION RETURNING FUNCTION ======
function test_func_return_func()
    local _ = "@@test_func_return_func"
    local function adder(x)
        return function(y)
            return x + y
        end
    end
    local add5 = adder(5)
    return add5(3)
end

-- ====== TEST: LOOP WITH CLOSURE CAPTURE ======
function test_loop_closure()
    local _ = "@@test_loop_closure"
    local handlers = {}
    for i = 1, 3 do
        local captured = i * 10
        handlers[i] = function()
            return captured
        end
    end
    return handlers[1](), handlers[2](), handlers[3]()
end

-- ====== TEST: BUILDER PATTERN ======
function test_builder()
    local _ = "@@test_builder"
    local Builder = {}
    function Builder.new()
        local self = {}
        self.items = {}
        function self:add(item)
            table.insert(self.items, item)
            return self
        end
        function self:build()
            return table.concat(self.items, ", ")
        end
        return self
    end
    return Builder.new():add("a"):add("b"):add("c"):build()
end

-- ====== TEST: LOADNIL ======
function test_loadnil()
    local _ = "@@test_loadnil"
    local ln1, ln2, ln3
    ln1, ln2, ln3 = nil, nil, nil
    return ln1, ln2, ln3
end

-- ====== TEST: MOVE (register copy) ======
function test_move()
    local _ = "@@test_move"
    local a = 42
    local mv1 = a
    local mv2 = mv1
    return mv1, mv2
end

-- ====== TEST: SETGLOBAL ======
function test_setglobal()
    local _ = "@@test_setglobal"
    EXPORTED = "exported"
    return EXPORTED
end

-- ============================================
-- CALL ALL TESTS (prevents dead code elimination)
-- ============================================
RESULTS = {}
RESULTS.test = function ()
    print("Running all tests...")
end
RESULTS.globals = { test_globals() }
RESULTS.locals = { test_locals() }
RESULTS.arithmetic = { test_arithmetic() }
RESULTS.string_concat = { test_string_concat() }
RESULTS.comparison = { test_comparison() }
RESULTS.logical = { test_logical() }
RESULTS.unary = { test_unary() }
RESULTS.simple_if = { test_simple_if() }
RESULTS.if_else = { test_if_else() }
RESULTS.if_elseif_else = { test_if_elseif_else() }
RESULTS.nested_if = { test_nested_if() }
RESULTS.complex_conditions = { test_complex_conditions() }
RESULTS.while_loop = { test_while_loop() }
RESULTS.while_break = { test_while_break() }
RESULTS.repeat_until = { test_repeat_until() }
RESULTS.numeric_for = { test_numeric_for() }
RESULTS.numeric_for_step = { test_numeric_for_step() }
RESULTS.generic_for_pairs = { test_generic_for_pairs() }
RESULTS.generic_for_ipairs = { test_generic_for_ipairs() }
RESULTS.nested_loops = { test_nested_loops() }
RESULTS.loop_skip = { test_loop_skip() }
RESULTS.do_block = { test_do_block() }
RESULTS.empty_table = { test_empty_table() }
RESULTS.array_table = { test_array_table() }
RESULTS.hash_table = { test_hash_table() }
RESULTS.mixed_table = { test_mixed_table() }
RESULTS.nested_table = { test_nested_table() }
RESULTS.bracket_keys = { test_bracket_keys() }
RESULTS.expr_keys = { test_expr_keys() }
RESULTS.table_access = { test_table_access() }
RESULTS.table_assignment = { test_table_assignment() }
RESULTS.simple_func = { test_simple_func() }
RESULTS.func_params = { test_func_params() }
RESULTS.multi_return = { test_multi_return() }
RESULTS.func_expr = { test_func_expr() }
RESULTS.func_no_return = { test_func_no_return() }
RESULTS.recursive = { test_recursive() }
RESULTS.mutual_recursion = { test_mutual_recursion() }
RESULTS.varargs = { test_varargs() }
RESULTS.closures = { test_closures() }
RESULTS.nested_closures = { test_nested_closures() }
RESULTS.closure_loop = { test_closure_loop() }
RESULTS.upvalue_mod = { test_upvalue_mod() }
RESULTS.method_calls = { test_method_calls() }
RESULTS.method_def = { test_method_def() }
RESULTS.self_pattern = { test_self_pattern() }
RESULTS.tailcall = { test_tailcall() }
RESULTS.string_methods = { test_string_methods() }
RESULTS.table_methods = { test_table_methods() }
RESULTS.math_methods = { test_math_methods() }
RESULTS.pcall = { test_pcall() }
RESULTS.xpcall = { test_xpcall() }
RESULTS.type_checking = { test_type_checking() }
RESULTS.conversions = { test_conversions() }
RESULTS.select = { test_select() }
RESULTS.unpack_test = { test_unpack() }
RESULTS.complex_expr = { test_complex_expr() }
RESULTS.chained_calls = { test_chained_calls() }
RESULTS.multiline_expr = { test_multiline_expr() }
RESULTS.func_table_value = { test_func_table_value() }
RESULTS.iife = { test_iife() }
RESULTS.nested_table_funcs = { test_nested_table_funcs() }
RESULTS.complex_flow = { test_complex_flow() }
RESULTS.deep_closures = { test_deep_closures() }
RESULTS.setlist = { test_setlist() }
RESULTS.testset = { test_testset() }
RESULTS.multi_assign = { test_multi_assign() }
RESULTS.global_func = { test_global_func() }
RESULTS.global_table_func = { test_global_table_func() }
RESULTS.loadbool = { test_loadbool() }
RESULTS.concat_error = { test_concat_error() }
RESULTS.multi_return_args = { test_multi_return_args() }
RESULTS.table_arg = { test_table_arg() }
RESULTS.func_return_func = { test_func_return_func() }
RESULTS.loop_closure = { test_loop_closure() }
RESULTS.builder = { test_builder() }
RESULTS.loadnil = { test_loadnil() }
RESULTS.move = { test_move() }
RESULTS.setglobal = { test_setglobal() }
