local current_skin = {}

function load(name)
    assert(type(name) == "string", "Theme name must be a string")

    local path = SKIN_DIR .. "/" .. name .. ".lua"
    local ok, result = pcall(dofile, path)

    if ok and type(result) == "table" then
        current_skin = result
    else
        print("Failed to load theme:", result)
        current_skin = {}
    end
end

function get()
    return current_skin
end
