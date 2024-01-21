-- RubyBot --
require "pokemondata"
json = require "json"

local clock = os.clock
function sleep(n)-- seconds
local t0 = clock()
while clock() - t0 <= n do emu.frameadvance() end
end

mdword = memory.readdwordunsigned
mword = memory.readword
mbyte = memory.readbyte
rshift = bit.rshift

mode = 2 -- 0: nada; 1: spin; 2: static (Suicune, Lugia...)

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
if tid ~= 0 and sid ~= 0 then
    print("TID: "..tid)
    print("SID: "..sid)
end

offsetPokeSalvaje = 0x56EB4
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

function sv(pid) -- Devuelve el shiny value (sv)
    return xor(xor(tid,sid),xor(math.floor(pid / 65536),pid % 65536))
end

function esShiny(pid)
    return sv(pid) < 8 -- es shiny si sv < 8
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
    pokemon["sv"] = sv(pid)
    return pokemon
end

function enCombate()
    return memory.readword(0x021DA704) == 16384
end

antmode = mode
orden = 0
alt = 0
function actuar(pid) -- Actúa acorde al mode actual
    joyset = {}

    if mode == 1 then -- Poke salvaje
        if enCombate()==false then 
            if orden >= 1 and orden < 4 then
                joyset.up = true
            elseif orden >= 4 and orden < 7 then
                joyset.right = true
            elseif orden >= 7 and orden < 10 then
                joyset.down = true
            elseif orden >= 10 and orden < 12 then
                joyset.left = true
            end
            orden = orden + 1
            if orden == 12 then orden = 0 end -- para que tenga tiempo para pensar
        else
            if esShiny(pid) then
                mode = 0
            else
                gui.text(157,-170,"No es shiny","black","white")
                if alt <= 4 then 
                    stylus.set({x = 125, y = 170, touch = true})
                    gui.text(125,170,"+")
            
                else 
                    stylus.set({x = 125, y = 170, touch = false}) 

                end
                alt = alt + 1
                if alt == 10 then alt = 0 end
            end
            orden = 0
            joyset.up = false
            joyset.down = false
            joyset.right = false
            joyset.left = false
        end
    elseif mode == 2 then -- Poke estático
        if enCombate()==false then 
            if orden >= 0 and orden <= 2 then
                joyset.up = true
            elseif orden > 2 and orden <= 4 then
                joyset.A = true
            end
            orden = orden + 1
            if orden == 5 then orden = 0 end
        else
            if esShiny(pid) then
                mode = 0
            else
                emu.reset()
            end
        end
    elseif enCombate()==false then -- volver al modo anterior si hemos salido del combate y era shiny (mode==0)
        mode = antmode
    end
    
    joypad.set(joyset)
end

antPID = -1
function main()
    if tid == 0 and sid == 0 then
        ids = mdword(mdword(idsPointer) + 0x84)
        sid = math.floor(ids / 0x10000)
        tid = ids % 0x10000
        if tid ~= 0 and sid ~= 0 then
            print("TID: "..tid)
            print("SID: "..sid)
        end
    end
    newpointer = memory.readdword(0x0211188C) -- por si acaso reseteamos el emulador
    if newpointer ~= pointer then
        resetPointer(newpointer)
    end
    if enCombate() then -- si estamos en combate
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

    actuar(pid)
end

-- pointer = memory.readdword(0x0211186C) -- USA
pointer = memory.readdword(0x0211188C) -- ESP
resetPointer(pointer)

emu.registerbefore(main)
