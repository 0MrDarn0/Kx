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


local function parseSize(size, total, margin)
    if not size then return nil end
    if type(size) == "string" and size:match("%%$") then
        local p = tonumber(size:sub(1, -2)) or 0
        return math.floor((total - margin*2) * (p / 100))
    end
    return tonumber(size)
end

function util.dock(mode, margin, offsetX, offsetY, sizeW, sizeH, anchorX, anchorY)
    margin = margin or 0
    offsetX = offsetX or 0
    offsetY = offsetY or 0
    anchorX = anchorX or "left"    -- "left", "center", "right"
    anchorY = anchorY or "bottom"  -- "top", "center", "bottom"

    return function()
        local winW, winH = get_window_size()

        local w = parseSize(sizeW, winW, margin) or (winW - margin*2)
        local h = parseSize(sizeH, winH, margin) or margin

        -- X nach anchorX
        local x
        if anchorX == "center" then
            x = math.floor((winW - w) / 2) + offsetX
        elseif anchorX == "right" then
            x = winW - margin - w + offsetX
        else -- left
            x = margin + offsetX
        end

        -- Y nach anchorY und mode
        local y
        if anchorY == "center" then
            y = math.floor((winH - h) / 2) + offsetY
        elseif anchorY == "top" then
            y = margin + offsetY
        else -- bottom
            y = winH - margin - h + offsetY
        end

        -- Schutz: nicht außerhalb
        if x < 0 then x = 0 end
        if y < 0 then y = 0 end
        if x + w > winW then x = winW - w end
        if y + h > winH then y = winH - h end

        return { x = x, y = y, width = w, height = h }
    end
end



function util.rect(x, y, w, h)
    return function() return { x = x, y = y, width = w, height = h } end
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



-- sizeWPercent, sizeHPercent: Werte zwischen 0 und 1 (z. B. 0.30 = 30%)
-- offsetX, offsetY: Pixel-Offsets relativ zum Anchor
-- anchor: "bottom_left", "bottom_right", "top_left", "center", ...
function util.relative_anchor(sizeWPercent, sizeHPercent, offsetX, offsetY, anchor)
  sizeWPercent = sizeWPercent or 0.3
  sizeHPercent = sizeHPercent or 0.15
  offsetX = offsetX or 0
  offsetY = offsetY or 0
  anchor = anchor or "bottom_left"

  return function()
    local winW, winH = get_window_size()
    local w = math.floor(winW * sizeWPercent)
    local h = math.floor(winH * sizeHPercent)

    local x, y

    if anchor == "bottom_left" then
      x = 0 + offsetX
      y = winH - h + offsetY
    elseif anchor == "bottom_right" then
      x = winW - w + offsetX
      y = winH - h + offsetY
    elseif anchor == "top_left" then
      x = 0 + offsetX
      y = 0 + offsetY
    elseif anchor == "center" then
      x = math.floor((winW - w) / 2) + offsetX
      y = math.floor((winH - h) / 2) + offsetY
    else
      -- fallback
      x = 0 + offsetX
      y = winH - h + offsetY
    end

    -- Clamp: bleibt im Fenster
    if x < 0 then x = 0 end
    if y < 0 then y = 0 end
    if x + w > winW then x = winW - w end
    if y + h > winH then y = winH - h end

    return { x = x, y = y, width = w, height = h }
  end
end


return util






