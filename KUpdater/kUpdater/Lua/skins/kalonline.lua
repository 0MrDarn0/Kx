-- Module / Helpers
local engine = require("actions.engine")
local http   = require("actions.http")
local util   = require("util.helper")

-- Konfigurationen
local background_config = {
  top_left      = "KalOnline:Frame:top_left.png",
  top_center    = "KalOnline:Frame:top_center.png",
  top_right     = "KalOnline:Frame:top_right.png",
  right_center  = "KalOnline:Frame:right_center.png",
  bottom_right  = "KalOnline:Frame:bottom_right.png",
  bottom_center = "KalOnline:Frame:bottom_center.png",
  bottom_left   = "KalOnline:Frame:bottom_left.png",
  left_center   = "KalOnline:Frame:left_center.png",
  fill_bitmap   = "KalOnline:Frame:fill_bitmap.bmp",
  fill_color    = "#101010"
}

local layout_config = {
  top_width_offset    = 7,
  bottom_width_offset = 15,
  left_height_offset  = 5,
  right_height_offset = 5,
  fill_pos_offset     = 5,
  fill_width_offset   = 12, --12,
  fill_height_offset  = 9
}

-- Serverstatus-Funktion
local function ServerStatus()
  if ServerApi and ServerApi.StatusOf then
    local ok = ServerApi.StatusOf("127.0.0.1", 30001)
    local text = ok and "ServerStatus:[Online]" or "ServerStatus:[Offline]"
    local color = ok and Color.Green or Color.Red

    local serverStatusLabel = Label(
      "lb_server_status",
      util.make_bounds(-280, 14, 150, 20),
      text,
      Font("Segoe UI", 9, "Bold"),
      color
    )

    Controls.Add(serverStatusLabel)
    print(ok and "✅ Online" or "❌ Offline")

    return serverStatusLabel
  else
    print("⚠️ ServerApi-Modul nicht verfügbar")
    return nil
  end
end


-- Rückgabe der Fensterdefinition
return {
  background = background_config,
  layout     = layout_config,

  init = function()
    -- Serverstatus prüfen und Label (Variable) erhalten
    local serverStatusLabel = ServerStatus()

    -- Title
    local titleLabel = Label(
      "lb_title",
      util.make_bounds(35, 0, 200, 40),
      T("app.title"),
      Font("Chiller", 40, "Italic"),
      Color.Orange
    )
    Controls.Add(titleLabel)

    -- Subtitle
    local subtitleLabel = Label(
      "lb_subtitle",
      util.make_bounds(-115, 12, 200, 27),
      T("app.subtitle"),
      Font("Malgun Gothic", 13, "Bold"),
      Color.Gold
    )
    Controls.Add(subtitleLabel)

    -- Start Button
    local startBtn = Button(
      "btn_start",
      util.make_bounds(-150, -70, 97, 22),
      T("button.start"),
      Font("Segoe UI", 11, "Regular"),
      Color.Orange,
      "KalOnline:Buttons",
      function() engine.start_game() end
    )
    Controls.Add(startBtn)

    -- Exit Button
    local exitBtn = Button(
      "btn_exit",
      util.make_bounds(-35, 16, 18, 18),
      T("button.exit"),
      Font("Segoe UI", 10, "Regular"),
      Color.Orange,
      "KalOnline:Buttons",
      function() Application.Exit() end
    )
    Controls.Add(exitBtn)

    -- Settings Button
    local settingsBtn = Button(
      "btn_settings",
      util.make_bounds(-255, -70, 97, 22),
      T("button.settings"),
      Font("Segoe UI", 11, "Regular"),
      Color.Orange,
      "KalOnline:Buttons",
      function() engine.open_settings() end
    )
    Controls.Add(settingsBtn)

    -- Website Button
    local websiteBtn = Button(
      "btn_website",
      util.make_bounds(-360, -70, 97, 22),
      T("button.website"),
      Font("Segoe UI", 11, "Regular"),
      Color.Orange,
      "KalOnline:Buttons",
      function() http.open("https://google.com") end
    )
    Controls.Add(websiteBtn)


    local bgColor = MakeColor.FromHex("#00000000")
    local fillColor = MakeColor.FromHex("#564F3D")--#A49167
    local borderColor = MakeColor.FromHex("#7A623C")  --#564F3D  #7A623C
    local textColor = MakeColor.FromHex("#7B6D4A")--#BC7900 #282828
    
    -- ProgressBar
    local progressBar = ProgressBar(
      "pb_update_progress",
      util.make_anchor(22, 19, -20, 4, "bottom_left"),
      Font("Chiller", 19, "Bold"),
      textColor,
      fillColor,
      borderColor,
      bgColor)
    Controls.Add(progressBar)

    -- Changelog TextBox
    local changelogBox = TextBox(
      "tb_changelog",
      util.make_anchor(36, 55, -400, 200, "bottom_left"),
      "Changelog ...",
      Font("Segoe UI", 10, "Regular"),
      Color.White,
      MakeColor.FromHex("#101010"),
      true, true, MakeColor.FromHex("#7C6E4B")
    )
    changelogBox.BorderColor     = MakeColor.FromHex("#7C6E4B")
    changelogBox.BorderThickness = 3
    changelogBox.GlowEnabled     = true
    changelogBox.GlowColor       = Color.White
    changelogBox.GlowRadius      = 6
    Controls.Add(changelogBox)


    -- Status-Label
    local statusLabel = Label(
      "lb_update_status",
      util.make_anchor(30, 24, 200, 20, "bottom_left"),
      T("status.waiting"),
      Font("Segoe UI", 8, "Italic"),
      Color.White
    )
    Controls.Add(statusLabel)


    -- Event-Registrierungen
    Events.TryRegisterLua("StatusEvent", function(ev)
      statusLabel.Text = ev.Text
    end)


    Events.TryRegisterLua("ChangelogEvent", function(ev)
      changelogBox.Text = ev.Text
    end)


    Events.TryRegisterLua("ProgressEvent", function(ev)
      if not progressBar.Visible then
        progressBar.Visible = true
      end
      progressBar.Progress = clamp(ev.Percent / 100, 0, 1)
    end)


    Events.TryRegisterLua("UpdateRequired", function(ev)
      startBtn.Visible = false
      settingsBtn.Visible = false
    end)


    Events.TryRegisterLua("UpdatePipelineCompleted", function(ev)
      progressBar.Visible = false
      startBtn.Visible = true
      settingsBtn.Visible = true
    end)

    Events.TryRegisterLua("MainWindow_OnShown", function(ev)
      --print("MainWindow_OnShown")
      --progressBar.Progress = clamp(100 / 100, 0, 1)
      --progressBar.Visible = true

      if progressBar.Visible then
        progressBar.Visible = false
      end
    end)

    Events.TryRegisterLua("MainWindow_OnFormClosed", function(ev)
      if ev.IsUserInitiated then
        print("User closed the window")
      end
    end)

    --[[
        Events.PrintAllEvents()
    ]]

  end
}
