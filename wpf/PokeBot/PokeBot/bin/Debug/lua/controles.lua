joyset = {}

function esperarRandom(n,m) -- espera entre n y m frames
    if n==nil then n = 5 end
    if m==nil then m = 60 end
    frames = math.random(n, m)
    esperar(frames)
end

function esperar(frames) -- espera unos frames 
    for f = 1, frames do
        emu.frameadvance()
    end
end

function resetInput() -- resetea todos los inputs
    for boton, _ in pairs(joyset) do
        joyset[boton] = false
    end
    emu.frameadvance()

end

function pulsarBoton(boton,frames) -- mantiene pulsado un boton unos frames, por defecto solo lo pulsa un momento
    resetInput()
    if frames == nil then frames = 9 end
    for f = 1, frames do
        joyset[boton]=true
        joypad.set(joyset)
        emu.frameadvance()
    end
    resetInput()
end

function tocarPantalla(x,y,frames) -- toca la pantalla en (x,y) durante unos frames
    if frames == nil then frames = 5 end
    for f = 1, frames do
        stylus.set({x = x, y = y, touch = true})
        gui.text(x,y,"+")
        emu.frameadvance()
    end
    resetInput()
end