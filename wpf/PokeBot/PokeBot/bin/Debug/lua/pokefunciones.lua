pointerESP = 0x0211188C
pointerUSA = 0x0211186C
offsetPokeSalvaje = 0x56EB4

pausado = false
mode = 1 -- 0: nada; 1: spin; 2: static (Suicune, Lugia...)

-- Estado para reactivar la radio tras salir de combate
estabaEnCombate = false
activarRadioPendiente = false

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

-- Lee si la radio está activa desde config.json o bot.json.
-- Prioriza config.json porque en la app actual el toggle de radio suele guardarse ahí.
function radioActivaBot()
    local cfg = leerArchivo("config.json")
    if cfg ~= nil and cfg["radio_activa"] ~= nil then
        return cfg["radio_activa"] == true
    end

    local bot = leerArchivo("bot.json")
    if bot ~= nil and bot["radio_activa"] ~= nil then
        return bot["radio_activa"] == true
    end

    return false
end

-- Placeholder para el futuro, por si más adelante consigues un puntero RAM fiable
-- que indique que el Pokégear está abierto.
-- Sustituir "puterpokegearxxxxxx" por un puntero/flag real de la RAM del juego
-- que indique "menú Pokégear abierto".
function enPokegear()
    -- Ejemplo futuro:
    -- return memory.readbyte(puterpokegearxxxxxx) == 1
    return false
end

-- Detecta la transición "estaba en combate" -> "ya no está en combate"
-- y marca una reactivación pendiente de la radio.
function actualizarEstadoCombate()
    local ahoraEnCombate = enCombate()

    if estabaEnCombate == true and ahoraEnCombate == false then
        if radioActivaBot() then
            activarRadioPendiente = true
        end
    end

    estabaEnCombate = ahoraEnCombate
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

-- Reactiva la radio dentro del juego.
-- Versión actual: "ciega" pero funcional, sin puntero RAM obligatorio.
-- Si en el futuro quieres algo 100% fiable, usa enPokegear() con un puntero real.
function activarRadioEnJuego()
    if not radioActivaBot() then return end
    if enCombate() then return end

    -- Toca la zona del Pokégear hasta intentar abrirlo
    local intentos = 0
    while intentos < 12 and not enCombate() do
        tocarPantalla(42,155,6)
        esperar(6)
        intentos = intentos + 1
    end

    -- FUTURO OPCIONAL:
    -- Si más adelante tenemos un puntero RAM fiable para detectar el Pokégear,
    -- se podría hacer algo así:
    --
    -- local intentos = 0
    -- while not enPokegear() and intentos < 10 do
    --     tocarPantalla(42,155,5)
    --     esperar(5)
    --     intentos = intentos + 1
    -- end

    -- Sale del Pokégear pulsando B
    local intentosB = 0
    while intentosB < 8 do
        pulsarBoton("B",10)
        esperar(6)
        intentosB = intentosB + 1
    end

end

-- Si la reactivación está pendiente y estamos fuera de combate, la ejecuta.
function procesarRadioPendiente()
    if activarRadioPendiente and not enCombate() then
        activarRadioEnJuego()
        activarRadioPendiente = false
    end
end

antmode = mode
antPID = -1

function comprobarPoke()
    pid = memory.readdword(PIDaddr) -- Leemos el PID del poke enemigo
    if pid ~= antPID then
        poke = crearPoke(pid)
        escribirArchivo(poke) -- Escribimos en un archivo la info
        antPID = pid
    end
    return pid
end

function pokeSalvaje()
    if enCombate()==false then
        procesarRadioPendiente()
        buscarCombate()
        return
    end -- Buscamos combate si no estamos en uno

    pid = comprobarPoke()
    
    if esShiny(pid) then
        print("Es shiny!")
        mode = 0 -- Pausar, temporal
    else
        -- gui.text(157,-170,"No es shiny","black","white")
        huir()
    end
end

pids = {}
function pokeLegend()
    if enCombate()==false then 
        procesarRadioPendiente()
        esperarRandom(1,300)
        pulsarBoton("A",2)
    else
        pid = comprobarPoke()

        if pids[pid] then
            print(pid .. " repetido")
        else
            -- Añadir el PID a la tabla
            pids[pid] = true
        end
        

        if esShiny(pid) then
            print("Es shiny!")
            mode = 0 -- Pausar, temporal
        else
            -- gui.text(157,-170,"No es shiny","black","white")
            emu.reset()
            esperarRandom(1,2000)
        end

    end
end

function actuar() -- Decide el procedimiento que hay que seguir segun un modo u otro
    actualizarEstadoCombate()

    if pausado then 
        huir()
    elseif mode == 1 then -- Poke salvaje
        pokeSalvaje()
    elseif mode == 2 then -- Poke estático
        pokeLegend()
    elseif enCombate() == false then -- TODO: PODER DECIDIR QUÉ HACER CUANDO SE SALE DEL COMBATE: PARAR / SEGUIR BUSCANDO
        procesarRadioPendiente()
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
    if nuevaConfig ~= nil and nuevaConfig["pausado"] ~= nil then
        pausado = nuevaConfig["pausado"]
    end
end