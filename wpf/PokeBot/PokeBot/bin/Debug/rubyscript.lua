-- RubyBot --
require "pokemondata"
json = require "json"

mdword = memory.readdwordunsigned
mword = memory.readword
mbyte = memory.readbyte
rshift = bit.rshift

function tohex(a)
    return string.format("%x", a)
end

if mbyte(0x02FFFE0F) == 0x4A then  -- Check game language
    language = "JPN"
    seedsOffset = 0
    delayOffset = 0
    ivsRngOffset = 0
elseif mbyte(0x02FFFE0F) == 0x45 then
    language = "USA"
    seedsOffset = 0xAC0
    delayOffset = 0xAC0
    ivsRngOffset = 0xACC
elseif mbyte(0x02FFFE0F) == 0x49 then
    language = "ITA"
    seedsOffset = 0xA60
    delayOffset = 0xA60
    ivsRngOffset = 0xA6C
elseif mbyte(0x02FFFE0F) == 0x44 then
    language = "GER"
    seedsOffset = 0xAA0
    delayOffset = 0xAA0
    ivsRngOffset = 0xAAC
elseif mbyte(0x02FFFE0F) == 0x46 then
    language = "FRE"
    seedsOffset = 0xAE0
    delayOffset = 0xAE0
    ivsRngOffset = 0xAEC
elseif mbyte(0x02FFFE0F) == 0x53 then
    language = "SPA"
    seedsOffset = 0xAE0
    delayOffset = 0xAE0
    ivsRngOffset = 0xAEC
elseif mbyte(0x02FFFE0F) == 0x4B then
    language = "KOR"
    seedsOffset = 0x14C0
    delayOffset = 0x14C0
    ivsRngOffset = 0x14A0
end

if mword(0x02FFFE08) == 0x4C50 then  -- Check game version
    game = "Platinum"
elseif mbyte(0x02FFFE08) == 0x44 then
    game = "Diamond"
elseif mbyte(0x02FFFE08) == 0x50 then
    game = "Pearl"
elseif mword(0x02FFFE08) == 0x4748 then
    game = "HeartGold"
elseif mword(0x02FFFE08) == 0x5353 then
    game = "SoulSilver"
    if language == "SPA" then
        seedsOffset = seedsOffset + 0x20
        delayOffset = delayOffset + 0x20
        ivsRngOffset = ivsRngOffset + 0x20
    end
end

idsPointer = 0x021D1768 + seedsOffset
ids = mdword(mdword(idsPointer) + 0x84)
sid = math.floor(ids / 0x10000)
tid = ids % 0x10000

if game ~= "HeartGold" and game ~= "SoulSilver" then
warning = " - Este script solo funciona en HeartGold"
else
warning = ""
end

print("Version: "..game..warning)
print("Idioma: "..language)
print("TID: "..tid)
print("SID: "..sid)


function resetPointer(newpointer)
    pointer = newpointer
    pokepointer = pointer + offsetPokeSalvaje -- primer byte del pokemon salvaje

    PIDaddr = pokepointer + 0x68
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

function esShiny(pid)
    return xor(xor(tid,sid),xor(math.floor(pid / 65536),pid % 65536)) < 8
end

function crearIVs(num)
    local ivs = {}
    ivs["hp"] = num%32
    num = math.floor(num/32)
    ivs["att"] = num%32
    num = math.floor(num/32)
    ivs["def"] = num%32
    num = math.floor(num/32)
    ivs["speed"] = num%32
    num = math.floor(num/32)
    ivs["spatt"] = num%32
    num = math.floor(num/32)
    ivs["spdef"] = num%32
    return ivs
end

function crearPoke(pid)
    local pokemon = {}
    pokemon["pid"] = tohex(pid)
    pokemon["id"] = memory.readword(pokepointer)
    pokemon["nombre"] = table["pokemon"][memory.readword(pokepointer)]
    pokemon["shiny"] = esShiny(pid) and 1 or 0
    pokemon["nivel"] = memory.readbyte(pokepointer+0x34)
    pokemon["movimientos"] = {}
    pokemon["movimientos"][1] = memory.readword(pokepointer+0x0C)
    pokemon["movimientos"][2] = memory.readword(pokepointer+0x0E)
    pokemon["movimientos"][3] = memory.readword(pokepointer+0x10)
    pokemon["movimientos"][4] = memory.readword(pokepointer+0x12)
    pokemon["habilidad"] = mbyte(pokepointer+0x27)
    pokemon["ivs"] = crearIVs(memory.readdword(pokepointer+0x14));
    pokemon["genero"] = mbyte(pokepointer+0x7E);
    return pokemon
end

antPID = -1
function main()
    newpointer = memory.readdword(0x0211188C) -- por si acaso reseteamos el emulador
    if newpointer ~= pointer then
        resetPointer(newpointer)
    end
    if memory.readword(0x021DA704) == 16384 then -- si estamos en combate
        pid = memory.readdword(PIDaddr)
        if pid ~= antPID then
            poke = crearPoke(pid)
            file = io.open("poke.json", "w+")
            io.output(file)
            pokejson = json.encode(poke)
            io.write(pokejson)
            io.close()
            antPID = pid
        end

    end 
end

-- pointer = memory.readdword(0x0211186C) -- USA
pointer = memory.readdword(0x0211188C) -- ESP
offsetPokeSalvaje = 0x56EB4
resetPointer(pointer)

gui.register(main)
