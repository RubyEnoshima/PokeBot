  -- RubyBot --
-- RubyEnoshima --
require "lua/utilidades"
require "lua/pokemondata"
require "lua/controles"
require "lua/pokefunciones"
json = require "json"



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




function main()
    
end

-- pointer = memory.readdword(0x0211186C) -- USA
pointer = memory.readdword(0x0211188C) -- ESP
resetPointer(pointer)

emu.registerbefore(main)

while true do
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
    
    actuar()
    
    emu.frameadvance()
end