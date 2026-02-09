local engine = require("actions.engine")
local http = require("actions.http")
local util = require("util.helper")

-- Hintergrund-Konfiguration
local background_config = {
  top_left      = "KalOnline:Frame:top_left.png",
  top_center    = "KalOnline:Frame:top_center.png",
  top_right     = "KalOnline:Frame:top_right.png",
  right_center  = "KalOnline:Frame:right_center.png",
  bottom_right  = "KalOnline:Frame:bottom_right.png",
  bottom_center = "KalOnline:Frame:bottom_center.png",
  bottom_left   = "KalOnline:Frame:bottom_left.png",
  left_center   = "KalOnline:Frame:left_center.png",
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


local function ServerStatus()
  if ServerApi and ServerApi.StatusOf then
    local ok = ServerApi.StatusOf("127.0.0.1", 30001)
    local text = ok and "ServerStatus:[Online]" or "ServerStatus:[Offline]"
    local color = ok and Color.Green or Color.Red

    Controls.Add(Label("lb_server_status", util.make_bounds(-150, 40, 150, 20), text, Font("Segoe UI", 9, "Bold"), color))

    print(ok and "✅ Online" or "❌ Offline")
  else
    print("⚠️ ServerApi-Modul nicht verfügbar")
  end
end



  -- Progressbar (27px vom linken Rand, 30px vom unteren Rand,
  -- rechts -27px Abstand, Höhe 5px)
progressBar = ProgressBar("pb_update_progress",
      util.make_anchor(27, 30, -27, 5, "bottom_left"),
      Font("Segoe UI", 11, "Regular"),
      Color.Orange)

startBtn = Button("btn_start",
      util.make_bounds(-150, -70, 97, 22),
      T("button.start"),
      Font("Segoe UI", 11, "Regular"),
      Color.Orange,
      "KalOnline:Buttons",
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
      util.make_bounds(35, 0, 200, 40),
      T("app.title"),
      Font("Chiller", 40, "Italic"),
      Color.Orange)
    )

    -- Subtitle
    Controls.Add(
      Label("lb_subtitle",
      util.make_bounds(-115, 12, 200, 27),
      T("app.subtitle"),
      Font("Malgun Gothic", 13, "Bold"),
      Color.Gold)
    )


    -- Buttons
    Controls.Add(startBtn)

    Controls.Add(Button("btn_exit",
      util.make_bounds(-35, 16, 18, 18),
      T("button.exit"),
      Font("Segoe UI", 10, "Regular"),
      Color.Orange,
      "KalOnline:Buttons",
      function()
        application_exit()
      end)
    )

    Controls.Add(Button("btn_settings",
      util.make_bounds(-255, -70, 97, 22),
      T("button.settings"),
      Font("Segoe UI", 11, "Regular"),
      Color.Orange,
      "KalOnline:Buttons",
      function()
        engine.open_settings()
        end)
    )

    Controls.Add(Button("btn_website",
      util.make_bounds(-360, -70, 97, 22),
      T("button.website"),
      Font("Segoe UI", 11, "Regular"),
      Color.Orange,
      "KalOnline:Buttons",
      function()
        http.open("https://google.com")
        end)
    )


    Controls.Add(progressBar)


    local changelogBox = TextBox("tb_changelog", 
        util.make_anchor(36, 55, -400, 200, "bottom_left"),
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
        util.make_anchor(27, 20, 200, 20, "bottom_left"),
        T("status.waiting"),
        Font("Segoe UI", 8, "Italic"),
        Color.White)
      )

      --EventManager.TryRegisterLua("StatusEvent", function(ev)
        --print("ev:", ev)
        --print("ev.Text:", ev.Text)
      --end)

     --EventManager.PrintAllEvents()

  end,
}
