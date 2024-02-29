mdword = memory.readdwordunsigned
mword = memory.readword
mbyte = memory.readbyte
rshift = bit.rshift

function tohex(a)
    return string.format("%x", a)
end

function xor(a,b)
    local result = 0
    local bit = 1

    while a > 0 or b > 0 do
        local bitA = a % 2
        local bitB = b % 2

        if bitA ~= bitB then
            result = result + bit
        end

        a = math.floor(a / 2)
        b = math.floor(b / 2)
        bit = bit * 2
    end

    return result
end

function leerArchivo(path)
    local file = io.open(path, "r")
    if not file then return nil end
    local content = file:read "*a"
    file:close()
    return json.decode(content)
end