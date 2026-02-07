-- Hintergrund-Konfiguration
local background_config = {
  top_left      = "Default:top_left.png",
  top_center    = "Default:top_center.png",
  top_right     = "Default:top_right.png",
  right_center  = "Default:right_center.png",
  bottom_right  = "Default:bottom_right.png",
  bottom_center = "Default:bottom_center.png",
  bottom_left   = "Default:bottom_left.png",
  left_center   = "Default:left_center.png",
  fill_color    = "#101010"
}

local layout_config = {
  top_width_offset    = 7,
  bottom_width_offset = 15,
  left_height_offset  = 5,
  right_height_offset = 5,
  fill_pos_offset     = 5,
  fill_width_offset   = 12,
  fill_height_offset  = 10
}


local function bounds(x, y, w, h)
  return function()
    local width, height = get_window_size()
    local rx = (x < 0) and (width + x) or x
    local ry = (y < 0) and (height + y) or y
    return { x = rx, y = ry, width = w, height = h }
  end
end

-- Generische Anchor-Funktion
-- mode: "top_left", "top_right", "bottom_left", "bottom_right"
local function anchor(x, y, w, h, mode)
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


local function ServerStatus()
  if ServerApi and ServerApi.StatusOf then
    local ok = ServerApi.StatusOf("127.0.0.1", 30001)
    local text = ok and "ServerStatus:[Online]" or "ServerStatus:[Offline]"
    local color = ok and Color.Green or Color.Red

    Controls.Add(Label("lb_server_status", bounds(-150, 40, 150, 20), text, Font("Segoe UI", 9, "Bold"), color))

    print(ok and "✅ Online" or "❌ Offline")
  else
    print("⚠️ ServerApi-Modul nicht verfügbar")
  end
end



local engine = require("actions.engine")
local http = require("actions.http")

  -- Progressbar (27px vom linken Rand, 30px vom unteren Rand,
  -- rechts -27px Abstand, Höhe 5px)
progressBar = ProgressBar("pb_update_progress", anchor(27, 30, -27, 5, "bottom_left"), Font("Segoe UI", 11, "Regular"), Color.Orange)
startBtn = Button("btn_start",
      bounds(-150, -70, 97, 22),
      T("button.start"),
      Font("Segoe UI", 11, "Regular"),
      Color.Orange,
      "Default",
      function()
        engine.start_game()
      end)

-- Rückgabe der gesamten Fensterdefinition
return {
  background = background_config,
  layout     = layout_config,

  init = function()

  ServerStatus()

    -- Title
    Controls.Add(
      Label("lb_title",
      bounds(35, 0, 200, 40),
      T("app.title"),
      Font("Chiller", 40, "Italic"),
      Color.Orange)
    )

    -- Subtitle
    Controls.Add(
      Label("lb_subtitle",
      bounds(-115, 12, 200, 27),
      T("app.subtitle"),
      Font("Malgun Gothic", 13, "Bold"),
      Color.Gold)
    )

    -- Buttons
    Controls.Add(
      Button("btn_exit",
      bounds(-35, 16, 18, 18),
      T("button.exit"),
      Font("Segoe UI", 10, "Regular"),
      Color.Orange,
      "Default",
      function()
        application_exit()
      end)
    )

    Controls.Add(startBtn)

    Controls.Add(
      Button("btn_settings",
      bounds(-255, -70, 97, 22),
      T("button.settings"),
      Font("Segoe UI", 11, "Regular"),
      Color.Orange,
      "Default",
      function()
        engine.open_settings()
        end)
    )

    Controls.Add(
      Button("btn_website",
      bounds(-360, -70, 97, 22),
      T("button.website"),
      Font("Segoe UI", 11, "Regular"),
      Color.Orange,
      "Default",
      function()
        http.open("https://google.com")
        end)
    )



    Controls.Add(progressBar)


    local changelogBox = TextBox("tb_changelog", 
        anchor(36, 55, -400, 200, "bottom_left"),
        "Changelog ...",
        Font("Segoe UI", 10, "Regular"),
        Color.White, MakeColor.FromHex("#101010"),
        true, true, MakeColor.FromHex("#7C6E4B"))

        -- Rahmen + Glow konfigurieren
        changelogBox.BorderColor = MakeColor.FromHex("#7C6E4B")
        changelogBox.BorderThickness = 3
        changelogBox.GlowEnabled = true
        changelogBox.GlowColor = Color.White
        changelogBox.GlowRadius = 6

    Controls.Add(changelogBox)


    -- Status-Label (27px vom linken Rand, 50px vom unteren Rand)
      Controls.Add(
        Label("lb_update_status",
        anchor(27, 20, 200, 20, "bottom_left"),
        T("status.waiting"),
        Font("Segoe UI", 8, "Italic"),
        Color.White)
      )

  end,
}
