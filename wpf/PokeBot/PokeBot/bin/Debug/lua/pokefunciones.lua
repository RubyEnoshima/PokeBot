pointerESP = 0x0211188C
pointerUSA = 0x0211186C
offsetPokeSalvaje = 0x56EB4

pausado = false
mode = 1 -- 0: nada; 1: spin; 2: static (Suicune, Lugia...)

function resetPointer(newpointer) -- Resetea el puntero y encuentra la dirección que tenemos que mirar para obtener el PID
    pointer = newpointer
    pokepointer = pointer + offsetPokeSalvaje -- primer byte del pokemon salvaje

    PIDaddr = pokepointer + 0x68
end

function sv(pid) -- Devuelve el shiny value (sv)
    return xor(xor(tid,sid),xor(math.floor(pid / 65536),pid % 65536))
end

function esShiny(pid) -- Devuelve true si el pid coincide con un pkm shiny
    return sv(pid) < 8 -- es shiny si sv < 8
end

function crearIVs(num) -- Devuelve una array con los IVs extraidos del numero
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

function crearPoke(pid) -- Devuelve el pokemon enemigo con toda la información que se puede obtener de él
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

function escribirArchivo(poke) -- Escribe en poke.json la información 
    local file, error_message = io.open("poke.json", "w+")

    if not file then
        print("Error opening file: " .. error_message)
    else
        local pokejson = json.encode(poke)

        local success, write_error = pcall(function()
            file:write(pokejson)
        end)

        if not success then
            print("Error writing to file: " .. write_error)
            antPID = -1
        end

        file:close()
    end
end

function enCombate() -- Devuelve true si el juego está en un combate
    return memory.readword(0x021DA704) == 16384
end

dir = 0
function buscarCombate() -- Se mueve en circulos hasta que encuentra un combate
    dir = dir + 1
    esperar(2)
    while enCombate()==false and pausado==false do
        if dir == 0 then pulsarBoton("up",3)
        elseif dir == 1 then pulsarBoton("left",3)
        elseif dir == 2 then pulsarBoton("down",3)
        else pulsarBoton("right",3) end
        esperar(2)
        dir = dir + 1
        if dir >= 4 then dir = 0 end
    end
end

function huir()
    if enCombate() then
        tocarPantalla(125,170)
    end
end

antmode = mode
antPID = -1
function pokeSalvaje()
    if enCombate()==false then buscarCombate() end -- Buscamos combate si no estamos en uno

    pid = memory.readdword(PIDaddr) -- Leemos el PID del poke enemigo
    if pid ~= antPID then
        poke = crearPoke(pid)
        escribirArchivo(poke) -- Escribimos en un archivo la info
        antPID = pid
    end

    
    if esShiny(pid) then
        print("Es shiny!")
        mode = 0 -- Pausar, temporal
    else
        -- gui.text(157,-170,"No es shiny","black","white")
        huir()
    end
end

function pokeLegend()
    -- emu.reset()
end

function actuar() -- Decide el procedimiento que hay que seguir segun un modo u otro
    if pausado then 
        huir()
    elseif mode == 1 then -- Poke salvaje
        pokeSalvaje()
    elseif mode == 2 then -- Poke estático
        pokeLegend()
    elseif enCombate() == false then -- TODO: PODER DECIDIR QUÉ HACER CUANDO SE SALE DEL COMBATE: PARAR / SEGUIR BUSCANDO
        mode = antmode 
    end
end

function actualizarBot()
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

    -- hacerlo en otro thread
    nuevaConfig = leerArchivo("bot.json")
    pausado = nuevaConfig["pausado"]
end