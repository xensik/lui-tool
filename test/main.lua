-- ============================================
-- HavokScript / Lua 5.1 Comprehensive Test
-- ============================================

-- ====== GLOBALS ======
globalVar = 1
globalString = "hello"
globalBool = true
globalNil = nil

-- ====== LOCAL VARIABLES ======
local a = 1
local b = 2.5
local c = "test"
local d = true
local e = false
local f = nil
local g, h = 10, 20
local i, j, k = 1, "two", false

-- ====== ARITHMETIC ======
local add = a + b
local sub = a - b
local mul = a * b
local div = a / b
local mod = a % b
local pow = a ^ b
local neg = -a
local complex = (a + b) * (c and 1 or 0) - d / e ^ 2

-- ====== STRING CONCAT ======
local str1 = "hello" .. " " .. "world"
local str2 = "num: " .. 42
local str3 = a .. b .. c

-- ====== COMPARISON ======
local eq = a == b
local ne = a ~= b
local lt = a < b
local gt = a > b
local le = a <= b
local ge = a >= b

-- ====== LOGICAL ======
local land = a and b
local lor = a or b
local lnot = not a
local chain = a and b or c
local shortA = false and "nope"
local shortB = nil or "fallback"
local tripleAnd = a and b and c
local tripleOr = a or b or c

-- ====== UNARY ======
local len = #c
local negation = -a
local negBool = not true

-- ====== SIMPLE IF ======
if a then
    globalVar = 100
end

-- ====== IF-ELSE ======
if a > b then
    globalVar = 1
else
    globalVar = 2
end

-- ====== IF-ELSEIF-ELSE ======
if a == 1 then
    globalVar = "one"
elseif a == 2 then
    globalVar = "two"
elseif a == 3 then
    globalVar = "three"
else
    globalVar = "other"
end

-- ====== NESTED IF ======
if a then
    if b then
        if c then
            globalVar = "deep"
        end
    else
        globalVar = "shallow"
    end
end

-- ====== IF WITH COMPLEX CONDITIONS ======
if a > 0 and b < 10 then
    globalVar = "both"
end

if a > 0 or b < 10 then
    globalVar = "either"
end

if not (a > 0) then
    globalVar = "negated"
end

if (a == 1 or a == 2) and (b == 3 or b == 4) then
    globalVar = "complex"
end

-- ====== WHILE LOOP ======
local counter = 0
while counter < 10 do
    counter = counter + 1
end

-- ====== WHILE WITH BREAK ======
local x = 0
while true do
    x = x + 1
    if x > 5 then
        break
    end
end

-- ====== REPEAT-UNTIL ======
local y = 0
repeat
    y = y + 1
until y >= 10

-- ====== NUMERIC FOR ======
local sum = 0
for i = 1, 10 do
    sum = sum + i
end

-- ====== NUMERIC FOR WITH STEP ======
local sum2 = 0
for i = 10, 1, -1 do
    sum2 = sum2 + i
end

-- ====== NUMERIC FOR WITH STEP 2 ======
local sum3 = 0
for i = 0, 100, 5 do
    sum3 = sum3 + i
end

-- ====== GENERIC FOR (PAIRS) ======
local t = { 1, 2, 3 }
for k, v in pairs(t) do
    globalVar = k + v
end

-- ====== GENERIC FOR (IPAIRS) ======
for i, v in ipairs(t) do
    globalVar = i * v
end

-- ====== NESTED LOOPS ======
for i = 1, 3 do
    for j = 1, 3 do
        globalVar = i * j
    end
end

-- ====== LOOP WITH CONTINUE-LIKE PATTERN (break in nested if) ======
for i = 1, 10 do
    if i == 5 then
        -- skip
    else
        globalVar = i
    end
end

-- ====== DO BLOCK (SCOPE) ======
do
    local scoped = "I'm scoped"
    globalVar = scoped
end

-- ====== EMPTY TABLE ======
local emptyTable = {}

-- ====== ARRAY TABLE ======
local arrayTable = { 1, 2, 3, 4, 5 }

-- ====== HASH TABLE ======
local hashTable = {
    name = "test",
    value = 42,
    flag = true,
    sub = nil,
}

-- ====== MIXED TABLE ======
local mixedTable = {
    "first",
    "second",
    key1 = "value1",
    key2 = "value2",
    "third",
}

-- ====== NESTED TABLE ======
local nestedTable = {
    inner = {
        deep = {
            value = 999,
        },
        list = { 1, 2, 3 },
    },
    flat = "top",
}

-- ====== TABLE WITH BRACKET KEYS ======
local bracketTable = {
    [1] = "one",
    [2] = "two",
    ["string key"] = "value",
    [true] = "bool key",
}

-- ====== TABLE WITH EXPRESSION KEYS ======
local exprTable = {
    [1 + 1] = "two",
    ["he" .. "llo"] = "greeting",
}

-- ====== TABLE ACCESS ======
local dotAccess = hashTable.name
local bracketAccess = hashTable["name"]
local numAccess = arrayTable[1]
local chainAccess = nestedTable.inner.deep.value
local dynKey = "name"
local dynAccess = hashTable[dynKey]

-- ====== TABLE ASSIGNMENT ======
hashTable.newField = "added"
hashTable["another"] = "also added"
arrayTable[6] = 6
nestedTable.inner.deep.newVal = true

-- ====== SIMPLE FUNCTION ======
local function simpleFunc()
    return 1
end

-- ====== FUNCTION WITH PARAMS ======
local function addFunc(x, y)
    return x + y
end

-- ====== FUNCTION WITH MULTIPLE RETURNS ======
local function multiReturn()
    return 1, 2, 3
end

local r1, r2, r3 = multiReturn()

-- ====== FUNCTION AS EXPRESSION ======
local funcExpr = function(x)
    return x * 2
end

-- ====== FUNCTION WITH NO RETURN ======
local function noReturn(x)
    globalVar = x
end

-- ====== RECURSIVE FUNCTION ======
local function factorial(n)
    if n <= 1 then
        return 1
    else
        return n * factorial(n - 1)
    end
end

-- ====== MUTUAL RECURSION ======
local isEven, isOdd
isEven = function(n)
    if n == 0 then return true end
    return isOdd(n - 1)
end
isOdd = function(n)
    if n == 0 then return false end
    return isEven(n - 1)
end

-- ====== VARARGS ======
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

-- ====== CLOSURES & UPVALUES ======
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

-- ====== NESTED CLOSURES ======
local function outerFunc(x)
    local function middleFunc(y)
        local function innerFunc(z)
            return x + y + z
        end
        return innerFunc
    end
    return middleFunc
end

-- ====== CLOSURE OVER LOOP VARIABLE ======
local funcs = {}
for i = 1, 5 do
    funcs[i] = function()
        return i
    end
end

-- ====== UPVALUE MODIFICATION ======
local function makeAccumulator(init)
    local total = init
    return function(n)
        total = total + n
        return total
    end
end

-- ====== METHOD CALLS ======
local obj = {}
obj.name = "myObj"
obj.getValue = function(self)
    return self.name
end

local val1 = obj.getValue(obj)
local val2 = obj:getValue()

-- ====== METHOD DEFINITION ======
function obj:setName(newName)
    self.name = newName
end

obj:setName("renamed")

-- ====== SELF PATTERN ======
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
local instName = instance:getName()
local instCount = instance:getCount()

-- ====== TAILCALL ======
local function tailHelper(n, acc)
    if n <= 0 then
        return acc
    end
    return tailHelper(n - 1, acc + n)
end

local function tailSum(n)
    return tailHelper(n, 0)
end

-- ====== STRING METHODS ======
local upper = string.upper("hello")
local lower = string.lower("HELLO")
local found = string.find("hello world", "world")
local formatted = string.format("x=%d y=%s", 10, "test")
local subbed = string.sub("hello", 1, 3)
local repped = string.rep("ab", 3)
local lenStr = string.len("hello")

-- ====== TABLE METHODS ======
local tbl = { 3, 1, 4, 1, 5 }
table.sort(tbl)
table.insert(tbl, 9)
table.remove(tbl, 1)
local concat = table.concat(tbl, ", ")

-- ====== MATH METHODS ======
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

-- ====== PCALL / ERROR HANDLING ======
local ok, err = pcall(function()
    error("test error")
end)

local ok2, val = pcall(function()
    return 42
end)

local ok3, err3 = pcall(error, "direct error")

-- ====== XPCALL ======
local function errorHandler(err)
    return "caught: " .. err
end

local ok4, result = xpcall(function()
    error("xpcall test")
end, errorHandler)

-- ====== TYPE CHECKING ======
local typeStr = type("hello")
local typeNum = type(42)
local typeBool = type(true)
local typeNil = type(nil)
local typeTbl = type({})
local typeFunc = type(print)

-- ====== TOSTRING / TONUMBER ======
local numToStr = tostring(42)
local strToNum = tonumber("42")
local hexToNum = tonumber("FF", 16)

-- ====== SELECT ======
local function testSelect(...)
    local n = select("#", ...)
    local first = select(1, ...)
    return n, first
end

-- ====== UNPACK ======
local unpacked = { 10, 20, 30 }
local u1, u2, u3 = unpack(unpacked)

-- ====== COMPLEX EXPRESSIONS ======
local ternary = a > 0 and "positive" or "non-positive"
local nilCoalesce = f or "default"
local guardedCall = obj and obj.getValue and obj:getValue()

-- ====== CHAINED TABLE ACCESS & CALLS ======
local chain1 = string.format("%d", math.floor(3.14))
local chain2 = tostring(math.abs(math.floor(-3.7)))

-- ====== MULTILINE EXPRESSIONS ======
local multiline = a
    + b
    + g
    + h

-- ====== FUNCTION AS TABLE VALUE ======
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

-- ====== IMMEDIATE FUNCTION CALL ======
local result2 = (function(x, y)
    return x + y
end)(10, 20)

-- ====== NESTED TABLE CONSTRUCTOR WITH FUNCTIONS ======
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

-- ====== COMPLEX CONTROL FLOW ======
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

-- ====== DEEPLY NESTED CLOSURES WITH UPVALS ======
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

-- ====== SETLIST (large table > LFIELDS_PER_FLUSH) ======
local bigArray = {
    1, 2, 3, 4, 5, 6, 7, 8, 9, 10,
    11, 12, 13, 14, 15, 16, 17, 18, 19, 20,
    21, 22, 23, 24, 25, 26, 27, 28, 29, 30,
    31, 32, 33, 34, 35, 36, 37, 38, 39, 40,
    41, 42, 43, 44, 45, 46, 47, 48, 49, 50,
    51, 52, 53, 54, 55, 56, 57, 58, 59, 60,
}

-- ====== TESTSET PATTERN (and/or assignment) ======
local ts1 = a or b
local ts2 = a and b
local ts3 = a and b or c
local ts4 = (a or b) and (c or d)

-- ====== MULTIPLE ASSIGNMENT ======
a, b = b, a
a, b, c = 1, 2, 3

-- ====== GLOBAL FUNCTION ======
function globalFunc(x)
    return x + 1
end

-- ====== NESTED GLOBAL TABLE FUNCTION ======
GlobalTable = {}
function GlobalTable.method(self, x)
    return x
end

function GlobalTable:method2(x)
    return self and x
end

-- ====== LOADBOOL SKIP PATTERN ======
local function boolFunc(x)
    if x > 0 then
        return true
    else
        return false
    end
end

-- ====== CONCAT IN VARIOUS CONTEXTS ======
local function concatTest(name, value)
    error("Error: " .. name .. " = " .. tostring(value))
end

-- ====== MULTIPLE RETURNS USED AS ARGS ======
local function returnTwo()
    return 1, 2
end

local function useMultiReturn()
    local a, b = returnTwo()
    return a + b
end

-- ====== TABLE AS ARGUMENT ======
local function takesTable(opts)
    return opts.x + opts.y
end

local res = takesTable({ x = 10, y = 20 })

-- ====== FUNCTION RETURNING FUNCTION ======
local function adder(x)
    return function(y)
        return x + y
    end
end

local add5 = adder(5)
local result3 = add5(3)

-- ====== LOOP WITH CLOSURE CAPTURE ======
local handlers = {}
for i = 1, 3 do
    local captured = i * 10
    handlers[i] = function()
        return captured
    end
end

-- ====== COMPLEX METHOD CHAIN PATTERN ======
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

local built = Builder.new():add("a"):add("b"):add("c"):build()

-- ====== LOADNIL ======
local ln1, ln2, ln3
ln1, ln2, ln3 = nil, nil, nil

-- ====== MOVE (register copy) ======
local mv1 = a
local mv2 = mv1

-- ====== SETGLOBAL ======
EXPORTED = localVar or "exported"
