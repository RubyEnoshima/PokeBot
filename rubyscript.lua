

mdword = memory.readdwordunsigned
mword = memory.readword
mbyte = memory.readbyte
rshift = bit.rshift

function tohex(a)
    return string.format("0x%x", a)
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
print(tohex(seedsOffset))
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

-- pointer = memory.readdword(0x0211186C) -- USA
pointer = memory.readdword(0x0211188C) -- ESP
pokepointer = pointer + 0x56EB4 -- primer byte del pokemon salvaje
PIDaddr = pointer + 0x56F1C-- offset para el pid generado: 0x38540

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

function esShiny()
    return xor(xor(tid,sid),xor(math.floor(pid / 65536),pid % 65536)) < 8
end


function main()
    pid = memory.readdword(PIDaddr)
    gui.text(2, 1, esShiny())
    gui.text(2, 11, pid)
    gui.text(2, 21, tohex(pid))
end

print(string.format("%x",pokepointer))
print("HP: "..memory.readword(pointer+0x56F00))
gui.register(main)


-- function main()
--     gui.text(0, 150, string.format("Frame: %d", frame))
-- end
-- emu.reset()