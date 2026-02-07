local util = {}

function util.safe_get(t, key, default)
  if t == nil then return default end
  local ok, v = pcall(function() return t[key] end)
  if not ok then return default end
  if v == nil then return default end
  return v
end



function util.make_bounds(x, y, w, h)
  return function()
    local width, height = get_window_size()
    local rx = (x < 0) and (width + x) or x
    local ry = (y < 0) and (height + y) or y
    return { x = rx, y = ry, width = w, height = h }
  end
end

-- Generische Anchor-Funktion
-- mode: "top_left", "top_right", "bottom_left", "bottom_right"
function util.make_anchor(x, y, w, h, mode)
  mode = mode or "top_left"

  return function()
    local winW, winH = get_window_size()

    local rx, ry, rw, rh

    -- Breite
    if w < 0 then
      rw = winW + w - ((x < 0) and (winW + x) or x)
    else
      rw = w
    end

    -- Höhe
    if h < 0 then
      rh = winH + h - ((y < 0) and (winH + y) or y)
    else
      rh = h
    end

    if mode == "top_left" then
      rx = (x < 0) and (winW + x) or x
      ry = (y < 0) and (winH + y) or y

    elseif mode == "top_right" then
      rx = winW - ((x < 0) and -x or x) - rw
      ry = (y < 0) and (winH + y) or y

    elseif mode == "bottom_left" then
      rx = (x < 0) and (winW + x) or x
      ry = winH - ((y < 0) and -y or y) - rh

    elseif mode == "bottom_right" then
      rx = winW - ((x < 0) and -x or x) - rw
      ry = winH - ((y < 0) and -y or y) - rh
    end

    return { x = rx, y = ry, width = rw, height = rh }
  end
end


return util






