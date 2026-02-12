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


-- full_anchor(20, 20, 20, 20)
-- Control bleibt 20px Abstand zu allen Seiten
-- wächst mit dem Fenster
function util.full_anchor(x1, y1, x2, y2)
    -- x1,y1 = Abstand zur linken/oberen Ecke
    -- x2,y2 = Abstand zur rechten/unteren Ecke

    return function()
        local winW, winH = get_window_size()

        return {
            x = x1,
            y = y1,
            width = winW - x1 - x2,
            height = winH - y1 - y2
        }
    end
end


function util.dock(mode, margin)
    margin = margin or 0

    return function()
        local winW, winH = get_window_size()

        if mode == "fill" then
            return { x = margin, y = margin, width = winW - margin*2, height = winH - margin*2 }
        end

        if mode == "top" then
            return { x = margin, y = margin, width = winW - margin*2, height = margin }
        end

        if mode == "bottom" then
            return { x = margin, y = winH - margin*2, width = winW - margin*2, height = margin }
        end

        if mode == "left" then
            return { x = margin, y = margin, width = margin, height = winH - margin*2 }
        end

        if mode == "right" then
            return { x = winW - margin*2, y = margin, width = margin, height = winH - margin*2 }
        end
    end
end


-- util.scale(0.1, 0.1, 0.8, 0.8)
-- Control nimmt immer 80% der Fenstergröße ein
-- bleibt 10% vom Rand entfernt
function util.scale(x, y, w, h)
    return function()
        local winW, winH = get_window_size()
        return {
            x = winW * x,
            y = winH * y,
            width = winW * w,
            height = winH * h
        }
    end
end


return util






